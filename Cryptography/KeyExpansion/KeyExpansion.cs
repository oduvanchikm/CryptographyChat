using Cryptography.Interfaces;

namespace Cryptography.KeyExpansion;

public class KeyExpansion : IKeyExpansion
{
    private readonly int rounds = 12;
    
    public byte[][] GenerateRoundKeys(byte[] key)
    {
        byte[] P = { 0xB7, 0xE1, 0x51, 0x63 };
        byte[] Q = { 0x9E, 0x37, 0x79, 0xB9 };

        byte[][] S = new byte[rounds][];
        
        S[0] = (byte[])P.Clone();;

        for (int i = 1; i < rounds; ++i)
        {
            S[i] = new byte[4];
            int carry = 0;
            
            for (int j = 3; j >= 0; --j) 
            {
                int sum = S[i - 1][j] + Q[j] + carry;
                S[i][j] = (byte)(sum & 0xFF);
                carry = sum >> 8;
            }
        }

        return S;
    }
}