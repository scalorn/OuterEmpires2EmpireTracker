# Implementation Plan: Empire Systems

## Overview

Empire Systems is implemented in 8 iterations plus a pre-iteration remediation phase. The data model is designed upfront in Iteration 1 to support all iterations without refactoring. Each iteration adds models, services, forms, and tests for its feature area. All new code follows the cross-cutting requirements (logging, PERF timing, threading, reference counting, code quality standards) established in the design.

## Tasks

### Pre-Iteration Remediation

- [x] 1. Blocking Fixes (crash/data integrity risks)
  - [x] 1.1 R4: Fix FormPricingPlan cross-thread bug
  - [x] 1.2 R5: Fix FormColonyActivity event leak
  - [x] 1.3 R6: Create DeliveryRouteReferenceCounter (counts DeliveryPlans, WarehouseOverflowRules, SupplyChainStages)
  - [x] 1.4 Fix FormSurvey resource filter combo — cmbResource selection is not passed to GetFilteredSurveys, only the text filter is used. Surveys should filter by both name substring and selected resource.

- [x] 2. Logging and PERF Remediation
  - [x] 2.1 R1: Add NLog Logger to FormPlayerProfile, FormAutoFill
  - [x] 2.2 R2: Add NLog Logger to high-priority services
  - [x] 2.3 R2: Add NLog Logger to medium-priority services
  - [x] 2.4 R3: Add PERF timing to FormBlueprintV2
  - [x] 2.5 R3: Add PERF timing to FormDeliveryRoute
  - [x] 2.6 R3: Add PERF timing to remaining forms

- [x] 3. Remediation Checkpoint

### Iteration 1: Build Planner + Data Model Foundation

- [x] 4. BaselineData Updates
  - [x] 4.1 Update Hull BlueprintType property list
  - [x] 4.2 Add Constants/SlotTypes.cs
  - [x] 4.3 Copy updated BaselineData.json to test project

- [-] 5. Data Model Foundation
  - [ ] 5.1 Add DestinationType enum (Colony, Station, Asteroid, Ship)
  - [ ] 5.2 Add RouteStopPurpose enum (Cargo, Refuel, CargoAndRefuel)
  - [ ] 5.3 Add BuildPlan model (IsActive flag included)
  - [ ] 5.4 Add BuildItem model (BuildLocationType + BuildLocationUUID instead of ColonyUUID)
  - [ ] 5.5 Add ShipTemplate and ShipComponentSlot (with damage fields)
  - [ ] 5.6 Add Ship model (with hull damage fields)
  - [ ] 5.7 Add Station model (with hull damage fields)
  - [ ] 5.8 Add Asteroid model with AsteroidReserve list
  - [ ] 5.9 Add MarketListing model (with condition fields)
  - [ ] 5.10 Add MarketTransaction model (with CounterpartyFaction + condition fields)
  - [ ] 5.11 Add StockPlan model (IsActive, ReplenishmentBuildPlanUUID)
  - [ ] 5.12 Add StockTarget model
  - [ ] 5.13 Add StockProfile model (IsActive)
  - [ ] 5.14 Add SupplyChain (IsActive) and SupplyChainStage (PickUp/Research stage types)
  - [ ] 5.15 Add WarehouseOverflowRule model (IsActive)
  - [ ] 5.16 Add Faction model (deterministic UUID)
  - [ ] 5.17 Add ExternalCharacter model (deterministic UUID)
  - [ ] 5.18 Add Crate support to Item
  - [ ] 5.19 Add damage fields to Item (CurrentHP, MaxHP, MaxRepairPercent)
  - [ ] 5.20 Add SurveyType enum and AsteroidUUID to Survey
  - [ ] 5.21 Add FactionUUID to PlayerProfile
  - [ ] 5.22 Update PlayerRoot with all 13 new arrays
  - [ ] 5.23 Update RouteStop (DestinationType, Purpose, FuelEstimate)
  - [ ] 5.24 Update DeliveryPlanStop (DestinationType)
  - [ ] 5.25 Add ShipUUID to DeliveryPlan
  - [ ] 5.26 Add ShipStats class
  - [ ] 5.27 Add StationStats class

- [ ] 6. PlayerContext Updates
  - [ ] 6.1 Add 13 new List fields
  - [ ] 6.2 Add 13 Init methods
  - [ ] 6.3 Update WriteContext to serialize all 13 new lists
  - [ ] 6.4 Add snapshot methods for all new lists
  - [ ] 6.5 Add convenience methods (GetCurrentPlayerBuildPlans, etc.)
  - [ ] 6.6 Add new events: BuildPlanDataChanged, MarketDataChanged, StationDataChanged
  - [ ] 6.7 Add CascadeStockTargetsDirty and CascadeResourceCheckDirty runtime flags
  - [ ] 6.8 Update CascadeDeletePlayer for all new entity types
  - [ ] 6.9 Update CleanupOrphanedData for all new entity types

- [ ] 7. In-Memory Indexing
  - [ ] 7.1 Add PlayerContext UUID caches for all new entity types
  - [ ] 7.2 Add EmpireContext _commodityNameCache
  - [ ] 7.3 Add _blueprintTypeCountCache
  - [ ] 7.4 Add cross-entity build item indexes (_blueprintBuildItemIndex, _buildLocationBuildItemIndex)

- [ ] 8. Migration
  - [ ] 8.1 Create Migration: add empty arrays, migrate RouteStop/DeliveryPlanStop ColonyUUID, increment DataVersion

- [ ] 9. Build Planner Services
  - [ ] 9.1 Implement BuildPlanService (ValidatePlanName, ValidateBuildItem, GenerateColonyBuildItems)
  - [ ] 9.2 Implement ResourceCheckService (ComputeShortfalls with BuildLocationType resolution, ComputePlanShortfalls)
  - [ ] 9.3 Implement DeliveryGenerationService (GenerateDeliveryPlan, GenerateConsolidatedDeliveryPlan, GenerateFlatpackDeliveryPlan)
  - [ ] 9.4 Implement QueueCalculator
  - [ ] 9.5 Implement AutoAssignService (ProposeAssignments with BuildLocationType + shipFinder/stationFinder)

- [ ] 10. Build Planner Form
  - [ ] 10.1 Create FormBuildPlanner MDI child: plan list, plan details, build items grid (Location column)
  - [ ] 10.2 Add Item panel with Queue Calc button
  - [ ] 10.3 Structure allocation dialog (Location column, future ship/station note)
  - [ ] 10.4 Resource shortfall display panel
  - [ ] 10.5 Generate Delivery dropdown: Resource (This Plan), Consolidated Resource, Flatpack Delivery
  - [ ] 10.6 Auto-Assign button
  - [ ] 10.7 IsActive checkbox with gray italic styling for inactive plans
  - [ ] 10.8 Wire events with BeginInvoke
  - [ ] 10.9 Build item status display with color coding
  - [ ] 10.10 Implement BuildPlanReferenceCounter (counts StockPlan.ReplenishmentBuildPlanUUID), add Refs column
  - [ ] 10.11 Add "Build Planner" to Manage menu

- [ ] 11. Colony Admin Tab Integration
  - [ ] 11.1 Add "Generate Build Plan" button to colony Administration tab
  - [ ] 11.2 Implement plan picker dialog (new or existing plan)
  - [ ] 11.3 Wire to BuildPlanService.GenerateColonyBuildItems

- [ ] 12. Cascade Processing
  - [ ] 12.1 Extend BackgroundProcessor: CascadeStockTargetsDirty and CascadeResourceCheckDirty
  - [ ] 12.2 Implement cascade status advancement (max ordinal, never decrease)
  - [ ] 12.3 Implement startup cascade
  - [ ] 12.4 Fire BuildPlanDataChanged outside all locks

- [ ] 13. Contacts Form
  - [ ] 13.1 Create FormContacts MDI child: Factions tab + External Characters tab
  - [ ] 13.2 Implement FactionReferenceCounter
  - [ ] 13.3 Add Refs column and delete protection
  - [ ] 13.4 Add "Contacts" to Manage menu

- [ ] 14. Reference Counter Expansions (Iteration 1)
  - [ ] 14.1 Expand BlueprintReferenceCounter: add BuildItem.BlueprintUUID
  - [ ] 14.2 Expand ColonyReferenceCounter: add BuildItem.BuildLocationUUID (when Colony)
  - [ ] 14.3 Implement BuildReferenceMap pattern on all reference counters

- [ ] 15. Iteration 1 Tests
  - [ ] 15.1 Property tests: P1-P5, P10-P12
  - [ ] 15.2 Unit tests: all Iteration 1 services, migration, reference counters

- [ ] 16. Iteration 1 Checkpoint

### Iteration 2: Ships

- [ ] 17. Ship Services
  - [ ] 17.1 Implement ShipBuildService.GenerateShipBuildItems
  - [ ] 17.2 Implement ShipBuildService.ValidateAssemblyLocation
  - [ ] 17.3 Implement ShipBuildService.ComputeStats (all ShipStats fields)
  - [ ] 17.4 Implement ShipBuildService.ComputeStationStats

- [ ] 18. Ship Template Form
  - [ ] 18.1 Create FormShipTemplate MDI child
  - [ ] 18.2 Implement slot grid with component installation
  - [ ] 18.3 Implement full stats panel
  - [ ] 18.4 Order Build button
  - [ ] 18.5 Implement ShipTemplateReferenceCounter, Refs column, delete protection
  - [ ] 18.6 Wire events, NLog, PERF
  - [ ] 18.7 Add "Ship Templates" to Manage menu

- [ ] 19. Ship Instance Form
  - [ ] 19.1 Create FormShipInstance MDI child with Overview + Cargo tabs
  - [ ] 19.2 Overview tab: component grid with editable Condition/MaxRepair columns, hull row first
  - [ ] 19.3 Cargo tab: radio toggle Cargo Hold / Hopper, crate master-detail, purity restriction
  - [ ] 19.4 Create from Template button
  - [ ] 19.5 Implement ShipReferenceCounter (DeliveryPlan.ShipUUID + BuildItem.BuildLocationUUID), Refs column
  - [ ] 19.6 Wire events, NLog, PERF
  - [ ] 19.7 Add "Ships" to Manage menu

- [ ] 20. Reference Counter Expansions (Iteration 2)
  - [ ] 20.1 Expand BlueprintReferenceCounter: ShipTemplate + Ship component BlueprintUUIDs

- [ ] 21. Iteration 2 Tests
  - [ ] 21.1 Property test: P6 (ship class assembly validation)
  - [ ] 21.2 Unit tests: ShipBuildService, ComputeStats, reference counters

- [ ] 22. Iteration 2 Checkpoint

### Iteration 3: Ship-Aware Delivery

- [ ] 23. Ship-Aware Delivery
  - [ ] 23.1 Add ship assignment UI to delivery plan
  - [ ] 23.2 Implement cargo volume computation (crate contents recursive one level)
  - [ ] 23.3 Add volume/mass display on delivery execution form
  - [ ] 23.4 Add volume warning when cargo exceeds capacity
  - [ ] 23.5 Implement trip splitting logic

- [ ] 24. Iteration 3 Checkpoint

### Iteration 4: Stations

- [ ] 25. Station Form
  - [ ] 25.1 Create FormStation MDI child with Hold, Components, Munitions tabs
  - [ ] 25.2 Hold tab: inventory grid with editable Condition/MaxRepair, crate master-detail (CrateInventoryPanel)
  - [ ] 25.3 Components tab: component grid with editable Condition/MaxRepair, hull row first
  - [ ] 25.4 Munitions tab for armed player-owned stations
  - [ ] 25.5 Implement StationReferenceCounter, Refs column, delete protection
  - [ ] 25.6 Wire events, NLog, PERF
  - [ ] 25.7 Add "Stations" to Manage menu

- [ ] 26. Station Integration
  - [ ] 26.1 Update FormDeliveryRoute: Station/Asteroid stop types, Purpose column, FuelEstimate display
  - [ ] 26.2 Update delivery plan form for Station/Asteroid stops
  - [ ] 26.3 Update delivery execution: station hold operations, refuel stop checklist items
  - [ ] 26.4 Update auto-fill to consider station inventory

- [ ] 27. Reference Counter Expansions (Iteration 4)
  - [ ] 27.1 Expand BlueprintReferenceCounter: Station component BlueprintUUIDs

- [ ] 28. Iteration 4 Checkpoint

### Iteration 5: Market

- [ ] 29. Market Services
  - [ ] 29.1 Implement MarketService.RecordSale (decrement listing, create transaction with condition + faction snapshots)
  - [ ] 29.2 Implement MarketService.RecordPurchase (add to station hold)
  - [ ] 29.3 Implement MarketService.ComputeProfitLoss

- [ ] 30. Market Form
  - [ ] 30.1 Create FormMarket MDI child: Listings tab, Transactions tab, Summary tab
  - [ ] 30.2 Listings tab: grid with condition column for damaged components
  - [ ] 30.3 Transactions tab: filters (type, item, counterparty, faction, station, date range), grid with Condition column
  - [ ] 30.4 Summary tab: pricing plan selector, date range, totals, per-item breakdown
  - [ ] 30.5 Record Sale dialog with condition snapshot
  - [ ] 30.6 Implement MarketListingReferenceCounter, Refs column
  - [ ] 30.7 Wire events, NLog, PERF
  - [ ] 30.8 Add "Market" to Manage menu

- [ ] 31. Reference Counter Expansions (Iteration 5)
  - [ ] 31.1 Expand BlueprintReferenceCounter: MarketListing.ItemReferenceID

- [ ] 32. Iteration 5 Tests
  - [ ] 32.1 Property tests: P8 (sale decrements listing), P9 (purchase adds to hold)
  - [ ] 32.2 Unit tests: MarketService, profit/loss, MarketListingReferenceCounter

- [ ] 33. Iteration 5 Checkpoint

### Iteration 6: Full Production Queue + Supply Chain + Asteroids

- [ ] 34. Production Queue Extension
  - [ ] 34.1 Enable Mining, Refining, Research BuildItemTypes in Build Planner
  - [ ] 34.2 Add mining/refining fields to build item UI
  - [ ] 34.3 Implement time-splitting (SequenceInStructure)
  - [ ] 34.4 Implement dependency tracking (DependsOnUUID)
  - [ ] 34.5 Extend ResourceCheckService for mining/refining

- [ ] 35. Asteroid Form
  - [ ] 35.1 Create FormAsteroid MDI child with reserves grid and linked surveys
  - [ ] 35.2 Implement auto-create asteroid on asteroid survey import (Flow 12)
  - [ ] 35.3 Implement AsteroidReferenceCounter, Refs column
  - [ ] 35.4 Wire events, NLog, PERF
  - [ ] 35.5 Add "Asteroids" to Manage menu

- [ ] 36. Supply Chain Form
  - [ ] 36.1 Create FormSupplyChain MDI child with stages grid and flow summary
  - [ ] 36.2 Stage types: Mine, AsteroidMine, PickUp, Refine, Research, Deliver
  - [ ] 36.3 IsActive checkbox with gray italic styling
  - [ ] 36.4 Route selector on threshold stages
  - [ ] 36.5 Wire events, NLog, PERF
  - [ ] 36.6 Add "Supply Chains" to Manage menu

- [ ] 37. Supply Chain Service
  - [ ] 37.1 Implement SupplyChainService.CheckThresholds (filter IsActive, resolve location inventory, return delivery requests)

- [ ] 38. Warehouse Overflow Tab
  - [ ] 38.1 Add Overflow tab to FormColony: rules grid with Active checkbox column
  - [ ] 38.2 Extend BackgroundProcessor for overflow threshold checks (filter IsActive rules)

- [ ] 39. Supply Chain Background Processing
  - [ ] 39.1 Extend BackgroundProcessor: call SupplyChainService.CheckThresholds, generate deliveries

- [ ] 40. Asteroid Survey Integration
  - [ ] 40.1 Extend SurveyParser for asteroid context
  - [ ] 40.2 Add SurveyType, Purity, and min Amount filters to FormSurvey (Type: Planet/Asteroid/All, Purity dropdown, min Amount numeric field)

- [ ] 41. Reference Counter Expansions (Iteration 6)
  - [ ] 41.1 Expand ColonyReferenceCounter: SupplyChainStage, WarehouseOverflowRule
  - [ ] 41.2 Expand SurveyReferenceCounter: BuildItem.MiningSurveyUUID

- [ ] 42. Iteration 6 Checkpoint

### Iteration 7: Fill-Level Automation (Stock Targets)

- [ ] 43. Stock Target Services
  - [ ] 43.1 Implement StockTargetService.CheckTargets (OR-pool within plan, AND across plans, expand templates)
  - [ ] 43.2 Implement StockTargetService.GenerateReplenishmentItems
  - [ ] 43.3 Integrate stock target cascade in BackgroundProcessor

- [ ] 44. Stock Targets Form
  - [ ] 44.1 Create FormStockTargets MDI child: Targets and Plans tab + Profiles tab
  - [ ] 44.2 Plans: IsActive checkbox, replenishment plan selector, targets grid, Quick Add
  - [ ] 44.3 Check and Generate Orders button
  - [ ] 44.4 Profiles tab: IsActive checkbox, entries grid, logic summary
  - [ ] 44.5 Implement StockPlanReferenceCounter, Refs column
  - [ ] 44.6 Wire events, NLog, PERF
  - [ ] 44.7 Add "Stock Targets" to Manage menu

- [ ] 45. Reference Counter Expansions (Iteration 7)
  - [ ] 45.1 Expand BlueprintReferenceCounter: StockPlan targets
  - [ ] 45.2 Expand ColonyReferenceCounter: StockPlan target LocationUUID
  - [ ] 45.3 Expand ShipTemplateReferenceCounter: StockPlan targets

- [ ] 46. Iteration 7 Tests
  - [ ] 46.1 Property test: P7 (stock target shortfall with OR/AND)
  - [ ] 46.2 Unit tests: StockTargetService, StockPlanReferenceCounter

- [ ] 47. Iteration 7 Checkpoint

### Iteration 8: Delivery Auto-Fill Time Horizon

- [ ] 48. Time Horizon Filter
  - [ ] 48.1 Add time horizon parameter to flatpack auto-fill
  - [ ] 48.2 Add time horizon input to FormAutoFill dialog
  - [ ] 48.3 Update AutoFillFlatpacks to filter by build completion time
  - [ ] 48.4 Persist time horizon as a preference

- [ ] 49. Iteration 8 Checkpoint

### Final

- [ ] 50. Final Integration
  - [ ] 50.1 Full test suite passes
  - [ ] 50.2 Verify all reference counters wired with Refs columns and delete protection
  - [ ] 50.3 Verify all forms have NLog, PERF, IProgrammaticUpdateSource, events, BeginInvoke
  - [ ] 50.4 Verify all IsActive toggles work with gray italic styling
  - [ ] 50.5 Verify all inventory grids have editable Condition/MaxRepair columns

## Notes

- Data model built entirely in Iteration 1 to avoid refactoring. All models include fields for later iterations with empty defaults.
- BuildItem uses BuildLocationType + BuildLocationUUID (not ColonyUUID) to support future factory ships.
- DestinationType includes Ship for future factory ship manufacturing/refining/research.
- IsActive flag on BuildPlan, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule for pause/resume.
- Damage tracking (CurrentHP/MaxHP/MaxRepairPercent) on ShipComponentSlot, Ship hull, Station hull, Item, MarketListing, MarketTransaction.
- MarketTransaction snapshots CounterpartyFaction and condition at time of recording.
- RouteStop has Purpose (Cargo/Refuel/CargoAndRefuel) and FuelEstimate for future fuel modeling.
- SupplyChainStageType uses PickUp (not Collect) for consistency with delivery terminology.
- Consolidated delivery generation (Flow 17) supports multi-plan resource and flatpack deliveries.
- Colony admin tab "Generate Build Plan" button creates build items from unstaged structures (Flow 16).
- Persistence Evolution: Func delegates on services, UUID references, separable historical data.
- Shared Faction Database readiness: no singleton access in services, abstractable events, deterministic UUIDs.
- Code Quality Standards: XML docs, null safety, 80-line method limit, naming conventions, defensive coding, test coverage.
- All new services follow the Service Implementation Checklist (9 points).
- All new forms follow the Form Implementation Checklist (10 points).
