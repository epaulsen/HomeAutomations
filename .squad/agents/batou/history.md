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

### Code Review — 2025-07-11

**Files reviewed (28 source files, excludes obj/):**
- `apps/CostSensor/`: CostSensorApp.cs, CostSensor.cs, PriceSensor.cs, CronScheduleTypeConverter.cs
- `apps/NordPoolApp/`: NordPoolSensorApp.cs, NordPoolSensor.cs, NordPoolSubsidizedSensor.cs
- `apps/UnifiApp/`: DeviceTrackerApp.cs, NetworkDeviceTrackerApp.cs, UnifiAppBase.cs, DeviceTracker.cs, VlanDeviceCountSensor.cs
- `Extensions/`: ComparableExtensions.cs, ConfigFolder.cs, CustomLoggingProvider.cs
- `Hosts/`: NordPoolBackgroundService.cs, UnifiBackgroundService.cs, UnifiData.cs
- `Models/`: CostSensorConfig.cs, CostSensorEntry.cs, CronSchedule.cs, ElectricityPrice.cs, HealthCheckResponse.cs, NordpoolData.cs, NordPoolDataStorage.cs, UnifiConfig.cs, UnifiDtos.cs, UnifiYamlConfig.cs
- `Services/`: INordpoolApiClient.cs, NordpoolApiClient.cs, NoSslValidationHandler.cs, UnifiHttpClient.cs
- `Utils/`: AzyncLazy.cs
- `Program.cs`

**Patterns found:**
- **Rx subscription leaking** is the #1 recurring critical issue — 4 separate `SubscribeAsync` calls have their `IDisposable` return value discarded (NordPoolSensor, NordPoolSubsidizedSensor, DeviceTrackerApp, NetworkDeviceTrackerApp)
- **Namespace inconsistency**: All UnifiApp files use `HomeAutomations.apps.UnifiApp` (lowercase `apps`) instead of `HomeAutomations.Apps.UnifiApp`
- **Dead code pattern**: `existingState` / `state` variables are fetched from HA state then never used (NordPoolSensor, NordPoolSubsidizedSensor, DeviceTracker)
- **`ConfigFolder` underused**: `CostSensorApp.GetConfigurationPath()` re-implements the same logic already in `ConfigFolder.Path`
- **`NordPoolDataStorage.CurrentPrice`** exposes `Subject<T>` directly instead of `IObservable<T>` — breaks encapsulation
- **`Environment.Exit(1)`** used in `CostSensorApp` instead of `IHostApplicationLifetime.StopApplication()`
- **Static mutable cache** `_siteId` in `UnifiHttpClient` is problematic for a transient/scoped HTTP client
- `NoSslValidationHandler.cs` is an empty file stub — SSL bypass is done with an inline lambda in Program.cs instead
- `AzyncLazy.cs` has a filename typo (should be `AsyncLazy.cs`) and no namespace declaration
- CostSensor sub-domain has the best code quality: consistent error handling, InvariantCulture parsing, subscription management all done correctly


### Full Codebase Code Review — 2026-03-22

**Session:** Parallel review with Motoko (architecture) and Ishikawa (tests)  
**Key Findings:**

**Critical (8) — Rx Subscription Leaks:**
- NordPoolSensor.cs:48, NordPoolSubsidizedSensor.cs:34, DeviceTrackerApp.cs:36, NetworkDeviceTrackerApp.cs:35 all discard `SubscribeAsync()` return values
- Fix: Store as `IDisposable` fields, dispose in `IDisposable` implementation

**Critical (8) — Null/Dictionary Crashes:**
- VlanDeviceCountSensor.cs:39 — `IPNetwork2.Parse(d.IpAddress)` crashes on null IP (string?)
- NordPoolSensor.cs:57 — `ma.EntryPerArea["NO2"]` crashes if key missing
- CostSensorApp.cs:159 — `Environment.Exit(1)` bypasses graceful shutdown

**Critical (8) — Static Mutable State:**
- UnifiHttpClient.cs:14 — Static `_siteId` on transient HTTP client breaks DI scoping and test isolation

**Key Warnings (20):**
- Namespace casing: UnifiApp uses lowercase `apps` (violates convention)
- Dead code: Unused `existingState` variables (NordPoolSensor, NordPoolSubsidizedSensor)
- Field naming: DeviceTracker missing `_` prefix convention
- Type leakage: NordPoolDataStorage exposes Subject<T> not IObservable<T>
- Dead imports: Multiple files have unused usings
- Exception handling: DeviceTracker SetState/CreateAsync calls lack try/catch
- Magic strings: "home"/"not_home" inlined in three places

**Suggestions (9):**
- SSL handler stub should be implemented instead of inline
- Subsidy threshold should use named constants
- DateTime.Now vs UtcNow inconsistency
- OrderBy before HashSet is wasted
- Unused models should be deleted

**Cross-Project Patterns Found:**
- Rx leaks appear in 4+ files — systemic subscription lifecycle issue
- DeviceTracker is the model for good exception handling (consistent try/catch pattern)
- CostSensor subdomain has better code quality than NordPool/Unifi
- CronScheduleTypeConverter used in YAML deserialization but ConvertFrom() untested

**Recommendations:**
1. Fix subscription leaks immediately (impact: silent feature degradation on app restart)
2. Guard null accesses (IP parsing, dictionary lookups)
3. Normalize UnifiApp namespace to uppercase
4. Replace Environment.Exit(1) with IHostApplicationLifetime
5. Remove static mutable cache pattern

