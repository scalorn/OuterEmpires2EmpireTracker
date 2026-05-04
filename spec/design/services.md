<!-- Extracted from .kiro/specs/empire-systems/design.md — Services section -->
# Services

## BuildPlanService (Iteration 1)

Static service in `Services/BuildPlanService.cs`.

```csharp
public static class BuildPlanService
{
    public static bool ValidatePlanName(string name);
    public static bool ValidateBuildItem(BuildItem item);

    /// <summary>
    /// Scans colony structures for unstaged entries and generates Manufactory
    /// build items for their flatpack blueprints. Skips structures already
    /// covered by existing items in the target plan. Items are created
    /// unallocated (empty BuildLocationUUID). Returns count of items added.
    /// </summary>
    public static int GenerateColonyBuildItems(
        Colony colony, BuildPlan targetPlan,
        Func<string, Blueprint> blueprintFinder);
}
```

- Unstaged = `IsStaged == false && IsBuilt == false`.
- Each produces one Manufactory BuildItem with `Quantity = 1`.
- Colony UUID stored in Notes for traceability.

## ResourceCheckService (Iteration 1)

Static service in `Services/ResourceCheckService.cs`.

```csharp
public static class ResourceCheckService
{
    /// <summary>
    /// Computes resource shortfalls for a build item at its allocated location.
    /// Returns resource name → shortfall quantity. Empty = all available.
    /// </summary>
    public static Dictionary<string, int> ComputeShortfalls(
        BuildItem item, ItemBag locationInventory,
        Func<string, Blueprint> blueprintFinder);

    /// <summary>
    /// Computes shortfalls for all allocated items in a build plan.
    /// Returns per-item shortfall maps keyed by BuildItem UUID.
    /// </summary>
    public static Dictionary<string, Dictionary<string, int>> ComputePlanShortfalls(
        BuildPlan plan, Func<string, Colony> colonyFinder,
        Func<string, Ship> shipFinder,
        Func<string, Station> stationFinder,
        string currentPlayerUUID,
        Func<string, Blueprint> blueprintFinder);
}
```

Logic:
- Resolves inventory by BuildLocationType: Colony → `colony.Items`, Ship → `ship.Cargo`, Station → `station.Holds[currentPlayerUUID]`.
- Manufactory: iterate blueprint.Resources, multiply by Quantity, subtract inventory stock.
- Commodity: iterate commodity.ConstructionResources, multiply by Quantity (runs).
- Research: no resource shortfall (time + lab only).
- Returns only positive shortfalls.

## BuildPlanExecutionService

Static service in `Services/BuildPlanExecutionService.cs`.

```csharp
public static class BuildPlanExecutionService
{
    // Status helpers
    public static Dictionary<BuildItemStatus, int> ComputeStatusSummary(BuildPlan plan);
    public static bool IsPlanComplete(BuildPlan plan);
    public static List<ContentionInfo> DetectContention(BuildItem item, IEnumerable<BuildPlan> allPlans);

    // Status detection (called by BackgroundProcessor cascade)
    public static bool AdvanceBuildItemStatuses(
        BuildPlan plan, Func<string, Colony> colonyFinder,
        Func<string, Blueprint> blueprintFinder,
        Func<string, Ship> shipFinder,
        Func<string, Station> stationFinder,
        string currentPlayerUUID);

    // Manufacturing pre-configuration
    public static bool CanStartManufacturing(
        BuildItem item, BuildPlan plan,
        Func<string, Colony> colonyFinder,
        Func<string, Blueprint> blueprintFinder);
    public static StartManufacturingResult StartManufacturing(
        BuildItem item, BuildPlan plan,
        Func<string, Colony> colonyFinder,
        Func<string, Blueprint> blueprintFinder);
    public static BatchStartResult StartAllReady(
        BuildPlan plan, Func<string, Colony> colonyFinder,
        Func<string, Blueprint> blueprintFinder);

    // Nested result types
    public class StartManufacturingResult { bool Success; string ErrorMessage; }
    public class BatchStartResult { int StartedCount; int SkippedCount; List<string> SkippedReasons; }
    public class ContentionInfo { string PlanName; string ItemName; string ItemUUID; }
}
```

Logic:
- Stateless service following the same pattern as ResourceCheckService.
- ComputeStatusSummary returns counts per BuildItemStatus for a plan. All enum values present, defaulting to zero.
- IsPlanComplete returns true only when all items are Completed (false for empty plans).
- DetectContention finds items from other active plans assigned to the same StructureUUID.
- AdvanceBuildItemStatuses handles three detection phases: Staged+allocated→Ready (zero shortfalls), Ready→InProgress (matching active job on structure), InProgress→Completed (job finished).
- CanStartManufacturing checks eligibility: Ready status, valid structure, structure idle, lowest sequence, dependency satisfied.
- StartManufacturing pre-configures ColonyStructure fields and advances item to InProgress.
- StartAllReady batch-starts first eligible Ready item per structure.

Satisfies: REQ-BPL-EXE (see .kiro/specs/build-plan-execution/requirements.md)

## DeliveryGenerationService (Iteration 1)

Static service in `Services/DeliveryGenerationService.cs`.

```csharp
public static class DeliveryGenerationService
{
    /// <summary>
    /// Creates/updates delivery plan for a single build plan's shortfalls.
    /// </summary>
    public static DeliveryPlan GenerateDeliveryPlan(
        BuildPlan buildPlan, DeliveryRoute route,
        Dictionary<string, Dictionary<string, int>> shortfalls,
        Func<string, Colony> colonyFinder,
        PlayerContext playerContext);

    /// <summary>
    /// Consolidates shortfalls across multiple plans into one delivery plan.
    /// Merges duplicate resources by destination.
    /// </summary>
    public static DeliveryPlan GenerateConsolidatedDeliveryPlan(
        IEnumerable<BuildPlan> buildPlans, DeliveryRoute route,
        Func<BuildPlan, Dictionary<string, Dictionary<string, int>>> shortfallProvider,
        Func<string, Colony> colonyFinder,
        PlayerContext playerContext, string planName);

    /// <summary>
    /// Generates flatpack delivery plan for completed build items.
    /// Groups by destination colony on the route.
    /// </summary>
    public static DeliveryPlan GenerateFlatpackDeliveryPlan(
        IEnumerable<BuildPlan> buildPlans, DeliveryRoute route,
        Func<string, Colony> colonyFinder,
        Func<string, Blueprint> blueprintFinder,
        PlayerContext playerContext, string planName);
}
```

Two-phase delivery workflow:
1. Consolidated Resource Delivery — merge resource shortfalls across plans
2. Flatpack Delivery — deliver completed flatpacks to destination colonies

## QueueCalculator (Iteration 1)

Static service in `Services/QueueCalculator.cs`.

```csharp
public static class QueueCalculator
{
    /// <summary>
    /// Computes manufacturing runs to fill target duration. Returns -1 if time unknown.
    /// </summary>
    public static int ComputeManufactoryRuns(Blueprint blueprint, int targetDurationSeconds);

    /// <summary>
    /// Computes commodity runs to fill target duration.
    /// </summary>
    public static int ComputeCommodityRuns(int targetDurationSeconds);

    /// <summary>
    /// Total items produced for N manufactory runs (accounts for items-per-run).
    /// </summary>
    public static int ManufactoryRunsToItems(Blueprint blueprint, int runs);

    /// <summary>
    /// Total items produced for N commodity runs.
    /// </summary>
    public static int CommodityRunsToItems(int runs);
}
```

- Manufactory: `ceiling(targetSeconds / mfgSeconds)`.
- Commodity: `ceiling(targetSeconds / CommodityCycleSeconds)`.

## AutoAssignService (Iteration 1)

Static service in `Services/AutoAssignService.cs`.

```csharp
public static class AutoAssignService
{
    /// <summary>
    /// Proposes structure assignments for unallocated build items,
    /// minimizing total completion time while respecting blueprint copy limits.
    /// </summary>
    public static List<AssignmentProposal> ProposeAssignments(
        BuildPlan plan, DeliveryRoute route,
        Func<string, Colony> colonyFinder,
        Func<string, Ship> shipFinder,
        Func<string, Station> stationFinder,
        Func<string, Blueprint> blueprintFinder);
}

public class AssignmentProposal
{
    public string BuildItemUUID { get; set; }
    public DestinationType BuildLocationType { get; set; } = DestinationType.Colony;
    public string BuildLocationUUID { get; set; }
    public string StructureUUID { get; set; }
    public int SequenceInStructure { get; set; }
    public string Reason { get; set; }
}
```

Logic:
1. Collect idle structures at locations on the delivery route.
2. Count blueprint copies per type (max parallelism).
3. Distribute runs across `min(structures, copies)`, splitting evenly.
4. Stack on existing assignments if more items than capacity.
5. Sort to minimize longest completion time.

## ShipBuildService (Iteration 2)

Static service in `Services/ShipBuildService.cs`.

```csharp
public static class ShipBuildService
{
    public static List<BuildItem> GenerateShipBuildItems(
        ShipTemplate template,
        string assemblyLocationUUID, DestinationType assemblyLocationType,
        Func<string, Colony> colonyFinder,
        Func<string, Station> stationFinder,
        Func<string, Blueprint> blueprintFinder);

    public static string ValidateAssemblyLocation(int shipClass, StationType stationType);

    public static ShipStats ComputeStats(
        Blueprint hull, IEnumerable<ShipComponentSlot> components,
        Func<string, Blueprint> blueprintFinder);

    public static StationStats ComputeStationStats(
        Blueprint stationBlueprint, IEnumerable<ShipComponentSlot> components,
        Func<string, Blueprint> blueprintFinder);
}
```

See [data-models.md](data-models.md) for ShipStats and StationStats class definitions.

## MarketService (Iteration 5)

Static service in `Services/MarketService.cs`.

```csharp
public static class MarketService
{
    public static MarketTransaction RecordSale(
        MarketListing listing, int quantitySold, decimal pricePerUnit,
        string counterpartyName, string counterpartyFactionName, string stationUUID);
    public static MarketTransaction RecordPurchase(
        ItemType.ItemTypeEnum itemType, string itemName, string itemReferenceID,
        int quantity, decimal pricePerUnit, string stationUUID,
        string counterpartyName, string counterpartyFactionName,
        string ownerUUID, Func<string, Station> stationFinder);
    public static ProfitLossSummary ComputeProfitLoss(
        IEnumerable<MarketTransaction> transactions,
        DateTime? startDate = null, DateTime? endDate = null,
        string itemNameFilter = null, string stationUUIDFilter = null);
}
```

## StockTargetService (Iteration 7)

Static service in `Services/StockTargetService.cs`.

```csharp
public static class StockTargetService
{
    public static List<StockShortfall> CheckTargets(
        IEnumerable<StockPlan> plans, string currentPlayerUUID,
        Func<string, Colony> colonyFinder, Func<string, Station> stationFinder,
        Func<string, ShipTemplate> templateFinder, Func<string, Blueprint> blueprintFinder,
        IEnumerable<Colony> allColonies, IEnumerable<Station> allStations);

    public static List<BuildItem> GenerateReplenishmentItems(
        List<StockShortfall> shortfalls, IEnumerable<BuildPlan> existingPlans);
}

public class StockShortfall
{
    public StockTarget Target { get; set; }
    public string PlanUUID { get; set; }
    public int CurrentQuantity { get; set; }
    public int ShortfallQuantity { get; set; }
    public bool IsCritical { get; set; }
}
```

- Within a plan: OR pooling (max across overlapping components).
- Across plans: AND/dedicated (summed).

## SupplyChainService (Iteration 6)

Static service in `Services/SupplyChainService.cs`.

```csharp
public static class SupplyChainService
{
    public static List<SupplyChainDeliveryRequest> CheckThresholds(
        IEnumerable<SupplyChain> chains,
        Func<string, Colony> colonyFinder,
        Func<string, Station> stationFinder,
        Func<string, Ship> shipFinder,
        string currentPlayerUUID);
}

public class SupplyChainDeliveryRequest
{
    public string SupplyChainUUID { get; set; }
    public int StageSequence { get; set; }
    public string ResourceName { get; set; }
    public string ResourcePurity { get; set; }
    public int ExcessQuantity { get; set; }
    public string DeliveryRouteUUID { get; set; }
    public string SourceLocationUUID { get; set; }
    public DestinationType SourceLocationType { get; set; }
}
```

- Filters to `IsActive` chains only.
- Inventory resolution: Colony → `colony.Items`, Station → `station.Holds[currentPlayerUUID]`, Ship → `ship.Cargo`.
- ExcessQuantity = quantity - threshold.
## CargoVolumeService

Static service in Services/CargoVolumeService.cs.

`csharp
public static class CargoVolumeService
{
    public static CargoLoadResult ComputeLoadVolume(
        List<DeliveryItem> loadList,
        Func<string, Blueprint> blueprintFinder);

    public static List<List<DeliveryItem>> SplitIntoTrips(
        List<DeliveryItem> loadList,
        decimal cargoCapacity,
        Func<string, Blueprint> blueprintFinder);
}
`

Logic:
- ComputeLoadVolume sums per-item volume × quantity. Volume by type: Resource=1, Commodity=10, WorkDetail=50, Blueprint/Survey=0, manufactured items=CargoVolumeSize property from blueprint.
- Crate items use the crate blueprint's own Cargo Volume Size (one-level, no nesting).
- SplitIntoTrips distributes items across trips within a cargo capacity limit. Items are assigned in order; oversized items get their own trip.

## BlueprintImportHandler

Static service in Services/BlueprintImportHandler.cs.

`csharp
public static class BlueprintImportHandler
{
    public static ImportType ClassifyImport(Blueprint tempBP);
    public static FindTargetResult FindTarget(Blueprint tempBP, Blueprint selected, PlayerContext pc, EmpireContext ec);
    public static Blueprint MergeAndPersist(FindTargetResult findResult, Blueprint tempBP, PlayerContext pc, EmpireContext ec);
}
`

Logic:
- ClassifyImport: ResourcesOnly (has resources but no properties/type), Full (has name), NoName (fallback).
- FindTarget: checks selected blueprint match first (Name+Evolution+Type), then dedup via MarketBlueprintImporter.FindByDedupKey. Routes Evo0→global, others→player.
- MergeAndPersist: updates existing or creates new with deterministic UUID (global) or random UUID (player), persists, fires event.

## CrateImporter

Static service in Services/CrateImporter.cs.

```csharp
public static class CrateImporter
{
    public static CrateImportResult ImportFromFile(string filePath, PlayerContext pc, EmpireContext ec);
    public static CrateImportResult ImportFromJson(string json, PlayerContext pc, EmpireContext ec);
}
```

Logic:
- Imports blueprints from a JSON file produced by the OE2 Blueprint Scraper browser console script.
- Each JSON entry contains name, evolution, techLevel, description, iconClass, properties, and resources scraped from the game UI.
- Parses each entry into a temporary Blueprint, applies property key remapping (same as BlueprintScanner), normalizes values.
- Routes via MarketBlueprintImporter.IsGlobalRoute (Evo0→global, others→player).
- Dedup via MarketBlueprintImporter.FindByDedupKey; updates existing or creates new.
- Persists once at the end (batch save), fires BlueprintDataChanged event.
- Blueprint type resolved via name-based classification (ReclassifyByName). Icon CSS class stored as `_IconClass` property for future mapping.

## SerializationSorter

Static helper in Services/SerializationSorter.cs.

```csharp
public static class SerializationSorter
{
    public static PlayerRoot SortPlayerRoot(PlayerRoot source);
    public static BaselineRoot SortBaselineRoot(BaselineRoot source);
    internal static T[] SortByString<T>(T[] source, Func<T, string> keySelector);
    internal static T[] SortByInt<T>(T[] source, Func<T, int> keySelector);
    internal static T[] SortByStringThenInt<T>(T[] source, Func<T, string> key1, Func<T, int> key2);
    internal static T[] SortByStringThenString<T>(T[] source, Func<T, string> key1, Func<T, string> key2);
}
```

Logic:
- SortPlayerRoot creates a new PlayerRoot with all 20 top-level arrays sorted by UUID (ordinal string), then sorts nested arrays on their respective keys (Colony.Structures by UUID, Colony.Commodities by Name, DeliveryRoute.Stops by Sequence, etc.).
- SortBaselineRoot creates a new BaselineRoot with arrays sorted by primary key (BlueprintType.Id, ShipClass.Id, TechLevel.Name, Commodity.ID, RefiningRecipe.OutputResource, ResearchTimeEntry.Evolution).
- Sort helpers return new arrays; null input returns empty array. Null keys coalesced to empty string (sort first).
- Called by WriteContext() methods before JSON serialization to produce deterministic output.

Satisfies: REQ-JSON-ORDER (see .kiro/specs/json-deterministic-order/requirements.md)

## SortedDictionaryContractResolver

Custom contract resolver in Services/SortedDictionaryContractResolver.cs.

```csharp
public class SortedDictionaryContractResolver : DefaultContractResolver
{
    protected override JsonDictionaryContract CreateDictionaryContract(Type objectType);
}
```

Logic:
- Overrides `CreateDictionaryContract` to attach a `SortedDictionaryConverter` to all `Dictionary<string, T>` types.
- The converter serializes dictionary entries with keys sorted in ascending ordinal string order (`StringComparer.Ordinal`).
- Deserialization is unaffected (`CanRead => false`).
- Registered in `JsonSettings.SerializerSettings` as the default contract resolver.
- Does not affect `ItemBag` which has its own `[JsonConverter]` attribute.

Satisfies: REQ-JSON-ORDER (see .kiro/specs/json-deterministic-order/requirements.md)


## BuildOrderOptimizer

Service in `Services/BuildOrderOptimizer.cs`.

Reorders colony structures so Power, Habitation, Food, and Entertainment constraints are satisfied at every build step after the first primary structure.

**Algorithm:**
1. Classify structures as Support (power/hab/food/ent providers + CC) or Primary (everything else).
2. Bootstrap: seed with CC, Reactor, Hab Block, Hydroponics Bay, Entertainment Centre.
3. For each primary: fix existing deficits, look ahead for future deficits (primary + hab + hydro), pre-place support, then place the primary.
4. Append leftover support, fixing deficits as each is added.

**Incremental simulation:** Maintains a running `ColonyStructureStatus` accumulator updated via `SimulateOneMore` (O(1)) each time a structure is appended. Look-ahead projections use `SimulateOneMore` against the accumulator without modifying it. Overall complexity: O(n) where n = output structure count.

**Key methods:**
- `Optimize(Colony colony)` → `List<ColonyStructure>` — main entry point
- `IsSupportStructure(Blueprint blueprint)` → `bool` — classification
- `FixDeficits(result, pool, targetStatus, workers, ref accumulator)` — deficit resolution loop (private)
- `PlaceSupportSafe(result, pool, deficitType, workers, ref accumulator)` — recursive support placement (private)
- `SimulateOneMore(prev, structure, blueprint, workers)` → `ColonyStructureStatus` — O(1) delta computation (private)

Satisfies: REQ-COL-095 through REQ-COL-095g

## CollectionSortHelper

Static helper in Services/CollectionSortHelper.cs.

```csharp
public static class CollectionSortHelper
{
    // Generic name-based sort for any INamed entity
    public static IReadOnlyList<T> OrderByName<T>(IEnumerable<T> items) where T : INamed;

    // Domain-specific sort methods — each returns a new sorted IReadOnlyList<T>
    public static IReadOnlyList<ColonyStructure> OrderStructures(IEnumerable<ColonyStructure> items);
    public static IReadOnlyList<RouteStop> OrderRouteStops(IEnumerable<RouteStop> items);
    public static IReadOnlyList<DeliveryPlanStop> OrderPlanStops(IEnumerable<DeliveryPlanStop> items);
    public static IReadOnlyList<SupplyChainStage> OrderSupplyChainStages(IEnumerable<SupplyChainStage> items);
    public static IReadOnlyList<Colony> OrderColonies(IEnumerable<Colony> items);
    public static IReadOnlyList<Survey> OrderSurveys(IEnumerable<Survey> items);
    public static IReadOnlyList<ShipComponentSlot> OrderComponents(IEnumerable<ShipComponentSlot> items);
    public static IReadOnlyList<Blueprint> OrderBlueprints(IEnumerable<Blueprint> items);
    // ... plus 20+ additional Order* methods for every domain collection
}
```

Logic:
- Pure static utility — no state, no side effects, no logger needed.
- Each method accepts an `IEnumerable<T>`, applies a domain-appropriate sort key (Name, Sequence, Timestamp, etc.), and returns a new `IReadOnlyList<T>`.
- Centralizes all collection ordering so that UI and service code never sort inline. Data model collections are unordered bags; sorting happens at consumption via this helper.
- Null-safe: null inputs return empty lists; null sort keys coalesced to empty string or zero.

Satisfies: REQ-DATA-ORDER (see .kiro/specs/data-model-ordering-invariant/requirements.md)

## PricingPlanService

Instance service in `Services/PricingPlanService.cs`. Sole mutator of PricingPlan entities (create, update, delete). Follows the same pattern as BlueprintService and PlayerProfileService from BL-108/BL-111.

```csharp
public class PricingPlanService
{
    public PricingPlanService(PlayerContext playerContext);
    public ReadOnlyPricingPlan Update(string uuid, PricingPlanUpdateRequest request);
    public ReadOnlyPricingPlan Create(PricingPlanCreateRequest request);
    public void Delete(string uuid);
}
```

Logic:
- Update: looks up mutable entity via `FindMutablePricingPlan`, applies all scalar fields and ResourcePrices, persists via `WriteContext()`, fires `PricingDataChanged`, returns `ReadOnlyPricingPlan`.
- Create: creates new PricingPlan with generated UUID, sets OwnerUUID from current player, populates fields, adds to PlayerContext, persists, fires event.
- Delete: looks up entity, removes from PlayerContext, persists, fires event. Returns silently if UUID empty or not found.

Satisfies: REQ-PRC (see .kiro/specs/bl-123-pricingplan-readonly/requirements.md)
## DeliveryRouteService

Instance service in Services/DeliveryRouteService.cs. Sole mutator of DeliveryRoute entities (create, update, delete). Uses DeliveryRouteCreateRequest and DeliveryRouteUpdateRequest DTOs.

Logic:
- Update: looks up mutable entity via FindMutableDeliveryRoute, applies Name, replaces Stops list with deep copy, persists via WriteContext(), fires DeliveryDataChanged, returns ReadOnlyDeliveryRoute.
- Create: creates new DeliveryRoute with generated UUID, sets OwnerUUID from current player, populates Name and Stops from request, adds to PlayerContext, persists, fires event.
- Delete: looks up entity, removes from PlayerContext, persists, fires event. Returns silently if UUID empty or not found.

Satisfies: REQ-DEL (see .kiro/specs/bl-112-deliveryroute-readonly/requirements.md)
