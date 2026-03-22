# Motoko — Project History

## Core Context

**Project:** HomeAutomations
**User:** Erling Paulsen
**Stack:** .NET 10 / C# / NetDaemon v4 / Docker
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

### 2025-07-17 — Full Codebase Review
- **Stack is now .NET 10**: Project targets `net10.0`, docs are stale (say 8/9). Updated history context.
- **Namespace inconsistency**: UnifiApp uses lowercase `apps` namespace while CostSensor/NordPool use uppercase `Apps`. Must normalize.
- **Subscription lifecycle is the #1 architectural gap**: NordPool and Unifi apps don't dispose Rx subscriptions. No systematic pattern for cleanup.
- **Timezone handling is inconsistent**: Mix of `DateTime.Now`, `DateTimeOffset.Now`, and explicit Norwegian timezone conversion. NordPoolDataStorage has a bug where HasPricesForToday uses local system time but stores with Norwegian timezone keys.
- **Thread safety gap in PriceSensor**: `_currentPrice` double is read/written across Rx threads without synchronization.
- **Dead code present**: `AsyncLazy<T>` (with filename typo `AzyncLazy.cs`), `ElectricityPrice`, `HealthCheckResponse`, `NoSslValidationHandler.cs` (empty file) — none are referenced.
- **Duplicate NuGet packages**: Both old `JoySoftware.NetDaemon.Extensions.Mqtt` and new `NetDaemon.Extensions.Mqtt` are referenced. The old one should be removed.
- **CostSensor subsystem is the best-structured part**: Good separation (PriceSensor, CostSensor, CostSensorApp, config models), proper dispose pattern, good logging. Use as template.
- **docker-compose.yml has broken `depends_on`** referencing commented-out service.
- **Tests are solid for DeviceTracker** (11 tests with good edge cases). CostSensor tests have some tautological assertions. IpTests.Serialize has no assertions.
- **`async void` in scheduler callbacks** is dangerous — used in both background services. Needs consistent try/catch wrapping.
- **Config path logic is duplicated** between `ConfigFolder.cs` and `CostSensorApp.GetConfigurationPath()`.
