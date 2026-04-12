# Implementation Plan: Colony Admin Summary

## Overview

Implement a per-colony status report on the Administration tab of FormColony. A new static service `ColonyAdminReportBuilder` collects activity/inactivity data, aggregates mining and refining rows, computes batch completion times for manufacturing, and renders an RTF string via `RtfBuilder`. The report is displayed in a `RichTextBox` that refreshes on a 60-second timer, colony selection change, and data change events.

## Tasks

- [ ] 1. Create ColonyAdminReportBuilder service with core structure
  - [ ] 1.1 Create `OE2EmpireTracker/Services/ColonyAdminReportBuilder.cs` with the `BuildReport(Colony, PlayerContext)` static method
    - Return empty string for null colony or empty UUID
    - Call `ColonyActivityCollector.CollectActivities` and `ColonyInactivityCollector.CollectInactivities` with a single-colony list
    - Partition activity rows into Building, CommodityRequest, and remaining activity types
    - Partition inactivity rows by ActivityType into sub-groups
    - Build RTF via `RtfBuilder` in section order: Building → Commodity Requests → Inactivity → Activity
    - Use distinct colors for section headers, countdown values, and completion times
    - Add `Compile Include` entry for `Services\ColonyAdminReportBuilder.cs` in `OE2EmpireTracker.csproj`
    - _Requirements: 1.1, 1.3, 2.1, 2a.1, 3.1, 4.1, 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ] 1.2 Implement Building section rendering
    - Sort building rows by `GetSecondsRemaining()` ascending (soonest first)
    - Each row displays source name, relative countdown, and local timezone time (`CountDown.EndTime.ToLocalTime().ToString("HH:mm ddd")`)
    - Omit section header when no building rows exist
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ] 1.3 Implement Commodity Requests section rendering
    - Display commodity name, requested quantity, and NeedBy date if set
    - Omit section when no unfulfilled commodity requests exist
    - _Requirements: 2a.1, 2a.2, 2a.3_

  - [ ] 1.4 Implement Inactivity section rendering
    - Render groups in fixed order: Colony Import Staleness, Idle Mining, Idle Refining, Idle Manufacturing, Idle Commodity Manufacturing, Idle Research, Underutilized Refining
    - Each group has a colored header; each row shows source name and process details
    - Omit groups with zero rows; omit entire section if all groups empty
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ] 1.5 Implement Activity section rendering — non-repeating rows (Manufacturing, CommodityManufacturing, Research)
    - Sort by seconds remaining ascending
    - Display source name, process details, relative countdown, and local timezone time
    - Exclude Building and CommodityRequest rows from this section
    - _Requirements: 4.1, 4.2, 4.3, 4.5, 4.9_

- [ ] 2. Implement batch completion time calculation for manufacturing
  - [ ] 2.1 Iterate colony structures directly for Manufacturing and CommodityManufacturing with `ManufacturingQuantity > 1`
    - Compute `remaining_cycles = ManufacturingQuantity - ManufacturingCompleted - 1`
    - Compute `batch_seconds = remaining_cycles * RepeatIntervalSeconds + current_cycle_remaining`
    - Display both next-item completion (countdown + local time) and full-batch completion (countdown + local time)
    - Skip batch line when `ManufacturingQuantity == 1` or on last cycle (`ManufacturingCompleted == ManufacturingQuantity - 1`)
    - Handle `RepeatIntervalSeconds == 0` as single-item (no batch calculation)
    - _Requirements: 4.4, 4.9_

  - [ ]* 2.2 Write property test for batch completion (Property 8)
    - **Property 8: Multi-quantity manufacturing shows next-item and batch completion**
    - Generate manufacturing structures with random quantities/completed/intervals, verify both times present and batch math correct
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderBatchPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 4.4**

- [ ] 3. Implement mining and refining aggregation
  - [ ] 3.1 Implement mining aggregation in the report builder
    - Iterate active miners on the colony, read survey data, compute rate per miner using `SurveyResource.Amount × (1.0 + ExtractionFocus × 0.01)`
    - Group by `(Resource, Purity)`, sum rates into one summary row per group
    - Display: `"Resource (Purity) — {totalRate}/h"`
    - _Requirements: 4.6_

  - [ ] 3.2 Implement refining aggregation in the report builder
    - Iterate active refiners, look up `RefiningRecipes.FindByInput` for synthetic recipes or use `GameConstants.RefiningBaseRate` with purity multiplier for normal refining
    - Group by `(InputResource, InputPurity)`, sum consume and produce rates
    - Display: `"{count}x Resource (Purity) — {totalConsume}:{totalProduce} OutputResource"`
    - For synthetic recipes, show the output resource name from the recipe
    - _Requirements: 4.7_

  - [ ]* 3.3 Write property test for mining aggregation (Property 9)
    - **Property 9: Mining aggregation — one row per resource+purity**
    - Generate colonies with multiple miners on same resource, verify single row with summed rate
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderMiningPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 4.6**

  - [ ]* 3.4 Write property test for refining aggregation (Property 10)
    - **Property 10: Refining aggregation — one row per resource+purity**
    - Generate colonies with multiple refiners on same resource+purity, verify single row with summed rates
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderRefiningPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 4.7**

- [ ] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Wire up FormColony UI — RichTextBox and Timer on Administration tab
  - [ ] 5.1 Add `RichTextBox rtbAdminReport` and `Timer timerAdminRefresh` to FormColony
    - Add controls programmatically in the constructor (after `InitializeComponent`)
    - `rtbAdminReport`: ReadOnly, Dock=Fill or sized dynamically, placed inside `flowLayoutPanel4` after `flowLayoutPanel3`
    - `timerAdminRefresh`: Interval=60000, started in constructor
    - Change `flowLayoutPanel4` to `Dock=Fill`, `FlowDirection=TopDown`, `WrapContents=false`
    - Size `rtbAdminReport` dynamically in a Layout handler to fill remaining vertical space after the button row
    - _Requirements: 1.2, 8.1, 8.2, 8.3_

  - [ ] 5.2 Implement `RefreshAdminReport()` and wire refresh triggers
    - `RefreshAdminReport()`: if no selected colony or empty UUID, set `rtbAdminReport.Rtf = ""`; otherwise call `ColonyAdminReportBuilder.BuildReport` and assign result
    - Wire to `lvwColonies_ItemSelectionChanged` → call `RefreshAdminReport()`
    - Wire to `OnColonyDataChanged` → if selected colony matches, call `RefreshAdminReport()`
    - Wire to `OnCurrentPlayerChanged` → clear `rtbAdminReport`
    - Wire to `timerAdminRefresh.Tick` → call `RefreshAdminReport()`
    - Guard timer tick with `IsDisposed` check
    - Stop/dispose timer in `Dispose` override
    - Wrap `BuildReport` call in try/catch, log exceptions via NLog, leave report unchanged on error
    - _Requirements: 1.1, 1.4, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3_

- [ ] 6. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Write property tests for report structure and ordering
  - [ ]* 7.1 Write property test for section ordering (Property 1)
    - **Property 1: Report section ordering**
    - Generate colonies with random mixes of building/idle/active structures, verify Building section appears before Commodity Requests, before Inactivity, before Activity in RTF output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderOrderPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 2.1, 2a.1, 3.1, 4.1**

  - [ ]* 7.2 Write property test for completion time dual display (Property 2)
    - **Property 2: Completion time dual display**
    - Generate random non-repeating CountDownTime values, verify output contains both countdown string and local time string
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderTimePropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 2.2, 4.5, 4.10**

  - [ ]* 7.3 Write property test for building sort order (Property 3)
    - **Property 3: Building rows sorted by soonest completion**
    - Generate colonies with multiple building structures at random times, verify ascending order in output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderBuildSortPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 2.3**

  - [ ]* 7.4 Write property test for inactivity group ordering (Property 4)
    - **Property 4: Inactivity group ordering**
    - Generate colonies with random idle structure types, verify group header order in output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderInactivityOrderPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 3.2**

  - [ ]* 7.5 Write property test for inactivity content (Property 5)
    - **Property 5: Inactivity rows contain header and details**
    - Generate inactivity rows, verify headers and row content present in output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderInactivityContentPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 3.3, 3.4**

  - [ ]* 7.6 Write property test for activity excludes building (Property 6)
    - **Property 6: Activity section excludes building and commodity request rows**
    - Generate colonies with building + active structures, verify no building text in activity section
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderExcludePropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 4.2**

  - [ ]* 7.7 Write property test for activity sort order (Property 7)
    - **Property 7: Non-repeating activity rows sorted by soonest completion**
    - Generate colonies with multiple non-repeating active processes, verify ascending order in output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderActivitySortPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 4.3**

  - [ ]* 7.8 Write property test for commodity request content (Property 11)
    - **Property 11: Commodity request rows contain name and quantity**
    - Generate random commodity requests, verify name and quantity in output
    - File: `OE2EmpireTracker.Tests/Services/ColonyAdminReportBuilderCommodityPropertyTests.cs`
    - Add `Compile Include` entry in test csproj
    - **Validates: Requirements 2a.2**

- [ ] 8. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests use FsCheck 2.16.6 with FsCheck.NUnit adapter (already in test project)
- New `.cs` files require `Compile Include` entries in the old-style csproj files
- The report builder does its own structure iteration for manufacturing batch calcs and mining/refining aggregation (doesn't rely solely on collector output)
- Checkpoints ensure incremental validation
