using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
public class ChatController : ControllerBase
{
    // private readonly IHubContext<ChatHub> _hubContext;
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly ILogger<ChatController> _logger;
    private readonly IDatabase _redisDb;
    private readonly IConnectionMultiplexer _redis;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly IEncryptionService _encryptionService;

    public ChatController(
        IChatService chatService,
        IDbContextFactory<SecureChatDbContext> dbContextFactory,
        ILogger<ChatController> logger,
        IUserService userService,
        IConnectionMultiplexer redis,
        KafkaProducerService kafkaProducer,
        IEncryptionService encryptionService
        )
    {
        _chatService = chatService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _userService = userService;
        _redis = redis;
        _redisDb = redis.GetDatabase();
        _kafkaProducer = kafkaProducer;
        _encryptionService = encryptionService;
        // _hubContext = hubContext;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        _logger.LogInformation("PUBLIC KEY: ");
        _logger.LogInformation(JsonSerializer.Serialize(request));

        var currentUserId = GetCurrentUserId();
        int participantId = request.ParticipantId;

        _logger.LogInformation("[CreateChat] Creating new chat2");

        await using var context = _dbContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var participantUser = await _userService.GetUserByIdAsync(participantId);
            if (participantUser == null)
            {
                _logger.LogInformation("[CreateChat] error with creating new chat");
                return NotFound(new { message = "Participant user not found" });
            }

            _logger.LogInformation("[CreateChat] Creating new chat3");

            var existingChat = await context.Chat
                .Where(c => c.ChatUser.Count() == 2 &&
                            c.ChatUser.Any(cu => cu.UserId == currentUserId) &&
                            c.ChatUser.Any(cu => cu.UserId == participantId))
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                _logger.LogInformation("[CreateChat] Chat already exists");

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

            _logger.LogInformation(
                $"[CreateChat] INFORMATION ABOUT CRYPTO {request.Algorithm}, {request.Padding}, {request.ModeCipher}");

            var chat = await _chatService.CreateChatAsync(currentUserId,
                participantId, participantUser.Username, request.Algorithm, request.Padding,
                request.ModeCipher);

            _logger.LogInformation("[CreateChat] Creating new chat4");

            // var redisKey = $"chat:{chat.Id}:publicKey";
            // await _redisDb.StringSetAsync(redisKey, request.PublicKey, TimeSpan.FromMinutes(10));

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var otherUserIdNew = chat.ChatUser
                .Where(u => u.UserId != currentUserId)
                .Select(u => u.UserId)
                .FirstOrDefault();

            // var otherPublicKeyRedisKeyNew = $"chat:{chat.Id}:user:{otherUserIdNew}:publicKey";
            // var otherPublicKeyNew = await _redisDb.StringGetAsync(otherPublicKeyRedisKeyNew);

            return Ok(new
            {
                ChatId = chat.Id,
                // OtherPublicKey = otherPublicKeyNew.HasValue ? otherPublicKeyNew.ToString() : string.Empty
            });
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, "Error creating chat");
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

    [HttpPost("{chatId}/send")]
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageRequest request)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var senderId = GetCurrentUserId();
        if (!await _chatService.IsUserInChatAsync(chatId, senderId))
        {
            return Forbid();
        }

        var redisKey = $"chat:{chatId}:publicKey";
        await _redisDb.StringSetAsync(redisKey, request.PublicKey, TimeSpan.FromMinutes(10));

        var chat = await _chatService.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            return NotFound(new { message = "Chat not found" });
        }

        var messageBytes = Encoding.UTF8.GetBytes(request.Message);
        _logger.LogInformation($"Padding {chat.Padding}, cipher mode {chat.ModeCipher}");

        var encryptedContent = await _encryptionService.EncryptAsync(
            messageBytes,
            chat.Algorithm,
            PaddingMode.ToPaddingMode(chat.Padding),
            CipherMode.ToCipherMode(chat.ModeCipher),
            chatId
        );

        _logger.LogInformation("Message first " + request.Message);
        var base64EncryptedContent = Convert.ToBase64String(encryptedContent);

        var messageEvent = new ChatMessageEvent
        {
            ChatId = chatId,
            SenderId = senderId,
            EncryptedContent = base64EncryptedContent,
            SentAt = DateTime.UtcNow
        };

        Console.WriteLine("[SendMessage] BEFORE ENCRYPT: " + request.Message);
        Console.WriteLine("[SendMessage] ENCRYPT RESULT: " + encryptedContent);

        await _kafkaProducer.SendMessage(messageEvent);

        var redisMessagesKey = $"chat:{chatId}:messages";
        var json = JsonSerializer.Serialize(messageEvent);
        await _redisDb.ListRightPushAsync(redisMessagesKey, json);

        return Ok();
    }

    [HttpGet("{chatId}/history")]
    public async Task<IActionResult> GetHistory(int chatId, [FromQuery] int count = 50)
    {
        var userId = GetCurrentUserId();
        await using var context = _dbContextFactory.CreateDbContext();
    
        if (!await _chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
        }
    
        var chat = await _chatService.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            return NotFound(new { message = "Chat not found" });
        }
    
        var messages = await _redisDb.ListRangeAsync($"chat:{chatId}:messages", -count, -1);
    
        var messageList = messages
            .Select(m => JsonSerializer.Deserialize<ChatMessageEvent>(m!))
            .Where(m => m != null)
            .ToList();
    
        var enrichedMessages = new List<MessageWithSenderInfo>();
    
        foreach (var message in messageList)
        {
            var senderUser = await _userService.GetUserByIdAsync(message.SenderId);
    
            var result1 = message.EncryptedContent; 
            Console.WriteLine("var result1 = message.EncryptedContent " + result1);
            var result2 = Convert.FromBase64String(result1); 
            Console.WriteLine("result2 = Convert.FromBase64String(result1) " + result2);
    
            var decryptedBytes = await _encryptionService.DecryptAsync(
                result2,
                chat.Algorithm,
                PaddingMode.ToPaddingMode(chat.Padding),
                CipherMode.ToCipherMode(chat.ModeCipher),
                chatId
            );
    
            _logger.LogInformation("MESSAGE AFTER DECRYPTION " + decryptedBytes);
    
            enrichedMessages.Add(new MessageWithSenderInfo
            {
                SenderId = message.SenderId,
                EncryptedContent = Encoding.UTF8.GetString(decryptedBytes),
                SentAt = message.SentAt,
                SenderUsername = senderUser?.Username ?? "Unknown",
                IsCurrentUser = message.SenderId == userId
            });
        }
    
        return Ok(enrichedMessages);
    }

    [HttpGet("{chatId}/participantKey")]
    public async Task<IActionResult> GetParticipantKey(int chatId)
    {
        var userId = GetCurrentUserId();
        if (!await _chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
        }

        var chat = await _chatService.GetChatByIdAsync(chatId);
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
        if (!await _chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
        }

        var redisKey = $"chat:{chatId}:user:{userId}:publicKey";
        await _redisDb.StringSetAsync(redisKey, request.PublicKey, TimeSpan.FromHours(24));
        return Ok();
    }
}