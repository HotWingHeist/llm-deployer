# LLM Deployer - Local AI Chat Application

A **professional Windows desktop application** for deploying and chatting with local Large Language Models (LLMs) with real-time system resource monitoring.

## 🎯 Project Vision

Create a production-ready, locally-deployed AI chat application that monitors system resources in real-time while maintaining a clean, professional UI. All models run locally (Ollama) with intelligent fallback to mock responses.

## ✨ Current Features

- 🤖 **Local LLM Integration**: Ollama API support for real model inference
- 💬 **Interactive Chat Interface**: Clean WPF desktop GUI with message history
- 📊 **Real-time Resource Monitoring**: CPU, Memory, GPU (NVIDIA), Disk I/O, Thread count
- 🎚️ **Auto-start Monitoring**: Resource panel always visible and automatically runs
- 🎨 **Professional UI**: Two-column layout (Chat + persistent Resources panel)
- 🔄 **Mock Fallback**: Intelligent context-aware responses when Ollama unavailable
- 💾 **Session Management**: Chat history and state persistence

## 📁 Project Structure

```
├── src/
│   ├── LLMDeployer.UI/                    # Console app (original CLI)
│   ├── LLMDeployer.UI.Desktop/            # WPF Desktop app (primary UI)
│   │   ├── MainWindow.xaml(.cs)           # Main UI with two-column layout
│   │   ├── ChatViewModel.cs               # Chat logic and Ollama integration
│   │   └── ResourceViewModel.cs           # Resource monitoring UI bindings
│   └── LLMDeployer.Core/                  # Core business logic
│       └── Services/
│           ├── ModelManager.cs            # Ollama API client
│           ├── ChatService.cs             # Chat session management
│           └── ResourceMonitor.cs         # System metrics gathering
├── tests/
│   ├── LLMDeployer.Core.Tests/            # xUnit unit tests (15 tests)
│   └── LLMDeployer.Specs/                 # SpecFlow BDD scenarios (4 features)
├── README.md                              # This file
└── RESOURCE_MONITORING.md                 # Detailed monitoring implementation
```

## 🛠 Tech Stack

- **Framework**: .NET 8.0 (modern, LTS)
- **Desktop UI**: WPF (Windows Presentation Foundation) with XAML
- **Testing**: xUnit (unit tests) + SpecFlow (BDD)
- **Architecture**: Clean Architecture with SOLID principles
- **DI/IoC**: Built-in .NET dependency injection
- **External APIs**: Ollama (local LLM inference)

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Windows 10/11** (WPF is Windows-only)
- **Ollama** (optional, for real AI models): [ollama.ai](https://ollama.ai)
- **Git** (for version control)

### Setup & Development Workflow

1. **Clone the repository**
   ```bash
   git clone https://github.com/hotwingheist/llm-deployer.git
   cd "llm-deployer"
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run tests** (verify everything works)
   ```bash
   dotnet test
   ```

4. **Launch the application**
   ```bash
   dotnet run --project src/LLMDeployer.UI.Desktop
   ```

### Using with Ollama (Real AI Models)

1. Install Ollama from [ollama.ai](https://ollama.ai)
2. Pull a model: `ollama pull mistral` (or llama2, neural-chat, etc.)
3. Start Ollama: `ollama serve` (runs on `localhost:11434`)
4. Launch LLM Deployer - it will automatically connect and use the model
5. If Ollama is unavailable, app falls back to intelligent mock responses

## 📊 Resource Monitoring Features

### Always-Visible Side Panel
- **CPU Usage**: Real-time percentage with blue progress bar
- **Memory Usage**: MB and percentage with green progress bar
- **GPU Usage**: NVIDIA GPU detection with utilization % and VRAM (yellow)
- **Disk I/O**: Read/write speed in MB/s
- **Thread Count**: Active threads in the application
- **Update Frequency**: 500ms for CPU/Memory/I/O, 1s for GPU (to avoid performance impact)

### Control
- **Start Button**: Begins monitoring (auto-started by default)
- **Stop Button**: Pauses monitoring (disabled by default)
- **Status Indicator**: Shows "Monitoring: Running" or "Monitoring: Stopped"

## 🔧 Development & Vibe Coding

This project uses **vibe coding** principles - maintaining consistent architecture, clear intent, and professional standards across all contributions.

### Code Standards
- ✅ **Nullable reference types**: Enabled throughout
- ✅ **Async/await**: All I/O operations are asynchronous
- ✅ **SOLID principles**: Single responsibility, dependency injection
- ✅ **Testing first**: Unit tests + BDD scenarios before feature implementation
- ✅ **Clean names**: Self-documenting code with clear intent
- ✅ **Error handling**: Graceful degradation (GPU unavailable → "N/A", Ollama down → mock responses)

### Development Commands

```bash
# Build solution
dotnet build

# Run all tests (15 unit + 4 BDD = 19 total)
dotnet test

# Run only unit tests
dotnet test src/LLMDeployer.Core.Tests/

# Run only BDD scenarios
dotnet test tests/LLMDeployer.Specs/

# Run with coverage
dotnet test /p:CollectCoverage=true

# Launch desktop app
dotnet run --project src/LLMDeployer.UI.Desktop

# Clean all build artifacts
dotnet clean
```

### Architecture Highlights

**LayeredArchitecture:**
```
UI Layer (WPF)
    ↓
ViewModels (Data Binding)
    ↓
Services (Business Logic)
    ↓
External APIs (Ollama, System Metrics)
```

**Service Layer:**
- `ChatService`: Session & message management
- `ModelManager`: Ollama API client
- `ResourceMonitor`: System metrics (CPU, Memory, GPU, I/O, Threads)

**Testing Strategy:**
1. Write BDD feature files (.feature)
2. Implement step definitions
3. Build unit tests
4. Develop features to pass tests

## 🎨 UI/UX Features

### Chat Interface (Left Column)
- Clean message bubbles (user right, AI left)
- System message highlighting
- Real-time message display
- Input field with Send button
- Clear History & Exit buttons
- Automatic focus on input

### Resource Panel (Right Column - Always Visible)
- Fixed 350px width
- Professional styling with color-coded metrics
- Blue = CPU, Green = Memory, Yellow = GPU
- Start/Stop monitoring controls
- Legend explaining each metric
- Real-time updates (500ms refresh)

## 🐛 Troubleshooting

### Monitoring Not Showing Updates
- Ensure `ResourceViewModel.Start()` is called in `Window_Loaded`
- Check that resource panel Border is properly bound to ResourceViewModel
- Verify `_updateTimer` is started

### Ollama Connection Fails
- Ensure Ollama is running: `ollama serve`
- Check `http://localhost:11434/api/generate` is accessible
- App will automatically fall back to mock responses

### GPU Not Showing
- Requires NVIDIA GPU with `nvidia-smi` installed
- On non-NVIDIA systems, shows "N/A"
- Non-Windows systems gracefully disable GPU monitoring

## 📈 Build Status

- ✅ **Build**: Succeeds (0 errors, ~8 warnings)
- ✅ **Tests**: All 19 passing (15 unit + 4 BDD)
- ✅ **Runtime**: Stable and performant
- ✅ **Cross-module**: Clean dependencies

## 🔄 Git Workflow

### For Vibe Coding Across PCs

1. **Pull latest before coding**
   ```bash
   git pull origin master
   ```

2. **Create feature branch** (optional but recommended)
   ```bash
   git checkout -b feature/my-feature
   ```

3. **Make changes** following the vibe coding standards

4. **Commit with clear messages**
   ```bash
   git add .
   git commit -m "feature: add resource alerts; fix GPU detection timeout"
   ```

5. **Push to GitHub**
   ```bash
   git push origin master
   # or for feature branch: git push origin feature/my-feature
   ```

### Commit Message Format

- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code restructuring
- `docs:` - Documentation
- `test:` - Test additions
- `perf:` - Performance improvements

**Example:**
```
feat: add persistent resource monitoring with auto-start
- Resources panel always visible
- Monitoring auto-starts on app launch
- GPU detection via nvidia-smi
- Real-time metric updates (500ms)
```

## 📝 Key Files & Modules

| File | Purpose | Status |
|------|---------|--------|
| `MainWindow.xaml(.cs)` | Main UI layout & event handlers | ✅ Production |
| `ChatViewModel.cs` | Chat logic & Ollama integration | ✅ Production |
| `ResourceViewModel.cs` | Resource monitoring UI bindings | ✅ Production |
| `ModelManager.cs` | Ollama API client | ✅ Production |
| `ChatService.cs` | Session & message management | ✅ Production |
| `ResourceMonitor.cs` | System metrics (CPU, GPU, I/O) | ✅ Production |
| `*Tests.cs` | Unit tests (15 tests) | ✅ All Passing |
| `*.feature` | BDD scenarios (4 features) | ✅ All Passing |

## 🎯 Future Enhancements

- [ ] Historical graphs with time-series metrics
- [ ] Multi-GPU support (AMD, Intel)
- [ ] Performance logging & CSV export
- [ ] Custom metric update intervals
- [ ] Resource usage alerts/thresholds
- [ ] Per-process GPU tracking
- [ ] Model switching UI
- [ ] Custom system prompt configuration

## 📄 License & Attribution

Created as part of local AI deployment initiative. Vibe coding maintained across all future contributions.

---

**Ready to code from any PC!** 🚀 Just clone, build, and start developing. All the context you need is in this file and the codebase.**All Tests:**
```bash
dotnet test
```

**Unit Tests Only:**
```bash
dotnet test tests/LLMDeployer.Core.Tests
```

**BDD Tests Only:**
```bash
dotnet test tests/LLMDeployer.Specs
```

### Running the Applications

**Console Version (Interactive CLI):**
```bash
dotnet run --project src/LLMDeployer.UI
```

**Desktop Version (WPF GUI):**
```bash
dotnet run --project src/LLMDeployer.UI.Desktop
```

## Usage

Once the application starts:

```
🤖 LLM Deployer - Chat UI
════════════════════════════════════
Type 'help' for commands, 'exit' to quit
════════════════════════════════════

You: <your message>
🤖 Assistant: <model response>

Commands:
  help    - Show available commands
  history - Display chat history
  clear   - Clear conversation
  exit    - Exit application
```

## Test Results

- **19 tests passing** (15 unit + 4 BDD)
- ChatService: Session management, message handling, history
- ModelManager: Model loading, inference, error handling
- Full coverage of interactive features

## Development Approach

This project follows **Behavior-Driven Development (BDD)** and **Test-Driven Development (TDD)** principles:

1. **Write specifications first** - Define features in Gherkin format (.feature files)
2. **Implement step definitions** - Map feature steps to code
3. **Develop with tests** - Write unit tests before implementing features
4. **Ensure coverage** - Maintain high test coverage

## Features

- Load and manage local LLM models
- Run inference on loaded models
- List active models
- Unload models

## License

MIT
