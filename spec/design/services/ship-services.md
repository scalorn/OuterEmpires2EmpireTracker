<!-- Extracted from spec/design/services.md — Ship domain -->
# Services — Ships

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

See [data-models.md](../data-models.md) for ShipStats and StationStats class definitions.

## Class Diagram

```mermaid
classDiagram
    class ShipBuildService {
        <<static>>
        +GenerateShipBuildItems(template, assemblyLocationUUID, assemblyLocationType, colonyFinder, stationFinder, blueprintFinder) List~BuildItem~
        +ValidateAssemblyLocation(shipClass, stationType) string
        +ComputeStats(hull, components, blueprintFinder) ShipStats
        +ComputeStationStats(stationBlueprint, components, blueprintFinder) StationStats
    }

    class ShipService {
        -Logger Log
        -PlayerContext _playerContext
        +ShipService(playerContext)
        +Update(uuid, request) ReadOnlyShip
        +Create(request) ReadOnlyShip
        +Delete(uuid) void
        +CreateFromTemplate(templateUUID) ReadOnlyShip
    }

    class ShipStats {
        +int CargoCapacity
        +int Speed
        +int HullHP
        +int ShieldHP
        +int WeaponDamage
    }

    class StationStats {
        +int HoldCapacity
        +int HullHP
        +int ShieldHP
    }

    class ReadOnlyShip {
        +string UUID
        +string Name
        +string TemplateUUID
    }

    class BuildItem {
        +string UUID
        +string ItemName
        +BuildItemType ItemType
        +int Quantity
    }

    ShipBuildService --> BuildItem : creates
    ShipBuildService --> ShipStats : creates
    ShipBuildService --> StationStats : creates
    ShipService --> PlayerContext : uses
    ShipService --> ReadOnlyShip : returns
```
