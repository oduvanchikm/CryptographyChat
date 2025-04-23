using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class ECB
{
    private const int BlockSize = 8;
    public static byte[] EncryptECB(byte[] data, ISymmetricEncryptionAlgorithm encryptor)
    {
        Console.WriteLine("Start ECB Encryptor");
        byte[] result = new byte[data.Length];

        Parallel.For(
            0, data.Length / BlockSize, i =>
            {
                int offset = i * BlockSize;
                byte[] tempBlock = new byte[BlockSize];
                Array.Copy(data, offset, tempBlock, 0, BlockSize);
                byte[] encryptedBlock = encryptor.Encrypt(tempBlock);
                Array.Copy(encryptedBlock, 0, result, offset, BlockSize);
            }
        );

        return result;
    }

    public static byte[] DecryptECB(byte[] data, ISymmetricEncryptionAlgorithm encryptor)
    {
        Console.WriteLine("Start ECB Decryptor");
        byte[] result = new byte[data.Length];

        Parallel.For(0, data.Length / BlockSize, i =>
        {
            int offset = i * BlockSize;
            byte[] block = new byte[BlockSize];
            Array.Copy(data, offset, block, 0, BlockSize);
            byte[] decryptedBlock = encryptor.Decrypt(block);
            Array.Copy(decryptedBlock, 0, result, offset, BlockSize);
        });

        return result;
    }
}