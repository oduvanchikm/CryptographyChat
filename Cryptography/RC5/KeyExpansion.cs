using Cryptography.Interfaces;

namespace Cryptography.RC5;
public class KeyExpansion : IKeyExpansion
{
    private const int w = 32; // Длина слова в битах
    private const int r = 12; // Количество раундов
    private const int b = 16; // Длина ключа в байтах
    private const int u = w / 8; // Длина слова в байтах
    private readonly int c; // Количество слов в ключе
    private readonly int t; // Длина расширенного ключа

    private readonly byte[] Pw = { 0xB7, 0xE1, 0x51, 0x63 }; 
    private readonly byte[] Qw = { 0x9E, 0x37, 0x79, 0xB9 }; 

    public KeyExpansion()
    { 
        c = (int)Math.Ceiling((double)b / u);  
        t = 2 * (r + 1);  
    }
    
    public byte[][] GenerateRoundKeys(byte[] key)
    {
        Console.WriteLine("key: " + BitConverter.ToString(key));
        if (key.Length != b)
            throw new ArgumentException($"Key length must be {b} bytes");
        
        byte[][] K = new byte[c][];
        byte[][] S = new byte[t][];
        Console.WriteLine("1");

        for (int i = 0; i < c; i++)
        {
            K[i] = new byte[u];
            Array.Copy(key, i * u, K[i], 0, u);
        }
        Console.WriteLine("2");

        S[0] = new byte[u];
        Array.Copy(Pw, 0, S[0], 0, u);
        Console.WriteLine("3");

        for (int i = 1; i < t; i++)
        {
            S[i] = new byte[u];  
            S[i] = BitManipulation.AddBytes(S[i - 1], Qw);
        }
        Console.WriteLine("4");

        byte[] A = new byte[u];
        byte[] B = new byte[u];
        int iIndex = 0, jIndex = 0;
        int maxIter = 3 * Math.Max(t, c);
        Console.WriteLine("5");

        for (int k = 0; k < maxIter; k++)
        {
            Console.WriteLine("6");

            if (S[iIndex] == null) S[iIndex] = new byte[u];
            if (K[jIndex] == null) K[jIndex] = new byte[u];

            S[iIndex] = BitManipulation.LeftRotateBytes(
                BitManipulation.AddBytes(S[iIndex], BitManipulation.AddBytes(A, B)), 
                3, 
                w
            );
            A = S[iIndex];
            Console.WriteLine("6,5");

            K[jIndex] = BitManipulation.LeftRotateBytes(
                BitManipulation.AddBytes(K[jIndex], BitManipulation.AddBytes(A, B)), 
                BitConverter.ToInt32(A, 0) % w, w
            );
            B = K[jIndex];
            Console.WriteLine("7");

            iIndex = (iIndex + 1) % t;
            jIndex = (jIndex + 1) % c;
        }
        Console.WriteLine("8");

        return S;
    }
}