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
dotnet restore
dotnet build
```

### Running Locally
1. Copy `appsettings.local.json.example` to `appsettings.local.json`
2. Fill in your Home Assistant connection details
3. Run the application:
   ```bash
   dotnet run
   ```

## Creating New Automations

### Basic Structure
1. Create a new C# file in the `apps/` directory
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

- Test your automations thoroughly before submitting
- Verify that your code builds without errors
- Ensure existing automations still work

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
