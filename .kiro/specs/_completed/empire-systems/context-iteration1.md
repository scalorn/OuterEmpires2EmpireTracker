# Empire Systems ? Iteration Context: Iteration 1 (Build Planner + Data Model)

## What This Phase Does
Creates ALL 13 new data models, updates PlayerContext/PlayerRoot, adds in-memory indexes, creates the migration, and implements the Build Planner form with its supporting services.

## Prerequisites
- Pre-remediation complete (tasks 1-3)

## Key Files to Read
- .kiro/steering/empire-patterns.md (implementation patterns ? LOAD FIRST)
- .kiro/specs/empire-systems/design.md sections:
  - Data Models (all model definitions)
  - Services: BuildPlanService, ResourceCheckService, DeliveryGenerationService, QueueCalculator, AutoAssignService
  - Forms: FormBuildPlanner, Structure Allocation Dialog
  - Cascade Processing
  - In-Memory Indexing Requirements
- .kiro/specs/empire-systems/tasks.md (tasks 4-16)

## Key Design Decisions
- DestinationType enum includes Ship for future factory ships
- BuildItem uses BuildLocationType + BuildLocationUUID (not ColonyUUID)
- All automation entities have IsActive (BuildPlan, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule)
- Damage fields on ShipComponentSlot, Ship hull, Station hull, Item, MarketListing, MarketTransaction
- MarketTransaction snapshots CounterpartyFaction and condition
- RouteStop has Purpose (Cargo/Refuel/CargoAndRefuel) and FuelEstimate
- SupplyChainStageType uses PickUp (not Collect)
- All models include fields for later iterations with empty defaults (DefaultValueHandling.Ignore)

## What Gets Built
- 13 new model classes + 3 new enums
- PlayerRoot updates (13 new arrays)
- PlayerContext: 13 lists, 13 init methods, snapshot methods, convenience methods, events, cascade flags
- UUID caches and cross-entity indexes
- Migration (RouteStop/DeliveryPlanStop ColonyUUID migration)
- 5 services: BuildPlanService, ResourceCheckService, DeliveryGenerationService, QueueCalculator, AutoAssignService
- FormBuildPlanner with Structure Allocation Dialog
- Colony admin tab Generate Build Plan button
- Cascade processing in BackgroundProcessor
- FormContacts (Factions + External Characters)
- Reference counter expansions + BuildPlanReferenceCounter

## Completion Criteria
- Build passes
- All existing tests pass
- New property tests: P1-P5, P10-P12
- New unit tests for all Iteration 1 services and migration
