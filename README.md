# LLM Deployer

A Windows desktop application for deploying and managing local Large Language Models (LLMs).

## Project Structure

- **src/LLMDeployer.UI** - Console application entry point
- **src/LLMDeployer.Core** - Core business logic and model management
- **tests/LLMDeployer.Core.Tests** - Unit tests (xUnit)
- **tests/LLMDeployer.Specs** - BDD feature tests (SpecFlow)

## Technology Stack

- **Framework**: .NET 8.0
- **UI**: Console Application (ready for WinUI 3 upgrade)
- **Testing**: xUnit + SpecFlow
- **Mocking**: Moq

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio Code or Visual Studio 2022+
- Windows 10/11

### Building

```bash
dotnet build
```

### Running Tests

**Unit Tests:**
```bash
dotnet test tests/LLMDeployer.Core.Tests
```

**BDD Feature Tests:**
```bash
dotnet test tests/LLMDeployer.Specs
```

### Running the Application

```bash
dotnet run --project src/LLMDeployer.UI
```

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
