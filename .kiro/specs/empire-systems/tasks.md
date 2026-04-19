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

- [x] 5. Data Model Foundation
  - [x] 5.1 Add DestinationType enum (Colony, Station, Asteroid, Ship)
  - [x] 5.2 Add RouteStopPurpose enum (Cargo, Refuel, CargoAndRefuel)
  - [x] 5.3 Add BuildPlan model (IsActive flag included)
  - [x] 5.4 Add BuildItem model (BuildLocationType + BuildLocationUUID instead of ColonyUUID)
  - [x] 5.5 Add ShipTemplate and ShipComponentSlot (with damage fields)
  - [x] 5.6 Add Ship model (with hull damage fields)
  - [x] 5.7 Add Station model (with hull damage fields)
  - [x] 5.8 Add Asteroid model with AsteroidReserve list
  - [x] 5.9 Add MarketListing model (with condition fields)
  - [x] 5.10 Add MarketTransaction model (with CounterpartyFaction + condition fields)
  - [x] 5.11 Add StockPlan model (IsActive, ReplenishmentBuildPlanUUID)
  - [x] 5.12 Add StockTarget model
  - [x] 5.13 Add StockProfile model (IsActive)
  - [x] 5.14 Add SupplyChain (IsActive) and SupplyChainStage (PickUp/Research stage types)
  - [x] 5.15 Add WarehouseOverflowRule model (IsActive)
  - [x] 5.16 Add Faction model (deterministic UUID)
  - [x] 5.17 Add ExternalCharacter model (deterministic UUID)
  - [x] 5.18 Add Crate support to Item
  - [x] 5.19 Add damage fields to Item (CurrentHP, MaxHP, MaxRepairPercent)
  - [x] 5.20 Add SurveyType enum and AsteroidUUID to Survey
  - [x] 5.21 Add FactionUUID to PlayerProfile
  - [x] 5.22 Update PlayerRoot with all 13 new arrays
  - [x] 5.23 Update RouteStop (DestinationType, Purpose, FuelEstimate)
  - [x] 5.24 Update DeliveryPlanStop (DestinationType)
  - [x] 5.25 Add ShipUUID to DeliveryPlan
  - [x] 5.26 Add ShipStats class
  - [x] 5.27 Add StationStats class

- [x] 6. PlayerContext Updates
  - [x] 6.1 Add 13 new List fields
  - [x] 6.2 Add 13 Init methods
  - [x] 6.3 Update WriteContext to serialize all 13 new lists
  - [x] 6.4 Add snapshot methods for all new lists
  - [x] 6.5 Add convenience methods (GetCurrentPlayerBuildPlans, etc.)
  - [x] 6.6 Add new events: BuildPlanDataChanged, MarketDataChanged, StationDataChanged
  - [x] 6.7 Add CascadeStockTargetsDirty and CascadeResourceCheckDirty runtime flags
  - [x] 6.8 Update CascadeDeletePlayer for all new entity types
  - [x] 6.9 Update CleanupOrphanedData for all new entity types

- [x] 7. In-Memory Indexing
  - [x] 7.1 Add PlayerContext UUID caches for all new entity types
  - [x] 7.2 Add EmpireContext _commodityNameCache
  - [x] 7.3 Add _blueprintTypeCountCache
  - [x] 7.4 Add cross-entity build item indexes (_blueprintBuildItemIndex, _buildLocationBuildItemIndex)

- [x] 8. Migration
  - [x] 8.1 Create Migration: add empty arrays, migrate RouteStop/DeliveryPlanStop ColonyUUID, increment DataVersion

- [x] 9. Build Planner Services
  - [x] 9.1 Implement BuildPlanService (ValidatePlanName, ValidateBuildItem, GenerateColonyBuildItems)
  - [x] 9.2 Implement ResourceCheckService (ComputeShortfalls with BuildLocationType resolution, ComputePlanShortfalls)
  - [x] 9.3 Implement DeliveryGenerationService (GenerateDeliveryPlan, GenerateConsolidatedDeliveryPlan, GenerateFlatpackDeliveryPlan)
  - [x] 9.4 Implement QueueCalculator
  - [x] 9.5 Implement AutoAssignService (ProposeAssignments with BuildLocationType + shipFinder/stationFinder)

- [x] 10. Build Planner Form
  - [x] 10.1 Create FormBuildPlanner MDI child: plan list, plan details, build items grid (Location column)
  - [x] 10.2 Add Item panel with Queue Calc button
  - [x] 10.3 Structure allocation dialog (Location column, future ship/station note)
  - [x] 10.4 Resource shortfall display panel
  - [x] 10.5 Generate Delivery dropdown: Resource (This Plan), Consolidated Resource, Flatpack Delivery
  - [x] 10.6 Auto-Assign button
  - [x] 10.7 IsActive checkbox with gray italic styling for inactive plans
  - [x] 10.8 Wire events with BeginInvoke
  - [x] 10.9 Build item status display with color coding
  - [x] 10.10 Implement BuildPlanReferenceCounter (counts StockPlan.ReplenishmentBuildPlanUUID), add Refs column
  - [x] 10.11 Add "Build Planner" to Manage menu

- [x] 11. Colony Admin Tab Integration
  - [x] 11.1 Add "Generate Build Plan" button to colony Administration tab
  - [x] 11.2 Implement plan picker dialog (new or existing plan)
  - [x] 11.3 Wire to BuildPlanService.GenerateColonyBuildItems

- [x] 12. Cascade Processing
  - [x] 12.1 Extend BackgroundProcessor: CascadeStockTargetsDirty and CascadeResourceCheckDirty
  - [x] 12.2 Implement cascade status advancement (max ordinal, never decrease)
  - [x] 12.3 Implement startup cascade
  - [x] 12.4 Fire BuildPlanDataChanged outside all locks

- [x] 13. Contacts Form
  - [x] 13.1 Create FormContacts MDI child: Factions tab + External Characters tab
  - [x] 13.2 Implement FactionReferenceCounter
  - [x] 13.3 Add Refs column and delete protection
  - [x] 13.4 Add "Contacts" to Manage menu

- [x] 14. Reference Counter Expansions (Iteration 1)
  - [x] 14.1 Expand BlueprintReferenceCounter: add BuildItem.BlueprintUUID
  - [x] 14.2 Expand ColonyReferenceCounter: add BuildItem.BuildLocationUUID (when Colony)
  - [x] 14.3 Implement BuildReferenceMap pattern on all reference counters

- [x] 15. Iteration 1 Tests
  - [x] 15.1 Property tests: P1-P5, P10-P12
  - [x] 15.2 Unit tests: all Iteration 1 services, migration, reference counters

- [x] 16. Iteration 1 Checkpoint

### Iteration 2: Ships

- [x] 17. Ship Services
  - [x] 17.1 Implement ShipBuildService.GenerateShipBuildItems
  - [x] 17.2 Implement ShipBuildService.ValidateAssemblyLocation
  - [x] 17.3 Implement ShipBuildService.ComputeStats (all ShipStats fields)
  - [x] 17.4 Implement ShipBuildService.ComputeStationStats

- [x] 18. Ship Template Form
  - [x] 18.1 Create FormShipTemplate MDI child
  - [x] 18.2 Implement slot grid with component installation
  - [x] 18.3 Implement full stats panel
  - [x] 18.4 Order Build button
  - [x] 18.5 Implement ShipTemplateReferenceCounter, Refs column, delete protection
  - [x] 18.6 Wire events, NLog, PERF
  - [x] 18.7 Add "Ship Templates" to Manage menu

- [x] 19. Ship Instance Form
  - [x] 19.1 Create FormShipInstance MDI child with Overview + Cargo tabs
  - [x] 19.2 Overview tab: component grid with editable Condition/MaxRepair columns, hull row first
  - [x] 19.3 Cargo tab: radio toggle Cargo Hold / Hopper, crate master-detail, purity restriction
  - [x] 19.4 Create from Template button
  - [x] 19.5 Implement ShipReferenceCounter (DeliveryPlan.ShipUUID + BuildItem.BuildLocationUUID), Refs column
  - [x] 19.6 Wire events, NLog, PERF
  - [x] 19.7 Add "Ships" to Manage menu

- [x] 20. Reference Counter Expansions (Iteration 2)
  - [x] 20.1 Expand BlueprintReferenceCounter: ShipTemplate + Ship component BlueprintUUIDs

- [x] 21. Iteration 2 Tests
  - [x] 21.1 Property test: P6 (ship class assembly validation)
  - [x] 21.2 Unit tests: ShipBuildService, ComputeStats, reference counters

- [x] 22. Iteration 2 Checkpoint

### Iteration 3: Ship-Aware Delivery

- [x] 23. Ship-Aware Delivery
  - [x] 23.1 Add ship assignment UI to delivery plan
  - [x] 23.2 Implement cargo volume computation (crate contents recursive one level)
  - [x] 23.3 Add volume/mass display on delivery execution form
  - [x] 23.4 Add volume warning when cargo exceeds capacity
  - [x] 23.5 Implement trip splitting logic

- [x] 24. Iteration 3 Checkpoint

### Iteration 4: Stations

- [x] 25. Station Form
  - [x] 25.1 Create FormStation MDI child with Hold, Components, Munitions tabs
  - [x] 25.2 Hold tab: inventory grid with editable Condition/MaxRepair, crate master-detail (CrateInventoryPanel)
  - [x] 25.3 Components tab: component grid with editable Condition/MaxRepair, hull row first
  - [x] 25.4 Munitions tab for armed player-owned stations
  - [x] 25.5 Implement StationReferenceCounter, Refs column, delete protection
  - [x] 25.6 Wire events, NLog, PERF
  - [x] 25.7 Add "Stations" to Manage menu

- [x] 26. Station Integration
  - [x] 26.1 Update FormDeliveryRoute: Station/Asteroid stop types, Purpose column, FuelEstimate display
  - [x] 26.2 Update delivery plan form for Station/Asteroid stops
  - [x] 26.3 Update delivery execution: station hold operations, refuel stop checklist items
  - [x] 26.4 Update auto-fill to consider station inventory

- [x] 27. Reference Counter Expansions (Iteration 4)
  - [x] 27.1 Expand BlueprintReferenceCounter: Station component BlueprintUUIDs

- [x] 28. Iteration 4 Checkpoint

### Iteration 5: Market

- [x] 29. Market Services
  - [x] 29.1 Implement MarketService.RecordSale (decrement listing, create transaction with condition + faction snapshots)
  - [x] 29.2 Implement MarketService.RecordPurchase (add to station hold)
  - [x] 29.3 Implement MarketService.ComputeProfitLoss

- [x] 30. Market Form
  - [x] 30.1 Create FormMarket MDI child: Listings tab, Transactions tab, Summary tab
  - [x] 30.2 Listings tab: grid with condition column for damaged components
  - [x] 30.3 Transactions tab: filters (type, item, counterparty, faction, station, date range), grid with Condition column
  - [x] 30.4 Summary tab: pricing plan selector, date range, totals, per-item breakdown
  - [x] 30.5 Record Sale dialog with condition snapshot
  - [x] 30.6 Implement MarketListingReferenceCounter, Refs column
  - [x] 30.7 Wire events, NLog, PERF
  - [x] 30.8 Add "Market" to Manage menu

- [x] 31. Reference Counter Expansions (Iteration 5)
  - [x] 31.1 Expand BlueprintReferenceCounter: MarketListing.ItemReferenceID

- [x] 32. Iteration 5 Tests
  - [x] 32.1 Property tests: P8 (sale decrements listing), P9 (purchase adds to hold)
  - [x] 32.2 Unit tests: MarketService, profit/loss, MarketListingReferenceCounter

- [x] 33. Iteration 5 Checkpoint

### Iteration 6: Full Production Queue + Supply Chain + Asteroids

- [x] 34. Production Queue Extension
  - [x] 34.1 Enable Mining, Refining, Research BuildItemTypes in Build Planner
  - [x] 34.2 Add mining/refining fields to build item UI
  - [x] 34.3 Implement time-splitting (SequenceInStructure)
  - [x] 34.4 Implement dependency tracking (DependsOnUUID)
  - [x] 34.5 Extend ResourceCheckService for mining/refining

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
