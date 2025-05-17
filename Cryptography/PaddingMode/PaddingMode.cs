using System.Security.Cryptography;

namespace Cryptography.PaddingMode;

public class PaddingMode
{
    public enum Mode
    {
        Zeros, // дополняет данные нулевыми байтами 
        ANSI_X_923, // дополняет данные нулевыми байтами, а последний байт указывает количество добавленных байтов (00 00 03)
        PKCS7, // дополняет данные байтами, каждый из которых равен количеству добавленных байтов (03 03 03)
        ISO_10126 // дополняет данные случайными байтами, а последний байт указывает количество добавленных байтов (AF DF 03)
    }

    public static Mode ToPaddingMode(string paddingModeString)
    {
        return paddingModeString switch
        {
            "Zeros" => Mode.Zeros,
            "ANSIX923" => Mode.ANSI_X_923,
            "PKCS7" => Mode.PKCS7,
            "ISO10126" => Mode.ISO_10126,
            _ => throw new ArgumentException($"Unknown padding mode: {paddingModeString}")
        };
    }

    public static byte[] ApplyPadding(byte[] data, int blockSize, Mode paddingMode)
    {
        int paddingLength = blockSize - (data.Length % blockSize);

        if (paddingLength == blockSize && paddingMode != Mode.Zeros)
        {
            paddingLength = blockSize;
        }
        else if (paddingLength == blockSize && paddingMode == Mode.Zeros)
        {
            return data;
        }


        byte[] paddedData = new byte[data.Length + paddingLength];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);

        switch (paddingMode)
        {
            case Mode.ANSI_X_923:
                ApplyANSI_X_923Padding(paddedData, data.Length, paddingLength);
                break;

            case Mode.PKCS7:
                ApplyPKCS7Padding(paddedData, data.Length, paddingLength);
                break;

            case Mode.ISO_10126:
                ApplyISO_10126Padding(paddedData, data.Length, paddingLength);
                break;
        }

        return paddedData;
    }

    private static void ApplyANSI_X_923Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        Array.Clear(paddedData, originalSize, paddingLength - 1);
        paddedData[^1] = (byte)paddingLength;
    }

    private static void ApplyPKCS7Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        for (int i = originalSize; i < paddedData.Length; i++)
        {
            paddedData[i] = (byte)paddingLength;
        }
    }

    private static void ApplyISO_10126Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[paddingLength - 1];
            rng.GetBytes(randomBytes);
            Buffer.BlockCopy(randomBytes, 0, paddedData, originalSize, randomBytes.Length);
        }

        paddedData[^1] = (byte)paddingLength;
    }

    public static byte[] DeletePadding(byte[] data, Mode paddingMode)
    {
        if (data == null || data.Length == 0)
            return data;

        try
        {
            int paddingLength = GetPaddingLength(data, paddingMode);

            if (paddingLength <= 0 || paddingLength > data.Length)
                return data;

            byte[] result = new byte[data.Length - paddingLength];
            Buffer.BlockCopy(data, 0, result, 0, result.Length);
            return result;
        }
        catch (CryptographicException)
        {
            return data;
        }
    }

    private static int GetPaddingLength(byte[] data, Mode paddingMode)
    {
        if (data == null || data.Length == 0)
            return 0;

        return paddingMode switch
        {
            Mode.Zeros => GetZerosPaddingLength(data),
            Mode.ANSI_X_923 => GetAnsiX923PaddingLength(data),
            Mode.PKCS7 => GetPkcs7PaddingLength(data),
            Mode.ISO_10126 => GetIso10126PaddingLength(data),
            _ => 0
        };
    }

    private static int GetZerosPaddingLength(byte[] data)
    {
        int i = data.Length - 1;
        while (i >= 0 && data[i] == 0)
            i--;

        return data.Length - i - 1;
    }

    private static int GetAnsiX923PaddingLength(byte[] data)
    {
        if (data.Length == 0) return 0;

        int paddingLength = data[^1];

        if (paddingLength <= 0 || paddingLength > data.Length)
            return 0;

        for (int i = data.Length - paddingLength; i < data.Length - 1; i++)
        {
            if (data[i] != 0)
                throw new CryptographicException("Invalid ANSI X.923 padding");
        }

        return paddingLength;
    }

    private static int GetPkcs7PaddingLength(byte[] data)
    {
        if (data.Length == 0) return 0;

        int paddingLength = data[^1];

        if (paddingLength <= 0 || paddingLength > data.Length)
            return 0;

        for (int i = data.Length - paddingLength; i < data.Length; i++)
        {
            if (data[i] != paddingLength)
                throw new CryptographicException("Invalid PKCS7 padding");
        }

        return paddingLength;
    }

    private static int GetIso10126PaddingLength(byte[] data)
    {
        if (data.Length == 0) return 0;

        int paddingLength = data[^1];

        if (paddingLength <= 0 || paddingLength > data.Length)
            return 0;

        return paddingLength;
    }
}