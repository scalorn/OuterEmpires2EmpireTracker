# Implementation Plan: BL-112 DeliveryRoute Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the DeliveryRoute form. Rewrite DeliveryRouteViewModel as a disconnected edit buffer for the route Name and Stops list. Create DeliveryRouteService as the sole mutator with CRUD methods. Create DTO request objects. Migrate FormDeliveryRoute to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. All stop operations (add, remove, reorder) accumulate in the ViewModel edit buffer until Save. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [x] 1. DTO request models and PlayerContext accessors
  - [x] 1.1 Create DeliveryRouteUpdateRequest DTO
    - Create OE2EmpireTracker/Models/DeliveryRouteUpdateRequest.cs
    - Properties: Original (ReadOnlyDeliveryRoute), Name (string), Stops (List of RouteStop)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1_

  - [x] 1.2 Create DeliveryRouteCreateRequest DTO
    - Create OE2EmpireTracker/Models/DeliveryRouteCreateRequest.cs
    - Properties: Name (string), Stops (List of RouteStop)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 13.1_

  - [x] 1.3 Add FindMutableDeliveryRoute internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint and FindMutableColony
    - Reuse existing _deliveryRouteCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 15.1, 15.2, 15.3_

- [x] 2. Rewrite DeliveryRouteViewModel as disconnected edit buffer
  - [x] 2.1 Rewrite DeliveryRouteViewModel class
    - Rewrite OE2EmpireTracker/ViewModels/DeliveryRouteViewModel.cs as a disconnected edit buffer
    - Remove mutable DeliveryRoute reference and public Data property
    - Remove PlayerContext dependency from constructor (ViewModel is a plain edit buffer)
    - Add private fields: _original (ReadOnlyDeliveryRoute), _uuid, _ownerUUID, local Name (string), local _stops (List of RouteStop)
    - Public properties: Name (get/set), Stops (List of RouteStop), UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyDeliveryRoute): copies Name, deep-copies Stops (new RouteStop instances preserving all 6 fields), stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates DeliveryRouteUpdateRequest from local state with deep-copied Stops
    - BuildCreateRequest(): creates DeliveryRouteCreateRequest from local state with deep-copied Stops
    - IsDirty: compares Name and Stops against _original snapshot; for new routes returns true once Name is non-empty or Stops has entries
    - Retain stop manipulation methods (AddStop, RemoveStop, RemoveStops, MoveStopUp/Down, MoveStopsUp/Down) operating on local _stops list
    - Retain RenumberStops() helper operating on local _stops list
    - Remove GetFilteredRoutes, Save, Delete, SelectRoute methods
    - Keep NLog Logger declaration
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3. Create DeliveryRouteService with CRUD methods
  - [x] 3.1 Create DeliveryRouteService class
    - Create OE2EmpireTracker/Services/DeliveryRouteService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, DeliveryRouteUpdateRequest): looks up mutable entity via FindMutableDeliveryRoute, applies Name from request, replaces Stops list with deep copy (correct Sequence numbering), persists via WriteContext(), fires DeliveryDataChanged event, returns ReadOnlyDeliveryRoute. Throws InvalidOperationException if UUID not found.
    - Create(DeliveryRouteCreateRequest): creates new DeliveryRoute with generated UUID, sets OwnerUUID from current player, populates Name and Stops from request, adds to PlayerContext, persists, fires event, returns ReadOnlyDeliveryRoute
    - Delete(string uuid): looks up mutable entity, removes from PlayerContext via RemoveDeliveryRoute, persists, fires event. Returns silently if UUID empty or not found.
    - Include NLog Logger declaration
    - No write locks (DeliveryRoute has no ReaderWriterLockSlim)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 14.1, 14.2, 14.3, 14.4, 14.5, 18.1_

- [x] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Migrate FormDeliveryRoute to ReadOnly wrappers and service
  - [x] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateRouteList to store ReadOnlyDeliveryRoute in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyRoutes())
    - Change filter logic to use ReadOnlyDeliveryRoute properties for filter comparison
    - Remove any direct mutable DeliveryRoute references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 3.1_

  - [x] 5.2 Wire ViewModel as edit buffer
    - Add DeliveryRouteViewModel _viewModel field and DeliveryRouteService _deliveryRouteService field
    - Change selection handler to extract ReadOnlyDeliveryRoute from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields (Name, Stops) instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - _Requirements: 1.2, 4.1, 4.2, 5.1, 5.2, 5.3, 5.4_

  - [x] 5.3 Replace write-through with local-only ViewModel updates
    - Change TxtRouteName_TextChanged handler to set _viewModel.Name instead of entity Name
    - Change stop add/remove/reorder handlers to call _viewModel stop methods instead of entity mutation
    - Remove any direct WriteContext() calls from control change handlers
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _deliveryRouteService.Create(BuildCreateRequest()); else call _deliveryRouteService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyDeliveryRoute
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [x] 5.5 Wire Delete button through service with reference protection
    - Check DeliveryRouteReferenceCounter for references before delete (delivery plans, overflow rules, supply chain stages)
    - If TotalCount > 0, show warning message listing reference counts by type and prevent deletion
    - If no references, prompt for confirmation before calling _deliveryRouteService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 17.1, 17.2, 17.3, 17.4_

- [x] 6. Add unsaved changes prompts
  - [x] 6.1 Add unsaved changes prompt on selection change
    - In route list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current route selected
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 6.2 Add unsaved changes prompt on New button
    - In CmdNew_Click handler, check _viewModel.IsDirty before clearing form
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x] 6.3 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Show same three-button dialog
    - Cancel sets e.Cancel = true to prevent close
    - Handles both form close (X button) and application exit (MainWindow closing)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 11.1, 11.2_

- [x] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Build with zero errors and zero warnings
  - All existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Add ViewModel property tests
  - [x] 8.1 Write property test: LoadFrom round-trip preserves all fields
    - Create OE2EmpireTracker.Tests/ViewModels/DeliveryRouteViewModelPropertyTests.cs
    - Create ValidDeliveryRouteGen() generator producing random DeliveryRoute entities with random Name, UUID, OwnerUUID, and 0-10 RouteStop entries (each with random ColonyUUID, Sequence, DestinationType, DestinationUUID, Purpose, FuelEstimate)
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 4.1, 4.2, 4.3**
    - Add Compile Include to test csproj

  - [x] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 7.1, 7.4**

  - [x] 8.3 Write property test: IsDirty detects Name change
    - **Property 3: IsDirty Detects Name Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing Name to a different value causes IsDirty to return true
    - **Validates: Requirements 7.1, 7.2**

  - [x] 8.4 Write property test: IsDirty detects Stops change
    - **Property 4: IsDirty Detects Stops Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test adding a stop, removing a stop, or modifying a stop field causes IsDirty to return true
    - **Validates: Requirements 7.1, 7.3**

- [x] 9. Add ViewModel unit tests
  - [x] 9.1 Write unit tests for DeliveryRouteViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/DeliveryRouteViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new route with non-empty Name, IsDirty returns true for new route with stops, BuildUpdateRequest copies Name and Stops, BuildCreateRequest copies Name and Stops, UUID and OwnerUUID are preserved from LoadFrom, AddStop increases Stops count by one, RemoveStop decreases Stops count by one, MoveStopUp swaps adjacent stops, MoveStopDown swaps adjacent stops, RenumberStops assigns sequential Sequence values
    - Add Compile Include to test csproj
    - _Requirements: 4.1, 4.2, 7.1, 7.4, 7.5_

- [x] 10. Add Service property tests
  - [x] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/DeliveryRouteServicePropertyTests.cs
    - Reuse ValidDeliveryRouteGen() pattern from ViewModel property tests
    - **Property 5: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 12.3, 12.4, 12.7**
    - Add Compile Include to test csproj

  - [x] 10.2 Write property test: Service.Create round-trip
    - **Property 6: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.2, 13.4, 13.8**

  - [x] 10.3 Write property test: Service.Delete removes route
    - **Property 7: Service.Delete Removes Route**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 14.1, 14.2**

- [x] 11. Add Service unit tests
  - [x] 11.1 Write unit tests for DeliveryRouteService
    - Create OE2EmpireTracker.Tests/Services/DeliveryRouteServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires DeliveryDataChanged event, Create fires DeliveryDataChanged event, Delete fires DeliveryDataChanged event, Update replaces Stops list with correct Sequence numbering, Create populates Stops list from request
    - Add Compile Include to test csproj
    - _Requirements: 12.7, 12.8, 13.2, 13.3, 13.7, 13.8, 14.4, 14.5_

- [x] 12. Add mutation guard test
  - [x] 12.1 Write mutation guard test for DeliveryRoute
    - Create OE2EmpireTracker.Tests/Services/DeliveryRouteMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, SurveyMutationGuardTests, ColonyMutationGuardTests, PlayerProfileMutationGuardTests, and PricingPlanMutationGuardTests pattern
    - First check: scan for direct DeliveryRoute property sets (Name, UUID, OwnerUUID), assert they only appear in DeliveryRouteService.cs, DeliveryRoute.cs, PlayerContext.cs (deserialization/migration), and test code
    - Second check: scan for direct DeliveryRoute.Stops list mutation (Add, Remove, Clear, Insert, Stops =), assert they only appear in DeliveryRouteService.cs, DeliveryRoute.cs, and test code
    - Verify FormDeliveryRoute.cs does not directly set properties on DeliveryRoute
    - Verify DeliveryRouteViewModel.cs does not directly set properties on DeliveryRoute
    - **Validates: Property 8**
    - **Validates: Requirements 18.1, 18.2, 18.3, 21.1, 21.2**
    - Add Compile Include to test csproj

- [-] 13. Final checkpoint --- Full build, all tests pass, audit clean
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
- DeliveryRouteViewModel is simpler than ColonyViewModel: only 1 scalar field (Name) plus Stops list vs Colony's 3 scalar fields plus immediate structure/item/commodity operations
- DeliveryRouteService is simpler than ColonyService: no write locks, no immediate operations, no import method
- All stop operations (add, remove, reorder) accumulate in the ViewModel edit buffer until Save --- unlike Colony where structure operations are immediate service calls
- ReadOnlyDeliveryRoute and ReadOnlyRouteStop are already complete --- no gap fill needed
- DeliveryRouteReferenceCounter checks 3 source types (delivery plans, overflow rules, supply chain stages)
- DeliveryPlan management (Plan tab) is OUT OF SCOPE for BL-112 and remains as-is
- GetCurrentPlayerReadOnlyRoutes already exists in PlayerContext
- FindMutableDeliveryRoute needs to be added (reuses existing _deliveryRouteCache)
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
