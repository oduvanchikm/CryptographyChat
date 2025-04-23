using CipherMode = Cryptography.CipherMode.CipherMode.Mode;
using PaddingMode = Cryptography.PaddingMode.PaddingMode.Mode;

namespace SecureChat.Server.Interfaces;

public interface IEncryptionService
{
    Task<byte[]> EncryptAsync(byte[] data, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, int chatId);
    Task<byte[]> DecryptAsync(byte[] encryptedData, string algorithm, PaddingMode paddingMode, CipherMode cipherMode, 
        int chatId);
}