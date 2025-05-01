namespace SecureChat.Common.ViewModels;

public class ChatDeletedEvent
{
    public int ChatId { get; set; }
    public DateTime DeletedAt { get; set; }
}