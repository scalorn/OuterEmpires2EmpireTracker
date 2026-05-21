# Requirements Document

## Introduction

This spec replaces the raw JSON file-proxy data endpoints in OE2EmpireTracker.Server with proper typed CRUD endpoints for each domain entity. The current `GET/PUT /characters/{uuid}/data/{dataType}` endpoints accept arbitrary JSON blobs without validation, typing, or business logic. The new endpoints use the domain models defined in OE2EmpireTracker.Common as the API contract, with typed request/response DTOs, input validation, and proper HTTP status codes. All endpoints must work identically regardless of which IStorageBackend implementation is active.

The typed endpoints mirror every CRUD method exposed by the service layer in the WinForms application (ColonyService, BlueprintService, SurveyService, PlayerProfileService, DeliveryRouteService, DeliveryPlanService, ShipService, ShipTemplateService, MarketListingService, PricingPlanService, StockTargetMutationService, BuildPlanMutationService, SupplyChainMutationService, AsteroidService, StationService, ContactsService). The API is the remote equivalent of these services — same validation, same authorization, same mutation semantics.

All three UI layers (WinForms desktop, .NET 8 server admin, web frontend) will consume these endpoints. The spec covers the full vertical: API definition, server implementation, storage backend extension, and client integration across all three UIs.

## Glossary

- **Server**: The OE2EmpireTracker.Server ASP.NET Core application (net8.0)
- **Common_Library**: The OE2EmpireTracker.Common shared library (netstandard2.0) containing domain models and DTOs
- **IStorageBackend**: The storage abstraction interface in the Server project that all persistence backends implement
- **JsonFileBackend**: The JSON file-based IStorageBackend implementation (primary development backend)
- **Typed_Endpoint**: An API endpoint that accepts and returns strongly-typed DTOs with validation, as opposed to raw JSON blobs
- **CreateRequest_DTO**: A Data Transfer Object for entity creation (defined in Common_Library, no UUID — service assigns it)
- **UpdateRequest_DTO**: A Data Transfer Object for entity updates (defined in Common_Library, includes UUID for identification)
- **Character_UUID**: The unique identifier of the player character who owns the data
- **Entity_UUID**: The unique identifier of a specific domain entity instance
- **Bulk_Endpoint**: The existing PUT /api/v1/characters/{uuid}/data endpoint that accepts the entire player data as a single JSON blob
- **Web_Frontend**: The browser-based UI that consumes the typed endpoints exclusively
- **Desktop_App**: The WinForms application that currently uses the Bulk_Endpoint for sync
- **TPS**: Transactions Per Second — the rate limit applied per API token


## Requirements

### Requirement 1: Rate Limiting

**User Story:** As a server operator, I want per-token rate limiting at 5 TPS, so that no single client can overwhelm the server.

#### Acceptance Criteria

1. THE Server SHALL enforce a rate limit of 5 transactions per second (TPS) per API token across all typed endpoints.
2. WHEN a token exceeds 5 requests in a 1-second sliding window, THE Server SHALL return HTTP 429 (Too Many Requests) with a Retry-After header indicating when the client may retry.
3. THE rate limit SHALL apply independently per token — one token hitting its limit does not affect other tokens.
4. THE rate limit SHALL apply to all HTTP methods (GET, POST, PUT, DELETE) equally.
5. THE Server SHALL use the existing Polly rate-limiting infrastructure for implementation consistency.
6. THE rate limit counter SHALL reset on a sliding window basis (not fixed windows).


### Requirement 2: Authorization and Access Control

**User Story:** As a server operator, I want typed endpoints to enforce the same authorization rules as existing endpoints, so that characters can only access their own data unless granted access. No data leakage is acceptable.

#### Acceptance Criteria

1. THE Server SHALL require authentication (valid API token) for all typed endpoints.
2. WHEN the authenticated token's Character_UUID matches the URL Character_UUID, THE Server SHALL allow the request.
3. WHEN the authenticated token has the Owner role, THE Server SHALL allow access to any character's data.
4. WHEN the authenticated token's Character_UUID does not match the URL Character_UUID and the token is not Owner, THE Server SHALL return HTTP 403 immediately without processing the request further. No entity data SHALL be returned or leaked in the error response.
5. THE Server SHALL use the same authorization logic (CanAccessCharacterData pattern) as the existing DataEndpoints for consistency.
6. THE Server SHALL NOT include entity details, UUIDs, or counts in 403 error responses — only a generic "Access denied" message.
7. THE Server SHALL validate authorization BEFORE performing any storage lookup, ensuring no data is fetched for unauthorized requests.


### Requirement 3: Input Validation

**User Story:** As a server developer, I want all typed endpoints to validate input before persisting, so that invalid or malformed data cannot corrupt the storage layer.

#### Acceptance Criteria

1. WHEN a POST or PUT request body cannot be deserialized into the expected DTO type, THE Server SHALL return HTTP 400 with error message "Invalid request body".
2. WHEN a POST or PUT request body is empty or null, THE Server SHALL return HTTP 400 with error message "Request body is required".
3. WHEN a CreateRequest_DTO is missing a required field (as defined per entity type), THE Server SHALL return HTTP 400 with an error message identifying the missing field (e.g., "PlanetName is required").
4. THE Server SHALL validate that string fields marked as required are not null, empty, or whitespace-only.
5. THE Server SHALL validate that the Character_UUID in the URL path is a valid non-empty string. WHEN invalid, return HTTP 400 with "Invalid character UUID".
6. THE Server SHALL validate that the Entity_UUID in the URL path (for GET/PUT/DELETE single) is a valid non-empty string. WHEN invalid, return HTTP 400 with "Invalid entity UUID".
7. IF a PUT request specifies an Entity_UUID in the URL that differs from the UUID in the request body, THEN THE Server SHALL use the URL Entity_UUID as authoritative (the body UUID is ignored for identification).
8. WHEN a request Content-Type is not application/json, THE Server SHALL return HTTP 415 (Unsupported Media Type).
9. WHEN a request body contains unknown/extra fields, THE Server SHALL ignore them (lenient deserialization) — no error for extra fields.


### Requirement 4: Typed Colony Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for colonies that mirror ColonyService, so that I can display and manage colony data with proper validation and type safety.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/colonies → Returns `List<Colony>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/colonies/{entityUuid} → Returns single `Colony` (HTTP 200) or HTTP 404 `{ "error": "Colony not found" }`.
3. POST /api/v1/characters/{uuid}/colonies → Input: `ColonyCreateRequest` (required: PlanetName, ColonyName). Output: created `Colony` with server-assigned UUID (HTTP 201). Location header: `/api/v1/characters/{uuid}/colonies/{newUuid}`. IF a colony with the same PlanetName+SystemName (case-insensitive) already exists for this character, THE Server SHALL merge into the existing colony and return HTTP 200 instead of 201.
4. PUT /api/v1/characters/{uuid}/colonies/{entityUuid} → Input: `ColonyUpdateRequest`. Output: updated `Colony` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/colonies/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN ColonyCreateRequest is missing PlanetName, return HTTP 400 `{ "error": "PlanetName is required" }`.
7. WHEN ColonyCreateRequest is missing ColonyName, return HTTP 400 `{ "error": "ColonyName is required" }`.
8. POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/structures → Input: `{ "flatpackBlueprintUUID": "..." }`. Adds a structure to the colony. Output: updated `Colony` (HTTP 200) or HTTP 404 if colony not found. HTTP 400 if flatpackBlueprintUUID is missing.
9. DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/structures/{structureUuid} → Removes a structure. HTTP 204 on success, HTTP 404 if colony or structure not found.
10. POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/items → Input: `Item` object. Adds an item to the colony inventory. Output: updated `Colony` (HTTP 200). HTTP 400 if item is invalid.
11. DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/items/{itemUuid} → Removes an item. HTTP 204 on success.
12. PUT /api/v1/characters/{uuid}/colonies/{colonyUuid}/items/{itemUuid} → Input: `{ "quantity": N }`. Updates item quantity. Output: updated `Colony` (HTTP 200). HTTP 400 if quantity < 0.
13. POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests → Input: `{ "commodityName": "...", "requested": N, "needBy": "ISO-date" }`. Adds a commodity request. Output: updated `Colony` (HTTP 200). HTTP 400 if commodityName missing.
14. DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests/{commodityName} → Removes a commodity request. HTTP 204 on success.
15. PUT /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests/{commodityName} → Input: `{ "requested": N, "delivered": N, "needBy": "ISO-date", "fulfilled": bool }`. Updates a commodity request. Output: updated `Colony` (HTTP 200).


### Requirement 5: Typed Blueprint Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for blueprints that mirror BlueprintService, so that I can display blueprint data with evolution chains, properties, and manufacturing information.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/blueprints → Returns `List<Blueprint>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/blueprints/{entityUuid} → Returns single `Blueprint` (HTTP 200) or HTTP 404 `{ "error": "Blueprint not found" }`.
3. POST /api/v1/characters/{uuid}/blueprints → Input: `BlueprintCreateRequest` (required: Name, BluePrintType). Additional field: `isGlobal` (bool, default false). Output: created `Blueprint` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/blueprints/{entityUuid} → Input: `BlueprintUpdateRequest`. Output: updated `Blueprint` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/blueprints/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN BlueprintCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.
7. WHEN BlueprintCreateRequest is missing BluePrintType, return HTTP 400 `{ "error": "BluePrintType is required" }`.
8. POST /api/v1/characters/{uuid}/blueprints/import → Input: `Blueprint` (full object for import/merge). Output: imported/merged `Blueprint` (HTTP 200 if merged, HTTP 201 if new). Mirrors BlueprintService.Import logic.
9. POST /api/v1/characters/{uuid}/blueprints/{entityUuid}/move-to-global → Moves blueprint to global scope. HTTP 200 on success, HTTP 404 if not found.
10. POST /api/v1/characters/{uuid}/blueprints/{entityUuid}/move-to-player → Moves blueprint to player scope. HTTP 200 on success, HTTP 404 if not found.


### Requirement 6: Typed Survey Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for surveys that mirror SurveyService, so that I can display resource breakdowns and survey locations with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/surveys → Returns `List<Survey>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/surveys/{entityUuid} → Returns single `Survey` (HTTP 200) or HTTP 404 `{ "error": "Survey not found" }`.
3. POST /api/v1/characters/{uuid}/surveys → Input: `SurveyCreateRequest` (required: PlanetName). Output: created `Survey` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/surveys/{entityUuid} → Input: `SurveyUpdateRequest`. Output: updated `Survey` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/surveys/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN SurveyCreateRequest is missing PlanetName, return HTTP 400 `{ "error": "PlanetName is required" }`.
7. POST /api/v1/characters/{uuid}/surveys/import → Input: `Survey` (full object for import/merge). Output: imported/merged `Survey` (HTTP 200 if merged, HTTP 201 if new). Mirrors SurveyService.Import logic including asteroid auto-linking.


### Requirement 7: Typed Player Profile Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for player profiles that mirror PlayerProfileService, so that I can display skills, rank, and profession data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/profiles → Returns `List<PlayerProfile>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/profiles/{entityUuid} → Returns single `PlayerProfile` (HTTP 200) or HTTP 404 `{ "error": "PlayerProfile not found" }`.
3. POST /api/v1/characters/{uuid}/profiles → Input: `PlayerProfileCreateRequest` (required: Name). Output: created `PlayerProfile` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/profiles/{entityUuid} → Input: `PlayerProfileUpdateRequest`. Output: updated `PlayerProfile` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/profiles/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN PlayerProfileCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.
7. POST /api/v1/characters/{uuid}/profiles/import → Input: `PlayerProfile` (full object for import/merge). Output: imported/merged `PlayerProfile` (HTTP 200 if merged, HTTP 201 if new). Mirrors PlayerProfileService.Import logic.


### Requirement 8: Typed Delivery Route Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for delivery routes that mirror DeliveryRouteService, so that I can display and manage logistics routes with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/delivery-routes → Returns `List<DeliveryRoute>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/delivery-routes/{entityUuid} → Returns single `DeliveryRoute` (HTTP 200) or HTTP 404 `{ "error": "DeliveryRoute not found" }`.
3. POST /api/v1/characters/{uuid}/delivery-routes → Input: `DeliveryRouteCreateRequest` (required: Name). Output: created `DeliveryRoute` with server-assigned UUID (HTTP 201). Stops are deep-copied and renumbered. Location header included.
4. PUT /api/v1/characters/{uuid}/delivery-routes/{entityUuid} → Input: `DeliveryRouteUpdateRequest`. Output: updated `DeliveryRoute` (HTTP 200) or HTTP 404 if not found. Stops are deep-copied and renumbered.
5. DELETE /api/v1/characters/{uuid}/delivery-routes/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN DeliveryRouteCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 9: Typed Delivery Plan Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for delivery plans that mirror DeliveryPlanService, so that I can display and manage delivery execution plans with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/delivery-plans → Returns `List<DeliveryPlan>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/delivery-plans/{entityUuid} → Returns single `DeliveryPlan` (HTTP 200) or HTTP 404 `{ "error": "DeliveryPlan not found" }`.
3. POST /api/v1/characters/{uuid}/delivery-plans → Input: `{ "name": "...", "routeUUID": "..." }` (required: name, routeUUID). Output: created `DeliveryPlan` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid} → Input: `DeliveryPlanUpdateRequest`. Output: updated `DeliveryPlan` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN create request is missing name, return HTTP 400 `{ "error": "name is required" }`.
7. WHEN create request is missing routeUUID, return HTTP 400 `{ "error": "routeUUID is required" }`.
8. POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/drop-off → Input: `{ "destInfo": StopDestinationInfo, "itemInfo": DeliveryItemInfo }`. Adds a drop-off item. Output: updated `DeliveryPlan` (HTTP 200). HTTP 404 if plan not found.
9. POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/pick-up → Input: `{ "destInfo": StopDestinationInfo, "itemInfo": DeliveryItemInfo }`. Adds a pick-up item. Output: updated `DeliveryPlan` (HTTP 200). HTTP 404 if plan not found.
10. DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/drop-off → Input: `{ "destInfo": StopDestinationInfo, "indices": [int] }`. Removes drop-off items by index. Output: updated `DeliveryPlan` (HTTP 200).
11. DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/pick-up → Input: `{ "destInfo": StopDestinationInfo, "indices": [int] }`. Removes pick-up items by index. Output: updated `DeliveryPlan` (HTTP 200).
12. PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-delivered → Input: `{ "stopSequence": int, "itemIndex": int, "listType": "dropOff"|"pickUp", "delivered": bool }`. Marks item delivered/undelivered. Output: updated `DeliveryPlan` (HTTP 200).
13. PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-stop-complete → Input: `{ "stopSequence": int }`. Marks a stop complete. Output: updated `DeliveryPlan` (HTTP 200).
14. PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-complete → Marks entire plan complete. Output: updated `DeliveryPlan` (HTTP 200).
15. PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/ship → Input: `{ "shipUUID": "..." }`. Assigns a ship. Output: updated `DeliveryPlan` (HTTP 200).
16. POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/split-trips → Input: `{ "cargoCapacity": decimal }` (required, > 0). Splits the plan into multiple trip plans based on cargo volume. Output: `List<DeliveryPlan>` — the newly created plans (HTTP 201). Original plan unchanged. HTTP 400 if cargoCapacity missing or <= 0. HTTP 404 if plan not found.


### Requirement 10: Typed Ship Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for ships that mirror ShipService, so that I can display fleet management data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/ships → Returns `List<Ship>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/ships/{entityUuid} → Returns single `Ship` (HTTP 200) or HTTP 404 `{ "error": "Ship not found" }`.
3. POST /api/v1/characters/{uuid}/ships → Input: `ShipCreateRequest` (required: Name). Output: created `Ship` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/ships/{entityUuid} → Input: `ShipUpdateRequest`. Output: updated `Ship` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/ships/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN ShipCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.
7. POST /api/v1/characters/{uuid}/ships/from-template → Input: `{ "templateUUID": "..." }` (required). Creates a ship from a template. Output: created `Ship` (HTTP 201). HTTP 400 if templateUUID missing. HTTP 404 if template not found.


### Requirement 11: Typed Ship Template Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for ship templates that mirror ShipTemplateService, so that I can manage ship designs with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/ship-templates → Returns `List<ShipTemplate>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/ship-templates/{entityUuid} → Returns single `ShipTemplate` (HTTP 200) or HTTP 404 `{ "error": "ShipTemplate not found" }`.
3. POST /api/v1/characters/{uuid}/ship-templates → Input: `ShipTemplateCreateRequest` (required: Name). Output: created `ShipTemplate` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/ship-templates/{entityUuid} → Input: `ShipTemplateUpdateRequest`. Output: updated `ShipTemplate` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/ship-templates/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN ShipTemplateCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 12: Typed Market Listing Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for market listings that mirror MarketListingService, so that I can display and manage market data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/market-listings → Returns `List<MarketListing>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/market-listings/{entityUuid} → Returns single `MarketListing` (HTTP 200) or HTTP 404 `{ "error": "MarketListing not found" }`.
3. POST /api/v1/characters/{uuid}/market-listings → Input: `MarketListingCreateRequest` (required: ItemName, StationUUID). Output: created `MarketListing` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/market-listings/{entityUuid} → Input: `MarketListingUpdateRequest`. Output: updated `MarketListing` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/market-listings/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN MarketListingCreateRequest is missing ItemName, return HTTP 400 `{ "error": "ItemName is required" }`.
7. WHEN MarketListingCreateRequest is missing StationUUID, return HTTP 400 `{ "error": "StationUUID is required" }`.
8. POST /api/v1/characters/{uuid}/market-listings/{entityUuid}/record-sale → Input: `{ "quantity": int, "pricePerUnit": decimal, "counterparty": string, "counterpartyFaction": string, "stationUUID": string }` (required: quantity, pricePerUnit). Output: `MarketTransaction` (HTTP 201) or null body with HTTP 422 if validation failed. HTTP 404 if listing not found.


### Requirement 13: Typed Market Transaction Endpoints

**User Story:** As a web frontend developer, I want read-only endpoints for market transactions, so that I can display transaction history.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/market-transactions → Returns `List<MarketTransaction>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/market-transactions/{entityUuid} → Returns single `MarketTransaction` (HTTP 200) or HTTP 404 `{ "error": "MarketTransaction not found" }`.
3. Market transactions are created via the record-sale endpoint on market-listings (Requirement 12, Criterion 8) or the record-purchase endpoint (Criterion 5). No standalone POST endpoint exists for raw transaction creation.
4. DELETE /api/v1/characters/{uuid}/market-transactions/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
5. POST /api/v1/characters/{uuid}/market-transactions/record-purchase → Input: `{ "itemName": string, "itemType": string, "quantity": int, "pricePerUnit": decimal, "counterparty": string, "counterpartyFaction": string, "stationUUID": string }` (required: itemName, quantity, pricePerUnit). Output: `MarketTransaction` (HTTP 201). HTTP 400 if required fields missing.
6. GET /api/v1/characters/{uuid}/market-transactions/profit-loss → Query params: `startDate` (ISO), `endDate` (ISO), `itemName` (optional filter). Output: `ProfitLossSummary` (HTTP 200). Returns computed profit/loss for the filtered period. HTTP 400 if date format is invalid.


### Requirement 14: Typed Pricing Plan Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for pricing plans that mirror PricingPlanService, so that I can manage pricing configurations with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/pricing-plans → Returns `List<PricingPlan>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/pricing-plans/{entityUuid} → Returns single `PricingPlan` (HTTP 200) or HTTP 404 `{ "error": "PricingPlan not found" }`.
3. POST /api/v1/characters/{uuid}/pricing-plans → Input: `PricingPlanCreateRequest` (required: Name). Output: created `PricingPlan` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/pricing-plans/{entityUuid} → Input: `PricingPlanUpdateRequest`. Output: updated `PricingPlan` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/pricing-plans/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN PricingPlanCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 15: Typed Stock Plan Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for stock plans that mirror StockTargetMutationService plan methods, so that I can manage stock targets with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/stock-plans → Returns `List<StockPlan>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/stock-plans/{entityUuid} → Returns single `StockPlan` (HTTP 200) or HTTP 404 `{ "error": "StockPlan not found" }`.
3. POST /api/v1/characters/{uuid}/stock-plans → Input: `StockPlanCreateRequest` (required: Name). Output: created `StockPlan` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/stock-plans/{entityUuid} → Input: `StockPlanUpdateRequest`. Output: updated `StockPlan` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/stock-plans/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN StockPlanCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 16: Typed Stock Profile Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for stock profiles that mirror StockTargetMutationService profile methods, so that I can manage stock profile groupings with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/stock-profiles → Returns `List<StockProfile>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/stock-profiles/{entityUuid} → Returns single `StockProfile` (HTTP 200) or HTTP 404 `{ "error": "StockProfile not found" }`.
3. POST /api/v1/characters/{uuid}/stock-profiles → Input: `StockProfileCreateRequest` (required: Name). Output: created `StockProfile` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/stock-profiles/{entityUuid} → Input: `StockProfileUpdateRequest`. Output: updated `StockProfile` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/stock-profiles/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN StockProfileCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 17: Typed Build Plan Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for build plans that mirror BuildPlanMutationService, so that I can manage manufacturing build plans with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/build-plans → Returns `List<BuildPlan>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/build-plans/{entityUuid} → Returns single `BuildPlan` (HTTP 200) or HTTP 404 `{ "error": "BuildPlan not found" }`.
3. POST /api/v1/characters/{uuid}/build-plans → Input: `BuildPlanCreateRequest` (required: Name). Output: created `BuildPlan` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/build-plans/{entityUuid} → Input: `BuildPlanUpdateRequest`. Output: updated `BuildPlan` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/build-plans/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN BuildPlanCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.
7. POST /api/v1/characters/{uuid}/build-plans/{entityUuid}/generate-colony-items → Input: `{ "colonyUUID": "..." }` (required). Scans colony for unstaged structures and generates build items. Output: `{ "itemsAdded": int }` (HTTP 200). HTTP 400 if colonyUUID missing. HTTP 404 if plan or colony not found.


### Requirement 18: Typed Supply Chain Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for supply chains that mirror SupplyChainMutationService, so that I can manage production pipelines with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/supply-chains → Returns `List<SupplyChain>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/supply-chains/{entityUuid} → Returns single `SupplyChain` (HTTP 200) or HTTP 404 `{ "error": "SupplyChain not found" }`.
3. POST /api/v1/characters/{uuid}/supply-chains → Input: `SupplyChainCreateRequest` (required: Name). Output: created `SupplyChain` with server-assigned UUID (HTTP 201). Stages are deep-copied and renumbered. Location header included.
4. PUT /api/v1/characters/{uuid}/supply-chains/{entityUuid} → Input: `SupplyChainUpdateRequest`. Output: updated `SupplyChain` (HTTP 200) or HTTP 404 if not found. Stages are deep-copied and renumbered.
5. DELETE /api/v1/characters/{uuid}/supply-chains/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN SupplyChainCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 19: Typed Asteroid Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for asteroids that mirror AsteroidService, so that I can manage asteroid survey data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/asteroids → Returns `List<Asteroid>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/asteroids/{entityUuid} → Returns single `Asteroid` (HTTP 200) or HTTP 404 `{ "error": "Asteroid not found" }`.
3. POST /api/v1/characters/{uuid}/asteroids → Input: `AsteroidCreateRequest` (required: Name). Output: created `Asteroid` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/asteroids/{entityUuid} → Input: `AsteroidUpdateRequest`. Output: updated `Asteroid` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/asteroids/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN AsteroidCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 20: Typed Station Endpoints

**User Story:** As a web frontend developer, I want typed CRUD endpoints for stations that mirror StationService, so that I can manage station data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/stations → Returns `List<Station>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/stations/{entityUuid} → Returns single `Station` (HTTP 200) or HTTP 404 `{ "error": "Station not found" }`.
3. POST /api/v1/characters/{uuid}/stations → Input: `StationCreateRequest` (required: Name). Output: created `Station` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/stations/{entityUuid} → Input: `StationUpdateRequest`. Output: updated `Station` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/stations/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN StationCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 21: Typed Contacts Endpoints (Factions and External Characters)

**User Story:** As a web frontend developer, I want typed CRUD endpoints for factions and external characters that mirror ContactsService, so that I can manage contact data with proper typing.

#### Acceptance Criteria

1. GET /api/v1/characters/{uuid}/factions → Returns `List<Faction>` (HTTP 200). Empty list if none exist.
2. GET /api/v1/characters/{uuid}/factions/{entityUuid} → Returns single `Faction` (HTTP 200) or HTTP 404 `{ "error": "Faction not found" }`.
3. POST /api/v1/characters/{uuid}/factions → Input: `FactionCreateRequest` (required: Name). Output: created `Faction` with server-assigned UUID (HTTP 201). Location header included.
4. PUT /api/v1/characters/{uuid}/factions/{entityUuid} → Input: `FactionUpdateRequest`. Output: updated `Faction` (HTTP 200) or HTTP 404 if not found.
5. DELETE /api/v1/characters/{uuid}/factions/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
6. WHEN FactionCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.
7. GET /api/v1/characters/{uuid}/contacts → Returns `List<ExternalCharacter>` (HTTP 200). Empty list if none exist.
8. GET /api/v1/characters/{uuid}/contacts/{entityUuid} → Returns single `ExternalCharacter` (HTTP 200) or HTTP 404 `{ "error": "ExternalCharacter not found" }`.
9. POST /api/v1/characters/{uuid}/contacts → Input: `ExternalCharacterCreateRequest` (required: Name). Output: created `ExternalCharacter` with server-assigned UUID (HTTP 201). Location header included.
10. PUT /api/v1/characters/{uuid}/contacts/{entityUuid} → Input: `ExternalCharacterUpdateRequest`. Output: updated `ExternalCharacter` (HTTP 200) or HTTP 404 if not found.
11. DELETE /api/v1/characters/{uuid}/contacts/{entityUuid} → HTTP 204 on success, HTTP 404 if not found.
12. WHEN ExternalCharacterCreateRequest is missing Name, return HTTP 400 `{ "error": "Name is required" }`.


### Requirement 22: Typed Storage Backend Interface Extension

**User Story:** As a server developer, I want the IStorageBackend interface extended with typed methods for each domain entity, so that storage operations are type-safe and backend-agnostic.

#### Acceptance Criteria

1. THE IStorageBackend interface SHALL define typed methods for each domain entity: GetAll{Entity}Async(characterUUID), Get{Entity}Async(characterUUID, entityUUID), Upsert{Entity}Async(characterUUID, entity), and Delete{Entity}Async(characterUUID, entityUUID).
2. THE IStorageBackend interface SHALL define typed methods for: Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction, PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain, Asteroid, Station, Faction, and ExternalCharacter.
3. THE JsonFileBackend SHALL implement all typed storage methods with full functionality (read/write JSON files per entity type).
4. THE SqliteStorageBackend SHALL implement all typed storage methods (initial implementation may throw NotImplementedException with a descriptive message).
5. THE PostgresStorageBackend SHALL implement all typed storage methods (initial implementation may throw NotImplementedException with a descriptive message).
6. THE DynamoStorageBackend SHALL implement all typed storage methods (initial implementation may throw NotImplementedException with a descriptive message).
7. WHEN a typed Get method is called for an entity that does not exist, THE IStorageBackend implementation SHALL return null.
8. WHEN a typed GetAll method is called for a character with no entities of that type, THE IStorageBackend implementation SHALL return an empty list.


### Requirement 23: Backend-Agnostic Behavior

**User Story:** As a server operator, I want the typed endpoints to produce identical behavior regardless of which storage backend is configured, so that I can switch backends without affecting API consumers.

#### Acceptance Criteria

1. THE Server SHALL produce identical HTTP responses (status codes, response body structure, error messages) for the same request regardless of which IStorageBackend implementation is active, provided the backend has completed implementation for that entity type. Backends that have not yet implemented typed methods SHALL cause the server to return HTTP 501 (Not Implemented).
2. THE Server SHALL deserialize request bodies into Common_Library DTOs using System.Text.Json with case-insensitive property matching.
3. THE Server SHALL serialize response bodies from Common_Library domain models using System.Text.Json with camelCase property naming.
4. WHEN a storage backend returns null for a single-entity lookup, THE Typed_Endpoint SHALL return HTTP 404 with a consistent error object format: `{ "error": "<descriptive message>" }`.
5. WHEN a storage backend returns an empty list for a collection lookup, THE Typed_Endpoint SHALL return HTTP 200 with an empty JSON array.
6. THE Server SHALL return HTTP 200 for all successful GET requests (both collection and single-entity).


### Requirement 24: Backward-Compatible Bulk Endpoint

**User Story:** As a desktop app developer, I want the existing bulk data endpoint to remain functional during migration, so that the desktop app continues to sync without changes.

#### Acceptance Criteria

1. THE Server SHALL continue to expose PUT /api/v1/characters/{uuid}/data that accepts the entire player data as a single JSON blob (the Bulk_Endpoint).
2. THE Server SHALL continue to expose GET /api/v1/characters/{uuid}/data that returns all player data as a single JSON blob.
3. THE Bulk_Endpoint SHALL coexist with the new typed endpoints without conflict.
4. WHEN data is written via the Bulk_Endpoint, THE typed endpoints SHALL eventually reflect the updated data on subsequent reads (eventual consistency is acceptable; immediate consistency is not required).
5. WHEN data is written via a typed endpoint, THE Bulk_Endpoint SHALL eventually reflect the updated data on subsequent reads (eventual consistency is acceptable; immediate consistency is not required).
6. THE Bulk_Endpoint SHALL remain the primary sync mechanism for the Desktop_App until a future migration spec deprecates it.


### Requirement 25: Event Dispatch for Typed Mutations

**User Story:** As a real-time push subscriber, I want typed endpoint mutations to dispatch server events, so that WebSocket clients receive updates when data changes.

#### Acceptance Criteria

1. WHEN a POST (create) succeeds on any typed endpoint, THE Server SHALL dispatch a ServerEvent with EventType=Created, the entity type name, the new Entity_UUID, and the owning Character_UUID.
2. WHEN a PUT (update) succeeds on any typed endpoint, THE Server SHALL dispatch a ServerEvent with EventType=Updated, the entity type name, the Entity_UUID, and the owning Character_UUID.
3. WHEN a DELETE succeeds on any typed endpoint, THE Server SHALL dispatch a ServerEvent with EventType=Deleted, the entity type name, the Entity_UUID, and the owning Character_UUID.
4. THE Server SHALL use the same EventDispatcher mechanism as the existing DataEndpoints for consistency.
5. IF the EventDispatcher is unavailable or throws an exception during dispatch, THE Server SHALL reject the mutation and return HTTP 503 (Service Unavailable) with an error message indicating the event system is temporarily unavailable.


### Requirement 26: Mutation Logging

**User Story:** As a server operator, I want all typed endpoint mutations logged, so that I can audit data changes.

#### Acceptance Criteria

1. WHEN a POST, PUT, or DELETE succeeds on any typed endpoint, THE Server SHALL log the mutation with: action type (Created/Updated/Deleted), entity type name, Entity_UUID, the API token ID that performed the action, and the remote IP address.
2. THE Server SHALL use the same logging pattern (ILoggerFactory) as the existing DataEndpoints for consistency.
3. IF logging fails (e.g., logger throws an exception), THE Server SHALL roll back the mutation and return HTTP 500 with an error message indicating an internal error occurred.


### Requirement 27: Common Library as API Contract

**User Story:** As a developer, I want the Common_Library domain models to serve as the single source of truth for API field definitions, so that the server, desktop app, and web frontend all share the same type definitions.

#### Acceptance Criteria

1. THE Server SHALL reference OE2EmpireTracker.Common for all domain model types used in endpoint request and response bodies. This is a prerequisite for all subsequent criteria in this requirement.
2. THE Server SHALL use CreateRequest_DTO types from Common_Library for POST request bodies (e.g., ColonyCreateRequest, BlueprintCreateRequest, SurveyCreateRequest).
3. THE Server SHALL use UpdateRequest_DTO types from Common_Library for PUT request bodies (e.g., ColonyUpdateRequest, BlueprintUpdateRequest, SurveyUpdateRequest).
4. THE Server SHALL return domain model types from Common_Library in response bodies (e.g., Colony, Blueprint, Survey).
5. THE Server SHALL NOT define duplicate or shadow model types for entities that already exist in Common_Library. Adoption is all-or-nothing: if the server references Common_Library, it must use all required DTOs with no duplicates.
6. WHEN a new field is added to a Common_Library model, THE typed endpoints SHALL automatically include that field in responses without server code changes.


### Requirement 28: Endpoint URL Convention

**User Story:** As an API consumer, I want consistent and predictable URL patterns for all typed endpoints, so that I can programmatically construct API calls.

#### Acceptance Criteria

1. THE Server SHALL use the URL pattern /api/v1/characters/{uuid}/{entity-type-plural} for collection endpoints (GET all, POST).
2. THE Server SHALL use the URL pattern /api/v1/characters/{uuid}/{entity-type-plural}/{entityUuid} for single-entity endpoints (GET one, PUT, DELETE).
3. THE Server SHALL use kebab-case for multi-word entity type names in URLs (e.g., delivery-routes, ship-templates, market-listings, pricing-plans, stock-plans, stock-profiles, supply-chains, build-plans).
4. THE Server SHALL group all typed endpoints under the /api/v1/ version prefix.
5. THE Server SHALL require the "Authenticated" authorization policy on all typed endpoint groups.
6. Sub-resource endpoints (structures, items, commodity-requests, drop-off, pick-up, etc.) SHALL follow the pattern: /api/v1/characters/{uuid}/{parent-entity}/{parentUuid}/{sub-resource}.


### Requirement 29: Pagination

**User Story:** As an API consumer, I want to paginate large collection responses, so that I can efficiently load data without fetching everything at once.

#### Acceptance Criteria

1. ALL collection GET endpoints SHALL support optional query parameters `limit` (integer, max items to return) and `offset` (integer, number of items to skip).
2. WHEN `limit` and `offset` are omitted, THE Server SHALL return the full collection (backward compatible).
3. WHEN `limit` is provided, THE Server SHALL return at most `limit` items starting from `offset` (default 0).
4. THE response SHALL include pagination metadata in a wrapper: `{ "items": [...], "total": N, "limit": N, "offset": N }`.
5. WHEN `limit` or `offset` is negative or non-numeric, THE Server SHALL return HTTP 400 with `{ "error": "Invalid pagination parameters" }`.
6. THE maximum allowed `limit` value SHALL be 500. Requests exceeding this SHALL be capped at 500 without error.


### Requirement 30: Delivery Plan Split-Trips Endpoint

**User Story:** As a web frontend developer, I want to split a delivery plan into multiple trips based on cargo capacity, so that I can plan logistics for ships with limited cargo space.

#### Acceptance Criteria

1. POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/split-trips → Input: `{ "cargoCapacity": decimal }` (required). Output: `List<DeliveryPlan>` — the newly created split plans (HTTP 201). HTTP 404 if plan not found. HTTP 400 if cargoCapacity missing or <= 0.
2. THE Server SHALL implement the same split logic as DeliveryPlanService.SplitTrips — dividing items across multiple plans based on cargo volume.
3. THE original plan SHALL remain unchanged. New plans are created as separate entities.
4. EACH new plan SHALL have a server-assigned UUID and be persisted independently.


### Requirement 31: Colony Create with Dedup

**User Story:** As a web frontend developer, I want the colony create endpoint to handle deduplication, so that importing colony data doesn't create duplicates.

#### Acceptance Criteria

1. WHEN a POST /api/v1/characters/{uuid}/colonies request specifies a PlanetName+SystemName combination that already exists for that character, THE Server SHALL merge the request into the existing colony rather than creating a duplicate.
2. WHEN a merge occurs, THE Server SHALL return the merged Colony with HTTP 200 (not 201) to indicate an existing entity was updated.
3. WHEN no matching colony exists, THE Server SHALL create a new colony and return HTTP 201 as normal.
4. THE dedup matching SHALL be case-insensitive on PlanetName and SystemName.


### Requirement 32: Market Transaction Endpoints (Purchase and Profit/Loss)

**User Story:** As a web frontend developer, I want endpoints for recording purchases and computing profit/loss, so that I can manage market analytics.

#### Acceptance Criteria

1. POST /api/v1/characters/{uuid}/market-transactions/record-purchase → Input: `{ "itemName": string, "itemType": string, "quantity": int, "pricePerUnit": decimal, "counterparty": string, "counterpartyFaction": string, "stationUUID": string }` (required: itemName, quantity, pricePerUnit). Output: `MarketTransaction` (HTTP 201). HTTP 400 if required fields missing.
2. GET /api/v1/characters/{uuid}/market-transactions/profit-loss → Query params: `startDate` (ISO), `endDate` (ISO), `itemName` (optional filter). Output: `ProfitLossSummary` (HTTP 200). Returns computed profit/loss for the filtered period.
3. WHEN startDate or endDate is malformed, THE Server SHALL return HTTP 400 with `{ "error": "Invalid date format" }`.


### Requirement 33: JSON Serialization Round-Trip Integrity

**User Story:** As a developer, I want to ensure that domain models serialized to JSON and deserialized back produce equivalent objects, so that no data is lost in transit.

#### Acceptance Criteria

1. FOR ALL domain model types used in typed endpoints, serializing a model to JSON and deserializing it back SHALL produce an object with equivalent field values (round-trip property).
2. THE Common_Library models SHALL be migrated from Newtonsoft.Json attributes to System.Text.Json attributes. Dual-annotation is acceptable during the transition period.
3. THE Server SHALL handle null optional fields gracefully — null fields SHALL be omitted from JSON responses (NullValueHandling equivalent).
4. THE Server SHALL handle default-value fields according to the DefaultValue attributes on Common_Library models.


### Requirement 34: Desktop App Client Integration

**User Story:** As a desktop app developer, I want the WinForms application to consume the typed endpoints for individual entity operations, so that it can transition from bulk sync to granular CRUD.

#### Acceptance Criteria

1. THE Desktop_App SHALL add a typed API client class (or extend ServerContext) that wraps HTTP calls to each typed endpoint.
2. THE typed client SHALL use the same authentication token mechanism as the existing SyncManager.
3. THE typed client SHALL handle HTTP 429 (rate limit) by respecting the Retry-After header and queuing retries.
4. THE typed client SHALL handle HTTP 401/403 by surfacing appropriate error messages to the user.
5. THE typed client SHALL handle HTTP 5xx by logging the error and surfacing a user-friendly message.
6. THE Desktop_App SHALL continue to use the Bulk_Endpoint for full sync operations until explicitly migrated.
7. THE typed client SHALL support offline queuing — mutations made while disconnected are queued and replayed when connectivity is restored (extending the existing OfflineQueue pattern).


### Requirement 35: Web Frontend Client Integration

**User Story:** As a web frontend developer, I want the web UI to consume the typed endpoints exclusively, so that it has full CRUD capability with proper typing.

#### Acceptance Criteria

1. THE Web_Frontend SHALL use the typed endpoints as its sole data access layer (no bulk endpoint usage).
2. THE Web_Frontend SHALL handle HTTP 429 by displaying a rate-limit notification and automatically retrying after the Retry-After period.
3. THE Web_Frontend SHALL handle HTTP 403 by displaying an "Access denied" message without exposing any entity data.
4. THE Web_Frontend SHALL handle HTTP 400 by displaying the specific validation error message from the response body.
5. THE Web_Frontend SHALL handle HTTP 404 by displaying an appropriate "not found" message and refreshing the entity list.
6. THE Web_Frontend SHALL receive real-time updates via WebSocket (ServerEvents) and refresh affected entity views automatically.


### Requirement 36: .NET 8 Server Admin Integration

**User Story:** As a server administrator, I want the .NET 8 admin interface to consume the typed endpoints for data management, so that admin operations use the same validated API path.

#### Acceptance Criteria

1. THE Server admin interface SHALL use the typed endpoints for all entity CRUD operations (no direct storage backend access for entity data).
2. THE Server admin interface SHALL authenticate using Owner-role tokens for cross-character data access.
3. THE Server admin interface SHALL display appropriate error messages for all HTTP error responses (400, 403, 404, 429, 500, 501, 503).


---

## References

- **Pattern reference:** `.kiro/specs/faction-server-expanded-permissions/` — demonstrates the correct pattern for typed models, typed storage methods, and typed endpoints with validation in this server.
- **Domain models:** `OE2EmpireTracker.Common/Models/` — source of truth for all entity types and request DTOs.
- **Storage interface:** `OE2EmpireTracker.Server/Storage/IStorageBackend.cs` — the interface to extend with typed methods.
- **Existing raw endpoints:** `OE2EmpireTracker.Server/Endpoints/DataEndpoints.cs` — the current JSON proxy to be superseded (but not removed).
- **Service layer (WinForms):** `OE2EmpireTracker.Common/Services/` — the service classes whose CRUD methods the API mirrors.


## Service-to-Endpoint Mapping

Complete mapping of WinForms service methods to API endpoints:

| Service Class | Method | HTTP Endpoint |
|---|---|---|
| ColonyService | Create | POST /api/v1/characters/{uuid}/colonies |
| ColonyService | Update | PUT /api/v1/characters/{uuid}/colonies/{entityUuid} |
| ColonyService | Delete | DELETE /api/v1/characters/{uuid}/colonies/{entityUuid} |
| ColonyService | AddStructure | POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/structures |
| ColonyService | RemoveStructure | DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/structures/{structureUuid} |
| ColonyService | AddItem | POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/items |
| ColonyService | RemoveItem | DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/items/{itemUuid} |
| ColonyService | UpdateItem | PUT /api/v1/characters/{uuid}/colonies/{colonyUuid}/items/{itemUuid} |
| ColonyService | AddCommodityRequest | POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests |
| ColonyService | RemoveCommodityRequest | DELETE /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests/{commodityName} |
| ColonyService | UpdateCommodityRequest | PUT /api/v1/characters/{uuid}/colonies/{colonyUuid}/commodity-requests/{commodityName} |
| BlueprintService | Create | POST /api/v1/characters/{uuid}/blueprints |
| BlueprintService | Update | PUT /api/v1/characters/{uuid}/blueprints/{entityUuid} |
| BlueprintService | Delete | DELETE /api/v1/characters/{uuid}/blueprints/{entityUuid} |
| BlueprintService | Import | POST /api/v1/characters/{uuid}/blueprints/import |
| BlueprintService | MoveToGlobal | POST /api/v1/characters/{uuid}/blueprints/{entityUuid}/move-to-global |
| BlueprintService | MoveToPlayer | POST /api/v1/characters/{uuid}/blueprints/{entityUuid}/move-to-player |
| SurveyService | Create | POST /api/v1/characters/{uuid}/surveys |
| SurveyService | Update | PUT /api/v1/characters/{uuid}/surveys/{entityUuid} |
| SurveyService | Delete | DELETE /api/v1/characters/{uuid}/surveys/{entityUuid} |
| SurveyService | Import | POST /api/v1/characters/{uuid}/surveys/import |
| PlayerProfileService | Create | POST /api/v1/characters/{uuid}/profiles |
| PlayerProfileService | Update | PUT /api/v1/characters/{uuid}/profiles/{entityUuid} |
| PlayerProfileService | Delete | DELETE /api/v1/characters/{uuid}/profiles/{entityUuid} |
| PlayerProfileService | Import | POST /api/v1/characters/{uuid}/profiles/import |
| DeliveryRouteService | Create | POST /api/v1/characters/{uuid}/delivery-routes |
| DeliveryRouteService | Update | PUT /api/v1/characters/{uuid}/delivery-routes/{entityUuid} |
| DeliveryRouteService | Delete | DELETE /api/v1/characters/{uuid}/delivery-routes/{entityUuid} |
| DeliveryPlanService | Create | POST /api/v1/characters/{uuid}/delivery-plans |
| DeliveryPlanService | UpdatePlan | PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid} |
| DeliveryPlanService | Delete | DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid} |
| DeliveryPlanService | AddDropOffItem | POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/drop-off |
| DeliveryPlanService | AddPickUpItem | POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/pick-up |
| DeliveryPlanService | RemoveDropOffItems | DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/drop-off |
| DeliveryPlanService | RemovePickUpItems | DELETE /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/pick-up |
| DeliveryPlanService | MarkItemDelivered | PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-delivered |
| DeliveryPlanService | MarkStopComplete | PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-stop-complete |
| DeliveryPlanService | MarkPlanComplete | PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/mark-complete |
| DeliveryPlanService | SetShipUUID | PUT /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/ship |
| DeliveryPlanService | SplitTrips | POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/split-trips |
| ShipService | Create | POST /api/v1/characters/{uuid}/ships |
| ShipService | Update | PUT /api/v1/characters/{uuid}/ships/{entityUuid} |
| ShipService | Delete | DELETE /api/v1/characters/{uuid}/ships/{entityUuid} |
| ShipService | CreateFromTemplate | POST /api/v1/characters/{uuid}/ships/from-template |
| ShipTemplateService | Create | POST /api/v1/characters/{uuid}/ship-templates |
| ShipTemplateService | Update | PUT /api/v1/characters/{uuid}/ship-templates/{entityUuid} |
| ShipTemplateService | Delete | DELETE /api/v1/characters/{uuid}/ship-templates/{entityUuid} |
| MarketListingService | CreateListing | POST /api/v1/characters/{uuid}/market-listings |
| MarketListingService | UpdateListing | PUT /api/v1/characters/{uuid}/market-listings/{entityUuid} |
| MarketListingService | DeleteListing | DELETE /api/v1/characters/{uuid}/market-listings/{entityUuid} |
| MarketListingService | RecordSale | POST /api/v1/characters/{uuid}/market-listings/{entityUuid}/record-sale |
| PricingPlanService | Create | POST /api/v1/characters/{uuid}/pricing-plans |
| PricingPlanService | Update | PUT /api/v1/characters/{uuid}/pricing-plans/{entityUuid} |
| PricingPlanService | Delete | DELETE /api/v1/characters/{uuid}/pricing-plans/{entityUuid} |
| StockTargetMutationService | CreatePlan | POST /api/v1/characters/{uuid}/stock-plans |
| StockTargetMutationService | UpdatePlan | PUT /api/v1/characters/{uuid}/stock-plans/{entityUuid} |
| StockTargetMutationService | DeletePlan | DELETE /api/v1/characters/{uuid}/stock-plans/{entityUuid} |
| StockTargetMutationService | CreateProfile | POST /api/v1/characters/{uuid}/stock-profiles |
| StockTargetMutationService | UpdateProfile | PUT /api/v1/characters/{uuid}/stock-profiles/{entityUuid} |
| StockTargetMutationService | DeleteProfile | DELETE /api/v1/characters/{uuid}/stock-profiles/{entityUuid} |
| BuildPlanMutationService | Create | POST /api/v1/characters/{uuid}/build-plans |
| BuildPlanMutationService | Update | PUT /api/v1/characters/{uuid}/build-plans/{entityUuid} |
| BuildPlanMutationService | Delete | DELETE /api/v1/characters/{uuid}/build-plans/{entityUuid} |
| BuildPlanService | GenerateColonyBuildItems | POST /api/v1/characters/{uuid}/build-plans/{entityUuid}/generate-colony-items |
| SupplyChainMutationService | Create | POST /api/v1/characters/{uuid}/supply-chains |
| SupplyChainMutationService | Update | PUT /api/v1/characters/{uuid}/supply-chains/{entityUuid} |
| SupplyChainMutationService | Delete | DELETE /api/v1/characters/{uuid}/supply-chains/{entityUuid} |
| AsteroidService | Create | POST /api/v1/characters/{uuid}/asteroids |
| AsteroidService | Update | PUT /api/v1/characters/{uuid}/asteroids/{entityUuid} |
| AsteroidService | Delete | DELETE /api/v1/characters/{uuid}/asteroids/{entityUuid} |
| StationService | Create | POST /api/v1/characters/{uuid}/stations |
| StationService | Update | PUT /api/v1/characters/{uuid}/stations/{entityUuid} |
| StationService | Delete | DELETE /api/v1/characters/{uuid}/stations/{entityUuid} |
| ContactsService | CreateFaction | POST /api/v1/characters/{uuid}/factions |
| ContactsService | UpdateFaction | PUT /api/v1/characters/{uuid}/factions/{entityUuid} |
| ContactsService | DeleteFaction | DELETE /api/v1/characters/{uuid}/factions/{entityUuid} |
| ContactsService | CreateCharacter | POST /api/v1/characters/{uuid}/contacts |
| ContactsService | UpdateCharacter | PUT /api/v1/characters/{uuid}/contacts/{entityUuid} |
| ContactsService | DeleteCharacter | DELETE /api/v1/characters/{uuid}/contacts/{entityUuid} |
| MarketService | RecordPurchase | POST /api/v1/characters/{uuid}/market-transactions/record-purchase |
| MarketService | ComputeProfitLoss | GET /api/v1/characters/{uuid}/market-transactions/profit-loss |


## Resolved Decisions

### DECISION-1: Concurrency — Last-Write-Wins

Concurrent PUT requests to the same entity use last-write-wins semantics. No ETags or optimistic concurrency. The server's existing `_lock` semaphore prevents file corruption, but the last request to complete wins logically.

### DECISION-2: Pagination — Supported

Collection GET endpoints SHALL support optional pagination via query parameters `?limit=N&offset=M`. When omitted, the full list is returned (backward compatible). When provided, the response includes pagination metadata.

### DECISION-3: Newtonsoft.Json — Migrate to System.Text.Json

Common_Library models will be migrated from Newtonsoft.Json attributes to System.Text.Json attributes. Dual-annotation is acceptable during transition.

### DECISION-4: Bulk/Typed Consistency — Same Files (Immediate)

Typed endpoints read/write the same per-type JSON files (`characters/{uuid}/{dataType}.json`) as the bulk endpoint. Consistency is immediate and automatic — both paths share the same underlying storage. The existing `_lock` semaphore prevents concurrent write corruption.

### DECISION-5: SplitTrips — Server-Side Endpoint

DeliveryPlanService.SplitTrips is common logic that should be handled server-side. Exposed as POST /api/v1/characters/{uuid}/delivery-plans/{entityUuid}/split-trips.

### DECISION-6: Colony Import — Server-Side Dedupe via Standard Create

HTML imports are not supported in the web UI. The standard POST /colonies endpoint handles creation. Server-side dedup logic (matching by PlanetName+SystemName) is built into the create endpoint — if a colony with the same PlanetName+SystemName already exists, the server merges rather than creating a duplicate.

### DECISION-7: MarketService.RecordPurchase and ComputeProfitLoss — Yes

Both are exposed as API endpoints:
- POST /api/v1/characters/{uuid}/market-transactions/record-purchase (creates a buy transaction)
- GET /api/v1/characters/{uuid}/market-transactions/profit-loss?startDate=&endDate=&itemName= (read-only computation)
