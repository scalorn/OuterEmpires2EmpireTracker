# Implementation Plan: BL-123 PricingPlan Immutable Data Model

## Overview

Apply the immutable data model pattern (established in BL-108 blueprints and BL-111 player profiles) to the PricingPlan form. Create PricingPlanViewModel as a disconnected edit buffer, PricingPlanService as the sole mutator, DTO request objects, and migrate FormPricingPlan to use ReadOnly wrappers. Add unsaved changes prompts, property-based tests, unit tests, and a mutation guard test.

## Tasks

- [x] 1. Create DTO request models
  - [x] 1.1 Create `PricingPlanUpdateRequest` DTO
    - Create `OE2EmpireTracker/Models/PricingPlanUpdateRequest.cs`
    - Properties: Original (ReadOnlyPricingPlan), Name, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices (Dictionary<string, decimal>)
    - Add `<Compile Include="Models\PricingPlanUpdateRequest.cs" />` to `OE2EmpireTracker.csproj`
    - _Requirements: 12.1, 12.3, 12.4_

  - [x] 1.2 Create `PricingPlanCreateRequest` DTO
    - Create `OE2EmpireTracker/Models/PricingPlanCreateRequest.cs`
    - Properties: Name, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices (Dictionary<string, decimal>)
    - No UUID (service assigns it), no OwnerUUID (service sets from current player)
    - Add `<Compile Include="Models\PricingPlanCreateRequest.cs" />` to `OE2EmpireTracker.csproj`
    - _Requirements: 13.1, 13.4_

- [x] 2. Create PricingPlanViewModel (edit buffer)
  - [x] 2.1 Create `PricingPlanViewModel` class
    - Create `OE2EmpireTracker/ViewModels/PricingPlanViewModel.cs`
    - Private fields: _original (ReadOnlyPricingPlan), _uuid, _ownerUUID, _name, _description, _fixedCostPerItem, _hourlyCostRate, _resourcePrices (Dictionary<string, decimal>)
    - Public properties: Name, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices, UUID, OwnerUUID, Original, IsNew, IsDirty
    - Methods: LoadFrom(ReadOnlyPricingPlan), Reset(), BuildUpdateRequest(), BuildCreateRequest()
    - IsDirty compares all scalar fields and ResourcePrices dictionary against _original snapshot
    - For new plans (_original == null), IsDirty returns true once any field has a non-default value
    - Include NLog Logger declaration
    - Add `<Compile Include="ViewModels\PricingPlanViewModel.cs" />` to `OE2EmpireTracker.csproj`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3. Create PlayerContext.FindMutablePricingPlan
  - [x] 3.1 Add `FindMutablePricingPlan` internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint
    - Add `_pricingPlanCache` dictionary field with lock-based lazy initialization
    - Mark method `internal` so only the service can access it
    - Invalidate cache when pricing plan list changes (AddPricingPlan, RemovePricingPlan)
    - _Requirements: 15.1, 15.2, 15.3_

- [x] 4. Create PricingPlanService
  - [x] 4.1 Create `PricingPlanService` class
    - Create `OE2EmpireTracker/Services/PricingPlanService.cs`
    - Constructor takes PlayerContext dependency
    - Update(string uuid, PricingPlanUpdateRequest): looks up mutable entity via FindMutablePricingPlan, applies all fields, persists via WriteContext(), fires PricingDataChanged, returns ReadOnlyPricingPlan
    - Create(PricingPlanCreateRequest): creates new PricingPlan with generated UUID, sets OwnerUUID from current player, populates fields, adds to PlayerContext, persists, fires event, returns ReadOnlyPricingPlan
    - Delete(string uuid): looks up entity, removes from PlayerContext, persists, fires event; returns silently if UUID empty or not found
    - Include NLog Logger declaration
    - Add `<Compile Include="Services\PricingPlanService.cs" />` to `OE2EmpireTracker.csproj`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 14.1, 14.2, 14.3, 14.4, 14.5, 17.1_

- [x] 5. Checkpoint --- Verify new classes compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Migrate FormPricingPlan to ReadOnly wrappers and ViewModel
  - [x] 6.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulatePlanList to store ReadOnlyPricingPlan in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyPricingPlans())
    - Change TxtPlanFilter_TextChanged to use ReadOnlyPricingPlan.Name for filter comparison
    - Remove the `_selectedPlan` mutable PricingPlan field
    - _Requirements: 2.1, 2.3, 3.1_

  - [x] 6.2 Wire ViewModel as edit buffer
    - Add `PricingPlanViewModel _viewModel` field and `PricingPlanService _pricingPlanService` field
    - Change LvwPlans_ItemSelectionChanged to extract ReadOnlyPricingPlan from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - _Requirements: 2.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2_

  - [x] 6.3 Replace write-through with local-only ViewModel updates
    - Change TxtPlanName_TextChanged to set _viewModel.Name instead of entity.Name
    - Change TxtDescription_TextChanged to set _viewModel.Description instead of entity.Description
    - Change TxtFixedCost_TextChanged to set _viewModel.FixedCostPerItem instead of entity.FixedCostPerItem
    - Change TxtHourlyCost_TextChanged to set _viewModel.HourlyCostRate instead of entity.HourlyCostRate
    - Change DgvResourcePrices_CellValueChanged to update _viewModel.ResourcePrices instead of entity.ResourcePrices
    - Remove any direct WriteContext() calls from cell change handlers
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 6.4 Wire Save button through service
    - Change CmdSave_Click: if _viewModel.IsNew, call _pricingPlanService.Create(BuildCreateRequest()); else call _pricingPlanService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyPricingPlan
    - Enable Save button only when _viewModel.IsDirty is true
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 7.6_

  - [x] 6.5 Wire Delete button through service
    - Change CmdDelete_Click to call _pricingPlanService.Delete(uuid) instead of direct entity removal
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [x] 7. Add unsaved changes prompts
  - [x] 7.1 Add unsaved changes prompt on selection change
    - In LvwPlans_ItemSelectionChanged, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current plan selected
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 7.2 Add unsaved changes prompt on New button
    - In CmdNew_Click, check _viewModel.IsDirty before clearing form
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [x] 7.3 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Show same three-button dialog
    - Cancel sets e.Cancel = true to prevent close
    - Handles both form close (X button) and application exit (MainWindow closing)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 10.1, 10.2_

- [x] 8. Checkpoint --- Verify form migration compiles and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Add ViewModel property tests
  - [x] 9.1 Write property test: LoadFrom round-trip preserves all fields
    - Create `OE2EmpireTracker.Tests/ViewModels/PricingPlanViewModelPropertyTests.cs`
    - Reuse ValidPricingPlanGen() pattern from PricingPlanSerializationPropertyTests
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - `[FsCheck.NUnit.Property(MaxTest = 25)]`
    - **Validates: Requirements 4.1, 4.2, 4.4**
    - Add `<Compile Include="ViewModels\PricingPlanViewModelPropertyTests.cs" />` to test csproj

  - [x] 9.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - `[FsCheck.NUnit.Property(MaxTest = 25)]`
    - **Validates: Requirements 7.1, 7.4**

  - [x] 9.3 Write property test: IsDirty detects any single field change
    - **Property 3: IsDirty Detects Any Single Field Change**
    - `[FsCheck.NUnit.Property(MaxTest = 50)]`
    - **Validates: Requirements 7.1, 7.2, 7.3**

- [x] 10. Add ViewModel unit tests
  - [x] 10.1 Write unit tests for PricingPlanViewModel
    - Create `OE2EmpireTracker.Tests/ViewModels/PricingPlanViewModelTests.cs`
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new plan with non-default Name, BuildUpdateRequest copies all fields, BuildCreateRequest copies all fields
    - Add `<Compile Include="ViewModels\PricingPlanViewModelTests.cs" />` to test csproj
    - _Requirements: 4.1, 4.2, 7.1, 7.5_

- [x] 11. Add Service property tests
  - [x] 11.1 Write property test: Service.Update round-trip
    - Create `OE2EmpireTracker.Tests/Services/PricingPlanServicePropertyTests.cs`
    - **Property 4: Service.Update Round-Trip**
    - `[FsCheck.NUnit.Property(MaxTest = 25)]`
    - **Validates: Requirements 12.3, 12.4, 12.7**
    - Add `<Compile Include="Services\PricingPlanServicePropertyTests.cs" />` to test csproj

  - [x] 11.2 Write property test: Service.Create round-trip
    - **Property 5: Service.Create Round-Trip**
    - `[FsCheck.NUnit.Property(MaxTest = 25)]`
    - **Validates: Requirements 13.2, 13.4, 13.8**

  - [x] 11.3 Write property test: Service.Delete removes plan
    - **Property 6: Service.Delete Removes Plan**
    - `[FsCheck.NUnit.Property(MaxTest = 25)]`
    - **Validates: Requirements 14.1, 14.2**

- [x] 12. Add Service unit tests
  - [x] 12.1 Write unit tests for PricingPlanService
    - Create `OE2EmpireTracker.Tests/Services/PricingPlanServiceTests.cs`
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires PricingDataChanged event, Create fires PricingDataChanged event, Delete fires PricingDataChanged event
    - Add `<Compile Include="Services\PricingPlanServiceTests.cs" />` to test csproj
    - _Requirements: 12.6, 12.8, 13.2, 13.3, 13.7, 14.4, 14.5_

- [x] 13. Add mutation guard test
  - [x] 13.1 Write mutation guard test for PricingPlan
    - Create `OE2EmpireTracker.Tests/Services/PricingPlanMutationGuardTests.cs`
    - Follow BlueprintMutationGuardTests pattern: scan FormPricingPlan.cs and PricingPlanViewModel.cs for direct entity mutation patterns
    - Verify ViewModel does not expose mutable PricingPlan via public property
    - **Validates: Requirements 17.1, 17.2, 17.3, 20.1**
    - Add `<Compile Include="Services\PricingPlanMutationGuardTests.cs" />` to test csproj

- [x] 14. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - `node .kiro/tools/audit.js` reports no new findings
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New `.cs` files require `<Compile Include="...">` entries in the old-style `.csproj`
- Reuse the `ValidPricingPlanGen()` and `NonNegativeDecimalGen()` generators from `PricingPlanSerializationPropertyTests`
- PricingPlan is simpler than Blueprint/PlayerProfile: flat scalars + one dictionary, no nested objects, no import, no move, no timer
- ReadOnlyPricingPlan is already complete --- no gap fill required (unlike BL-108/BL-111)
- Build with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Run tests with vstest.console: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx`
