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
            "ECB" => Mode.ECB,
            "CBC" => Mode.CBC,
            "PCBC" => Mode.PCBC,
            "CFB" => Mode.CFB,
            "OFB" => Mode.OFB,
            "CTR" => Mode.CTR,
            "RD" => Mode.RD,
            "NONE" => Mode.NONE,
            _ => throw new ArgumentException($"Unknown cipher mode: {cipherModeString}")
        };
    }
}