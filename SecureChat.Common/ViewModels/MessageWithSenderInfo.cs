namespace SecureChat.Common.ViewModels;

public class MessageWithSenderInfo
{
    public int SenderId { get; set; }
    public string EncryptedContent { get; set; }
    public DateTime SentAt { get; set; }
    public string SenderUsername { get; set; }
    public bool IsCurrentUser { get; set; }
}
