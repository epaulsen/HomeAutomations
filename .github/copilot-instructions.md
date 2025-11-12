# Copilot Instructions for HomeAutomations

This repository contains a NetDaemon-based home automation project for Home Assistant. Follow these guidelines when contributing or making changes to this codebase.

## Quick Reference

### Essential Commands
```bash
# Build the solution
dotnet build HomeAutomations.sln --configuration Release

# Run tests
dotnet test HomeAutomations.sln

# Run locally (requires Home Assistant connection)
dotnet run --project src/NetDaemon/HomeAutomations.csproj
```

### Key Files
- `src/NetDaemon/apps/` - Automation applications
- `.github/copilot-instructions.md` - This file
- `CONTRIBUTING.md` - Contribution guidelines
- `README.md` - Getting started guide

### When Adding New Automations
1. Create file in `src/NetDaemon/apps/YourFeature/`
2. Use namespace `HomeAutomations.Apps`
3. Add `[NetDaemonApp]` attribute
4. Inject `IHaContext` and `ILogger<T>`
5. Subscribe to state changes or events
6. Log initialization
7. Add tests in `tests/HomeAutomations.Tests/`

## Important: Consult Documentation First

**When working on issues in this repository, always refer to the official documentation instead of making assumptions about how things work.**

### Primary Documentation Sources (Whitelisted & Encouraged)

1. **NetDaemon Documentation**: https://netdaemon.xyz/
   - This is the official NetDaemon framework documentation
   - Contains comprehensive guides, API references, and examples
   - **Always consult this first** when uncertain about NetDaemon behavior or features

2. **NetDaemon GitHub Repository**: https://github.com/net-daemon/netdaemon/
   - Source code reference for NetDaemon framework
   - Useful for understanding implementation details and advanced usage
   - Contains additional examples and test cases

Both URLs are **whitelisted and actively encouraged** for GitHub Copilot to access. Use these resources to:
- Understand NetDaemon APIs and patterns
- Find code examples and best practices
- Verify correct usage of framework features
- Resolve uncertainties about behavior

## Project Overview

- **Framework**: NetDaemon v4 (v23.44.1)
- **Runtime**: .NET 9.0
- **Language**: C# with latest language version
- **Architecture**: Event-driven automation system for Home Assistant
- **Container**: Docker/Docker Compose support

### Repository Structure
- **src/NetDaemon/** - Main application code
  - **apps/** - Automation applications
  - **Models/** - Data models and DTOs
  - **Services/** - Service layer (HTTP clients, etc.)
  - **Program.cs** - Application entry point
  - **appsettings.json** - Configuration

- **tests/HomeAutomations.Tests/** - Unit tests using xUnit and Moq
- **.github/** - GitHub-specific files (workflows, documentation)
- **Dockerfile** - Container image definition
- **docker-compose.yml** - Local development and deployment

### GitHub Workflow
1. **Development**: Work in feature branches
2. **Pull Requests**: Target `main` branch
3. **CI**: Build workflow runs automatically on PRs
4. **CD**: Docker images published to DockerHub on merge to `main` (requires approval)

## Technology Stack

### Core Dependencies
- `JoySoftware.NetDaemon.AppModel` - NetDaemon application model
- `JoySoftware.NetDaemon.HassModel` - Home Assistant model
- `JoySoftware.NetDaemon.Runtime` - NetDaemon runtime
- `Microsoft.Extensions.Hosting` - .NET hosting infrastructure
- `Serilog.AspNetCore` - Structured logging

### Key Interfaces
- `IHaContext` - Home Assistant context for interacting with entities and services
- `ILogger<T>` - Structured logging interface

## Code Structure

### Directory Layout
```
src/NetDaemon/
├── apps/                    # Automation applications
│   ├── CostSensor/          # Cost sensor automation
│   ├── NordPoolApp/         # NordPool sensor automation
│   └── config/              # Configuration files
├── HomeAutomations.csproj   # Project file
├── Program.cs               # Application entry point
└── appsettings.json         # Configuration
```

## Coding Standards

### Automation Structure
All automation classes should follow this pattern:

```csharp
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps;

/// <summary>
/// Brief description of what this automation does
/// </summary>
[NetDaemonApp]
public class MyAutomation
{
    public MyAutomation(IHaContext ha, ILogger<MyAutomation> logger)
    {
        // Initialization and subscriptions
        logger.LogInformation("MyAutomation initialized");
    }
}
```

### Key Conventions
1. **Namespace**: Always use `HomeAutomations.Apps` for automation classes
2. **Attribute**: Mark classes with `[NetDaemonApp]` attribute
3. **Constructor**: Inject `IHaContext` and `ILogger<YourClass>`
4. **Logging**: Always log initialization and important state changes
5. **Documentation**: Add XML summary comments for all automation classes
6. **File Location**: Place new automations in `src/NetDaemon/apps/` directory

### Existing Automations
The repository includes several working automations that serve as examples:

1. **CostSensorApp** - Electricity cost calculation
   - Located in `apps/CostSensor/`
   - Monitors energy sensors and calculates costs based on tariff data
   - Configured via `apps/config/cost_sensors.yaml`
   - Handles first state changes gracefully (skips when no previous value)

2. **NordPoolSensorApp** - Electricity price tracking
   - Located in `apps/NordPoolApp/`
   - Fetches and exposes NordPool electricity prices
   - Creates sensors for current and subsidized prices
   - Updates periodically

3. **DeviceTrackerApp** - UniFi device tracking
   - Located in `apps/UnifiApp/`
   - Tracks devices on UniFi network
   - Updates Home Assistant device tracker states

These automations demonstrate real-world patterns and can be used as templates for new automations.

### Entity ID Format
- Use string literals for entity IDs (e.g., `"light.living_room"`)
- Entity IDs follow Home Assistant format: `domain.object_id`
- Common domains: `light`, `switch`, `sensor`, `binary_sensor`, `sun`, `climate`

### State Subscriptions
Use the reactive pattern with `StateChanges()`:

```csharp
ha.Entity("sensor.temperature")
    .StateChanges()
    .Subscribe(change =>
    {
        // Handle state change
        logger.LogInformation("Temperature changed from {Old} to {New}",
            change.Old?.State, change.New?.State);
    });
```

### Service Calls
Call Home Assistant services using:

```csharp
ha.CallService("domain", "service", data: new { entity_id = "entity.id" });
```

Common services:
- `light.turn_on` / `light.turn_off`
- `switch.turn_on` / `switch.turn_off`
- `notify.notify` (for notifications)

### Logging Best Practices
- Use structured logging with named parameters
- Log levels:
  - `LogInformation`: Normal operation events
  - `LogWarning`: Unexpected but handled situations
  - `LogError`: Error conditions
  - `LogDebug`: Detailed debugging information

Example:
```csharp
logger.LogInformation("Light {EntityId} turned {State}", entityId, state);
```

## Building and Testing

### Build Commands
```bash
# Restore dependencies
dotnet restore src/NetDaemon/HomeAutomations.csproj

# Build project
dotnet build src/NetDaemon/HomeAutomations.csproj --configuration Release

# Run locally (requires Home Assistant connection)
dotnet run --project src/NetDaemon/HomeAutomations.csproj
```

### Configuration
- Base config: `src/NetDaemon/appsettings.json`
- Local overrides: `src/NetDaemon/appsettings.local.json` (not in source control)
- Environment variables: Use `HomeAssistant__*` pattern for Docker deployments

## Common Patterns

### Time-based Automations
For sunrise/sunset or time-based triggers, subscribe to the `sun.sun` entity or use Home Assistant's time entities.

### Motion Detection
Subscribe to `binary_sensor` entities with device class `motion`:

```csharp
ha.Entity("binary_sensor.hallway_motion")
    .StateChanges()
    .Where(e => e.New?.State == "on")
    .Subscribe(change =>
    {
        // Handle motion detected
    });
```

### Temperature Monitoring
Monitor numeric sensors and implement thresholds:

```csharp
ha.Entity("sensor.temperature")
    .StateChanges()
    .Subscribe(change =>
    {
        if (double.TryParse(change.New?.State, CultureInfo.InvariantCulture, out var temp))
        {
            // Check thresholds
        }
    });
```

### Number Parsing
Always use culture-invariant parsing for numeric values to ensure consistent behavior across different locales:

```csharp
// Always specify CultureInfo.InvariantCulture when parsing numbers
if (double.TryParse(value, CultureInfo.InvariantCulture, out var result))
{
    // Use result
}

// Also applies to other numeric types
if (int.TryParse(value, CultureInfo.InvariantCulture, out var intResult))
{
    // Use intResult
}
```

Remember to add `using System.Globalization;` at the top of your file when using `CultureInfo`.

### Delayed Actions
For actions that should happen after a delay:

```csharp
Observable.Timer(TimeSpan.FromMinutes(5))
    .Subscribe(_ =>
    {
        // Delayed action
    });
```

## Error Handling

- Wrap service calls in try-catch blocks for critical operations
- Log exceptions with full context
- Handle null states gracefully (entities may be unavailable)
- Use `?.` null-conditional operator when accessing state properties

### Exception Handling Pattern
```csharp
try
{
    ha.CallService("light", "turn_on", data: new { entity_id = "light.living_room" });
    logger.LogInformation("Light turned on successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to turn on light {EntityId}", "light.living_room");
}
```

## Debugging and Troubleshooting

### Common Issues and Solutions

#### Automation Not Loading
- **Symptom**: Automation class doesn't initialize
- **Check**: Ensure `[NetDaemonApp]` attribute is present
- **Check**: Verify namespace is `HomeAutomations.Apps`
- **Check**: Look for constructor injection errors in logs

#### State Changes Not Triggering
- **Symptom**: Subscriptions don't fire when entity state changes
- **Check**: Verify entity ID exists in Home Assistant
- **Check**: Ensure entity ID is spelled correctly (case-sensitive)
- **Check**: Check Home Assistant logs for connectivity issues
- **Solution**: Use Home Assistant Developer Tools → States to verify entity names

#### Numeric Parsing Failures
- **Symptom**: `FormatException` when parsing sensor values
- **Check**: Ensure using `CultureInfo.InvariantCulture`
- **Check**: Handle cases where state might be "unavailable" or "unknown"
- **Solution**: Always use `TryParse` instead of `Parse`

#### Connection Issues
- **Symptom**: NetDaemon can't connect to Home Assistant
- **Check**: Verify `appsettings.local.json` has correct host/port
- **Check**: Ensure long-lived access token is valid
- **Check**: Confirm SSL setting matches your Home Assistant setup
- **Check**: Test connectivity: `curl http://your-ha-host:8123/api/`

### Logging Best Practices for Debugging
```csharp
// Use structured logging with context
logger.LogDebug("Processing state change for {EntityId}: {OldState} → {NewState}",
    entityId, change.Old?.State, change.New?.State);

// Log at appropriate levels
logger.LogTrace("Detailed debugging info");     // Very verbose, typically disabled
logger.LogDebug("Debug information");            // Development debugging
logger.LogInformation("Normal operation");       // Key application events
logger.LogWarning("Unexpected but handled");     // Potential issues
logger.LogError(ex, "Operation failed");         // Errors that need attention
```

## Security Considerations

- Never commit tokens or sensitive data to source control
- Use `appsettings.local.json` for local development secrets
- Use environment variables or Docker secrets for production
- Keep `.env` files in `.gitignore`

## Testing

- This project does not currently have unit tests
- Test automations manually in a Home Assistant test environment
- Verify entity IDs exist in your Home Assistant instance
- Monitor logs for initialization and runtime errors

### Unit Testing
The project includes a comprehensive test suite in `tests/HomeAutomations.Tests/`:
- Uses **xUnit** as the test framework
- Uses **Moq** for mocking dependencies
- Tests focus on automation logic and behavior
- Run tests with: `dotnet test HomeAutomations.sln`

### Writing Tests for Automations
When creating new automations, consider adding unit tests:

```csharp
public class MyAutomationTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var mockHaContext = new Mock<IHaContext>();
        var mockLogger = new Mock<ILogger<MyAutomation>>();
        
        // Setup necessary mocks
        var stateSubject = new Subject<StateChange>();
        mockHaContext.Setup(x => x.StateAllChanges()).Returns(stateSubject);

        // Act
        var automation = new MyAutomation(mockHaContext.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(automation);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

### Test Coverage Goals
- Constructor initialization and logging
- State change subscriptions and filtering
- Service calls with correct parameters
- Error handling and edge cases
- Null state handling

## Docker Deployment

The project includes Docker support:
- `Dockerfile` - Container build definition
- `docker-compose.yml` - Service orchestration
- Environment variables passed via `.env` file

## Anti-Patterns to Avoid

### Common Mistakes
1. **Hardcoding Configuration Values**
   - ❌ Don't: `var host = "192.168.1.100";`
   - ✅ Do: Use configuration from `appsettings.json` or environment variables

2. **Ignoring Null Safety**
   - ❌ Don't: `change.New.State` (can throw NullReferenceException)
   - ✅ Do: `change.New?.State` (use null-conditional operator)

3. **Blocking Operations in Event Handlers**
   - ❌ Don't: Use `Thread.Sleep()` or blocking I/O in subscriptions
   - ✅ Do: Use `Observable.Timer()` or async patterns

4. **Not Logging Important Events**
   - ❌ Don't: Silently handle state changes without logging
   - ✅ Do: Always log significant state changes and errors

5. **Using Wrong Culture for Number Parsing**
   - ❌ Don't: `double.Parse(value)` (culture-dependent)
   - ✅ Do: `double.TryParse(value, CultureInfo.InvariantCulture, out var result)`

6. **Forgetting the NetDaemonApp Attribute**
   - ❌ Don't: Create automation classes without `[NetDaemonApp]`
   - ✅ Do: Always mark automation classes with `[NetDaemonApp]` attribute

## CI/CD and Automation

### GitHub Actions Workflows
- **Build Workflow** (`.github/workflows/build.yml`) - Runs on PRs and pushes to main
  - Builds the project with .NET 8.0
  - Runs tests (currently continues on error)
  - Validates code compiles correctly
  
- **Docker Workflow** (`.github/workflows/docker.yml`) - Publishes Docker images
  - Requires manual approval via PROD environment
  - Publishes to DockerHub as `epaulsen/homeautomation:latest`
  - Tagged with both `latest` and commit SHA

### Environment Protection
The repository uses GitHub Environments for deployment protection. See `.github/ENVIRONMENT_SETUP.md` for details on configuring the PROD environment.

## Contributing Guidelines

For detailed contribution instructions, see [CONTRIBUTING.md](../CONTRIBUTING.md).

Key points:
- Fork the repository and create a feature branch
- Follow the coding standards outlined in this document
- Write tests for new automations
- Ensure all tests pass: `dotnet test HomeAutomations.sln`
- Submit a pull request with a clear description

## Project Documentation

### Additional Documentation Files
- **[README.md](../README.md)** - Getting started guide and project overview
- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Detailed contribution guidelines and development setup
- **[.github/ENVIRONMENT_SETUP.md](ENVIRONMENT_SETUP.md)** - GitHub environment configuration for CI/CD

## Resources

### Official NetDaemon Resources (Whitelisted - Use These!)
- **[NetDaemon Documentation](https://netdaemon.xyz/)** - Primary reference for all NetDaemon features and APIs
- **[NetDaemon GitHub Repository](https://github.com/net-daemon/netdaemon/)** - Source code and implementation details

### Related Documentation
- [Home Assistant Developer Docs](https://developers.home-assistant.io/) - For Home Assistant entity and service information
- [Reactive Extensions (Rx)](http://reactivex.io/) - For understanding reactive programming patterns used in NetDaemon

## Common Tasks

### Adding a New Automation
1. Create a new `.cs` file in `src/NetDaemon/apps/`
2. Add namespace `HomeAutomations.Apps`
3. Add `[NetDaemonApp]` attribute to the class
4. Inject `IHaContext` and `ILogger<YourClass>`
5. Subscribe to relevant state changes or events
6. Log initialization

### Modifying Existing Automations
- Keep changes minimal and focused
- Maintain existing logging patterns
- Update XML documentation if behavior changes
- Test thoroughly before committing

### Updating Dependencies
- Update `HomeAutomations.csproj` package references
- Ensure compatibility with NetDaemon v4
- Test after updates to verify functionality
