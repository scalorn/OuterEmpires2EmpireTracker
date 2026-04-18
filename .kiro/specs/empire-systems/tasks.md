# Implementation Plan: Empire Systems

## Overview

Empire Systems is implemented in 8 iterations, each building on the shared data model established in Iteration 1. The data model is designed upfront to support all iterations without refactoring. Each iteration adds models, services, forms, and tests for its feature area.

## Tasks

### Iteration 1: Build Planner + Data Model Foundation

- [ ] 1. Data Model Foundation
  - [ ] 1.1 Add DestinationType enum (Colony, Station, Asteroid)
  - [ ] 1.2 Add BuildPlan and BuildItem models with enums (BuildItemType, BuildItemStatus)
  - [ ] 1.3 Add ShipTemplate and ShipComponentSlot models
  - [ ] 1.4 Add Ship model with Cargo and RawMaterialHold ItemBags
  - [ ] 1.5 Add Station model with StationType, StationOwnership, Holds dictionary, Components
  - [ ] 1.6 Add Asteroid model with AsteroidReserve list
  - [ ] 1.7 Add MarketListing and MarketTransaction models
  - [ ] 1.8 Add StockTarget, StockPlan, StockProfile models
  - [ ] 1.9 Add SupplyChain and SupplyChainStage models
  - [ ] 1.10 Add WarehouseOverflowRule model
  - [ ] 1.11 Add Faction and ExternalCharacter models
  - [ ] 1.12 Add Crate support (ItemType.Crate + Contents ItemBag on Item)
  - [ ] 1.13 Add SurveyType enum and AsteroidUUID to Survey model
  - [ ] 1.14 Add FactionUUID to PlayerProfile
  - [ ] 1.15 Update PlayerRoot with all new arrays
  - [ ] 1.16 Update PlayerContext: new List<T> fields, Init methods, WriteContext, snapshot methods, events, convenience methods
  - [ ] 1.17 Update RouteStop and DeliveryPlanStop with DestinationType + DestinationUUID (keep ColonyUUID for compat)
  - [ ] 1.18 Add ShipUUID to DeliveryPlan

- [ ] 2. Migration
  - [ ] 2.1 Create Migration008_EmpireSystems: add empty arrays, migrate RouteStop.ColonyUUID → DestinationUUID, migrate DeliveryPlanStop.ColonyUUID → DestinationUUID, increment DataVersion

- [ ] 3. Build Planner Services
  - [ ] 3.1 Implement BuildPlanService (ValidatePlanName, ValidateBuildItem)
  - [ ] 3.2 Implement ResourceCheckService (ComputeShortfalls, ComputePlanShortfalls)
  - [ ] 3.3 Implement DeliveryGenerationService (GenerateDeliveryPlan)
  - [ ] 3.4 Implement QueueCalculator (ComputeManufactoryRuns, ComputeCommodityRuns, RunsToItems)
  - [ ] 3.5 Implement AutoAssignService (ProposeAssignments)

- [ ] 4. Build Planner Form
  - [ ] 4.1 Create FormBuildPlanner MDI child: plan list, plan details, build items grid
  - [ ] 4.2 Add Item panel: type combo, item combo with filter, quantity, target duration, recipient
  - [ ] 4.3 Structure allocation modal dialog (colony/structure picker, idle-only toggle)
  - [ ] 4.4 Resource shortfall display panel
  - [ ] 4.5 Generate Delivery button (route picker, delivery plan creation)
  - [ ] 4.6 Wire data change events (CurrentPlayerChanged, ColonyDataChanged, BuildPlanDataChanged)
  - [ ] 4.7 Add "Build Planner" to Manage menu

- [ ] 5. Cascade Processing
  - [ ] 5.1 Add CascadeStockTargetsDirty and CascadeResourceCheckDirty flags to PlayerContext
  - [ ] 5.2 Extend BackgroundProcessor tick to check cascade flags and run evaluations
  - [ ] 5.3 Implement startup cascade (unconditional full evaluation on app start)

- [ ] 6. Iteration 1 Tests
  - [ ] 6.1 Property tests: build item quantity validation, queue calculator (manufactory + commodity), resource shortfall computation, delivery plan covers shortfalls, serialization round-trip
  - [ ] 6.2 Unit tests: BuildPlanService validation, ResourceCheckService with known data, QueueCalculator edge cases, DeliveryGenerationService, Migration008

- [ ] 7. Checkpoint — Iteration 1 compiles and all tests pass

### Iteration 2: Ships

- [ ] 8. Ship Services
  - [ ] 8.1 Implement ShipBuildService (GenerateShipBuildItems, ValidateAssemblyLocation, ComputeStats)

- [ ] 9. Ship Forms
  - [ ] 9.1 Create FormShipTemplate MDI child: template list, hull selector, slot grid, computed stats, "Order Build" button
  - [ ] 9.2 Create FormShipInstance MDI child: ship list, component display, cargo hold, "Create from Template"
  - [ ] 9.3 Add "Ship Templates" and "Ships" to Manage menu

- [ ] 10. Iteration 2 Tests
  - [ ] 10.1 Property test: ship class assembly validation
  - [ ] 10.2 Unit tests: ShipBuildService with known templates, stat computation

- [ ] 11. Checkpoint — Iteration 2 compiles and all tests pass

### Iteration 3: Ship-Aware Delivery

- [ ] 12. Ship-Aware Delivery
  - [ ] 12.1 Add ship assignment to delivery plan (ShipUUID field, ship selector on form)
  - [ ] 12.2 Implement cargo volume computation (sum item volumes, crate recursion)
  - [ ] 12.3 Add volume warning display on delivery execution form
  - [ ] 12.4 Implement trip splitting logic (split plan into multiple trips when over capacity)

- [ ] 13. Checkpoint — Iteration 3 compiles and all tests pass

### Iteration 4: Stations

- [ ] 14. Station Form
  - [ ] 14.1 Create FormStation MDI child: station list, station details, hold inventory grid
  - [ ] 14.2 Crate UI pattern (master-detail grid, move to/from crate buttons)
  - [ ] 14.3 Component management panel for player-owned stations
  - [ ] 14.4 Munitions hold grid for armed stations
  - [ ] 14.5 Add "Stations" to Manage menu

- [ ] 15. Station Integration
  - [ ] 15.1 Update delivery route form to support Station and Asteroid stops
  - [ ] 15.2 Update delivery plan form to support Station and Asteroid stops
  - [ ] 15.3 Update delivery execution to handle station hold pickups/dropoffs

- [ ] 16. Checkpoint — Iteration 4 compiles and all tests pass

### Iteration 5: Market

- [ ] 17. Market Services
  - [ ] 17.1 Implement MarketService (RecordSale, RecordPurchase, ComputeProfitLoss)

- [ ] 18. Market Form
  - [ ] 18.1 Create FormMarket MDI child: Active Listings tab, Transaction History tab, Summary tab
  - [ ] 18.2 Record Sale button (creates transaction, decrements listing, sets cascade dirty flag)
  - [ ] 18.3 Record Purchase button (creates transaction, adds to station hold)
  - [ ] 18.4 Add "Market" to Manage menu

- [ ] 19. Iteration 5 Tests
  - [ ] 19.1 Property tests: market sale decrements listing, market purchase adds to hold
  - [ ] 19.2 Unit tests: MarketService edge cases, profit/loss computation

- [ ] 20. Checkpoint — Iteration 5 compiles and all tests pass

### Iteration 6: Full Production Queue

- [ ] 21. Production Queue Extension
  - [ ] 21.1 Enable Mining, Refining, Research BuildItemTypes in the Build Planner
  - [ ] 21.2 Add mining/refining resource fields to build item allocation UI
  - [ ] 21.3 Implement time-splitting (SequenceInStructure > 0, DependsOnUUID)
  - [ ] 21.4 Extend ResourceCheckService for mining/refining resource requirements
  - [ ] 21.5 Integrate asteroid mining into supply chain (AsteroidMine stage type)

- [ ] 22. Checkpoint — Iteration 6 compiles and all tests pass

### Iteration 7: Fill-Level Automation (Stock Targets)

- [ ] 23. Stock Target Services
  - [ ] 23.1 Implement StockTargetService (CheckTargets, GenerateReplenishmentItems)
  - [ ] 23.2 Implement stock target cascade in BackgroundProcessor (check targets → generate build items)

- [ ] 24. Stock Target Form
  - [ ] 24.1 Create FormStockTargets MDI child: target grid, add/edit/delete, "Check & Generate Orders"
  - [ ] 24.2 Add "Stock Targets" to Manage menu

- [ ] 25. Iteration 7 Tests
  - [ ] 25.1 Property test: stock target shortfall computation
  - [ ] 25.2 Unit tests: StockTargetService with mixed scopes, ship template expansion

- [ ] 26. Checkpoint — Iteration 7 compiles and all tests pass

### Iteration 8: Delivery Auto-Fill Time Horizon

- [ ] 27. Time Horizon Filter
  - [ ] 27.1 Add time horizon parameter to flatpack auto-fill (only include structures expected to be built within window)
  - [ ] 27.2 Add time horizon input to FormAutoFill dialog
  - [ ] 27.3 Update AutoFillFlatpacks to filter by build completion time

- [ ] 28. Checkpoint — Iteration 8 compiles and all tests pass

### Cross-Cutting

- [ ] 29. RouteStop Migration Property Test
  - [ ] 29.1 Property test: RouteStop migration preserves destinations (ColonyUUID → DestinationUUID + DestinationType.Colony)

- [ ] 30. Final checkpoint — Full test suite passes, all iterations integrated

## Notes

- Data model is built entirely in Iteration 1 to avoid refactoring. All models include fields for later iterations with empty defaults (omitted from JSON via DefaultValueHandling.Ignore).
- Iterations can be reordered based on priorities — the data model supports all from the start.
- Each iteration follows the pattern: models → services → forms → tests → checkpoint.
- New entity lists use List<T> (not BindingList<T>). Thread safety via PlayerContext._listLock.
- Lock ordering: _listLock → ColonyLock → _syncRoot. Events fired outside all locks.
- Deterministic UUIDs for shared game-world entities (Station, Asteroid, Faction, ExternalCharacter). Random UUIDs for player-specific entities.
- Asteroid UUID seed is "SystemName:AsteroidName" (names unique within system, not globally).
- Survey model extended with SurveyType (Planet/Asteroid) — no migration needed (defaults to Planet).
