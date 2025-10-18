# Contributing to HomeAutomations

Thank you for considering contributing to this project! Here are some guidelines to help you get started.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature or bug fix
4. Make your changes
5. Test your changes
6. Submit a pull request

## Development Setup

### Prerequisites
- .NET 8.0 SDK or later
- A Home Assistant instance for testing
- A code editor (Visual Studio, Visual Studio Code, or Rider recommended)

### Building the Project
```bash
# Build using solution file (recommended)
dotnet restore HomeAutomations.sln
dotnet build HomeAutomations.sln

# Or build individual projects
dotnet restore src/NetDaemon/HomeAutomations.csproj
dotnet build src/NetDaemon/HomeAutomations.csproj
```

### Running Tests
```bash
# Run all tests
dotnet test HomeAutomations.sln

# Run tests with detailed output
dotnet test HomeAutomations.sln --verbosity normal

# Run tests for a specific project
dotnet test tests/HomeAutomations.Tests/HomeAutomations.Tests.csproj
```

### Running Locally
1. Copy `src/NetDaemon/appsettings.local.json.example` to `src/NetDaemon/appsettings.local.json`
2. Fill in your Home Assistant connection details
3. Run the application:
   ```bash
   dotnet run --project src/NetDaemon/HomeAutomations.csproj
   ```

## Creating New Automations

### Basic Structure
1. Create a new C# file in the `src/NetDaemon/apps/` directory
2. Add the `[NetDaemonApp]` attribute to your class
3. Inject `IHaContext` and `ILogger<YourClass>` in the constructor
4. Subscribe to Home Assistant events or state changes

### Example
```csharp
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps;

[NetDaemonApp]
public class MyAutomation
{
    public MyAutomation(IHaContext ha, ILogger<MyAutomation> logger)
    {
        ha.Entity("light.my_light")
            .StateChanges()
            .Subscribe(change =>
            {
                logger.LogInformation("Light changed from {Old} to {New}",
                    change.Old?.State, change.New?.State);
            });

        logger.LogInformation("MyAutomation initialized");
    }
}
```

## Coding Standards

- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Log important events and errors
- Handle exceptions gracefully
- Write clean, readable code

## Testing

### Unit Tests
The project includes a test suite using xUnit and Moq:

- Test your automations thoroughly before submitting
- Write unit tests for new automation classes
- Verify that your code builds without errors
- Run all tests and ensure they pass: `dotnet test HomeAutomations.sln`
- Ensure existing automations and tests still work

### Writing Tests
1. Create test files in the `tests/HomeAutomations.Tests/` directory
2. Follow the naming convention: `<ClassName>Tests.cs`
3. Use Moq to mock `IHaContext` and `ILogger` dependencies
4. Use xUnit's `Assert` class for assertions

Example test:
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using NetDaemon.HassModel;
using HomeAutomations.Apps;
using System.Reactive.Subjects;
using NetDaemon.HassModel.Entities;

namespace HomeAutomations.Tests;

public class MyAutomationTests
{
    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<MyAutomation>>();
        
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var automation = new MyAutomation(mockHaContext.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(automation);
    }
}
```

## Pull Request Guidelines

- Provide a clear description of what your PR does
- Reference any related issues
- Keep changes focused and minimal
- Update documentation if needed
- Ensure the code builds successfully

## Code Review

All submissions require review. We aim to provide feedback within a few days.

## Questions?

Feel free to open an issue for any questions or concerns.
