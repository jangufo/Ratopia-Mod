# HeaterEnhancement Orphan HeatSystem Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent plants outside every registered heater range from retaining `HeatSystem` and growing during winter.

**Architecture:** Keep geometric and actual-effective coverage in separate registries. Derive effective cells from the original heater's completed wall calculation plus winter, activation, and electricity state; mark a live-plant audit dirty only when relevant lifecycle state can invalidate coverage. Keep original heater Buff application, growth coroutines, and save schema unchanged.

**Tech Stack:** C# 7.3, .NET Framework 4.7.2, BepInEx 5 Mono, Harmony 2.9.0, xUnit, Mono.Cecil, PowerShell.

## Global Constraints

- Plugin GUID remains `cn.ratopia.heaterenhancement` and display name remains `加热器加强优化`.
- Release version is `0.1.3` in BepInPlugin metadata, assembly metadata, README, scripts, tests, and ZIP name.
- Do not add custom save fields or persist derived `HeatSystem` state.
- Do not patch plant growth coroutines or global `WorldObject.AddBuff`/`ResumeGrowing` behavior.
- Preserve existing heater geometry, wall blocking, electricity rules, and package whitelist.
- Never treat geometric 9x5 inclusion alone as proof of active heating.
- Do not scan `EnvironmentMgr.List_WorldObj` on a fixed interval when no coverage-relevant state changed.
- Do not claim interception of arbitrary third-party `HeatSystem` writes that trigger no known heater lifecycle event; the guaranteed repair points are initial session synchronization and coverage-invalidating events.
- Do not start Ratopia; install only after confirming the process is closed.

---

### Task 1: Enforce live HeatSystem coverage invariants

**Files:**
- Modify: `src/HeaterEnhancement/Core/HeaterCoverageRegistry.cs`
- Create: `src/HeaterEnhancement/Core/HeaterEffectiveCoverageCalculator.cs`
- Modify: `src/HeaterEnhancement/Runtime/HeaterRuntime.cs`
- Modify: `src/HeaterEnhancement/Patches/RuntimePatches.cs`
- Modify: `tests/HeaterEnhancement.Tests/HeaterCoverageRegistryTests.cs`
- Create: `tests/HeaterEnhancement.Tests/HeaterEffectiveCoverageCalculatorTests.cs`
- Modify: `tests/HeaterEnhancement.Tests/PluginContractTests.cs`
- Modify: `tests/HeaterEnhancement.Tests/GameContractTests.cs`
- Modify: `src/HeaterEnhancement/Plugin.cs`
- Modify: `src/HeaterEnhancement/HeaterEnhancement.csproj`
- Modify: `tests/HeaterEnhancement.Tests/PackagingContractTests.cs`
- Modify: `scripts/Package.ps1`
- Modify: `scripts/Install.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: `Building_Heater.List_LocalPos`, private `Building_Heater.List_BlockPos`, `Building.m_Activation`, `Building.m_ElecNum`, `EnvironmentMgr.List_WorldObj`, `WorldObject.GetSizeRect()`, `WorldObject.RemoveBuff(string)`.
- Produces: `bool HeaterCoverageRegistry.CoversAny(IEnumerable<GridPoint> points)`, pure `HeaterEffectiveCoverageCalculator.Create(...)`, effective-coverage lifecycle hooks, and dirty-guarded private runtime audit `RemoveOrphanedHeatSystemBuffs(SeasonState season, EnvironmentMgr environmentManager)`.

- [ ] **Step 1: Write failing pure-logic and assembly contract tests**

Add tests proving `CoversAny` is false for null/empty/all-outside points and true when any occupied point is covered. Add pure tests proving effective coverage is empty outside winter, while inactive, or without electricity; in active powered winter it excludes blocked cells, preserves order, and supports overlapping registry entries. Add Mono.Cecil assertions for the real fields, lifecycle hooks, two distinct registries, dirty guard, live-object audit, manager-unavailable dirty retention, destroyed-object guards, every invalidation event, no mid-batch audit, and `0.1.3` release contracts.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test .\HeaterEnhancement.sln -c Release `
  /p:RatopiaDir='E:\steam\steamapps\common\Ratopia' `
  /p:InstallAfterBuild=false
```

Expected: FAIL because the effective-coverage calculator, lifecycle hooks, dirty-guarded audit, and `0.1.3` metadata do not exist.

- [ ] **Step 3: Implement actual effective coverage and the event-driven audit**

Implement `CoversAny` by returning true on the first registered point. Add a separate `EffectiveCoverage` registry. After a successful original `Building_Update`, register only cells allowed by winter, activation, power, and `List_BlockPos`; on failed wire checks, working stop, non-winter disable, demolition, missing heaters, or reset, unregister effective cells. These per-heater hooks only update state and dirty. Consume dirty once at the end of `TickSafely` after all heaters and missing IDs are synchronized, and once at the end of `ReapplyAllSeasonStates` after all tracked heaters are reapplied from the season/electricity-refresh Postfix. Iterate only live objects containing `HeatSystem`; remove it outside winter or when none of `GetSizeRect()` is effectively covered. Leave dirty set when managers are unavailable and log only a positive removal count.

- [ ] **Step 4: Update release metadata and documentation**

Set all release metadata and script expectations to `0.1.3`, set ZIP name to `加热器加强优化-v0.1.3-BepInEx5.zip`, and document live orphan-Buff cleanup without claiming game validation.

- [ ] **Step 5: Run focused and full verification**

Run the full test command from Step 2, then:

```powershell
dotnet build .\src\HeaterEnhancement\HeaterEnhancement.csproj -c Release `
  /p:RatopiaDir='E:\steam\steamapps\common\Ratopia' `
  /p:InstallAfterBuild=false
& .\scripts\Package.ps1 -RatopiaDir 'E:\steam\steamapps\common\Ratopia'
```

Expected: all tests pass, Release build has zero errors, output contains only `HeaterEnhancement.dll`, and package validation accepts exactly README plus the plugin DLL.

- [ ] **Step 6: Review, back up, and install**

Obtain independent spec and code-quality review. Confirm Ratopia process count is zero, run `Install.ps1`, then independently verify installed GUID/version/SHA-256, the `0.1.2` backup, no duplicate GUID, and no `.installing` residue. Do not start the game.
