# HomeAutomations

A NetDaemon-based home automation project for Home Assistant. This project provides a flexible and powerful way to create automations using C# and .NET.

## Features

- **NetDaemon Integration**: Built on NetDaemon v4, a modern .NET framework for Home Assistant automation
- **Type-Safe**: Leverage C#'s type safety and IntelliSense for writing automations
- **Extensible**: Easy to add new automations as C# classes
- **Containerized**: Docker support for easy deployment
- **Example Automations**: Includes several example automations to get you started

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Docker](https://www.docker.com/) and Docker Compose (for containerized deployment)
- A running Home Assistant instance
- A [Long-Lived Access Token](https://developers.home-assistant.io/docs/auth_api/#long-lived-access-token) from Home Assistant

## Project Structure

```
HomeAutomations/
├── src/
│   └── NetDaemon/                     # NetDaemon application directory
│       ├── apps/                      # Automation app files
│       │   ├── CostSensorApp.cs       # Electricity cost calculation
│       │   ├── LightAutomation.cs     # Sunrise/sunset light control
│       │   ├── MotionLightAutomation.cs # Motion-activated lighting
│       │   ├── TemperatureMonitor.cs  # Temperature monitoring with alerts
│       │   ├── cost_sensors.yaml      # Cost sensor configuration
│       │   └── example_config.yaml    # Example YAML configuration
│       ├── HomeAutomations.csproj     # Project file
│       ├── Program.cs                 # Application entry point
│       ├── appsettings.json           # Base configuration
│       └── appsettings.local.json.example # Local settings template
├── Dockerfile                         # Container build file
├── docker-compose.yml                 # Container orchestration
└── README.md                          # This file
```

## Getting Started

### Option 1: Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/epaulsen/HomeAutomations.git
   cd HomeAutomations
   ```

2. **Configure Home Assistant connection**
   
   Copy the example settings file:
   ```bash
   cp src/NetDaemon/appsettings.local.json.example src/NetDaemon/appsettings.local.json
   ```
   
   Edit `src/NetDaemon/appsettings.local.json` with your Home Assistant details:
   ```json
   {
     "HomeAssistant": {
       "Host": "your-homeassistant-host",
       "Port": 8123,
       "Ssl": true,
       "Token": "your_long_lived_access_token"
     }
   }
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore src/NetDaemon/HomeAutomations.csproj
   ```

4. **Build the project**
   ```bash
   dotnet build src/NetDaemon/HomeAutomations.csproj
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/NetDaemon/HomeAutomations.csproj
   ```

### Option 2: Docker Deployment

1. **Clone the repository**
   ```bash
   git clone https://github.com/epaulsen/HomeAutomations.git
   cd HomeAutomations
   ```

2. **Configure environment**
   
   Copy the example environment file:
   ```bash
   cp .env.example .env
   ```
   
   Edit `.env` with your Home Assistant token:
   ```
   HASS_TOKEN=your_long_lived_access_token_here
   ```
   
   Copy and configure local settings:
   ```bash
   cp src/NetDaemon/appsettings.local.json.example src/NetDaemon/appsettings.local.json
   ```
   
   Edit `src/NetDaemon/appsettings.local.json` with your Home Assistant host details.

3. **Build and run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

4. **View logs**
   ```bash
   docker-compose logs -f netdaemon
   ```

## Creating Your Own Automations

To create a new automation:

1. Create a new C# class in the `src/NetDaemon/apps/` directory
2. Add the `[NetDaemonApp]` attribute to your class
3. Inject `IHaContext` and `ILogger<YourClass>` in the constructor
4. Subscribe to Home Assistant events or state changes
5. Implement your automation logic

Example:
```csharp
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace HomeAutomations.Apps;

[NetDaemonApp]
public class MyAutomation
{
    private readonly IHaContext _ha;
    private readonly ILogger<MyAutomation> _logger;

    public MyAutomation(IHaContext ha, ILogger<MyAutomation> logger)
    {
        _ha = ha;
        _logger = logger;

        // Subscribe to events
        _ha.Events.Where(e => e.EventType == "state_changed")
            .Subscribe(OnStateChanged);

        _logger.LogInformation("MyAutomation initialized");
    }

    private void OnStateChanged(Event evt)
    {
        // Your automation logic here
    }
}
```

## Included Example Automations

### LightAutomation
Automatically controls lights based on sunrise and sunset:
- Turns on living room lights at sunset
- Turns off living room lights at sunrise

### MotionLightAutomation
Motion-activated lighting:
- Turns on hallway lights when motion is detected
- Turns off lights 5 minutes after motion clears

### TemperatureMonitor
Monitors temperature and sends notifications:
- Sends alert when temperature exceeds 25°C
- Sends alert when temperature drops below 18°C
- Notifies when temperature returns to normal

### CostSensorApp
Manages electricity cost calculations:
- Listens to energy sensor changes and calculates costs based on tariff sensors
- Configured via `cost_sensors.yaml` file
- Supports multiple cost sensors with individual tariff and energy sensors
- Handles first state change events gracefully (skips when no old value)
- Includes optional cron field for future reset schedule functionality

**Note**: These examples use placeholder entity IDs. Update them in the respective files to match your Home Assistant entities.

## Configuration

### appsettings.json
Base configuration file with default settings.

### appsettings.local.json
Local overrides for sensitive information (not committed to git). Use this file to store your Home Assistant connection details and tokens.

### Environment Variables
For Docker deployments, you can also use environment variables:
- `HomeAssistant__Host`: Home Assistant hostname or IP
- `HomeAssistant__Port`: Home Assistant port (default: 8123)
- `HomeAssistant__Ssl`: Use SSL (true/false)
- `HomeAssistant__Token`: Long-lived access token

## Development Tips

- Use `ILogger` for debugging and monitoring
- Entity IDs must match your Home Assistant configuration
- Test automations carefully before deploying to production
- Use `appsettings.local.json` for local development settings
- Keep sensitive tokens out of source control

## Troubleshooting

### Connection Issues
- Verify your Home Assistant is accessible from the NetDaemon container/host
- Check that your long-lived access token is valid
- Ensure the port and SSL settings are correct

### Automation Not Triggering
- Check the logs for error messages
- Verify entity IDs match your Home Assistant setup
- Use Home Assistant's Developer Tools to verify entity states

### Build Errors
- Ensure .NET 9.0 SDK is installed
- Run `dotnet restore` to fetch dependencies
- Check that all required packages are available

## Resources

- [NetDaemon Documentation](https://netdaemon.xyz/)
- [Home Assistant Documentation](https://www.home-assistant.io/docs/)
- [Home Assistant Developer Docs](https://developers.home-assistant.io/)

## License

This project is open source. Feel free to modify and extend it for your own needs.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
