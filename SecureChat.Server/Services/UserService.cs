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

    public async Task AddDhPublicKeyAsync(int userId, int chatId, string publicKey)
    {
        try
        {
            Console.WriteLine("bebebebeb");
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            Console.WriteLine("bebebebeb2");
            var key = new DhPublicKey
            {
                UserId = userId,
                ChatId = chatId,
                PublicKey = publicKey,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            Console.WriteLine("bebebebeb3");

            await context.DhPublicKey.AddAsync(key);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AddDhPublicKeyAsync] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetParticipantPublicKeyAsync(int chatId, int currentUserId)
    {
        Console.WriteLine("bebebebebebebebebebebebebebebeb");
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        try
        {
            var latestKey = await context.DhPublicKey
                .Where(k => k.ChatId == chatId && k.UserId != currentUserId)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => k.PublicKey)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(latestKey))
            {
                Console.WriteLine("[GetParticipantPublicKeyAsync] Public key not found");
                throw new KeyNotFoundException("Public key not found");
            }

            return latestKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetParticipantPublicKeyAsync] ERROR: {ex.Message}");
            throw;
        }
    }
}