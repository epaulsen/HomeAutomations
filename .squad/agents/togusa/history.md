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

### UnifiApp Code Review Fixes — Applied 2026-03-22

**Files changed:**

**Namespace fix (W1):** All 5 UnifiApp `.cs` files renamed namespace from `HomeAutomations.apps.UnifiApp` → `HomeAutomations.Apps.UnifiApp`. `Program.cs` using directive updated to match. **Key implication:** any test file referencing the old namespace also needs updating — `DeviceTrackerTests.cs` had this and was fixed.

**Subscription Lifecycle (C4/C5):** `DeviceTrackerApp` and `NetworkDeviceTrackerApp` now implement `IDisposable`. The return value of `data.ClientDevices.SubscribeAsync(...)` is stored in `private IDisposable? _subscription` and disposed in `Dispose()`. Pattern is identical to `CostSensor` — use that as the canonical reference.

**Dead Code Removal (W3/W20):**  
- `DeviceTracker.cs`: removed `state` field (set from HA but never read after init)
- `DeviceTracker.cs`: removed `context` constructor parameter (only used to read `state`, which was dead code). Callers (`DeviceTrackerApp`) updated.
- `VlanDeviceCountSensor.cs`: removed `_currentCount` field (set but never read). Removed `context` constructor parameter and `IHaContext` using. Callers (`NetworkDeviceTrackerApp`) updated.
- **Cascading effect:** When you remove dead code that uses an injected dependency, check if the dependency parameter itself becomes unused — and propagate the removal up to all callers.

**Null Guard (C6):** `VlanDeviceCountSensor.UpdateCountAsync` now skips devices with null/empty `IpAddress` and logs a warning instead of crashing with `ArgumentNullException` in `IPNetwork2.Parse`.

**Static Cache Bug (C7):** `UnifiHttpClient._siteId` changed from `static` to instance field. `GetSiteId` changed from `static` method (taking `HttpClient` + `ILogger` as params) to instance method. This ensures a new HTTP client instance gets a fresh site ID after controller restarts, rather than sharing stale state across all instances.

**CancellationToken Forwarding (W13):** Both `GetFromJsonAsync` calls in `UnifiHttpClient` now receive `cancellationToken`. Enables proper cooperative cancellation on shutdown.

**IComparable Contract Fix (W7):** `ClientDevice.CompareTo` now compares all 6 fields (Id, Name, IpAddress, Type, MacAddress, Access.Type) instead of only `Id`. Ensures `CompareTo == 0` iff `Equals == true`. `GetHashCode` updated to include `Access.Type` for consistency with `Equals`.

**Norwegian Comments (W8):** Two Norwegian comments in `UnifiDtos.cs` translated to English.

**AccessInfo Self-Comparer Fix (W18):** `AccessInfo` no longer implements `IEqualityComparer<AccessInfo>` (anti-pattern — class as its own comparer). Now implements only `IEquatable<AccessInfo>` with a proper `override GetHashCode()`.

**Field Naming (W2):** `DeviceTracker` fields renamed: `state` → removed (dead code), `lastSeenTime` → `_lastSeenTime`.

**Magic Strings → Constants (W17):** `DeviceTracker` now has `private const string StateHome = "home"` and `private const string StateNotHome = "not_home"`. All `SetStateAsync` calls use these constants.

**Error Handling (W11/W12):** `DeviceTracker.InitializeAsync` (CreateAsync) and `SetState` (SetStateAsync) now wrapped in try/catch. Errors logged with entity ID context.

**docker-compose.yml (C13):** Removed broken `depends_on: - homeassistant` block. The HA service is commented out — `depends_on` referencing it would cause `docker-compose up` to fail.

**NoSslValidationHandler.cs (S5):** Deleted empty file. SSL bypass is already inline in `Program.cs` as a lambda — no separate class needed.

**Pre-existing issue fixed (bonus):** `NordPoolDataStorage.cs` was missing `using System.Reactive.Linq;` for `AsObservable()`. Added. `CostSensorAppTests.cs` needed `IHostApplicationLifetime` mock added to `CostSensorApp` constructor calls (from Batou's `Environment.Exit` fix). Fixed.

**Build result:** ✅ Build passes, 31/31 tests pass.



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


---

## 2026-03-22 — NordPoolDataStorage using Fix

During UnifiApp namespace fixes, discovered that NordPoolDataStorage was missing `using System.Reactive.Linq` directive. This import is required for the `AsObservable()` extension method used by Batou's subscription lifecycle fix.

**Pattern established:** When exposing `IObservable<T>` via property accessor (`public IObservable<T> Property => _subject.AsObservable()`), ensure `System.Reactive.Linq` is imported. This is a common pattern in Rx-based systems.

