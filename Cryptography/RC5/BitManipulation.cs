namespace Cryptography.RC5;

public class BitManipulation
{
    private const int w = 32;          // размер слова в битах (4 байта)
    private const int r = 12;          // количество раундов
    private const int b = 16;          // длина ключа в байтах
    private const int c = 4;           // количество слов в ключе (b/(w/8))
    private const int t = 26;          // размер таблицы S (2*(r+1))
    private const int blockSize = 8;
    
    // public static byte[] RotateLeft(byte[] value, int shift)
    // {
    //     shift %= w;
    //     uint temp = BitConverter.ToUInt32(value, 0);
    //     temp = (temp << shift) | (temp >> (w - shift));
    //     return BitConverter.GetBytes(temp);
    // }
    
    public static byte[] AddByteArrays(byte[] a, byte[] b)
    {
        byte[] result = new byte[4];
        int carry = 0;
        
        for (int i = 3; i >= 0; i--)
        {
            int sum = a[i] + b[i] + carry;
            result[i] = (byte)(sum & 0xFF);
            carry = sum >> 8;
        }
        
        return result;
    }
    
    public static byte[] RotateLeft(byte[] value, int shift)
    {
        uint temp = BitConverter.ToUInt32(value, 0);
        temp = (temp << shift) | (temp >> (w - shift));
        return BitConverter.GetBytes(temp);
    }

    public static byte[] RotateRight(byte[] value, int shift)
    {
        uint temp = BitConverter.ToUInt32(value, 0);
        temp = (temp >> shift) | (temp << (w - shift));
        return BitConverter.GetBytes(temp);
    }

    public static byte[] AddBytes(byte[] a, byte[] b)
    {
        byte[] result = new byte[4];
        int carry = 0;
        
        for (int i = 3; i >= 0; i--)
        {
            int sum = a[i] + b[i] + carry;
            result[i] = (byte)(sum & 0xFF);
            carry = sum >> 8;
        }
        
        return result;
    }

    public static byte[] SubtractBytes(byte[] a, byte[] b)
    {
        byte[] result = new byte[4];
        int borrow = 0;
        
        for (int i = 3; i >= 0; i--)
        {
            int diff = a[i] - b[i] - borrow;
            if (diff < 0)
            {
                diff += 256;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }
            result[i] = (byte)diff;
        }
        
        return result;
    }

    public static byte[] XorBytes(byte[] a, byte[] b)
    {
        byte[] result = new byte[4];
        for (int i = 0; i < 4; i++)
            result[i] = (byte)(a[i] ^ b[i]);
        return result;
    }

    public static byte[] GetWord(byte[] buffer, int offset)
    {
        byte[] word = new byte[4];
        Array.Copy(buffer, offset, word, 0, 4);
        return word;
    }

    public static void ValidateBlock(byte[] input, byte[] output)
    {
        if (input.Length != blockSize || output.Length != blockSize)
            throw new ArgumentException($"Block size must be {blockSize} bytes");
    }
}