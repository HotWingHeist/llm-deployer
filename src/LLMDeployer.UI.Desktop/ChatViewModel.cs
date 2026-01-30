using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LLMDeployer.Core.Services;
using LLMDeployer.Core.Models;

namespace LLMDeployer.UI.Desktop;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly ModelManager _modelManager;
    private readonly ChatService _chatService;
    private ChatSession? _currentSession;
    private string _inputText = string.Empty;
    private bool _isSendEnabled = false;
    private string _statusText = "Ready";
    private LlmModel? _selectedModel;

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = new();
    public ObservableCollection<LlmModel> LoadedModels { get; } = new();

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText != value)
            {
                _inputText = value;
                OnPropertyChanged();
                IsSendEnabled = !string.IsNullOrWhiteSpace(_inputText) && _currentSession != null;
            }
        }
    }

    public bool IsSendEnabled
    {
        get => _isSendEnabled;
        set { if (_isSendEnabled != value) { _isSendEnabled = value; OnPropertyChanged(); } }
    }

    public string StatusText
    {
        get => _statusText;
        set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
    }

    public LlmModel? SelectedModel
    {
        get => _selectedModel;
        set { if (_selectedModel != value) { _selectedModel = value; OnPropertyChanged(); } }
    }

    public ChatViewModel()
    {
        _modelManager = new ModelManager();
        _chatService = new ChatService(_modelManager);
    }

    public async Task InitializeAsync()
    {
        try
        {
            StatusText = "Loading default model...";
            var model = await _modelManager.LoadModelAsync("C:\\models\\default-model.bin");
            LoadedModels.Add(model);
            SelectedModel = model;

            _currentSession = _chatService.StartSession(model.Id);
            StatusText = $"✓ Ready - Session: {_currentSession.Id.Substring(0, 8)}...";
            IsSendEnabled = true;
            
            AddSystemMessage("Chat session started. Ready to chat!");
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            AddSystemMessage($"Failed to initialize: {ex.Message}");
        }
    }

    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || _currentSession == null)
            return;

        var message = InputText;
        InputText = string.Empty;

        ChatMessages.Add(new ChatMessageViewModel 
        { 
            Content = message, 
            Role = "user",
            IsUserMessage = true 
        });

        try
        {
            StatusText = "⏳ Processing...";
            var response = await _chatService.SendMessageAsync(_currentSession.Id, message);
            
            ChatMessages.Add(new ChatMessageViewModel 
            { 
                Content = response, 
                Role = "assistant",
                IsUserMessage = false 
            });

            StatusText = "✓ Ready";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            AddSystemMessage($"Error: {ex.Message}");
        }
    }

    public void ClearHistory()
    {
        if (_currentSession != null)
        {
            _chatService.ClearHistory(_currentSession.Id);
            ChatMessages.Clear();
            StatusText = "Chat history cleared";
            AddSystemMessage("Chat history cleared");
        }
    }

    private void AddSystemMessage(string message)
    {
        ChatMessages.Add(new ChatMessageViewModel 
        { 
            Content = message, 
            Role = "system",
            IsUserMessage = false 
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class ChatMessageViewModel
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }

    public System.Windows.HorizontalAlignment Alignment => 
        IsUserMessage ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
    
    public System.Windows.Media.Brush Background =>
        Role == "user" ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 244, 255)) :
        Role == "system" ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 240)) :
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
}
