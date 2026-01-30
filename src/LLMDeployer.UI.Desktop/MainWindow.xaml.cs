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
            _chatViewModel.Initialize();
            
            // Add welcome message
            _chatViewModel.ChatMessages.Add(new ChatMessageViewModel
            {
                Content = "Welcome to LLM Deployer!\n\nTo use REAL AI models:\n1. Install Ollama from https://ollama.ai\n2. Run: ollama pull mistral\n3. Restart this app\n\nFor now, using intelligent mock responses.",
                Role = "system",
                IsUserMessage = false
            });
            
            // Set up Resources monitoring - find the resource panel Border and bind it
            var resourceBorder = FindVisualChild<Border>(this);
            if (resourceBorder != null)
            {
                resourceBorder.DataContext = _resourceViewModel;
                Debug.WriteLine("[LOADED] ResourceViewModel bound to Border");
            }
            else
            {
                Debug.WriteLine("[LOADED] Warning: Could not find resource Border");
            }
            
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

    private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            if (child is T result)
                return result;
            
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
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