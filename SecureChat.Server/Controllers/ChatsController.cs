using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureChat.Database;

namespace SecureChat.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetUsers([FromQuery] string search)
    {
        try
        {
            int userId = GetCurrentUserId();

            await using var context = _dbContextFactory.CreateDbContext();

            var query = context.Users
                .Where(u => u.Id != userId);

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
                    username = u.Username
                })
                .ToListAsync();

            _logger.LogInformation($"Found {users.Count} users for search query: {search}");

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, "Internal server error");
        }
    }

    private int GetCurrentUserId()
    {
        _logger.LogInformation($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            _logger.LogError("User ID not found in claims!");
            throw new InvalidOperationException("User ID not found");
        }

        _logger.LogInformation($"User ID from claims: {userIdClaim.Value}");
        return int.Parse(userIdClaim.Value);
    }


    [HttpGet("userschats")]
    public async Task<IActionResult> GetAllChatsAsync([FromQuery] string search = null)
    {
        try
        {
            await using var context = _dbContextFactory.CreateDbContext();
            var currentUserId = GetCurrentUserId();

            var chats = await context.ChatUser
                .Include(cu => cu.Chat)
                .ThenInclude(c => c.ChatUser)
                .ThenInclude(cu => cu.User)
                .Where(cu => cu.UserId == currentUserId)
                .Select(cu => new
                {
                    ChatId = cu.ChatId,
                    OtherUsers = cu.Chat.ChatUser
                        .Where(x => x.UserId != currentUserId)
                        .Select(x => new
                        {
                            x.User.Id,
                            x.User.Username
                        })
                })
                .ToListAsync();

            var result = chats
                .Select(c => new
                {
                    id = c.ChatId,
                    name = string.Join(", ", c.OtherUsers.Select(u => u.Username)),
                    avatar = c.OtherUsers.Any()
                        ? $"https://i.pravatar.cc/150?u={c.OtherUsers.First().Id}"
                        : "https://i.pravatar.cc/150?u=default"
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(x => EF.Functions.Like(x.name, $"%{search}%"));
            }

            return Ok(result.ToList());
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