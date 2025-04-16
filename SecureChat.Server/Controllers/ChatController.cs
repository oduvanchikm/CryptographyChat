using System.Security.Claims;
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

namespace SecureChat.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly ILogger<ChatController> _logger;
    private readonly IDatabase _redisDb;
    private readonly IConnectionMultiplexer _redis;
    private readonly KafkaProducerService _kafkaProducer;

    public ChatController(
        IChatService chatService,
        IDbContextFactory<SecureChatDbContext> dbContextFactory,
        ILogger<ChatController> logger,
        IUserService userService,
        IConnectionMultiplexer redis,
        KafkaProducerService kafkaProducer)
    {
        _chatService = chatService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _userService = userService;
        _redis = redis;
        _redisDb = redis.GetDatabase();
        _kafkaProducer = kafkaProducer;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        _logger.LogInformation("PUBLIC KEY: ");
        _logger.LogInformation(JsonSerializer.Serialize(request));
        _logger.LogInformation("[CreateChat] Creating new chat with {0}", request.ParticipantId);
        _logger.LogInformation("[CreateChat] Creating new chat1");
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
                return Ok(new { ChatId = existingChat.Id });
            }

            var chat = await _chatService.CreateChatAsync(currentUserId, 
                participantId, participantUser.Username, "RC5", request.PublicKey);
            
            _logger.LogInformation("[CreateChat] Creating new chat4");
            
            await _userService.AddDhPublicKeyAsync(currentUserId, chat.Id, request.PublicKey);
            
            Console.WriteLine($"[CreateChat] PUBLIC KEY {request.PublicKey}");
            _logger.LogInformation($"[CreateChat] PUBLIC KEY {request.PublicKey}");
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { 
                ChatId = chat.Id,
                OtherPublicKey = (await _userService.GetParticipantPublicKeyAsync(chat.Id, currentUserId)) ?? string.Empty
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
        var senderId = GetCurrentUserId();

        if (!await _chatService.IsUserInChatAsync(chatId, senderId))
            return Forbid();

        var messageEvent = new ChatMessageEvent
        {
            ChatId = chatId,
            SenderId = senderId,
            EncryptedContent = request.Message,
            SentAt = DateTime.UtcNow
        };

        await _kafkaProducer.SendMessage(messageEvent);

        var redisKey = $"chat:{chatId}:messages";
        var json = JsonSerializer.Serialize(messageEvent);
        await _redisDb.ListRightPushAsync(redisKey, json);

        return Ok();
    }
    
    [HttpGet("{chatId}/history")]
    public async Task<IActionResult> GetHistory(int chatId, [FromQuery] int count = 50)
    {
        var userId = GetCurrentUserId();

        if (!await _chatService.IsUserInChatAsync(chatId, userId))
        {
            return Forbid();
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

            enrichedMessages.Add(new MessageWithSenderInfo
            {
                SenderId = message.SenderId,
                EncryptedContent = message.EncryptedContent,
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
        _logger.LogInformation("[GetParticipantKey] Getting participant key");
        var userId = GetCurrentUserId();
        _logger.LogInformation($"[GetParticipantKey] {userId} and {chatId}");
        if (!await _chatService.IsUserInChatAsync(chatId, userId))
        {
            _logger.LogInformation("[GetParticipantKey] error with creating participant key1");
            return Forbid();
        }
        _logger.LogInformation($"[GetParticipantKey] {userId} and {chatId}");

        string key = null;

        try
        {
            key = await _userService.GetParticipantPublicKeyAsync(chatId, userId);
            return Ok(new { publicKey = key });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogInformation("[GetParticipantKey] Public key not found");
            return NotFound(new { message = "Public key not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetParticipantKey] Unexpected error");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    [HttpPost("{chatId}/updateKey")]
    public async Task<IActionResult> UpdatePublicKey(int chatId, [FromBody] UpdateKeyRequest request)
    {
        var userId = GetCurrentUserId();
        if (!await _chatService.IsUserInChatAsync(chatId, userId))
            return Forbid();

        await _userService.AddDhPublicKeyAsync(userId, chatId, request.PublicKey);
        return Ok();
    }
}