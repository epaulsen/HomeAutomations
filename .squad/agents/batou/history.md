# Batou — Project History

## Core Context

**Project:** HomeAutomations
**User:** Erling Paulsen
**Stack:** .NET 9 / C# / NetDaemon v4 / Docker
**Purpose:** Home automation app running as a Docker container, connecting to Home Assistant via MQTT and HA WebSocket API

### Key Architecture
- Framework: NetDaemon v4 (JoySoftware.NetDaemon.*)
- Automations live in `src/NetDaemon/apps/`
- All automation classes require `[NetDaemonApp]` attribute and namespace `HomeAutomations.Apps`
- Tests in `tests/HomeAutomations.Tests/` using xUnit + Moq
- Configuration: `appsettings.json` / `appsettings.local.json` / environment variables for Docker
- Deployment: Docker via `docker-compose.yml`

### Existing Automations
- **CostSensorApp** (`apps/CostSensor/`) — electricity cost calculation from energy sensors + tariff data
- **NordPoolSensorApp** (`apps/NordPoolApp/`) — fetches and exposes NordPool electricity prices
- **DeviceTrackerApp** (`apps/UnifiApp/`) — tracks UniFi network devices in HA

### Code Conventions
- **Allman brace style** — opening brace always on its own line
- **CultureInfo.InvariantCulture** for all numeric parsing
- Null-conditional operators (`?.`) for HA state access
- Structured logging with named parameters
- Never commit secrets — use `appsettings.local.json` or environment variables

## Learnings

