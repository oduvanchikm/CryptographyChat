namespace Cryptography;

public class BitManipulation
{
    public static byte[] LeftRotateBytes(byte[] x, int shift, int w)
    {
        shift = (shift % w + w) % w;
        if (shift == 0) return (byte[])x.Clone();

        int totalBytes = x.Length;
        byte[] result = new byte[totalBytes];

        int byteShift = shift / 8;
        int bitShift = shift % 8;

        for (int i = 0; i < totalBytes; i++)
        {
            int currentIdx = i;
            int nextIdx = (i + 1) % totalBytes;

            int newIdx = (i + byteShift) % totalBytes;

            byte currentByte = x[currentIdx];
            byte nextByte = x[nextIdx];

            if (bitShift == 0)
            {
                result[newIdx] = currentByte;
            }
            else
            {
                ushort combined = (ushort)((currentByte << 8) | nextByte);
                ushort rotated = (ushort)((combined << bitShift) | (combined >> (16 - bitShift)));
                result[newIdx] = (byte)(rotated >> 8);
            }
        }

        return result;
    }

    public static byte[] RightRotateBytes(byte[] x, int shift, int w)
    {
        shift = ((shift % w) + w) % w;
        if (shift == 0) return (byte[])x.Clone();

        int totalBytes = x.Length;
        byte[] result = new byte[totalBytes];

        int byteShift = shift / 8;
        int bitShift = shift % 8;

        for (int i = 0; i < totalBytes; i++)
        {
            int currentIdx = i;
            int prevIdx = (i - 1 + totalBytes) % totalBytes;

            int newIdx = (i - byteShift + totalBytes) % totalBytes;

            byte currentByte = x[currentIdx];
            byte prevByte = x[prevIdx];

            if (bitShift == 0)
            {
                result[newIdx] = currentByte;
            }
            else
            {
                ushort combined = (ushort)((prevByte << 8) | currentByte);
                ushort rotated = (ushort)((combined >> bitShift) | (combined << (16 - bitShift)));
                result[newIdx] = (byte)(rotated & 0xFF);
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
}