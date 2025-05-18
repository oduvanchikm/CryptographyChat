using System.Security.Cryptography;
using Cryptography.CipherMode;
using Cryptography.Interfaces;

namespace Cryptography;

public class ContextCrypto
{
    private readonly ISymmetricEncryptionAlgorithm _encryptor;
    private readonly CipherMode.CipherMode.Mode _cipherMode;
    private readonly PaddingMode.PaddingMode.Mode _paddingMode;
    private readonly int BlockSize;
    private byte[] _IV;
    private byte[] _delta;
    private bool _isIVGenerated = false;

    public ContextCrypto(byte[] key, ISymmetricEncryptionAlgorithm encryptor,
        CipherMode.CipherMode.Mode cipherMode,
        PaddingMode.PaddingMode.Mode paddingMode,
        byte[] iv = null)
    {
        BlockSize = encryptor switch
        {
            RC5.RC5 _ => 8, 
            MARS.MARS _ => 16
        };
        
        _cipherMode = cipherMode;
        _paddingMode = paddingMode;
        _encryptor = encryptor;

        _IV = iv ?? GenerateIV();
        if (iv == null) _isIVGenerated = true;
    }

    private byte[] GenerateIV()
    {
        _isIVGenerated = true;
        return BitManipulation.Generate(BlockSize);
    }

    public byte[] GetIV() => _IV;

    public async Task<byte[]> EncryptAsync(byte[] data)
    {
        data = PaddingMode.PaddingMode.ApplyPadding(data, BlockSize, _paddingMode);

        var result = new byte[data.Length];
        var blocks = data.Length / BlockSize;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, blocks),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (i, ct) =>
            {
                var block = data.AsMemory(i * BlockSize, BlockSize);
                var encrypted = EncryptBlock(block.ToArray());
                encrypted.CopyTo(result.AsMemory(i * BlockSize));
            });

        return result;
    }

    private byte[] EncryptBlock(byte[] block)
    {
        switch (_cipherMode)
        {
            case CipherMode.CipherMode.Mode.RD:
                var (encrypted, delta) = RD.EncryptRD(block, _encryptor, _IV);
                _delta = delta;
                return encrypted;

            default:
                return _cipherMode switch
                {
                    CipherMode.CipherMode.Mode.ECB => ECB.EncryptECB(block, _encryptor),
                    CipherMode.CipherMode.Mode.CBC => CBC.EncryptCBC(block, _encryptor, _IV),
                    CipherMode.CipherMode.Mode.CFB => CFB.EncryptCFB(block, _encryptor, _IV),
                    CipherMode.CipherMode.Mode.OFB => OFB.EncryptOFB(block, _encryptor, _IV),
                    CipherMode.CipherMode.Mode.PCBC => PCBC.EncryptPCBC(block, _encryptor, _IV),
                    CipherMode.CipherMode.Mode.CTR => CTR.EncryptCTR(block, _encryptor, _IV),
                    CipherMode.CipherMode.Mode.NONE => _encryptor.Encrypt(block),
                    _ => throw new NotSupportedException()
                };
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        var result = new byte[data.Length];
        var blocks = data.Length / BlockSize;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, blocks),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (i, ct) =>
            {
                var block = data.AsMemory(i * BlockSize, BlockSize);
                var decrypted = DecryptBlock(block.ToArray());
                decrypted.CopyTo(result.AsMemory(i * BlockSize));
            });

        return PaddingMode.PaddingMode.DeletePadding(result, _paddingMode);
    }

    private byte[] DecryptBlock(byte[] data)
    {
        byte[] decryptedData = _cipherMode switch
        {
            CipherMode.CipherMode.Mode.ECB => ECB.DecryptECB(data, _encryptor),
            CipherMode.CipherMode.Mode.CBC => CBC.DecryptCBC(data, _encryptor, _IV),
            CipherMode.CipherMode.Mode.CFB => CFB.DecryptCFB(data, _encryptor, _IV),
            CipherMode.CipherMode.Mode.OFB => OFB.DecryptOFB(data, _encryptor, _IV),
            CipherMode.CipherMode.Mode.PCBC => PCBC.DecryptPCBC(data, _encryptor, _IV),
            CipherMode.CipherMode.Mode.RD => RD.DecryptRD(data, _encryptor, _IV, _delta),
            CipherMode.CipherMode.Mode.CTR => CTR.DecryptCTR(data, _encryptor, _IV),
            CipherMode.CipherMode.Mode.NONE => _encryptor.Decrypt(data),
            _ => throw new NotSupportedException()
        };
        // if (_cipherMode != CipherMode.CipherMode.Mode.NONE)
        // {
        //     return PaddingMode.PaddingMode.DeletePadding(decryptedData, _paddingMode);
        // }

        return decryptedData;
    }
}