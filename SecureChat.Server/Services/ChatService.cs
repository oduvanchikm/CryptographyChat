using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Services;

public class ChatService(
    IDbContextFactory<SecureChatDbContext> dbContext,
    IProducer<int, ChatMessageEvent> producer,
    ILogger<ChatService> logger)
    : IChatService
{
    public async Task<Chats?> GetChatByIdAsync(int id)
    {
        await using var context = await dbContext.CreateDbContextAsync();
        return await context.Chat
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }


    public async Task<Chats> CreateChatAsync(int creatorId, int participantId, string username, 
        string algorithm, string padding, string modeCipher)
    {
        logger.LogInformation("start creating new chat in chat service");
        await using var context = await dbContext.CreateDbContextAsync();
        var chat = new Chats
        {
            Algorithm = algorithm,
            Padding = padding,
            ModeCipher = modeCipher,
            Name = username,
            ChatUser = new List<ChatUser>
            {
                new() { UserId = creatorId },
                new() { UserId = participantId }
            }
        };

        await context.Chat.AddAsync(chat);
        await context.SaveChangesAsync();

        logger.LogInformation("finish creating new chat in chat service");
        return chat;
    }

    public async Task<bool> ChatExists(int idChat)
    {
        await using var context = await dbContext.CreateDbContextAsync();
        return await context.Chat.AnyAsync(u => u.Id == idChat);
    }

    public async Task AddMessageAsync(int chatId, int senderId, string encryptedContent)
    {
        var message = new ChatMessageEvent
        {
            ChatId = chatId,
            SenderId = senderId,
            EncryptedContent = encryptedContent,
            SentAt = DateTime.UtcNow
        };

        await producer.ProduceAsync("chat-messages",
            new Message<int, ChatMessageEvent>
            {
                Key = chatId,
                Value = message
            });
    }

    public async Task<IEnumerable<Chats>> GetUserChatsAsync(int userId)
    {
        await using var context = await dbContext.CreateDbContextAsync();
        return await context.Chat
            .Where(c => c.ChatUser.Any(cu => cu.UserId == userId))
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .ToListAsync();
    }

    public async Task<bool> IsUserInChatAsync(int chatId, int userId)
    {
        await using var context = await dbContext.CreateDbContextAsync();
        
        var result = await context.ChatUser
            .AnyAsync(cu => cu.ChatId == chatId && cu.UserId == userId);
        
        Console.WriteLine(result);

        return result;
    }
}