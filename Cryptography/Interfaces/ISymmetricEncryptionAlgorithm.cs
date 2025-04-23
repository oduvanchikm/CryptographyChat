namespace Cryptography.Interfaces;

public interface ISymmetricEncryptionAlgorithm
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
    void SetKey(byte[] key);
}