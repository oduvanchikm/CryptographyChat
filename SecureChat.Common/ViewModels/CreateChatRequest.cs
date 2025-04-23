namespace SecureChat.Common.ViewModels;

public class CreateChatRequest
{
    public int ParticipantId { get; set; }
    public string Algorithm { get; set; }
    public string Padding { get; set; }
    public string ModeCipher { get; set; }
}