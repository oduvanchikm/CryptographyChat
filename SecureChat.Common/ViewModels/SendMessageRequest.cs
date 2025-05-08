namespace SecureChat.Common.ViewModels;

public class SendMessageRequest
{
    public string Message { get; set; } = default!;
    public string PublicKey { get; set; }
    
    public string? ContentType { get; set; }
    public string? FileName { get; set; }
}