using LLMDeployer.Core.Services;

namespace LLMDeployer.UI;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LLM Deployer ===");
        Console.WriteLine("Local LLM Deployment Tool\n");

        var modelManager = new ModelManager();

        // Demo: Load a model
        Console.WriteLine("Loading model...");
        var model = await modelManager.LoadModelAsync("C:\\models\\test-model.bin");
        Console.WriteLine($"✓ Model loaded: {model.Name} (ID: {model.Id})");

        // Demo: Run inference
        Console.WriteLine("\nRunning inference...");
        var response = await modelManager.InferenceAsync(model.Id, "What is AI?");
        Console.WriteLine($"✓ Response: {response}");

        // Demo: List models
        Console.WriteLine("\nLoaded models:");
        var models = await modelManager.GetLoadedModelsAsync();
        foreach (var m in models)
        {
            Console.WriteLine($"  - {m.Name} (Running: {m.IsRunning})");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
