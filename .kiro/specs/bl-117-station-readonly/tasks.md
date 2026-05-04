# Implementation Plan: BL-117 Station Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Station form (FormStation). Create StationViewModel as a disconnected edit buffer for all station fields including Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Hold, and MunitionsHold. Create StationService as the sole mutator with CRUD methods. Create DTO request objects. Migrate FormStation to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. All operations accumulate in the ViewModel edit buffer until Save. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create StationUpdateRequest DTO
    - Create OE2EmpireTracker/Models/StationUpdateRequest.cs
    - Properties: Original (ReadOnlyStation), Name (string), StationType (StationType), Ownership (StationOwnership), StationBlueprintUUID (string), HullCurrentHP (int), HullMaxHP (int), HullMaxRepairPercent (decimal), Components (List of ShipComponentSlot), Hold (ItemBag), MunitionsHold (ItemBag)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1_

  - [ ] 1.2 Create StationCreateRequest DTO
    - Create OE2EmpireTracker/Models/StationCreateRequest.cs
    - Properties: Name (string)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1_

  - [ ] 1.3 Add FindMutableStation internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute
    - Reuse existing _stationCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 14.1, 14.2, 14.3_
- [ ] 2. Create StationViewModel as disconnected edit buffer
  - [ ] 2.1 Create StationViewModel class
    - Create OE2EmpireTracker/ViewModels/StationViewModel.cs
    - Private fields: _original (ReadOnlyStation), _uuid, _ownerUUID, local Name (string), StationType (StationType), Ownership (StationOwnership), StationBlueprintUUID (string), HullCurrentHP (int), HullMaxHP (int), HullMaxRepairPercent (decimal), _components (List of ShipComponentSlot), _hold (ItemBag), _munitionsHold (ItemBag)
    - Public properties: Name (get/set), StationType (get/set), Ownership (get/set), StationBlueprintUUID (get/set), HullCurrentHP (get/set), HullMaxHP (get/set), HullMaxRepairPercent (get/set), Components (List of ShipComponentSlot), Hold (ItemBag), MunitionsHold (ItemBag), UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyStation, string playerUUID): copies all scalar fields, deep-copies Components (new ShipComponentSlot instances preserving all 6 fields), deep-copies the player's Hold and MunitionsHold (new ItemBag with new Item instances), stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates StationUpdateRequest from local state with deep-copied collections
    - BuildCreateRequest(): creates StationCreateRequest from local state
    - IsDirty: compares all scalar fields, Components, Hold, and MunitionsHold against _original snapshot; for new stations returns true once Name is non-empty
    - SetHullComponent(currentHP, maxRepairPercent): updates hull HP fields
    - SetComponentCondition(slotType, slotIndex, currentHP, maxRepairPercent): updates component HP fields
    - ClearComponents(): clears the local Components list
    - AddHoldItem(item): adds an item to the local Hold bag
    - RemoveHoldItem(uuid): removes an item from the local Hold bag
    - AddMunitionsItem(item): adds an item to the local MunitionsHold bag
    - RemoveMunitionsItem(uuid): removes an item from the local MunitionsHold bag
    - Deep-copy helper for ItemBag: creates new ItemBag, iterates Items dictionary, creates new Item instances with all fields copied (including recursive Contents for crates)
    - Keep NLog Logger declaration
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.12, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 6.11_
- [ ] 3. Create StationService with CRUD methods
  - [ ] 3.1 Create StationService class
    - Create OE2EmpireTracker/Services/StationService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, StationUpdateRequest): looks up mutable entity via FindMutableStation, applies Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent from request, replaces Components list with deep copy, replaces current player's Hold in Holds dictionary with deep copy, replaces MunitionsHold with deep copy, persists via WriteContext(), fires StationDataChanged event, returns ReadOnlyStation. Throws InvalidOperationException if UUID not found.
    - Create(StationCreateRequest): creates new Station with generated UUID, sets OwnerUUID from current player, populates Name from request (defaults to 'New Station' if empty), adds to PlayerContext, persists, fires event, returns ReadOnlyStation
    - Delete(string uuid): looks up mutable entity, removes from PlayerContext via RemoveStation, persists, fires event. Returns silently if UUID empty or not found.
    - Include NLog Logger declaration
    - No write locks (Station has no ReaderWriterLockSlim)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 13.1, 13.2, 13.3, 13.4, 13.5, 17.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.
- [ ] 5. Migrate FormStation to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateStationList to store ReadOnlyStation in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyStations())
    - Change filter logic to use ReadOnlyStation properties for filter comparison
    - Remove any direct mutable Station references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 2.1_

  - [ ] 5.2 Wire ViewModel as edit buffer
    - Add StationViewModel _viewModel field and StationService _stationService field
    - Change selection handler to extract ReadOnlyStation from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields (Name, StationType, Ownership, StationBlueprintUUID, Components, Hold, MunitionsHold, HullCurrentHP, HullMaxRepairPercent) instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - Remove _selectedStation mutable reference field
    - _Requirements: 1.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.3, 4.4, 4.5, 4.6, 4.8, 4.10_

  - [ ] 5.3 Replace write-through with local-only ViewModel updates
    - Change TxtName_TextChanged handler to set _viewModel.Name instead of entity Name
    - Change CmbStationType_SelectedIndexChanged handler to set _viewModel.StationType instead of entity StationType
    - Change CmbOwnership_SelectedIndexChanged handler to set _viewModel.Ownership instead of entity Ownership
    - Change CmbStationBlueprint_SelectedIndexChanged handler to set _viewModel.StationBlueprintUUID and call _viewModel.ClearComponents() instead of entity mutation
    - Change DgvComponents_CellEndEdit handler to call _viewModel.SetHullComponent or _viewModel.SetComponentCondition instead of entity mutation
    - Change DgvHold_CellEndEdit handler to update ViewModel hold items instead of entity mutation
    - Change CmdHoldAdd_Click handler to call _viewModel.AddHoldItem instead of entity mutation
    - Change CmdHoldRemove_Click handler to call _viewModel.RemoveHoldItem instead of entity mutation
    - Change CmdMunAdd_Click handler to call _viewModel.AddMunitionsItem instead of entity mutation
    - Change CmdMunRemove_Click handler to call _viewModel.RemoveMunitionsItem instead of entity mutation
    - Remove any direct WriteContext() calls from control change handlers
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_
  - [ ] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _stationService.Create(BuildCreateRequest()); else call _stationService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyStation
    - _Requirements: 15.1, 15.2, 15.3, 15.4_

  - [ ] 5.5 Wire Delete button through service with reference protection
    - Check StationReferenceCounter for references before delete
    - If CountReferences > 0, show warning message with reference count and prevent deletion
    - If no references, prompt for confirmation before calling _stationService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompt on selection change
    - In station list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current station selected
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
    - Create OE2EmpireTracker.Tests/ViewModels/StationViewModelPropertyTests.cs
    - Create ValidStationGen() generator producing random Station entities with random Name, UUID, OwnerUUID, StationBlueprintUUID, random StationType, random StationOwnership, random HullCurrentHP, HullMaxHP, HullMaxRepairPercent, 0-10 ShipComponentSlot entries, 0-5 Hold items, 0-5 MunitionsHold items
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**
    - Add Compile Include to test csproj

  - [ ] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 6.1, 6.10**

  - [ ] 8.3 Write property test: IsDirty detects Name change
    - **Property 3: IsDirty Detects Name Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing Name to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.2**

  - [ ] 8.4 Write property test: IsDirty detects StationBlueprintUUID change
    - **Property 4: IsDirty Detects StationBlueprintUUID Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing StationBlueprintUUID to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.5**

  - [ ] 8.5 Write property test: IsDirty detects Components change
    - **Property 5: IsDirty Detects Components Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding a component, removing a component, or modifying a component field causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.7**

  - [ ] 8.6 Write property test: IsDirty detects StationType change
    - **Property 6: IsDirty Detects StationType Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing StationType to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.3**

  - [ ] 8.7 Write property test: IsDirty detects Hold change
    - **Property 7: IsDirty Detects Hold Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding or removing a hold item causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.8**
- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for StationViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/StationViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new station with non-empty Name, BuildUpdateRequest copies all fields and collections, BuildCreateRequest copies Name, UUID and OwnerUUID are preserved from LoadFrom, SetComponentCondition updates component HP fields, SetHullComponent updates hull HP fields, ClearComponents empties list, AddHoldItem adds item to Hold, RemoveHoldItem removes item from Hold, AddMunitionsItem adds item to MunitionsHold, RemoveMunitionsItem removes item from MunitionsHold, StationType and Ownership are preserved from LoadFrom, HullCurrentHP HullMaxHP HullMaxRepairPercent are preserved from LoadFrom
    - Add Compile Include to test csproj
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.7, 3.8, 6.1, 6.10, 6.11_

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/StationServicePropertyTests.cs
    - Reuse ValidStationGen() pattern from ViewModel property tests
    - **Property 8: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 11.3, 11.4, 11.5, 11.6, 11.9**
    - Add Compile Include to test csproj

  - [ ] 10.2 Write property test: Service.Create round-trip
    - **Property 9: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 12.2, 12.4, 12.8**

  - [ ] 10.3 Write property test: Service.Delete removes station
    - **Property 10: Service.Delete Removes Station**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.1, 13.2**
- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for StationService
    - Create OE2EmpireTracker.Tests/Services/StationServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires StationDataChanged event, Create fires StationDataChanged event, Delete fires StationDataChanged event, Update replaces Components list, Update replaces Hold and MunitionsHold, Create populates Name from request
    - Add Compile Include to test csproj
    - _Requirements: 11.9, 11.10, 12.2, 12.3, 12.7, 12.8, 13.4, 13.5_

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for Station
    - Create OE2EmpireTracker.Tests/Services/StationMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests, ShipTemplateMutationGuardTests pattern
    - First check: scan for direct Station property sets (Name, UUID, OwnerUUID, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent), assert they only appear in StationService.cs, Station.cs, PlayerContext.cs (deserialization/migration), and test code
    - Second check: scan for direct Station.Components list mutation (Add, Remove, Clear, Insert, Components =), assert they only appear in StationService.cs, Station.cs, and test code
    - Third check: scan for direct Station.Holds and Station.MunitionsHold mutation (AddItem, Remove, Clear, Holds[, Holds =, MunitionsHold =), assert they only appear in StationService.cs, Station.cs, and test code
    - Verify FormStation.cs does not directly set properties on Station
    - Verify StationViewModel.cs does not directly set properties on Station
    - **Validates: Property 11**
    - **Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2, 20.3**
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - node .kiro/tools/audit.js reports no new findings
  - Ensure all tests pass, ask the user if questions arise.
## Notes

- Tasks marked with * are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New .cs files require Compile Include entries in the old-style .csproj
- StationViewModel has 5 scalar fields (Name, StationType, Ownership, StationBlueprintUUID, OwnerUUID) plus 3 hull HP fields plus Components list plus Hold and MunitionsHold ItemBags
- StationService is simple: no write locks, no CreateFromTemplate method
- All operations (component changes, hold add/remove, munitions add/remove, field edits) accumulate in the ViewModel edit buffer until Save
- ReadOnlyStation, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are already complete --- no gap fill needed
- StationReferenceCounter checks 10 source types (route stops, delivery plan stops, build item assembly/build locations, market listings, market transactions, supply chain stages, stock plan targets, overflow rules, ship locations)
- GetCurrentPlayerReadOnlyStations already exists in PlayerContext
- FindMutableStation needs to be added (reuses existing _stationCache)
- StationDataChanged event and OnStationDataChanged already exist in PlayerContext
- ItemBag deep-copy requires creating new Item instances with all fields, including recursive Contents for crates
- Hold is player-scoped: the ViewModel buffers only the current player's hold from the station's Holds dictionary
- Ownership-gated tabs: Components and Munitions tabs are only enabled for PlayerOwned stations
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx