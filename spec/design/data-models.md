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
public enum StationType
{
    Outpost,
    Station,
    Starbase
}

public enum StationOwnership { Government, PlayerOwned }

public class Station
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(StringEnumConverter))]
    public StationType StationType { get; set; } = StationType.Station;

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

PlayerRoot is the serialization container. PlayerContext exposes all entity lists as `IReadOnlyList<T>` properties backed by private `List<T>` fields. All mutations go through dedicated `Add{Entity}`/`Remove{Entity}` methods that maintain UUID caches inline.

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

### PlayerContext List Encapsulation

Each entity list on PlayerContext follows this pattern:

```
private List<T> _entityList;                          // private backing field
public IReadOnlyList<T> EntityList => _entityList;    // read-only public property
public void AddEntity(T item) { ... }                 // controlled mutation + inline cache update
public void RemoveEntity(T item) { ... }              // controlled mutation + inline cache update
```

All 20 PlayerContext lists and all 9 EmpireContext lists follow this pattern. See `spec/design/code-standards.md` for the full pattern specification and mutation method variants.

## Asteroid Surveys (Survey Model Extension)

```csharp
public enum SurveyType { Planet, Asteroid }

// Add to existing Survey class:
[JsonConverter(typeof(StringEnumConverter))]
[DefaultValue(SurveyType.Planet)]
public SurveyType SurveyType { get; set; } = SurveyType.Planet;
public string AsteroidUUID { get; set; } = string.Empty;

// Transient property — carries parsed max reserve data from parser to import helper.
// Not serialized to JSON.
[JsonIgnore]
public Dictionary<string, int> ParsedMaxReserves { get; set; }
```

- Planet and asteroid surveys share the same model. `SurveyType` defaults to Planet for backward compat.
- For asteroid surveys, `Amount` means "rate per mining cycle" (vs "rate per hour" for planet).
- When importing asteroid survey, auto-creates Asteroid entity if not found.
- `ParsedMaxReserves` is populated by `SurveyParser.ProcessHtml` when `ScanDetailOutputMaxReserve` HTML nodes are present. It carries per-resource max reserve values transiently during import — `LinkOrCreateAsteroid` reads it to populate `Asteroid.Reserves`. Not persisted to JSON.
## Filter Criteria

### BlueprintFilterCriteria

`BlueprintFilterCriteria` holds the current filter state for the blueprint list view — text filter, type filter, tech level filter, and evolution range. Used by FormBlueprintV2 to persist and apply list filtering.

## ViewModels

### BlueprintViewModel

`BlueprintViewModel` is the ViewModel for FormBlueprintV2, wrapping a Blueprint model and exposing typed properties for UI binding, computed display values (ExtendedName, property summaries), and edit operations (Save, Delete, Import).

### PricingPlanViewModel

PricingPlanViewModel is the ViewModel for FormPricingPlan, serving as a disconnected edit buffer for PricingPlan entities. Copies all fields from a ReadOnlyPricingPlan into local state, tracks dirty status, and builds PricingPlanUpdateRequest/PricingPlanCreateRequest DTOs for the PricingPlanService. See .kiro/specs/bl-123-pricingplan-readonly/design.md for full design.

## Colony Status Calculation Models

### ColonyStructureStatus

Accumulator for colony-wide resource totals computed by ColonyStatusCalculator. Each field tracks a provided/required pair for a resource category. Not serialized  recomputed on demand.

Fields:
- PowerProvided / PowerRequired (decimal)  total power generation vs demand
- HabitationProvision / HabitationRequired (decimal)  housing units vs worker count
- FoodProvision / FoodRequired (decimal)  food production vs worker count
- EntertainmentProvided / EntertainmentRequired (decimal)  entertainment vs worker demand (2 per worker)
- WarehouseCapacity / WarehouseRequired (decimal)  storage volume vs current usage
- UnallocatedBlueCollarPresent / UnallocatedWhiteCollarPresent / UnallocatedSpecialistPresent (bool)  whether unassigned workers of each type exist in the warehouse

### StructureStatusDelta

Per-structure incremental resource contribution. Used for O(1) single-structure recalculation: subtract old delta, add new delta, instead of recomputing the entire colony.

Fields:
- PowerProvided, PowerRequired, HabitationProvision, FoodProvision, EntertainmentProvided, WarehouseCapacity (decimal)  same categories as ColonyStructureStatus but for one structure
- WorkerCount (int)  assigned workers on this structure
- UnallocatedCount (int)  unallocated workers contributed by this structure

### ColonyWorker

Represents a single worker slot on a colony structure. Used internally by ColonyStatusCalculator to track labor allocation.

Fields:
- Structure (ColonyStructure)  the structure this worker is assigned to
- WorkerType (string)  role key (e.g. "BlueCollar1", "WhiteCollar1", "Specialist1")
- Assigned (bool)  whether this slot is currently active

### ResearchTimeEntry

Maps a blueprint evolution level to its research duration. Serialized in BaselineData.json under the ResearchTime array. Used by ResearchTimeLookup for research timer calculations.

Fields:
- Evolution (int)  the current evolution level (0-14)
- ResearchTimeSeconds (long)  duration in seconds to research from this level to the next

## Read-Only Data Wrappers

Read-only wrapper classes provide controlled access to the data model. Each mutable entity has a corresponding `ReadOnly{Entity}` class that holds a private readonly reference and exposes only getter properties. Wrappers are separate classes (no shared interfaces or base class with the mutable type), preventing consumer code from casting back to the mutable type. See `.kiro/specs/readonly-data-wrappers/` for the full spec.

### Utility Container Wrappers

- **ReadOnlyPropertyBag** — wraps PropertyBag, exposes GetDecimal, GetLong, GetBoolean, GetString, ContainsKey, Count
- **ReadOnlyItemBag** — wraps ItemBag, exposes CountByType, FindByType, FindResource, Count, ContainsKey
- **ReadOnlyLockTracking** — wraps LockTracking, exposes GetLockedQuantity, GetLocksForProcess
- **ReadOnlyCountDownTime** — wraps CountDownTime, exposes TimeRemaining, TimeRemainingString, IntervalsPassed, IsRepeating, RepeatIntervalSeconds, StartTime, EndTime

### Top-Level Entity Wrappers

- **ReadOnlyBlueprint** — wraps Blueprint (Properties as ReadOnlyPropertyBag, Resources as IReadOnlyDictionary)
- **ReadOnlyColony** — wraps Colony (Items as ReadOnlyItemBag, Structures as IReadOnlyList of ReadOnlyColonyStructure, Commodities as IReadOnlyList of ReadOnlyCommodityRequested, Locks as ReadOnlyLockTracking)
- **ReadOnlySurvey** — wraps Survey (Resources as IReadOnlyDictionary of ReadOnlySurveyResource)
- **ReadOnlyPlayerProfile** — wraps PlayerProfile (GetSkill returns ReadOnlyPlayerSkill, ranks as ReadOnlyPlayerRank)
- **ReadOnlyDeliveryRoute** — wraps DeliveryRoute (Stops as IReadOnlyList of ReadOnlyRouteStop)
- **ReadOnlyDeliveryPlan** — wraps DeliveryPlan (Stops as IReadOnlyList of ReadOnlyDeliveryPlanStop)
- **ReadOnlyBuildPlan** — wraps BuildPlan (Items as IReadOnlyList of ReadOnlyBuildItem)
- **ReadOnlyShipTemplate** — wraps ShipTemplate (Components as IReadOnlyList of ReadOnlyShipComponentSlot)
- **ReadOnlyShip** — wraps Ship (Components, Cargo as ReadOnlyItemBag, Hopper as ReadOnlyItemBag)
- **ReadOnlyStation** — wraps Station (Holds as IReadOnlyDictionary of ReadOnlyItemBag, Components, MunitionsHold as ReadOnlyItemBag)
- **ReadOnlyMarketListing** — wraps MarketListing
- **ReadOnlyMarketTransaction** — wraps MarketTransaction
- **ReadOnlyStockPlan** — wraps StockPlan (Targets as IReadOnlyList of ReadOnlyStockTarget)
- **ReadOnlyStockProfile** — wraps StockProfile (Entries as IReadOnlyList of ReadOnlyStockProfileEntry)
- **ReadOnlySupplyChain** — wraps SupplyChain (Stages as IReadOnlyList of ReadOnlySupplyChainStage)
- **ReadOnlyWarehouseOverflowRule** — wraps WarehouseOverflowRule
- **ReadOnlyFaction** — wraps Faction
- **ReadOnlyExternalCharacter** — wraps ExternalCharacter
- **ReadOnlyAsteroid** — wraps Asteroid (Reserves as IReadOnlyList of ReadOnlyAsteroidReserve)
- **ReadOnlyPricingPlan** — wraps PricingPlan (ResourcePrices as IReadOnlyDictionary)
- **ReadOnlyCommodity** — wraps Commodity
- **ReadOnlyResource** — wraps Resource

### Nested Type Wrappers

- **ReadOnlyColonyStructure** — wraps ColonyStructure (Properties/AssignedWorkers as ReadOnlyPropertyBag, timers as nullable ReadOnlyCountDownTime, Statuses as IReadOnlyDictionary of ReadOnlyColonyStructureStatus)
- **ReadOnlyColonyStructureStatus** — wraps ColonyStructureStatus
- **ReadOnlyCommodityRequested** — wraps CommodityRequested
- **ReadOnlyItem** — wraps Item (Contents as nullable ReadOnlyItemBag)
- **ReadOnlySurveyResource** — wraps SurveyResource
- **ReadOnlyPlayerRank** — wraps PlayerRank
- **ReadOnlyPlayerSkill** — wraps PlayerSkill
- **ReadOnlyRouteStop** — wraps RouteStop

### Service DTOs

- **BlueprintUpdateRequest** — DTO carrying the original ReadOnlyBlueprint snapshot and current local field values for updating an existing blueprint through BlueprintService
- **BlueprintCreateRequest** — DTO carrying field values for creating a new blueprint through BlueprintService (no Original snapshot, no UUID)
- **PlayerProfileUpdateRequest** — DTO carrying the original ReadOnlyPlayerProfile snapshot and current local field values for updating an existing profile through PlayerProfileService
- **PlayerProfileCreateRequest** — DTO carrying field values for creating a new player profile through PlayerProfileService (no Original snapshot, no UUID)
- **SkillUpdateData** — DTO carrying a single skill's state (Level, TrainingStarted, CompletionStartTime, CompletionEndTime) for profile create/update requests
- **LocalSkillData** — Local edit buffer copy of a single skill's state in the PlayerProfileViewModel, disconnected from the PlayerSkill entity
- **LocalRankData** — Local edit buffer copy of a single rank track's state in the PlayerProfileViewModel, disconnected from the PlayerRank entity
- **PricingPlanUpdateRequest** — DTO carrying the original ReadOnlyPricingPlan snapshot and current local field values for updating an existing pricing plan through PricingPlanService
- **PricingPlanCreateRequest** — DTO carrying field values for creating a new pricing plan through PricingPlanService (no Original snapshot, no UUID)
- **ColonyUpdateRequest** — DTO carrying the original ReadOnlyColony snapshot and current local field values for updating an existing colony through ColonyService
- **ColonyCreateRequest** — DTO carrying field values for creating a new colony through ColonyService (no Original snapshot, no UUID, no OwnerUUID)
- **ReadOnlyDeliveryPlanStop** — wraps DeliveryPlanStop (DropOff/PickUp as IReadOnlyList of ReadOnlyDeliveryItem)
- **ReadOnlyDeliveryItem** — wraps DeliveryItem
- **ReadOnlyBuildItem** — wraps BuildItem
- **ReadOnlyShipComponentSlot** — wraps ShipComponentSlot
- **ReadOnlyStockTarget** — wraps StockTarget
- **ReadOnlyStockProfileEntry** — wraps StockProfileEntry
- **ReadOnlyAsteroidReserve** — wraps AsteroidReserve
- **ReadOnlySupplyChainStage** — wraps SupplyChainStage
