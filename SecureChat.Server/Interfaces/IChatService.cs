using SecureChat.Common.Models;

namespace SecureChat.Server.Interfaces;

public interface IChatService
{
    Task<Chats?> GetChatByIdAsync(int id);
    Task<Chats> CreateChatAsync(int creatorId, int participantId, string username, string algorithm);
    Task AddMessageAsync(int chatId, int senderId, string encryptedContent);
    IAsyncEnumerable<ChatMessageEvent> StreamMessagesAsync(int chatId, CancellationToken ct = default);
    Task<IEnumerable<Chats>> GetUserChatsAsync(int userId);
    Task<bool> IsUserInChatAsync(int chatId, int userId);
}