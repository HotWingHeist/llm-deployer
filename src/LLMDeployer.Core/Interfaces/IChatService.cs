namespace LLMDeployer.Core.Interfaces;

using LLMDeployer.Core.Models;

public interface IChatService
{
    ChatSession StartSession(string modelId);
    Task<string> SendMessageAsync(string sessionId, string message);
    ChatSession GetSession(string sessionId);
    IEnumerable<ChatMessage> GetChatHistory(string sessionId);
    void ClearHistory(string sessionId);
    void EndSession(string sessionId);
}
