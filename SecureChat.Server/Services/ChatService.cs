using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;

namespace SecureChat.Server.Services;

public class ChatService : IChatService
{
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly IProducer<int, ChatMessageEvent> _producer;
    // private readonly IConsumer<int, ChatMessageEvent> _consumer;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IDbContextFactory<SecureChatDbContext> dbContext, IProducer<int, ChatMessageEvent> producer,
        IConsumer<int, ChatMessageEvent> consumer, ILogger<ChatService> logger)
    {
        _dbContextFactory = dbContext;
        _producer = producer;
        // _consumer = consumer;
        _logger = logger;
    }

    public async Task<Chats?> GetChatByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Chat
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }


    public async Task<Chats> CreateChatAsync(int creatorId, int participantId, string username, 
        string algorithm, string padding, string modeCipher)
    {
        _logger.LogInformation("start creating new chat in chat service");
        await using var context = await _dbContextFactory.CreateDbContextAsync();
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

        _logger.LogInformation("finish creating new chat in chat service");
        return chat;
    }

    public async Task<bool> ChatExists(int idChat)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
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

        await _producer.ProduceAsync("chat-messages",
            new Message<int, ChatMessageEvent>
            {
                Key = chatId,
                Value = message
            });
    }

    // public async IAsyncEnumerable<ChatMessageEvent> StreamMessagesAsync(
    //     int chatId,
    //     [EnumeratorCancellation] CancellationToken ct = default)
    // {
    //     _consumer.Subscribe("chat-messages");
    //     try
    //     {
    //         while (!ct.IsCancellationRequested)
    //         {
    //             var consumeResult = _consumer.Consume(ct);
    //             if (consumeResult.Message.Value?.ChatId == chatId)
    //             {
    //                 yield return consumeResult.Message.Value;
    //             }
    //         }
    //     }
    //     finally
    //     {
    //         _consumer.Close();
    //     }
    // }

    public async Task<IEnumerable<Chats>> GetUserChatsAsync(int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Chat
            .Where(c => c.ChatUser.Any(cu => cu.UserId == userId))
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .ToListAsync();
    }

    public async Task<bool> IsUserInChatAsync(int chatId, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var result = await context.ChatUser
            .AnyAsync(cu => cu.ChatId == chatId && cu.UserId == userId);
        
        Console.WriteLine(result);

        return result;
    }
}