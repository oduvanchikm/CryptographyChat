using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Services;

public class UserService(IDbContextFactory<SecureChatDbContext> dbContext) : IUserService
{
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        await using var context = await dbContext.CreateDbContextAsync();
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}