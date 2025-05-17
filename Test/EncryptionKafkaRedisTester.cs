using System.Security.Cryptography;
using System.Text;
using SecureChat.Broker;
using SecureChat.Broker.Services;
using SecureChat.Common.Models;
using SecureChat.Server.Interfaces;
using StackExchange.Redis;
using CipherMode = Cryptography.CipherMode.CipherMode;
using PaddingMode = Cryptography.PaddingMode.PaddingMode;

namespace Test;

public class EncryptionKafkaRedisTester
{
    private readonly KafkaProducerService _kafkaProducer;
    private readonly IDatabase _redisDb;
    private readonly IEncryptionService _encryptionService;
    private readonly Chats _chat;
    private readonly int _chatId;
    private readonly int _senderId;

    public EncryptionKafkaRedisTester(
        KafkaProducerService kafkaProducer,
        IConnectionMultiplexer redis,
        IEncryptionService encryptionService,
        Chats chat,
        int chatId,
        int senderId)
    {
        _kafkaProducer = kafkaProducer;
        _redisDb = redis.GetDatabase();
        _encryptionService = encryptionService;
        _chat = chat;
        _chatId = chatId;
        _senderId = senderId;
    }

    public async Task RunTestAsync(string plainText, string contentType = "text")
    {
        Console.WriteLine("---- ENCRYPTION KAFKA REDIS TEST START ----");

        // 1. Исходные данные
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var originalHash = SHA256.HashData(plainBytes);
        Console.WriteLine($"[1] Original Text: {plainText}");
        Console.WriteLine($"[1] SHA256: {Convert.ToHexString(originalHash)}");

        // 2. Шифруем
        var encryptedBytes = await _encryptionService.EncryptAsync(
            plainBytes,
            _chat.Algorithm,
            PaddingMode.ToPaddingMode(_chat.Padding),
            CipherMode.ToCipherMode(_chat.ModeCipher),
            _chatId,
            _senderId
        );
        var base64Encrypted = Convert.ToBase64String(encryptedBytes);
        Console.WriteLine($"[2] Encrypted (base64): {base64Encrypted.Substring(0, 50)}... (len={base64Encrypted.Length})");

        // 3. Отправка в Kafka
        var message = new ChatMessageEvent
        {
            ChatId = _chatId,
            SenderId = _senderId,
            EncryptedContent = base64Encrypted,
            SentAt = DateTime.UtcNow,
            ContentType = contentType
        };

        await _kafkaProducer.SendMessage(message);
        Console.WriteLine("[3] Kafka: message sent");

        // 4. Сохраняем в Redis
        var json = new KafkaSerialization.JsonSerializer<ChatMessageEvent>().Serialize(message, default);
        var redisKey = $"chat:{_chatId}:messages:test";
        await _redisDb.ListRightPushAsync(redisKey, json);
        Console.WriteLine("[4] Redis: message saved");

        // 5. Чтение из Redis и проверка
        var fromRedisRaw = await _redisDb.ListRightPopAsync(redisKey);
        var fromRedis = new KafkaSerialization.JsonDeserializer<ChatMessageEvent>()
            .Deserialize(Encoding.UTF8.GetBytes(fromRedisRaw!), false, default);
        Console.WriteLine("[5] Redis: message loaded");

        if (fromRedis is null)
        {
            Console.WriteLine("❌ [ERROR] Redis message null");
            return;
        }

        // 6. Base64 → Bytes
        try
        {
            var encryptedFromRedis = Convert.FromBase64String(fromRedis.EncryptedContent);
            var redisHash = SHA256.HashData(encryptedFromRedis);
            Console.WriteLine($"[6] Base64 decoded. Length: {encryptedFromRedis.Length}, SHA256: {Convert.ToHexString(redisHash)}");

            // 7. Расшифровка
            var decrypted = await _encryptionService.DecryptAsync(
                encryptedFromRedis,
                _chat.Algorithm,
                PaddingMode.ToPaddingMode(_chat.Padding),
                CipherMode.ToCipherMode(_chat.ModeCipher),
                _chatId,
                _senderId
            );

            var decryptedText = contentType == "text"
                ? Encoding.UTF8.GetString(decrypted)
                : Convert.ToBase64String(decrypted);

            Console.WriteLine($"[7] Decrypted Text: {decryptedText}");
            var decryptedHash = SHA256.HashData(decrypted);
            Console.WriteLine($"[7] SHA256 Decrypted: {Convert.ToHexString(decryptedHash)}");

            if (originalHash.SequenceEqual(decryptedHash))
                Console.WriteLine("✅ SUCCESS: Data matches original.");
            else
                Console.WriteLine("❌ MISMATCH: Decrypted data differs from original.");

        }
        catch (FormatException ex)
        {
            Console.WriteLine($"❌ [ERROR] Base64 decode failed: {ex.Message}");
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"❌ [ERROR] Decryption failed: {ex.Message}");
        }

        Console.WriteLine("---- ENCRYPTION KAFKA REDIS TEST END ----");
    }
}