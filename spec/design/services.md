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

    public static string ValidateAssemblyLocation(int shipClass, Station station);

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
    public static bool RecordSale(MarketTransaction transaction, List<MarketListing> listings);
    public static void RecordPurchase(MarketTransaction transaction, Func<string, Station> stationFinder);
    public static decimal ComputeProfitLoss(MarketTransaction transaction, PricingPlan plan, Func<string, Blueprint> blueprintFinder);
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