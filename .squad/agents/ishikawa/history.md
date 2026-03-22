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


### 2025-07-16 — Test Fixes and New Coverage

**Before:** 31 tests across 4 files.  
**After:** 59 tests across 7 files. All passing.

#### Fixed broken tests (3 changes)

1. **`IpTests.Serialize`** (`tests/HomeAutomations.Tests/IpTests.cs`)  
   - Removed `Debug.WriteLine(yaml)` (zero-assertion anti-pattern)  
   - Added 3 string-content assertions on serialized YAML (`name: Default`, `vlan: 192.168.1.0/24`, `mac_address: ...`)  
   - Added deserialization round-trip test with 3 property assertions  
   - Removed unused `System.Diagnostics` and `System.Net` imports

2. **`CronSchedule_ShouldHaveExpectedValues`** (`tests/HomeAutomations.Tests/CostSensorAppTests.cs`)  
   - Was: `var actual = expected; Assert.Equal(expected, actual)` — tested nothing  
   - Fixed to: `[InlineData(CronSchedule.None, 0)]` etc., asserting `(int)schedule == expectedIntValue`  
   - Now verifies the actual enum integer values (None=0, Daily=1, Monthly=2, Yearly=3)

3. **`EqualityTests.EqualToCopy`** (`tests/HomeAutomations.Tests/EqualityTests.cs`)  
   - Replaced `DateTime.Now` (precision risk in JSON round-trip) with `new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc)`

#### New test files (3 created)

**`tests/HomeAutomations.Tests/NordPoolApp/NordPoolSubsidizedSensorTests.cs`** — 8 tests  
- Tests `ComputeSubsidizedPrice` (private) via reflection  
- Covers: null input, zero price, negative price, below threshold (no subsidy), at threshold (boundary), above threshold (formula verification with precision), high price  
- Formula verified: `subsidy = 0.9375 + (price - 0.9375) * 0.1` for price >= 0.9375

**`tests/HomeAutomations.Tests/NordPoolApp/NordPoolDataStorageTests.cs`** — 8 tests  
- Mocks `INetDaemonScheduler` with `RunAt`/`RunEvery` returning `Mock.Of<IDisposable>()`  
- Covers: `HasPricesForToday` true/false, `HasPricesForTomorrow` false, duplicate date ignored, `CurrentHourlyPrice` returns entry, VAT formula (`/1000 * 1.25`), averaging across multiple entries in same hour, null when no prices stored, `CurrentPrice` observable emission
- Key pattern: uses `TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone)` + UTC DateTime for entries to match production filtering logic

**`tests/HomeAutomations.Tests/CostSensor/CostSensorCalculationTests.cs`** — 8 tests  
- Uses `Subject<StateChange>` + `Entity(haContext, entityId)` to emit real NetDaemon state changes  
- Covers: first state change skipped (null old value), cost calculation (`delta * tariff`), spike detection (large delta within 60s ignored), large delta on first-ever change processed normally (no prior time → no spike check), small delta within 60s not a spike, `ResetCostAsync` resets to "0.00" (via reflection), entity created when absent, entity NOT re-created when already in HA
- Key patterns: `SutComponents` record for clean test setup, reflection for `ResetCostAsync`, `CreateStateChange(haContext, old, new)` helper using positional `StateChange` constructor

#### Key technical learnings

- `StateChange` in NetDaemon v23 is a positional record: `StateChange(Entity entity, EntityState? old, EntityState? new)` — cannot use object initializer syntax
- `Entity.StateChanges()` filters by entity ID from `StateAllChanges()` — mocking `StateAllChanges()` is the correct approach
- `SubscribeAsync` is async — always add `Task.Delay(300+ms)` after emitting events before asserting
- `Mock<INetDaemonScheduler>` needs explicit `RunAt` and `RunEvery` setups returning `Mock.Of<IDisposable>()` or constructor throws
- Testing private methods via `BindingFlags.NonPublic | BindingFlags.Instance` reflection works well for pure functions with no external dependencies
- Backdating `DateTime` fields via reflection works but can race with async handlers — prefer testing boundary conditions that don't require timing manipulation

---

## 2026-03-22 — Reflection Patterns for Private Method Testing

Added comprehensive test suites for previously untested private methods using reflection:

**Pattern:** 
```csharp
var method = typeof(NordPoolSubsidizedSensor)
    .GetMethod("ComputeSubsidizedPrice", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
        null, new[] { typeof(double?) }, null);

var result = (double?)method.Invoke(instance, new object[] { price });
```

**Observations:**
- Private methods tested this way are good candidates for `internal` + `InternalsVisibleTo` in future refactors
- Reflection-based tests should document *why* a method is private (e.g., "internal implementation detail")
- Always test boundary conditions and null cases for private calculation methods

**Files using pattern:**
- NordPoolSubsidizedSensorTests.cs (ComputeSubsidizedPrice)
- CostSensorCalculationTests.cs (ResetCostAsync, spike detection state machine)

