namespace SecureChat.Common.ViewModels;

public class SendMessageRequest
{
    public string EncryptedContent { get; set; }
    public string? Algorithm { get; set; }
    public DateTimeOffset? ClientTimestamp { get; set; }
}