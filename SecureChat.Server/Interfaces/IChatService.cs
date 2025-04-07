using SecureChat.Common.Models;

namespace SecureChat.Server.Interfaces;

public interface IChatService
{
    Task<Chats?> GetChatByIdAsync(int id);
    Task<Chats> CreateChatAsync(int creatorId, int participantId, string algorithm);
    Task AddMessageAsync(int chatId, int senderId, string encryptedContent);
    IAsyncEnumerable<ChatMessageEvent> StreamMessagesAsync(int chatId, CancellationToken ct = default);
    Task<IEnumerable<Chats>> GetUserChatsAsync(int userId);
    // Task<bool> CheckKafkaConnectionAsync();
    // Task<string> PerformKeyExchangeAsync(int chatId, int userId, string partnerPublicKey);
}