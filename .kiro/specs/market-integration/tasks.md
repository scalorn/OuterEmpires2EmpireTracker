# Implementation Plan

## Overview

This implementation plan covers the market integration feature — syncing market data from the OE2 Public API into a unified local dataset, browsing merged market orders offline, and connecting stock targets to market sell orders. The plan is decomposed into 9 phases with 44 tasks across data models, infrastructure, core service logic, sync integration, alert system, stock target extension, property-based tests, and UI.

## Task Dependency Graph

```json
{
  "waves": [
    {
      "name": "Phase 1: Data Models",
      "tasks": ["1.1", "2.1", "2.2", "2.3", "2.4", "2.5"]
    },
    {
      "name": "Phase 2: Infrastructure",
      "tasks": ["3.1", "4.1"]
    },
    {
      "name": "Phase 3: Core Service",
      "tasks": ["5.1", "5.2", "5.3", "5.4", "5.5", "5.6"]
    },
    {
      "name": "Phase 4: Sync Integration",
      "tasks": ["6.1", "6.2", "6.3", "6.4"]
    },
    {
      "name": "Phase 5: Alerts and Stock Targets",
      "tasks": ["7.1", "8.1", "8.2", "8.3"]
    },
    {
      "name": "Phase 6: Property Tests",
      "tasks": ["9.1", "9.2", "9.3", "9.4", "9.5", "9.6", "9.7", "9.8", "9.9"]
    },
    {
      "name": "Phase 7: UI Controls Layout",
      "tasks": ["10.1", "10.2", "10.3", "10.4", "10.5", "10.6", "10.7", "10.8"]
    },
    {
      "name": "Phase 8: UI Wiring and Service Logic",
      "tasks": ["10.9", "10.10", "10.11", "10.12", "10.13"]
    }
  ]
}
```

## Tasks

### Phase 1: Data Models

- [x] 1.1 Extend MarketListing model with API sync fields — Add nullable fields to existing `MarketListing` class: `MarketId` (long?), `BuyOrder` (bool), `BaseItemTypeID`, `ResourcePurity`, `GameTypeCode`, `GameTypeId` (long?), `GameSubTypeId`, location details (LocationName, SystemId, SystemName, GameLocationId), order details (AmountRemaining, AmountOriginal, AmountSold, EscrowRemaining, SalesTaxEstimate, ValueRemaining, Evolution, HealthPercentage), seller/buyer info (SellerName, SellerFactionTag, PrivateSale, BuyerName, BuyerFactionTag), timing (PlacedDT, ExpiresDT), sync tracking (SyncedByCharacterUUID, SyncTimestamp). Satisfies: Req 1 Criterion 1-2, Req 3 Criterion 1. Output: `OE2EmpireTracker.Common/Models/MarketListing.cs`. Verification: getDiagnostics — zero errors/warnings.
- [x] 2.1 Create MarketSyncData model class — Create `MarketSyncData` with properties: PriceStats (List<StoredPriceStats>), SyncMetadata (List<CharacterSyncMetadata>), Alerts (List<MarketAlert>), SavedSearches (List<SavedMarketSearch>). All default to empty lists. Satisfies: Req 1 Criterion 3, Req 14 Criterion 1. Output: `OE2EmpireTracker.Common/Models/MarketSyncData.cs`. Verification: getDiagnostics.
- [x] 2.2 Create SavedMarketSearch model class — Properties: UUID, CharacterUUID, Name, Enabled (bool), ItemType (with StringEnumConverter), BaseItemTypeID, ResourcePurity, SearchText, BuyOrdersOnly (bool?), GameTypeCode (default "all"), LastExecutedTimestamp, LastResultCount (int). Satisfies: Req 1 Criterion 1. Output: `OE2EmpireTracker.Common/Models/SavedMarketSearch.cs`. Verification: getDiagnostics.
- [x] 2.3 Create CharacterSyncMetadata model class — Properties: CharacterUUID, SystemId (int), SystemName, RangeJas (int), SystemsInRange (HashSet<int>), LastSyncTimestamp, GrantedScopes (List<string>). Satisfies: Req 1 Criterion 3, Req 14 Criterion 1. Output: `OE2EmpireTracker.Common/Models/CharacterSyncMetadata.cs`. Verification: getDiagnostics.
- [x] 2.4 Create StoredPriceStats model class — Properties: ItemType (with StringEnumConverter), BaseItemTypeID, ItemName, ResourcePurity, GameTypeCode, GameTypeId (long), LowPrice (decimal?), AvgPrice (decimal?), HighPrice (decimal?), SampleCount (int), SearchRadius, DaysSearched (int), OrderType, FetchedTimestamp, FetchedByCharacterUUID. Satisfies: Req 5 Criterion 1-2. Output: `OE2EmpireTracker.Common/Models/StoredPriceStats.cs`. Verification: getDiagnostics.
- [x] 2.5 Create MarketAlert model and enums — Create MarketAlert class (UUID, Name, Enabled, AlertType, ItemType, BaseItemTypeID, ResourcePurity, PriceCondition, PriceThreshold, StationUUID, LastTriggeredTimestamp, LastTriggeredMarketId) and enums MarketAlertType (SellOrderAppears, BuyOrderAppears) and PriceCondition (AnyPrice, AtOrBelow, AtOrAbove). Satisfies: Design Alert System. Output: `OE2EmpireTracker.Common/Models/MarketAlert.cs`. Verification: getDiagnostics.

### Phase 2: Infrastructure

- [x] 3.1 Add MarketSyncData storage to PlayerContext — Add MarketSyncData field to PlayerRoot. In PlayerContext: add _marketSyncData field, expose MarketSyncData property, initialize in InitMarketSyncData(playerRoot), include in WriteContext() serialization. Add FindMarketListingByMarketId(long) method with dictionary cache. Satisfies: Req 1 Criterion 2-3, Req 12 Criterion 1. Output: `OE2EmpireTracker.Common/Services/PlayerContext.cs`, `OE2EmpireTracker.Common/Models/PlayerRoot.cs`. Verification: getDiagnostics; existing tests pass.
- [x] 4.1 Create SystemGridIndex for range queries — Constructor accepts SystemRepository, builds grid with 50 JAS cell size. ComputeSystemsInRange(int systemId, int rangeJas) returns HashSet<int> using bounding-box cell overlap then exact distance. IsInRange convenience method. Satisfies: Req 14 Criterion 1, 4. Output: `OE2EmpireTracker.Common/Services/SystemGridIndex.cs`. Verification: getDiagnostics.

### Phase 3: Core Service

- [x] 5.1 Create MarketDataService — merge logic and DTO mapping — Constructor accepts PlayerContext and SystemGridIndex. ProcessMarketListings maps DTO entries to MarketListing domain objects using item type mapping, upserts by MarketId, deduplicates. Does NOT handle stale removal (separate task). Satisfies: Req 1 Criterion 1-2, 7. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 5.2 Implement stale order removal in MarketDataService — Add RemoveStaleOrders(HashSet<long> freshMarketIds, string characterUUID, CharacterSyncMetadata metadata). For each synced listing where SystemId is in metadata.SystemsInRange and MarketId not in freshMarketIds, remove it. Out-of-range and ambiguous orders preserved. Satisfies: Req 1 Criterion 4-6, Req 14 Criterion 1-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 5.3 Implement own orders processing in MarketDataService — ProcessOwnBuyOrders and ProcessOwnSellOrders map own order DTOs to MarketListing entries with OwnerUUID=characterUUID, includes escrow, tax estimate, outbid/undercut flags, amounts. Satisfies: Req 3 Criterion 1-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 5.4 Implement competitor processing in MarketDataService — ProcessBuyCompetitors and ProcessSellCompetitors store competitor orders linked to own orders via MarketId. Satisfies: Req 4 Criterion 1-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 5.5 Implement price stats processing in MarketDataService — ProcessPriceStats maps price DTO to StoredPriceStats, upserts into MarketSyncData.PriceStats keyed by composite item identity. ResolveItemForPriceLookup validates typeCode+typeId resolution. Satisfies: Req 5 Criterion 1-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 5.6 Implement private sale visibility filtering — GetVisibleOrders(characterUUID) returns all public orders plus private orders visible to seller or named buyer. Satisfies: Req 2 Criterion 1-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.

### Phase 4: Sync Integration

- [x] 6.1 Modify QueueSyncService — persist market listings — Modify CreateMarketListingsItem to accept SavedMarketSearch params, pass DTO to MarketDataService.ProcessMarketListings. On partial failure retain previous data. Add MarketDataService as constructor dependency. Satisfies: Req 1 Criterion 1, 8. Output: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`. Verification: getDiagnostics; existing tests pass.
- [x] 6.2 Modify QueueSyncService — persist own orders — Modify CreateMarketBuyOrdersItem and CreateMarketSellOrdersItem to pass DTOs to MarketDataService.ProcessOwnBuyOrders/ProcessOwnSellOrders. Satisfies: Req 3 Criterion 1. Output: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`. Verification: getDiagnostics.
- [x] 6.3 Modify QueueSyncService — persist competitors — Add CreateMarketBuyCompetitorsItem and CreateMarketSellCompetitorsItem work items cascading from own order results. Satisfies: Req 4 Criterion 1-2. Output: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`. Verification: getDiagnostics.
- [x] 6.4 Implement multi-character sync orchestration — RunMarketSyncAsync iterates all configured characters sequentially, exchanges tokens, executes saved searches, fetches own orders/competitors, updates CharacterSyncMetadata, runs stale removal, WriteContext once. Skips failed token exchanges with warning. Satisfies: Req 1 Criterion 2-3, Req 9 Criterion 2-5, Req 11 Criterion 1-2. Output: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`. Verification: getDiagnostics.

### Phase 5: Alerts and Stock Targets

- [x] 7.1 Implement market alert evaluation in MarketDataService — EvaluateAlerts(freshOrders) filters by AlertType, item key, station, price condition. Excludes already-triggered MarketIds. Fires MarketAlertTriggeredEvent. Updates tracking fields. Satisfies: Design Alert Evaluation. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [x] 8.1 Add Market and StationPlusMarket to StockTargetScope enum — Add two new values to existing enum. Satisfies: Req 7 Criterion 1. Output: `OE2EmpireTracker.Common/Models/StockTarget.cs`. Verification: getDiagnostics.
- [x] 8.2 Implement Market scope counting in StockTargetService — Handle StockTargetScope.Market: filter to own sell orders (OwnerUUID match, BuyOrder=false, MarketId not null), match item composite key, optional station filter. Sum AmountRemaining. Subtract in-production items. Satisfies: Req 7 Criterion 2-3, Req 8 Criterion 1-2. Output: `OE2EmpireTracker.Common/Services/StockTargetService.cs`. Verification: getDiagnostics.
- [x] 8.3 Implement StationPlusMarket scope counting — Compute station warehouse qty (existing Station logic) + market qty (Market logic filtered to same station). Return sum. Satisfies: Req 8 Criterion 1-2. Output: `OE2EmpireTracker.Common/Services/StockTargetService.cs`. Verification: getDiagnostics.

### Phase 6: Property Tests

- [~] 9.1 Test merge idempotency property — FsCheck: syncing same DTO twice produces identical dataset. Satisfies: Correctness Property 1 (Req 1 Criterion 2). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console — test passes.
- [~] 9.2 Test stale removal soundness property — FsCheck: every removed order was within range AND absent from fresh results. Satisfies: Correctness Property 2 (Req 1 Criterion 4, Req 14 Criterion 2). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console.
- [~] 9.3 Test stale removal completeness property — FsCheck: every in-range order absent from fresh results IS removed. Satisfies: Correctness Property 3 (Req 14 Criterion 1-2). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console.
- [~] 9.4 Test out-of-range preservation property — FsCheck: orders outside range never removed or modified after sync. Satisfies: Correctness Property 4 (Req 1 Criterion 5, Req 14 Criterion 3). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console.
- [~] 9.5 Test deduplication by MarketId property — FsCheck: no two listings share same non-null MarketId after syncs. Satisfies: Correctness Property 5 (Req 1 Criterion 2). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console.
- [~] 9.6 Test private sale isolation property — FsCheck: private sale visible only to seller and named buyer. Satisfies: Correctness Property 6 (Req 2 Criterion 2-3). Output: `OE2EmpireTracker.Tests/Services/MarketDataServicePropertyTests.cs`. Verification: vstest.console.
- [~] 9.7 Test stock target shortfall non-negative property — FsCheck: adjusted shortfall always >= 0. Satisfies: Correctness Property 7 (Req 7 Criterion 3-4). Output: `OE2EmpireTracker.Tests/Services/StockTargetMarketScopePropertyTests.cs`. Verification: vstest.console.
- [~] 9.8 Test SystemGridIndex correctness — FsCheck: ComputeSystemsInRange matches brute-force scan. Integration test with actual galaxy data. Satisfies: Req 14 Criterion 1, 4. Output: `OE2EmpireTracker.Tests/Services/SystemGridIndexTests.cs`. Verification: vstest.console.
- [~] 9.9 Test alert evaluation correctness — Unit tests: alert fires for matching orders, respects price conditions, skips triggered MarketIds, disabled alerts don't fire. Satisfies: Design Alert Evaluation. Output: `OE2EmpireTracker.Tests/Services/MarketAlertEvaluationTests.cs`. Verification: vstest.console.

### Phase 7: UI — Controls Layout

- [~] 10.1 FormMarket — Saved Searches tab controls — Add "Saved Searches" TabPage to FormMarket Designer with controls: cmbSearchCharacter, dgvSavedSearches, txtSearchName, cmbSearchType, cmbSearchItem, cmbSearchPurity, cmbSearchOrderType, chkRunOnSync, cmdTestSearch, cmdSaveSearch, cmdDeleteSearch, dgvTestResults. Layout only, no event handlers. Satisfies: Req 6 Criterion 1. Output: FormMarket.Designer.cs. Verification: getDiagnostics.
- [~] 10.2 FormMarket — Saved Searches tab wiring — Wire event handlers for Saved Searches tab: populate character combo, load/save/delete searches via MarketDataService, dgvSavedSearches selection handling, Test Search button calls API and displays results in dgvTestResults. Satisfies: Req 6 Criterion 1-3. Output: FormMarket.cs. Verification: getDiagnostics.
- [~] 10.3 FormMarket — My Orders tab controls — Add "My Orders" TabPage to FormMarket Designer with controls: cmbOrderCharacter, cmbOrderType, txtOrderSearch, cmbOrderLocation, dgvSellOrders, dgvBuyOrders. Layout only, no event handlers. Satisfies: Req 3 Criterion 2. Output: FormMarket.Designer.cs. Verification: getDiagnostics.
- [~] 10.4 FormMarket — My Orders tab wiring — Wire event handlers: populate sell/buy grids from MarketDataService.GetOwnOrders, highlight outbid/undercut rows, show last-synced timestamp, competitor detail on row selection (ordered most-to-least threatening). Satisfies: Req 3 Criterion 2-5, Req 4 Criterion 3-4. Output: FormMarket.cs. Verification: getDiagnostics.
- [~] 10.5 FormMarket — Prices tab controls — Add "Prices" TabPage to FormMarket Designer with controls: cmbPriceType, cmbPriceItem, cmbPricePurity, numDaysBack, chkBuyOrderPrices, cmdFetchPrices, dgvPriceStats, cmdAutoPopulate, cmbAutoPopPlan. Layout only. Satisfies: Req 5 Criterion 2. Output: FormMarket.Designer.cs. Verification: getDiagnostics.
- [~] 10.6 FormMarket — Prices tab wiring — Wire Fetch Prices button to call API via typed client, pass result to MarketDataService.ProcessPriceStats. Populate dgvPriceStats from stored stats. Handle days-back and buy-order params. Satisfies: Req 5 Criterion 2-4. Output: FormMarket.cs. Verification: getDiagnostics.
- [~] 10.7 FormMarket — Alerts tab controls — Add "Alerts" TabPage to FormMarket Designer with controls: dgvAlerts, txtAlertName, cmbAlertType, cmbAlertItemType, cmbAlertItem, cmbAlertPurity, cmbPriceCondition, txtPriceThreshold, cmbAlertLocation, cmdAddAlert, cmdEditAlert, cmdDeleteAlert, chkAlertEnabled. Layout only. Satisfies: Design Alert System. Output: FormMarket.Designer.cs. Verification: getDiagnostics.
- [~] 10.8 FormMarket — Alerts tab wiring — Wire event handlers for alert CRUD: add/edit/delete alerts via MarketDataService, populate dgvAlerts, enable/disable toggle, display last-triggered info. Satisfies: Design Alert System. Output: FormMarket.cs. Verification: getDiagnostics.

### Phase 8: UI — Service Logic and Remaining

- [~] 10.9 Implement AutoPopulatePricingPlan in MarketDataService — For each resource in the pricing plan, find matching StoredPriceStats, use AvgPrice. Return MarketPricePopulationResult showing which resources updated and which had no data. Satisfies: Req 13 Criterion 2-3. Output: `OE2EmpireTracker.Common/Services/MarketDataService.cs`. Verification: getDiagnostics.
- [~] 10.10 FormMarket — auto-populate pricing plan UI — Wire cmdAutoPopulate button: show confirmation dialog, call MarketDataService.AutoPopulatePricingPlan, display update summary showing updated vs missing resources. Satisfies: Req 13 Criterion 1, 4. Output: FormMarket.cs. Verification: getDiagnostics.
- [~] 10.11 FormStockTargets — Market scope UI controls — Add Market and StationPlusMarket to scope dropdown. Filter Location combo to stations when selected. Location required for StationPlusMarket. Update save/load logic. Satisfies: Req 7 Criterion 5, Req 8 Criterion 3. Output: FormStockTargets.cs, FormStockTargets.Designer.cs. Verification: getDiagnostics.
- [~] 10.12 FormMarket — scope status display — Show which scopes are granted and which are missing per character. Show credential configuration prompt when no credentials configured. Satisfies: Req 11 Criterion 3-4. Output: FormMarket.cs. Verification: getDiagnostics.
- [~] 10.13 FormMarket — stale data indicator — Add visual warning when sync timestamp exceeds configurable threshold. Ensure manual Listings/Transactions/Summary tabs remain fully operational regardless of API state. Satisfies: Req 12 Criterion 2-3. Output: FormMarket.cs. Verification: getDiagnostics.

## Notes

- Requirements 9 (Auth) and 10 (Rate Limiting) are satisfied by the existing `IGameApiTypedClient` infrastructure. Task 6.4 orchestrates their use but does not reimplement them.
- The `MarketDataService` does NOT make API calls directly — it receives typed DTOs from `QueueSyncService` and handles domain mapping, merge logic, and persistence.
- UI tasks (Phase 7) depend on service tasks (Phases 3-5) being complete so data flows are testable end-to-end.
- Property-based tests (Phase 6) can be written in parallel with UI work since they test the service layer independently.
- All model classes use Newtonsoft.Json attributes (`JsonConverter`, `DefaultValue`) consistent with the project's serialization stack.
- FsCheck tests use version 2.16.6 APIs (no Fluent namespace, use `Arb.From<T>(gen)`, LINQ query syntax for generators).
