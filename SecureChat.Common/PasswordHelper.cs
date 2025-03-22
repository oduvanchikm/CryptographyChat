using System.Security.Cryptography;
using System.Text;

namespace SecureChat.Common;

public class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] hashBytes = sha256.ComputeHash(passwordBytes);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2")); 
            }

            return builder.ToString();
        }
    }

    public static bool VerifyPassword(string password, string hash)
    {
        string hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }
}