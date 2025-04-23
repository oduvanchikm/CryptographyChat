using System.Security.Cryptography;
using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class RD
{
    private const int BlockSize = 8;

    public static (byte[] ciphertext, byte[] deltas) EncryptRD(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        Console.WriteLine("Start RD Encryption");
        byte[] result = new byte[data.Length];
        byte[] deltas = new byte[data.Length];
        byte[] previousCipher = IV;

        using (var rng = RandomNumberGenerator.Create())
        {
            for (int i = 0; i < data.Length; i += BlockSize)
            {
                byte[] delta = new byte[BlockSize];
                rng.GetBytes(delta);
                Array.Copy(delta, 0, deltas, i, BlockSize);

                byte[] block = new byte[BlockSize];
                Array.Copy(data, i, block, 0, BlockSize);

                byte[] xored = BitManipulation.Xor(block, BitManipulation.Xor(delta, previousCipher));
                byte[] encrypted = encryptor.Encrypt(xored);

                Array.Copy(encrypted, 0, result, i, BlockSize);
                previousCipher = encrypted;
            }
        }

        return (result, deltas);
    }

    public static byte[] DecryptRD(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV, byte[] deltas)
    {
        Console.WriteLine("Start RD Decryption");
        byte[] result = new byte[data.Length];
        byte[] previousCipher = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] delta = new byte[BlockSize];
            Array.Copy(deltas, i, delta, 0, BlockSize);

            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);

            byte[] decrypted = encryptor.Decrypt(block);
            
            byte[] xored = BitManipulation.Xor(decrypted, BitManipulation.Xor(delta, previousCipher));

            Array.Copy(xored, 0, result, i, BlockSize);
            previousCipher = block; 
        }

        return result;
    }
}