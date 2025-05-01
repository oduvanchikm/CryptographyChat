using SecureChat.Common.Models;
using StackExchange.Redis;

namespace SecureChat.Server.Interfaces;

public interface IChatService
{
    Task<Chats?> GetChatByIdAsync(int id);
    Task<Chats> CreateChatAsync(int creatorId, int participantId, string username, string algorithm,
        string padding, string modeCipher);
    Task<bool> IsUserInChatAsync(int chatId, int userId);
    Task<bool> DeleteChatAsync(int chatId, int userId, IConnectionMultiplexer redis);
}