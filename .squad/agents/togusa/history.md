# Togusa — Project History

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


## Learnings

### Full Codebase Code Review — 2026-03-22

**Cross-Cutting Issues Togusa Should Track:**

**Docker/Deployment:**
- docker-compose.yml line 19: `depends_on: - homeassistant` references service that is commented out
- docker-compose up will fail until fixed
- Fix: Remove `depends_on` block or uncomment service

**Infrastructure/Rx Patterns:**
- 4 Rx subscription leaks identified (NordPoolSensor, NordPoolSubsidizedSensor, DeviceTrackerApp, NetworkDeviceTrackerApp)
- Subscriptions discarded in SubscribeAsync() calls never disposed
- Apps restart without cleanup → subscriptions accumulate → feature silently degrades
- Pattern: Store IDisposable from SubscribeAsync(), implement IDisposable on app, call dispose in shutdown
- CostSensor subdomain does this correctly (reference for other apps)

**Package Management:**
- Duplicate NuGet packages: `JoySoftware.NetDaemon.Extensions.Mqtt` (v23.44.0) + `NetDaemon.Extensions.Mqtt` (v25.46.0)
- Old package should be removed (package was renamed, old version no longer needed)
- Type ambiguity risk if both packages loaded

**Configuration/Namespacing:**
- UnifiApp uses lowercase namespace `HomeAutomations.apps.UnifiApp` (violates convention)
- All other apps use uppercase `HomeAutomations.Apps.*`
- Needs standardization to uppercase throughout

**For Future Deployments:**
- Verify all Rx leaks fixed before release (test app restart scenarios)
- Verify docker-compose.yml dependency is resolved
- Update package references to remove old MQTT package version

