using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class OFB
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
    
    public static byte[] EncryptOFB(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        BlockSize = GetBlockSize(encryptor);
        byte[] result = new byte[data.Length];
        byte[] previousBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            
            previousBlock = encryptor.Encrypt(previousBlock);
            byte[] xoredBlock = BitManipulation.Xor(block, previousBlock);
            
            Array.Copy(xoredBlock, 0, result, i, BlockSize);
            previousBlock = block;
        }
        
        return data;
    }
    
    public static byte[] DecryptOFB(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV) => EncryptOFB(data, encryptor, IV);
}