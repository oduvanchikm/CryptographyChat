using Cryptography.Interfaces;

namespace Cryptography.MARS;

public class MARS : ISymmetricEncryptionAlgorithm
{
    private byte[][] roundKeys;
    private IKeyExpansion _keyExpansion;

    private void SetKey(byte[] key)
    {
        _keyExpansion = new KeyExpansion();
    }

    public MARS(byte[] key)
    {
        SetKey(key);
        roundKeys = _keyExpansion.GenerateRoundKeys(key);
    }

    public byte[] Encrypt(byte[] data)
    {
        uint[] K = new uint[40];
        for (int i = 0; i < 40; i++)
        {
            K[i] = BitConverter.ToUInt32(roundKeys[i], 0);
        }

        uint A = BitConverter.ToUInt32(data, 0) + K[0];
        uint B = BitConverter.ToUInt32(data, 4) + K[1];
        uint C = BitConverter.ToUInt32(data, 8) + K[2];
        uint D = BitConverter.ToUInt32(data, 12) + K[3];

        // Forward Mixing
        for (int i = 0; i < 8; i++)
        {
            B = (B ^ Data.S0[A & 0xff]) + Data.S1[BitManipulation.RightRotate(A, 8) & 0xff];
            C += Data.S0[BitManipulation.RightRotate(A, 16) & 0xff];
            D ^= Data.S1[BitManipulation.RightRotate(A, 24) & 0xff];

            A = BitManipulation.RightRotate(A, 24);

            if (i == 1 || i == 5) A += B;
            else if (i == 0 || i == 4) A += D;

            uint temp = A;
            A = B;
            B = C;
            C = D;
            D = temp;
        }

        // Cryptographic core
        for (int i = 0; i < 16; i++)
        {
            uint firstKey = BitConverter.ToUInt32(roundKeys[2 * i + 5], 0);
            uint secondKey = BitConverter.ToUInt32(roundKeys[2 * i + 4], 0);
            
            var (L, M, R) = EFunction(A, firstKey, secondKey);

            C += M;
            if (i < 8)
            {
                D += R;
                B += L;
            }
            else
            {
                D += L;
                B += R;
            }

            uint temp = A;
            A = B;
            B = C;
            C = D;
            D = BitManipulation.LeftRotate(temp, 13);
        }

        // Backward Mixing
        for (int i = 0; i < 8; i++)
        {
            if (i == 3 || i == 7) A -= B;
            if (i == 2 || i == 6) A -= D;

            B ^= Data.S1[A & 0xff];
            C -= Data.S0[BitManipulation.LeftRotate(A, 8) & 0xff];
            D = (D - Data.S1[BitManipulation.LeftRotate(A, 16) & 0xff]) ^
                Data.S0[BitManipulation.LeftRotate(A, 24) & 0xff];

            uint temp = A;
            A = B;
            B = C;
            C = D;
            D = BitManipulation.LeftRotate(temp, 24);
        }

        A -= K[36];
        B -= K[37];
        C -= K[38];
        D -= K[39];

        byte[] result = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(A), 0, result, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(B), 0, result, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(C), 0, result, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(D), 0, result, 12, 4);

        return result;
    }

    private (uint L, uint M, uint R) EFunction(uint A, uint firstKey, uint secondKey)
    {
        uint R = BitManipulation.LeftRotate(
            BitManipulation.LeftRotate(A, 13) * firstKey,
            10);

        uint M = BitManipulation.LeftRotate(
            A + secondKey,
            (int)((R >> 5) & 0x1F));

        uint L = BitManipulation.LeftRotate(
            Data.S[M & 0x1FF] ^ (R >> 5) ^ R,
            (int)(R & 0x1F));

        return (L, M, R);
    }

    public byte[] Decrypt(byte[] data)
    {
        if (data.Length != 16) throw new ArgumentException("MARS decrypts 128-bit (16 byte) blocks.");

        uint[] K = new uint[40];
        for (int i = 0; i < 40; i++)
        {
            K[i] = BitConverter.ToUInt32(roundKeys[i], 0);
        }

        uint A = BitConverter.ToUInt32(data, 0) + K[36];
        uint B = BitConverter.ToUInt32(data, 4) + K[37];
        uint C = BitConverter.ToUInt32(data, 8) + K[38];
        uint D = BitConverter.ToUInt32(data, 12) + K[39];

        // Inverse Backward Mixing
        for (int i = 7; i >= 0; i--)
        {
            uint tmp = BitManipulation.RightRotate(D, 24);
            D = C;
            C = B;
            B = A;
            A = tmp;

            D = (D ^ Data.S0[BitManipulation.LeftRotate(A, 24) & 0xff]) + Data.S1[BitManipulation.LeftRotate(A, 16) & 0xff];
            C += Data.S0[BitManipulation.LeftRotate(A, 8) & 0xff];
            B ^= Data.S1[A & 0xff];

            if (i == 3 || i == 7) A += B;
            else if (i == 2 || i == 6) A += D;
        }

        // Inverse Core Rounds
        for (int i = 15; i >= 0; i--)
        {
            uint tmp = BitManipulation.RightRotate(D, 13);
            D = C;
            C = B;
            B = A;
            A = tmp;
            
            uint firstKey = BitConverter.ToUInt32(roundKeys[2 * i + 5], 0);
            uint secondKey = BitConverter.ToUInt32(roundKeys[2 * i + 4], 0);
            var (L, M, R) = EFunction(A, firstKey, secondKey);
            
            if (i < 8) {
                B -= L;
                D -= R;
            } else {
                B -= R;
                D -= L;
            }
            C -= M;
        }

        // Inverse Forward Mixing
        for (int i = 7; i >= 0; i--)
        {
            uint tmp = D;
            D = C;
            C = B;
            B = A;
            A = tmp;

            if (i == 1 || i == 5) A -= B;
            if (i == 0 || i == 4) A -= D;

            A = BitManipulation.LeftRotate(A, 24);
            
            D ^= Data.S1[BitManipulation.RightRotate(A, 24) & 0xff];
            C -= Data.S0[BitManipulation.RightRotate(A, 16) & 0xff];
            B = (B - Data.S1[BitManipulation.RightRotate(A, 8) & 0xff]) ^ Data.S0[A & 0xff];
        }

        A -= K[0];
        B -= K[1];
        C -= K[2];
        D -= K[3];

        byte[] result = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(A), 0, result, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(B), 0, result, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(C), 0, result, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(D), 0, result, 12, 4);

        return result;
    }
}