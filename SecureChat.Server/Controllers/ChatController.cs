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

    public ChatController(
        IChatService chatService,
        IDbContextFactory<SecureChatDbContext> dbContextFactory)
    {
        _chatService = chatService;
        _dbContextFactory = dbContextFactory;
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var chat = await _chatService.CreateChatAsync(currentUserId, request.ParticipantId, "ECDH-AES256");
        return Ok(new { ChatId = chat.Id });
    }

    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChatInfo(int chatId)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var currentUserId = GetCurrentUserId();

        var chat = await context.Chat
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.ChatUser.Any(cu => cu.UserId == currentUserId));

        if (chat == null) return NotFound();

        var otherUser = chat.ChatUser.First(cu => cu.UserId != currentUserId).User;

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