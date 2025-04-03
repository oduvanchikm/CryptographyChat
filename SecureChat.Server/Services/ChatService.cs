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
    private readonly SecureChatDbContext _dbContext;
    private readonly IProducer<int, ChatMessageEvent> _producer;
    private readonly IConsumer<int, ChatMessageEvent> _consumer;

    public ChatService(SecureChatDbContext dbContext, IProducer<int, ChatMessageEvent> producer, 
        IConsumer<int, ChatMessageEvent> consumer)
    {
        _dbContext = dbContext;
        _producer = producer;
        _consumer = consumer;
    }

    public async Task<Chats?> GetChatByIdAsync(int id)
    {
        return await _dbContext.Chat
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Chats> CreateChatAsync(int creatorId, int participantId, string algorithm)
    {
        var chat = new Chats
        {
            Algorithm = algorithm,
            ChatUser = new List<ChatUser>
            {
                new() { UserId = creatorId },
                new() { UserId = participantId }
            }
        };

        await _dbContext.Chat.AddAsync(chat);
        await _dbContext.SaveChangesAsync();

        return chat;
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

    public async IAsyncEnumerable<ChatMessageEvent> StreamMessagesAsync(
        int chatId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _consumer.Subscribe("chat-messages");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(ct);
                if (consumeResult.Message.Value?.ChatId == chatId)
                {
                    yield return consumeResult.Message.Value;
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public async Task<IEnumerable<Chats>> GetUserChatsAsync(int userId)
    {
        return await _dbContext.Chat
            .Where(c => c.ChatUser.Any(cu => cu.UserId == userId))
            .Include(c => c.ChatUser)
            .ThenInclude(cu => cu.User)
            .ToListAsync();
    }

    // public async Task<string> PerformKeyExchangeAsync(int chatId, int userId, string partnerPublicKey)
    // {
    //     using var dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    //     var publicKey = Convert.ToBase64String(dh.PublicKey.ExportSubjectPublicKeyInfo());
    //
    //     var key = new DhPublicKey
    //     {
    //         UserId = userId,
    //         ChatId = chatId,
    //         PublicKey = publicKey,
    //         CreatedAt = DateTime.UtcNow,
    //         ExpiresAt = DateTime.UtcNow.AddHours(24)
    //     };
    //
    //     await _dbContext.DhPublicKey.AddAsync(key);
    //     await _dbContext.SaveChangesAsync();
    //
    //     var otherPartyKey = ECDiffieHellmanPublicKey.CreateFromSubjectPublicKeyInfo(
    //         Convert.FromBase64String(partnerPublicKey));
    //
    //     return Convert.ToBase64String(dh.DeriveKeyMaterial(otherPartyKey));
    // }
}