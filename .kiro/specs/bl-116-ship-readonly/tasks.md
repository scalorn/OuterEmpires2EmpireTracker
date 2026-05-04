# Implementation Plan: BL-116 Ship Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Ship form (FormShipInstance). Create ShipViewModel as a disconnected edit buffer for all ship fields including Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Cargo, and Hopper. Create ShipService as the sole mutator with CRUD methods plus CreateFromTemplate. Create DTO request objects. Migrate FormShipInstance to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. All operations accumulate in the ViewModel edit buffer until Save. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create ShipUpdateRequest DTO
    - Create OE2EmpireTracker/Models/ShipUpdateRequest.cs
    - Properties: Original (ReadOnlyShip), Name (string), TemplateUUID (string), HullBlueprintUUID (string), LocationType (DestinationType), LocationUUID (string), HullCurrentHP (int), HullMaxHP (int), HullMaxRepairPercent (decimal), Components (List of ShipComponentSlot), Cargo (ItemBag), Hopper (ItemBag)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1_

  - [ ] 1.2 Create ShipCreateRequest DTO
    - Create OE2EmpireTracker/Models/ShipCreateRequest.cs
    - Properties: Name (string)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1_

  - [ ] 1.3 Add FindMutableShip internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute
    - Reuse existing _shipCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 15.1, 15.2, 15.3_
- [ ] 2. Create ShipViewModel as disconnected edit buffer
  - [ ] 2.1 Create ShipViewModel class
    - Create OE2EmpireTracker/ViewModels/ShipViewModel.cs
    - Private fields: _original (ReadOnlyShip), _uuid, _ownerUUID, local Name (string), TemplateUUID (string), HullBlueprintUUID (string), LocationType (DestinationType), LocationUUID (string), HullCurrentHP (int), HullMaxHP (int), HullMaxRepairPercent (decimal), _components (List of ShipComponentSlot), _cargo (ItemBag), _hopper (ItemBag)
    - Public properties: Name (get/set), TemplateUUID (get/set), HullBlueprintUUID (get/set), LocationType (get/set), LocationUUID (get/set), HullCurrentHP (get/set), HullMaxHP (get/set), HullMaxRepairPercent (get/set), Components (List of ShipComponentSlot), Cargo (ItemBag), Hopper (ItemBag), UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyShip): copies all scalar fields, deep-copies Components (new ShipComponentSlot instances preserving all 6 fields), deep-copies Cargo and Hopper (new ItemBag with new Item instances), stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates ShipUpdateRequest from local state with deep-copied collections
    - BuildCreateRequest(): creates ShipCreateRequest from local state
    - IsDirty: compares all scalar fields, Components, Cargo, and Hopper against _original snapshot; for new ships returns true once Name is non-empty
    - SetComponent(slotType, slotIndex, blueprintUUID): adds or updates a component in the local list
    - RemoveComponent(slotType, slotIndex): removes a component from the local list
    - ClearComponents(): clears the local Components list
    - SetHullComponent(currentHP, maxRepairPercent): updates hull HP fields
    - SetComponentCondition(slotType, slotIndex, currentHP, maxRepairPercent): updates component HP fields
    - AddCargoItem(item): adds an item to the local Cargo bag
    - RemoveCargoItem(uuid): removes an item from the local Cargo bag
    - AddHopperItem(item): adds an item to the local Hopper bag
    - RemoveHopperItem(uuid): removes an item from the local Hopper bag
    - GetSelectedBag(isHopper): returns Cargo or Hopper based on flag
    - Deep-copy helper for ItemBag: creates new ItemBag, iterates Items dictionary, creates new Item instances with all fields copied (including recursive Contents for crates)
    - Keep NLog Logger declaration
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12, 4.1, 4.2, 4.3, 4.5, 4.9, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 6.11, 6.12_

- [ ] 3. Create ShipService with CRUD methods
  - [ ] 3.1 Create ShipService class
    - Create OE2EmpireTracker/Services/ShipService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, ShipUpdateRequest): looks up mutable entity via FindMutableShip, applies Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent from request, replaces Components list with deep copy, replaces Cargo and Hopper with deep copies, persists via WriteContext(), fires ShipDataChanged event, returns ReadOnlyShip. Throws InvalidOperationException if UUID not found.
    - Create(ShipCreateRequest): creates new Ship with generated UUID, sets OwnerUUID from current player, populates Name from request (defaults to 'New Ship' if empty), adds to PlayerContext, persists, fires event, returns ReadOnlyShip
    - Delete(string uuid): looks up mutable entity, removes from PlayerContext via RemoveShip, persists, fires event. Returns silently if UUID empty or not found.
    - CreateFromTemplate(string templateUUID): looks up ShipTemplate via FindShipTemplate, creates new Ship with generated UUID, OwnerUUID from current player, Name from template, TemplateUUID from template UUID, HullBlueprintUUID from template, Components deep-copied from template. Adds to PlayerContext, persists, fires event, returns ReadOnlyShip. Throws InvalidOperationException if template not found.
    - Include NLog Logger declaration
    - No write locks (Ship has no ReaderWriterLockSlim)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 13.1, 13.2, 13.3, 13.4, 13.5, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 18.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate FormShipInstance to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateShipList to store ReadOnlyShip in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyShips())
    - Change filter logic to use ReadOnlyShip properties for filter comparison
    - Remove any direct mutable Ship references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 2.1_

  - [ ] 5.2 Wire ViewModel as edit buffer
    - Add ShipViewModel _viewModel field and ShipService _shipService field
    - Change selection handler to extract ReadOnlyShip from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields (Name, HullBlueprintUUID, LocationType, LocationUUID, Components, Cargo, Hopper, HullCurrentHP, HullMaxRepairPercent) instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - Remove _selectedShip mutable reference field
    - _Requirements: 1.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 4.1, 4.3, 4.5, 4.6, 4.7, 4.9_

  - [ ] 5.3 Replace write-through with local-only ViewModel updates
    - Change TxtName_TextChanged handler to set _viewModel.Name instead of entity Name
    - Change CmbHull_SelectedItemChanged handler to set _viewModel.HullBlueprintUUID and call _viewModel.ClearComponents() instead of entity mutation
    - Change CmbLocationType_SelectedIndexChanged handler to set _viewModel.LocationType instead of entity LocationType
    - Change DgvComponents_CellValueChanged handler to call _viewModel.SetComponent/RemoveComponent instead of entity mutation
    - Change DgvComponents_CellEndEdit handler to call _viewModel.SetHullComponent or _viewModel.SetComponentCondition instead of entity mutation
    - Change CmdAddItem_Click handler to call _viewModel.AddCargoItem or _viewModel.AddHopperItem instead of entity mutation
    - Change CmdRemoveItem_Click handler to call _viewModel.RemoveCargoItem or _viewModel.RemoveHopperItem instead of entity mutation
    - Remove any direct WriteContext() calls from control change handlers
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [ ] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _shipService.Create(BuildCreateRequest()); else call _shipService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyShip
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [ ] 5.5 Wire Delete button through service with reference protection
    - Check ShipReferenceCounter for references before delete (delivery plans, build items)
    - If CountReferences > 0, show warning message with reference count and prevent deletion
    - If no references, prompt for confirmation before calling _shipService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 17.1, 17.2, 17.3, 17.4_

  - [ ] 5.6 Wire Create from Template through service
    - Change CmdFromTemplate_Click to show template selection dialog, then call _shipService.CreateFromTemplate(templateUUID)
    - After creation, refresh list view and reload _viewModel from returned ReadOnlyShip
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_
- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompt on selection change
    - In ship list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current ship selected
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
    - Create OE2EmpireTracker.Tests/ViewModels/ShipViewModelPropertyTests.cs
    - Create ValidShipGen() generator producing random Ship entities with random Name, UUID, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, 0-10 ShipComponentSlot entries, 0-5 Cargo items, 0-5 Hopper items
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9**
    - Add Compile Include to test csproj

  - [ ] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 6.1, 6.11**

  - [ ] 8.3 Write property test: IsDirty detects Name change
    - **Property 3: IsDirty Detects Name Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing Name to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.2**

  - [ ] 8.4 Write property test: IsDirty detects HullBlueprintUUID change
    - **Property 4: IsDirty Detects HullBlueprintUUID Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing HullBlueprintUUID to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.4**

  - [ ] 8.5 Write property test: IsDirty detects Components change
    - **Property 5: IsDirty Detects Components Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding a component, removing a component, or modifying a component field causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.8**

  - [ ] 8.6 Write property test: IsDirty detects Location change
    - **Property 6: IsDirty Detects Location Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing LocationType or LocationUUID to a different value causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.5, 6.6**

  - [ ] 8.7 Write property test: IsDirty detects Cargo change
    - **Property 7: IsDirty Detects Cargo Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding or removing a cargo item causes IsDirty to return true
    - **Validates: Requirements 6.1, 6.9**

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for ShipViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ShipViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new ship with non-empty Name, BuildUpdateRequest copies all fields and collections, BuildCreateRequest copies Name, UUID and OwnerUUID are preserved from LoadFrom, SetComponent adds new component, SetComponent updates existing component, RemoveComponent removes component, ClearComponents empties list, AddCargoItem adds item to Cargo, RemoveCargoItem removes item from Cargo, AddHopperItem adds item to Hopper, RemoveHopperItem removes item from Hopper, LocationType and LocationUUID are preserved from LoadFrom, HullCurrentHP HullMaxHP HullMaxRepairPercent are preserved from LoadFrom
    - Add Compile Include to test csproj
    - _Requirements: 3.1, 3.2, 3.4, 3.5, 3.6, 3.8, 3.9, 6.1, 6.11, 6.12_

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/ShipServicePropertyTests.cs
    - Reuse ValidShipGen() pattern from ViewModel property tests
    - **Property 8: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 11.3, 11.4, 11.5, 11.6, 11.9**
    - Add Compile Include to test csproj

  - [ ] 10.2 Write property test: Service.Create round-trip
    - **Property 9: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 12.2, 12.4, 12.8**

  - [ ] 10.3 Write property test: Service.Delete removes ship
    - **Property 10: Service.Delete Removes Ship**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.1, 13.2**

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for ShipService
    - Create OE2EmpireTracker.Tests/Services/ShipServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires ShipDataChanged event, Create fires ShipDataChanged event, Delete fires ShipDataChanged event, Update replaces Components list, Update replaces Cargo and Hopper, Create populates Name from request, CreateFromTemplate copies fields from template, CreateFromTemplate with non-existent template throws InvalidOperationException
    - Add Compile Include to test csproj
    - _Requirements: 11.9, 11.10, 12.2, 12.3, 12.7, 12.8, 13.4, 13.5, 14.3, 14.6_

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for Ship
    - Create OE2EmpireTracker.Tests/Services/ShipMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests, ShipTemplateMutationGuardTests pattern
    - First check: scan for direct Ship property sets (Name, UUID, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent), assert they only appear in ShipService.cs, Ship.cs, PlayerContext.cs (deserialization/migration), and test code
    - Second check: scan for direct Ship.Components list mutation (Add, Remove, Clear, Insert, Components =), assert they only appear in ShipService.cs, Ship.cs, and test code
    - Third check: scan for direct Ship.Cargo and Ship.Hopper mutation (AddItem, Remove, Clear, Cargo =, Hopper =), assert they only appear in ShipService.cs, Ship.cs, and test code
    - Verify FormShipInstance.cs does not directly set properties on Ship
    - Verify ShipViewModel.cs does not directly set properties on Ship
    - **Validates: Property 11**
    - **Validates: Requirements 18.1, 18.2, 18.3, 21.1, 21.2, 21.3**
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
- ShipViewModel has 5 scalar fields (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID) plus 3 hull HP fields plus Components list plus Cargo and Hopper ItemBags
- ShipService is simple: no write locks, no immediate operations, includes CreateFromTemplate method
- All operations (component changes, cargo/hopper add/remove, field edits) accumulate in the ViewModel edit buffer until Save
- ReadOnlyShip, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are already complete --- no gap fill needed
- ShipReferenceCounter checks 2 source types (delivery plans, build items)
- GetCurrentPlayerReadOnlyShips already exists in PlayerContext
- FindMutableShip needs to be added (reuses existing _shipCache)
- ShipDataChanged event and OnShipDataChanged already exist in PlayerContext
- ItemBag deep-copy requires creating new Item instances with all fields, including recursive Contents for crates
- Create from Template uses PlayerContext.FindShipTemplate to look up the template
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
