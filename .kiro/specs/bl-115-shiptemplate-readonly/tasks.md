# Implementation Plan: BL-115 ShipTemplate Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the ShipTemplate form. Create ShipTemplateViewModel as a disconnected edit buffer for the template Name, HullBlueprintUUID, and Components list. Create ShipTemplateService as the sole mutator with CRUD methods. Create DTO request objects. Migrate FormShipTemplate to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. All component operations (add, remove, change) accumulate in the ViewModel edit buffer until Save. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create ShipTemplateUpdateRequest DTO
    - Create OE2EmpireTracker/Models/ShipTemplateUpdateRequest.cs
    - Properties: Original (ReadOnlyShipTemplate), Name (string), HullBlueprintUUID (string), Components (List of ShipComponentSlot)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1_

  - [ ] 1.2 Create ShipTemplateCreateRequest DTO
    - Create OE2EmpireTracker/Models/ShipTemplateCreateRequest.cs
    - Properties: Name (string), HullBlueprintUUID (string), Components (List of ShipComponentSlot)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1_

  - [ ] 1.3 Add FindMutableShipTemplate internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute
    - Reuse existing _shipTemplateCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 14.1, 14.2, 14.3_

- [ ] 2. Create ShipTemplateViewModel as disconnected edit buffer
  - [ ] 2.1 Create ShipTemplateViewModel class
    - Create OE2EmpireTracker/ViewModels/ShipTemplateViewModel.cs
    - Private fields: _original (ReadOnlyShipTemplate), _uuid, _ownerUUID, local Name (string), local HullBlueprintUUID (string), local _components (List of ShipComponentSlot)
    - Public properties: Name (get/set), HullBlueprintUUID (get/set), Components (List of ShipComponentSlot), UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyShipTemplate): copies Name, HullBlueprintUUID, deep-copies Components (new ShipComponentSlot instances preserving all 6 fields), stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates ShipTemplateUpdateRequest from local state with deep-copied Components
    - BuildCreateRequest(): creates ShipTemplateCreateRequest from local state with deep-copied Components
    - IsDirty: compares Name, HullBlueprintUUID, and Components against _original snapshot; for new templates returns true once Name is non-empty
    - SetComponent(slotType, slotIndex, blueprintUUID): adds or updates a component in the local list
    - RemoveComponent(slotType, slotIndex): removes a component from the local list
    - ClearComponents(): clears the local Components list
    - Keep NLog Logger declaration
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 3. Create ShipTemplateService with CRUD methods
  - [ ] 3.1 Create ShipTemplateService class
    - Create OE2EmpireTracker/Services/ShipTemplateService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, ShipTemplateUpdateRequest): looks up mutable entity via FindMutableShipTemplate, applies Name and HullBlueprintUUID from request, replaces Components list with deep copy, persists via WriteContext(), fires ShipTemplateDataChanged event, returns ReadOnlyShipTemplate. Throws InvalidOperationException if UUID not found.
    - Create(ShipTemplateCreateRequest): creates new ShipTemplate with generated UUID, sets OwnerUUID from current player, populates Name, HullBlueprintUUID, and Components from request, adds to PlayerContext, persists, fires event, returns ReadOnlyShipTemplate
    - Delete(string uuid): looks up mutable entity, removes from PlayerContext via RemoveShipTemplate, persists, fires event. Returns silently if UUID empty or not found.
    - Include NLog Logger declaration
    - No write locks (ShipTemplate has no ReaderWriterLockSlim)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 13.1, 13.2, 13.3, 13.4, 13.5, 17.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate FormShipTemplate to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateTemplateList to store ReadOnlyShipTemplate in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyShipTemplates())
    - Change filter logic to use ReadOnlyShipTemplate properties for filter comparison
    - Remove any direct mutable ShipTemplate references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 2.1_

  - [ ] 5.2 Wire ViewModel as edit buffer
    - Add ShipTemplateViewModel _viewModel field and ShipTemplateService _shipTemplateService field
    - Change selection handler to extract ReadOnlyShipTemplate from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields (Name, HullBlueprintUUID, Components) instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - Remove _selectedTemplate mutable reference field
    - _Requirements: 1.2, 3.1, 3.2, 3.3, 4.1, 4.3, 4.5_

  - [ ] 5.3 Replace write-through with local-only ViewModel updates
    - Change TxtName_TextChanged handler to set _viewModel.Name instead of entity Name
    - Change CmbHull_SelectedItemChanged handler to set _viewModel.HullBlueprintUUID and call _viewModel.ClearComponents() instead of entity mutation
    - Change DgvSlots_CellValueChanged handler to call _viewModel.SetComponent/RemoveComponent instead of entity mutation
    - Remove any direct WriteContext() calls from control change handlers
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _shipTemplateService.Create(BuildCreateRequest()); else call _shipTemplateService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyShipTemplate
    - _Requirements: 15.1, 15.2, 15.3, 15.4_

  - [ ] 5.5 Wire Delete button through service with reference protection
    - Check ShipTemplateReferenceCounter for references before delete (ships, build items, stock targets)
    - If CountReferences > 0, show warning message with reference count and prevent deletion
    - If no references, prompt for confirmation before calling _shipTemplateService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [ ] 5.6 Wire Order Build to read from ViewModel
    - Change CmdOrderBuild_Click to read HullBlueprintUUID, Components, UUID, and Name from _viewModel instead of _selectedTemplate
    - No behavioral change --- Order Build reads from local state, does not mutate ShipTemplate

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompt on selection change
    - In template list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current template selected
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 6.2 Add unsaved changes prompt on New button
    - In CmdNew_Click handler, check _viewModel.IsDirty before clearing form
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [ ] 6.3 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Show same three-button dialog
    - Cancel sets e.Cancel = true to prevent close
    - Handles both form close (X button) and application exit (MainWindow closing)
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 10.1, 10.2_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Build with zero errors and zero warnings
  - All existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property test: LoadFrom round-trip preserves all fields
    - Create OE2EmpireTracker.Tests/ViewModels/ShipTemplateViewModelPropertyTests.cs
    - Create ValidShipTemplateGen() generator producing random ShipTemplate entities with random Name, UUID, OwnerUUID, HullBlueprintUUID, and 0-10 ShipComponentSlot entries (each with random SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent)
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 3.1, 3.2, 3.3**
    - Add Compile Include to test csproj

  - [ ] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 6.1, 6.5**

  - [ ] 8.3 Write property test: IsDirty detects Name change
    - **Property 3: IsDirty Detects Name Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing Name to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.2**

  - [ ] 8.4 Write property test: IsDirty detects HullBlueprintUUID change
    - **Property 4: IsDirty Detects HullBlueprintUUID Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing HullBlueprintUUID to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.3**

  - [ ] 8.5 Write property test: IsDirty detects Components change
    - **Property 5: IsDirty Detects Components Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding a component, removing a component, or modifying a component field causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.4**

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for ShipTemplateViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ShipTemplateViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new template with non-empty Name, BuildUpdateRequest copies Name HullBlueprintUUID and Components, BuildCreateRequest copies Name HullBlueprintUUID and Components, UUID and OwnerUUID are preserved from LoadFrom, SetComponent adds new component, SetComponent updates existing component, RemoveComponent removes component, ClearComponents empties list
    - Add Compile Include to test csproj
    - _Requirements: 3.1, 3.2, 6.1, 6.5, 6.6_

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/ShipTemplateServicePropertyTests.cs
    - Reuse ValidShipTemplateGen() pattern from ViewModel property tests
    - **Property 6: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 11.3, 11.4, 11.7**
    - Add Compile Include to test csproj

  - [ ] 10.2 Write property test: Service.Create round-trip
    - **Property 7: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 12.2, 12.4, 12.8**

  - [ ] 10.3 Write property test: Service.Delete removes template
    - **Property 8: Service.Delete Removes Template**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.1, 13.2**

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for ShipTemplateService
    - Create OE2EmpireTracker.Tests/Services/ShipTemplateServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires ShipTemplateDataChanged event, Create fires ShipTemplateDataChanged event, Delete fires ShipTemplateDataChanged event, Update replaces Components list, Create populates Components list from request
    - Add Compile Include to test csproj
    - _Requirements: 11.7, 11.8, 12.2, 12.3, 12.7, 12.8, 13.4, 13.5_

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for ShipTemplate
    - Create OE2EmpireTracker.Tests/Services/ShipTemplateMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests pattern
    - First check: scan for direct ShipTemplate property sets (Name, UUID, OwnerUUID, HullBlueprintUUID), assert they only appear in ShipTemplateService.cs, ShipTemplate.cs, PlayerContext.cs (deserialization/migration), and test code
    - Second check: scan for direct ShipTemplate.Components list mutation (Add, Remove, Clear, Insert, Components =), assert they only appear in ShipTemplateService.cs, ShipTemplate.cs, and test code
    - Verify FormShipTemplate.cs does not directly set properties on ShipTemplate
    - Verify ShipTemplateViewModel.cs does not directly set properties on ShipTemplate
    - **Validates: Property 9**
    - **Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2**
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - node .kiro/tools/audit.js reports no new findings
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New .cs files require Compile Include entries in the old-style .csproj
- ShipTemplateViewModel has 2 scalar fields (Name, HullBlueprintUUID) plus Components list
- ShipTemplateService is simple: no write locks, no immediate operations, no import method
- All component operations (add, remove, change) accumulate in the ViewModel edit buffer until Save
- ReadOnlyShipTemplate and ReadOnlyShipComponentSlot are already complete --- no gap fill needed
- ShipTemplateReferenceCounter checks 3 source types (ships, build items, stock targets)
- Order Build reads from ViewModel local state but does not mutate ShipTemplate --- remains as-is
- GetCurrentPlayerReadOnlyShipTemplates already exists in PlayerContext
- FindMutableShipTemplate needs to be added (reuses existing _shipTemplateCache)
- ShipTemplateDataChanged event and OnShipTemplateDataChanged already exist in PlayerContext
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
