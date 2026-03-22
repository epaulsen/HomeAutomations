# Squad Decisions — Code Review Session (2026-03-22)

## Overview

Full codebase review completed by Motoko (architecture), Batou (C# conventions), and Ishikawa (test coverage). Session produced 58 findings across 3 severity levels: 15 critical, 13 warning, 27 suggestion+minor.

This decision log consolidates all three reviews. For full details, see `/squad/orchestration-log/` entries.

---

## Architecture & Patterns (Motoko)

### Critical Issues (7)

#### C-01: NordPoolSensor — Unhandled KeyNotFoundException
**File:** `src/NetDaemon/apps/NordPoolApp/NordPoolSensor.cs:57`  
**Impact:** Runtime crash  
**Fix:** Use `TryGetValue()` instead of direct dictionary access

#### C-02: async void Scheduler Callbacks
**Files:** `src/NetDaemon/Hosts/NordPoolBackgroundService.cs:57`, `src/NetDaemon/Hosts/UnifiBackgroundService.cs:26`  
**Impact:** Unhandled exceptions crash entire process  
**Fix:** Wrap in try/catch or use `Func<Task>` if scheduler supports it

#### C-03: Thread Safety — _currentPrice Not Atomic
**File:** `src/NetDaemon/apps/CostSensor/PriceSensor.cs:16`  
**Impact:** Data corruption on multi-threaded access  
**Fix:** Use `volatile` or `Interlocked.Exchange`

#### C-04: Timezone Bug — HasPricesForToday/Tomorrow
**File:** `src/NetDaemon/Models/NordPoolDataStorage.cs:35-36, 43-44`  
**Impact:** Wrong date lookups if server TZ ≠ Europe/Oslo  
**Fix:** Use `TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone)`

#### C-05: docker-compose.yml — depends_on References Missing Service
**File:** `docker-compose.yml:19`  
**Impact:** `docker-compose up` fails  
**Fix:** Remove `depends_on` or uncomment homeassistant service

#### C-06: Namespace Inconsistency — apps vs Apps
**Files:** All `src/NetDaemon/apps/UnifiApp/` use lowercase; others use uppercase  
**Impact:** Convention violation, dev confusion  
**Fix:** Rename to `HomeAutomations.Apps.UnifiApp` (uppercase)

#### C-07: Duplicate NuGet Packages
**File:** `src/NetDaemon/HomeAutomations.csproj:15,20`  
**Issue:** Both `JoySoftware.NetDaemon.Extensions.Mqtt` (v23.44.0) and `NetDaemon.Extensions.Mqtt` (v25.46.0)  
**Impact:** Type ambiguity, bloated restore  
**Fix:** Remove old `JoySoftware.*` reference

### Warnings & Suggestions (27)
- Subscription lifecycle not managed (4 leaks)
- Configuration path duplication
- `Environment.Exit(1)` anti-pattern
- Unused models and dead code
- Mixed UTC/local time usage
- See full Motoko report for all 13 warnings and 14 suggestions

---

## C# Code Quality (Batou)

### Critical Issues (8)

#### C1-C5: Rx Subscription Leaks (Same as Motoko C-03 pattern, but 4 sites)
**Files:** `NordPoolSensor.cs:48`, `NordPoolSubsidizedSensor.cs:34`, `DeviceTrackerApp.cs:36`, `NetworkDeviceTrackerApp.cs:35`  
**Fix:** Store return from `SubscribeAsync()` as `IDisposable`, dispose in cleanup

#### C6: Null IP Address Crash
**File:** `src/NetDaemon/apps/UnifiApp/VlanDeviceCountSensor.cs:39`  
**Issue:** `IPNetwork2.Parse(d.IpAddress)` with nullable `IpAddress`  
**Fix:** Filter: `devices.Where(d => !string.IsNullOrEmpty(d.IpAddress))`

#### C7: Static Mutable Cache (Scoping Bug)
**File:** `src/NetDaemon/Services/UnifiHttpClient.cs:14`  
**Issue:** `private static Guid? _siteId` on transient class  
**Fix:** Make non-static or move to singleton

#### C8: Environment.Exit(1)
**File:** `src/NetDaemon/apps/CostSensor/CostSensorApp.cs:159`  
**Fix:** Use `IHostApplicationLifetime.StopApplication()` or throw exception

### Warnings & Suggestions (29)
- Namespace casing across UnifiApp
- Field naming convention violations
- Dead code (unused variables)
- Type exposure (Subject<T> instead of IObservable<T>)
- Missing initialization logging
- See full Batou report for all 20 warnings and 9 suggestions

---

## Test Coverage & Quality (Ishikawa)

### Critical Coverage Gaps (6)

#### C1: IpTests.Serialize — False Positive
**File:** `tests/HomeAutomations.Tests/IpTests.cs`  
**Issue:** Calls `Debug.WriteLine()` but asserts nothing  
**Fix:** Add assertions or delete test

#### C2-C3: NordPool Subsystem Untested
- `NordPoolSubsidizedSensor.ComputeSubsidizedPrice()` — Complex formula, no tests
- `NordPoolDataStorage` — Time-zone logic, averaging, VAT, no tests
**Fix:** Add unit tests for subsidy thresholds and time-zone edge cases

#### C4: VlanDeviceCountSensor Null IP Crash (Same as Batou C6)

#### C5-C6: CostSensor Calculation Engines Untested
- `CostSensor` class — Spike detection, cost increment, reset logic untested
- `PriceSensor` — Null state fallback, non-numeric parsing untested
**Fix:** Add tests for state changes, null handling, parse failures

### Warnings & Suggestions (9)
- Spike detection tests test only pure math, not production code
- File-system coupling in `CostSensorAppTests`
- Dead code in tests (`CronSchedule_ShouldHaveExpectedValues`)
- Missing logging verification
- Unused models not tested
- See full Ishikawa report for all 5 warnings and 4 suggestions

---

## Consolidated Severity Breakdown

| Severity | Motoko | Batou | Ishikawa | Total |
|----------|--------|-------|----------|-------|
| 🔴 CRITICAL | 7 | 8 | 6 | **21** |
| 🟡 WARNING | 13 | 20 | 5 | **38** |
| 🟢 SUGGESTION | 14 | 9 | 4 | **27** |
| **Total** | **34** | **37** | **15** | **86** |

---

## Decision: Action Items (Priority 1 — Blocking Issues)

1. ✅ **Fix Rx Leaks** — Store subscriptions, dispose on shutdown (Motoko C-03, Batou C1–C5)
2. ✅ **Guard Null Access** — Null IP in VLAN sensor, missing key in NordPool (Batou C6, Motoko C-01)
3. ✅ **Replace Environment.Exit(1)** — Use `IHostApplicationLifetime` (Batou C8)
4. ✅ **Normalize Namespaces** — Rename to `HomeAutomations.Apps` (Motoko C-06)
5. ✅ **Dedup NuGet Packages** — Remove old MQTT package (Motoko C-07)

## Decision: Action Items (Priority 2 — Functional Bugs)

6. Thread safety for `_currentPrice` (volatile or Interlocked)
7. Timezone handling — Use UTC consistently (Motoko C-04)
8. Fix `docker-compose.yml` dependency (Motoko C-05)
9. Remove `async void` anti-pattern (Motoko C-02)

## Decision: Action Items (Priority 3 — Code Quality)

10. Add NordPool tests (subsidy formula, time-zone logic)
11. Add CostSensor inner-class tests
12. Fix test false positives (IpTests, CronSchedule)
13. Remove dead code and unused models

---

## Notes

- Codebase architecture is sound; issues are mostly implementation-level
- DeviceTracker tests are a good model for the rest of the suite
- Highest risk: NordPool and CostSensor internals (entirely/partially untested + active bugs)
- Strong recommendation: Fix subscription leaks and null guards before next release

