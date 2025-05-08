using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class CFB
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
    public static byte[] EncryptCFB(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        BlockSize = GetBlockSize(encryptor);
        byte[] result = new byte[data.Length];
        byte[] previousBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] encryptedFeedback = encryptor.Encrypt(previousBlock);
            
            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            
            byte[] decryptedBlock = BitManipulation.Xor(block, encryptedFeedback);
            
            Array.Copy(decryptedBlock, 0, result, i, BlockSize);
            previousBlock = block;
        }
        
        return result;
    }

    public static byte[] DecryptCFB(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        byte[] result = new byte[data.Length];
        byte[] previousBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] encryptedFeedback = encryptor.Encrypt(previousBlock);
            
            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            
            byte[] decryptedBlock = BitManipulation.Xor(block, encryptedFeedback);
            
            Array.Copy(decryptedBlock, 0, result, i, BlockSize);
            previousBlock = block;
        }
        
        return result;
    }
}