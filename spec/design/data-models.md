<!-- Extracted from .kiro/specs/empire-systems/design.md — Data Models section -->
# Data Models

All new models follow the existing POCO pattern: public properties with defaults, Newtonsoft.Json serialization, UUID + OwnerUUID ownership, persisted as top-level arrays in PlayerRoot.

## Shared Enums

These enums are used across multiple models and are defined first to avoid forward references.

```csharp
public enum DestinationType
{
    Colony,
    Station,
    Asteroid,
    Ship        // Future: factory ships for manufacturing/refining/research in space
}
```

## BuildPlan

```csharp
public class BuildPlan
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DeliveryPlanUUID { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<BuildItem> Items { get; set; } = new List<BuildItem>();
}
```

- Items nested inside plan (not separate top-level array) — deleting a plan deletes all items automatically.
- DeliveryPlanUUID links to generated delivery plan. Empty = no delivery plan yet.

## BuildItem

```csharp
public enum BuildItemType
{
    Manufactory,
    Commodity,
    ShipTemplate,    // Expands into component Manufactory items
    Mining,          // Iteration 6
    Refining,        // Iteration 6
    Research         // Iteration 6
}

public enum BuildItemStatus
{
    Staged,
    Delivering,
    Ready,
    InProgress,
    Completed
}

public class BuildItem
{
    public string UUID { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BuildItemType ItemType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BuildItemStatus Status { get; set; } = BuildItemStatus.Staged;

    // What to build
    public string BlueprintUUID { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string ShipTemplateUUID { get; set; } = string.Empty;

    // How many
    public int Quantity { get; set; } = 0;  // Always runs

    // Where to build
    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType BuildLocationType { get; set; } = DestinationType.Colony;
    public string BuildLocationUUID { get; set; } = string.Empty;
    public string StructureUUID { get; set; } = string.Empty;

    // Assembly location (ShipTemplate items)
    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType AssemblyLocationType { get; set; } = DestinationType.Station;
    public string AssemblyLocationUUID { get; set; } = string.Empty;

    // Parent-child relationship
    public string ParentBuildItemUUID { get; set; } = string.Empty;

    // Metadata
    public string Recipient { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int SequenceInStructure { get; set; } = 0;
    public string DependsOnUUID { get; set; } = string.Empty;

    // Mining/Refining (Iteration 6)
    public string MiningResource { get; set; } = string.Empty;
    public string MiningSurveyUUID { get; set; } = string.Empty;
    public string RefiningResource { get; set; } = string.Empty;
    public string RefiningPurity { get; set; } = string.Empty;
}
```

- Iteration 6 fields present from start with empty defaults. `DefaultValueHandling.Ignore` omits from JSON until used.
- Quantity is always runs. Service layer computes total output.
- BuildLocationType future-proofs for factory ships.
- Status is string enum for readable JSON.
## ShipTemplate

```csharp
public class ShipTemplate
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string HullBlueprintUUID { get; set; } = string.Empty;
    public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();
}

public class ShipComponentSlot
{
    public string SlotType { get; set; } = string.Empty;
    public int SlotIndex { get; set; } = 0;
    public string BlueprintUUID { get; set; } = string.Empty;

    // Damage state (Ship and Station instances — ignored on ShipTemplate)
    public int CurrentHP { get; set; } = 0;
    public int MaxHP { get; set; } = 0;
    public decimal MaxRepairPercent { get; set; } = 0m;
}
```

- SlotType is string (game may add new slot types). Hull blueprint defines valid types/counts.
- Damage fields default to 0 ("undamaged"), omitted from JSON via `DefaultValueHandling.Ignore`.

## Ship

```csharp
public class Ship
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string TemplateUUID { get; set; } = string.Empty;
    public string HullBlueprintUUID { get; set; } = string.Empty;
    public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();

    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType LocationType { get; set; } = DestinationType.Station;
    public string LocationUUID { get; set; } = string.Empty;

    public ItemBag Cargo { get; set; } = new ItemBag();
    public ItemBag Hopper { get; set; } = new ItemBag();

    // Hull damage state
    public int HullCurrentHP { get; set; } = 0;
    public int HullMaxHP { get; set; } = 0;
    public decimal HullMaxRepairPercent { get; set; } = 0m;
}
```

- Ship duplicates hull/components from template (independent entity — template can change without affecting ship).
- Hopper: separate ItemBag for unrefined resources (High/Medium/Low purity only). Capacity from hull `Raw Material Capacity` + sum(Ore Hopper `Raw Material Capacity`).
- Hull damage: same three-field pattern as components. All default to 0 (undamaged, omitted from JSON).

## Station

```csharp
public enum StationOwnership { Government, Player }

public class Station
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;

    [JsonConverter(typeof(StringEnumConverter))]
    public StationOwnership Ownership { get; set; } = StationOwnership.Government;

    public string OwnerUUID { get; set; } = string.Empty;
    public Dictionary<string, ItemBag> Holds { get; set; } = new Dictionary<string, ItemBag>();
    public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();
    public string StationBlueprintUUID { get; set; } = string.Empty;
    public ItemBag MunitionsHold { get; set; } = new ItemBag();

    public int HullCurrentHP { get; set; } = 0;
    public int HullMaxHP { get; set; } = 0;
    public decimal HullMaxRepairPercent { get; set; } = 0m;
}
```

- UUID deterministic from station name. Holds keyed by player UUID. Missing key = empty hold.
- Player-owned stations reuse ShipComponentSlot for components.

## Asteroid

```csharp
public class Asteroid
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public List<AsteroidReserve> Reserves { get; set; } = new List<AsteroidReserve>();
}

public class AsteroidReserve
{
    public string ResourceName { get; set; } = string.Empty;
    public string Purity { get; set; } = string.Empty;
    public int MaxReserve { get; set; } = 0;
    public int CurrentReserve { get; set; } = 0;
    public string ResetTimestamp { get; set; } = string.Empty;
}
```

- UUID deterministic from "SystemName:AsteroidName". Reserves are shared pool across all players.

## MarketListing

```csharp
public class MarketListing
{
    public string UUID { get; set; }
    public string OwnerUUID { get; set; } = string.Empty;
    public string StationUUID { get; set; } = string.Empty;

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
    public string ItemReferenceID { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 0;
    public decimal PricePerUnit { get; set; } = 0m;

    public int CurrentHP { get; set; } = 0;
    public int MaxHP { get; set; } = 0;
    public decimal MaxRepairPercent { get; set; } = 0m;
}
```

## MarketTransaction

```csharp
public enum TransactionType { Buy, Sell }

public class MarketTransaction
{
    public string UUID { get; set; }
    public string OwnerUUID { get; set; } = string.Empty;

    [JsonConverter(typeof(StringEnumConverter))]
    public TransactionType TransactionType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
    public string ItemReferenceID { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 0;
    public decimal PricePerUnit { get; set; } = 0m;
    public decimal TotalPrice { get; set; } = 0m;
    public string Counterparty { get; set; } = string.Empty;
    public string CounterpartyFaction { get; set; } = string.Empty;
    public string StationUUID { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ListingUUID { get; set; } = string.Empty;

    public int CurrentHP { get; set; } = 0;
    public int MaxHP { get; set; } = 0;
    public decimal MaxRepairPercent { get; set; } = 0m;
}
```

- CounterpartyFaction is a snapshot (people change factions).
- Condition fields are snapshots preserved even after listing deletion.
## StockPlan & StockTarget

```csharp
public enum StockTargetScope { EmpireWide, Colony, Station }

public class StockPlan
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string ReplenishmentBuildPlanUUID { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<StockTarget> Targets { get; set; } = new List<StockTarget>();
}

public class StockTarget
{
    public string UUID { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
    public string ItemReferenceID { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ShipTemplateUUID { get; set; } = string.Empty;

    public int TargetQuantity { get; set; } = 0;
    public int CriticalThreshold { get; set; } = 0;

    [JsonConverter(typeof(StringEnumConverter))]
    public StockTargetScope Scope { get; set; } = StockTargetScope.EmpireWide;
    public string LocationUUID { get; set; } = string.Empty;
}
```

## Faction & ExternalCharacter

```csharp
public class Faction
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ExternalCharacter
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
}
```

- Factions are shared (no OwnerUUID). UUID deterministic from name.
- ExternalCharacters are shared. UUID deterministic from name.

## Item Changes (Crate Support)

```csharp
// Add to existing ItemType.ItemTypeEnum:
Crate   // A container that holds other items

// Add to existing Item class:
public ItemBag Contents { get; set; }  // Non-null for Crate items, null for everything else
```

- No nesting: validation prevents adding a Crate to another Crate's Contents.
- Crates have no inherent volume/mass — purely organizational.

## Item Changes (Component Damage)

```csharp
// Add to existing Item class:
public int CurrentHP { get; set; } = 0;
public int MaxHP { get; set; } = 0;
public decimal MaxRepairPercent { get; set; } = 0m;
```

- Same three-field pattern as ShipComponentSlot. Damage transfers between inventory and ship slots.

## PlayerProfile Changes

```csharp
// Add to existing PlayerProfile:
public string FactionUUID { get; set; } = string.Empty;
```

## SupplyChain

```csharp
public class SupplyChain
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<SupplyChainStage> Stages { get; set; } = new List<SupplyChainStage>();
}

public enum SupplyChainStageType
{
    Mine, AsteroidMine, PickUp, Refine, Deliver, Research
}

public class SupplyChainStage
{
    public int Sequence { get; set; } = 0;

    [JsonConverter(typeof(StringEnumConverter))]
    public SupplyChainStageType StageType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType LocationType { get; set; }
    public string LocationUUID { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;
    public string ResourcePurity { get; set; } = string.Empty;

    public int AccumulationThreshold { get; set; } = 0;
    public decimal ProductionRatePerHour { get; set; } = 0m;
    public string DeliveryRouteUUID { get; set; } = string.Empty;
}
```

## StockProfile

```csharp
public class StockProfile
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<StockProfileEntry> Entries { get; set; } = new List<StockProfileEntry>();
}

public class StockProfileEntry
{
    public string GroupID { get; set; } = string.Empty;
    public string StockPlanUUID { get; set; } = string.Empty;
}
```

- Same GroupID = ORed (max). Different GroupIDs = ANDed (summed).

## WarehouseOverflowRule

```csharp
public class WarehouseOverflowRule
{
    public string UUID { get; set; }
    public string OwnerUUID { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ColonyUUID { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ResourcePurity { get; set; } = string.Empty;
    public int TriggerThreshold { get; set; } = 0;

    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType DestinationType { get; set; } = DestinationType.Station;
    public string DestinationUUID { get; set; } = string.Empty;
    public string DeliveryRouteUUID { get; set; } = string.Empty;
}
```

## IsActive Pattern

Entities with `IsActive` (default `true`): BuildPlan, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule. All service methods filter to `IsActive == true`. UI shows Active/Inactive toggle. Toggling is immediate write-through.

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

## PlayerRoot Changes

```csharp
public class PlayerRoot
{
    // Existing
    public int DataVersion { get; set; }
    public string CurrentPlayerUUID { get; set; }
    public PlayerProfile[] PlayerProfile { get; set; }
    public Blueprint[] Blueprint { get; set; }
    public Survey[] Survey { get; set; }
    public Colony[] Colony { get; set; }
    public DeliveryRoute[] DeliveryRoute { get; set; }
    public DeliveryPlan[] DeliveryPlan { get; set; }
    public PricingPlan[] PricingPlan { get; set; }

    // New — all default to empty arrays
    public BuildPlan[] BuildPlan { get; set; }
    public ShipTemplate[] ShipTemplate { get; set; }
    public Ship[] Ship { get; set; }
    public Station[] Station { get; set; }
    public MarketListing[] MarketListing { get; set; }
    public MarketTransaction[] MarketTransaction { get; set; }
    public StockPlan[] StockPlan { get; set; }
    public StockProfile[] StockProfile { get; set; }
    public SupplyChain[] SupplyChain { get; set; }
    public WarehouseOverflowRule[] WarehouseOverflowRule { get; set; }
    public Faction[] Faction { get; set; }
    public ExternalCharacter[] ExternalCharacter { get; set; }
    public Asteroid[] Asteroid { get; set; }
}
```

## Asteroid Surveys (Survey Model Extension)

```csharp
public enum SurveyType { Planet, Asteroid }

// Add to existing Survey class:
[JsonConverter(typeof(StringEnumConverter))]
[DefaultValue(SurveyType.Planet)]
public SurveyType SurveyType { get; set; } = SurveyType.Planet;
public string AsteroidUUID { get; set; } = string.Empty;
```

- Planet and asteroid surveys share the same model. `SurveyType` defaults to Planet for backward compat.
- For asteroid surveys, `Amount` means "rate per mining cycle" (vs "rate per hour" for planet).
- When importing asteroid survey, auto-creates Asteroid entity if not found.