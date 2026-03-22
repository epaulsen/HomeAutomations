# Ishikawa — Project History

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

### 2025-07-16 — Test Coverage Audit

**Baseline:** 31 tests, all passing. 4 test files.

**Coverage state:**
- `DeviceTracker` — well-tested, parametrized boundary tests, clean AAA structure. Best class in the suite.
- `CostSensorApp` — partially tested but coupled to `apps/config/cost_sensors.yaml` on disk (PreserveNewest copy). Tests are environment-dependent.
- `ClientDevice` equality — partially tested (happy path only, no negatives, no null).
- `IpTests.Serialize` — a non-test: no assertion, always passes, provides false confidence.

**Entirely untested classes (high risk):**
- `NordPoolSensor`, `NordPoolSubsidizedSensor`, `NordPoolSensorApp`, `NordPoolDataStorage`
- `CostSensor` (inner class — actual calculation engine, spike detection, cron reset)
- `PriceSensor` (tariff tracking — default to 0.0 on parse failure silently)
- `VlanDeviceCountSensor` — null IP address crash risk (`IpAddress` is nullable, no guard before `IPNetwork2.Parse`)
- `DeviceTrackerApp`, `NetworkDeviceTrackerApp`
- `ComparableExtensions.IsIdenticalTo`

**Key quality issues found:**
- `IpTests.Serialize` — no assertion (C1, critical false confidence)
- `SpikeDetection_*` tests — test inline math, not production code (C6)
- `CronSchedule_ShouldHaveExpectedValues` — asserts `expected == expected` (useless) (W1)
- `EqualityTests.EqualToCopy` — potential DateTime JSON precision issue, no negative cases (W3)
- Redundant Fact/Theory overlap in DeviceTrackerTests (W2)

**Recommended priority for next work:**
1. Fix `IpTests.Serialize` — add YAML content assertions or delete
2. `NordPoolSubsidizedSensor.ComputeSubsidizedPrice` tests — pure function, easy wins
3. `PriceSensor` tests — null/non-numeric state init + subscription
4. `CostSensor` state-change tests — actual spike detection, cost accumulation
5. Null guard in `VlanDeviceCountSensor.UpdateCountAsync` + tests

