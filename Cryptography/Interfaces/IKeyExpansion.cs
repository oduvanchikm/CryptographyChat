namespace Cryptography.Interfaces;

public interface IKeyExpansion
{
    byte[][] GenerateRoundKeys(byte[] key);
}