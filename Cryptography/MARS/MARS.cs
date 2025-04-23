using Cryptography.Interfaces;

namespace Cryptography.MARS;

public class MARS : ISymmetricEncryptionAlgorithm
{
    public MARS(byte[] key) { }
    public byte[] Encrypt(byte[] data)
    {
        return data;
    }

    public byte[] Decrypt(byte[] data)
    {
        return data;
    }

    public void SetKey(byte[] key)
    {
        
    }
}