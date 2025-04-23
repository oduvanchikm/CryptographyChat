namespace Cryptography.CipherMode;

public class CipherMode
{
    public enum Mode
    {
        ECB, // 
        CBC, //
        PCBC, //
        CFB, //
        OFB, //
        CTR,
        RD, //
        NONE //
    };
    
    public static Mode ToCipherMode(string cipherModeString)
    {
        return cipherModeString switch
        {
            "ECB" => CipherMode.Mode.ECB,
            "CBC" => CipherMode.Mode.CBC,
            "PCBC" => CipherMode.Mode.PCBC,
            "CFB" => CipherMode.Mode.CFB,
            "OFB" => CipherMode.Mode.OFB,
            "CTR" => CipherMode.Mode.CTR,
            "RD" => CipherMode.Mode.RD,
            "NONE" => CipherMode.Mode.NONE,
            _ => throw new ArgumentException($"Unknown cipher mode: {cipherModeString}")
        };
    }
}