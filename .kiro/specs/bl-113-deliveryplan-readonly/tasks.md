# Implementation Plan: BL-113 DeliveryPlan Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to DeliveryPlan. Rewrite DeliveryPlanViewModel as a disconnected edit buffer. Create DeliveryPlanService as the sole mutator with CRUD, item operations, execution operations, and trip splitting. Create DTO request objects. Migrate FormDeliveryRoute (Plan tab) and FormDeliveryExecution to route all mutations through the service. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessor
  - [ ] 1.1 Create DeliveryPlanUpdateRequest DTO
    - Create OE2EmpireTracker/Models/DeliveryPlanUpdateRequest.cs
    - Properties: Name (string), Stops (List of DeliveryPlanStop)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 10.1_

  - [ ] 1.2 Create DeliveryPlanCreateRequest DTO
    - Create OE2EmpireTracker/Models/DeliveryPlanCreateRequest.cs
    - Properties: Name (string), RouteUUID (string)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1_

  - [ ] 1.3 Create StopDestinationInfo DTO
    - Create OE2EmpireTracker/Models/StopDestinationInfo.cs
    - Properties: ColonyUUID (string), Sequence (int), DestinationType (DestinationType), DestinationUUID (string)
    - Used by AddDropOffItem, AddPickUpItem, RemoveDropOffItems, RemovePickUpItems service methods
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1, 12.1_

  - [ ] 1.4 Create DeliveryItemInfo DTO
    - Create OE2EmpireTracker/Models/DeliveryItemInfo.cs
    - Properties: ItemType (ItemType.ItemTypeEnum), BaseItemTypeID (string), Name (string), Quantity (int), ResourcePurity (string)
    - Used by AddDropOffItem, AddPickUpItem service methods
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1_

  - [ ] 1.5 Add FindMutableDeliveryPlan internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableDeliveryRoute and FindMutableColony
    - Reuse existing _deliveryPlanCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 18.1, 18.2, 18.3_

- [ ] 2. Rewrite DeliveryPlanViewModel as disconnected edit buffer
  - [ ] 2.1 Rewrite DeliveryPlanViewModel class
    - Rewrite OE2EmpireTracker/ViewModels/DeliveryPlanViewModel.cs as a disconnected edit buffer
    - Remove mutable DeliveryPlan reference and public Data property
    - Remove PlayerContext dependency from constructor (ViewModel takes ReadOnlyDeliveryPlan)
    - Add private fields: _original (ReadOnlyDeliveryPlan), _uuid, _ownerUUID, _routeUUID, _shipUUID, _completed, local Name (string), local _stops (List of DeliveryPlanStop)
    - Public properties: Name (get/set), Stops (List of DeliveryPlanStop), UUID, OwnerUUID, RouteUUID, ShipUUID, Completed, Original
    - LoadFrom(ReadOnlyDeliveryPlan): copies all scalar fields, deep-copies Stops (new DeliveryPlanStop instances with deep-copied DropOff and PickUp DeliveryItem lists), stores original snapshot
    - BuildUpdateRequest(): creates DeliveryPlanUpdateRequest from local state with deep-copied Stops
    - Retain GetOrCreateStop operating on local _stops list
    - Retain AddDropOffItem, AddPickUpItem, RemoveDropOffItems, RemovePickUpItems operating on local stops
    - Retain AutoFillCommodities, AutoFillFlatpacks, AutoFillManufacturingResources, AutoFillWorkers
    - AutoFill methods accept PlayerContext as parameter where needed (for blueprint/worker lookups)
    - Remove Save() method, FindOrCreateForRoute static method
    - Keep NLog Logger declaration
    - _Requirements: 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3_

- [ ] 3. Create DeliveryPlanService with all methods
  - [ ] 3.1 Create DeliveryPlanService class
    - Create OE2EmpireTracker/Services/DeliveryPlanService.cs
    - Constructor takes PlayerContext dependency
    - Create(string name, string routeUUID): creates new DeliveryPlan with generated UUID, sets OwnerUUID from current player, sets Name and RouteUUID, adds to PlayerContext, persists via WriteContext(), fires DeliveryDataChanged event, returns ReadOnlyDeliveryPlan
    - Delete(string uuid): looks up mutable entity via FindMutableDeliveryPlan, removes from PlayerContext via RemoveDeliveryPlan, persists, fires event. Returns silently if UUID empty or not found.
    - UpdatePlan(string uuid, DeliveryPlanUpdateRequest): looks up mutable entity, applies Name from request, replaces Stops list with deep copy from request, persists, fires event, returns ReadOnlyDeliveryPlan. Throws InvalidOperationException if UUID not found.
    - AddDropOffItem(string uuid, StopDestinationInfo, DeliveryItemInfo): looks up mutable plan, finds/creates stop, adds item to DropOff list, persists, fires event, returns ReadOnlyDeliveryPlan
    - AddPickUpItem(string uuid, StopDestinationInfo, DeliveryItemInfo): same as AddDropOffItem but for PickUp list
    - RemoveDropOffItems(string uuid, StopDestinationInfo, IEnumerable<int> indices): looks up mutable plan, finds stop, removes items at indices (descending order), persists, fires event, returns ReadOnlyDeliveryPlan
    - RemovePickUpItems(string uuid, StopDestinationInfo, IEnumerable<int> indices): same as RemoveDropOffItems but for PickUp list
    - MarkItemDelivered(string uuid, int stopSequence, int itemIndex, string listType, bool delivered): looks up mutable plan, finds stop by sequence, sets item Delivered flag, triggers side effects (DeliveryFulfillment methods, station hold updates), persists, auto-completes plan if all items delivered, returns ReadOnlyDeliveryPlan
    - MarkStopComplete(string uuid, int stopSequence): sets StopCompleted on stop, auto-completes plan if all items delivered, persists, returns ReadOnlyDeliveryPlan
    - MarkPlanComplete(string uuid): sets Completed on plan, persists, fires event, returns ReadOnlyDeliveryPlan
    - SetShipUUID(string uuid, string shipUUID): sets ShipUUID on plan, persists, returns ReadOnlyDeliveryPlan
    - SplitTrips(string uuid, decimal cargoCapacity, Func<string, ReadOnlyBlueprint> blueprintFinder): uses CargoVolumeService.SplitIntoTrips, creates new plans for additional trips, renames original with trip suffix, adds new plans to PlayerContext, persists, fires event, returns list of ReadOnlyDeliveryPlan
    - Include NLog Logger declaration
    - No write locks (single-threaded UI access only)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1-8.9, 9.1-9.5, 10.1-10.8, 11.1-11.6, 12.1-12.5, 13.1-13.10, 14.1-14.5, 15.1-15.5, 16.1-16.4, 17.1-17.7, 21.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate FormDeliveryRoute Plan tab to service
  - [ ] 5.1 Replace plan dropdown with ReadOnly wrappers
    - Change PopulatePlanDropdown to use PlayerContext.GetCurrentPlayerReadOnlyPlans() for display
    - Change CmbPlan_SelectedIndexChanged to extract ReadOnlyDeliveryPlan UUID and call service or load ViewModel
    - Remove direct mutable DeliveryPlan references from plan dropdown code paths
    - _Requirements: 1.1, 1.2, 1.3, 3.1_

  - [ ] 5.2 Wire ViewModel as edit buffer for plan data
    - Change CmbPlan_SelectedIndexChanged to create DeliveryPlanViewModel from ReadOnlyDeliveryPlan via LoadFrom
    - Change plan name display to read from ViewModel local fields
    - Change plan item grids to read from ViewModel local stops
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ] 5.3 Wire CmdNewPlan through service
    - Replace direct DeliveryPlan creation with DeliveryPlanService.Create(name, routeUUID)
    - After create, refresh plan dropdown and select the new plan
    - _Requirements: 19.1_

  - [ ] 5.4 Wire CmdDeletePlan through service
    - Replace direct PlayerContext.RemoveDeliveryPlan with DeliveryPlanService.Delete(uuid)
    - After delete, clear plan state and refresh dropdown
    - _Requirements: 19.2_

  - [ ] 5.5 Wire TxtPlanName through service
    - Replace direct planViewModel.Data.Name = ... with DeliveryPlanService.UpdatePlan
    - Persist name change immediately via service
    - _Requirements: 19.3_

  - [ ] 5.6 Wire item add/remove through service
    - Replace CmdAddDropOff_Click to call DeliveryPlanService.AddDropOffItem
    - Replace CmdAddPickUp_Click to call DeliveryPlanService.AddPickUpItem
    - Replace CmdRemoveDropOff_Click to call DeliveryPlanService.RemoveDropOffItems
    - Replace CmdRemovePickUp_Click to call DeliveryPlanService.RemovePickUpItems
    - _Requirements: 19.4, 19.5, 19.6, 19.7_

  - [ ] 5.7 Wire AutoFill through ViewModel + service
    - Keep ViewModel AutoFill methods for local item accumulation
    - After AutoFill, call DeliveryPlanService.UpdatePlan to persist
    - _Requirements: 19.8_

- [ ] 6. Migrate FormDeliveryExecution to service
  - [ ] 6.1 Replace mutable plan reference with service calls
    - Add DeliveryPlanService _deliveryPlanService field
    - Change CmbPlan_SelectedIndexChanged to store plan UUID instead of mutable DeliveryPlan reference
    - Change BuildExecution to read from ReadOnlyDeliveryPlan (from service return or PlayerContext)
    - _Requirements: 2.1, 2.2, 3.2_

  - [ ] 6.2 Wire DeliveryItem_CheckedChanged through service
    - Replace direct item.Delivered = chk.Checked with DeliveryPlanService.MarkItemDelivered
    - Remove direct calls to UpdateCommodityFulfillment, UpdateFlatpackStaging, UpdateWorkerDelivery, UpdateResourceDelivery, UpdateStationHold (service handles side effects)
    - After service call, refresh display from returned ReadOnlyDeliveryPlan
    - _Requirements: 20.1_

  - [ ] 6.3 Wire CompleteStop_Click through service
    - Replace direct stop.StopCompleted = true with DeliveryPlanService.MarkStopComplete
    - After service call, rebuild execution display
    - _Requirements: 20.2_

  - [ ] 6.4 Wire CmdCompletePlan_Click through service
    - Replace direct selectedPlan.Completed = true with DeliveryPlanService.MarkPlanComplete
    - After service call, clear execution and refresh plan dropdown
    - _Requirements: 20.3_

  - [ ] 6.5 Wire CmbShip_SelectedIndexChanged through service
    - Replace direct selectedPlan.ShipUUID = shipUUID with DeliveryPlanService.SetShipUUID
    - _Requirements: 20.4_

  - [ ] 6.6 Wire CmdSplitTrips_Click through service
    - Replace direct CreateSplitTripPlans with DeliveryPlanService.SplitTrips
    - Remove CreateSplitTripPlans method from form
    - After service call, refresh plan dropdown and rebuild execution
    - _Requirements: 20.5_

  - [ ] 6.7 Wire CmdDeletePlan_Click through service
    - Replace direct PlayerContext.RemoveDeliveryPlan with DeliveryPlanService.Delete
    - After service call, clear execution and refresh plan dropdown
    - _Requirements: 20.6_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Build with zero errors and zero warnings
  - All existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property test: LoadFrom round-trip preserves all fields
    - Create OE2EmpireTracker.Tests/ViewModels/DeliveryPlanViewModelPropertyTests.cs
    - Create ValidDeliveryPlanGen() generator producing random DeliveryPlan entities with random Name, UUID, OwnerUUID, RouteUUID, ShipUUID, Completed, and 0-5 DeliveryPlanStop entries (each with random ColonyUUID, Sequence, StopCompleted, DestinationType, DestinationUUID, and 0-3 DropOff/PickUp DeliveryItem entries)
    - **Property: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**
    - Add Compile Include to test csproj

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for DeliveryPlanViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/DeliveryPlanViewModelTests.cs
    - Tests: LoadFrom copies all scalar fields, LoadFrom deep-copies Stops, GetOrCreateStop finds existing stop, GetOrCreateStop creates new stop, AddDropOffItem adds to DropOff list, AddPickUpItem adds to PickUp list, RemoveDropOffItems removes by index, RemovePickUpItems removes by index, BuildUpdateRequest copies Name and Stops, AutoFill methods add items to local stops only
    - Add Compile Include to test csproj
    - _Requirements: 4.1, 4.2, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2_

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property test: Service.Create round-trip
    - Create OE2EmpireTracker.Tests/Services/DeliveryPlanServicePropertyTests.cs
    - Reuse ValidDeliveryPlanGen() pattern from ViewModel property tests
    - **Property 1: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 8.2, 8.4, 8.5, 8.9**
    - Add Compile Include to test csproj

  - [ ] 10.2 Write property test: Service.UpdatePlan round-trip
    - **Property 2: Service.UpdatePlan Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 10.3, 10.4, 10.7**

  - [ ] 10.3 Write property test: Service.Delete removes plan
    - **Property 3: Service.Delete Removes Plan**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 9.1, 9.2**

  - [ ] 10.4 Write property test: Service.MarkItemDelivered sets flag
    - **Property 4: Service.MarkItemDelivered Sets Flag**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 13.1, 13.2**

  - [ ] 10.5 Write property test: Service.MarkStopComplete sets flag
    - **Property 5: Service.MarkStopComplete Sets Flag**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 14.1, 14.2**

  - [ ] 10.6 Write property test: Service.MarkPlanComplete sets flag
    - **Property 6: Service.MarkPlanComplete Sets Flag**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 15.1, 15.2**

  - [ ] 10.7 Write property test: AddDropOffItem increases item count
    - **Property 7: AddDropOffItem Increases Item Count**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 11.2, 11.6**

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for DeliveryPlanService
    - Create OE2EmpireTracker.Tests/Services/DeliveryPlanServiceTests.cs
    - Tests: UpdatePlan with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Create sets RouteUUID from parameter, UpdatePlan fires DeliveryDataChanged event, Create fires DeliveryDataChanged event, Delete fires DeliveryDataChanged event, MarkItemDelivered sets Delivered flag, MarkStopComplete sets StopCompleted flag, MarkPlanComplete sets Completed flag, SetShipUUID sets ShipUUID field, AddDropOffItem adds item to stop, RemoveDropOffItems removes items from stop
    - Add Compile Include to test csproj
    - _Requirements: 8.2, 8.3, 8.9, 9.2, 9.5, 10.3, 10.7, 10.8, 11.2, 12.2, 13.2, 14.2, 15.2, 16.2_

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for DeliveryPlan
    - Create OE2EmpireTracker.Tests/Services/DeliveryPlanMutationGuardTests.cs
    - Follow DeliveryRouteMutationGuardTests, BlueprintMutationGuardTests, ColonyMutationGuardTests pattern
    - First check: scan for direct DeliveryPlan property sets (Name, ShipUUID, Completed, Stops), assert they only appear in DeliveryPlanService.cs, DeliveryPlan.cs, PlayerContext.cs (deserialization/migration), and test code
    - Second check: scan for direct DeliveryPlanStop mutation (StopCompleted =), assert they only appear in DeliveryPlanService.cs, DeliveryPlan.cs, and test code
    - Third check: scan for direct DeliveryItem.Delivered sets, assert they only appear in DeliveryPlanService.cs and test code
    - Fourth check: scan for direct DropOff.Add, PickUp.Add, assert they only appear in DeliveryPlanService.cs, DeliveryPlan.cs, DeliveryPlanViewModel.cs (local buffer), and test code
    - Verify FormDeliveryRoute.cs does not directly set properties on DeliveryPlan
    - Verify FormDeliveryExecution.cs does not directly set properties on DeliveryPlan
    - **Validates: Property 8**
    - **Validates: Requirements 21.1, 21.2, 21.3, 21.4, 24.1, 24.2, 24.3**
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - node .kiro/tools/audit.js reports no new findings
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New .cs files require Compile Include entries in the old-style .csproj
- DeliveryPlanViewModel is more complex than DeliveryRouteViewModel: nested 3-level structure (Plan -> Stops -> Items), AutoFill methods, and PlayerContext dependency for lookups
- DeliveryPlanService is more complex than DeliveryRouteService: CRUD plus item operations, execution operations with side effects, and trip splitting
- Two forms mutate DeliveryPlan: FormDeliveryRoute (Plan tab) for CRUD/items, FormDeliveryExecution for execution operations
- Execution operations (MarkItemDelivered, MarkStopComplete, MarkPlanComplete, SetShipUUID) persist immediately with side effects
- Plan tab operations (create, delete, rename, add/remove items) also persist immediately via the service (matching current behavior)
- AutoFill operations use the ViewModel for local accumulation, then persist via UpdatePlan
- ReadOnlyDeliveryPlan, ReadOnlyDeliveryPlanStop, and ReadOnlyDeliveryItem are already complete --- no gap fill needed
- GetCurrentPlayerReadOnlyPlans already exists in PlayerContext
- FindMutableDeliveryPlan needs to be added (reuses existing _deliveryPlanCache)
- No write locks on DeliveryPlan --- single-threaded UI access only
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
