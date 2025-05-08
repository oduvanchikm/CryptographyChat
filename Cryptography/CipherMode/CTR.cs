using Cryptography.Interfaces;

namespace Cryptography.CipherMode;

public class CTR
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
    public static byte[] EncryptCTR(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV)
    {
        Console.WriteLine("Start CTR Encryption");
        BlockSize = GetBlockSize(encryptor);
        byte[] result = new byte[BlockSize];
        byte[] counterBlock = IV;

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            byte[] keyStream = encryptor.Encrypt(counterBlock);
            
            int bytesToProcess = Math.Min(BlockSize, data.Length - i);
            
            for (int j = 0; j < bytesToProcess; j++)
            {
                result[i + j] = (byte)(data[i + j] ^ keyStream[j]);
            }

            IncrementCounterBigEndian(counterBlock);
        }
        
        return data;
    }
    
    private static void IncrementCounterBigEndian(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
        {
            if (++counter[i] != 0)
                break;
        }
    }
    
    public static byte[] DecryptCTR(byte[] data, ISymmetricEncryptionAlgorithm encryptor, byte[] IV) => EncryptCTR(data, encryptor, IV);
}