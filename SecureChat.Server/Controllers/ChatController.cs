using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Common.ViewModels;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IDbContextFactory<SecureChatDbContext> dbContextFactory,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        _logger.LogInformation("Creating new chat");
        var currentUserId = GetCurrentUserId();
        var chat = await _chatService.CreateChatAsync(currentUserId, request.ParticipantId, "RC5");
        _logger.LogInformation("Creating new chat2");
        return Ok(new { ChatId = chat.Id });
    }

    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChatInfo(int chatId)
    {
        _logger.LogInformation("GetChatInfo1");
        await using var context = _dbContextFactory.CreateDbContext();
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("GetChatInfo2");

        var chat = await context.Chat
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.ChatUser.Any(cu => cu.UserId == currentUserId));

        _logger.LogInformation("GetChatInfo3");
        if (chat == null)
        {
            _logger.LogInformation("GetChatInfo3,5");
            return NotFound();
        }

        var otherUser = chat.ChatUser.First(cu => cu.UserId != currentUserId).User;
        _logger.LogInformation("GetChatInfo4");

        return Ok(new {
            ChatId = chat.Id,
            OtherUserId = otherUser.Id,
            Username = otherUser.Username,
            Avatar = $"https://i.pravatar.cc/150?u={otherUser.Id}"
        });
    }
    
    private int GetCurrentUserId()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return userId;
        }
        throw new InvalidOperationException("Invalid user ID in claims");
    }


    [HttpPost("{chatId}/messages")]
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageRequest request)
    {
        _logger.LogInformation("SendMessage1");
        await _chatService.AddMessageAsync(
            chatId,
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
            request.EncryptedContent);

        return Ok();
    }

    [HttpGet("{chatId}/messages")]
    public async IAsyncEnumerable<ChatMessageEvent> GetMessages(int chatId, CancellationToken ct)
    {
        await foreach (var message in _chatService.StreamMessagesAsync(chatId, ct))
        {
            yield return message;
        }
    }
}