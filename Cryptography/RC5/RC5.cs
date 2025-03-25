using Cryptography.Interfaces;

namespace Cryptography.RC5;

public class RC5 : ISymmetricEncryptionAlgorithm
{
    private readonly byte[][] S;
    private const int rounds = 12; 
    private const int t = 26;
    
    public RC5(byte[][] expandedKey)
    {
        if (expandedKey.Length != t || expandedKey[0].Length != 4)
            throw new ArgumentException($"Invalid key size. Expected {t} words of 4 bytes each");
        
        S = expandedKey;
    }
    
    public byte[] Encrypt(byte[] plaintext, byte[] ciphertext)
    {
        BitManipulation.ValidateBlock(plaintext, ciphertext);

        byte[] A = BitManipulation.AddBytes(BitManipulation.GetWord(plaintext, 0), S[0]);
        byte[] B = BitManipulation.AddBytes(BitManipulation.GetWord(plaintext, 4), S[1]);

        for (int i = 1; i <= rounds; i++)
        {
            byte[] temp = BitManipulation.XorBytes(A, B);
            A = BitManipulation.AddBytes(BitManipulation.RotateLeft(temp, BitConverter.ToInt32(B, 0)), S[2 * i]);
            
            temp = BitManipulation.XorBytes(B, A);
            B = BitManipulation.AddBytes(BitManipulation.RotateLeft(temp, BitConverter.ToInt32(A, 0)), S[2 * i + 1]);
        }

        Array.Copy(A, 0, ciphertext, 0, 4);
        Array.Copy(B, 0, ciphertext, 4, 4);
        
        return ciphertext;
    }
    
    public byte[] Decrypt(byte[] ciphertext, byte[] plaintext)
    {
        BitManipulation.ValidateBlock(ciphertext, plaintext);

        byte[] A = BitManipulation.GetWord(ciphertext, 0);
        byte[] B = BitManipulation.GetWord(ciphertext, 4);

        for (int i = rounds; i > 0; i--)
        {
            byte[] temp = BitManipulation.SubtractBytes(B, S[2 * i + 1]);
            B = BitManipulation.XorBytes(BitManipulation.RotateRight(temp, BitConverter.ToInt32(A, 0)), A);
            
            temp = BitManipulation.SubtractBytes(A, S[2 * i]);
            A = BitManipulation.XorBytes(BitManipulation.RotateRight(temp, BitConverter.ToInt32(B, 0)), B);
        }

        Array.Copy(BitManipulation.SubtractBytes(A, S[0]), 0, plaintext, 0, 4);
        Array.Copy(BitManipulation.SubtractBytes(B, S[1]), 0, plaintext, 4, 4);
        
        return plaintext;
    }
}