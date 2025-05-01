using Cryptography.Interfaces;

namespace Cryptography.MARS;

public class KeyExpansion : IKeyExpansion
{
        public byte[][] GenerateRoundKeys(byte[] key)
    {
        int n = key.Length / 4;
        Console.WriteLine(n);
        uint[] T = new uint[15];
        uint[] K = new uint[40];

        // Step 1: Initialize T[0...n-1] = key[...]
        for (int i = 0; i < n; ++i)
        {
            T[i] = BitConverter.ToUInt32(key, i * 4);
        }

        T[n] = (uint)n;
        for (int i = n + 1; i < 15; i++)
        {
            T[i] = 0;
        }

        for (int j = 0; j < 4; ++j)
        {
            // Step 2: Linear Key-Word Expansion
            for (int i = 0; i < 15; i++)
            {
                T[i] ^= BitManipulation.LeftRotate(T[(i + 8) % 15] ^ T[(i + 13) % 15], 3) ^ (uint)(4 * i + j);
            }

            // Step 3: S-box Based Stirring
            for (int round = 0; round < 4; round++)
            {
                for (int i = 0; i < 15; i++)
                {
                    uint sIndex = T[(i + 14) % 15] & 0x1FF;
                    T[i] = BitManipulation.LeftRotate(T[i] + Data.S[sIndex], 9);
                }
            }

            // Step 4: Store next 10 key words into K
            for (int i = 0; i < 10; i++)
            {
                K[10 * j + i] = T[(4 * i) % 15];
            }
        }

        for (int i = 5; i <= 35; i += 2)
        {
            uint j = K[i] & 0x3;
            uint w = K[i] | 0x3;
            uint r = K[i - 1] & 0x1f;

            uint p = BitManipulation.LeftRotate(Data.B[j], (int)r);
            uint M = BitManipulation.ComputeMask(w);

            K[i] = w ^ (p & M);
        }

        byte[][] roundKeys = new byte[40][];
        for (int i = 0; i < 40; i++)
        {
            roundKeys[i] = BitConverter.GetBytes(K[i]);
        }

        Console.WriteLine("Generated Round Keys:");
        for (int i = 0; i < roundKeys.Length; i++)
        {
            string hex = BitConverter.ToString(roundKeys[i]).Replace("-", "");
            Console.WriteLine($"K[{i:D2}] = 0x{hex}");
        }

        return roundKeys;
    }
}