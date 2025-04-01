using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureChat.Database;

namespace SecureChat.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly ILogger<ChatsController> _logger;

    public ChatsController(IDbContextFactory<SecureChatDbContext> dbContextFactory, ILogger<ChatsController> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsersAsync([FromQuery] string search = null)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();
            var currentUserId = GetCurrentUserId();
            
            var query = context.Users
                .Where(u => u.Id != currentUserId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lowerSearch = search.ToLower();
                query = query.Where(u => EF.Functions.Like(u.Username.ToLower(), $"%{lowerSearch}%"));
            }


            var users = await query
                .OrderBy(u => u.Username)
                .Select(u => new 
                {
                    id = u.Id,
                    username = u.Username,
                    lastLogin = u.LastLogin
                })
                .ToListAsync();

            _logger.LogInformation($"Found {users.Count} users for search query: {search}");

            return Ok(users);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Authentication error while getting users");
            return Unauthorized(new { Message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred" });
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
    
    [HttpGet("userschats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllChatsAsync([FromQuery] string search = null)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();
            var currentUserId = GetCurrentUserId();

            var chatsQuery = context.ChatUser
                .Include(cu => cu.Chat)
                .ThenInclude(c => c.ChatUser)
                .ThenInclude(u => u.User)
                .Where(cu => cu.UserId == currentUserId)
                .Select(cu => new
                {
                    ChatId = cu.Chat.Id,
                    OtherUser = cu.Chat.ChatUser
                        .FirstOrDefault(u => u.UserId != currentUserId).User
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                chatsQuery = chatsQuery.Where(x => 
                    EF.Functions.Like(x.OtherUser.Username, $"%{search}%"));
            }

            var chats = await chatsQuery
                .Select(x => new
                {
                    id = x.ChatId,
                    name = x.OtherUser.Username,
                    avatar = $"https://i.pravatar.cc/150?u={x.OtherUser.Id}"
                })
                .ToListAsync();

            return Ok(chats);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Authentication error while getting chats");
            return Unauthorized(new { Message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting chats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred" });
        }
    }
}