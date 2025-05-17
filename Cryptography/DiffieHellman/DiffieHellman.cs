using System.Numerics;
using System.Security.Cryptography;

namespace Cryptography.DiffieHellman;

public class DiffieHellman
{
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
    
    public static (BigInteger g, BigInteger p) GenerateParameters(int bitLength)
    {
        BigInteger p = GenerateProbablePrime(bitLength);
        BigInteger g = FindPrimitiveRootSimple(p);
        return (g, p);
    }

    private static BigInteger GenerateProbablePrime(int bitLength)
    {
        byte[] bytes = new byte[bitLength / 8 + 1];
        BigInteger p;
    
        do {
            rng.GetBytes(bytes);
            bytes[^1] &= (byte)(0xFF >> (8 - bitLength % 8));
            p = new BigInteger(bytes, isUnsigned: true);
            p |= BigInteger.One;
        } while (!IsProbablePrime(p, 20));
    
        return p;
    }

    private static BigInteger FindPrimitiveRootSimple(BigInteger p)
    {
        if (p == 2) return 1;
        BigInteger pMinus1 = p - 1;
    
        foreach (var g in new[] { 2, 3, 5, 7, 11 })
        {
            if (BigInteger.ModPow(g, pMinus1, p) == 1)
                return g;
        }
    
        throw new Exception("Failed to find primitive root");
    }

    private static bool IsProbablePrime(BigInteger n, int certainty)
    {
        if (n < 2) return false;
        if (n == 2 || n == 3) return true;
        if (n % 2 == 0) return false;
        
        BigInteger d = n - 1;
        int s = 0;
        
        while (d % 2 == 0)
        {
            d /= 2;
            s++;
        }

        byte[] bytes = new byte[n.ToByteArray().LongLength];

        for (int i = 0; i < certainty; i++)
        {
            BigInteger a;
            do
            {
                rng.GetBytes(bytes);
                a = new BigInteger(bytes, isUnsigned: true);
            }
            while (a < 2 || a >= n - 2);

            BigInteger x = BigInteger.ModPow(a, d, n);
            if (x == 1 || x == n - 1)
                continue;

            bool passedLoop = false;
            for (int j = 0; j < s - 1; j++)
            {
                x = BigInteger.ModPow(x, 2, n);
                if (x == 1) return false;
                if (x == n - 1)
                {
                    passedLoop = true;
                    break;
                }
            }

            if (!passedLoop)
                return false;
        }

        return true;
    }
}