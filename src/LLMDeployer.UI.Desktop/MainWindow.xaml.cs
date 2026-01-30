using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace LLMDeployer.UI.Desktop;

public partial class MainWindow : Window
{
    private ChatViewModel _chatViewModel;
    private ResourceViewModel _resourceViewModel;

    public MainWindow()
    {
        InitializeComponent();
        _chatViewModel = new ChatViewModel();
        _resourceViewModel = new ResourceViewModel();
        Debug.WriteLine("[CTOR] MainWindow created");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[LOADED] Window_Loaded called");
        
        try
        {
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
            
            // Update status
            StatusTextBlock.Text = _chatViewModel.StatusText;
            
            InputTextBox.Focus();
            Debug.WriteLine("[LOADED] Window_Loaded complete");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ERROR: {ex.Message}\n{ex.StackTrace}", "ERROR");
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
}