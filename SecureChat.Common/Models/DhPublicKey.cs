namespace SecureChat.Common.Models;

public class DhPublicKey
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int ChatId { get; set; }
    public Chats Chat { get; set; }

    public string PublicKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }
}