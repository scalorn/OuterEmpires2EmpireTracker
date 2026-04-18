# Implementation Plan: Empire Systems

## Overview

Empire Systems is implemented in 8 iterations plus a pre-iteration remediation phase. The data model is designed upfront in Iteration 1 to support all iterations without refactoring. Each iteration adds models, services, forms, and tests for its feature area. All new code follows the cross-cutting requirements (logging, PERF timing, threading, reference counting) established in the design.

## Tasks

### Pre-Iteration Remediation

- [ ] 1. Blocking Fixes (crash/data integrity risks)
  - [ ] 1.1 R4: Fix FormPricingPlan cross-thread bug — add InvokeRequired/BeginInvoke check to OnCurrentPlayerChanged handler
  - [ ] 1.2 R5: Fix FormColonyActivity event leak — add `playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged` to OnFormClosed
  - [ ] 1.3 R6: Create DeliveryRouteReferenceCounter service (counts DeliveryPlans referencing a route), add Refs column to FormDeliveryRoute ListView, disable Delete when in use

- [ ] 2. Logging & PERF Remediation
  - [ ] 2.1 R1: Add NLog Logger to FormPlayerProfile, FormAutoFill
  - [ ] 2.2 R2: Add NLog Logger to high-priority services: ColonyAdminReportBuilder, ColonyActivityCollector, ColonyInactivityCollector, ColonyBuildEligibility, ColonyImportHelper, SurveyImportHelper
  - [ ] 2.3 R2: Add NLog Logger to medium-priority services: BlueprintReferenceCounter, ColonyReferenceCounter, SurveyReferenceCounter, BuildTimeCalculator, EvolutionChainService
  - [ ] 2.4 R3: Add PERF Stopwatch timing to FormBlueprintV2 (PopulateListView, PopulateForm, PopulateGrid)
  - [ ] 2.5 R3: Add PERF Stopwatch timing to FormDeliveryRoute (PopulateRouteList, PopulateStops, PopulatePlanStops)
  - [ ] 2.6 R3: Add PERF Stopwatch timing to FormSurvey, FormPlayerProfile, FormPricingPlan, FormDeliveryExecution, FormColonyActivity, FormColonyDailyBuild

- [ ] 3. Remediation Checkpoint — build passes, all existing tests pass

### Iteration 1: Build Planner + Data Model Foundation

- [ ] 4. BaselineData Updates
  - [ ] 4.1 Update Hull BlueprintType property list: add Max Ore Hoppers, Raw Material Capacity, Eng Capacity Available
  - [ ] 4.2 Add SlotType mapping constants class (hull property → SlotType string → BlueprintType IDs, 20 slot types per OQ-31/OQ-32)
  - [ ] 4.3 Copy updated BaselineData.json to test project TestData

- [ ] 5. Data Model Foundation
  - [ ] 5.1 Add DestinationType enum (Colony, Station, Asteroid)
  - [ ] 5.2 Add BuildPlan model (UUID, Name, OwnerUUID, Description, DeliveryPlanUUID, nested List of BuildItem)
  - [ ] 5.3 Add BuildItem model with enums (BuildItemType: Manufactory/Commodity/ShipTemplate/Mining/Refining/Research; BuildItemStatus: Staged/Delivering/Ready/InProgress/Completed) and all fields including Iteration 6 placeholders
  - [ ] 5.4 Add ShipTemplate and ShipComponentSlot models (SlotType as string, SlotIndex, BlueprintUUID)
  - [ ] 5.5 Add Ship model with Cargo ItemBag and Hopper ItemBag, LocationType/LocationUUID
  - [ ] 5.6 Add Station model with StationType, StationOwnership, per-player Holds dictionary, Components list, StationBlueprintUUID, MunitionsHold
  - [ ] 5.7 Add Asteroid model with AsteroidReserve list (ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp)
  - [ ] 5.8 Add MarketListing model (StationUUID, ItemType, ItemReferenceID, Quantity, PricePerUnit)
  - [ ] 5.9 Add MarketTransaction model (TransactionType, ListingUUID, Counterparty, Timestamp, StationUUID)
  - [ ] 5.10 Add StockPlan model (Name, OwnerUUID, ReplenishmentBuildPlanUUID, nested List of StockTarget)
  - [ ] 5.11 Add StockTarget model (ItemType, ItemReferenceID, ShipTemplateUUID, TargetQuantity, CriticalThreshold, Scope, LocationUUID — no OwnerUUID or StockPlanUUID per OQ-30)
  - [ ] 5.12 Add StockProfile model with StockProfileEntry (GroupID, StockPlanUUID — no StockTargetUUID per OQ-30)
  - [ ] 5.13 Add SupplyChain and SupplyChainStage models (StageType enum, LocationType/UUID, ResourceName/Purity, AccumulationThreshold, ProductionRatePerHour, DeliveryRouteUUID per OQ-39)
  - [ ] 5.14 Add WarehouseOverflowRule model (ColonyUUID, ResourceName, ResourcePurity, TriggerThreshold, DestinationType/UUID, DeliveryRouteUUID per OQ-33)
  - [ ] 5.15 Add Faction model (Name, Description — no OwnerUUID, deterministic UUID from name)
  - [ ] 5.16 Add ExternalCharacter model (Name, FactionUUID — no OwnerUUID, deterministic UUID from name)
  - [ ] 5.17 Add Crate support: ItemType.Crate enum value + Contents ItemBag on Item (null for non-crates, NullValueHandling.Ignore)
  - [ ] 5.18 Add SurveyType enum (Planet, Asteroid) and AsteroidUUID to Survey model (DefaultValue Planet)
  - [ ] 5.19 Add FactionUUID to PlayerProfile
  - [ ] 5.20 Update PlayerRoot with all 13 new arrays
  - [ ] 5.21 Update RouteStop: add DestinationType + DestinationUUID, keep ColonyUUID for backward compat
  - [ ] 5.22 Update DeliveryPlanStop: add DestinationType + DestinationUUID, keep ColonyUUID for backward compat
  - [ ] 5.23 Add ShipUUID to DeliveryPlan
  - [ ] 5.24 Add ShipStats class (all 30+ fields: core, capacity, defence, propulsion, mining, scanning, weapons, license)
  - [ ] 5.25 Add StationStats class (subset: core, defence, weapons)

- [ ] 6. PlayerContext Updates
  - [ ] 6.1 Add 13 new List<T> fields (BuildPlanList through AsteroidList)
  - [ ] 6.2 Add 13 Init methods (InitBuildPlans through InitAsteroids)
  - [ ] 6.3 Update WriteContext to serialize all 13 new lists
  - [ ] 6.4 Add snapshot methods (SnapshotBuildPlanList, SnapshotStationList, etc.)
  - [ ] 6.5 Add 10 convenience methods (GetCurrentPlayerBuildPlans through GetCurrentPlayerOverflowRules)
  - [ ] 6.6 Add new events: BuildPlanDataChanged, MarketDataChanged, StationDataChanged
  - [ ] 6.7 Add CascadeStockTargetsDirty and CascadeResourceCheckDirty runtime flags
  - [ ] 6.8 Update CascadeDeletePlayer for all new entity types with OwnerUUID
  - [ ] 6.9 Update CleanupOrphanedData for all new entity types

- [ ] 7. In-Memory Indexing
  - [ ] 7.1 Add PlayerContext UUID caches: _stationCache, _shipTemplateCache, _shipCache, _buildPlanCache, _asteroidCache, _factionCache, _marketListingCache (with Find/Invalidate methods)
  - [ ] 7.2 Add EmpireContext _commodityNameCache (FindCommodity by name)
  - [ ] 7.3 Add _blueprintTypeCountCache (CountBlueprintsByType for auto-assign copy constraint)
  - [ ] 7.4 Add cross-entity build item indexes: _blueprintBuildItemIndex, _colonyBuildItemIndex (with InvalidateBuildItemIndexes)

- [ ] 8. Migration
  - [ ] 8.1 Create Migration008_EmpireSystems: add empty arrays for all new entity types, migrate RouteStop.ColonyUUID → DestinationUUID + DestinationType.Colony, migrate DeliveryPlanStop similarly, increment DataVersion

- [ ] 9. Build Planner Services
  - [ ] 9.1 Implement BuildPlanService (ValidatePlanName, ValidateBuildItem) with NLog + PERF
  - [ ] 9.2 Implement ResourceCheckService (ComputeShortfalls, ComputePlanShortfalls) — checks colony warehouse + current player's station holds on route per OQ-40
  - [ ] 9.3 Implement DeliveryGenerationService (GenerateDeliveryPlan) with NLog + PERF
  - [ ] 9.4 Implement QueueCalculator (ComputeManufactoryRuns, ComputeCommodityRuns, ManufactoryRunsToItems, CommodityRunsToItems)
  - [ ] 9.5 Implement AutoAssignService (ProposeAssignments) — uses _blueprintTypeCountCache for copy constraint

- [ ] 10. Build Planner Form
  - [ ] 10.1 Create FormBuildPlanner MDI child: plan list (left), plan details + build items grid (right)
  - [ ] 10.2 Add Item panel: type combo, item combo with filter, quantity, target duration, recipient, Queue Calc button
  - [ ] 10.3 Structure allocation modal dialog (colony/structure picker, filter, idle-only toggle, busy indicators)
  - [ ] 10.4 Resource shortfall display panel (per-item shortfalls grid, visible on selection)
  - [ ] 10.5 Generate Delivery button (route picker combo, delivery plan creation/update)
  - [ ] 10.6 Auto-Assign button (calls AutoAssignService, shows proposal for review)
  - [ ] 10.7 Wire events: CurrentPlayerChanged, ColonyDataChanged, BuildPlanDataChanged with BeginInvoke
  - [ ] 10.8 Implement IProgrammaticUpdateSource, NLog, PERF timing on PopulateForm/PopulateList
  - [ ] 10.9 Build item status display with color coding, manual status transitions per OQ-34 (forward only for cascade, any direction for user)
  - [ ] 10.10 Add "Build Planner" to Manage menu in MainWindow

- [ ] 11. Cascade Processing
  - [ ] 11.1 Extend BackgroundProcessor tick: check CascadeStockTargetsDirty → run StockTargetService, check CascadeResourceCheckDirty → run ResourceCheckService on all active plans
  - [ ] 11.2 Implement cascade status advancement: max(currentStatus, computedStatus) per OQ-34 — never decrease ordinal
  - [ ] 11.3 Implement startup cascade (unconditional full evaluation on app start)
  - [ ] 11.4 Fire BuildPlanDataChanged outside all locks after cascade completes

- [ ] 12. Contacts Form
  - [ ] 12.1 Create FormContacts MDI child: Factions tab (CRUD, members grid) + External Characters tab (CRUD, faction combo)
  - [ ] 12.2 Implement FactionReferenceCounter (counts PlayerProfile.FactionUUID + ExternalCharacter.FactionUUID references)
  - [ ] 12.3 Add Refs column and delete protection to Factions tab
  - [ ] 12.4 Add "Contacts" to Manage menu

- [ ] 13. Reference Counter Expansions (Iteration 1 entities)
  - [ ] 13.1 Expand BlueprintReferenceCounter: add BuildItem.BlueprintUUID count
  - [ ] 13.2 Expand ColonyReferenceCounter: add BuildItem.ColonyUUID count
  - [ ] 13.3 Implement BuildReferenceMap() pattern on all reference counters (pre-compute UUID → count map in one pass)

- [ ] 14. Iteration 1 Tests
  - [ ] 14.1 Property tests: build item quantity validation (P1), queue calculator manufactory (P2), queue calculator commodity (P3), resource shortfall computation with station holds (P4), delivery plan covers shortfalls (P5), serialization round-trip for all new types (P10), migration preserves destinations (P11), cascade status monotonicity (P12)
  - [ ] 14.2 Unit tests: BuildPlanService validation, ResourceCheckService with known data, QueueCalculator edge cases, DeliveryGenerationService, AutoAssignService, Migration008, FactionReferenceCounter

- [ ] 15. Iteration 1 Checkpoint — build passes, all tests pass

### Iteration 2: Ships

- [ ] 16. Ship Services
  - [ ] 16.1 Implement ShipBuildService.GenerateShipBuildItems (template expansion, stock check at assembly location)
  - [ ] 16.2 Implement ShipBuildService.ValidateAssemblyLocation (class 2-5 any, 6 Station+Starbase, 7-8 Starbase only)
  - [ ] 16.3 Implement ShipBuildService.ComputeStats (all ShipStats fields from hull + components using SlotType mapping)
  - [ ] 16.4 Implement ShipBuildService.ComputeStationStats (StationStats subset — deferred display per OQ-37 but service ready)

- [ ] 17. Ship Template Form
  - [ ] 17.1 Create FormShipTemplate MDI child: template list (left), hull selector, component slot grid, install panel
  - [ ] 17.2 Implement slot grid: one row per slot from hull properties, show installed blueprint or (empty)
  - [ ] 17.3 Implement component installation with SlotType validation (BlueprintType → SlotType mapping)
  - [ ] 17.4 Implement full stats panel (core, capacity, defence, propulsion, weapons, license — mining/scanning conditional)
  - [ ] 17.5 "Order Build" button: select/create build plan, specify assembly location, generate build items
  - [ ] 17.6 Implement ShipTemplateReferenceCounter, add Refs column and delete protection
  - [ ] 17.7 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 17.8 Add "Ship Templates" to Manage menu

- [ ] 18. Ship Instance Form
  - [ ] 18.1 Create FormShipInstance MDI child: ship list (left), tabbed detail (Overview + Cargo)
  - [ ] 18.2 Overview tab: component grid (read-only), swap component button, full stats panel (same as template)
  - [ ] 18.3 Cargo tab: radio toggle Cargo Hold / Hopper, cargo hold with crate master-detail, hopper with purity restriction (High/Medium/Low only)
  - [ ] 18.4 "Create from Template" button: copy hull + components, assign name/location
  - [ ] 18.5 Implement ShipReferenceCounter (counts DeliveryPlan.ShipUUID), add Refs column and delete protection
  - [ ] 18.6 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 18.7 Add "Ships" to Manage menu

- [ ] 19. Reference Counter Expansions (Iteration 2 entities)
  - [ ] 19.1 Expand BlueprintReferenceCounter: add ShipTemplate.HullBlueprintUUID, ShipTemplate.Components[].BlueprintUUID, Ship.HullBlueprintUUID, Ship.Components[].BlueprintUUID

- [ ] 20. Iteration 2 Tests
  - [ ] 20.1 Property test: ship class assembly validation (P6)
  - [ ] 20.2 Unit tests: ShipBuildService with known templates, ComputeStats, ValidateAssemblyLocation matrix, ShipTemplateReferenceCounter, ShipReferenceCounter

- [ ] 21. Iteration 2 Checkpoint — build passes, all tests pass

### Iteration 3: Ship-Aware Delivery

- [ ] 22. Ship-Aware Delivery
  - [ ] 22.1 Add ship assignment UI to delivery plan (ShipUUID field, ship selector combo on FormDeliveryRoute plan tab)
  - [ ] 22.2 Implement cargo volume computation: sum item volumes, crate contents recursive one level (crate itself = 0 volume per OQ-36)
  - [ ] 22.3 Add volume/mass display on delivery execution form (used/capacity header)
  - [ ] 22.4 Add volume warning when cargo exceeds ship capacity (advisory, not blocking)
  - [ ] 22.5 Implement trip splitting logic (split plan into multiple trips respecting volume limit, maintain stop order)

- [ ] 23. Iteration 3 Checkpoint — build passes, all tests pass

### Iteration 4: Stations

- [ ] 24. Station Form
  - [ ] 24.1 Create FormStation MDI child: station list (left), station details (name, type, ownership), tabbed detail
  - [ ] 24.2 Hold tab: player selector combo, inventory grid with crate master-detail (reusable CrateInventoryPanel), add-item panel
  - [ ] 24.3 Components tab: deferred per OQ-37 (station blueprint not yet available in game) — show placeholder message
  - [ ] 24.4 Munitions tab: munitions hold grid for armed player-owned stations
  - [ ] 24.5 Implement StationReferenceCounter (routes, plans, ships, listings, transactions, build plans, supply chains, stock plans, overflow rules), add Refs column and delete protection
  - [ ] 24.6 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 24.7 Add "Stations" to Manage menu

- [ ] 25. Station Integration
  - [ ] 25.1 Update FormDeliveryRoute: add Station and Asteroid to stop type selector, update stop grid columns
  - [ ] 25.2 Update delivery plan form to support Station and Asteroid stops
  - [ ] 25.3 Update delivery execution: station hold pickups add to hold, dropoffs remove from hold
  - [ ] 25.4 Update auto-fill to consider station inventory when computing shortfalls

- [ ] 26. Reference Counter Expansions (Iteration 4 entities)
  - [ ] 26.1 Expand BlueprintReferenceCounter: add Station.StationBlueprintUUID, Station.Components[].BlueprintUUID

- [ ] 27. Iteration 4 Checkpoint — build passes, all tests pass

### Iteration 5: Market

- [ ] 28. Market Services
  - [ ] 28.1 Implement MarketService.RecordSale (decrement listing, create transaction, set CascadeStockTargetsDirty)
  - [ ] 28.2 Implement MarketService.RecordPurchase (add to station hold, set CascadeResourceCheckDirty)
  - [ ] 28.3 Implement MarketService.ComputeProfitLoss (transaction price vs pricing plan valuation)

- [ ] 29. Market Form
  - [ ] 29.1 Create FormMarket MDI child: Listings tab (grid, add listing panel, Record Sale/Edit/Delete buttons)
  - [ ] 29.2 Transactions tab: filter row (type, item, counterparty, station, date range), transaction grid, Add/Edit/Delete
  - [ ] 29.3 Summary tab: pricing plan selector, date range, running totals, per-item breakdown grid
  - [ ] 29.4 Record Sale dialog: quantity, counterparty, notes → creates transaction + decrements listing
  - [ ] 29.5 Implement MarketListingReferenceCounter (counts MarketTransaction.ListingUUID), add Refs column and delete protection on Listings tab
  - [ ] 29.6 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 29.7 Add "Market" to Manage menu

- [ ] 30. Reference Counter Expansions (Iteration 5 entities)
  - [ ] 30.1 Expand BlueprintReferenceCounter: add MarketListing.ItemReferenceID (when ItemType=Blueprint)

- [ ] 31. Iteration 5 Tests
  - [ ] 31.1 Property tests: market sale decrements listing (P8), market purchase adds to station hold (P9)
  - [ ] 31.2 Unit tests: MarketService edge cases, profit/loss computation, listing quantity clamped to 0, MarketListingReferenceCounter

- [ ] 32. Iteration 5 Checkpoint — build passes, all tests pass

### Iteration 6: Full Production Queue + Supply Chain + Asteroids

- [ ] 33. Production Queue Extension
  - [ ] 33.1 Enable Mining, Refining, Research BuildItemTypes in Build Planner (structure allocation filters to Mining Rig, Refinery, Research Lab)
  - [ ] 33.2 Add mining/refining fields to build item UI (MiningResource, MiningSurveyUUID, RefiningResource, RefiningPurity)
  - [ ] 33.3 Implement time-splitting: SequenceInStructure > 0, schedule display per structure
  - [ ] 33.4 Implement dependency tracking: DependsOnUUID, flag items with unmet prerequisites in Inactivity
  - [ ] 33.5 Extend ResourceCheckService for mining/refining resource requirements

- [ ] 34. Asteroid Form
  - [ ] 34.1 Create FormAsteroid MDI child: asteroid list (left), name/system fields, reserves grid, add-reserve panel, linked surveys grid
  - [ ] 34.2 Implement AsteroidReferenceCounter (counts Survey.AsteroidUUID, SupplyChainStage.LocationUUID, DeliveryRoute stops), add Refs column and delete protection
  - [ ] 34.3 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 34.4 Add "Asteroids" to Manage menu

- [ ] 35. Supply Chain Form
  - [ ] 35.1 Create FormSupplyChain MDI child: chain list (left), stages grid, add/edit stage panel with move up/down, flow summary label
  - [ ] 35.2 Stage type determines relevant fields: Mine/AsteroidMine (no threshold), Collect/Refine/Deliver (threshold + route)
  - [ ] 35.3 Route selector combo on stages with AccumulationThreshold > 0
  - [ ] 35.4 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 35.5 Add "Supply Chains" to Manage menu

- [ ] 36. Warehouse Overflow Tab
  - [ ] 36.1 Add Overflow tab to FormColony: rules grid (resource, purity, threshold, current with color coding, destination), add-rule panel with route selector
  - [ ] 36.2 Extend BackgroundProcessor: check warehouse levels vs overflow thresholds, generate deliveries on designated route for excess

- [ ] 37. Supply Chain Background Processing
  - [ ] 37.1 Extend BackgroundProcessor: check accumulation at each supply chain stage, generate deliveries on designated route when threshold exceeded

- [ ] 38. Asteroid Survey Integration
  - [ ] 38.1 Extend SurveyParser to detect asteroid context and set SurveyType=Asteroid + AsteroidUUID
  - [ ] 38.2 Add SurveyType filter to FormSurvey (planet vs asteroid)

- [ ] 39. Reference Counter Expansions (Iteration 6 entities)
  - [ ] 39.1 Expand ColonyReferenceCounter: add SupplyChainStage.LocationUUID (Colony), WarehouseOverflowRule.ColonyUUID + DestinationUUID (Colony)
  - [ ] 39.2 Expand SurveyReferenceCounter: add BuildItem.MiningSurveyUUID
  - [ ] 39.3 Expand DeliveryRouteReferenceCounter: add WarehouseOverflowRule.DeliveryRouteUUID, SupplyChainStage.DeliveryRouteUUID

- [ ] 40. Iteration 6 Checkpoint — build passes, all tests pass

### Iteration 7: Fill-Level Automation (Stock Targets)

- [ ] 41. Stock Target Services
  - [ ] 41.1 Implement StockTargetService.CheckTargets (takes IEnumerable of StockPlan + currentPlayerUUID, OR-pool within plan, AND across plans, expand ship templates, check scoped inventory with current player's station holds)
  - [ ] 41.2 Implement StockTargetService.GenerateReplenishmentItems (create build items in designated ReplenishmentBuildPlanUUID, avoid duplicates)
  - [ ] 41.3 Integrate stock target cascade in BackgroundProcessor (CascadeStockTargetsDirty → CheckTargets → generate items → set CascadeResourceCheckDirty)

- [ ] 42. Stock Targets Form
  - [ ] 42.1 Create FormStockTargets MDI child: Targets & Plans tab + Profiles tab
  - [ ] 42.2 Targets & Plans tab: plan list (left) with New Plan/Quick Add/Delete, plan name + replenishment plan selector, targets grid with color-coded shortfall, add-target panel, expanded components panel
  - [ ] 42.3 Quick Add button: creates single-target plan in one step (prompts for item, qty, scope, names plan after item)
  - [ ] 42.4 "Check & Generate Orders" button: runs StockTargetService, displays shortfalls, creates build items in replenishment plan (prompts if no plan designated per OQ-35)
  - [ ] 42.5 Profiles tab: profile list, entries grid (GroupID, Plan name), add-entry panel (GroupID + plan combo), logic summary label
  - [ ] 42.6 Implement StockPlanReferenceCounter (counts StockProfileEntry.StockPlanUUID), add Refs column and delete protection
  - [ ] 42.7 Wire events, IProgrammaticUpdateSource, NLog, PERF timing
  - [ ] 42.8 Add "Stock Targets" to Manage menu

- [ ] 43. Reference Counter Expansions (Iteration 7 entities)
  - [ ] 43.1 Expand BlueprintReferenceCounter: add StockPlan.Targets[].ItemReferenceID (when ItemType=Blueprint)
  - [ ] 43.2 Expand ColonyReferenceCounter: add StockPlan.Targets[].LocationUUID (when Scope=Colony)
  - [ ] 43.3 Expand ShipTemplateReferenceCounter: add StockPlan.Targets[].ShipTemplateUUID

- [ ] 44. Iteration 7 Tests
  - [ ] 44.1 Property test: stock target shortfall computation with OR/AND pooling and per-player station holds (P7)
  - [ ] 44.2 Unit tests: StockTargetService with mixed scopes, ship template expansion, OR-pool within plan, AND across plans, StockPlanReferenceCounter

- [ ] 45. Iteration 7 Checkpoint — build passes, all tests pass

### Iteration 8: Delivery Auto-Fill Time Horizon

- [ ] 46. Time Horizon Filter
  - [ ] 46.1 Add time horizon parameter to flatpack auto-fill (only include structures expected to be built within window)
  - [ ] 46.2 Add time horizon input to FormAutoFill dialog
  - [ ] 46.3 Update AutoFillFlatpacks to filter by build completion time
  - [ ] 46.4 Persist time horizon as a preference (carries across sessions)

- [ ] 47. Iteration 8 Checkpoint — build passes, all tests pass

### Final

- [ ] 48. Final Integration
  - [ ] 48.1 Full test suite passes (all property tests + all unit tests)
  - [ ] 48.2 Verify all reference counters are wired to their forms with Refs columns and delete protection
  - [ ] 48.3 Verify all forms have NLog, PERF timing, IProgrammaticUpdateSource, event subscribe/unsubscribe, BeginInvoke

## Notes

- Data model is built entirely in Iteration 1 to avoid refactoring. All models include fields for later iterations with empty defaults (omitted from JSON via DefaultValueHandling.Ignore).
- Iterations can be reordered based on priorities — the data model supports all from the start.
- Each iteration follows the pattern: models → services → forms → tests → checkpoint.
- New entity lists use List<T> (not BindingList<T>). Thread safety via PlayerContext._listLock.
- Lock ordering: _listLock → ColonyLock → _syncRoot. Events fired outside all locks.
- Deterministic UUIDs for shared game-world entities (Station, Asteroid, Faction, ExternalCharacter). Random UUIDs for player-specific entities.
- All new services must follow the Service Implementation Checklist (9 points) from the design.
- All new forms must follow the Form Implementation Checklist (10 points) from the design.
- Cascade status advancement uses max(currentStatus, computedStatus) — never decreases ordinal (OQ-34).
- Station Components tab deferred until game releases build package properties (OQ-37).
- Resource checks include current player's station holds on the delivery route (OQ-40).
- All stock targets live inside StockPlans — no standalone targets (OQ-30). Simple targets are single-target plans.
