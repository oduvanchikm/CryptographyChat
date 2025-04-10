using System.Text.Json.Serialization;

namespace SecureChat.Common.Models;

public class ChatMessageEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string EncryptedContent { get; set; }
    public DateTime SentAt { get; set; }

    [JsonIgnore] public User? Sender { get; set; }
}