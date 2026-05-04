# Implementation Plan: BL-114 Market Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Market forms. Create MarketListingService as the sole mutator with CRUD for listings and sale recording. Create DTO request objects. Migrate FormMarket to use ReadOnly wrappers in grid Tags, route all mutations through the service. Migrate FormListingEdit to accept ReadOnlyMarketListing and return edited values as properties. Migrate FormRecordSale to accept ReadOnlyMarketListing. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessor
  - [ ] 1.1 Create MarketListingCreateRequest DTO
    - Create OE2EmpireTracker/Models/MarketListingCreateRequest.cs
    - Properties: ItemName (string), ItemType (ItemType.ItemTypeEnum), ItemReferenceID (string), StationUUID (string), Quantity (int), PricePerUnit (decimal), CurrentHP (int), MaxHP (int), MaxRepairPercent (decimal)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 4.1_

  - [ ] 1.2 Create MarketListingUpdateRequest DTO
    - Create OE2EmpireTracker/Models/MarketListingUpdateRequest.cs
    - Properties: ItemName (string), ItemType (ItemType.ItemTypeEnum), ItemReferenceID (string), StationUUID (string), Quantity (int), PricePerUnit (decimal), CurrentHP (int), MaxHP (int), MaxRepairPercent (decimal)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 5.1_

  - [ ] 1.3 Add FindMutableMarketListing internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint and FindMutableDeliveryRoute
    - Reuse existing _marketListingCache with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 2. Create MarketListingService with CRUD methods
  - [ ] 2.1 Create MarketListingService class
    - Create OE2EmpireTracker/Services/MarketListingService.cs
    - Constructor takes PlayerContext dependency
    - CreateListing(MarketListingCreateRequest): creates new MarketListing with generated UUID, sets OwnerUUID from current player, populates all fields from request, adds to PlayerContext, invalidates cache, persists via WriteContext(), fires MarketDataChanged event, returns ReadOnlyMarketListing
    - UpdateListing(string uuid, MarketListingUpdateRequest): looks up mutable entity via FindMutableMarketListing, applies all fields from request, invalidates cache, persists, fires event, returns ReadOnlyMarketListing. Throws InvalidOperationException if UUID not found.
    - DeleteListing(string uuid): looks up mutable entity, removes from PlayerContext via RemoveMarketListing, invalidates cache, persists, fires event. Returns silently if UUID empty or not found.
    - RecordSale(string listingUUID, int quantity, decimal pricePerUnit, string counterparty, string counterpartyFaction, string stationUUID): looks up mutable entity, delegates to static MarketService.RecordSale, adds transaction to PlayerContext, invalidates caches, persists, fires event, returns MarketTransaction (or null). Throws InvalidOperationException if listing not found.
    - Include NLog Logger declaration
    - No write locks (MarketListing has no ReaderWriterLockSlim)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 4.1-4.9, 5.1-5.8, 6.1-6.6, 7.1-7.9, 15.1_
- [ ] 3. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Migrate FormListingEdit to use ReadOnly input
  - [ ] 4.1 Modify FormListingEdit constructor and behavior
    - Change constructor to accept ReadOnlyMarketListing (null for new) instead of mutable MarketListing
    - Populate controls from ReadOnlyMarketListing properties (or defaults for new)
    - Change OK handler to set public properties instead of mutating the entity
    - Add public properties: EditedItemName, EditedItemType, EditedItemReferenceID, EditedStationUUID, EditedQuantity, EditedPricePerUnit, EditedCurrentHP, EditedMaxHP, EditedMaxRepairPercent
    - Remove mutable MarketListing reference
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [ ] 5. Migrate FormRecordSale to use ReadOnly input
  - [ ] 5.1 Modify FormRecordSale constructor
    - Change constructor to accept ReadOnlyMarketListing instead of mutable MarketListing
    - Populate display fields from ReadOnlyMarketListing properties
    - No behavioral change to return properties (SaleQuantity, SalePricePerUnit, Counterparty, CounterpartyFaction)
    - Remove mutable MarketListing reference
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 6. Migrate FormMarket to ReadOnly wrappers and service
  - [ ] 6.1 Replace mutable entity references with ReadOnly wrappers in listings grid
    - Change PopulateListingsGrid to use GetCurrentPlayerReadOnlyListings()
    - Store ReadOnlyMarketListing in row Tags instead of mutable MarketListing
    - Use ReadOnlyMarketListing properties for display values
    - _Requirements: 1.1, 1.2, 1.3, 3.1_

  - [ ] 6.2 Replace mutable entity references with ReadOnly wrappers in transactions grid
    - Change PopulateTransactionsGrid to use GetCurrentPlayerReadOnlyTransactions()
    - Use ReadOnlyMarketTransaction properties for filter comparisons and display values
    - _Requirements: 2.1, 2.2, 3.2_

  - [ ] 6.3 Wire Add button through service
    - Change CmdListingAdd_Click to call MarketListingService.CreateListing with default MarketListingCreateRequest
    - Remove direct MarketListing construction, AddMarketListing, InvalidateMarketListingCache, WriteContext, OnMarketDataChanged calls
    - _Requirements: 9.1, 9.2, 9.3_
  - [ ] 6.4 Wire Edit button through service
    - Change CmdListingEdit_Click to extract ReadOnlyMarketListing from row Tag
    - Open FormListingEdit with ReadOnlyMarketListing
    - On OK, build MarketListingUpdateRequest from dialog properties and call MarketListingService.UpdateListing
    - Remove direct entity mutation, InvalidateMarketListingCache, WriteContext, OnMarketDataChanged calls
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [ ] 6.5 Wire Delete button through service with reference protection
    - Change CmdListingDelete_Click to extract ReadOnlyMarketListing from row Tag
    - Check MarketListingReferenceCounter for transaction references
    - If references exist, show warning and prevent deletion
    - If no references, prompt confirmation and call MarketListingService.DeleteListing
    - Remove direct RemoveMarketListing, InvalidateMarketListingCache, WriteContext, OnMarketDataChanged calls
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

  - [ ] 6.6 Wire Record Sale button through service
    - Change CmdRecordSale_Click to extract ReadOnlyMarketListing from row Tag
    - Open FormRecordSale with ReadOnlyMarketListing
    - On OK, call MarketListingService.RecordSale with listing UUID and dialog values
    - If service returns null, show validation warning
    - Remove direct MarketService.RecordSale call, AddMarketTransaction, WriteContext, OnMarketDataChanged calls
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

  - [ ] 6.7 Add MarketListingService field to FormMarket
    - Add private MarketListingService _marketListingService field
    - Initialize in constructor with playerContext
    - _Requirements: 4.1, 5.1, 6.1, 7.1_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Build with zero errors and zero warnings
  - All existing tests pass
  - Ensure all tests pass, ask the user if questions arise.
- [ ] 8. Add Service property tests
  - [ ] 8.1 Write property test: CreateListing round-trip
    - Create OE2EmpireTracker.Tests/Services/MarketListingServicePropertyTests.cs
    - Create ValidMarketListingCreateRequestGen() generator producing random request values
    - **Property 1: CreateListing Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 4.2, 4.4, 4.9**
    - Add Compile Include to test csproj

  - [ ] 8.2 Write property test: UpdateListing round-trip
    - Create ValidMarketListingGen() generator producing random MarketListing entities
    - **Property 2: UpdateListing Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 5.3, 5.7**

  - [ ] 8.3 Write property test: DeleteListing removes listing
    - **Property 3: DeleteListing Removes Listing**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 6.2**

  - [ ] 8.4 Write property test: RecordSale decrements quantity and creates transaction
    - **Property 4: RecordSale Decrements Quantity**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 7.3, 7.4, 7.8**

- [ ] 9. Add Service unit tests
  - [ ] 9.1 Write unit tests for MarketListingService
    - Create OE2EmpireTracker.Tests/Services/MarketListingServiceTests.cs
    - Tests: CreateListing assigns non-empty UUID, CreateListing sets OwnerUUID to current player UUID, CreateListing fires MarketDataChanged event, UpdateListing with non-existent UUID throws InvalidOperationException, UpdateListing fires MarketDataChanged event, DeleteListing with empty UUID returns without error, DeleteListing with non-existent UUID returns without error, DeleteListing fires MarketDataChanged event, RecordSale with non-existent listing UUID throws InvalidOperationException, RecordSale with quantity exceeding availability returns null, RecordSale with valid inputs returns transaction with correct fields, RecordSale fires MarketDataChanged event, RecordSale adds transaction to PlayerContext
    - Add Compile Include to test csproj
    - _Requirements: 4.2, 4.3, 4.8, 5.3, 5.6, 5.8, 6.2, 6.5, 6.6, 7.2, 7.3, 7.4, 7.7, 7.8, 7.9_
- [ ] 10. Add mutation guard test
  - [ ] 10.1 Write mutation guard test for MarketListing
    - Create OE2EmpireTracker.Tests/Services/MarketListingMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests pattern
    - Scan for direct MarketListing property sets (ItemName, ItemType, Quantity, PricePerUnit, StationUUID, CurrentHP, MaxHP, MaxRepairPercent, OwnerUUID, UUID, ItemReferenceID)
    - Assert they only appear in: MarketListingService.cs, MarketService.cs (existing static), MarketListing.cs, PlayerContext.cs (deserialization/migration), and test code
    - Verify FormMarket.cs does not directly set properties on MarketListing
    - Verify FormListingEdit.cs does not directly set properties on MarketListing
    - Verify FormRecordSale.cs does not directly set properties on MarketListing
    - **Validates: Property 5**
    - **Validates: Requirements 15.1, 15.2, 15.3, 15.4, 18.1, 18.2**
    - Add Compile Include to test csproj

- [ ] 11. Final checkpoint --- Full build, all tests pass, audit clean
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
- No ViewModel needed --- FormMarket is grid-based with modal dialog editing, not a detail form
- No unsaved changes prompts --- each operation is atomic via the service
- MarketListingService is an instance service (like DeliveryRouteService), not static
- The existing static MarketService is retained and called by MarketListingService.RecordSale
- ReadOnlyMarketListing and ReadOnlyMarketTransaction are already complete --- no gap fill needed
- MarketListingReferenceCounter checks MarketTransaction.ListingUUID references (single source type)
- FormRecordSale already returns values as properties --- minimal change needed (just accept ReadOnly input)
- FormListingEdit needs more significant changes: accept ReadOnly input, expose edited values as properties instead of mutating entity
- GetCurrentPlayerReadOnlyListings and GetCurrentPlayerReadOnlyTransactions already exist in PlayerContext
- FindMutableMarketListing needs to be added (reuses existing _marketListingCache)
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx