using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Net.Http;

namespace LLMDeployer.UI.Desktop;

public partial class MainWindow : Window
{
    private System.Windows.Threading.DispatcherTimer? _ollamaStatusTimer;
    private bool _ollamaIsRunning = false;
    private readonly ChatViewModel _chatViewModel;
    private readonly ResourceViewModel _resourceViewModel;

    private static void Log(string message)
    {
        try
        {
            string logPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "LLMDeployer",
                "debug.log"
            );
            
            string? dir = System.IO.Path.GetDirectoryName(logPath);
            if (dir != null && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}\n");
        }
        catch { }
        Debug.WriteLine(message);
    }

    public MainWindow()
    {
        Log("[CTOR] ========== CONSTRUCTOR CALLED ==========");
        InitializeComponent();
        Log("[CTOR] InitializeComponent completed");
        _chatViewModel = new ChatViewModel();
        _resourceViewModel = new ResourceViewModel();
        DataContext = _chatViewModel;
        StatusTextBlock.DataContext = _chatViewModel;
        StatusBarText.DataContext = _chatViewModel;
        Log("[CTOR] ViewModels created");
        Debug.WriteLine("[CTOR] MainWindow created");
        Log("[CTOR] ========== CONSTRUCTOR COMPLETE ==========");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Log("[LOADED] ========== WINDOW LOADED ==========");
        
        try
        {
            // Test if UI elements exist
            Log($"[LOADED] Testing UI elements...");
            Log($"[LOADED] OllamaStatusIndicator exists: {OllamaStatusIndicator != null}");
            Log($"[LOADED] OllamaStatusText exists: {OllamaStatusText != null}");
            
            // Set up Chat Tab
            ChatMessagesPanel.ItemsSource = _chatViewModel.ChatMessages;
            ModelSelectionComboBox.DataContext = _chatViewModel;
            
            // Subscribe to collection changes for auto-scroll
            _chatViewModel.ChatMessages.CollectionChanged += (s, e) => 
            {
                Dispatcher.InvokeAsync(() => 
                {
                    ChatScrollViewer.ScrollToBottom();
                }, System.Windows.Threading.DispatcherPriority.Background);
            };
            
            _chatViewModel.Initialize();
            
            // Add welcome message
            _chatViewModel.ChatMessages.Add(new ChatMessageViewModel
            {
                Content = "Welcome to LLM Deployer!\n\nTo use REAL AI models:\n1. Install Ollama from https://ollama.ai\n2. Run: ollama pull mistral\n3. Restart this app\n\nFor now, using intelligent mock responses.",
                Role = "system",
                IsUserMessage = false
            });
            
            // Set up Resources monitoring - bind to the named resource panel
            ResourcePanelBorder.DataContext = _resourceViewModel;
            Debug.WriteLine("[LOADED] ResourceViewModel bound to ResourcePanelBorder");
            
            // Auto-start monitoring
            _resourceViewModel.Start();
            StartMonitoringButton.IsEnabled = false;
            StopMonitoringButton.IsEnabled = true;
            MonitoringStatusText.Text = "Monitoring: Running";
            Debug.WriteLine("[LOADED] Monitoring auto-started");
            
            InputTextBox.Focus();
            
            // Start Ollama status monitor - THIS IS CRITICAL
            Log("[LOADED] About to start Ollama status monitor...");
            StartOllamaStatusMonitor();
            Log("[LOADED] Ollama status monitor started");
            
            Log("[LOADED] ========== WINDOW LOADED COMPLETE ==========");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LOADED] ERROR: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[LOADED] Stack: {ex.StackTrace}");
            MessageBox.Show($"ERROR: {ex.Message}\n{ex.StackTrace}", "ERROR");
        }
    }

    private void StartOllamaStatusMonitor()
    {
        Log($"[TIMER SETUP] ========== Starting Timer Setup ==========");
        try
        {
            Log($"[TIMER SETUP] Creating DispatcherTimer...");
            _ollamaStatusTimer = new System.Windows.Threading.DispatcherTimer();
            
            Log($"[TIMER SETUP] Setting interval to 1 second...");
            _ollamaStatusTimer.Interval = TimeSpan.FromSeconds(1);
            
            Log($"[TIMER SETUP] Adding Tick event handler...");
            int tickCount = 0;
            _ollamaStatusTimer.Tick += async (s, e) => 
            {
                tickCount++;
                Log($"[TIMER TICK #{tickCount}] ================================================");
                await UpdateOllamaStatusAsync();
            };
            
            Log($"[TIMER SETUP] Starting timer...");
            _ollamaStatusTimer.Start();
            Log($"[TIMER SETUP] Timer started successfully");
            
            // Check status immediately
            Log($"[TIMER SETUP] Doing initial status check...");
            _ = UpdateOllamaStatusAsync();
            
            Log($"[TIMER SETUP] ========== Timer Setup Complete ==========");
        }
        catch (Exception ex)
        {
            Log($"[TIMER SETUP] ERROR: {ex.GetType().Name}: {ex.Message}");
            Log($"[TIMER SETUP] Stack: {ex.StackTrace}");
        }
    }

    private async Task UpdateOllamaStatusAsync()
    {
        try
        {
            Log($"[STATUS UPDATE] ========== Start Status Check ==========");
            
            var isRunning = false;
            var statusCode = "Unknown";
            
            try
            {
                Log($"[STATUS UPDATE] Creating HTTP client with 2 second timeout...");
                var handler = new System.Net.Http.HttpClientHandler
                {
                    UseProxy = false,
                    Proxy = null
                };

                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(2);

                    var versionUrls = new[]
                    {
                        "http://127.0.0.1:11434/api/version",
                        "http://localhost:11434/api/version"
                    };

                    foreach (var versionUrl in versionUrls)
                    {
                        try
                        {
                            Log($"[STATUS UPDATE] Sending GET to {versionUrl}...");
                            var response = await client.GetAsync(versionUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                            statusCode = response.StatusCode.ToString();
                            isRunning = response.IsSuccessStatusCode;

                            Log($"[STATUS UPDATE] ✓ Response received: HTTP {statusCode}, isRunning={isRunning}");
                            if (isRunning)
                            {
                                break;
                            }
                        }
                        catch (TaskCanceledException tcEx)
                        {
                            Log($"[STATUS UPDATE] ✗ Timeout on {versionUrl}: {tcEx.Message}");
                            statusCode = "Timeout";
                            isRunning = false;
                        }
                    }
                }
            }
            catch (TaskCanceledException tcEx)
            {
                Log($"[STATUS UPDATE] ✗ Timeout: {tcEx.Message}");
                statusCode = "Timeout";
                isRunning = false;
            }
            catch (HttpRequestException hEx)
            {
                Log($"[STATUS UPDATE] ✗ HTTP Error: {hEx.Message}");
                statusCode = "No Connection";
                isRunning = false;
            }
            catch (Exception innerEx)
            {
                Log($"[STATUS UPDATE] ✗ Inner Exception ({innerEx.GetType().Name}): {innerEx.Message}");
                statusCode = innerEx.GetType().Name;
                isRunning = false;
            }
            
            _ollamaIsRunning = isRunning;
            Log($"[STATUS UPDATE] Determined: isRunning={isRunning}");
            
            // Update UI
            Log($"[STATUS UPDATE] Updating UI...");
            await Dispatcher.InvokeAsync(() =>
            {
                if (isRunning)
                {
                    Log($"[STATUS UPDATE] → Setting to GREEN (Running)");
                    OllamaStatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69));
                    OllamaStatusText.Text = "Ollama: Running ✓";
                    OllamaStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69));
                }
                else
                {
                    Log($"[STATUS UPDATE] → Setting to RED (Not Running) - Status: {statusCode}");
                    OllamaStatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69));
                    OllamaStatusText.Text = $"Ollama: Not Running ({statusCode})";
                    OllamaStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69));
                }
            });
            Log($"[STATUS UPDATE] ========== Check Complete ==========");
        }
        catch (Exception ex)
        {
            Log($"[STATUS UPDATE] FATAL EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log($"[STATUS UPDATE] Stack: {ex.StackTrace}");
            _ollamaIsRunning = false;
            
            try
            {
                OllamaStatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69));
                OllamaStatusText.Text = "Ollama: Error";
                OllamaStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69));
            }
            catch (Exception uiEx)
            {
                Log($"[STATUS UPDATE] UI Update failed: {uiEx.Message}");
            }
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string text = InputTextBox.Text;
            Debug.WriteLine($"[SEND] Button clicked with text: '{text}'");
            
            if (string.IsNullOrWhiteSpace(text))
                return;
            
            InputTextBox.Text = "";
            _chatViewModel.InputText = text;
            
            await _chatViewModel.SendMessageAsync();
            
            InputTextBox.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SEND] Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Error");
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _chatViewModel.ClearHistory();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _resourceViewModel.Stop();
        _resourceViewModel.Dispose();
        _chatViewModel?.Dispose();
        Debug.WriteLine("[EXIT] Application closing");
        Application.Current.Shutdown();
    }

    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Return)
        {
            SendButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
        }
    }

    private void StartMonitoring_Click(object sender, RoutedEventArgs e)
    {
        _resourceViewModel.Start();
        StartMonitoringButton.IsEnabled = false;
        StopMonitoringButton.IsEnabled = true;
        MonitoringStatusText.Text = "Monitoring: Running";
        Debug.WriteLine("[RESOURCES] Monitoring started");
    }

    private void StopMonitoring_Click(object sender, RoutedEventArgs e)
    {
        _resourceViewModel.Stop();
        StartMonitoringButton.IsEnabled = true;
        StopMonitoringButton.IsEnabled = false;
        MonitoringStatusText.Text = "Monitoring: Stopped";
        Debug.WriteLine("[RESOURCES] Monitoring stopped");
    }

    private async void StartOllama_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if ollama is already running by testing the API
            using (var client = new System.Net.Http.HttpClient() { Timeout = System.TimeSpan.FromSeconds(2) })
            {
                try
                {
                    var response = await client.GetAsync("http://localhost:11434/api/tags");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Ollama is already running!", "Info");
                        await UpdateOllamaStatusAsync();
                        return;
                    }
                }
                catch
                {
                    // Not running, continue to start it
                }
            }

            // Find Ollama executable
            var possiblePaths = new[]
            {
                @"C:\Users\zhife\AppData\Local\Programs\Ollama\ollama app.exe",
                @"C:\Users\zhife\AppData\Local\Programs\Ollama\ollama.exe",
                @"C:\Program Files\Ollama\ollama.exe",
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama app.exe"),
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe")
            };

            string? ollamaPath = null;
            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    ollamaPath = path;
                    Debug.WriteLine($"[OLLAMA] Found at: {path}");
                    break;
                }
            }

            if (ollamaPath == null)
            {
                MessageBox.Show("Ollama not found. Please install Ollama from https://ollama.ai\n\nTried locations:\n" + string.Join("\n", possiblePaths.Take(3)), "Error");
                return;
            }

            MessageBox.Show($"Starting Ollama...\n\nThis may take a few moments.", "Starting Ollama");
            
            // Use powershell to start Ollama in background
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"& '{ollamaPath}'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var process = System.Diagnostics.Process.Start(psi);
            Debug.WriteLine($"[OLLAMA] Started with PID: {process?.Id}");
            
            // Wait for Ollama to start
            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(1000);
                try
                {
                    using (var client = new System.Net.Http.HttpClient() { Timeout = System.TimeSpan.FromSeconds(2) })
                    {
                        var response = await client.GetAsync("http://localhost:11434/api/tags");
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Ollama started successfully!", "Success");
                            Debug.WriteLine("[OLLAMA] Ollama started and responding");
                            
                            // CRITICAL: Reinitialize model manager to pick up available models
                            await _chatViewModel.ReinitializeModelsAsync();
                            
                            await _chatViewModel.RefreshModelsAsync();
                            await UpdateOllamaStatusAsync();
                            return;
                        }
                    }
                }
                catch { }
            }
            
            MessageBox.Show("Ollama was started but may still be initializing.\n\nPlease wait a moment and check the status indicator.", "Info");
            await UpdateOllamaStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting Ollama: {ex.Message}", "Error");
            Debug.WriteLine($"[OLLAMA] Error: {ex.Message}");
        }
    }

    private async void StopOllama_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("ollama");
            if (processes.Length == 0)
            {
                MessageBox.Show("Ollama is not running", "Info");
                return;
            }

            foreach (var process in processes)
            {
                process.Kill();
            }
            MessageBox.Show("Ollama service stopped", "Success");
            Debug.WriteLine("[OLLAMA] Ollama service stopped");
            
            // Update status indicator
            await UpdateOllamaStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error stopping Ollama: {ex.Message}", "Error");
            Debug.WriteLine($"[OLLAMA] Error stopping: {ex.Message}");
        }
    }

    // Test button to manually refresh status
    private async void TestStatusButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Status Indicator exists: {OllamaStatusIndicator != null}\n" +
                       $"Status Text exists: {OllamaStatusText != null}\n" +
                       $"Current _ollamaIsRunning: {_ollamaIsRunning}\n" +
                       $"Current indicator color: {(OllamaStatusIndicator?.Fill)}\n" +
                       $"Current text: {OllamaStatusText?.Text}", "Status Debug");
        await UpdateOllamaStatusAsync();
        MessageBox.Show($"After update _ollamaIsRunning: {_ollamaIsRunning}\n" +
                       $"After update indicator color: {(OllamaStatusIndicator?.Fill)}\n" +
                       $"After update text: {OllamaStatusText?.Text}", "Status After Update");
    }
}