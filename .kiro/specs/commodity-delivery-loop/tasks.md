# Implementation Plan: Commodity Delivery Loop

## Overview

Implement the commodity delivery loop in two parts: (A) auto-fill commodity drop-offs from colony requests on the route builder plan tab, and (B) commodity fulfillment/unfulfillment when checking delivery items on the execution form. All logic is C# on .NET Framework 4.8.1 with NUnit tests.

## Tasks

- [x] 1. Implement AutoFillCommodities on DeliveryPlanViewModel
  - [x] 1.1 Add `AutoFillCommodities(IEnumerable<RouteStop> routeStops, Func<string, Colony> colonyFinder)` method to `OE2EmpireTracker/ViewModels/DeliveryPlanViewModel.cs`
    - Iterate each RouteStop in sequence order
    - For each stop, look up Colony via colonyFinder delegate
    - Skip stop if colony is null
    - For each `CommodityRequested` where `Fulfilled == false` and `Requested - Delivered > 0`, call `AddDropOffItem` with `ItemType.Commodity`, `Name = cr.Name`, `BaseItemTypeID = cr.Name`, `Quantity = shortfall`
    - Return the count of items added
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 4.2, 5.1, 5.2_

  - [x]* 1.2 Write unit tests for AutoFillCommodities
    - Add tests to `OE2EmpireTracker.Tests/Baseline/DeliveryPlanViewModelTests.cs`
    - Test: single colony with unfulfilled commodities adds correct drop-off items
    - Test: fulfilled commodities are skipped
    - Test: zero/negative shortfall commodities are skipped
    - Test: missing colony (colonyFinder returns null) skips stop, processes others
    - Test: existing drop-off items are preserved (additive behavior)
    - Test: pick-up lists are not modified
    - Test: multiple stops with mixed fulfilled/unfulfilled commodities
    - Test: colony with empty Commodities list adds nothing
    - **Property 1: Auto-fill commodity shortfall mapping** — verify items added match unfulfilled entries with correct quantities
    - **Validates: Requirements 3.1, 3.3, 3.4, 3.6**
    - **Property 2: Auto-fill preserves existing drop-off items** — verify pre-existing items unchanged after auto-fill
    - **Validates: Requirements 4.1, 4.2**
    - **Property 3: Auto-fill PickUp list invariant** — verify PickUp lists identical before and after
    - **Validates: Requirements 5.1, 5.2**

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Create FormAutoFill modal dialog
  - [x] 3.1 Create `OE2EmpireTracker/Forms/DeliveryRoute/FormAutoFill.cs` and `FormAutoFill.Designer.cs`
    - Modal form with four checkboxes: Commodities (enabled, checked by default), Flatpacks (disabled, "(Future)"), Resources for Manufacturing (disabled, "(Future)"), Workers (disabled, "(Future)")
    - OK and Cancel buttons; `ShowDialog()` returns `DialogResult.OK` or `Cancel`
    - Expose `public bool IncludeCommodities` property
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 3.2 Add `FormAutoFill.resx` resource file
    - Standard empty resx for the Designer-generated form
    - _Requirements: 2.1_

- [x] 4. Add Auto-Fill button to FormDeliveryRoute and wire it up
  - [x] 4.1 Add `cmdAutoFill` button to `flpPlanSelector` in `FormDeliveryRoute.Designer.cs`
    - Place after `cmdExecutePlan` in the plan command area
    - Text: "Auto-Fill"
    - _Requirements: 1.1_

  - [x] 4.2 Wire `cmdAutoFill` click handler in `FormDeliveryRoute.cs`
    - Show/hide button based on whether a plan is selected (visible when `planViewModel != null`)
    - On click: show `FormAutoFill` modal; if OK and `IncludeCommodities`, call `planViewModel.AutoFillCommodities(viewModel.Stops, uuid => playerContext.FindColony(uuid))`
    - After auto-fill: call `planViewModel.Save()`, refresh drop-off grid via `PopulatePlanGrids()`, retain selected stop
    - _Requirements: 1.1, 1.2, 3.5, 9.1, 9.2_

- [x] 5. Add commodity fulfillment logic to FormDeliveryExecution
  - [x] 5.1 Extend `DeliveryItem_CheckedChanged` in `OE2EmpireTracker/Forms/DeliveryExecution/FormDeliveryExecution.cs`
    - After setting `item.Delivered`, check if `item.ItemType == ItemType.ItemTypeEnum.Commodity`
    - If commodity: find the `DeliveryPlanStop` containing this item, look up colony via `playerContext.FindColony(stop.ColonyUUID)`
    - Find matching `CommodityRequested` by `Name == item.Name`
    - On check: set `cr.Delivered = cr.Requested`, `cr.Fulfilled = true`
    - On uncheck: set `cr.Delivered = 0`, `cr.Fulfilled = false`
    - Log warning if colony not found or no matching CommodityRequested
    - `playerContext.writeContext()` is already called by existing handler
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 8.1, 8.2, 8.3, 8.4_

  - [x]* 5.2 Write unit tests for commodity fulfillment logic
    - Since fulfillment logic is in the Form handler, extract a static helper method or test via integration pattern
    - Test: checking commodity item sets CommodityRequested.Delivered = Requested and Fulfilled = true
    - Test: unchecking commodity item sets CommodityRequested.Delivered = 0 and Fulfilled = false
    - Test: non-commodity items do not affect CommodityRequested
    - Test: missing colony logs warning, no exception
    - Test: no matching CommodityRequested logs warning, no exception
    - **Property 4: Commodity fulfillment on delivery check** — verify Delivered == Requested and Fulfilled == true for any Requested value
    - **Validates: Requirements 6.2, 6.3, 7.1, 7.2**
    - **Property 5: Commodity unfulfillment on delivery uncheck** — verify Delivered == 0 and Fulfilled == false
    - **Validates: Requirements 8.2, 8.3**

- [x] 6. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Update .csproj files with new Compile Include entries
  - [x] 7.1 Add entries to `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - Add `<Compile Include="Forms\DeliveryRoute\FormAutoFill.cs"><SubType>Form</SubType></Compile>`
    - Add `<Compile Include="Forms\DeliveryRoute\FormAutoFill.Designer.cs"><DependentUpon>FormAutoFill.cs</DependentUpon></Compile>`
    - Add `<EmbeddedResource Include="Forms\DeliveryRoute\FormAutoFill.resx"><DependentUpon>FormAutoFill.cs</DependentUpon></EmbeddedResource>`
    - _Requirements: 1.1, 2.1_

  - [x] 7.2 Add test file entry to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj` (if new test file created)
    - Add `<Compile Include="...">` for any new test files
    - _Requirements: N/A (build infrastructure)_

- [x] 8. Final checkpoint — Ensure all tests pass and solution builds
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- FsCheck is not available in the project; property-style tests use regular NUnit `[Test]` methods with parameterized/loop-based assertions instead
- The .csproj uses explicit `<Compile Include>` entries — new files must be added manually (Task 7)
- 581 tests currently passing; new tests must not break existing ones
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
