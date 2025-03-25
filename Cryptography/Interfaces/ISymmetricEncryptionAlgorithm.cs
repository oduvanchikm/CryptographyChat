namespace Cryptography.Interfaces;

public interface ISymmetricEncryptionAlgorithm
{
    byte[] Encrypt(byte[] data, byte[] key);
    byte[] Decrypt(byte[] data, byte[] key);
}