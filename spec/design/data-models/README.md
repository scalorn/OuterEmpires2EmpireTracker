# Data Models

Split into domain-specific files for easier navigation and maintenance.

| File | Contents |
|------|----------|
| [build-planner.md](build-planner.md) | BuildPlan, BuildItem (enums BuildItemType, BuildItemStatus) |
| [ships.md](ships.md) | ShipTemplate, ShipComponentSlot, Ship |
| [stations.md](stations.md) | Station (enums StationType, StationOwnership) |
| [asteroids.md](asteroids.md) | Asteroid, AsteroidReserve, Survey model extensions (SurveyType enum, AsteroidUUID field) |
| [market.md](market.md) | MarketListing, MarketTransaction (enum TransactionType) |
| [stock-targets.md](stock-targets.md) | StockPlan, StockTarget, StockProfile, StockProfileEntry (enum StockTargetScope) |
| [contacts.md](contacts.md) | Faction, ExternalCharacter |
| [supply-chains.md](supply-chains.md) | SupplyChain, SupplyChainStage (enum SupplyChainStageType), WarehouseOverflowRule |
| [delivery.md](delivery.md) | RouteStop changes, DeliveryPlan changes (ShipUUID, DestinationType on stops) |
| [shared.md](shared.md) | Shared Enums (DestinationType), Item changes (Crate support, Component Damage), PlayerProfile changes, IsActive pattern, PlayerRoot changes, PlayerContext list encapsulation |
| [colony.md](colony.md) | Colony Status Calculation Models (ColonyStructureStatus, StructureStatusDelta, ColonyWorker, ResearchTimeEntry) |
| [viewmodels.md](viewmodels.md) | ViewModels section (BlueprintViewModel, PricingPlanViewModel, BlueprintFilterCriteria) |
| [readonly-wrappers.md](readonly-wrappers.md) | Read-Only Data Wrappers (all ReadOnly* classes, utility wrappers, nested type wrappers, service DTOs) |

## Conventions

All new models follow the existing POCO pattern: public properties with defaults, Newtonsoft.Json serialization, UUID + OwnerUUID ownership, persisted as top-level arrays in PlayerRoot.
