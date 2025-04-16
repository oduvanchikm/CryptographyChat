namespace DefaultNamespace;

public class DiffieHellmanInterop
{
    private static BigInteger? privateKey;
    private static BigInteger? publicKey;
    private static BigInteger? sharedSecret;
    private static BigInteger p = BigInteger.Parse("17976931348623159077083915679378745319786029604875" +
                                                   "47640243670811216448438636763252878214128340491059" +
                                                   "186660364106107015065328323950298151797150210212709" +
                                                   "400301735657197700564195");
    private static BigInteger g = new BigInteger(2);

    private static DiffieHellman? instance;

    [JSInvokable]
    public static string GeneratePublicKey()
    {
        instance = new DiffieHellman(p, g);
        privateKey = instance.PrivateKey;
        publicKey = instance.PublicKey;
        return publicKey.ToString();
    }

    [JSInvokable]
    public static string ComputeSharedSecret(string otherPublicKeyStr)
    {
        if (instance == null || string.IsNullOrWhiteSpace(otherPublicKeyStr))
            return "Error: Keys not initialized";

        var otherPublicKey = BigInteger.Parse(otherPublicKeyStr);
        sharedSecret = instance.ComputeSharedSecret(otherPublicKey);
        return sharedSecret.ToString();
    }
}