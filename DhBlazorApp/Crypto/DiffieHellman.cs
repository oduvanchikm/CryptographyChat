namespace DefaultNamespace;

public class DiffieHellman
{
    public BigInteger PrivateKey { get; private set; }
    public BigInteger PublicKey { get; private set; }
    public BigInteger P { get; private set; } 
    public BigInteger G { get; private set; } 

    public DiffieHellman(BigInteger p, BigInteger g)
    {
        P = p;
        G = g;
        GenerateKeys();
    }

    private void GeneratePublicKey()
    {
        // PublicKey = G^PrivateKey mod P
        byte[] bytes = new byte[128];
        RandomNumberGenerator.Fill(bytes);
        PrivateKey = new BigInteger(bytes, true);
        PublicKey = BigInteger.ModPow(G, PrivateKey, P);
    }

    public BigInteger ComputeSharedSecret(BigInteger otherPublicKey)
    {
        // S = B^PrivateKey mod P
        return BigInteger.ModPow(otherPublicKey, PrivateKey, P);
    }
}