# Implementation Plan: BL-109 Colony Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern (established in BL-108 blueprints, BL-110 surveys, BL-111 player profiles, and BL-123 pricing plans) to the Colony form. Rewrite ColonyViewModel as a scoped disconnected edit buffer for colony-level scalar fields only (PlanetName, ColonyName, SystemName). Create ColonyService as the sole mutator with CRUD, import, structure, item, and commodity request methods --- all with ReaderWriterLockSlim concurrency. Create DTO request objects. Migrate FormColonyV2 to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. Structure/item/commodity operations are immediate service calls bypassing the edit buffer. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [x] 1. DTO request models and PlayerContext accessors
  - [x] 1.1 Create ColonyUpdateRequest DTO
    - Create OE2EmpireTracker/Models/ColonyUpdateRequest.cs
    - Properties: Original (ReadOnlyColony), PlanetName (string), ColonyName (string), SystemName (string)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 13.1_

  - [x] 1.2 Create ColonyCreateRequest DTO
    - Create OE2EmpireTracker/Models/ColonyCreateRequest.cs
    - Properties: PlanetName (string), ColonyName (string), SystemName (string)
    - No UUID (service assigns it), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 14.1_

  - [x] 1.3 Add GetCurrentPlayerReadOnlyColonies method to PlayerContext
    - Return List of ReadOnlyColony wrapping all colonies owned by the current player
    - Follow the same pattern as GetCurrentPlayerReadOnlySurveys and GetCurrentPlayerReadOnlyBlueprints
    - _Requirements: 2.1, 2.2_

  - [x] 1.4 Add FindMutableColony internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint and FindMutableSurvey
    - Reuse existing _colonyCache dictionary with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 25.1, 25.2, 25.3_

- [x] 2. Rewrite ColonyViewModel as disconnected edit buffer
  - [x] 2.1 Rewrite ColonyViewModel class
    - Rewrite OE2EmpireTracker/ViewModels/ColonyViewModel.cs as a disconnected edit buffer
    - Remove mutable Colony reference and public Data property
    - Add private fields: _original (ReadOnlyColony), _uuid, _ownerUUID, and local edit fields for PlanetName, ColonyName, SystemName
    - Public properties: PlanetName, ColonyName, SystemName with get/set, UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyColony): copies scalar fields, stores original snapshot for dirty comparison
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates ColonyUpdateRequest from local state
    - BuildCreateRequest(): creates ColonyCreateRequest from local state
    - IsDirty: compares PlanetName, ColonyName, SystemName against _original; for new colonies (_original == null), returns true once any field has non-default value
    - Keep NLog Logger declaration
    - _Requirements: 3.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3. Create ColonyService with CRUD and import methods
  - [x] 3.1 Create ColonyService class with Update, Create, Delete
    - Create OE2EmpireTracker/Services/ColonyService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, ColonyUpdateRequest): looks up mutable entity via FindMutableColony, acquires write lock via TryEnterWriteLock with Colony.WriteLockTimeoutMs timeout, applies PlanetName/ColonyName/SystemName from request, releases lock in finally, persists via WriteContext(), fires ColonyDataChanged event, returns ReadOnlyColony. Throws InvalidOperationException if UUID not found. Throws TimeoutException if write lock times out.
    - Create(ColonyCreateRequest): creates new Colony with generated UUID, sets OwnerUUID from current player, populates scalar fields from request, adds to PlayerContext, persists, fires event, returns ReadOnlyColony
    - Delete(string uuid): removes Colony from PlayerContext via RemoveColony, persists, fires event. Returns silently if UUID empty or not found.
    - Include NLog Logger declaration
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 15.1, 15.2, 15.3, 15.4, 15.5_

  - [x] 3.2 Add Import method to ColonyService
    - Import(Colony tempColony, EmpireContext empireContext): searches existing colonies by PlanetName (case-insensitive) for match; if found acquires write lock and calls ColonyParser.ProcessHtml on existing colony; if not found creates new Colony with generated UUID and current player OwnerUUID; sets LastImportDateTime to current UTC time; persists via WriteContext(); fires ColonyDataChanged event; returns ReadOnlyColony
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7, 16.8_

- [x] 4. Add immediate structure operations to ColonyService
  - [x] 4.1 Add AddStructure method
    - AddStructure(string colonyUUID, string flatpackBlueprintUUID): acquires write lock, creates new ColonyStructure with generated UUID, flatpack blueprint UUID, next DisplaySequence for type, next BuildQueueSequence, adds to colony Structures list, releases lock, persists, fires event
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6_

  - [x] 4.2 Add RemoveStructure method
    - RemoveStructure(string colonyUUID, string structureUUID): acquires write lock, finds structure by UUID, removes from colony Structures list, releases lock, persists, fires event. No-op if structure not found.
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5_

- [x] 5. Add immediate item operations to ColonyService
  - [x] 5.1 Add AddItem method
    - AddItem(string colonyUUID, Item item): acquires write lock, adds item to colony ItemBag, releases lock, persists, fires event
    - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5_

  - [x] 5.2 Add RemoveItem method
    - RemoveItem(string colonyUUID, string itemUUID): acquires write lock, removes item from colony ItemBag, releases lock, persists, fires event
    - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5_

  - [x] 5.3 Add UpdateItem method
    - UpdateItem(string colonyUUID, string itemUUID, int newQuantity): acquires write lock, updates item quantity in colony ItemBag, releases lock, persists, fires event
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5_

- [x] 6. Add immediate commodity request operations to ColonyService
  - [x] 6.1 Add AddCommodityRequest method
    - AddCommodityRequest(string colonyUUID, string commodityName, int requested, DateTime? needBy): acquires write lock, adds CommodityRequested to colony Commodities list, releases lock, persists, fires event
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5_

  - [x] 6.2 Add RemoveCommodityRequest method
    - RemoveCommodityRequest(string colonyUUID, string commodityName): acquires write lock, removes CommodityRequested from colony Commodities list by name, releases lock, persists, fires event
    - _Requirements: 23.1, 23.2, 23.3, 23.4, 23.5_

  - [x] 6.3 Add UpdateCommodityRequest method
    - UpdateCommodityRequest(string colonyUUID, string commodityName, int requested, int delivered, DateTime needBy): acquires write lock, updates CommodityRequested fields, releases lock, persists, fires event
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5_

- [x] 7. Checkpoint --- Verify new classes compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Migrate FormColonyV2 list view to ReadOnly wrappers
  - [x] 8.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateColonyList to store ReadOnlyColony in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlyColonies())
    - Change filter logic to use ReadOnlyColony properties for filter comparison
    - Remove any direct mutable Colony references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 3.1_

  - [x] 8.2 Wire ViewModel as edit buffer
    - Add ColonyViewModel _viewModel field and ColonyService _colonyService field
    - Change selection handler to extract ReadOnlyColony from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - _Requirements: 1.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4_

  - [x] 8.3 Replace write-through with local-only ViewModel updates
    - Change text box TextChanged handlers (txtPlanetName, txtColonyName, txtSystemName) to set _viewModel local fields instead of entity fields
    - Remove any direct WriteContext() calls from control change handlers for scalar fields
    - _Requirements: 6.1, 6.2_

  - [x] 8.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _colonyService.Create(BuildCreateRequest()); else call _colonyService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlyColony
    - Enable Save button only when _viewModel.IsDirty is true
    - _Requirements: 26.1, 26.2, 26.3, 26.4, 7.5_

  - [x] 8.5 Wire Delete button through service with reference protection
    - Check ColonyReferenceCounter for references before delete (delivery routes, delivery plans, build plans, supply chains, overflow rules)
    - If TotalCount > 0, show warning message listing reference counts by type and prevent deletion
    - If no references, prompt for confirmation before calling _colonyService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 27.1, 27.2, 27.3, 27.4_

  - [x] 8.6 Wire Import button through service
    - Validate clipboard content (HTML present, correct content type, player selected)
    - Call ColonyParser.ParseClipboardToTemp to get temporary Colony object
    - Call _colonyService.Import(tempColony, empireContext) instead of directly creating/merging entities
    - After import, refresh list view, select imported colony, load into ViewModel
    - _Requirements: 28.1, 28.2, 28.3, 28.4_

  - [x] 8.7 Wire immediate structure operations through service
    - Change AddStructure handler to call _colonyService.AddStructure(colonyUUID, flatpackUUID)
    - Change RemoveStructure handler to call _colonyService.RemoveStructure(colonyUUID, structureUUID)
    - Remove direct entity mutation for structure operations
    - _Requirements: 17.1, 18.1, 29.1_

  - [x] 8.8 Wire immediate item operations through service
    - Change AddItem handler to call _colonyService.AddItem(colonyUUID, item)
    - Change RemoveItem handler to call _colonyService.RemoveItem(colonyUUID, itemUUID)
    - Change UpdateItem handler to call _colonyService.UpdateItem(colonyUUID, itemUUID, newQuantity)
    - Remove direct entity mutation for item operations
    - _Requirements: 19.1, 20.1, 21.1, 29.1_

  - [x] 8.9 Wire immediate commodity request operations through service
    - Change AddCommodityRequest handler to call _colonyService.AddCommodityRequest(...)
    - Change RemoveCommodityRequest handler to call _colonyService.RemoveCommodityRequest(...)
    - Change UpdateCommodityRequest handler to call _colonyService.UpdateCommodityRequest(...)
    - Remove direct entity mutation for commodity request operations
    - _Requirements: 22.1, 23.1, 24.1, 29.1_

- [x] 9. Add unsaved changes prompts
  - [x] 9.1 Add unsaved changes prompt on selection change
    - In colony list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current colony selected
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 9.2 Add unsaved changes prompt on New button
    - In New handler, check _viewModel.IsDirty before clearing form
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x] 9.3 Add unsaved changes prompt on Import button
    - In Import handler, check _viewModel.IsDirty before proceeding with import
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [x] 9.4 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Show same three-button dialog
    - Cancel sets e.Cancel = true to prevent close
    - Handles both form close (X button) and application exit (MainWindow closing)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 12.1, 12.2_

- [x] 10. Checkpoint --- Verify form migration compiles and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Add ViewModel property tests
  - [x] 11.1 Write property test: LoadFrom round-trip preserves all scalar fields
    - Create OE2EmpireTracker.Tests/ViewModels/ColonyViewModelPropertyTests.cs
    - Create ValidColonyGen() generator producing random Colony entities with random string scalar fields (PlanetName, ColonyName, SystemName), random UUID and OwnerUUID, empty Structures list, empty ItemBag, empty Commodities list
    - **Property 1: LoadFrom Round-Trip Preserves All Scalar Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 4.1, 4.2, 4.3**
    - Add Compile Include to test csproj

  - [x] 11.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 7.1, 7.3**

  - [x] 11.3 Write property test: IsDirty detects any single scalar field change
    - **Property 3: IsDirty Detects Any Single Scalar Field Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing each scalar field (PlanetName, ColonyName, SystemName) individually to a different value
    - **Validates: Requirements 7.1, 7.2**

- [x] 12. Add ViewModel unit tests
  - [x] 12.1 Write unit tests for ColonyViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ColonyViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new colony with non-default PlanetName, BuildUpdateRequest copies all scalar fields, BuildCreateRequest copies all scalar fields, UUID and OwnerUUID are preserved from LoadFrom
    - Add Compile Include to test csproj
    - _Requirements: 4.1, 4.2, 7.1, 7.4, 7.5_

- [x] 13. Add Service property tests
  - [x] 13.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/ColonyServicePropertyTests.cs
    - Reuse ValidColonyGen() pattern from ViewModel property tests
    - **Property 4: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.4, 13.7**
    - Add Compile Include to test csproj

  - [x] 13.2 Write property test: Service.Create round-trip
    - **Property 5: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 14.2, 14.4, 14.8**

  - [x] 13.3 Write property test: Service.Delete removes colony
    - **Property 6: Service.Delete Removes Colony**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 15.1, 15.2**

  - [x] 13.4 Write property test: Service.AddStructure increases structure count
    - **Property 7: Service.AddStructure Increases Structure Count**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 17.3, 17.4**

  - [x] 13.5 Write property test: Service.RemoveStructure decreases structure count
    - **Property 8: Service.RemoveStructure Decreases Structure Count**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 18.3**

- [x] 14. Add Service unit tests
  - [x] 14.1 Write unit tests for ColonyService
    - Create OE2EmpireTracker.Tests/Services/ColonyServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires ColonyDataChanged event, Create fires ColonyDataChanged event, Delete fires ColonyDataChanged event, AddStructure creates structure with correct flatpack UUID, AddStructure assigns next DisplaySequence for type, RemoveStructure removes correct structure by UUID, AddItem adds item to colony ItemBag, RemoveItem removes item from colony ItemBag, UpdateItem changes item quantity, AddCommodityRequest adds commodity to colony, RemoveCommodityRequest removes commodity from colony, UpdateCommodityRequest updates commodity fields, Write lock timeout throws TimeoutException
    - Add Compile Include to test csproj
    - _Requirements: 13.7, 13.8, 14.2, 14.3, 14.7, 14.8, 15.4, 15.5, 17.3, 17.4, 18.3, 19.3, 20.3, 21.3, 22.3, 23.3, 24.3_

- [x] 15. Add mutation guard test
  - [x] 15.1 Write mutation guard test for Colony
    - Create OE2EmpireTracker.Tests/Services/ColonyMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, SurveyMutationGuardTests, PlayerProfileMutationGuardTests, and PricingPlanMutationGuardTests pattern
    - First check: scan for direct Colony scalar property sets (PlanetName, ColonyName, SystemName), assert they only appear in ColonyService.cs, ColonyParser.cs, Colony.cs (including ProcessColony), JSON deserialization, migration code, and test code
    - Second check: scan for direct Colony.Structures list mutation (Add, Remove, Clear), assert they only appear in ColonyService.cs, ColonyParser.cs, Colony.cs, JSON deserialization, and test code
    - Verify FormColonyV2.cs does not directly set properties on Colony
    - Verify ColonyViewModel.cs does not directly set properties on Colony
    - **Validates: Requirements 29.1, 29.2, 29.3, 32.1, 32.2**
    - Add Compile Include to test csproj

- [~] 16. Final checkpoint --- Full build, all tests pass, audit clean
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
- ColonyViewModel is simpler than SurveyViewModel: only 3 scalar fields (PlanetName, ColonyName, SystemName) vs Survey's 10+ fields plus Resources and Properties dictionaries
- ColonyService is more complex than SurveyService due to: ReaderWriterLockSlim concurrency, immediate structure/item/commodity operations, and import merge via ColonyParser.ProcessHtml
- Structure, item, and commodity operations are immediate service calls that bypass the ViewModel edit buffer entirely
- The ViewModel edit buffer covers ONLY colony-level scalar fields --- this is a key difference from Survey
- All service mutations follow the write lock pattern: TryEnterWriteLock with timeout, mutation in try block, ExitWriteLock in finally block
- Background processing (Colony.ProcessColony) continues to mutate directly under its own write lock --- not wrapped by the service
- ReadOnlyColony is already complete --- no gap fill needed (unlike Survey which needed 4 new properties)
- ColonyReferenceCounter checks 5 source types (delivery routes, delivery plans, build plans, supply chains, overflow rules) vs Survey's 2
- Build with MSBuild: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Run tests with vstest.console: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx