# LLM Deployer

A **Windows desktop application** for deploying and managing local Large Language Models (LLMs) with an interactive chat interface.

## Features

- 🤖 Load and manage local LLM models
- 💬 Interactive chat interface with model inference
- 📜 Conversation history tracking
- 🔧 Model lifecycle management (load, unload, clear)

## Project Structure

- **src/LLMDeployer.UI** - Windows desktop application (console UI)
- **src/LLMDeployer.Core** - Core business logic and model management
- **tests/LLMDeployer.Core.Tests** - Unit tests (xUnit)
- **tests/LLMDeployer.Specs** - BDD feature tests (SpecFlow)

## Technology Stack

- **Framework**: .NET 8.0
- **UI**: Windows Console Application
- **Testing**: xUnit + SpecFlow
- **Mocking**: Moq
- **Architecture**: Clean architecture with dependency injection

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio Code or Visual Studio 2022+
- Windows 10/11

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11

### Building

```bash
dotnet build
```

### Running Tests

**All Tests:**
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

### Running the Application

```bash
dotnet run --project src/LLMDeployer.UI
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
