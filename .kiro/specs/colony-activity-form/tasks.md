# Implementation Plan: Colony Activity Form

## Overview

Implement a read-only WinForms form that aggregates all active countdown timers and unfulfilled commodity requests across all colonies for the current player into a single sortable, filterable DataGridView. The core logic is extracted into a testable static helper class (`ColonyActivityCollector`), with the form handling display, filtering, and live countdown refresh.

## Tasks

- [-] 1. Create ColonyActivityCollector with ActivityType enum and ActivityRow POCO
  - [x] 1.1 Create `Baseline/ColonyActivityCollector.cs` with `ActivityType` enum, `ActivityRow` class, and `ColonyActivityCollector.CollectActivities` static method
    - Define `ActivityType` enum: Building, Manufacturing, CommodityManufacturing, CommodityRequest, Research, Mining, Refining
    - Define `ActivityRow` POCO with Type, SystemName, ColonyName, SourceName, ProcessDetails, CountDown, NeedBy properties
    - Implement `GetSecondsRemaining()`, `GetTimeRemainingString()`, and `FormatSeconds(long)` on ActivityRow
    - Implement `CollectActivities(IEnumerable<Colony>, PlayerContext)` that scans structures and commodity requests
    - For each structure: check BuildCompletionTime first (Building priority), then ProcessCompletionTime classified by BluePrintType
    - For each unfulfilled CommodityRequested: create CommodityRequest row with NeedBy-based countdown
    - Skip structures whose FlatpackBlueprintUUID cannot be resolved to a Blueprint
    - Source name format: `$"#{structure.gameSequence} {blueprint.ExtendedName}"` for structures, `"Commodity Request"` for commodities
    - Process details per type: Building→"Building", Mining→"{Amount}/h {Resource} ({Purity})", Refining→normal or synthetic format, Research→"Evo {n}->{n+1} {Name}", Manufacturing→"({completed}/{qty}) {ExtendedName}", CommodityManufacturing→"({completed}/{qty}) {CommodityName} x{CommoditiesPerCycle}", CommodityRequest→"{Name} x{Requested}"
    - Add `Compile Include` entry for `Baseline\ColonyActivityCollector.cs` in `OE2EmpireTracker.csproj`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8_

  - [x] 1.2 Write property test: Activity collection completeness and classification (Property 1)
    - **Property 1: Activity collection completeness and classification**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.5, 6.6**
    - Create `OE2EmpireTracker.Tests/Baseline/ColonyActivityCollectorTests.cs`
    - Add `Compile Include` entry in test `.csproj`
    - Generate random colonies with 0–5 structures each having random timer states and random blueprint types, plus 0–3 commodity requests with random Fulfilled states
    - Verify row count matches expected active timers + unfulfilled commodities, and each row's ActivityType matches the timer source
    - Minimum 100 iterations

  - [x] 1.3 Write property test: Commodity request time remaining computation (Property 2)
    - **Property 2: Commodity request time remaining computation**
    - **Validates: Requirements 2.2, 2.3**
    - Generate random NeedBy DateTimes from 1 day past to 10 days future
    - Verify GetSecondsRemaining() within 2 seconds of expected, and past dates return 0/"0s"
    - Minimum 100 iterations

  - [x] 1.4 Write property test: FormatSeconds equivalence (Property 3)
    - **Property 3: FormatSeconds equivalence with CountDownTime.TimeRemainingString**
    - **Validates: Requirements 6.2, 6.4**
    - Generate random second values 1–864000, compare FormatSeconds output with CountDownTime.TimeRemainingString allowing ±1s tolerance
    - Minimum 100 iterations

  - [x] 1.5 Write property test: Source name and process details formatting (Property 4)
    - **Property 4: Source name and process details formatting**
    - **Validates: Requirements 6.8, 6.9, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8**
    - Generate structures of each blueprint type with appropriate fields populated
    - Verify SourceName and ProcessDetails match expected format patterns
    - Minimum 100 iterations

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [-] 3. Create FormColonyActivity with Designer, filtering, and grid display
  - [x] 3.1 Create `Forms/ColonyActivity/FormColonyActivity.cs`, `FormColonyActivity.Designer.cs`, and `FormColonyActivity.resx`
    - Implement IProgrammaticUpdateSource with _isProgrammaticUpdate field, BeginProgrammaticUpdate/EndProgrammaticUpdate
    - Designer.cs: flpBase (Dock=Fill, TopDown, WrapContents=false), flpFilters (LeftToRight, AutoSize), dgvActivities (ReadOnly, AllowUserToAddRows=false, AllowUserToDeleteRows=false)
    - 7 CheckBoxes in flpFilters: chkBuilding, chkManufacturing, chkCommodityManufacturing, chkCommodityRequest, chkResearch, chkMining, chkRefining
    - txtFilter (ValidatedTextBox) in flpFilters for cross-column text search
    - dgvActivities columns: colCountDown, colSystemName, colColonyName, colActivityType, colSource, colProcessDetails, colSecondsRemaining (hidden)
    - timerRefresh (System.Windows.Forms.Timer, Interval=1000)
    - On load: subscribe to playerContext.CurrentPlayerChanged and ColonyDataChanged with named methods; set default checkbox states (Mining/Refining unchecked); call RefreshData
    - RefreshData: call ColonyActivityCollector.CollectActivities, apply filters, populate grid sorted by colSecondsRemaining ascending
    - CheckBox CheckedChanged handlers: call ApplyFiltersAndPopulate
    - txtFilter TextChanged handler: call ApplyFiltersAndPopulate
    - ApplyFiltersAndPopulate: filter by selected ActivityTypes AND case-insensitive text substring across all visible columns
    - Timer tick: use ProgrammaticUpdateGuard, update colCountDown and colSecondsRemaining for each visible row
    - SortCompare handler for CountDownTime column to sort by hidden colSecondsRemaining numeric value
    - Layout handler to resize dgvActivities when form resizes
    - OnFormClosed: unsubscribe from PlayerContext events, stop and dispose timer
    - Add `Compile Include` entries in `OE2EmpireTracker.csproj` for FormColonyActivity.cs (SubType=Form), FormColonyActivity.Designer.cs (DependentUpon), and `EmbeddedResource` for FormColonyActivity.resx (DependentUpon)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4, 6.1, 8.1, 8.2, 8.3, 8.4, 9.1, 9.2, 9.3, 9.4, 10.1, 10.2, 10.3, 10.4, 12.1, 12.2, 12.3, 12.4, 12.5, 13.1, 13.2, 13.3_

  - [~] 3.2 Write property test: Combined activity type and text filtering (Property 5)
    - **Property 5: Combined activity type and text filtering**
    - **Validates: Requirements 4.3, 5.2, 5.3, 5.4**
    - Add to `ColonyActivityCollectorTests.cs`
    - Generate random ActivityRow lists (5–20 rows), random subsets of ActivityType, and random text filter strings
    - Apply filter logic and verify result matches expected set
    - Minimum 100 iterations

  - [~] 3.3 Write property test: Default sort order by numeric seconds remaining (Property 6)
    - **Property 6: Default sort order by numeric seconds remaining**
    - **Validates: Requirements 8.1, 8.4**
    - Add to `ColonyActivityCollectorTests.cs`
    - Generate random ActivityRow lists with varying seconds remaining, sort by GetSecondsRemaining() ascending, verify monotonic non-decreasing order
    - Minimum 100 iterations

- [x] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Wire MainWindow menu item and .csproj entries
  - [x] 5.1 Add "Colony Activity" menu item to MainWindow
    - Add `colonyActivityToolStripMenuItem` field and Designer.cs entries under editToolStripMenuItem DropDownItems
    - Add `using OE2EmpireTracker.Forms.ColonyActivity;` to MainWindow.cs
    - Add click handler `colonyActivityToolStripMenuItem_Click` that creates `new FormColonyActivity()`, sets `MdiParent = this`, calls `Show()`
    - _Requirements: 11.1, 11.2_

  - [x] 5.2 Write unit tests for ColonyActivityCollector edge cases
    - Add to `ColonyActivityCollectorTests.cs`
    - Test: empty colony list → zero rows
    - Test: colony with no active timers → zero rows
    - Test: colony with one active mining rig → one Mining row with correct details
    - Test: colony with one building structure → one Building row with "Building" details
    - Test: colony with one unfulfilled commodity request → one CommodityRequest row
    - Test: fulfilled commodity request → no row
    - Test: NeedBy in the past → "0s" display
    - Test: structure with both BuildCompletionTime and ProcessCompletionTime → Building takes priority
    - Test: refining with synthetic recipe → correct format
    - Test: refining with normal resource → correct format
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 3.1, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8_

- [x] 6. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests are implemented as regular NUnit `[Test]` methods with parameterized/loop-based assertions (FsCheck is not available)
- The .csproj uses explicit `Compile Include` entries — new files must be added manually
- All forms must implement IProgrammaticUpdateSource and use `using var guard = new ProgrammaticUpdateGuard(this);`
- Do NOT use anonymous lambdas for event subscriptions — use named methods
- All forms MUST unsubscribe from PlayerContext events in OnFormClosed
