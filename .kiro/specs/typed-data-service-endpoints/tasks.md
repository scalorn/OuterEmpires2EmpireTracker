# Implementation Plan: Typed Data Service Endpoints

## Overview

Replace raw JSON proxy endpoints with strongly-typed CRUD endpoints for 19 domain entity types. Uses a generic `TypedEndpointBase<TEntity, TCreate, TUpdate>` abstract base class, one endpoint file per entity, typed storage methods on `IStorageBackend`, and `PaginationHelper` for collection responses. Implementation language: C#.

## Tasks

- [x] 1. Infrastructure: PaginatedResponse model and PaginationHelper
  - [x] 1.1 Create PaginatedResponse<T> in Common_Library
    - Create `OE2EmpireTracker.Common/Models/PaginatedResponse.cs`
    - Generic class with Items, Total, Limit, Offset properties
    - Dual JSON annotations (Newtonsoft + STJ)
    - _Requirements: 29.1, 29.4_
  - [x] 1.2 Create PaginationHelper in Server
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/PaginationHelper.cs`
    - Static method `ApplyPagination<T>` with MaxLimit=500 cap
    - Returns raw array when no params, PaginatedResponse when params present
    - Validates negative values → 400
    - _Requirements: 29.1, 29.2, 29.3, 29.5, 29.6_

- [x] 2. Infrastructure: TypedEndpointBase abstract class
  - [x] 2.1 Create TypedEndpointBase<TEntity, TCreate, TUpdate> (Part 1 — class skeleton and abstract members)
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/TypedEndpointBase.cs`
    - Abstract properties: EntityTypeName, RoutePrefix
    - Abstract methods: ValidateCreate, ValidateUpdate, ApplyCreate, ApplyUpdate
    - Virtual method: HandleCreateDedup
    - _Requirements: 2.5, 3.1, 3.2_
  - [x] 2.2 Implement TypedEndpointBase handler methods (Part 2 — HandleGetAll, HandleGetOne)
    - HandleGetAll: auth check, storage call, pagination
    - HandleGetOne: auth check, UUID validation, storage call, 404 handling
    - _Requirements: 2.2, 2.3, 2.4, 3.5, 3.6, 23.4, 23.5_
  - [x] 2.3 Implement TypedEndpointBase handler methods (Part 3 — HandleCreate, HandleUpdate, HandleDelete)
    - HandleCreate: auth, body read, validation, dedup hook, storage, logging, event dispatch, rollback on failure
    - HandleUpdate: auth, body read, validation, storage, logging, event dispatch, rollback
    - HandleDelete: auth, UUID validation, storage, logging, event dispatch, rollback
    - _Requirements: 25.1, 25.2, 25.3, 25.5, 26.1, 26.3_

- [x] 3. Storage interface extension (Group 1: Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute)
  - [x] 3.1 Add typed methods to IStorageBackend for Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute
    - Add 4 methods × 5 entities = 20 method signatures to IStorageBackend interface
    - _Requirements: 22.1, 22.2_
  - [x] 3.2 Implement typed methods in JsonFileStorageBackend for Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute
    - Implement GetAll, Get, Upsert, Delete for each entity type
    - Read/write JSON files per entity type
    - _Requirements: 22.3, 22.7, 22.8_

- [x] 4. Storage interface extension (Group 2: DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction)
  - [x] 4.1 Add typed methods to IStorageBackend for DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction
    - Add 4 methods × 5 entities = 20 method signatures to IStorageBackend interface
    - _Requirements: 22.1, 22.2_
  - [x] 4.2 Implement typed methods in JsonFileStorageBackend for DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction
    - Implement GetAll, Get, Upsert, Delete for each entity type
    - _Requirements: 22.3, 22.7, 22.8_

- [x] 5. Storage interface extension (Group 3: PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain)
  - [x] 5.1 Add typed methods to IStorageBackend for PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain
    - Add 4 methods × 5 entities = 20 method signatures to IStorageBackend interface
    - _Requirements: 22.1, 22.2_
  - [x] 5.2 Implement typed methods in JsonFileStorageBackend for PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain
    - Implement GetAll, Get, Upsert, Delete for each entity type
    - _Requirements: 22.3, 22.7, 22.8_

- [x] 6. Storage interface extension (Group 4: Asteroid, Station, Faction, ExternalCharacter)
  - [x] 6.1 Add typed methods to IStorageBackend for Asteroid, Station, Faction, ExternalCharacter
    - Add 4 methods × 4 entities = 16 method signatures to IStorageBackend interface
    - _Requirements: 22.1, 22.2_
  - [x] 6.2 Implement typed methods in JsonFileStorageBackend for Asteroid, Station, Faction, ExternalCharacter
    - Implement GetAll, Get, Upsert, Delete for each entity type
    - _Requirements: 22.3, 22.7, 22.8_

- [x] 7. Storage stubs: Sqlite, Postgres, Dynamo backends
  - [x] 7.1 Add NotImplementedException stubs for Group 1 entities (Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute) to all 3 backends
    - Add 20 stub methods (4 per entity × 5 entities) to SqliteStorageBackend, PostgresStorageBackend, DynamoStorageBackend
    - Each method throws NotImplementedException with descriptive message
    - 3 files modified, ~80 LOC per file
    - _Requirements: 22.4, 22.5, 22.6_
  - [x] 7.2 Add NotImplementedException stubs for Group 2 entities (DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction) to all 3 backends
    - Add 20 stub methods (4 per entity × 5 entities) to SqliteStorageBackend, PostgresStorageBackend, DynamoStorageBackend
    - Each method throws NotImplementedException with descriptive message
    - 3 files modified, ~80 LOC per file
    - _Requirements: 22.4, 22.5, 22.6_
  - [x] 7.3 Add NotImplementedException stubs for Group 3 entities (PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain) to all 3 backends
    - Add 20 stub methods (4 per entity × 5 entities) to SqliteStorageBackend, PostgresStorageBackend, DynamoStorageBackend
    - Each method throws NotImplementedException with descriptive message
    - 3 files modified, ~80 LOC per file
    - _Requirements: 22.4, 22.5, 22.6_
  - [x] 7.4 Add NotImplementedException stubs for Group 4 entities (Asteroid, Station, Faction, ExternalCharacter) to all 3 backends
    - Add 16 stub methods (4 per entity × 4 entities) to SqliteStorageBackend, PostgresStorageBackend, DynamoStorageBackend
    - Each method throws NotImplementedException with descriptive message
    - 3 files modified, ~64 LOC per file
    - _Requirements: 22.4, 22.5, 22.6_

- [x] 8. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Simple CRUD endpoint: Asteroid
  - [x] 9.1 Create AsteroidEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/AsteroidEndpoints.cs`
    - Inherit TypedEndpointBase<Asteroid, AsteroidCreateRequest, AsteroidUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapAsteroidEndpoints extension method with GET/POST/PUT/DELETE routes
    - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6_

- [x] 10. Simple CRUD endpoint: Station
  - [x] 10.1 Create StationEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/StationEndpoints.cs`
    - Inherit TypedEndpointBase<Station, StationCreateRequest, StationUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapStationEndpoints extension method
    - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5, 20.6_

- [x] 11. Simple CRUD endpoint: PricingPlan
  - [x] 11.1 Create PricingPlanEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/PricingPlanEndpoints.cs`
    - Inherit TypedEndpointBase<PricingPlan, PricingPlanCreateRequest, PricingPlanUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapPricingPlanEndpoints extension method
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

- [x] 12. Simple CRUD endpoint: StockPlan
  - [x] 12.1 Create StockPlanEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/StockPlanEndpoints.cs`
    - Inherit TypedEndpointBase<StockPlan, StockPlanCreateRequest, StockPlanUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapStockPlanEndpoints extension method
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_

- [x] 13. Simple CRUD endpoint: StockProfile
  - [x] 13.1 Create StockProfileEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/StockProfileEndpoints.cs`
    - Inherit TypedEndpointBase<StockProfile, StockProfileCreateRequest, StockProfileUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapStockProfileEndpoints extension method
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6_

- [x] 14. Simple CRUD endpoint: ShipTemplate
  - [x] 14.1 Create ShipTemplateEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/ShipTemplateEndpoints.cs`
    - Inherit TypedEndpointBase<ShipTemplate, ShipTemplateCreateRequest, ShipTemplateUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapShipTemplateEndpoints extension method
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

- [x] 15. Simple CRUD endpoint: DeliveryRoute
  - [x] 15.1 Create DeliveryRouteEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/DeliveryRouteEndpoints.cs`
    - Inherit TypedEndpointBase<DeliveryRoute, DeliveryRouteCreateRequest, DeliveryRouteUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - Deep-copy and renumber stops on create/update
    - MapDeliveryRouteEndpoints extension method
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 16. Simple CRUD endpoint: PlayerProfile
  - [x] 16.1 Create PlayerProfileEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/PlayerProfileEndpoints.cs`
    - Inherit TypedEndpointBase<PlayerProfile, PlayerProfileCreateRequest, PlayerProfileUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapPlayerProfileEndpoints extension method
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_
  - [x] 16.2 Add import endpoint to PlayerProfileEndpoints
    - POST /profiles/import — full object import/merge logic
    - Return 200 if merged, 201 if new
    - _Requirements: 7.7_

- [x] 17. Simple CRUD endpoint: Survey
  - [x] 17.1 Create SurveyEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/SurveyEndpoints.cs`
    - Inherit TypedEndpointBase<Survey, SurveyCreateRequest, SurveyUpdateRequest>
    - Implement ValidateCreate (PlanetName required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapSurveyEndpoints extension method
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  - [x] 17.2 Add import endpoint to SurveyEndpoints
    - POST /surveys/import — full object import/merge with asteroid auto-linking
    - Return 200 if merged, 201 if new
    - _Requirements: 6.7_

- [x] 18. Simple CRUD endpoints: Faction and ExternalCharacter
  - [x] 18.1 Create FactionContactEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/FactionContactEndpoints.cs`
    - MapFactionContactEndpoints extension method with both /factions and /contacts route groups
    - Faction: ValidateCreate (Name required), standard CRUD
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6_
  - [x] 18.2 Add ExternalCharacter routes to FactionContactEndpoints
    - /contacts route group: GET all, GET one, POST, PUT, DELETE
    - ValidateCreate (Name required)
    - _Requirements: 21.7, 21.8, 21.9, 21.10, 21.11, 21.12_

- [x] 19. Simple CRUD endpoint: SupplyChain
  - [x] 19.1 Create SupplyChainEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/SupplyChainEndpoints.cs`
    - Inherit TypedEndpointBase<SupplyChain, SupplyChainCreateRequest, SupplyChainUpdateRequest>
    - Deep-copy and renumber stages on create/update
    - MapSupplyChainEndpoints extension method
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6_

- [x] 20. Simple CRUD endpoint: BuildPlan
  - [x] 20.1 Create BuildPlanEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/BuildPlanEndpoints.cs`
    - Inherit TypedEndpointBase<BuildPlan, BuildPlanCreateRequest, BuildPlanUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapBuildPlanEndpoints extension method
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6_
  - [x] 20.2 Add generate-colony-items action endpoint to BuildPlanEndpoints
    - POST /build-plans/{entityUuid}/generate-colony-items
    - Input: { colonyUUID } — scans colony for unstaged structures
    - Output: { itemsAdded: int }
    - _Requirements: 17.7_

- [x] 21. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 22. Complex endpoint: Ship (CRUD + from-template)
  - [x] 22.1 Create ShipEndpoints.cs
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/ShipEndpoints.cs`
    - Inherit TypedEndpointBase<Ship, ShipCreateRequest, ShipUpdateRequest>
    - Implement ValidateCreate (Name required), ValidateUpdate, ApplyCreate, ApplyUpdate
    - MapShipEndpoints extension method
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_
  - [x] 22.2 Add from-template action endpoint to ShipEndpoints
    - POST /ships/from-template — creates ship from template UUID
    - Validate templateUUID required, lookup template (404 if not found)
    - Return created Ship (201)
    - _Requirements: 10.7_

- [ ] 23. Complex endpoint: Blueprint (CRUD)
  - [x] 23.1 Create BlueprintEndpoints.cs — standard CRUD
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/BlueprintEndpoints.cs`
    - Inherit TypedEndpointBase<Blueprint, BlueprintCreateRequest, BlueprintUpdateRequest>
    - ValidateCreate (Name, BluePrintType required)
    - MapBlueprintEndpoints extension method
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_
  - [x] 23.2 Add import and move actions to BlueprintEndpoints
    - POST /blueprints/import — full object import/merge (200 if merged, 201 if new)
    - POST /blueprints/{entityUuid}/move-to-global — moves to global scope
    - POST /blueprints/{entityUuid}/move-to-player — moves to player scope
    - _Requirements: 5.8, 5.9, 5.10_

- [x] 24. Complex endpoint: Colony (CRUD with dedup)
  - [x] 24.1 Create ColonyEndpoints.cs — standard CRUD with dedup
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/ColonyEndpoints.cs`
    - Inherit TypedEndpointBase<Colony, ColonyCreateRequest, ColonyUpdateRequest>
    - ValidateCreate (PlanetName, ColonyName required)
    - Override HandleCreateDedup for PlanetName+SystemName case-insensitive matching
    - MapColonyEndpoints extension method
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 31.1, 31.2, 31.3, 31.4_
  - [x] 24.2 Add colony sub-resource endpoints: structures
    - POST /colonies/{colonyUuid}/structures — add structure (flatpackBlueprintUUID required)
    - DELETE /colonies/{colonyUuid}/structures/{structureUuid} — remove structure
    - _Requirements: 4.8, 4.9_
  - [x] 24.3 Add colony sub-resource endpoints: items
    - POST /colonies/{colonyUuid}/items — add item
    - DELETE /colonies/{colonyUuid}/items/{itemUuid} — remove item
    - PUT /colonies/{colonyUuid}/items/{itemUuid} — update quantity
    - _Requirements: 4.10, 4.11, 4.12_
  - [x] 24.4 Add colony sub-resource endpoints: commodity-requests
    - POST /colonies/{colonyUuid}/commodity-requests — add request (commodityName required)
    - DELETE /colonies/{colonyUuid}/commodity-requests/{commodityName} — remove request
    - PUT /colonies/{colonyUuid}/commodity-requests/{commodityName} — update request
    - _Requirements: 4.13, 4.14, 4.15_

- [ ] 25. Complex endpoint: MarketListing (CRUD + record-sale)
  - [x] 25.1 Create MarketListingEndpoints.cs — standard CRUD
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/MarketListingEndpoints.cs`
    - Inherit TypedEndpointBase<MarketListing, MarketListingCreateRequest, MarketListingUpdateRequest>
    - ValidateCreate (ItemName, StationUUID required)
    - MapMarketListingEndpoints extension method
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7_
  - [x] 25.2 Add record-sale action to MarketListingEndpoints
    - POST /market-listings/{entityUuid}/record-sale
    - Input: quantity, pricePerUnit (required), counterparty, counterpartyFaction, stationUUID
    - Creates MarketTransaction, returns 201 or 422 on validation failure
    - _Requirements: 12.8_

- [ ] 26. Complex endpoint: MarketTransaction (read + purchase + profit-loss)
  - [x] 26.1 Create MarketTransactionEndpoints.cs — read-only CRUD + delete
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/MarketTransactionEndpoints.cs`
    - GET all, GET one, DELETE (no standalone POST)
    - MapMarketTransactionEndpoints extension method
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
  - [x] 26.2 Add record-purchase and profit-loss endpoints to MarketTransactionEndpoints
    - POST /market-transactions/record-purchase — creates transaction (201)
    - GET /market-transactions/profit-loss — computes ProfitLossSummary with date filters
    - Validate required fields, date format
    - _Requirements: 13.5, 13.6, 32.1, 32.2, 32.3_

- [x] 27. Complex endpoint: DeliveryPlan (CRUD)
  - [x] 27.1 Create DeliveryPlanEndpoints.cs — standard CRUD
    - Create `OE2EmpireTracker.Server/Endpoints/Typed/DeliveryPlanEndpoints.cs`
    - Inherit TypedEndpointBase<DeliveryPlan, object, DeliveryPlanUpdateRequest> (inline create DTO)
    - ValidateCreate (name, routeUUID required)
    - MapDeliveryPlanEndpoints extension method
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_
  - [x] 27.2 Add drop-off and pick-up action endpoints to DeliveryPlanEndpoints
    - POST /delivery-plans/{entityUuid}/drop-off — add drop-off item
    - POST /delivery-plans/{entityUuid}/pick-up — add pick-up item
    - DELETE /delivery-plans/{entityUuid}/drop-off — remove drop-off items by index
    - DELETE /delivery-plans/{entityUuid}/pick-up — remove pick-up items by index
    - _Requirements: 9.8, 9.9, 9.10, 9.11_
  - [x] 27.3 Add mark-delivered, mark-stop-complete, mark-complete, ship actions
    - PUT /delivery-plans/{entityUuid}/mark-delivered
    - PUT /delivery-plans/{entityUuid}/mark-stop-complete
    - PUT /delivery-plans/{entityUuid}/mark-complete
    - PUT /delivery-plans/{entityUuid}/ship
    - _Requirements: 9.12, 9.13, 9.14, 9.15_
  - [x] 27.4 Add split-trips action endpoint to DeliveryPlanEndpoints
    - POST /delivery-plans/{entityUuid}/split-trips
    - Input: { cargoCapacity } (required, > 0)
    - Splits plan into multiple trip plans, returns List<DeliveryPlan> (201)
    - Original plan unchanged
    - _Requirements: 9.16, 30.1, 30.2, 30.3, 30.4_

- [x] 28. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 29. Program.cs wiring: Register all 19 endpoint groups
  - [-] 29.1 Register endpoint groups in Program.cs
    - Add MapColonyEndpoints, MapBlueprintEndpoints, MapSurveyEndpoints, MapPlayerProfileEndpoints, MapDeliveryRouteEndpoints, MapDeliveryPlanEndpoints, MapShipEndpoints, MapShipTemplateEndpoints, MapMarketListingEndpoints, MapMarketTransactionEndpoints, MapPricingPlanEndpoints, MapStockPlanEndpoints, MapStockProfileEndpoints, MapBuildPlanEndpoints, MapSupplyChainEndpoints, MapAsteroidEndpoints, MapStationEndpoints, MapFactionContactEndpoints, MapExternalCharacterEndpoints
    - All under /api/v1/ prefix with "Authenticated" policy
    - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.5_

- [ ] 30. Rate limiting configuration
  - [-] 30.1 Configure rate limiting for typed endpoints
    - Verify existing RateLimitMiddleware applies to typed endpoint routes
    - Ensure 5 TPS per token, sliding window, Owner exempt
    - Verify Retry-After header on 429 responses
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [ ] 31. JSON serialization configuration
  - [-] 31.1 Configure System.Text.Json options for typed endpoints
    - Verify JsonSerializerOptions: camelCase, case-insensitive, ignore null, enum converter
    - Ensure Common_Library models have dual annotations (Newtonsoft + STJ)
    - Verify lenient deserialization (extra fields ignored)
    - _Requirements: 23.2, 23.3, 27.1, 27.5, 27.6, 33.2, 33.3, 33.4, 3.8, 3.9_

- [~] 32. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 33. Unit tests: TypedEndpointBase authorization and validation
  - [~] 33.1 Write unit tests for authorization enforcement in TypedEndpointBase
    - Test 403 for mismatched Character_UUID (non-Owner)
    - Test pass-through for matching Character_UUID
    - Test pass-through for Owner role
    - Test no entity data in 403 response
    - _Requirements: 2.2, 2.3, 2.4, 2.6_
  - [~] 33.2 Write unit tests for input validation in TypedEndpointBase
    - Test 400 for empty body, invalid JSON, missing required fields
    - Test 400 for invalid Character_UUID, invalid Entity_UUID
    - Test 415 for wrong Content-Type
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [ ] 34. Unit tests: PaginationHelper
  - [~] 34.1 Write unit tests for PaginationHelper
    - Test raw array returned when no params
    - Test PaginatedResponse when limit/offset provided
    - Test MaxLimit cap at 500
    - Test 400 for negative values
    - Test offset beyond collection returns empty items with correct total
    - _Requirements: 29.1, 29.2, 29.3, 29.4, 29.5, 29.6_

- [ ] 35. Unit tests: Colony endpoint (dedup + sub-resources)
  - [~] 35.1 Write unit tests for ColonyEndpoints dedup logic
    - Test merge on duplicate PlanetName+SystemName (case-insensitive) returns 200
    - Test new colony returns 201
    - _Requirements: 31.1, 31.2, 31.3, 31.4_
  - [~] 35.2 Write unit tests for Colony sub-resource endpoints
    - Test add/remove structure, add/remove/update item, add/remove/update commodity-request
    - Test 404 for missing colony, 400 for missing required fields
    - _Requirements: 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15_

- [ ] 36. Unit tests: DeliveryPlan actions
  - [~] 36.1 Write unit tests for DeliveryPlan action endpoints
    - Test drop-off/pick-up add and remove
    - Test mark-delivered, mark-stop-complete, mark-complete
    - Test ship assignment
    - _Requirements: 9.8, 9.9, 9.10, 9.11, 9.12, 9.13, 9.14, 9.15_
  - [~] 36.2 Write unit tests for split-trips endpoint
    - Test split creates multiple plans based on cargo capacity
    - Test original plan unchanged
    - Test 400 for missing/invalid cargoCapacity
    - _Requirements: 9.16, 30.1, 30.2, 30.3, 30.4_

- [ ] 37. Unit tests: Blueprint, MarketListing, MarketTransaction actions
  - [~] 37.1 Write unit tests for Blueprint import and move actions
    - Test import merge (200) vs new (201)
    - Test move-to-global, move-to-player (200, 404)
    - _Requirements: 5.8, 5.9, 5.10_
  - [~] 37.2 Write unit tests for MarketListing record-sale and MarketTransaction endpoints
    - Test record-sale creates transaction (201), 422 on validation failure
    - Test record-purchase (201), profit-loss computation (200)
    - Test 400 for invalid date format
    - _Requirements: 12.8, 13.5, 13.6, 32.1, 32.2, 32.3_

- [ ] 38. Property-based tests
  - [~] 38.1 Write property test: Round-trip serialization integrity
    - **Property 5: Round-Trip Serialization Integrity**
    - For all 19 domain model types, generate random instances, serialize to JSON with STJ config, deserialize back, verify equivalent field values
    - **Validates: Requirements 33.1, 33.3, 33.4**
  - [~] 38.2 Write property test: Pagination correctness
    - **Property 4: Pagination Correctness**
    - For any collection and valid limit/offset: total == collection.Count, items.length <= limit, offset + items.length <= total
    - **Validates: Requirements 29.1, 29.2, 29.3, 29.4, 29.5, 29.6**
  - [~] 38.3 Write property test: Authorization isolation
    - **Property 1: Authorization Isolation**
    - For any two distinct Character_UUIDs, token A (non-Owner) cannot access token B's data (403, no entity data leaked)
    - **Validates: Requirements 2.3, 2.4, 2.6, 2.7**

- [ ] 39. Integration tests: Storage backend round-trip
  - [~] 39.1 Write integration tests for JsonFileStorageBackend typed methods
    - Test round-trip: upsert then get returns equivalent entity
    - Test GetAll returns all entities for a character
    - Test Delete removes the entity
    - Test Get returns null for non-existent entity
    - Test GetAll returns empty list for character with no entities
    - _Requirements: 22.3, 22.7, 22.8_

- [ ] 40. Backward compatibility verification
  - [~] 40.1 Write integration tests for bulk endpoint coexistence
    - Test data written via typed endpoint visible via bulk GET
    - Test data written via bulk PUT visible via typed GET
    - Test both endpoints coexist without conflict
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5_

- [ ] 41. Event dispatch and logging tests
  - [~] 41.1 Write unit tests for event dispatch on mutations
    - Test ServerEvent dispatched on create (Created), update (Updated), delete (Deleted)
    - Test correct EventType, EntityType, EntityUUID, OwnerCharacterUUID
    - Test 503 returned when EventDispatcher throws, mutation rolled back
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5_
  - [~] 41.2 Write unit tests for mutation logging
    - Test log entry on POST/PUT/DELETE with action type, entity type, UUID, token ID, IP
    - Test 500 returned when logging fails, mutation rolled back
    - _Requirements: 26.1, 26.2, 26.3_

- [~] 42. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Requirements 34 (Desktop App Client), 35 (Web Frontend Client), and 36 (Server Admin) are client integration requirements that will be addressed in separate specs — they define consumer behavior, not server implementation
- Requirement 24.6 (Bulk_Endpoint remains primary for Desktop_App) is a policy statement requiring no code changes
- The design specifies C# with ASP.NET Core Minimal API patterns

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["2.1"] },
    { "id": 2, "tasks": ["2.2", "2.3"] },
    { "id": 3, "tasks": ["3.1", "4.1", "5.1", "6.1"] },
    { "id": 4, "tasks": ["3.2", "4.2", "5.2", "6.2"] },
    { "id": 5, "tasks": ["7.1", "7.2", "7.3", "7.4"] },
    { "id": 6, "tasks": ["9.1", "10.1", "11.1", "12.1", "13.1", "14.1"] },
    { "id": 7, "tasks": ["15.1", "16.1", "17.1", "18.1", "19.1", "20.1"] },
    { "id": 8, "tasks": ["16.2", "17.2", "18.2", "20.2", "22.1"] },
    { "id": 9, "tasks": ["22.2", "23.1", "24.1", "25.1", "26.1", "27.1"] },
    { "id": 10, "tasks": ["23.2", "24.2", "25.2", "26.2", "27.2"] },
    { "id": 11, "tasks": ["24.3", "24.4", "27.3", "27.4"] },
    { "id": 12, "tasks": ["29.1", "30.1", "31.1"] },
    { "id": 13, "tasks": ["33.1", "33.2", "34.1"] },
    { "id": 14, "tasks": ["35.1", "35.2", "36.1", "36.2"] },
    { "id": 15, "tasks": ["37.1", "37.2", "38.1", "38.2", "38.3"] },
    { "id": 16, "tasks": ["39.1", "40.1", "41.1", "41.2"] }
  ]
}
```
