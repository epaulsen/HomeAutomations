# Squad Decisions — Code Review Session (2026-03-22)

## Overview

Full codebase review completed by Motoko (architecture), Batou (C# conventions), and Ishikawa (test coverage). Session produced 86 findings across 3 severity levels: 21 critical, 38 warning, 27 suggestion+minor.

This decision log consolidates all three reviews with action items and their completion status. For full orchestration details, see `/squad/orchestration-log/` entries.

---

## Architecture & Patterns (Motoko)

### Critical Issues (7) — ✅ ALL RESOLVED

#### C-01: NordPoolSensor — Unhandled KeyNotFoundException
**File:** `src/NetDaemon/apps/NordPoolApp/NordPoolSensor.cs:57`  
**Impact:** Runtime crash  
**Fix Applied:** Use `TryGetValue()` instead of direct dictionary access — ✅ Batou

#### C-02: async void Scheduler Callbacks
**Files:** `src/NetDaemon/Hosts/NordPoolBackgroundService.cs:57`, `src/NetDaemon/Hosts/UnifiBackgroundService.cs:26`  
**Impact:** Unhandled exceptions crash entire process  
**Fix Applied:** Wrapped in fire-and-forget with error logging — ✅ Batou

#### C-03: Thread Safety — _currentPrice Not Atomic
**File:** `src/NetDaemon/apps/CostSensor/PriceSensor.cs:16`  
**Impact:** Data corruption on multi-threaded access  
**Fix Applied:** Use `Volatile.Read/Write` pattern — ✅ Batou

#### C-04: Timezone Bug — HasPricesForToday/Tomorrow
**File:** `src/NetDaemon/Models/NordPoolDataStorage.cs:35-36, 43-44`  
**Impact:** Wrong date lookups if server TZ ≠ Europe/Oslo  
**Fix Applied:** Use `TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, NorwegianTimeZone)` — ✅ Batou

#### C-05: docker-compose.yml — depends_on References Missing Service
**File:** `docker-compose.yml:19`  
**Impact:** `docker-compose up` fails  
**Fix Applied:** Remove `depends_on` or uncomment homeassistant service — ✅ Togusa

#### C-06: Namespace Inconsistency — apps vs Apps
**Files:** All `src/NetDaemon/apps/UnifiApp/` use lowercase; others use uppercase  
**Impact:** Convention violation, dev confusion  
**Fix Applied:** Rename to `HomeAutomations.Apps.UnifiApp` (uppercase) — ✅ Togusa

#### C-07: Duplicate NuGet Packages
**File:** `src/NetDaemon/HomeAutomations.csproj:15,20`  
**Issue:** Both `JoySoftware.NetDaemon.Extensions.Mqtt` (v23.44.0) and `NetDaemon.Extensions.Mqtt` (v25.46.0)  
**Impact:** Type ambiguity, bloated restore  
**Fix Applied:** Remove old `JoySoftware.*` reference — ✅ Batou

### C# Code Quality (Batou)

### Critical Issues (8) — ✅ ALL RESOLVED

#### C1-C5: Rx Subscription Leaks (4 sites)
**Files:** `NordPoolSensor.cs:48`, `NordPoolSubsidizedSensor.cs:34`, `DeviceTrackerApp.cs:36`, `NetworkDeviceTrackerApp.cs:35`  
**Fix Applied:** Store return from `SubscribeAsync()` as `IDisposable`, dispose in cleanup — ✅ Batou, Togusa

#### C6: Null IP Address Crash
**File:** `src/NetDaemon/apps/UnifiApp/VlanDeviceCountSensor.cs:39`  
**Issue:** `IPNetwork2.Parse(d.IpAddress)` with nullable `IpAddress`  
**Fix Applied:** Filter: `devices.Where(d => !string.IsNullOrEmpty(d.IpAddress))` with logging — ✅ Togusa

#### C7: Static Mutable Cache (Scoping Bug)
**File:** `src/NetDaemon/Services/UnifiHttpClient.cs:14`  
**Issue:** `private static Guid? _siteId` on transient class  
**Fix Applied:** Make non-static (instance field) — ✅ Togusa

#### C8: Environment.Exit(1)
**File:** `src/NetDaemon/apps/CostSensor/CostSensorApp.cs:159`  
**Fix Applied:** Use `IHostApplicationLifetime.StopApplication()` — ✅ Batou

### Test Coverage & Quality (Ishikawa)

### Critical Coverage Gaps (6) — ✅ ALL RESOLVED

#### C1: IpTests.Serialize — False Positive
**File:** `tests/HomeAutomations.Tests/IpTests.cs`  
**Issue:** Calls `Debug.WriteLine()` but asserts nothing  
**Fix Applied:** Add 6 real assertions + deserialization round-trip — ✅ Ishikawa

#### C2-C3: NordPool Subsystem Untested
- `NordPoolSubsidizedSensor.ComputeSubsidizedPrice()` — Complex formula, no tests
- `NordPoolDataStorage` — Time-zone logic, averaging, VAT, no tests
**Fix Applied:** Add 8+8 unit tests for subsidy thresholds, time-zone edge cases, VAT — ✅ Ishikawa

#### C4: VlanDeviceCountSensor Null IP Crash (Same as Batou C6) — ✅ Fixed

#### C5-C6: CostSensor Calculation Engines Untested
- `CostSensor` class — Spike detection, cost increment, reset logic untested
- `PriceSensor` — Null state fallback, non-numeric parsing untested
**Fix Applied:** Add 12 unit tests for state changes, null handling, spike detection — ✅ Ishikawa

---

## Consolidated Summary

| Category | Critical | Warning | Suggestion | Total |
|----------|----------|---------|-----------|-------|
| **Motoko** | 7 | 13 | 14 | 34 |
| **Batou** | 8 | 20 | 9 | 37 |
| **Ishikawa** | 6 | 5 | 4 | 15 |
| **TOTAL** | **21** | **38** | **27** | **86** |

**STATUS:** ✅ ALL CRITICAL ITEMS RESOLVED

---

## Decisions Applied

### Priority 1 — Blocking Issues ✅ COMPLETE
1. Fix Rx Leaks — Store subscriptions, dispose on shutdown
2. Guard Null Access — Null IP in VLAN sensor, missing key in NordPool
3. Replace Environment.Exit(1) — Use `IHostApplicationLifetime`
4. Normalize Namespaces — Rename to `HomeAutomations.Apps`
5. Dedup NuGet Packages — Remove old MQTT package

### Priority 2 — Functional Bugs ✅ COMPLETE
6. Thread safety for `_currentPrice` (volatile or Interlocked)
7. Timezone handling — Use UTC consistently
8. Fix `docker-compose.yml` dependency
9. Remove `async void` anti-pattern

### Priority 3 — Code Quality ✅ COMPLETE
10. Add NordPool tests (subsidy formula, time-zone logic)
11. Add CostSensor inner-class tests
12. Fix test false positives (IpTests, CronSchedule)
13. Remove dead code and unused models

---

## Implementation Timeline

**2026-03-22 Batch 1 (Batou + Togusa):**
- NordPool subsystem: 11 files, 24 fixes applied
- UnifiApp subsystem: 10 files, 18 fixes applied
- Dead files removed: 3 (AzyncLazy, ElectricityPrice, HealthCheckResponse)
- Docker config corrected

**2026-03-22 Batch 2 (Ishikawa):**
- Fixed 3 broken tests: 0 → 6 assertions, tautology fix, precision fix
- Added 28 new tests: 3 new test files, 59 total tests
- All tests passing: 59/59

**2026-03-22 Batch 3 (Motoko):**
- Full build validation: ✅ Clean (1 deferred warning)
- Test suite: ✅ 59/59 passing
- Approval: ✅ Ready to commit

---

## Outstanding Minor Items

- **CS9113 warning:** Unused `context` parameter in NordPoolSensor constructor
  - **Status:** Deferred (non-blocking, can be cleaned up in next pass)
  - **Impact:** Zero — parameter present but unused

---

## Recommendations

**APPROVED FOR PRODUCTION.** All critical findings resolved. Build clean. Test suite comprehensive (59/59 passing). Codebase ready for main branch integration.

**Future Work:**
- Consider `internal` + `InternalsVisibleTo` for private test methods (reflection-tested)
- Clean up deferred CS9113 warning in next maintenance pass
- Establish subscription lifecycle pattern in architecture docs

