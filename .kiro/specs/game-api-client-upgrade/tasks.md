# Implementation Plan: Game API Client Upgrade

## Overview

Extend the GameApiClient with 14 new methods (3 asset detail + 11 market) and ~25 new DTO classes covering all endpoints in the OE2 Public API swagger specification. Each task adds a small, testable slice: DTO file(s) → client method(s) → discovery test(s).

## Tasks

- [x] 1. Create asset detail DTO files
  - [x] 1.1 Create GameApiCrateContentsResponse.cs
    - Define GameApiCrateContentsResponse with cargo array reusing GameApiAssetCargoItem
    - File: `OE2EmpireTracker.Common/Models/GameApiCrateContentsResponse.cs`
    - _Requirements: 2.5_
  - [x] 1.2 Create GameApiSurveyResponse.cs
    - Define GameApiSurveyResponse, GameApiSurveyDetail, GameApiSurveyResource
    - File: `OE2EmpireTracker.Common/Models/GameApiSurveyResponse.cs`
    - _Requirements: 3.5_
  - [x] 1.3 Create GameApiBlueprintDetailResponse.cs
    - Define GameApiBlueprintDetailResponse, GameApiBlueprintInfo, GameApiBlueprintResource; reuse GameApiAssetItemProperty for blueprintProperties
    - File: `OE2EmpireTracker.Common/Models/GameApiBlueprintDetailResponse.cs`
    - _Requirements: 4.5_

- [x] 2. Create colony summary DTO file — response and notices
  - [x] 2.1 Create GameApiColonySummaryResponse.cs with response class and GameApiColonyNotice
    - Define GameApiColonySummaryResponse (colonyId, colonyName, canLand, characterName, colonySize, isOwnColony, systemObjectName, systemName, assetValue, hasCommandCenter, timeToFirstBuildingComplete, mustSolidifyClaim, imagePreFix, surfaceVariation, atmosVariation, hexValue, notices array, industriesPresent array, operationsPresent, colonyDurability, operationalEfficiencies) and GameApiColonyNotice (noticeText, noticeDt)
    - File: `OE2EmpireTracker.Common/Models/GameApiColonySummaryResponse.cs`
    - _Requirements: 1.1, 1.2_
  - [x] 2.2 Add nested colony summary types to GameApiColonySummaryResponse.cs
    - Define GameApiColonyIndustry (sum, industryId, industryName), GameApiColonyOperation, GameApiColonyDurability, GameApiColonyOperationalEfficiency matching Swagger_Spec
    - File: `OE2EmpireTracker.Common/Models/GameApiColonySummaryResponse.cs`
    - _Requirements: 1.3, 1.4_

- [x] 3. Create ship configuration DTO file — response and components
  - [x] 3.1 Create GameApiShipConfigurationResponse.cs with response, component, and munition classes
    - Define GameApiShipConfigurationResponse (shipId, summary, components array), GameApiShipComponent (name, evolution, blueprintType, Id, healthPercentage, lastRepairHealthPercentage, properties array, munitionDetails), GameApiShipComponentProperty (propertyName, friendlyPropertyName, propertyValue, unit), GameApiShipComponentMunition (name, evolution, containingAmount, containingType, icon, munitionProperties array)
    - File: `OE2EmpireTracker.Common/Models/GameApiShipConfigurationResponse.cs`
    - _Requirements: 13.1, 13.3, 13.4_
  - [x] 3.2 Add GameApiShipSummary to GameApiShipConfigurationResponse.cs
    - Define GameApiShipSummary with all ~40 fields from the shipSummary schema (shipAssetName, shipName, shipTypeId, shipType, baseModelType, transponder, shipTypeDescription, fuelCap, currentFuel, engCap, availableEngCap, currentCargo, cargoCap, currentHopper, hopperCap, powerCap, availablePower, powerRegenPerSecond, jumpRange, maxSpeed, smallWeaponMounts, mediumWeaponMounts, largeWeaponMounts, locationId, locationName, systemName, systemId, shipSize, isDamaged, overallIntegrity, totalMass, cargoMass, maxAcceleration, maxTurnRate, jumpFuelConsumptionPerSecond, maxJumpRange, xCoord, yCoord, angle, shipId)
    - File: `OE2EmpireTracker.Common/Models/GameApiShipConfigurationResponse.cs`
    - _Requirements: 13.2_

- [x] 4. Create ship cargo and accepted jobs DTO files
  - [x] 4.1 Create GameApiShipCargoResponse.cs
    - Define GameApiShipCargoResponse reusing GameApiAssetCargoItem
    - File: `OE2EmpireTracker.Common/Models/GameApiShipCargoResponse.cs`
    - _Requirements: 14.1_
  - [x] 4.2 Create GameApiAcceptedJobsResponse.cs
    - Define GameApiAcceptedJobsResponse and GameApiAcceptedJob with all fields from acceptedJob schema
    - File: `OE2EmpireTracker.Common/Models/GameApiAcceptedJobsResponse.cs`
    - _Requirements: 15.1, 15.2_

- [x] 5. Create market DTO files — listings
  - [x] 5.1 Create GameApiMarketListingsResponse.cs with response and supporting types
    - Define GameApiMarketListingsResponse (listings array), GameApiMarketListingProperty (modTypeId, propertyName, friendlyPropertyName, propertyValue, unit), GameApiMarketListingResource (resourceId, resourceName, resourceIcon, resourceAmount, rarityClassification)
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketListingsResponse.cs`
    - _Requirements: 5.4_
  - [x] 5.2 Add GameApiMarketListing class to GameApiMarketListingsResponse.cs
    - Define GameApiMarketListing with all ~25 fields (marketId, locationName, distance, buyOrder, type, typeId, subTypeId, groupA, amountRemaining, price, ownOrder, description, icon, evolution, privateSale, sellerName, sellerFactionTag, privateSaleTo, healthPercentage, lastRepairHealth, baseItemTypeId, properties array, resourcesRequired array, blueprintProperties array)
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketListingsResponse.cs`
    - _Requirements: 5.5_

- [x] 6. Create market DTO files — prices and items
  - [x] 6.1 Create GameApiMarketPriceStatsResponse.cs
    - Define GameApiMarketPriceStatsResponse (lowPrice, avgPrice, highPrice, sampleCount, searchRadius, daysSearched, orderType)
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketPriceStatsResponse.cs`
    - _Requirements: 6.4_
  - [x] 6.2 Create GameApiMarketItemsResponse.cs
    - Define GameApiMarketItemsResponse and GameApiMarketItem (typeC, typeId as long, itemName)
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketItemsResponse.cs`
    - _Requirements: 7.4_

- [x] 7. Create market DTO files — ship components, orders, competitors
  - [x] 7.1 Create GameApiMarketShipComponentsResponse.cs
    - Define GameApiMarketShipComponentsResponse and GameApiMarketShipComponent (name, blueprintType, Id, healthPercentage, evolution, lastRepairHealthPercentage, properties array)
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketShipComponentsResponse.cs`
    - _Requirements: 8.5_
  - [x] 7.2 Create GameApiMarketOrdersResponse.cs
    - Define GameApiMarketBuyOrdersResponse, GameApiMarketBuyOrder, GameApiMarketSellOrdersResponse, GameApiMarketSellOrder
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketOrdersResponse.cs`
    - _Requirements: 9.4, 10.5_
  - [x] 7.3 Create GameApiMarketCompetitorsResponse.cs
    - Define GameApiMarketCompetitorOrdersResponse, GameApiMarketOrderCompetitors, GameApiMarketCompetitor
    - File: `OE2EmpireTracker.Common/Models/GameApiMarketCompetitorsResponse.cs`
    - _Requirements: 11.4, 12.4_

- [x] 8. Checkpoint — Verify DTO compilation
  - Ensure all DTO files compile with zero errors and zero warnings. Run MSBuild on the full solution.

- [x] 9. Add asset detail client methods to GameApiClient
  - [x] 9.1 Add GetAssetCrateAsync method
    - Implement GET `/v1/assets/crates/{crateId}` following existing method template; handles 401/403/404/circuit breaker/timeout
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  - [x] 9.2 Add GetAssetSurveyAsync method
    - Implement GET `/v1/assets/surveys/{surveyId}` following existing method template; handles 401/403/404/circuit breaker/timeout
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  - [x] 9.3 Add GetAssetBlueprintAsync method
    - Implement GET `/v1/assets/blueprints/{blueprintId}` following existing method template; handles 401/403/404/circuit breaker/timeout
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 10. Add market client methods — listings and prices
  - [x] 10.1 Add GetMarketListingsAsync method
    - Implement GET `/v1/market/listings` with required view param and optional filters (range, search, type, subType, evolution, orderBy, orderByDirection); URL-encode search with Uri.EscapeDataString
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 5.1, 5.2, 5.3_
  - [x] 10.2 Add GetMarketPricesAsync method
    - Implement GET `/v1/market/prices` with required type and typeId params, optional daysBack and buyOrders
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 6.1, 6.2, 6.3_
  - [x] 10.3 Add GetMarketItemsAsync method
    - Implement GET `/v1/market/items` with required type and search params
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 11. Add market client methods — ship components and orders
  - [x] 11.1 Add GetMarketShipComponentsAsync method
    - Implement GET `/v1/market/ships/{marketId}/components`; handles 404 for non-ship listings
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [x] 11.2 Add GetMarketBuyOrdersAsync method
    - Implement GET `/v1/market/orders/buy`; no path params, no 404 handling
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 9.1, 9.2, 9.3_
  - [x] 11.3 Add GetMarketSellOrdersAsync method
    - Implement GET `/v1/market/orders/sell`; no path params, no 404 handling
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [x] 12. Add market client methods — competitors
  - [x] 12.1 Add GetMarketBuyCompetitorsAsync method
    - Implement GET `/v1/market/orders/buy/competitors?marketIds={ids}`; URL-encode marketIds
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 11.1, 11.2, 11.3_
  - [x] 12.2 Add GetMarketSellCompetitorsAsync method
    - Implement GET `/v1/market/orders/sell/competitors?marketIds={ids}`; URL-encode marketIds
    - File: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 13. Checkpoint — Verify client methods compile
  - Ensure all new client methods compile with zero errors and zero warnings. Run MSBuild on the full solution.


- [x] 14. Add asset detail discovery tests
  - [x] 14.1 Add GetAssetCrateDetail discovery test (Order 7)
    - Find first crate in assetLocationsJson, call GetAssetCrateAsync, save to assets/crate-{crateId}.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.10, 19.13_
  - [x] 14.2 Add GetAssetSurveyDetail discovery test (Order 8)
    - Find first survey in assetLocationsJson, call GetAssetSurveyAsync, save to assets/survey-{surveyId}.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.11, 19.13_
  - [x] 14.3 Add GetAssetBlueprintDetail discovery test (Order 9)
    - Find first blueprint in assetLocationsJson, call GetAssetBlueprintAsync, save to assets/blueprint-{bpId}.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.12, 19.13_

- [x] 15. Add market discovery tests — listings, prices, items, ship components
  - [x] 15.1 Add GetMarketListings discovery test (Order 16)
    - Call GetMarketListingsAsync with view "All", save to market/listings.json; store result for dependent tests; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.1, 19.2, 19.14_
  - [x] 15.2 Add GetMarketPrices discovery test (Order 17)
    - Extract type/typeId from first listing in saved listings JSON, call GetMarketPricesAsync, save to market/prices.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.3, 19.13_
  - [x] 15.3 Add GetMarketItems discovery test (Order 18)
    - Call GetMarketItemsAsync with a common type search term, save to market/items.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.4, 19.13_
  - [x] 15.4 Add GetMarketShipComponents discovery test (Order 19)
    - Find a ship listing in listings JSON, call GetMarketShipComponentsAsync, save to market/ship-components.json; skip on 403/404
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.9, 19.13_

- [x] 16. Add market discovery tests — orders and competitors
  - [x] 16.1 Add GetMarketBuyOrders discovery test (Order 20)
    - Call GetMarketBuyOrdersAsync, save to market/buy-orders.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.5, 19.13_
  - [x] 16.2 Add GetMarketSellOrders discovery test (Order 21)
    - Call GetMarketSellOrdersAsync, save to market/sell-orders.json; skip on 403
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.6, 19.13_
  - [x] 16.3 Add GetMarketBuyCompetitors discovery test (Order 22)
    - Extract first 5 marketIds from buy orders JSON, call GetMarketBuyCompetitorsAsync, save to market/buy-competitors.json; skip on 403 or if no buy orders
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.7, 19.13_
  - [x] 16.4 Add GetMarketSellCompetitors discovery test (Order 23)
    - Extract first 5 marketIds from sell orders JSON, call GetMarketSellCompetitorsAsync, save to market/sell-competitors.json; skip on 403 or if no sell orders
    - File: `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
    - _Requirements: 19.8, 19.13_

- [x] 17. Final checkpoint — Ensure full solution builds and tests compile
  - Ensure all tests compile and pass (explicit discovery tests skipped without credentials). Run MSBuild on full solution with zero errors and zero warnings.

## Notes

- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- All new client methods follow the exact template in design.md (rate limit → execute → status handling → exception handling)
- All DTOs follow existing conventions: `OE2EmpireTracker.Client` namespace, `[JsonProperty]` attributes, nullable types for nullable swagger fields
- Discovery tests are integration tests (Explicit fixture) that require real API credentials and won't run in CI
- Requirements 16.1–16.8 (consistent error handling) and 17.1–17.6 (DTO conventions) and 18.1–18.4 (filter params) are cross-cutting concerns satisfied by following the template in design.md; they apply to all client method and DTO tasks respectively

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "2.1", "2.2", "3.1", "3.2", "4.1", "4.2", "5.1", "5.2", "6.1", "6.2", "7.1", "7.2", "7.3"] },
    { "id": 1, "tasks": ["9.1", "9.2", "9.3", "10.1", "10.2", "10.3", "11.1", "11.2", "11.3", "12.1", "12.2"] },
    { "id": 2, "tasks": ["14.1", "14.2", "14.3", "15.1"] },
    { "id": 3, "tasks": ["15.2", "15.3", "15.4", "16.1", "16.2"] },
    { "id": 4, "tasks": ["16.3", "16.4"] }
  ]
}
```
