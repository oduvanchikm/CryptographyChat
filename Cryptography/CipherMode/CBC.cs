using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class CBC
{
    private const int BlockSize = 8;
    public static byte[] EncryptCBC(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        Console.WriteLine("Start CBC Encryptor");
        byte[] result = new byte[data.Length];
        byte[] previousBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            
            byte[] xoredBlock = BitManipulation.Xor(block, previousBlock);
            byte[] encryptedBlock = encryptor.Encrypt(xoredBlock);
            
            Array.Copy(encryptedBlock, 0, result, i, BlockSize);
            previousBlock = encryptedBlock;
        }
        
        return result;
    }
    
    public static byte[] DecryptCBC(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        Console.WriteLine("Start CBC Decryptor");
        byte[] result = new byte[data.Length];
        byte[] previousBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            
            byte[] encryptedBlock = encryptor.Decrypt(block);
            
            byte[] xoredBlock = BitManipulation.Xor(encryptedBlock, previousBlock);
            
            Array.Copy(xoredBlock, 0, result, i, BlockSize);
            previousBlock = block;
        }
        
        return result;
    }
}