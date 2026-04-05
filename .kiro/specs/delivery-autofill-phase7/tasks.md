# Implementation Plan: Delivery Auto-Fill Phase 7

## Overview

Extend the delivery auto-fill system from commodity-only to support Flatpacks, Manufacturing Resources, and Workers. Add StagingResources flag to ColonyStructure, Stage Resources checkbox to the ColonyStructure UI, and flatpack staging logic to FormDeliveryExecution. All new auto-fill methods follow the established `AutoFillCommodities` pattern on `DeliveryPlanViewModel`.

## Tasks

- [x] 1. Add StagingResources property to data and ViewModel layers
  - [x] 1.1 Add `StagingResources` boolean property to `ColonyStructure` (default false)
    - Add `public bool StagingResources { get; set; } = false;` to `OE2EmpireTracker/Baseline/ColonyStructure.cs`
    - Newtonsoft.Json serializes it automatically (same pattern as `ManufacturingQuantity`)
    - _Requirements: 4.1_

  - [x] 1.2 Add `StagingResources` pass-through property to `ColonyStructureViewModel`
    - Add getter/setter that reads/writes `_structure.StagingResources` in `OE2EmpireTracker/ViewModels/ColonyStructureViewModel.cs`
    - _Requirements: 4.2_

  - [x]* 1.3 Write unit tests for StagingResources round-trip
    - **Property 5: StagingResources serialization round-trip**
    - **Validates: Requirements 4.1, 4.2**
    - Test default value is false
    - Test set true via ViewModel reads back true from both ViewModel and underlying ColonyStructure
    - Test JSON serialize/deserialize preserves StagingResources value
    - Add tests to `OE2EmpireTracker.Tests/Baseline/ColonyStructureTests.cs`

- [x] 2. Add Stage Resources checkbox to ColonyStructure UI
  - [x] 2.1 Add `chkStageResources` CheckBox to `ColonyStructure.Designer.cs`
    - Add checkbox field declaration and InitializeComponent wiring
    - Position before cmdStart in the manufacturing controls area
    - _Requirements: 5.1, 5.2_

  - [x] 2.2 Wire Stage Resources checkbox visibility and behavior in `ColonyStructure.cs`
    - Show only for Manufactory and CommodityFactory blueprint types
    - Hide when `ProcessCompletionTime` is not null (manufacturing running)
    - Enable only when blueprint/commodity is selected AND `ManufacturingQuantity > 0`
    - On CheckedChanged: set `ViewModel.StagingResources` and fire `ColonyStructureDataChanged`
    - Wire in `handleManufactoryControls()` and `handleCommodityFactoryControls()`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 3. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement AutoFillFlatpacks method
  - [x] 4.1 Add `AutoFillFlatpacks` method to `DeliveryPlanViewModel`
    - Accepts `IEnumerable<RouteStop> routeStops` and `Func<string, Colony> colonyFinder`
    - For each stop's colony, iterate structures; create `ColonyStructureViewModel` to check `IsBuilt`/`IsStaged`
    - For unbuilt+unstaged structures with a valid blueprint: add drop-off item with `ItemType.Flatpack`, `BaseItemTypeID = FlatpackBlueprintUUID`, `Name = blueprint.ExtendedName`, `Quantity = 1`
    - Return count of items added
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x]* 4.2 Write unit tests for AutoFillFlatpacks
    - **Property 1: Flatpack auto-fill produces correct items for unbuilt+unstaged structures**
    - **Validates: Requirements 2.1, 2.2, 2.3**
    - Test no structures → returns 0
    - Test all structures built → returns 0
    - Test all structures staged → returns 0
    - Test mix of built/staged/unbuilt → correct count and item fields
    - Test preserves existing plan items (Property 2, validates Req 2.4, 11.3)
    - Test missing colony → skips stop
    - Test missing blueprint → skips structure
    - Add tests to `OE2EmpireTracker.Tests/Baseline/DeliveryPlanViewModelTests.cs`

- [x] 5. Implement AutoFillManufacturingResources method
  - [x] 5.1 Add `AutoFillManufacturingResources` method to `DeliveryPlanViewModel`
    - Accepts `IEnumerable<RouteStop> routeStops`, `Func<string, Colony> colonyFinder`, `Func<string, Blueprint> blueprintFinder`
    - For each stop's colony, iterate structures where `StagingResources == true`
    - For Manufactory: look up `ManufacturingBlueprintUUID` → `Blueprint.Resources` → multiply by `ManufacturingQuantity`
    - For CommodityFactory: look up `ManufacturingCommodityName` → `Commodity.ConstructionResources` → multiply by `ManufacturingQuantity`
    - Aggregate resource needs per colony, subtract warehouse Refined stock via `colony.Items.FindResource(name, "Refined")`
    - Add drop-off items for shortfalls with `ItemType.Resource`, `ResourcePurity = "Refined"`
    - Return count of items added
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4_

  - [x]* 5.2 Write unit tests for AutoFillManufacturingResources
    - **Property 3: Manufacturing resource shortfall calculation is correct**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4**
    - Test no staging structures → returns 0
    - Test warehouse fully stocked → returns 0 (no shortfall)
    - Test partial warehouse → correct shortfall quantities
    - Test multiple staging structures on same colony aggregate needs
    - Test CommodityFactory staging with ConstructionResources
    - Test ManufacturingQuantity <= 0 → skipped
    - Test missing blueprint → skipped
    - Test preserves existing plan items (Property 2)
    - Add tests to `OE2EmpireTracker.Tests/Baseline/DeliveryPlanViewModelTests.cs`

- [x] 6. Implement AutoFillWorkers method
  - [x] 6.1 Add `AutoFillWorkers` method to `DeliveryPlanViewModel`
    - Accepts `IEnumerable<RouteStop> routeStops`, `Func<string, Colony> colonyFinder`, `PlayerContext playerContext`
    - For each stop's colony, create `ColonyStatusCalculator`, run `CalculateBuilt()` and `CalculateIdeal()`
    - Compare `finalIdealStatus.HabitationRequired` vs `finalActualStatus.HabitationRequired`; skip if ideal <= actual
    - For each worker type in `WorkerDetail.WorkerTypes`: sum ideal vs actual worker counts across structures, compute gap
    - Add drop-off items for gaps with `ItemType.WorkDetail`, `BaseItemTypeID = wt.DetailKey`, `Name = workerDetail.Name`
    - Return count of items added
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x]* 6.2 Write unit tests for AutoFillWorkers
    - **Property 4: Worker gap calculation produces correct worker items**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4**
    - Test fully staffed colony → returns 0
    - Test partial staffing → correct gap per worker type
    - Test colony with no structures → returns 0
    - Test preserves existing plan items (Property 2)
    - Add tests to `OE2EmpireTracker.Tests/Baseline/DeliveryPlanViewModelTests.cs`

- [x] 7. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Enable FormAutoFill checkboxes and expose properties
  - [x] 8.1 Enable chkFlatpacks, chkResources, chkWorkers in `FormAutoFill.Designer.cs`
    - Set `Enabled = true` on all three checkboxes
    - Update text: "Flatpacks", "Resources for Manufacturing", "Workers" (remove "(Future)" suffixes)
    - _Requirements: 1.1, 1.2, 6.1, 6.2, 9.1, 9.2_

  - [x] 8.2 Add `IncludeFlatpacks`, `IncludeResources`, `IncludeWorkers` properties to `FormAutoFill.cs`
    - Each returns the Checked state of the corresponding checkbox
    - _Requirements: 1.2, 6.2, 9.2_

- [x] 9. Update FormDeliveryRoute orchestration
  - [x] 9.1 Update `cmdAutoFill_Click` in `FormDeliveryRoute.cs` to call all four auto-fill methods
    - Read `dlg.IncludeFlatpacks`, `dlg.IncludeResources`, `dlg.IncludeWorkers`
    - Call `AutoFillFlatpacks`, `AutoFillManufacturingResources`, `AutoFillWorkers` based on checkbox state
    - Accumulate total added count; save and refresh if any items added
    - _Requirements: 11.1, 11.2, 11.3_

- [x] 10. Add flatpack staging logic to FormDeliveryExecution
  - [x] 10.1 Add Flatpack handling in `DeliveryItem_CheckedChanged` in `FormDeliveryExecution.cs`
    - When `item.ItemType == ItemType.ItemTypeEnum.Flatpack`: find matching `ColonyStructure` where `FlatpackBlueprintUUID == item.BaseItemTypeID`
    - On check: set `Properties["Staged"] = "True"`; on uncheck: set `Properties["Staged"] = "False"`
    - Fire `playerContext.OnColonyDataChanged(stop.ColonyUUID)`
    - Log warning if no matching structure found
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 11. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- FsCheck is not available in this project; property-based tests are implemented as regular NUnit `[Test]` methods covering the same correctness properties
- The .csproj uses explicit `<Compile Include>` entries — new files must be added manually if any are created
- All new test methods go in the existing `DeliveryPlanViewModelTests.cs` and `ColonyStructureTests.cs` files
