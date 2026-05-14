# Data Models — Delivery

## RouteStop Changes

```csharp
public enum RouteStopPurpose { Cargo, Refuel, CargoAndRefuel }

public class RouteStop
{
    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType DestinationType { get; set; } = DestinationType.Colony;
    public string DestinationUUID { get; set; } = string.Empty;
    public int Sequence { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [DefaultValue(RouteStopPurpose.Cargo)]
    public RouteStopPurpose Purpose { get; set; } = RouteStopPurpose.Cargo;

    public decimal FuelEstimate { get; set; } = 0m;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string ColonyUUID { get; set; }  // Backward compat
}
```

## DeliveryPlan & DeliveryPlanStop Changes

```csharp
// Add to DeliveryPlan:
public string ShipUUID { get; set; } = string.Empty;  // Optional ship assignment (Iteration 3)

// DeliveryPlanStop gets same DestinationType treatment:
[JsonConverter(typeof(StringEnumConverter))]
public DestinationType DestinationType { get; set; } = DestinationType.Colony;
public string DestinationUUID { get; set; } = string.Empty;
[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
public string ColonyUUID { get; set; }  // Backward compat
```


## Class Diagram

```mermaid
classDiagram
    class DeliveryRoute {
        +string UUID
        +string Name
        +string OwnerUUID
        +List~RouteStop~ Stops
    }

    class RouteStop {
        +DestinationType DestinationType
        +string DestinationUUID
        +int Sequence
        +RouteStopPurpose Purpose
        +decimal FuelEstimate
    }

    class DeliveryPlan {
        +string UUID
        +string Name
        +string OwnerUUID
        +string RouteUUID
        +string ShipUUID
        +bool Completed
        +List~DeliveryPlanStop~ Stops
        +CalculateLoadList() List~DeliveryItem~
    }

    class DeliveryPlanStop {
        +DestinationType DestinationType
        +string DestinationUUID
        +int Sequence
        +bool StopCompleted
        +List~DeliveryItem~ DropOff
        +List~DeliveryItem~ PickUp
    }

    class DeliveryItem {
        +ItemTypeEnum ItemType
        +string BaseItemTypeID
        +string Name
        +string ResourcePurity
        +int Quantity
        +bool Delivered
    }

    class RouteStopPurpose {
        <<enum>>
        Cargo
        Refuel
        CargoAndRefuel
    }

    class DestinationType {
        <<enum>>
        Colony
        Station
        Asteroid
        Ship
    }

    DeliveryRoute *-- RouteStop : Stops
    DeliveryPlan *-- DeliveryPlanStop : Stops
    DeliveryPlanStop *-- DeliveryItem : DropOff
    DeliveryPlanStop *-- DeliveryItem : PickUp
    DeliveryPlan --> DeliveryRoute : RouteUUID
    RouteStop --> DestinationType
    RouteStop --> RouteStopPurpose
    DeliveryPlanStop --> DestinationType
```
