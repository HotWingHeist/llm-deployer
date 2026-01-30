namespace LLMDeployer.Core.Services;

using LLMDeployer.Core.Interfaces;
using LLMDeployer.Core.Models;

public class ChatService : IChatService
{
    private readonly IModelManager _modelManager;
    private readonly Dictionary<string, ChatSession> _sessions = new();

    public ChatService(IModelManager modelManager)
    {
        _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
    }

    public ChatSession StartSession(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be empty", nameof(modelId));

        var session = new ChatSession
        {
            ModelId = modelId,
            IsActive = true
        };

        _sessions[session.Id] = session;
        return session;
    }

    public async Task<string> SendMessageAsync(string sessionId, string message)
    {
        var session = GetSession(sessionId);

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        // Add user message to history
        var userMessage = new ChatMessage
        {
            Role = "user",
            Content = message
        };
        session.History.Add(userMessage);

        // Get inference from model
        var response = await _modelManager.InferenceAsync(session.ModelId, message);

        // Add assistant response to history
        var assistantMessage = new ChatMessage
        {
            Role = "assistant",
            Content = response
        };
        session.History.Add(assistantMessage);

        return response;
    }

    public ChatSession GetSession(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
            throw new KeyNotFoundException($"Chat session {sessionId} not found");

        var session = _sessions[sessionId];
        if (!session.IsActive)
            throw new InvalidOperationException($"Chat session {sessionId} is not active");

        return session;
    }

    public IEnumerable<ChatMessage> GetChatHistory(string sessionId)
    {
        var session = _sessions.ContainsKey(sessionId) ? _sessions[sessionId] : null;
        if (session == null)
            throw new KeyNotFoundException($"Chat session {sessionId} not found");

        return session.History.AsReadOnly();
    }

    public void ClearHistory(string sessionId)
    {
        var session = GetSession(sessionId);
        session.History.Clear();
    }

    public void EndSession(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
            throw new KeyNotFoundException($"Chat session {sessionId} not found");

        var session = _sessions[sessionId];
        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
    }
}
