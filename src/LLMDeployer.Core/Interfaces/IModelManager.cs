namespace LLMDeployer.Core.Interfaces;

using LLMDeployer.Core.Models;

public interface IModelManager : IDisposable
{
    Task<LlmModel> LoadModelAsync(string modelPath);
    Task UnloadModelAsync(string modelId);
    Task<IEnumerable<LlmModel>> GetLoadedModelsAsync();
    Task<string> InferenceAsync(string modelId, string prompt, int maxTokens = 100);
}
