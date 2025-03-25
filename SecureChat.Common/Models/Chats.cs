namespace SecureChat.Common.Models;

public class Chats
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public IEnumerable<Sessions> Session { get; set; }
}