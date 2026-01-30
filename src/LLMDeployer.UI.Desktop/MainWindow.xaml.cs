using System.Windows;

namespace LLMDeployer.UI.Desktop;

public partial class MainWindow : Window
{
    private ChatViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new ChatViewModel();
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        InputTextBox.Focus();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SendMessageAsync();
        InputTextBox.Focus();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearHistory();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
