using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureChat.Broker.Services;
using SecureChat.Common.Models;
using SecureChat.Common.ViewModels;
using SecureChat.Database;
using SecureChat.Server.Interfaces;
using StackExchange.Redis;
using PaddingMode = Cryptography.PaddingMode.PaddingMode;
using CipherMode = Cryptography.CipherMode.CipherMode;

namespace SecureChat.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController(
    IChatService chatService,
    IDbContextFactory<SecureChatDbContext> dbContextFactory,
    ILogger<ChatController> logger,
    IUserService userService,
    IConnectionMultiplexer redis,
    KafkaProducerService kafkaProducer,
    IEncryptionService encryptionService)
    : ControllerBase
{
    private readonly IDatabase _redisDb = redis.GetDatabase();

    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        // logger.LogInformation("PUBLIC KEY: ");
        logger.LogInformation(JsonSerializer.Serialize(request));

        var currentUserId = GetCurrentUserId();
        int participantId = request.ParticipantId;

        // logger.LogInformation("[CreateChat] Creating new chat2");

        await using var context = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var participantUser = await userService.GetUserByIdAsync(participantId);
            if (participantUser == null)
            {
                logger.LogInformation("[CreateChat] error with creating new chat");
                return NotFound(new { message = "Participant user not found" });
            }

            // logger.LogInformation("[CreateChat] Creating new chat3");

            var existingChat = await context.Chat
                .Where(c => c.ChatUser.Count() == 2 &&
                            c.ChatUser.Any(cu => cu.UserId == currentUserId) &&
                            c.ChatUser.Any(cu => cu.UserId == participantId))
                .Include(chats => chats.ChatUser)
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                // logger.LogInformation("[CreateChat] Chat already exists");

                var otherUserId = existingChat.ChatUser
                    .Where(u => u.UserId != currentUserId)
                    .Select(u => u.UserId)
                    .FirstOrDefault();

                var otherPublicKeyRedisKey = $"chat:{existingChat.Id}:user:{otherUserId}:publicKey";
                var otherPublicKey = await _redisDb.StringGetAsync(otherPublicKeyRedisKey);

                return Ok(new
                {
                    ChatId = existingChat.Id,
                    OtherPublicKey = otherPublicKey.HasValue ? otherPublicKey.ToString() : string.Empty
                });
            }

            // logger.LogInformation(
            //     $"[CreateChat] INFORMATION ABOUT CRYPTO {request.Algorithm}, {request.Padding}, {request.ModeCipher}");

            var chat = await chatService.CreateChatAsync(currentUserId,
                participantId, participantUser.Username, request.Algorithm, request.Padding,
                request.ModeCipher);

            // logger.LogInformation("[CreateChat] Creating new chat4");
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return Ok(new
            {
                ChatId = chat.Id,
            });
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, "Error creating chat");
            return StatusCode(500, new { message = "Error creating chat" });
        }
    }

    private int GetCurrentUserId()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return userId;
        }

        throw new InvalidOperationException("Invalid user ID in claims");
    }
    
    [HttpDelete("{chatId}")]
    public async Task<IActionResult> DeleteChat(int chatId)
    {
        var currentUserId = GetCurrentUserId();
        
        await using var context = await dbContextFactory.CreateDbContextAsync();
    
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var success = await chatService.DeleteChatAsync(chatId, currentUserId, redis);
        
            if (!success)
            {
                return NotFound(new { message = "Chat not found or you don't have permissions" });
            }
        
            await kafkaProducer.SendMessage(new ChatDeletedEvent
            {
                ChatId = chatId,
                DeletedAt = DateTime.UtcNow
            });

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error deleting chat");
            return StatusCode(500, new { message = "Error deleting chat" });
        }
    }

    [HttpPost("{chatId}/send")] // конц токен
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageRequest request)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var senderId = GetCurrentUserId();
        if (!await chatService.IsUserInChatAsync(chatId, senderId))
        {
            return Forbid();
        }

        var redisKey = $"chat:{chatId}:user:{senderId}:publicKey";
        await _redisDb.StringSetAsync(redisKey, request.PublicKey, TimeSpan.FromHours(24));

        var chat = await chatService.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            return NotFound(new { message = "Chat not found" });
        }
        // logger.LogInformation($"Padding {chat.Padding}, cipher mode {chat.ModeCipher}");

        byte[] messageBytes;
        
        // получаем сообщение в стринге 
        // Console.WriteLine("1. [SendMessage] Message: " + request.Message);
    
        if (request.ContentType == "file" || request.ContentType.StartsWith("video/") || 
            request.ContentType.StartsWith("audio/"))
        {
            if (request.Message.Length > 10 * 1024 * 1024 * 4 / 3)
            {
                return BadRequest("File size exceeds 5MB limit");
            }
            
            // для файлов оставляем как есть
            messageBytes = Convert.FromBase64String(request.Message);
        }
        else
        {
            // переводим сообщение в байты
            messageBytes = Encoding.UTF8.GetBytes(request.Message);
            Console.WriteLine("2. [SendMessage] Message in bytes : " + BitConverter.ToString(messageBytes));
        }

        // шифруем сообщение
        var encryptedContent = await encryptionService.EncryptAsync(
            messageBytes,
            chat.Algorithm,
            PaddingMode.ToPaddingMode(chat.Padding),
            CipherMode.ToCipherMode(chat.ModeCipher),
            chatId,
            senderId
        );
        
        // Console.WriteLine("3. [SendMessage] Encrypted content : " + BitConverter.ToString(encryptedContent));
        
        // переводим результат из байтов в стрингу
        var base64EncryptedContent = Convert.ToBase64String(encryptedContent);
        // Console.WriteLine("4. [SendMessage] Encrypted Base64: " + base64EncryptedContent);

        // добавляем необходимые данные
        var messageEvent = new ChatMessageEvent
        {
            ChatId = chatId,
            SenderId = senderId,
            EncryptedContent = base64EncryptedContent,
            SentAt = DateTime.UtcNow,
            ContentType = request.ContentType ?? "text",
            FileName = request.FileName
        };
        
        // отправляем в кафку
        await kafkaProducer.SendMessage(messageEvent);

        var key = $"chat:{chatId}:messages";
        var json = JsonSerializer.Serialize(messageEvent);
        await _redisDb.ListRightPushAsync(key, json);

        return Ok();
    }

    [HttpGet("{chatId}/history")]
    public async Task<IActionResult> GetHistory(int chatId, [FromQuery] int count = 50)
    {
        // logger.LogInformation($"[GetHistory] ChatId: {chatId}");
        var userId = GetCurrentUserId();
        await using var context = dbContextFactory.CreateDbContext();
    
        if (!await chatService.IsUserInChatAsync(chatId, userId))
        {
            logger.LogInformation($"[GetHistory] User {userId} not in chat");
            return Forbid();
        }
    
        var chat = await chatService.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            logger.LogInformation($"[GetHistory] Chat {chatId} not in chat");
            return NotFound(new { message = "Chat not found" });
        }
        
        var messages = await _redisDb.ListRangeAsync($"chat:{chatId}:messages", -count, -1);
        if (!messages.Any())
        {
            logger.LogInformation($"[GetHistory] Chat {chatId} has no messages");
        }
        
        var messageList = messages
            .Select(m => JsonSerializer.Deserialize<ChatMessageEvent>(m!))
            .Where(m => m != null)
            .ToList();
    
        var enrichedMessages = new List<MessageWithSenderInfo>();
    
        foreach (var message in messageList)
        {
            var senderUser = await userService.GetUserByIdAsync(message.SenderId);
            
            // получаем сообщение в стринге
            string messageString = message.EncryptedContent;
            // Console.WriteLine("1. [HistoryMessage] Message in string: " + messageString);
            
            try 
            {
                // декодируем из Base64 в байты
                byte[] encryptedBytes = Convert.FromBase64String(messageString);
                // Console.WriteLine("2. [HistoryMessage] Encrypted bytes: " + BitConverter.ToString(encryptedBytes));

                // дешифруем сообщение
                var decryptedBytes = await encryptionService.DecryptAsync(
                    encryptedBytes,
                    chat.Algorithm,
                    PaddingMode.ToPaddingMode(chat.Padding),
                    CipherMode.ToCipherMode(chat.ModeCipher),
                    chatId,
                    message.SenderId
                );
                
                string decryptedContent;
                if (message.ContentType == "file" || message.ContentType == "application/pdf" || message.ContentType == "application/txt")
                {
                    decryptedContent = Convert.ToBase64String(decryptedBytes);
                }
                else
                {
                    // декодируем оригинальное сообщение из байтов
                    // Для текста декодируем в строку
                    decryptedContent = Encoding.UTF8.GetString(decryptedBytes);
                    // Console.WriteLine("3. [HistoryMessage] Decrypted message: " + decryptedContent);
                }
                
                enrichedMessages.Add(new MessageWithSenderInfo
                {
                    SenderId = message.SenderId,
                    EncryptedContent = decryptedContent, 
                    SentAt = message.SentAt,
                    SenderUsername = senderUser?.Username ?? "Unknown",
                    IsCurrentUser = message.SenderId == userId,
                    ContentType = message.ContentType,
                    FileName = message.FileName
                });
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Base64 decode error: {ex.Message}"); 
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
            }
        }
    
        return Ok(enrichedMessages.OrderBy(m => m.SentAt));
    }

    [HttpGet("{chatId}/participantKey")]
    public async Task<IActionResult> GetParticipantKey(int chatId)
    {
        var userId = GetCurrentUserId();
        if (!await chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
        }

        var chat = await chatService.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            return NotFound(new { message = "Chat not found" });
        }

        var participantId = chat.ChatUser.FirstOrDefault(u => u.UserId != userId)?.UserId;

        if (participantId == null)
        {
            return NotFound(new { message = "Participant not found" });
        }

        var redisKey = $"chat:{chatId}:user:{participantId}:publicKey";
        var publicKey = await _redisDb.StringGetAsync(redisKey);

        if (publicKey.IsNullOrEmpty)
        {
            return NotFound(new { message = "Public key not found" });
        }

        return Ok(new { publicKey = publicKey.ToString() });
    }

    [HttpPost("{chatId}/updateKey")]
    public async Task<IActionResult> UpdatePublicKey(int chatId, [FromBody] UpdateKeyRequest request)
    {
        var userId = GetCurrentUserId();
        if (!await chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
        }
        
        if (string.IsNullOrEmpty(request.PublicKey) || !IsBase64(request.PublicKey))
        {
            return BadRequest(new { message = "Invalid public key format" });
        }

        var redisKey = $"chat:{chatId}:user:{userId}:publicKey";
        await _redisDb.StringSetAsync(redisKey, request.PublicKey, TimeSpan.FromHours(24));
        return Ok();
    }
    
    private bool IsBase64(string base64)
    {
        Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}