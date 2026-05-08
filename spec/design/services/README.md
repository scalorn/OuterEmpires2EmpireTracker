# Services

Split into domain-specific files for maintainability.

## Design Files

| File | Services |
|------|----------|
| [build-planner-services.md](build-planner-services.md) | BuildPlanService, ResourceCheckService, BuildPlanExecutionService, QueueCalculator, AutoAssignService, DeliveryGenerationService |
| [ship-services.md](ship-services.md) | ShipBuildService |
| [market-services.md](market-services.md) | MarketService, MarketListingService |
| [stock-services.md](stock-services.md) | StockTargetService |
| [supply-chain-services.md](supply-chain-services.md) | SupplyChainService |
| [delivery-services.md](delivery-services.md) | CargoVolumeService, DeliveryRouteService |
| [blueprint-services.md](blueprint-services.md) | BlueprintImportHandler, CrateImporter |
| [serialization-services.md](serialization-services.md) | SerializationSorter, SortedDictionaryContractResolver |
| [colony-services.md](colony-services.md) | BuildOrderOptimizer |
| [shared-services.md](shared-services.md) | CollectionSortHelper, PricingPlanService |

## Mutation Services

Instance services that are the sole mutators of their respective entities. Each owns CRUD operations and calls WriteContext() to persist changes.

| Class | Description | Documented In |
|-------|-------------|---------------|
| ColonyService | CRUD for colonies; manages colony lifecycle | spec/requirements/Colony.md |
| BlueprintService | CRUD for blueprints; manages blueprint lifecycle | spec/requirements/Blueprints.md |
| PlayerProfileService | CRUD for player profiles; manages profile lifecycle | spec/requirements/PlayerProfiles.md |
| SurveyService | CRUD for surveys; manages survey lifecycle | spec/requirements/Surveys.md |
| DeliveryRouteService | CRUD for delivery routes | spec/requirements/DeliveryRoutes.md |
| DeliveryPlanService | CRUD for delivery plans | spec/requirements/DeliveryRoutes.md |
| ShipService | CRUD for ship instances | spec/requirements/Ships.md |
| ShipTemplateService | CRUD for ship templates | spec/requirements/Ships.md |
| StationService | CRUD for stations | spec/requirements/Stations.md |
| MarketListingService | CRUD for market listings | spec/design/services/market-services.md |
| BuildPlanMutationService | Mutates build plan items and state | spec/design/services/build-planner-services.md |
| StockTargetMutationService | Mutates stock targets and plans | spec/design/services/stock-services.md |
| SupplyChainMutationService | Mutates supply chain stages | spec/design/services/supply-chain-services.md |
| ContactsService | CRUD for factions and external characters | spec/requirements/Contacts.md |
| AsteroidService | CRUD for asteroids | spec/requirements/Asteroids.md |
| PricingPlanService | CRUD for pricing plans | spec/design/services/shared-services.md |

## Reference Counters

Count cross-entity UUID references to enable delete protection. All follow the same pattern: constructor takes collections to search, CountReferences(uuid) returns a typed report with per-source counts and TotalCount. Documented in [spec/design/reference-counting.md](../reference-counting.md).

| Class | Entity Protected | Status |
|-------|-----------------|--------|
| BlueprintReferenceCounter | Blueprint | Existing (expanding) |
| ColonyReferenceCounter | Colony | Existing (expanding) |
| SurveyReferenceCounter | Survey | Existing (expanding) |
| StationReferenceCounter | Station | New |
| ShipReferenceCounter | Ship | New |
| ShipTemplateReferenceCounter | ShipTemplate | New |
| AsteroidReferenceCounter | Asteroid | New |
| FactionReferenceCounter | Faction | New |
| BuildPlanReferenceCounter | BuildPlan | New |
| DeliveryRouteReferenceCounter | DeliveryRoute | New |
| DeliveryPlanReferenceCounter | DeliveryPlan | New |
| StockPlanReferenceCounter | StockPlan | New |
| MarketListingReferenceCounter | MarketListing | New |

## Singletons & Context

| Class | Description | Documented In |
|-------|-------------|---------------|
| PlayerContext | Singleton; loads/manages player data, persistence, UUID caches, change events | spec/design/indexing.md, spec/requirements/DataModel.md |
| EmpireContext | Singleton; loads/manages shared game data (baseline commodities, resources, ship classes) | spec/requirements/DataModel.md |

## Static Utilities

Stateless helper classes providing calculations, background processing, and domain logic.

| Class | Description | Documented In |
|-------|-------------|---------------|
| ColonyStatusCalculator | Computes built/ideal status for colony structures | spec/requirements/Colony.md |
| ColonyActivityCollector | Collects colony activity data for reporting | spec/requirements/ColonyActivity.md |
| ColonyInactivityCollector | Identifies inactive colonies for alerting | spec/requirements/ColonyActivity.md |
| ColonyBootstrap | Initializes new colony with default structures | spec/requirements/Colony.md |
| ColonyBuildEligibility | Checks whether a colony structure is eligible to build | spec/requirements/Colony.md |
| ColonyAdminReportBuilder | Builds admin report data for colony overview | spec/requirements/Colony.md |
| BuildTimeCalculator | Calculates build/research/manufacturing durations | spec/design/services/build-planner-services.md |
| BuildOrderOptimizer | Optimizes build order for colony daily builds | spec/design/services/colony-services.md |
| DeliveryFulfillment | Computes delivery fulfillment status and shortfalls | spec/requirements/DeliveryRoutes.md |
| DeliveryGenerationService | Generates delivery plans from build plan requirements | spec/design/services/build-planner-services.md |
| EvolutionChainService | Resolves blueprint evolution chains | spec/requirements/Blueprints.md |
| TabWarningService | Computes warning indicators for main window tabs | spec/requirements/Architecture.md |
| SurveyDateTimeParser | Parses survey date/time strings from game HTML | spec/requirements/Surveys.md |
| PriceCalculator | Computes commodity prices from pricing plans | spec/requirements/PricingPlans.md |
| BackgroundProcessor | Runs timed processing cycles (mining, refining, research) | spec/requirements/Colony.md |
| ResourceCheckService | Checks resource availability for build plans | spec/design/services/build-planner-services.md |
| QueueCalculator | Calculates build queue ordering and timing | spec/design/services/build-planner-services.md |
| AutoAssignService | Auto-assigns build items to locations | spec/design/services/build-planner-services.md |
| CargoVolumeService | Calculates cargo volumes for delivery routes | spec/design/services/delivery-services.md |
| ShipBuildService | Orchestrates ship build workflows | spec/design/services/ship-services.md |
| CollectionSortHelper | Provides deterministic sorting for unordered collections | spec/design/services/shared-services.md |
| HelpRenderer | Renders markdown help content for in-app display | docs/README.md |
| HelpTopicRegistry | Maps help topics to their markdown files | docs/README.md |
| JsonSettings | Provides shared Newtonsoft.Json serializer settings | spec/requirements/DataModel.md |
| PreferencesStore | Persists user preferences to file | spec/requirements/Preferences.md |
| SystemClock | Abstraction over DateTime.Now for testability | spec/requirements/NonFunctional.md |

## Parsers & Import Helpers

Parse game HTML/clipboard data and import external content into the data model. Located in OE2EmpireTracker/Parsers/ and OE2EmpireTracker/Services/.

| Class | Description | Documented In |
|-------|-------------|---------------|
| ColonyParser | Parses colony HTML into Colony model | spec/requirements/Colony.md |
| SurveyParser | Parses survey HTML into Survey model | spec/requirements/Surveys.md |
| PlayerProfileParser | Parses player profile HTML into PlayerProfile model | spec/requirements/PlayerProfiles.md |
| BlueprintScanner | Scans blueprint HTML into Blueprint model | spec/requirements/Blueprints.md |
| MarketBlueprintImporter | Imports blueprints from market listing data | spec/design/services/blueprint-services.md |
| ColonyImportHelper | Orchestrates colony import from clipboard/HTML | spec/requirements/Colony.md |
| SurveyImportHelper | Orchestrates survey import from clipboard/HTML | spec/requirements/Surveys.md |
| ClipboardHelper | Reads and writes system clipboard content | spec/requirements/Architecture.md |
| ClipboardContentDetector | Detects content type from clipboard text | spec/requirements/Architecture.md |
| MinerSetupHelper | Parses miner setup data from clipboard | spec/requirements/Colony.md |
| RefinerySetupHelper | Parses refinery setup data from clipboard | spec/requirements/Colony.md |
| CountdownFormatParser | Parses countdown timer strings (e.g. "2d 3h 15m") | spec/requirements/Colony.md |
| CrateImporter | Imports crate/container data from clipboard | spec/design/services/blueprint-services.md |
| BlueprintImportHandler | Orchestrates blueprint import workflows | spec/design/services/blueprint-services.md |

## Serialization

| Class | Description | Documented In |
|-------|-------------|---------------|
| SerializationSorter | Sorts JSON properties for deterministic output | spec/design/services/serialization-services.md |
| SortedDictionaryContractResolver | Custom JSON contract resolver for sorted dictionaries | spec/design/services/serialization-services.md |

## Migration

Data migration utilities in OE2EmpireTracker/Services/Migration/.

| Class | Description | Documented In |
|-------|-------------|---------------|
| MigrationRunner | Executes data migrations in sequence | spec/requirements/DataModel.md |
| DeterministicUUID | Generates deterministic UUIDs for migration | spec/requirements/DataModel.md |
| RemapUUID | Remaps old UUIDs to new ones during migration | spec/requirements/DataModel.md |
| RenameTable | Renames data tables during migration | spec/requirements/DataModel.md |