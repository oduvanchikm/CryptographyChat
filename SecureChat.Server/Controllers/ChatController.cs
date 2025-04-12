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
        _logger.LogInformation("[CreateChat] Creating new chat with {0}", request.ParticipantId);
        _logger.LogInformation("[CreateChat] Creating new chat");
        var currentUserId = GetCurrentUserId();
        int participantId = request.ParticipantId;
        _logger.LogInformation("[CreateChat] Creating new chat");
        
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

            _logger.LogInformation("[CreateChat] Creating new chat");
            
            var existingChat = await context.Chat
                .Where(c => c.ChatUser.Count() == 2 &&
                            c.ChatUser.Any(cu => cu.UserId == currentUserId) &&
                            c.ChatUser.Any(cu => cu.UserId == participantId))
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                return Ok(new { ChatId = existingChat.Id });
            }

            var chat = await _chatService.CreateChatAsync(currentUserId, participantId, participantUser.Username, "RC5");
            
            _logger.LogInformation("[CreateChat] chat id is {0}", chat.Id);

            _logger.LogInformation("[CreateChat] Creating new chat2");
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return Ok(new { ChatId = chat.Id });
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
            return Forbid();

        var messages = await _redisDb.ListRangeAsync($"chat:{chatId}:messages", -count, -1);
        var messageList = messages
            .Select(m => JsonSerializer.Deserialize<ChatMessageEvent>(m!))
            .Where(m => m != null)
            .ToList();

        return Ok(messageList);
    }
}