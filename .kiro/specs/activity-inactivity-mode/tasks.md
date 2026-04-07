# Implementation Plan: Activity / Inactivity Mode

## Overview

Add an Inactivity Mode to the Colony Activity form that surfaces idle and underutilized production structures. A new `ColonyInactivityCollector` service class detects idle miners, refiners, research labs, manufactories, and commodity factories, plus underutilized refiners with warehouse stockpile exemption. The form toggles between Activity Mode and Inactivity Mode via a "Show Inactive" checkbox.

## Tasks

- [x] 1. Create ColonyInactivityCollector with idle structure detection
  - [x] 1.1 Create `OE2EmpireTracker/Services/ColonyInactivityCollector.cs` with the static class and `CollectInactivities(IEnumerable<Colony>, PlayerContext)` method
    - Implement `IsBuiltAndOnline(ColonyStructure)` helper — checks `Properties["Built"] == "True"` and `Properties["Online"] == "True"`
    - Implement `HasActiveProcess(ColonyStructure)` helper — checks `ProcessCompletionTime != null && TimeRemaining > 0`
    - Implement `BuildSourceName(ColonyStructure, PlayerContext)` helper — formats `"#{gameSequence} {blueprint.ExtendedName}"`
    - Implement `CollectIdleStructures(Colony, PlayerContext, List<ActivityRow>)` scanning all 5 production types (MiningRig, Refinery, ResearchLaboratory, Manufactory, CommodityFactory)
    - For each type, detect "no work item assigned" vs "work item assigned but no active timer" and set ProcessDetails accordingly ("No survey assigned" / "Idle", "No resource assigned" / "Idle", "No blueprint assigned" / "Idle", "No commodity assigned" / "Idle")
    - Skip structures where `FindBlueprint()` returns null (same as existing ColonyActivityCollector)
    - Add `<Compile Include="Services\ColonyInactivityCollector.cs" />` to `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.3, 8.4, 9.1, 9.2, 9.3, 9.4_

  - [x] 1.2 Write NUnit tests for idle structure detection
    - Test each of the 5 structure types: idle with no work item, idle with work item but no timer, active (should not appear), not built (should not appear), not online (should not appear)
    - Test ProcessDetails strings match spec ("No survey assigned", "Idle", "No resource assigned", etc.)
    - Test that non-production structure types are ignored
    - Test empty colony returns empty list
    - Test null blueprint skips structure silently
    - Create `OE2EmpireTracker.Tests/Services/ColonyInactivityCollectorTests.cs`
    - Add `<Compile Include="Services\ColonyInactivityCollectorTests.cs" />` to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - **Property 1: Idle structure detection**
    - **Property 2: Idle structure ProcessDetails correctness**
    - **Property 3: Inactivity row metadata format**
    - **Validates: Requirements 3.1–3.4, 4.1–4.4, 6.1–6.4, 7.1–7.4, 8.1–8.4, 9.1–9.4**

- [x] 2. Checkpoint — Ensure idle detection builds and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Add underutilized refiner detection to ColonyInactivityCollector
  - [x] 3.1 Implement underutilized refiner logic in `ColonyInactivityCollector`
    - Implement `GetMiningOutputRate(ColonyStructure, PlayerContext, Colony)` — reads `SurveyResource.Amount`, applies ExtractionFocus bonus `(1.0 + level * 0.01)`
    - Implement `GetRefiningConsumptionRate(ColonyStructure)` — returns `GameConstants.RefiningBaseRate` (25) for normal, `RefiningRecipe.ConsumeRate` for synthetic
    - Implement `GetWarehouseStockpile(Colony, string resource, string purity)` — returns quantity from `colony.Items.FindResource()`
    - Implement `CollectUnderutilizedRefiners(Colony, PlayerContext, List<ActivityRow>)`:
      - Per colony, group active refiners by resource+purity
      - Sum mining output rate per resource+purity across all active miners
      - Sum refining consumption rate per resource+purity across all active refiners
      - When total consumption > total mining output, flag excess refiners starting from highest `gameSequence`
      - Before flagging, check warehouse stockpile >= one cycle's consumption rate (exempt if sufficient)
      - Set ProcessDetails to `"Underutilized: {available}/{consumeRate} per cycle"`
    - Wire `CollectUnderutilizedRefiners` into `CollectInactivities` after `CollectIdleStructures`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 3.2 Write NUnit tests for underutilized refiner detection
    - Test: 1 miner (10/h Low) + 2 refiners (25/cycle each) — second refiner flagged as "Underutilized: 0/25 per cycle"
    - Test: same setup with 25+ units in warehouse — no underutilized flag (warehouse exemption)
    - Test: synthetic refiner with `RefiningRecipe.ConsumeRate` (1250) — verify correct rate comparison
    - Test: ExtractionFocus skill bonus applied correctly to mining output rate
    - Test: multiple resources in same colony handled independently
    - Test: all refiners have sufficient mining supply — none flagged
    - **Property 4: Underutilized refiner detection**
    - **Property 5: Warehouse stockpile exemption**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**

- [x] 4. Checkpoint — Ensure underutilized refiner logic builds and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Add chkShowInactive checkbox to FormColonyActivity UI
  - [x] 5.1 Modify `FormColonyActivity.Designer.cs` to add `chkShowInactive` checkbox
    - Declare `private System.Windows.Forms.CheckBox chkShowInactive;`
    - Add to `flpFilters.Controls` after `chkRefining` and before `txtFilter`
    - Set `Text = "Show Inactive"`, `AutoSize = true`, unchecked by default
    - _Requirements: 1.1_

  - [x] 5.2 Modify `FormColonyActivity.cs` to wire up Inactivity Mode toggle
    - Wire `chkShowInactive.CheckedChanged` handler in constructor
    - Modify `RefreshData()` to branch: unchecked calls `ColonyActivityCollector.CollectActivities()`, checked calls `ColonyInactivityCollector.CollectInactivities()`
    - Modify `ApplyFiltersAndPopulate()` to hide `chkCommodityRequest` and `colCountDown` when in Inactivity Mode, show them when in Activity Mode
    - Modify `GetSelectedActivityTypes()` to exclude `CommodityRequest` when in Inactivity Mode
    - Modify `timerRefresh_Tick` to skip countdown cell updates when in Inactivity Mode
    - _Requirements: 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 9.5, 10.1, 10.2_

- [x] 6. Final checkpoint — Ensure full solution builds and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- No FsCheck — all tests use plain NUnit (project uses packages.config, old-style csproj)
- Build with: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Test with: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
- Old-style csproj requires explicit `<Compile Include>` entries for new `.cs` files
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
