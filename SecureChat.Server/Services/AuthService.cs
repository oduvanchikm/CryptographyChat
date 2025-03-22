using Microsoft.EntityFrameworkCore;
using SecureChat.Common;
using SecureChat.Common.Models;
using SecureChat.Database;

namespace SecureChat.Server.Services;

public class AuthService
{
    private readonly SecureChatDbContext _dbContext;

    public AuthService(SecureChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
        {
            return false;
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(password)
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return true;
    }
    
    public async Task<User> LoginAsync(string email, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
        {
            return null; // Неверный логин или пароль
        }

        return user;
    }
}