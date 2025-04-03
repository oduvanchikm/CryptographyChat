namespace SecureChat.Common.ViewModels;

public class SendMessageDto
{
    public int ChatId { get; set; }
    public string EncryptedMessage { get; set; }
}