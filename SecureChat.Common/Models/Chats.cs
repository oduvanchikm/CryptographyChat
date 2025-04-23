namespace SecureChat.Common.Models;

public class Chats
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string Algorithm { get; set; }
    
    public string? Padding { get; set; }
    
    public string? ModeCipher  { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

    public IEnumerable<ChatUser> ChatUser { get; set; }
    public IEnumerable<DhPublicKey> DhPublicKey { get; set; }
}