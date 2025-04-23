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
            "Zeros" => PaddingMode.Mode.Zeros,
            "ANSIX923" => PaddingMode.Mode.ANSI_X_923,
            "PKCS7" => PaddingMode.Mode.PKCS7,
            "ISO10126" => PaddingMode.Mode.ISO_10126,
            _ => throw new ArgumentException($"Unknown padding mode: {paddingModeString}")
        };
    }

    public static byte[] ApplyPadding(byte[] data, int blockSize, Mode paddingMode)
    {
        int paddingLength = blockSize - (data.Length % blockSize);

        if (paddingLength == blockSize) return data;

        byte[] paddedData = new byte[data.Length + paddingLength];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);

        switch (paddingMode)
        {
            case Mode.ANSI_X_923:
                paddedData = ApplyANSI_X_923Padding(paddedData, data.Length, paddingLength);
                break;

            case Mode.PKCS7:
                paddedData = ApplyPKCS7Padding(paddedData, data.Length, paddingLength);
                break;

            case Mode.ISO_10126:
                paddedData = ApplyISO_10126Padding(paddedData, data.Length, paddingLength);
                break;
        }

        return paddedData;
    }

    private static byte[] ApplyANSI_X_923Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        for (int i = originalSize; i < paddingLength - 1; ++i)
        {
            paddedData[i] = 0x00;
        }

        paddedData[^1] = (byte)paddingLength;
        return paddedData;
    }

    private static byte[] ApplyPKCS7Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        for (int i = originalSize; i < paddingLength; ++i)
        {
            paddedData[i] = (byte)paddingLength;
        }

        return paddedData;
    }

    private static byte[] ApplyISO_10126Padding(byte[] paddedData, int originalSize, int paddingLength)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[paddingLength - 1];
            rng.GetBytes(randomBytes);
            Buffer.BlockCopy(randomBytes, 0, paddedData, originalSize, randomBytes.Length);
        }

        paddedData[^1] = (byte)paddingLength;
        return paddedData;
    }

    public static byte[] DeletePadding(byte[] data, Mode paddingMode)
    {
        int paddingLength = 0;

        switch (paddingMode)
        {
            case Mode.Zeros:
                paddingLength = GetZerosPaddingLength(data);
                break;

            case Mode.ANSI_X_923:
                paddingLength = GetANSI_X_923PaddingLength(data);
                break;

            case Mode.PKCS7:
                paddingLength = GetPKCS7PaddingLength(data);
                break;

            case Mode.ISO_10126:
                paddingLength = GetISO_10126PaddingLength(data);
                break;
        }

        byte[] result = new byte[data.Length - paddingLength];
        Buffer.BlockCopy(data, 0, result, 0, result.Length);

        return result;
    }

    private static int GetZerosPaddingLength(byte[] data)
    {
        int i = data.Length - 1;
        while (i >= 0 && data[i] == 0)
        {
            i--;
        }

        return data.Length - i - 1;
    }

    private static int GetANSI_X_923PaddingLength(byte[] data)
    {
        int paddingLength = data[^1];

        for (int i = data.Length - paddingLength; i < data.Length - 1; i++)
        {
            if (data[i] != 0)
            {
                throw new CryptographicException("Invalid ANSI X.923 padding");
            }
        }

        return paddingLength;
    }

    private static int GetPKCS7PaddingLength(byte[] data)
    {
        int paddingLength = data[^1];

        for (int i = data.Length - paddingLength; i < data.Length; i++)
        {
            if (data[i] != paddingLength)
            {
                throw new CryptographicException("Invalid PKCS7 padding");
            }
        }

        return paddingLength;
    }

    private static int GetISO_10126PaddingLength(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty");
    
        int paddingLength = data[^1];
    
        // Проверяем что значение паддинга в допустимом диапазоне
        if (paddingLength <= 0 || paddingLength > 16) // Максимальный размер паддинга для AES - 16 байт
            throw new CryptographicException($"Invalid ISO 10126 padding length {paddingLength}");
    
        // Проверяем что в массиве достаточно места для паддинга
        if (paddingLength > data.Length)
            throw new CryptographicException("Padding length exceeds data length");
    
        return paddingLength;
    }
}