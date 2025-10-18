# Copilot Instructions for HomeAutomations

This repository contains a NetDaemon-based home automation project for Home Assistant. Follow these guidelines when contributing or making changes to this codebase.

## Project Overview

- **Framework**: NetDaemon v4 (v23.44.1)
- **Runtime**: .NET 8.0
- **Language**: C# with latest language version
- **Architecture**: Event-driven automation system for Home Assistant
- **Container**: Docker/Docker Compose support

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
│   ├── LightAutomation.cs
│   ├── MotionLightAutomation.cs
│   └── TemperatureMonitor.cs
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
        if (double.TryParse(change.New?.State, out var temp))
        {
            // Check thresholds
        }
    });
```

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

## Docker Deployment

The project includes Docker support:
- `Dockerfile` - Container build definition
- `docker-compose.yml` - Service orchestration
- Environment variables passed via `.env` file

## Resources

- [NetDaemon Documentation](https://netdaemon.xyz/)
- [Home Assistant Developer Docs](https://developers.home-assistant.io/)
- [Reactive Extensions (Rx)](http://reactivex.io/)

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
