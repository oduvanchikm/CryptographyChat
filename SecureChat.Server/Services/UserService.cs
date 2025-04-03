using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Services;

public class UserService : IUserService
{
    private readonly SecureChatDbContext _dbContext;

    public UserService(SecureChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UserExists(string username)
    {
        return await _dbContext.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<User>> SearchUsers(string query, int currentUserId)
    {
        return await _dbContext.Users.Where(u => u.Username.Contains(query) && u.Id != currentUserId).ToListAsync();
    }

    public async Task AddDhPublicKeyAsync(int userId, int chatId, string publicKey)
    {
        var key = new DhPublicKey
        {
            UserId = userId,
            ChatId = chatId,
            PublicKey = publicKey,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _dbContext.DhPublicKey.AddAsync(key);
        await _dbContext.SaveChangesAsync();
    }
}