using System.Numerics;
using System.Security.Cryptography;

namespace Cryptography.DiffieHellman;

public class DiffieHellman
{
    public BigInteger P { get; }
    public BigInteger G { get; }
    public BigInteger PrivateKey { get; }
    public BigInteger PublicKey { get; }

    public DiffieHellman(string pHex, string g)
    {
        P = BigInteger.Parse("00" + pHex, System.Globalization.NumberStyles.HexNumber); // prepend to ensure positive
        G = BigInteger.Parse(g);
        PrivateKey = GeneratePrivateKey();
        PublicKey = BigInteger.ModPow(G, PrivateKey, P);
    }

    private BigInteger GeneratePrivateKey()
    {
        byte[] bytes = new byte[256 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        var key = new BigInteger(bytes);
        if (key < 0) key = -key;
        return (key % (P - 1)) + 1;
    }

    public BigInteger ComputeSharedSecret(BigInteger otherPublicKey)
    {
        return BigInteger.ModPow(otherPublicKey, PrivateKey, P);
    }
}