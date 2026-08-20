# HeaterEnhancement Orphan HeatSystem Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent plants outside every registered heater range from retaining `HeatSystem` and growing during winter.

**Architecture:** Extend the pure coverage registry with an any-cell query, then audit live plants from the existing throttled runtime coordinator. Keep original heater Buff application, wall blocking, power behavior, and save schema unchanged.

**Tech Stack:** C# 7.3, .NET Framework 4.7.2, BepInEx 5 Mono, Harmony 2.9.0, xUnit, Mono.Cecil, PowerShell.

## Global Constraints

- Plugin GUID remains `cn.ratopia.heaterenhancement` and display name remains `加热器加强优化`.
- Release version is `0.1.3` in BepInPlugin metadata, assembly metadata, README, scripts, tests, and ZIP name.
- Do not add custom save fields or persist derived `HeatSystem` state.
- Do not patch plant growth coroutines or global `WorldObject.AddBuff`/`ResumeGrowing` behavior.
- Preserve existing heater geometry, wall blocking, electricity rules, and package whitelist.
- Do not start Ratopia; install only after confirming the process is closed.

---

### Task 1: Enforce live HeatSystem coverage invariants

**Files:**
- Modify: `src/HeaterEnhancement/Core/HeaterCoverageRegistry.cs`
- Modify: `src/HeaterEnhancement/Runtime/HeaterRuntime.cs`
- Modify: `tests/HeaterEnhancement.Tests/HeaterCoverageRegistryTests.cs`
- Modify: `tests/HeaterEnhancement.Tests/PluginContractTests.cs`
- Modify: `tests/HeaterEnhancement.Tests/GameContractTests.cs`
- Modify: `src/HeaterEnhancement/Plugin.cs`
- Modify: `src/HeaterEnhancement/HeaterEnhancement.csproj`
- Modify: `tests/HeaterEnhancement.Tests/PackagingContractTests.cs`
- Modify: `scripts/Package.ps1`
- Modify: `scripts/Install.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: `HeaterCoverageRegistry.Covers(GridPoint)`, `EnvironmentMgr.List_WorldObj`, `WorldObject.GetSizeRect()`, `WorldObject.RemoveBuff(string)`.
- Produces: `bool HeaterCoverageRegistry.CoversAny(IEnumerable<GridPoint> points)` and private runtime audit `RemoveOrphanedHeatSystemBuffs(SeasonState season, EnvironmentMgr environmentManager)`.

- [ ] **Step 1: Write failing pure-logic and assembly contract tests**

Add tests proving `CoversAny` is false for null/empty/all-outside points and true when any occupied point is covered. Add Mono.Cecil assertions that the runtime audit exists, reads `EnvironmentMgr.List_WorldObj`, calls `WorldObject.GetSizeRect`, calls `HeaterCoverageRegistry.CoversAny`, calls `WorldObject.RemoveBuff`, and is invoked from `TickSafely`. Update release-contract expectations to `0.1.3`.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test .\HeaterEnhancement.sln -c Release `
  /p:RatopiaDir='E:\steam\steamapps\common\Ratopia' `
  /p:InstallAfterBuild=false
```

Expected: FAIL because `CoversAny` and `RemoveOrphanedHeatSystemBuffs` do not exist and release metadata is still `0.1.2`.

- [ ] **Step 3: Implement the minimal runtime audit**

Implement `CoversAny` by returning true on the first registered point. In `TickSafely`, after heater and season synchronization, iterate live plants containing `HeatSystem`; remove it when the season is not winter or none of `GetSizeRect()` is covered. Convert each occupied `Vector2` to a `GridPoint`; log only a positive removal count. Call the same audit after explicit season-state reapplication.

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
