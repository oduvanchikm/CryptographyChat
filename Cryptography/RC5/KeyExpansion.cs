using Cryptography.Interfaces;

namespace Cryptography.RC5;

public class KeyExpansion : IKeyExpansion
{
    private readonly byte[] Pw = { 0xB7, 0xE1, 0x51, 0x63 };
    private readonly byte[] Qw = { 0x9E, 0x37, 0x79, 0xB9 };
    
    private const int w = 32;          // размер слова в битах (4 байта)
    private const int rounds = 12;          // количество раундов
    private const int b = 16;          // длина ключа в байтах
    private const int c = 4;           // количество слов в ключе (b/(w/8))
    private const int t = 26;          // размер таблицы S (2*(r+1))
    
    public byte[][] GenerateRoundKeys(byte[] key)
    {
        byte[][] S = new byte[t][]; // Расширенная таблица ключей
        byte[][] L = new byte[c][]; // Массив слов ключа
        
        // 1. Инициализация массива L из ключа K
        for (int i = 0; i < c; i++)
        {
            L[i] = new byte[4];
            Array.Copy(key, i * 4, L[i], 0, 4);
        }
        
        // 2. Инициализация таблицы S
        S[0] = Pw;
        for (int i = 1; i < rounds; ++i)
        {
            S[i] = BitManipulation.AddByteArrays(S[i - 1], Qw);
        }
        
        // Смешивание ключа с таблицей S
        byte[] A = new byte[4];
        byte[] B = new byte[4];
        
        for (int k = 0, i = 0, j = 0; k < 3 * t; k++)
        {
            byte[] temp = BitManipulation.AddByteArrays(BitManipulation.AddByteArrays(S[i], A), B);
            S[i] = BitManipulation.RotateLeft(temp, 3);
            A = S[i];
            
            temp = BitManipulation.AddByteArrays(BitManipulation.AddByteArrays(L[j], A), B);
            L[j] = BitManipulation.RotateLeft(temp, BitConverter.ToInt32(BitManipulation.AddByteArrays(A, B), 0));
            B = L[j];
            
            i = (i + 1) % t;
            j = (j + 1) % c;
        }

        return S;
    }
}