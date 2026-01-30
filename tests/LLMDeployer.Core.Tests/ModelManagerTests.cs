using Xunit;
using LLMDeployer.Core.Services;
using LLMDeployer.Core.Models;

namespace LLMDeployer.Core.Tests;

public class ModelManagerTests
{
    [Fact]
    public async Task LoadModelAsync_WithValidPath_ShouldReturnModel()
    {
        // Arrange
        var modelManager = new ModelManager();
        string modelPath = "C:\\models\\test-model.bin";

        // Act
        var result = await modelManager.LoadModelAsync(modelPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-model", result.Name);
        Assert.Equal(modelPath, result.Path);
        Assert.True(result.IsRunning);
    }

    [Fact]
    public async Task LoadModelAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var modelManager = new ModelManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => modelManager.LoadModelAsync(""));
    }

    [Fact]
    public async Task UnloadModelAsync_WithValidId_ShouldRemoveModel()
    {
        // Arrange
        var modelManager = new ModelManager();
        var model = await modelManager.LoadModelAsync("C:\\models\\test.bin");

        // Act
        await modelManager.UnloadModelAsync(model.Id);

        // Assert
        var models = await modelManager.GetLoadedModelsAsync();
        Assert.Empty(models);
    }

    [Fact]
    public async Task GetLoadedModelsAsync_ShouldReturnAllModels()
    {
        // Arrange
        var modelManager = new ModelManager();
        await modelManager.LoadModelAsync("C:\\models\\model1.bin");
        await modelManager.LoadModelAsync("C:\\models\\model2.bin");

        // Act
        var result = await modelManager.GetLoadedModelsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task InferenceAsync_WithValidInput_ShouldReturnResult()
    {
        // Arrange
        var modelManager = new ModelManager();
        var model = await modelManager.LoadModelAsync("C:\\models\\test.bin");

        // Act
        var result = await modelManager.InferenceAsync(model.Id, "hello");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // The mock response returns one of the predefined hello responses
        Assert.True(result.Length > 0, "Expected non-empty response");
    }
}
