using System.Numerics;

namespace Cryptography.Interfaces;

public interface IDiffieHellmanProtocol
{
    (string publicKey, string privateKey) GenerateKeyPair();
    string CalculateSharesSecret(string publicKey, string privateKey);
}