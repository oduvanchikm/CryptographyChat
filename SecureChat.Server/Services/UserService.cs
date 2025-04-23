using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Services;

public class UserService : IUserService
{
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;

    public UserService(IDbContextFactory<SecureChatDbContext> dbContext)
    {
        _dbContextFactory = dbContext;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UserExists(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<User>> SearchUsers(string query, int currentUserId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users.Where(u => u.Username.Contains(query) && u.Id != currentUserId).ToListAsync();
    }
}