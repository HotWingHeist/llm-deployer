namespace LLMDeployer.Core.Services;

using LLMDeployer.Core.Interfaces;
using LLMDeployer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class ModelManager : IModelManager
{
    private readonly Dictionary<string, LlmModel> _models = new();
    private readonly HttpClient _httpClient;
    private const string OLLAMA_API_URL = "http://localhost:11434/api/generate";

    public ModelManager()
    {
        _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(120) };
    }

    public Task<LlmModel> LoadModelAsync(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be empty", nameof(modelPath));

        var model = new LlmModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = Path.GetFileNameWithoutExtension(modelPath),
            Path = modelPath,
            IsRunning = true
        };

        _models[model.Id] = model;
        return Task.FromResult(model);
    }

    public Task UnloadModelAsync(string modelId)
    {
        if (!_models.ContainsKey(modelId))
            throw new KeyNotFoundException($"Model with ID {modelId} not found");

        _models.Remove(modelId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LlmModel>> GetLoadedModelsAsync()
    {
        return Task.FromResult(_models.Values.AsEnumerable());
    }

    public async Task<string> InferenceAsync(string modelId, string prompt, int maxTokens = 100)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        try
        {
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Starting inference request to Ollama...");
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Ollama URL: {OLLAMA_API_URL}");
            // Try to use Ollama API (local LLM)
            var result = await CallOllamaAsync(prompt, maxTokens);
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Got Ollama response successfully");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Ollama failed ({ex.GetType().Name}): {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Falling back to mock response");
            // Fallback to mock response if Ollama is not available
            return GetMockResponse(prompt);
        }
    }

    private async Task<string> CallOllamaAsync(string prompt, int maxTokens)
    {
        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Sending request to {OLLAMA_API_URL}");
        
        var requestBody = new
        {
            model = "mistral",  // Change to your preferred model: mistral, neural-chat, llama2, etc.
            prompt = prompt,
            stream = false,
            num_predict = maxTokens,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Request body: {json.Substring(0, Math.Min(150, json.Length))}...");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Making HTTP POST request...");
        var response = await _httpClient.PostAsync(OLLAMA_API_URL, content);
        
        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Response status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[OLLAMA] Error response: {errorContent}");
            throw new HttpRequestException($"Ollama API returned {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Response JSON: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");

        using (JsonDocument doc = JsonDocument.Parse(responseJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("response", out var responseText))
            {
                var text = responseText.GetString() ?? "No response";
                System.Diagnostics.Debug.WriteLine($"[OLLAMA] Extracted response: {text.Substring(0, Math.Min(100, text.Length))}...");
                return text;
            }
        }

        throw new InvalidOperationException("No response field in Ollama API response");
    }

    private string GetMockResponse(string prompt)
    {
        System.Diagnostics.Debug.WriteLine($"[INFERENCE] Using enhanced mock response");
        
        // More realistic and context-aware mock responses
        var mockResponses = new Dictionary<string, string[]>
        {
            { "hello", new[] { 
                "Hello! How can I assist you today?",
                "Hi there! What can I help you with?",
                "Greetings! Feel free to ask me anything."
            }},
            { "how are you", new[] { 
                "I'm doing well, thank you for asking! I'm here to help with any questions you have.",
                "I'm functioning well and ready to assist. How can I help you?",
                "I'm in good shape and ready to chat. What's on your mind?"
            }},
            { "what is", new[] { 
                $"That's a great question about {prompt.Substring(8)}. It's a complex topic that involves many aspects. Would you like me to explain further?",
                $"Regarding {prompt.Substring(8)}, it's important to understand the context. This typically refers to...",
                $"{prompt.Substring(8)} is an interesting subject. Let me provide some insight..."
            }},
            { "help", new[] { 
                "Of course! I'm here to help. What do you need assistance with?",
                "I'd be happy to help! What can I do for you?",
                "Sure thing! What kind of help do you need?"
            }},
            { "thank", new[] { 
                "You're welcome! Feel free to ask if you need anything else.",
                "Happy to help! Let me know if there's anything else.",
                "My pleasure! Don't hesitate to reach out again."
            }}
        };

        // Try to match keywords in the prompt
        foreach (var key in mockResponses.Keys)
        {
            if (prompt.ToLower().Contains(key))
            {
                var responses = mockResponses[key];
                var random = new Random();
                return responses[random.Next(responses.Length)];
            }
        }

        // Default responses if no keyword match
        var defaultResponses = new[]
        {
            $"That's an interesting point about '{prompt.Substring(0, Math.Min(25, prompt.Length))}'. Could you tell me more?",
            $"Regarding '{prompt.Substring(0, Math.Min(25, prompt.Length))}', I think there are several important aspects to consider.",
            $"I appreciate your question about '{prompt.Substring(0, Math.Min(25, prompt.Length))}'. This is a nuanced topic.",
            $"You raise a good point about '{prompt.Substring(0, Math.Min(25, prompt.Length))}'. In my analysis, key factors include...",
            $"Interesting thought on '{prompt.Substring(0, Math.Min(25, prompt.Length))}'. Let me elaborate on that."
        };

        var rnd = new Random();
        return defaultResponses[rnd.Next(defaultResponses.Length)];
    }
}
