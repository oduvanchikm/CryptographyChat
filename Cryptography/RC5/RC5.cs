using Cryptography.Interfaces;

namespace Cryptography.RC5;

public class RC5 : ISymmetricEncryptionAlgorithm
{
    private IKeyExpansion _keyExpansion;
    private readonly byte[][] _S;
    private const int w = 32;
    private const int r = 12;
    private readonly int t;
    private const int BlockSize = 8;

    public RC5(byte[] key)
    {
        _keyExpansion = new KeyExpansion();
        _S = _keyExpansion.GenerateRoundKeys(key);
        t = 2 * (r + 1);
    }
    
    public byte[] Encrypt(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return Array.Empty<byte>();

        int paddedLength = data.Length % BlockSize == 0 
            ? data.Length 
            : data.Length + (BlockSize - data.Length % BlockSize);
        byte[] paddedData = new byte[paddedLength];
        Array.Copy(data, paddedData, data.Length);

        byte[] encrypted = new byte[paddedLength];

        for (int j = 0; j < paddedLength; j += BlockSize)
        {
            byte[] block = new byte[BlockSize];
            Array.Copy(paddedData, j, block, 0, BlockSize);

            byte[] A = new byte[4];
            byte[] B = new byte[4];
            Array.Copy(block, 0, A, 0, 4);
            Array.Copy(block, 4, B, 0, 4);

            A = BitManipulation.AddBytes(A, _S[0]);
            B = BitManipulation.AddBytes(B, _S[1]);

            for (int i = 1; i <= r; ++i)
            {
                A = BitManipulation.AddBytes(
                    BitManipulation.LeftRotateBytes(
                        BitManipulation.Xor(A, B), 
                        BitConverter.ToInt32(B, 0), 
                        w),
                    _S[2 * i]
                );

                B = BitManipulation.AddBytes(
                    BitManipulation.LeftRotateBytes(
                        BitManipulation.Xor(B, A), 
                        BitConverter.ToInt32(A, 0), 
                        w),
                    _S[2 * i + 1]
                );
            }

            Array.Copy(A, 0, encrypted, j, 4);
            Array.Copy(B, 0, encrypted, j + 4, 4);
        }

        return encrypted;
    }

    public byte[] Decrypt(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return Array.Empty<byte>();
        if (data.Length % BlockSize != 0) 
            throw new ArgumentException("Data length must be a multiple of block size", nameof(data));

        byte[] decrypted = new byte[data.Length];

        for (int j = 0; j < data.Length; j += BlockSize)
        {
            byte[] block = new byte[BlockSize];
            Array.Copy(data, j, block, 0, BlockSize);

            byte[] A = new byte[4];
            byte[] B = new byte[4];
            Array.Copy(block, 0, A, 0, 4);
            Array.Copy(block, 4, B, 0, 4);

            for (int i = r; i >= 1; --i)
            {
                B = BitManipulation.Xor(
                    BitManipulation.RightRotateBytes(
                        BitManipulation.SubBytes(B, _S[2 * i + 1]), 
                        BitConverter.ToInt32(A, 0), 
                        w),
                    A
                );

                A = BitManipulation.Xor(
                    BitManipulation.RightRotateBytes(
                        BitManipulation.SubBytes(A, _S[2 * i]), 
                        BitConverter.ToInt32(B, 0), 
                        w),
                    B
                );
            }

            B = BitManipulation.SubBytes(B, _S[1]);
            A = BitManipulation.SubBytes(A, _S[0]);

            Array.Copy(A, 0, decrypted, j, 4);
            Array.Copy(B, 0, decrypted, j + 4, 4);
        }

        return decrypted;
    }
}