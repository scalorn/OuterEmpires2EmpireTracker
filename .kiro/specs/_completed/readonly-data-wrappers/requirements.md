# Requirements Document

## Introduction

This feature adds read-only wrapper types to the entire data model. Each mutable entity gets a corresponding `ReadOnly{Entity}` class that holds a private reference to the mutable entity and exposes only getter properties. The wrappers are separate classes (not interfaces on the mutable type), so consumer code cannot cast back to the mutable type. This provides controlled mutation paths ahead of game API integration and prevents accidental mutation by read-only consumers such as filtered combo boxes, list views, read-only grids, and reference counters.

## Glossary

- **Wrapper**: A `ReadOnly{Entity}` class that holds a private reference to a mutable entity and exposes only read-only access to its data.
- **Mutable_Entity**: The existing POCO model class (e.g. `Blueprint`, `Colony`) with public getters and setters.
- **Read_Only_Consumer**: Code that displays or queries entity data without modifying it — combo boxes, list views, read-only grids, reference counters, status calculators.
- **Mutable_Consumer**: Code that modifies entity data — ViewModels, parsers, background processor, import helpers.
- **PlayerContext**: Singleton service managing player-specific entity lists and persistence.
- **EmpireContext**: Singleton service managing shared game reference data (commodities, resources, blueprint types).
- **Nested_Type**: A child object owned by a parent entity (e.g. `ColonyStructure` within `Colony`, `RouteStop` within `DeliveryRoute`).
- **ReadOnlyPropertyBag**: Read-only wrapper for PropertyBag exposing only Get methods and Count.
- **ReadOnlyItemBag**: Read-only wrapper for ItemBag exposing only query methods (CountByType, FindByType, FindResource, Count, ContainsKey).
- **ReadOnlyLockTracking**: Read-only wrapper for LockTracking exposing only query methods (GetLockedQuantity, GetLocksForProcess).
- **ReadOnlyCountDownTime**: Read-only wrapper for CountDownTime exposing only computed read properties (TimeRemaining, TimeRemainingString, IntervalsPassed, IsRepeating, RepeatIntervalSeconds).
- **Gen0_Allocation**: A short-lived .NET object allocated on the Gen0 heap, collected cheaply by the garbage collector without promotion to Gen1/Gen2.

## Requirements

### Requirement 1: Wrapper Class Pattern

**User Story:** As a developer, I want each entity to have a separate read-only wrapper class, so that read-only consumers cannot accidentally mutate entity data.

#### Acceptance Criteria

1. THE Wrapper SHALL be a separate class named `ReadOnly{Entity}` in the `OE2EmpireTracker.Models` namespace.
2. THE Wrapper SHALL hold a single private readonly field referencing the Mutable_Entity instance.
3. THE Wrapper SHALL expose a public constructor accepting the Mutable_Entity instance.
4. THE Wrapper SHALL NOT inherit from, implement an interface shared with, or be castable to the Mutable_Entity type.
5. THE Wrapper SHALL expose only getter properties corresponding to each public property on the Mutable_Entity.
6. THE Wrapper SHALL NOT expose any methods or properties that allow mutation of the wrapped entity's state.
7. WHEN a Mutable_Entity property is a value type or string, THE Wrapper SHALL expose it as a read-only property returning the same value.
8. WHEN a Mutable_Entity property is a Nested_Type, THE Wrapper SHALL expose it as the corresponding `ReadOnly{NestedType}` wrapper.
9. WHEN a Mutable_Entity property is a `List<T>` of Nested_Types, THE Wrapper SHALL expose it as `IReadOnlyList<ReadOnly{T}>`.
10. WHEN a Mutable_Entity property is a `Dictionary<TKey, TValue>` where TValue is a value type or string, THE Wrapper SHALL expose it as `IReadOnlyDictionary<TKey, TValue>`.
11. WHEN a Mutable_Entity property is a `Dictionary<string, ItemBag>` (e.g. Station.Holds), THE Wrapper SHALL expose it as `IReadOnlyDictionary<string, ReadOnlyItemBag>`.
12. THE Wrapper SHALL NOT be decorated with any JSON serialization attributes. Wrappers are transient in-memory objects and SHALL NOT be serialized.

### Requirement 2: No Casting Path

**User Story:** As a developer, I want the read-only wrapper to be a completely separate type from the mutable entity, so that no code can bypass the read-only protection via casting.

#### Acceptance Criteria

1. THE Wrapper SHALL NOT implement any interface that the Mutable_Entity also implements.
2. THE Wrapper SHALL NOT inherit from the Mutable_Entity class or any shared base class (other than `object`).
3. THE Wrapper SHALL NOT expose the wrapped Mutable_Entity reference through any public or internal property, method, or field.
4. THE Wrapper SHALL NOT provide any implicit or explicit conversion operator to or from the Mutable_Entity type.

### Requirement 3: Lightweight Allocation

**User Story:** As a developer, I want wrappers to be thin Gen0 allocations, so that creating them during form population has negligible performance impact.

#### Acceptance Criteria

1. THE Wrapper SHALL contain exactly one instance field: the private readonly reference to the Mutable_Entity.
2. THE Wrapper SHALL NOT cache, copy, or pre-compute any data from the Mutable_Entity at construction time.
3. THE Wrapper SHALL delegate all property access to the wrapped Mutable_Entity at call time (live read-through).
4. THE Wrapper SHALL NOT implement object pooling, caching, or reuse mechanisms.

### Requirement 4: Computed and JsonIgnore Properties

**User Story:** As a developer, I want read-only wrappers to expose computed properties (ExtendedName, OutputItemName, IsBuiltAndOnline, TimeRemainingString), so that read-only consumers can display derived values.

#### Acceptance Criteria

1. WHEN the Mutable_Entity has a `[JsonIgnore]` computed property, THE Wrapper SHALL expose a corresponding read-only property that delegates to the Mutable_Entity's computed property.
2. THE ReadOnlyBlueprint SHALL expose ExtendedName and OutputItemName as read-only properties.
3. THE ReadOnlySurvey SHALL expose ExtendedName as a read-only property.
4. THE ReadOnlyColonyStructure SHALL expose IsBuiltAndOnline as a read-only property.
5. THE ReadOnlyCountDownTime SHALL expose TimeRemaining, TimeRemainingString, IntervalsPassed, and IsRepeating as read-only properties.
6. THE ReadOnlyItem SHALL expose ExtendedName as a read-only property.

### Requirement 5: Utility Container Wrappers (PropertyBag, ItemBag, LockTracking, CountDownTime)

**User Story:** As a developer, I want read-only wrappers for the shared utility containers, so that nested data within entities is also protected from accidental mutation.

#### Acceptance Criteria

1. THE ReadOnlyPropertyBag SHALL expose GetDecimal, GetLong, GetBoolean, GetString, ContainsKey, and Count — all delegating to the wrapped PropertyBag.
2. THE ReadOnlyPropertyBag SHALL NOT expose SetProperty, Remove, Clear, or the Properties dictionary.
3. THE ReadOnlyItemBag SHALL expose CountByType, FindByType, FindResource, Count, and ContainsKey — all delegating to the wrapped ItemBag.
4. THE ReadOnlyItemBag SHALL NOT expose AddItem, Remove, Clear, or the Items dictionary.
5. THE ReadOnlyLockTracking SHALL expose GetLockedQuantity and GetLocksForProcess — both delegating to the wrapped LockTracking.
6. THE ReadOnlyLockTracking SHALL NOT expose LockItem, LockItems, ClearLocksForProcess, or RawLocks.
7. THE ReadOnlyCountDownTime SHALL expose TimeRemaining (getter only), TimeRemainingString (getter only), IntervalsPassed, IsRepeating, RepeatIntervalSeconds, StartTime, and EndTime as read-only properties.
8. THE ReadOnlyCountDownTime SHALL NOT expose TimeRemaining setter, TimeRemainingString setter, ConsumeIntervals, StartRepeating, or any other mutation method.
9. WHEN a ReadOnlyItemBag.FindByType or FindResource is called, THE ReadOnlyItemBag SHALL return `IReadOnlyList<ReadOnlyItem>` wrapping the results.

### Requirement 6: Top-Level Entity Wrappers

**User Story:** As a developer, I want every entity type in the data model to have a read-only wrapper, so that the entire model is covered before API integration.

#### Acceptance Criteria

1. THE System SHALL provide ReadOnlyBlueprint wrapping Blueprint, exposing: UUID, Name, OwnerUUID, BaseBlueprintUUID, LegacyUUID, BluePrintType, Evolution, TechLevel, Class, CopyCost, NickName, Description, ExtendedName, OutputItemName, Properties (as ReadOnlyPropertyBag), Resources (as IReadOnlyDictionary).
2. THE System SHALL provide ReadOnlyColony wrapping Colony, exposing: UUID, OwnerUUID, LegacyUUID, PlanetName, SystemName, ColonyName, LastImportDateTime, Items (as ReadOnlyItemBag), Structures (as IReadOnlyList of ReadOnlyColonyStructure), Commodities (as IReadOnlyList of ReadOnlyCommodityRequested), Locks (as ReadOnlyLockTracking).
3. THE System SHALL provide ReadOnlyColonyStructure wrapping ColonyStructure, exposing all public properties as read-only, with Properties and AssignedWorkers as ReadOnlyPropertyBag, BuildCompletionTime and ProcessCompletionTime as ReadOnlyCountDownTime (or null), Statuses as IReadOnlyDictionary, and IsBuiltAndOnline.
4. THE System SHALL provide ReadOnlySurvey wrapping Survey, exposing: UUID, Name, OwnerUUID, PlanetName, SystemName, SurveyID, NickName, SurveyType, AsteroidUUID, ExtendedName, Resources (as IReadOnlyDictionary of string to ReadOnlySurveyResource).
5. THE System SHALL provide ReadOnlySurveyResource wrapping SurveyResource, exposing: Resource, Purity, Amount, ExtendedName.
6. THE System SHALL provide ReadOnlyPlayerProfile wrapping PlayerProfile, exposing: UUID, Name, FactionUUID, and read-only access to skills (GetSkill returning read-only data) and skill groups (GetSkillGroup).
7. THE System SHALL provide ReadOnlyPlayerRank wrapping PlayerRank, exposing: Rank, CurrentXP, NextXP.
8. THE System SHALL provide ReadOnlyPlayerSkill wrapping PlayerSkill, exposing: Level, CurrentXP, NextXP.
9. THE System SHALL provide ReadOnlyDeliveryRoute wrapping DeliveryRoute, exposing: UUID, Name, OwnerUUID, Stops (as IReadOnlyList of ReadOnlyRouteStop).
10. THE System SHALL provide ReadOnlyRouteStop wrapping RouteStop, exposing: ColonyUUID, Sequence, DestinationType, DestinationUUID, Purpose, FuelEstimate.
11. THE System SHALL provide ReadOnlyDeliveryPlan wrapping DeliveryPlan, exposing all public properties as read-only, with Stops as IReadOnlyList of ReadOnlyDeliveryPlanStop.
12. THE System SHALL provide ReadOnlyBuildPlan wrapping BuildPlan, exposing: UUID, Name, OwnerUUID, Description, DeliveryPlanUUID, IsActive, Items (as IReadOnlyList of ReadOnlyBuildItem).
13. THE System SHALL provide ReadOnlyBuildItem wrapping BuildItem, exposing all public properties as read-only.
14. THE System SHALL provide ReadOnlyShipTemplate wrapping ShipTemplate, exposing: UUID, Name, OwnerUUID, HullBlueprintUUID, Components (as IReadOnlyList of ReadOnlyShipComponentSlot).
15. THE System SHALL provide ReadOnlyShipComponentSlot wrapping ShipComponentSlot, exposing: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent.
16. THE System SHALL provide ReadOnlyShip wrapping Ship, exposing: UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID, Components (as IReadOnlyList of ReadOnlyShipComponentSlot), LocationType, LocationUUID, Cargo (as ReadOnlyItemBag), Hopper (as ReadOnlyItemBag), HullCurrentHP, HullMaxHP, HullMaxRepairPercent.
17. THE System SHALL provide ReadOnlyStation wrapping Station, exposing: UUID, Name, StationType, Ownership, OwnerUUID, Holds (as IReadOnlyDictionary of string to ReadOnlyItemBag), Components (as IReadOnlyList of ReadOnlyShipComponentSlot), StationBlueprintUUID, MunitionsHold (as ReadOnlyItemBag), HullCurrentHP, HullMaxHP, HullMaxRepairPercent.
18. THE System SHALL provide ReadOnlyMarketListing wrapping MarketListing, exposing all public properties as read-only.
19. THE System SHALL provide ReadOnlyMarketTransaction wrapping MarketTransaction, exposing all public properties as read-only.
20. THE System SHALL provide ReadOnlyStockPlan wrapping StockPlan, exposing: UUID, Name, OwnerUUID, ReplenishmentBuildPlanUUID, IsActive, Targets (as IReadOnlyList of ReadOnlyStockTarget).
21. THE System SHALL provide ReadOnlyStockTarget wrapping StockTarget, exposing all public properties as read-only.
22. THE System SHALL provide ReadOnlyStockProfile wrapping StockProfile, exposing: UUID, Name, OwnerUUID, IsActive, Entries (as IReadOnlyList of ReadOnlyStockProfileEntry).
23. THE System SHALL provide ReadOnlyStockProfileEntry wrapping StockProfileEntry, exposing: GroupID, StockPlanUUID.
24. THE System SHALL provide ReadOnlySupplyChain wrapping SupplyChain, exposing: UUID, Name, OwnerUUID, IsActive, Stages (as IReadOnlyList of ReadOnlySupplyChainStage).
25. THE System SHALL provide ReadOnlySupplyChainStage wrapping SupplyChainStage, exposing all public properties as read-only.
26. THE System SHALL provide ReadOnlyWarehouseOverflowRule wrapping WarehouseOverflowRule, exposing all public properties as read-only.
27. THE System SHALL provide ReadOnlyFaction wrapping Faction, exposing: UUID, Name, Description.
28. THE System SHALL provide ReadOnlyExternalCharacter wrapping ExternalCharacter, exposing: UUID, Name, FactionUUID.
29. THE System SHALL provide ReadOnlyAsteroid wrapping Asteroid, exposing: UUID, Name, SystemName, Reserves (as IReadOnlyList of ReadOnlyAsteroidReserve).
30. THE System SHALL provide ReadOnlyAsteroidReserve wrapping AsteroidReserve, exposing: ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp.
31. THE System SHALL provide ReadOnlyPricingPlan wrapping PricingPlan, exposing: UUID, Name, OwnerUUID, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices (as IReadOnlyDictionary of string to decimal).
32. THE System SHALL provide ReadOnlyItem wrapping Item, exposing all public properties as read-only, with Contents (as ReadOnlyItemBag when non-null, null otherwise) and ExtendedName.
33. THE System SHALL provide ReadOnlyCommodity wrapping Commodity, exposing: Name, ExtendedName, Industry, Group, and ConstructionResources (as IReadOnlyDictionary).
34. THE System SHALL provide ReadOnlyResource wrapping Resource, exposing: Name, Group, Synthetic.
35. THE System SHALL provide ReadOnlyCommodityRequested wrapping CommodityRequested, exposing all public properties as read-only.
36. THE System SHALL provide ReadOnlyColonyStructureStatus wrapping ColonyStructureStatus, exposing all public properties as read-only.

### Requirement 7: PlayerContext Read-Only List Exposure

**User Story:** As a developer, I want PlayerContext to expose read-only wrapper lists, so that read-only consumers receive wrapped entities instead of mutable ones.

#### Acceptance Criteria

1. THE PlayerContext SHALL expose a `GetReadOnlyBlueprintList()` method returning `IReadOnlyList<ReadOnlyBlueprint>`.
2. THE PlayerContext SHALL expose a `GetReadOnlyColonyList()` method returning `IReadOnlyList<ReadOnlyColony>`.
3. THE PlayerContext SHALL expose a `GetReadOnlySurveyList()` method returning `IReadOnlyList<ReadOnlySurvey>`.
4. THE PlayerContext SHALL expose a `GetReadOnlyPlayerProfileList()` method returning `IReadOnlyList<ReadOnlyPlayerProfile>`.
5. THE PlayerContext SHALL expose a `GetReadOnlyDeliveryRouteList()` method returning `IReadOnlyList<ReadOnlyDeliveryRoute>`.
6. THE PlayerContext SHALL expose a `GetReadOnlyDeliveryPlanList()` method returning `IReadOnlyList<ReadOnlyDeliveryPlan>`.
7. THE PlayerContext SHALL expose a `GetReadOnlyPricingPlanList()` method returning `IReadOnlyList<ReadOnlyPricingPlan>`.
8. THE PlayerContext SHALL expose a `GetReadOnlyBuildPlanList()` method returning `IReadOnlyList<ReadOnlyBuildPlan>`.
9. THE PlayerContext SHALL expose a `GetReadOnlyShipTemplateList()` method returning `IReadOnlyList<ReadOnlyShipTemplate>`.
10. THE PlayerContext SHALL expose a `GetReadOnlyShipList()` method returning `IReadOnlyList<ReadOnlyShip>`.
11. THE PlayerContext SHALL expose a `GetReadOnlyStationList()` method returning `IReadOnlyList<ReadOnlyStation>`.
12. THE PlayerContext SHALL expose a `GetReadOnlyMarketListingList()` method returning `IReadOnlyList<ReadOnlyMarketListing>`.
13. THE PlayerContext SHALL expose a `GetReadOnlyMarketTransactionList()` method returning `IReadOnlyList<ReadOnlyMarketTransaction>`.
14. THE PlayerContext SHALL expose a `GetReadOnlyStockPlanList()` method returning `IReadOnlyList<ReadOnlyStockPlan>`.
15. THE PlayerContext SHALL expose a `GetReadOnlyStockProfileList()` method returning `IReadOnlyList<ReadOnlyStockProfile>`.
16. THE PlayerContext SHALL expose a `GetReadOnlySupplyChainList()` method returning `IReadOnlyList<ReadOnlySupplyChain>`.
17. THE PlayerContext SHALL expose a `GetReadOnlyWarehouseOverflowRuleList()` method returning `IReadOnlyList<ReadOnlyWarehouseOverflowRule>`.
18. THE PlayerContext SHALL expose a `GetReadOnlyFactionList()` method returning `IReadOnlyList<ReadOnlyFaction>`.
19. THE PlayerContext SHALL expose a `GetReadOnlyExternalCharacterList()` method returning `IReadOnlyList<ReadOnlyExternalCharacter>`.
20. THE PlayerContext SHALL expose a `GetReadOnlyAsteroidList()` method returning `IReadOnlyList<ReadOnlyAsteroid>`.
21. WHEN a GetReadOnly list method is called, THE PlayerContext SHALL create wrapper instances on the fly by iterating the backing list under `_listLock` and wrapping each element.
22. THE PlayerContext SHALL expose `FindReadOnlyBlueprint(string id)` returning `ReadOnlyBlueprint` (or null), delegating to FindBlueprint and wrapping the result.
23. THE PlayerContext SHALL expose `FindReadOnly{Entity}(string id)` methods for all entity types that have existing Find methods, each returning the corresponding read-only wrapper or null.

### Requirement 8: EmpireContext Read-Only List Exposure

**User Story:** As a developer, I want EmpireContext to expose read-only wrapper lists for its reference data, so that consumers of shared game data also receive read-only wrappers.

#### Acceptance Criteria

1. THE EmpireContext SHALL expose a `GetReadOnlyCommodityList()` method returning `IReadOnlyList<ReadOnlyCommodity>`.
2. THE EmpireContext SHALL expose a `GetReadOnlyResourceList()` method returning `IReadOnlyList<ReadOnlyResource>`.
3. THE EmpireContext SHALL expose a `GetReadOnlyGlobalBlueprintList()` method returning `IReadOnlyList<ReadOnlyBlueprint>`.
4. THE EmpireContext SHALL expose `FindReadOnlyCommodity(string name)` returning `ReadOnlyCommodity` or null.
5. THE EmpireContext SHALL expose `FindReadOnlyGlobalBlueprint(string id)` returning `ReadOnlyBlueprint` or null.
6. WHEN a GetReadOnly list method is called, THE EmpireContext SHALL create wrapper instances on the fly from the backing list.

### Requirement 9: Current-Player Filtered Read-Only Lists

**User Story:** As a developer, I want current-player filtered lists to also be available as read-only wrappers, so that forms populating combo boxes and list views for the current player receive read-only data.

#### Acceptance Criteria

1. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyColonies()` returning `List<ReadOnlyColony>` filtered to the current player.
2. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyBlueprints()` returning `List<ReadOnlyBlueprint>` filtered to the current player.
3. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlySurveys()` returning `List<ReadOnlySurvey>` filtered to the current player.
4. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyRoutes()` returning `List<ReadOnlyDeliveryRoute>` filtered to the current player.
5. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyPlans()` returning `List<ReadOnlyDeliveryPlan>` filtered to the current player.
6. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyPricingPlans()` returning `List<ReadOnlyPricingPlan>` filtered to the current player.
7. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyBuildPlans()` returning `List<ReadOnlyBuildPlan>` filtered to the current player.
8. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyShipTemplates()` returning `List<ReadOnlyShipTemplate>` filtered to the current player.
9. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyShips()` returning `List<ReadOnlyShip>` filtered to the current player.
10. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyStations()` returning `List<ReadOnlyStation>` filtered to the current player.
11. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyListings()` returning `List<ReadOnlyMarketListing>` filtered to the current player.
12. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyTransactions()` returning `List<ReadOnlyMarketTransaction>` filtered to the current player.
13. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyStockPlans()` returning `List<ReadOnlyStockPlan>` filtered to the current player.
14. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyStockProfiles()` returning `List<ReadOnlyStockProfile>` filtered to the current player.
15. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlySupplyChains()` returning `List<ReadOnlySupplyChain>` filtered to the current player.
16. THE PlayerContext SHALL expose `GetCurrentPlayerReadOnlyOverflowRules()` returning `List<ReadOnlyWarehouseOverflowRule>` filtered to the current player.
17. THE PlayerContext SHALL expose `GetAllReadOnlyBlueprints()` returning `List<ReadOnlyBlueprint>` combining player and global blueprints.

### Requirement 10: Mutable Path Unchanged

**User Story:** As a developer, I want the existing mutable access path to remain unchanged, so that ViewModels, parsers, and the background processor continue to work without modification.

#### Acceptance Criteria

1. THE existing `IReadOnlyList<T>` properties on PlayerContext (BlueprintList, ColonyList, etc.) SHALL continue to return mutable entity references as they do today.
2. THE existing `Add{Entity}` and `Remove{Entity}` mutation methods on PlayerContext SHALL remain unchanged.
3. THE existing `Find{Entity}` methods on PlayerContext SHALL continue to return mutable entity references.
4. THE existing ViewModel classes SHALL continue to receive mutable entities for write-through editing.
5. THE existing parser classes SHALL continue to create and populate mutable entities.
6. THE BackgroundProcessor SHALL continue to receive mutable Colony references for ProcessColony.
7. THE existing `GetCurrentPlayer{Entity}` methods SHALL continue to return `List<T>` of mutable entities.

### Requirement 11: Read-Only Consumer Migration

**User Story:** As a developer, I want to identify which consumers should switch to read-only wrappers, so that the migration is planned and traceable.

#### Acceptance Criteria

1. THE following consumer categories SHALL switch to read-only wrappers:
   - Filtered combo boxes (FilteredTextComboSet, DataGridViewFilteredComboBoxColumn) that populate dropdown lists from entity collections
   - List views (left-panel entity lists in forms) that display entity names and properties for selection
   - Read-only grids (DataGridView in read-only mode) that display entity data without editing
   - Reference counters that count cross-entity references for delete-guard logic
   - Status calculators (ColonyStatusCalculator) that read colony structure data for status computation
   - Route dropdown helpers (RouteDropdownHelper) that populate route/destination combo boxes
2. THE following consumer categories SHALL continue to use mutable entities:
   - ViewModel classes that perform write-through editing
   - Parser classes (ColonyParser, SurveyParser, BlueprintScanner) that create and populate entities
   - BackgroundProcessor that mutates colony state during ProcessColony
   - Import helpers that merge parsed data into existing entities
   - WriteContext and serialization code that reads entity data for JSON output
3. WHEN a Read_Only_Consumer currently receives a mutable entity list, THE migration SHALL change the parameter or local variable type to the corresponding read-only wrapper type.
4. IF a consumer both reads and writes entity data, THEN THE consumer SHALL continue to use mutable entities and SHALL NOT be migrated.

### Requirement 12: Nullable Nested Wrapper Handling

**User Story:** As a developer, I want read-only wrappers to handle nullable nested objects correctly, so that null CountDownTime or null Contents fields do not cause NullReferenceException.

#### Acceptance Criteria

1. WHEN a Mutable_Entity has a nullable Nested_Type property (e.g. ColonyStructure.BuildCompletionTime, Item.Contents), THE Wrapper SHALL return null for the corresponding read-only wrapper property when the underlying value is null.
2. WHEN the underlying nullable Nested_Type property is non-null, THE Wrapper SHALL return a new read-only wrapper instance wrapping the value.
3. THE ReadOnlyColonyStructure.BuildCompletionTime SHALL return `ReadOnlyCountDownTime` when non-null, null when the underlying BuildCompletionTime is null.
4. THE ReadOnlyColonyStructure.ProcessCompletionTime SHALL return `ReadOnlyCountDownTime` when non-null, null when the underlying ProcessCompletionTime is null.
5. THE ReadOnlyItem.Contents SHALL return `ReadOnlyItemBag` when the underlying Item is a Crate with non-null Contents, null otherwise.

### Requirement 13: Enum Property Passthrough

**User Story:** As a developer, I want enum properties on read-only wrappers to use the same enum types as the mutable entities, so that consumer code can compare and switch on enum values without conversion.

#### Acceptance Criteria

1. WHEN a Mutable_Entity property is an enum type (e.g. DestinationType, ItemType.ItemTypeEnum, StationType, TransactionType, BuildItemType, BuildItemStatus, StockTargetScope, SurveyType, RouteStopPurpose, StationOwnership, SupplyChainStageType), THE Wrapper SHALL expose the same enum type as a read-only property.
2. THE Wrapper SHALL NOT define new enum types or wrapper enums. Existing enum types SHALL be reused directly.

### Requirement 14: Thread Safety of Wrapper Construction

**User Story:** As a developer, I want read-only wrapper list construction to respect the existing lock hierarchy, so that wrapper creation does not introduce data races.

#### Acceptance Criteria

1. WHEN PlayerContext constructs a read-only wrapper list, THE PlayerContext SHALL acquire `_listLock` while iterating the backing list to create wrappers.
2. WHEN EmpireContext constructs a read-only wrapper list, THE EmpireContext SHALL follow the same locking pattern used by existing list access methods.
3. THE Wrapper construction itself (calling `new ReadOnly{Entity}(entity)`) SHALL NOT acquire any locks — it stores only a reference.
4. WHEN a read-only wrapper property is accessed that delegates to a thread-safe method on the underlying entity (e.g. PropertyBag.GetDecimal, ItemBag.CountByType), THE Wrapper SHALL rely on the underlying entity's existing lock for thread safety.

### Requirement 15: Equality and Identity

**User Story:** As a developer, I want read-only wrappers to support equality comparison based on the wrapped entity's UUID, so that collections and lookups work correctly.

#### Acceptance Criteria

1. WHEN two Wrapper instances wrap the same Mutable_Entity instance, calling `Equals` SHALL return true.
2. WHEN two Wrapper instances wrap different Mutable_Entity instances with the same UUID, calling `Equals` SHALL return true.
3. THE Wrapper SHALL override `GetHashCode` to return the same hash code as the wrapped entity's UUID.
4. THE Wrapper SHALL override `ToString` to return the wrapped entity's ExtendedName (if available) or Name, matching the display behavior of the mutable entity.

### Requirement 16: Incremental Adoption Strategy

**User Story:** As a developer, I want the wrapper types to be created for the entire data model upfront, so that consumers can be migrated incrementally without blocking on wrapper availability.

#### Acceptance Criteria

1. THE System SHALL create all ReadOnly wrapper classes for all 28 entity types and 4 utility containers in a single implementation phase.
2. THE System SHALL add all GetReadOnly methods to PlayerContext and EmpireContext in the same phase.
3. Consumer migration (switching read-only consumers from mutable to wrapper types) SHALL be performed incrementally after the wrapper infrastructure is in place.
4. THE existing mutable API SHALL remain available throughout and after the migration — wrappers are additive, not replacements.
