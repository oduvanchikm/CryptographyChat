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

public class EncryptionService(
    IConnectionMultiplexer redis,
    ILogger<EncryptionService> logger)
    : IEncryptionService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task<byte[]> EncryptAsync(byte[] data, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, 
        int chatId, int senderId)
    {
        try
        {
            logger.LogInformation($"[EncryptAsync] EncryptAsync start");
            
            var context = await CreateCryptoContext(algorithm, paddingMode, cipherMode, chatId, senderId);
            
            logger.LogInformation($"[EncryptAsync] after create context");
            
            var encryptedBytes = await context.EncryptAsync(data);
            var iv = context.GetIV();
            
            logger.LogInformation($"[EncryptAsync] after encrypt method");
            
            var result = new byte[iv.Length + encryptedBytes.Length];
            
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Encryption failed: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] data, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, 
        int chatId, int senderId)
    {
        try
        {
            var iv = new byte[8];
            Buffer.BlockCopy(data, 0, iv, 0, 8);
            
            var encryptedData = new byte[data.Length - 8];
            Buffer.BlockCopy(data, 8, encryptedData, 0, encryptedData.Length);
            
            var context = await CreateCryptoContext(algorithm, paddingMode, cipherMode, chatId, senderId, iv);
            return await context.DecryptAsync(encryptedData);
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Decryption failed: {ex.Message}", ex);
        }
    }

    private async Task<ContextCrypto> CreateCryptoContext(string algorithm, PaddingMode paddingMode, CipherMode cipherMode,
        int chatId, int senderId, byte[] iv = null)
    {
        var publicKey = await GetPublicKeyAsync(chatId, senderId);
        
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

        return new ContextCrypto(key, encryptor, cipherMode, paddingMode, iv);
    }

    private async Task<byte[]> GetPublicKeyAsync(int chatId, int senderId)
    {
        var redisKey = $"chat:{chatId}:user:{senderId}:publicKey";
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


