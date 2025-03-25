using System.Numerics;

namespace Cryptography.Interfaces;

public interface IDiffieHellmanProtocol
{
    BigInteger GeneratePrivateKey();
    BigInteger GeneratePublicKey(BigInteger privateKey);
    BigInteger ComputeSharedSecret(BigInteger publicKey, BigInteger privateKey);
}