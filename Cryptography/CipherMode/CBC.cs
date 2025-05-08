using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class CBC
{
    private static int BlockSize;
    private static int GetBlockSize(ISymmetricEncryptionAlgorithm encryptor)
    {
        return encryptor switch
        {
            RC5.RC5 _ => 8,
            MARS.MARS _ => 16
        };
    }

    public static byte[] EncryptCBC(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        BlockSize = GetBlockSize(encryptor);
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