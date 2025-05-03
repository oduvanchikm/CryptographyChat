namespace SecureChat.Common.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Username { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTimeOffset LastLogin { get; set; }
    public IEnumerable<ChatUser> ChatUser { get; set; }
}