<!-- Extracted from spec/design/services.md — Stock Targets domain -->
# Services — Stock Targets

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

## Class Diagram

```mermaid
classDiagram
    class StockTargetService {
        <<static>>
        +CheckTargets(plans, currentPlayerUUID, colonyFinder, stationFinder, templateFinder, blueprintFinder, allColonies, allStations) List~StockShortfall~
        +GenerateReplenishmentItems(shortfalls, existingPlans) List~BuildItem~
        -ResolveCurrentQuantity(target, ...) int
        -CountItemStock(target, ...) int
        -CountShipTemplateStock(target, ...) int
        -MapToBuildItemType(itemType) BuildItemType
    }

    class StockTargetMutationService {
        -Logger Log
        -PlayerContext _playerContext
        +StockTargetMutationService(playerContext)
        +UpdatePlan(uuid, request) ReadOnlyStockPlan
        +CreatePlan(request) ReadOnlyStockPlan
        +DeletePlan(uuid) void
        +UpdateProfile(uuid, request) ReadOnlyStockProfile
        +CreateProfile(request) ReadOnlyStockProfile
        +DeleteProfile(uuid) void
    }

    class StockShortfall {
        +StockTarget Target
        +string PlanUUID
        +int CurrentQuantity
        +int ShortfallQuantity
        +bool IsCritical
    }

    class ReadOnlyStockPlan {
        +string UUID
        +string Name
        +bool IsActive
    }

    class ReadOnlyStockProfile {
        +string UUID
        +string Name
        +bool IsActive
    }

    class BuildItem {
        +string UUID
        +string ItemName
        +BuildItemType ItemType
        +int Quantity
    }

    StockTargetService --> StockShortfall : creates
    StockTargetService --> BuildItem : creates
    StockTargetMutationService --> PlayerContext : uses
    StockTargetMutationService --> ReadOnlyStockPlan : returns
    StockTargetMutationService --> ReadOnlyStockProfile : returns
```
