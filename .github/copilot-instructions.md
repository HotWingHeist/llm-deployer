# LLM Deployer - Development Guidelines

## Project Overview
Local LLM deployment Windows desktop application using C# with WinUI 3, following BDD/TDD principles.

## Architecture
- **UI Layer**: WinUI 3 (LLMDeployer.UI)
- **Core Logic**: Model management and inference (LLMDeployer.Core)
- **Tests**: xUnit unit tests + SpecFlow BDD tests

## Testing Strategy
1. Write Gherkin features first (.feature files)
2. Implement step definitions
3. Write unit tests for implementation
4. Develop features to pass tests

## Key Commands
- Build: `dotnet build`
- Run tests: `dotnet test`
- Run app: `dotnet run --project src/LLMDeployer.UI`

## Code Standards
- Enable nullable reference types
- Use async/await for I/O operations
- Follow SOLID principles
- Dependency injection for services
