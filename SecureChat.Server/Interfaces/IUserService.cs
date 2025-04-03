using SecureChat.Common.Models;

namespace SecureChat.Server.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> UserExists(string username);
    Task<IEnumerable<User>> SearchUsers(string query, int currentUserId);
    Task AddDhPublicKeyAsync(int userId, int chatId, string publicKey);
}