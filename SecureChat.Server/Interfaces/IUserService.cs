using SecureChat.Common.Models;

namespace SecureChat.Server.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
}