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
                System.Diagnostics.Debug.WriteLine($"InputText changed to: '{value}', updating IsSendEnabled");
                UpdateSendEnabled();
            }
        }
    }

    private void UpdateSendEnabled()
    {
        var oldState = _isSendEnabled;
        var newState = !string.IsNullOrWhiteSpace(_inputText) && _currentSession != null;
        if (oldState != newState)
        {
            System.Diagnostics.Debug.WriteLine($"IsSendEnabled: {oldState} -> {newState} (InputText='{_inputText}', CurrentSession={_currentSession != null})");
            IsSendEnabled = newState;
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

    public void Initialize()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[INIT] Step 1: Starting initialization...");
            StatusText = "Initializing...";
            
            System.Diagnostics.Debug.WriteLine("[INIT] Step 2: Creating default model...");
            var model = new LlmModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default LLM",
                Path = "default"
            };
            System.Diagnostics.Debug.WriteLine($"[INIT] Step 2b: Model created with ID: {model.Id}");
            
            System.Diagnostics.Debug.WriteLine("[INIT] Step 3: Adding to collections...");
            LoadedModels.Add(model);
            SelectedModel = model;

            System.Diagnostics.Debug.WriteLine("[INIT] Step 4: Creating session...");
            _currentSession = _chatService.StartSession(model.Id);
            System.Diagnostics.Debug.WriteLine($"[INIT] Step 4b: Session created: {_currentSession.Id}");
            
            System.Diagnostics.Debug.WriteLine("[INIT] Step 5: Setting UI state...");
            StatusText = "✓ Ready";
            IsSendEnabled = true;
            
            System.Diagnostics.Debug.WriteLine("[INIT] Step 6: INITIALIZATION COMPLETE!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[INIT] ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[INIT] StackTrace: {ex.StackTrace}");
            StatusText = $"Error: {ex.Message}";
            IsSendEnabled = false;
        }
    }

    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || _currentSession == null)
        {
            System.Diagnostics.Debug.WriteLine($"[SEND] Cannot send: InputText empty={string.IsNullOrWhiteSpace(InputText)}, Session null={_currentSession == null}");
            return;
        }

        var message = InputText;
        InputText = string.Empty;

        System.Diagnostics.Debug.WriteLine($"[SEND] User message: {message}");
        ChatMessages.Add(new ChatMessageViewModel 
        { 
            Content = message, 
            Role = "user",
            IsUserMessage = true 
        });

        try
        {
            StatusText = "Waiting for LLM response...";
            System.Diagnostics.Debug.WriteLine($"[SEND] Calling ChatService.SendMessageAsync...");
            var response = await _chatService.SendMessageAsync(_currentSession.Id, message);
            System.Diagnostics.Debug.WriteLine($"[SEND] Got response: {response.Substring(0, Math.Min(100, response.Length))}");
            
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
            System.Diagnostics.Debug.WriteLine($"[SEND] Error: {ex.Message}");
            StatusText = $"Error: {ex.Message}";
            ChatMessages.Add(new ChatMessageViewModel
            {
                Content = $"Error: {ex.Message}",
                Role = "system",
                IsUserMessage = false
            });
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
