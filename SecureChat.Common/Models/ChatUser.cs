namespace SecureChat.Common.Models;

public class ChatUser
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int ChatId { get; set; }
    public Chats Chat { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTime.UtcNow;
}