namespace LLMDeployer.Core.Models;

public class LlmModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
