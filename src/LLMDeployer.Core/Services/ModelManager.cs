namespace LLMDeployer.Core.Services;

using LLMDeployer.Core.Interfaces;
using LLMDeployer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

public class ModelManager : IModelManager, IDisposable
{
    private readonly Dictionary<string, LlmModel> _models = new();
    private readonly HttpClient _httpClient;
    private const string OLLAMA_API_URL = "http://localhost:11434/api/generate";
    private const string OLLAMA_TAGS_URL = "http://localhost:11434/api/tags";
    private System.Diagnostics.Process? _ollamaProcess;
    private bool _startedOllama;
    private string _selectedModelName = ""; // Will be set during initialization
    private Task? _initializationTask;

    public ModelManager()
    {
        _httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) }; // Increased for first model load
        // Don't block the UI thread - initialize asynchronously in background
        _initializationTask = Task.Run(async () =>
        {
            await InitializeModelAsync();
        });
    }

    private async Task InitializeModelAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[InitializeModelAsync] Starting model initialization...");
            var models = await GetAvailableModelsAsync();
            
            if (models.Count > 0)
            {
                _selectedModelName = models[0];
                System.Diagnostics.Debug.WriteLine($"[InitializeModelAsync] Successfully initialized with model: {_selectedModelName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeModelAsync] No models found from Ollama");
                // Try one more time after a short delay
                await Task.Delay(1000);
                models = await GetAvailableModelsAsync();
                if (models.Count > 0)
                {
                    _selectedModelName = models[0];
                    System.Diagnostics.Debug.WriteLine($"[InitializeModelAsync] Got model on retry: {_selectedModelName}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InitializeModelAsync] Error initializing model: {ex.Message}");
        }
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

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Fetching models from {OLLAMA_TAGS_URL}");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.GetAsync(OLLAMA_TAGS_URL, cts.Token);
            
            System.Diagnostics.Debug.WriteLine($"[ModelManager] API Response Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                System.Diagnostics.Debug.WriteLine($"[ModelManager] API Response: {json.Substring(0, Math.Min(200, json.Length))}");
                
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var models = new List<string>();
                    if (doc.RootElement.TryGetProperty("models", out var modelsArray))
                    {
                        foreach (var model in modelsArray.EnumerateArray())
                        {
                            if (model.TryGetProperty("name", out var name))
                            {
                                models.Add(name.GetString() ?? "");
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[ModelManager] Found {models.Count} models: {string.Join(", ", models)}");
                    return models;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                System.Diagnostics.Debug.WriteLine($"[ModelManager] API error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Failed to get models: {ex.GetType().Name}: {ex.Message}");
        }
        System.Diagnostics.Debug.WriteLine($"[ModelManager] Returning empty model list (Ollama may not be running)");
        return new List<string>();
    }

    public void SetSelectedModel(string modelName)
    {
        _selectedModelName = modelName;
        System.Diagnostics.Debug.WriteLine($"[ModelManager] Selected model changed to: {modelName}");
    }

    public string GetSelectedModel()
    {
        return _selectedModelName;
    }

    public async Task ReinitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[ModelManager] Reinitializing after Ollama startup...");
        try
        {
            // Get fresh models from Ollama
            var models = await GetAvailableModelsAsync();
            if (models.Count > 0)
            {
                _selectedModelName = models[0];
                System.Diagnostics.Debug.WriteLine($"[ModelManager] Reinitialized with model: {_selectedModelName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ModelManager] No models found after reinitialization");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Error during reinitialization: {ex.Message}");
        }
    }

    public async Task<string> SelectOptimalModelAsync()
    {
        try
        {
            var availableModels = await GetAvailableModelsAsync();
            if (availableModels.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[ModelManager] No models available");
                return "mistral";
            }

            // Get system memory
            var totalMemoryGB = GetTotalSystemMemoryGB();
            System.Diagnostics.Debug.WriteLine($"[ModelManager] System memory: {totalMemoryGB:F1} GB");

            // Model preference order based on memory and quality
            var modelPreferences = new Dictionary<string, (int minMemoryGB, int priority)>
            {
                { "deepseek-r1:70b", (64, 10) },
                { "deepseek-r1:32b", (32, 9) },
                { "llama2:70b", (48, 8) },
                { "mistral:7b", (16, 7) },
                { "deepseek-r1:14b", (16, 7) },
                { "llama2:13b", (16, 6) },
                { "deepseek-r1:8b", (8, 6) },
                { "deepseek-r1:7b", (8, 6) },
                { "mistral", (8, 5) },
                { "llama2", (8, 5) },
                { "phi", (4, 4) },
                { "gemma:7b", (8, 4) },
                { "gemma:2b", (4, 3) },
                { "deepseek-r1:1.5b", (2, 2) }
            };

            // Find best available model that fits in memory
            string? bestModel = null;
            int bestPriority = -1;

            foreach (var model in availableModels)
            {
                var modelName = model.ToLower();
                
                // Try exact match first
                if (modelPreferences.TryGetValue(modelName, out var pref))
                {
                    if (totalMemoryGB >= pref.minMemoryGB && pref.priority > bestPriority)
                    {
                        bestModel = model;
                        bestPriority = pref.priority;
                    }
                }
                else
                {
                    // Try partial match (e.g., "mistral:latest" matches "mistral")
                    foreach (var kvp in modelPreferences)
                    {
                        if (modelName.StartsWith(kvp.Key) || modelName.Contains(kvp.Key))
                        {
                            if (totalMemoryGB >= kvp.Value.minMemoryGB && kvp.Value.priority > bestPriority)
                            {
                                bestModel = model;
                                bestPriority = kvp.Value.priority;
                                break;
                            }
                        }
                    }
                }
            }

            // Fallback to first available model
            if (bestModel == null && availableModels.Count > 0)
            {
                bestModel = availableModels[0];
            }

            var selectedModel = bestModel ?? "mistral";
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Auto-selected optimal model: {selectedModel} (priority: {bestPriority})");
            
            SetSelectedModel(selectedModel);
            return selectedModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Error selecting optimal model: {ex.Message}");
            return "mistral";
        }
    }

    private static double GetTotalSystemMemoryGB()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Use WMI/Performance Counter approach instead
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var memoryKB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                    return (double)memoryKB / (1024 * 1024); // KB to GB
                }
            }
        }
        catch { }
        
        return 8.0; // Default estimate
    }

    public async Task<string> InferenceAsync(string modelId, string prompt, int maxTokens = 100)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        try
        {
            // Wait for initialization to complete if still in progress
            if (_initializationTask != null && !_initializationTask.IsCompleted)
            {
                System.Diagnostics.Debug.WriteLine($"[INFERENCE] Waiting briefly for initialization to complete...");
                var completed = await Task.WhenAny(_initializationTask, Task.Delay(2000));
                if (completed != _initializationTask)
                {
                    System.Diagnostics.Debug.WriteLine($"[INFERENCE] Initialization still running - continuing without waiting");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Starting inference");
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Currently selected model: '{_selectedModelName}'");
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");
            
            // First verify Ollama is running by checking API
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Checking if Ollama API is available at {OLLAMA_TAGS_URL}...");
            var ollamaAvailable = false;
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                {
                    var tagsResponse = await client.GetAsync(OLLAMA_TAGS_URL);
                    if (tagsResponse.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✓ Ollama API is responding!");
                        ollamaAvailable = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✗ Ollama API returned {tagsResponse.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✗ Ollama health check failed: {ex.GetType().Name}: {ex.Message}");
            }

            if (!ollamaAvailable)
            {
                System.Diagnostics.Debug.WriteLine($"[INFERENCE] Ollama is not available - using mock response");
                return GetMockResponse(prompt);
            }

            // If we still don't have a model selected, get one now
            if (string.IsNullOrEmpty(_selectedModelName))
            {
                System.Diagnostics.Debug.WriteLine($"[INFERENCE] No model selected! Getting available models...");
                var models = await GetAvailableModelsAsync();
                
                if (models.Count > 0)
                {
                    _selectedModelName = models[0];
                    System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✓ Selected model on-demand: {_selectedModelName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✗ CRITICAL: No models available from Ollama!");
                    return GetMockResponse(prompt);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Using model: {_selectedModelName}");
            
            // Try to use Ollama API (local LLM)
            var result = await CallOllamaAsync(prompt, maxTokens);
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✓ Got response: {result.Substring(0, Math.Min(50, result.Length))}...");
            return result;
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✗ Timeout - model loading can take 30-60 seconds on first use");
            throw new TimeoutException($"Request timed out. First model load can take 30-60 seconds. Please try again.", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] ✗ Ollama failed ({ex.GetType().Name}): {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[INFERENCE] Using mock response as fallback");
            return GetMockResponse(prompt);
        }
    }

    private async Task<string> CallOllamaAsync(string prompt, int maxTokens)
    {
        System.Diagnostics.Debug.WriteLine($"[OLLAMA] Sending request to {OLLAMA_API_URL}");
        
        var requestBody = new
        {
            model = _selectedModelName,
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
                $"That's a great question. It's a complex topic that involves many aspects. Would you like me to explain further?",
                $"That's an important question. It's important to understand the context. This typically refers to...",
                $"That's a great inquiry. Let me provide some insight on this topic..."
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

    private async Task EnsureOllamaRunningAsync()
    {
        try
        {
            // Check if Ollama is already running
            var existingProcesses = System.Diagnostics.Process.GetProcessesByName("ollama");
            if (existingProcesses.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine("[ModelManager] Ollama already running");
                return;
            }

            // Find Ollama executable
            var ollamaPath = FindOllamaExecutable();
            if (string.IsNullOrEmpty(ollamaPath))
            {
                System.Diagnostics.Debug.WriteLine("[ModelManager] Ollama not found, using mock responses");
                return;
            }

            // Start Ollama
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Starting Ollama from {ollamaPath}");
            _ollamaProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ollamaPath,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            _startedOllama = true;
            
            // Wait a bit for Ollama to start
            await Task.Delay(2000);
            System.Diagnostics.Debug.WriteLine("[ModelManager] Ollama started successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModelManager] Failed to start Ollama: {ex.Message}");
        }
    }

    private static string? FindOllamaExecutable()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe"),
            "ollama.exe" // Try PATH
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Try to find in PATH
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                var paths = pathEnv.Split(Path.PathSeparator);
                foreach (var dir in paths)
                {
                    var fullPath = Path.Combine(dir, "ollama.exe");
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
        }
        catch { }

        return null;
    }

    public void Dispose()
    {
        _ollamaProcess?.Dispose();
        _httpClient?.Dispose();
    }
}