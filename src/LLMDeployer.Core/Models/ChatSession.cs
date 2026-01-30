namespace LLMDeployer.Core.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ModelId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<ChatMessage> History { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}
