namespace Cryptography;

public class BitManipulation
{
    public static byte[] LeftRotateBytes(byte[] x, int shift, int w)
    {
        if (x == null || x.Length == 0)
            throw new ArgumentException("Input array must not be null or empty");

        int byteLen = x.Length;
        byte[] result = new byte[byteLen];

        shift %= w;
        if (shift == 0) return (byte[])x.Clone();

        int byteShift = shift / 8;
        int bitShift = shift % 8;

        for (int i = 0; i < byteLen; i++)
        {
            int newIndex = (i + byteShift) % byteLen;

            if (newIndex < 0 || newIndex >= byteLen)
            {
                throw new IndexOutOfRangeException($"newIndex ({newIndex}) вышел за границы массива (0-{byteLen - 1})");
            }

            byte currentByte = x[i];
            byte nextByte = x[(i + 1) % byteLen];

            if (bitShift == 0)
            {
                result[newIndex] = currentByte;
            }
            else
            {
                result[newIndex] = (byte)(
                    ((currentByte << bitShift) & 0xFF) | 
                    ((nextByte >> (8 - bitShift)) & 0xFF)
                );
            }
        }

        return result;
    }
    
    public static byte[] Xor(byte[] block1, byte[] block2)
    {
        byte[] result = new byte[block1.Length];

        for (int i = 0; i < block1.Length; i++)
        {
            result[i] = (byte)(block1[i] ^ block2[i]);
        }

        return result;
    }

    
    public static byte[] RightRotateBytes(byte[] x, int shift, int w)
    {
        int byteLen = x.Length;
        byte[] result = new byte[byteLen];

        shift %= w;
        if (shift == 0) return (byte[])x.Clone();

        int byteShift = shift / 8;
        int bitShift = shift % 8;

        for (int i = 0; i < byteLen; i++)
        {
            int newIndex = (i - byteShift + byteLen) % byteLen;
            byte currentByte = x[i];
            byte prevByte = x[(i - 1 + byteLen) % byteLen];

            result[newIndex] = (byte)(
                ((currentByte >> bitShift) & 0xFF) | 
                ((prevByte << (8 - bitShift)) & 0xFF)
            );
        }

        return result;
    }


    public static byte[] AddBytes(byte[] a, byte[] b)
    {
        int length = Math.Max(a.Length, b.Length);
        byte[] result = new byte[length];
        int carry = 0;

        for (int i = 0; i < length; i++)
        {
            int aVal = (i < a.Length) ? a[i] : 0;
            int bVal = (i < b.Length) ? b[i] : 0;

            int sum = aVal + bVal + carry;
            result[i] = (byte)(sum & 0xFF);
            carry = sum >> 8;
        }

        return result;
    }

    
    public static byte[] SubBytes(byte[] a, byte[] b)
    {
        int length = Math.Max(a.Length, b.Length);
        byte[] result = new byte[length];
        int borrow = 0;

        for (int i = 0; i < length; i++)
        {
            int aVal = (i < a.Length) ? a[i] : 0;
            int bVal = (i < b.Length) ? b[i] : 0;

            int temp = aVal - bVal - borrow;
            if (temp < 0)
            {
                temp += 256;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }
            result[i] = (byte)(temp & 0xFF);
        }

        return result;
    }

    
    public static void PrintBlocks(byte[] block)
    {
        foreach (var b in block)
        {
            Console.Write(Convert.ToString(b, 2).PadLeft(8, '0') + " ");
        }
        Console.WriteLine();
    }

}