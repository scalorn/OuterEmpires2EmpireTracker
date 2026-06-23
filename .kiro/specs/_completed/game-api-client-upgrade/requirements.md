# Requirements Document

## Introduction

Upgrade the existing `GameApiClient` in `OE2EmpireTracker.Common/Client/` to fully support all endpoints defined in the Outer Empires 2 Public API swagger specification (`docs/game-api-swagger.json`). 

The client already has async methods for all endpoints including colony summary, warehouse, workers, ship configuration/cargo, and accepted jobs. However, some methods lack the corresponding DTO models for deserialization, and the market endpoints (added in a recent API update) have no client methods or DTOs at all.

This spec covers:
1. Adding client methods for the 11 new market endpoints and 3 new asset detail endpoints
2. Adding DTO models for all endpoints that lack them (colony summary, asset crate/survey/blueprint, market, ship config/cargo, accepted jobs)
3. Ensuring all new methods follow the established patterns (rate limiting, Polly retry/circuit breaker, scope-aware error handling)
4. Updating the GameApiFullDiscoveryTests fixture to pull data from all new endpoints, producing real JSON examples for DTO validation

## Glossary

- **GameApiClient**: The HTTP client class responsible for all communication with the OE2 public API. Located in `OE2EmpireTracker.Common/Client/`.
- **DTO**: Data Transfer Object — a plain C# class representing the JSON response shape from an API endpoint.
- **Swagger_Spec**: The OpenAPI 3.0 specification at `docs/game-api-swagger.json` defining all endpoints and schemas.
- **ServiceResponse_Envelope**: The standard `{ success, returnCode, returnString, data, errors }` wrapper around every API response, implemented as `GameApiServiceResponse<T>`.
- **Scope**: An OAuth2 permission string (e.g. `market.listings.read`) that a token must carry to access a given endpoint.
- **Rate_Limiter**: The sliding-window SemaphoreSlim mechanism that throttles outbound requests to stay within API limits.
- **Circuit_Breaker**: The Polly circuit breaker policy that stops sending requests after consecutive failures.


## Requirements

### Requirement 1: Colony Summary DTO

**User Story:** As a developer, I want a strongly-typed DTO for the colony summary response, so that consuming code can deserialize and display colony headline information.

#### Acceptance Criteria

1. THE GameApiColonySummaryResponse DTO SHALL contain all fields defined in the `colonySummary` schema: colonyId, colonyName, canLand, characterName, colonySize, isOwnColony, systemObjectName, systemName, assetValue, hasCommandCenter, timeToFirstBuildingComplete, mustSolidifyClaim, imagePreFix, surfaceVariation, atmosVariation, hexValue
2. THE GameApiColonySummaryResponse DTO SHALL contain a notices array of GameApiColonyNotice objects (noticeText, noticeDt)
3. THE GameApiColonySummaryResponse DTO SHALL contain an industriesPresent array of GameApiColonyIndustry objects (sum, industryId, industryName)
4. THE GameApiColonySummaryResponse DTO SHALL contain operationsPresent, colonyDurability, and operationalEfficiencies nested objects matching the Swagger_Spec

### Requirement 2: Asset Crate Contents Client Method and DTO

**User Story:** As a player, I want to fetch the contents of a specific crate, so that I can see what items are stored inside it.

#### Acceptance Criteria

1. WHEN the caller invokes `GetAssetCrateAsync` with a valid appId, accessToken, and crateId (int), THE GameApiClient SHALL send a GET request to `/v1/assets/crates/{crateId}` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `assets.locations.read` scope is not granted
4. WHEN the API returns HTTP 404, THE GameApiClient SHALL return `(false, "404")` and log that the crate was not found or not owned
5. THE GameApiCrateContentsResponse DTO SHALL contain a cargo array of GameApiAssetCargoItem objects reusing the existing DTO class

### Requirement 3: Asset Survey Detail Client Method and DTO

**User Story:** As a player, I want to fetch the detailed results of a survey report, so that I can see resource accessibility, abundance, and rarity.

#### Acceptance Criteria

1. WHEN the caller invokes `GetAssetSurveyAsync` with a valid appId, accessToken, and surveyId (int), THE GameApiClient SHALL send a GET request to `/v1/assets/surveys/{surveyId}` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `assets.surveys.read` scope is not granted
4. WHEN the API returns HTTP 404, THE GameApiClient SHALL return `(false, "404")` and log that the survey was not found or not owned
5. THE GameApiSurveyResponse DTO SHALL contain a survey object with id, traceElements, scanDate, scanCharacter, encryptedId, systemObjectId, objectType, and a resources array of GameApiSurveyResource (resourceId, resourceName, accessibility, abundance, rarityClassification, maxReserve)

### Requirement 4: Asset Blueprint Detail Client Method and DTO

**User Story:** As a player, I want to fetch the full detail of a blueprint I own, so that I can see resource requirements and researchable properties.

#### Acceptance Criteria

1. WHEN the caller invokes `GetAssetBlueprintAsync` with a valid appId, accessToken, and blueprintId (int), THE GameApiClient SHALL send a GET request to `/v1/assets/blueprints/{blueprintId}` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `assets.blueprints.read` scope is not granted
4. WHEN the API returns HTTP 404, THE GameApiClient SHALL return `(false, "404")` and log that the blueprint was not found or not owned
5. THE GameApiBlueprintDetailResponse DTO SHALL contain a blueprint object (id, name, type, evolution, partTypeIcon, description, manufactureTime, manufactureAmount), a resourcesRequired array (resourceId, resourceName, resourceIcon, resourceAmount, rarityClassification), and a blueprintProperties array reusing GameApiAssetItemProperty

### Requirement 5: Market Listings Client Method and DTO

**User Story:** As a player, I want to browse live market orders visible from my current system, so that I can find items to buy or see competitor sell prices.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketListingsAsync` with a valid appId, accessToken, and required `view` parameter (string: "Buy", "Sell", or "All"), THE GameApiClient SHALL send a GET request to `/v1/market/listings` with view and optional filter parameters (range, search, type, subType, evolution, orderBy, orderByDirection) as query string parameters
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.listings.read` scope is not granted; the failure tuple SHALL be returned regardless of whether the logging operation succeeds
4. THE GameApiMarketListingsResponse DTO SHALL contain a listings array of GameApiMarketListing objects matching the `marketListing` schema
5. THE GameApiMarketListing DTO SHALL include all fields: marketId (long), locationName, distance, buyOrder, type, typeId (long), subTypeId, groupA, amountRemaining, price, ownOrder, description, icon, evolution, privateSale, sellerName, sellerFactionTag, privateSaleTo, healthPercentage, lastRepairHealth, baseItemTypeId, properties array, resourcesRequired array, and blueprintProperties array

### Requirement 6: Market Price Statistics Client Method and DTO

**User Story:** As a player, I want to get low/avg/high price statistics for a specific item, so that I can evaluate fair market value.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketPricesAsync` with a valid appId, accessToken, required `type` (string) and `typeId` (long) parameters, and optional `daysBack` (int) and `buyOrders` (bool), THE GameApiClient SHALL send a GET request to `/v1/market/prices` with all parameters as query string values
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.prices.read` scope is not granted
4. THE GameApiMarketPriceStatsResponse DTO SHALL contain lowPrice (double?), avgPrice (double?), highPrice (double?), sampleCount (int), searchRadius (string), daysSearched (int), and orderType (string)

### Requirement 7: Market Item Catalog Search Client Method and DTO

**User Story:** As a player, I want to search the item catalog by name and type, so that I can resolve the typeId needed for the listings and prices endpoints.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketItemsAsync` with a valid appId, accessToken, required `type` (string) and `search` (string) parameters, THE GameApiClient SHALL send a GET request to `/v1/market/items` with type and search as query parameters
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.items.read` scope is not granted
4. THE GameApiMarketItemsResponse DTO SHALL contain an items array of GameApiMarketItem objects (typeC, typeId as long, itemName)

### Requirement 8: Market Ship Components Client Method and DTO

**User Story:** As a player, I want to see the fitted components of a ship listed for sale, so that I can evaluate ship builds before purchasing.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketShipComponentsAsync` with a valid appId, accessToken, and marketId (long), THE GameApiClient SHALL send a GET request to `/v1/market/ships/{marketId}/components` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.listings.read` scope is not granted
4. WHEN the API returns HTTP 404, THE GameApiClient SHALL return `(false, "404")` and log that the market listing was not found or is not a ship listing
5. THE GameApiMarketShipComponentsResponse DTO SHALL contain a components array of GameApiMarketShipComponent objects (name, blueprintType, Id as long?, healthPercentage, evolution, lastRepairHealthPercentage, properties array)

### Requirement 9: Character Buy Orders Client Method and DTO

**User Story:** As a player, I want to see my open buy orders with outbid indicators, so that I can manage my market activity.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketBuyOrdersAsync` with a valid appId and accessToken, THE GameApiClient SHALL send a GET request to `/v1/market/orders/buy` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.orders.read` scope is not granted; logging and failure response SHALL be treated as an atomic operation (both must complete)
4. THE GameApiMarketBuyOrdersResponse DTO SHALL contain an orders array of GameApiMarketBuyOrder objects matching the `marketBuyOrder` schema (marketId, typeC, typeId, itemName, amountRemaining, amountOriginal, amountFilled, price, escrowRemaining, minEvolution, systemObjectId, locationName, systemId, systemName, placedDT, expiresDT, minutesRemaining, distance, isInRange, isOutbid)

### Requirement 10: Character Sell Orders Client Method and DTO

**User Story:** As a player, I want to see my open sell orders with undercut indicators, so that I can manage my market activity.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketSellOrdersAsync` with a valid appId and accessToken, THE GameApiClient SHALL send a GET request to `/v1/market/orders/sell` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200 with a valid JSON body, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 200 with a malformed or empty JSON body, THE GameApiClient SHALL return `(false, null)` and log the deserialization error
4. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.orders.read` scope is not granted
5. THE GameApiMarketSellOrdersResponse DTO SHALL contain an orders array of GameApiMarketSellOrder objects matching the `marketSellOrder` schema (marketId, typeC, typeId, itemName, evolution, amountRemaining, amountOriginal, amountSold, price, valueRemaining, healthPercentage, lastRepairHealth, characterIdTo, privateSaleToName, salesTaxEstimate, systemObjectId, locationName, systemId, systemName, placedDT, expiresDT, minutesRemaining, distance, isInRange, isUndercut)

### Requirement 11: Buy Order Competitors Client Method and DTO

**User Story:** As a player, I want to see which orders are outbidding my buy orders, so that I can decide whether to raise my bid.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketBuyCompetitorsAsync` with a valid appId, accessToken, and a comma-separated string of marketIds, THE GameApiClient SHALL send a GET request to `/v1/market/orders/buy/competitors?marketIds={ids}` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.competitors.read` scope is not granted or that a supplied marketId is not the character's own order
4. THE GameApiMarketCompetitorOrdersResponse DTO SHALL contain an orders array of GameApiMarketOrderCompetitors objects (marketId as long, competitors array of GameApiMarketCompetitor objects: marketId, price, amountRemaining, locationName, pilotName)

### Requirement 12: Sell Order Competitors Client Method and DTO

**User Story:** As a player, I want to see which orders are undercutting my sell orders, so that I can decide whether to lower my price.

#### Acceptance Criteria

1. WHEN the caller invokes `GetMarketSellCompetitorsAsync` with a valid appId, accessToken, and a comma-separated string of marketIds, THE GameApiClient SHALL send a GET request to `/v1/market/orders/sell/competitors?marketIds={ids}` with Bearer and X-App-Id headers
2. WHEN the API returns HTTP 200, THE GameApiClient SHALL return `(true, json)` containing the raw response body
3. WHEN the API returns HTTP 403, THE GameApiClient SHALL return `(false, "403")` and log that `market.competitors.read` scope is not granted or that a supplied marketId is not the character's own order
4. THE response SHALL reuse the same GameApiMarketCompetitorOrdersResponse DTO as the buy competitors endpoint (same response shape)

### Requirement 13: Ship Configuration DTO

**User Story:** As a developer, I want a strongly-typed DTO for the ship configuration response, so that consuming code can display the active ship's fit.

#### Acceptance Criteria

1. THE GameApiShipConfigurationResponse DTO SHALL contain shipId (int), a summary object (GameApiShipSummary), and a components array (GameApiShipComponent)
2. THE GameApiShipSummary DTO SHALL contain all fields from the `shipSummary` schema: shipAssetName, shipName, shipTypeId, shipType, baseModelType, transponder, shipTypeDescription, fuelCap, currentFuel, engCap, availableEngCap, currentCargo, cargoCap, currentHopper, hopperCap, powerCap, availablePower, powerRegenPerSecond, jumpRange, maxSpeed, smallWeaponMounts, mediumWeaponMounts, largeWeaponMounts, locationId, locationName, systemName, systemId, shipSize, isDamaged, overallIntegrity, totalMass, cargoMass, maxAcceleration, maxTurnRate, jumpFuelConsumptionPerSecond, maxJumpRange, xCoord, yCoord, angle, and shipId
3. THE GameApiShipComponent DTO SHALL contain name, evolution, blueprintType, Id (int?), healthPercentage (double?), lastRepairHealthPercentage, properties array, and munitionDetails (nullable GameApiShipComponentMunition)
4. THE GameApiShipComponentMunition DTO SHALL contain name, evolution, containingAmount, containingType (int?), icon, and munitionProperties array

### Requirement 14: Ship Cargo DTO

**User Story:** As a developer, I want a strongly-typed DTO for the ship cargo response, so that consuming code can display cargo hold contents.

#### Acceptance Criteria

1. THE GameApiShipCargoResponse DTO SHALL contain a cargo array of GameApiAssetCargoItem objects, reusing the existing DTO class from the asset detail response

### Requirement 15: Accepted Jobs DTO

**User Story:** As a developer, I want a strongly-typed DTO for the accepted jobs response, so that consuming code can display job tracking information.

#### Acceptance Criteria

1. THE GameApiAcceptedJobsResponse DTO SHALL contain a jobs array of GameApiAcceptedJob objects
2. THE GameApiAcceptedJob DTO SHALL contain all fields from the `acceptedJob` schema: jobId, jobName, jobType, track, detail, xp, credits (double), bonus, completeByBonus (DateTime?), characterId, charName, info1, systemName1, info2, systemName2, jobIssuedLocId, jobIssuedLocationName, jobIssuedLocationReputation

### Requirement 16: Consistent Error Handling Across All New Client Methods

**User Story:** As a developer, I want all new endpoint methods to follow the same error handling pattern as existing methods.

#### Acceptance Criteria

1. WHEN an accessToken parameter is null or empty, THE GameApiClient SHALL return `(false, null)` without making a network request
2. WHEN the API returns HTTP 401, THE GameApiClient SHALL return `(false, "401")` and log that the token is invalid or expired
3. WHEN a BrokenCircuitException occurs, THE GameApiClient SHALL return `(false, null)` and log that the circuit breaker is open
4. WHEN an HttpRequestException occurs, THE GameApiClient SHALL return `(false, null)` and log the exception details
5. WHEN a TaskCanceledException occurs, THE GameApiClient SHALL return `(false, null)` and log that the request timed out
6. THE GameApiClient SHALL call `AcquireRateLimitTokenAsync` before executing each new endpoint request to respect the sliding window rate limit
7. WHEN the API returns HTTP 200 but the response body is null, empty, or not valid JSON, THE GameApiClient SHALL return `(false, null)` and log the deserialization error
8. WHEN an HTTP 403 response is received, THE failure tuple SHALL be returned to the caller regardless of whether the associated logging operation succeeds

### Requirement 17: DTO Conventions

**User Story:** As a developer, I want all new DTOs to follow consistent naming and serialization conventions.

#### Acceptance Criteria

1. THE DTO classes SHALL be placed in the `OE2EmpireTracker.Common/Models/` folder alongside existing game API DTOs
2. THE DTO classes SHALL use `[JsonProperty("camelCaseName")]` attributes from Newtonsoft.Json to map JSON property names to C# PascalCase property names
3. THE DTO classes SHALL use nullable types (e.g. `int?`, `double?`) for properties marked `nullable: true` in the Swagger_Spec
4. THE DTO classes SHALL use `DateTime?` for nullable date-time properties and `DateTime` for required date-time properties
5. THE DTO classes SHALL follow the existing naming convention: `GameApi{SchemaName}` prefix (e.g. `GameApiMarketListing`, `GameApiShipConfiguration`)
6. THE DTO classes SHALL reuse existing shared types where schemas overlap (e.g. `GameApiAssetCargoItem` for cargo arrays, `GameApiAssetItemProperty` for property arrays, `GameApiColonyCapacities` for capacities)

### Requirement 18: Market Listings Filter Parameters

**User Story:** As a developer, I want the market listings method to accept all filter parameters defined in the swagger spec, so that callers can narrow results without client-side filtering.

#### Acceptance Criteria

1. THE `GetMarketListingsAsync` method SHALL accept optional parameters: range (int?), search (string), type (string), subType (string), evolution (int?), orderBy (string), orderByDirection (string)
2. WHEN optional parameters are null or empty, THE GameApiClient SHALL omit them from the query string
3. WHEN the `view` parameter is provided, THE GameApiClient SHALL always include it in the query string as it is required by the API
4. THE method SHALL URL-encode the search parameter to handle special characters and the `||` multi-term separator

### Requirement 19: Full Discovery Tool Coverage

**User Story:** As a developer, I want the GameApiFullDiscoveryTests fixture to pull data from every new endpoint, so that I have real JSON examples for building and validating the new DTOs and client methods.

#### Acceptance Criteria

1. THE GameApiFullDiscoveryTests SHALL create a `market` output subdirectory alongside existing categories (character, colonies, banking, assets, jobs, killmails, mail, ship)
2. THE discovery fixture SHALL pull market listings (view "All") and save to market/listings.json
3. THE discovery fixture SHALL pull market prices for at least one item found in the listings response (using its type and typeId) and save to market/prices.json
4. THE discovery fixture SHALL pull market item catalog search (searching for a common item type) and save to market/items.json
5. THE discovery fixture SHALL pull the character's buy orders and save to market/buy-orders.json
6. THE discovery fixture SHALL pull the character's sell orders and save to market/sell-orders.json
7. WHEN buy orders exist, THE discovery fixture SHALL pull buy order competitors for up to the first 5 order marketIds and save to market/buy-competitors.json
8. WHEN sell orders exist, THE discovery fixture SHALL pull sell order competitors for up to the first 5 order marketIds and save to market/sell-competitors.json
9. WHEN a ship listing is found in the market listings, THE discovery fixture SHALL pull ship components for that listing and save to market/ship-components.json
10. THE discovery fixture SHALL pull asset crate contents for the first crate found in the asset locations response and save to assets/crate-{crateId}.json
11. THE discovery fixture SHALL pull asset survey detail for the first survey found in the asset locations response and save to assets/survey-{surveyId}.json
12. THE discovery fixture SHALL pull asset blueprint detail for the first blueprint found in the asset locations response and save to assets/blueprint-{blueprintId}.json
13. WHEN an endpoint returns HTTP 403 (scope not granted), THE discovery fixture SHALL record it as skipped with the scope reason rather than failing the test
14. THE discovery fixture SHALL use the same test ordering pattern (Test attribute with Order) as existing tests, placing market endpoints after ship (Order 16+) and asset detail endpoints within the existing asset section
