using System.Security.Cryptography;
using Cryptography;
using Cryptography.Interfaces;
using Cryptography.MARS;
using Cryptography.RC5;
using SecureChat.Server.Interfaces;
using StackExchange.Redis;
using CipherMode = Cryptography.CipherMode.CipherMode.Mode;
using PaddingMode = Cryptography.PaddingMode.PaddingMode.Mode;

namespace SecureChat.Server.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IDatabase _redisDb;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConnectionMultiplexer redis,
        ILogger<EncryptionService> logger)
    {
        _redis = redis;
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<byte[]> EncryptAsync(byte[] data, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, 
        int chatId)
    {
        try
        {
            _logger.LogInformation($"[EncryptAsync] EncryptAsync start");
            var context = CreateCryptoContext(algorithm, paddingMode, cipherMode, chatId);
            _logger.LogInformation($"[EncryptAsync] after create context");
            var encryptedBytes = await context.EncryptAsync(data);
            _logger.LogInformation($"[EncryptAsync] after encrypt method");
            return encryptedBytes;
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Encryption failed: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] data, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, 
        int chatId)
    {
        try
        {
            var context = CreateCryptoContext(algorithm, paddingMode, cipherMode, chatId);
            return await context.DecryptAsync(data);
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Decryption failed: {ex.Message}", ex);
        }
    }

    private ContextCrypto CreateCryptoContext(string algorithm, PaddingMode paddingMode, CipherMode cipherMode,
        int chatId)
    {
        var publicKey = GetPublicKeyAsync(chatId).Result;
        
        Console.WriteLine($"[ CreateCryptoContext ] {publicKey.Length}");
        Console.WriteLine($"[ CreateCryptoContext ] 2 {publicKey.Take(16).ToArray().Length}");
        
        byte[] key = algorithm.ToUpper() switch
        {
            "RC5" => publicKey.Take(16).ToArray(),
            "MARS" => publicKey,
            _ => throw new NotSupportedException($"Algorithm {algorithm} is not supported")
        };


        ISymmetricEncryptionAlgorithm encryptor = algorithm.ToUpper() switch
        {
            "RC5" => new RC5(key),
            "MARS" => new MARS(key),
            _ => throw new NotSupportedException($"Algorithm {algorithm} is not supported")
        };

        return new ContextCrypto(key, encryptor, cipherMode, paddingMode);
    }

    private async Task<byte[]> GetPublicKeyAsync(int chatId)
    {
        var redisKey = $"chat:{chatId}:publicKey";
        var publicKey = await _redisDb.StringGetAsync(redisKey);

        if (!IsBase64String(publicKey))
        {
            throw new CryptographicException($"Public key for users in chat {chatId} is not a valid Base64 string.");
        }

        return Convert.FromBase64String(publicKey);
    }

    private bool IsBase64String(string s)
    {
        Span<byte> buffer = new Span<byte>(new byte[s.Length]);
        return Convert.TryFromBase64String(s, buffer, out _);
    }
}


