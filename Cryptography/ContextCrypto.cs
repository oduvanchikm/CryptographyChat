using System.Security.Cryptography;
using Cryptography.CipherMode;
using Cryptography.Interfaces;

namespace Cryptography;

public class ContextCrypto
{
    private readonly ISymmetricEncryptionAlgorithm _encryptor;
    private readonly CipherMode.CipherMode.Mode _cipherMode;
    private readonly PaddingMode.PaddingMode.Mode _paddingMode;
    private const int BlockSize = 8;
    private byte[] _IV;
    private byte[] _delta;
    private bool _isIVGenerated = false;

    public ContextCrypto(byte[] key, ISymmetricEncryptionAlgorithm encryptor,
        CipherMode.CipherMode.Mode cipherMode,
        PaddingMode.PaddingMode.Mode paddingMode,
        byte[] iv = null)
    {
        _cipherMode = cipherMode;
        _paddingMode = paddingMode;
        _encryptor = encryptor;
        _IV = BitManipulation.Generate(8);
        
        _IV = iv ?? GenerateIV();
        if (iv == null) _isIVGenerated = true;
    }
    
    private byte[] GenerateIV()
    {
        _isIVGenerated = true;
        return BitManipulation.Generate(8);
    }
    
    public byte[] GetIV() => _IV;

    public async Task<byte[]> EncryptAsync(byte[] data)
    {
        data = PaddingMode.PaddingMode.ApplyPadding(data, BlockSize, _paddingMode);

        Console.WriteLine("After padding");

        var tasks = new List<Task<byte[]>>();

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            var block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, BlockSize);
            tasks.Add(Task.Run(() => EncryptBlockAsync(block)));
        }

        var encryptedBlocks = await Task.WhenAll(tasks);

        var encryptedData = encryptedBlocks.SelectMany(b => b).ToArray();

        return encryptedData;
    }

    private byte[] EncryptBlockAsync(byte[] block)
    {
        switch (_cipherMode)
        {
            case CipherMode.CipherMode.Mode.ECB:
                return ECB.EncryptECB(block, _encryptor);
            case CipherMode.CipherMode.Mode.CBC:
                return CBC.EncryptCBC(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.CFB:
                return CFB.EncryptCFB(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.OFB:
                return OFB.EncryptOFB(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.PCBC:
                return PCBC.EncryptPCBC(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.CTR:
                return CTR.EncryptCTR(block, _encryptor, GetIV());
            default:
                return _encryptor.Encrypt(block);
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] data)
    {
        var tasks = new List<Task<byte[]>>();

        if (data == null || data.Length == 0)
        {
            Console.WriteLine("[ CONTEXT CRYPTO DECRYPT ] data is null");
        }

        Console.WriteLine($"[ CONTEXT CRYPTO DECRYPT ] {data.Length}");

        if (data.Length % BlockSize != 0)
        {
            throw new CryptographicException("Encrypted data length is not a multiple of block size.");
        }

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            int remaining = Math.Min(BlockSize, data.Length - i);
            var block = new byte[BlockSize];
            Array.Copy(data, i, block, 0, remaining);
            tasks.Add(Task.Run(() => DecryptBlockAsync(block)));
        }

        var decryptedBlocks = await Task.WhenAll(tasks);

        var decryptedData = decryptedBlocks.SelectMany(b => b).ToArray();

        return PaddingMode.PaddingMode.DeletePadding(decryptedData, _paddingMode);
    }

    private byte[] DecryptBlockAsync(byte[] block)
    {
        switch (_cipherMode)
        {
            case CipherMode.CipherMode.Mode.ECB:
                return ECB.DecryptECB(block, _encryptor);
            case CipherMode.CipherMode.Mode.CBC:
                return CBC.DecryptCBC(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.CFB:
                return CFB.DecryptCFB(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.OFB:
                return OFB.DecryptOFB(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.PCBC:
                return PCBC.DecryptPCBC(block, _encryptor, GetIV());
            case CipherMode.CipherMode.Mode.CTR:
                return CTR.DecryptCTR(block, _encryptor, GetIV());
            default:
                return _encryptor.Decrypt(block);
        }
    }
}