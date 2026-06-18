namespace nhom2.Domain.Entities;

public class ChatSession
{
    public int Id { get; set; }
    public int CustomerUserId { get; set; }
    public string? CustomerEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
}
