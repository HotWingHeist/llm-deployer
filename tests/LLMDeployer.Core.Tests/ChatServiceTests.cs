using Xunit;
using Moq;
using LLMDeployer.Core.Services;
using LLMDeployer.Core.Interfaces;
using LLMDeployer.Core.Models;

namespace LLMDeployer.Core.Tests;

public class ChatServiceTests
{
    private readonly Mock<IModelManager> _modelManagerMock;
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _modelManagerMock = new Mock<IModelManager>();
        _chatService = new ChatService(_modelManagerMock.Object);
    }

    [Fact]
    public void StartSession_WithValidModelId_ShouldCreateActiveSession()
    {
        // Arrange
        string modelId = "model-123";

        // Act
        var session = _chatService.StartSession(modelId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(modelId, session.ModelId);
        Assert.True(session.IsActive);
        Assert.Empty(session.History);
    }

    [Fact]
    public void StartSession_WithEmptyModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _chatService.StartSession(""));
    }

    [Fact]
    public async Task SendMessageAsync_WithValidMessage_ShouldAddToHistory()
    {
        // Arrange
        var session = _chatService.StartSession("model-123");
        _modelManagerMock
            .Setup(m => m.InferenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("Test response");

        // Act
        var response = await _chatService.SendMessageAsync(session.Id, "Hello");

        // Assert
        Assert.NotNull(response);
        var history = _chatService.GetChatHistory(session.Id);
        Assert.Equal(2, history.Count()); // User message + assistant response
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var session = _chatService.StartSession("model-123");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _chatService.SendMessageAsync(session.Id, ""));
    }

    [Fact]
    public void GetSession_WithValidId_ShouldReturnSession()
    {
        // Arrange
        var createdSession = _chatService.StartSession("model-123");

        // Act
        var retrievedSession = _chatService.GetSession(createdSession.Id);

        // Assert
        Assert.Equal(createdSession.Id, retrievedSession.Id);
    }

    [Fact]
    public void GetSession_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _chatService.GetSession("invalid-id"));
    }

    [Fact]
    public async Task GetChatHistory_ShouldReturnAllMessages()
    {
        // Arrange
        var session = _chatService.StartSession("model-123");
        _modelManagerMock
            .Setup(m => m.InferenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("Response");

        // Act
        await _chatService.SendMessageAsync(session.Id, "Message 1");
        await _chatService.SendMessageAsync(session.Id, "Message 2");
        var history = _chatService.GetChatHistory(session.Id);

        // Assert
        Assert.Equal(4, history.Count()); // 2 messages + 2 responses
        Assert.Equal("user", history.First().Role);
        Assert.Equal("assistant", history.Skip(1).First().Role);
    }

    [Fact]
    public void ClearHistory_ShouldRemoveAllMessages()
    {
        // Arrange
        var session = _chatService.StartSession("model-123");
        session.History.Add(new ChatMessage { Role = "user", Content = "Test" });

        // Act
        _chatService.ClearHistory(session.Id);

        // Assert
        var history = _chatService.GetChatHistory(session.Id);
        Assert.Empty(history);
    }

    [Fact]
    public void EndSession_ShouldDeactivateSession()
    {
        // Arrange
        var session = _chatService.StartSession("model-123");

        // Act
        _chatService.EndSession(session.Id);

        // Assert
        Assert.False(session.IsActive);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void EndSession_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _chatService.EndSession("invalid-id"));
    }
}
