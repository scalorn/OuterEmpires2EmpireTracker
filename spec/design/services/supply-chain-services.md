<!-- Extracted from spec/design/services.md — Supply Chain domain -->
# Services — Supply Chain

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

## Class Diagram

```mermaid
classDiagram
    class SupplyChainService {
        <<static>>
        -Logger Log
        +CheckThresholds(chains, colonyFinder, stationFinder, shipFinder, currentPlayerUUID) List~SupplyChainDeliveryRequest~
        -ResolveInventory(stage, colonyFinder, stationFinder, shipFinder, currentPlayerUUID) int
    }

    class SupplyChainMutationService {
        -Logger Log
        -PlayerContext _playerContext
        +SupplyChainMutationService(playerContext)
        +Update(uuid, request) ReadOnlySupplyChain
        +Create(request) ReadOnlySupplyChain
        +Delete(uuid) void
    }

    class SupplyChainDeliveryRequest {
        +string SupplyChainUUID
        +int StageSequence
        +string ResourceName
        +string ResourcePurity
        +int ExcessQuantity
        +string DeliveryRouteUUID
        +string SourceLocationUUID
        +DestinationType SourceLocationType
    }

    class ReadOnlySupplyChain {
        +string UUID
        +string Name
        +bool IsActive
    }

    class DestinationType {
        <<enum>>
        Colony
        Station
        Ship
    }

    SupplyChainService --> SupplyChainDeliveryRequest : creates
    SupplyChainDeliveryRequest --> DestinationType : uses
    SupplyChainMutationService --> PlayerContext : uses
    SupplyChainMutationService --> ReadOnlySupplyChain : returns
```
