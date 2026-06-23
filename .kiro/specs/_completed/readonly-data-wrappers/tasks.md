# Implementation Plan: Read-Only Data Wrappers

## Overview

Implement a complete set of read-only wrapper classes for the OE2EmpireTracker data model. Each mutable entity gets a corresponding `ReadOnly{Entity}` class that holds a private readonly reference and exposes only getter properties. The wrappers are separate classes (no shared interfaces or base classes with the mutable type), preventing consumer code from casting back to the mutable type. PlayerContext and EmpireContext gain `GetReadOnly*` methods that construct wrapper lists on the fly under lock.

The implementation is organized in layers: utility container wrappers first, then entity wrappers (building from leaf/nested types up to top-level entities), then context methods, then tests. This ensures each layer can reference the wrappers it depends on.

## Tasks

- [x] 1. Implement utility container wrappers
  - [x] 1.1 Create ReadOnlyPropertyBag wrapper class
    - Create `OE2EmpireTracker/Models/ReadOnlyPropertyBag.cs`
    - Single private readonly field `_entity` of type `PropertyBag`
    - Constructor with null guard (`ArgumentNullException`)
    - Expose query methods: `GetDecimal`, `GetLong`, `GetBoolean`, `GetString`, `ContainsKey`, `Count`
    - Do NOT expose: `SetProperty`, `Remove`, `Clear`, `Properties` dictionary
    - Override `Equals` (reference equality on wrapped entity), `GetHashCode`, `ToString`
    - _Requirements: 1.1-1.6, 3.1-3.3, 5.1, 5.2_

  - [x] 1.2 Create ReadOnlyItemBag wrapper class
    - Create `OE2EmpireTracker/Models/ReadOnlyItemBag.cs`
    - Single private readonly field `_entity` of type `ItemBag`
    - Constructor with null guard
    - Expose query methods: `CountByType`, `FindByType` (returns `IReadOnlyList<ReadOnlyItem>`), `FindResource` (returns `IReadOnlyList<ReadOnlyItem>`), `Count`, `ContainsKey`
    - Do NOT expose: `AddItem`, `Remove`, `Clear`, `Items` dictionary
    - Note: `ReadOnlyItem` is created in task 2 — implement the wrapping logic referencing it now
    - _Requirements: 1.1-1.6, 3.1-3.3, 5.3, 5.4, 5.9_

  - [x] 1.3 Create ReadOnlyLockTracking wrapper class
    - Create `OE2EmpireTracker/Models/ReadOnlyLockTracking.cs`
    - Single private readonly field `_entity` of type `LockTracking`
    - Constructor with null guard
    - Expose query methods: `GetLockedQuantity`, `GetLocksForProcess` (returns `IReadOnlyList<ItemLock>`)
    - Do NOT expose: `LockItem`, `LockItems`, `ClearLocksForProcess`, `RawLocks`
    - _Requirements: 1.1-1.6, 3.1-3.3, 5.5, 5.6_

  - [x] 1.4 Create ReadOnlyCountDownTime wrapper class
    - Create `OE2EmpireTracker/Models/ReadOnlyCountDownTime.cs`
    - Single private readonly field `_entity` of type `CountDownTime`
    - Constructor with null guard
    - Expose read-only properties: `TimeRemaining` (getter only), `TimeRemainingString` (getter only), `IntervalsPassed`, `IsRepeating`, `RepeatIntervalSeconds`, `StartTime`, `EndTime`
    - Do NOT expose: `TimeRemaining` setter, `TimeRemainingString` setter, `ConsumeIntervals`, `StartRepeating`
    - _Requirements: 1.1-1.6, 3.1-3.3, 4.5, 5.7, 5.8_

- [x] 2. Implement nested/leaf type wrappers (no UUID, owned by parent entities)
  - [x] 2.1 Create ReadOnlyItem, ReadOnlyCommodityRequested, ReadOnlyColonyStructureStatus wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyItem.cs` — wrap all public properties; `Contents` as `ReadOnlyItemBag` (nullable); expose `ExtendedName` computed property
    - Create `OE2EmpireTracker/Models/ReadOnlyCommodityRequested.cs` — wrap all public properties as read-only
    - Create `OE2EmpireTracker/Models/ReadOnlyColonyStructureStatus.cs` — wrap all public properties as read-only
    - Each: single private readonly field, constructor with null guard, no JSON attributes
    - Equality: reference equality on wrapped entity (no UUID)
    - _Requirements: 1.1-1.12, 3.1-3.3, 4.6, 6.32, 6.35, 6.36, 12.5, 13.1_

  - [x] 2.2 Create ReadOnlyColonyStructure wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyColonyStructure.cs`
    - Wrap all public properties as read-only
    - `Properties` and `AssignedWorkers` as `ReadOnlyPropertyBag`
    - `BuildCompletionTime` and `ProcessCompletionTime` as nullable `ReadOnlyCountDownTime` (null check)
    - `Statuses` as `IReadOnlyDictionary<string, ReadOnlyColonyStructureStatus>`
    - Expose `IsBuiltAndOnline` computed property
    - _Requirements: 1.8, 4.4, 6.3, 12.1-12.4, 13.1_

  - [x] 2.3 Create ReadOnlySurveyResource wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlySurveyResource.cs`
    - Expose: `Resource`, `Purity`, `Amount`, `ExtendedName`
    - _Requirements: 6.5_

  - [x] 2.4 Create ReadOnlyPlayerRank and ReadOnlyPlayerSkill wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyPlayerRank.cs` — expose: `Rank`, `CurrentXP`, `NextXP`
    - Create `OE2EmpireTracker/Models/ReadOnlyPlayerSkill.cs` — expose: `Level`, `CurrentXP`, `NextXP`
    - _Requirements: 6.7, 6.8_

  - [x] 2.5 Create ReadOnlyRouteStop wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyRouteStop.cs`
    - Expose: `ColonyUUID`, `Sequence`, `DestinationType`, `DestinationUUID`, `Purpose`, `FuelEstimate`
    - Enum properties (`DestinationType`, `Purpose`) use existing enum types directly
    - _Requirements: 6.10, 13.1_

  - [x] 2.6 Create ReadOnlyDeliveryPlanStop, ReadOnlyDeliveryItem wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyDeliveryPlanStop.cs` — wrap all public properties; expose items as `IReadOnlyList<ReadOnlyDeliveryItem>`
    - Create `OE2EmpireTracker/Models/ReadOnlyDeliveryItem.cs` — wrap all public properties as read-only
    - _Requirements: 6.11_

  - [x] 2.7 Create ReadOnlyBuildItem wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyBuildItem.cs` — wrap all public properties as read-only
    - Enum properties (`BuildItemType`, `BuildItemStatus`) use existing enum types
    - _Requirements: 6.13, 13.1_

  - [x] 2.8 Create ReadOnlyShipComponentSlot wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyShipComponentSlot.cs`
    - Expose: `SlotType`, `SlotIndex`, `BlueprintUUID`, `CurrentHP`, `MaxHP`, `MaxRepairPercent`
    - _Requirements: 6.15_

  - [x] 2.9 Create ReadOnlyStockTarget, ReadOnlyStockProfileEntry wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyStockTarget.cs` — wrap all public properties as read-only
    - Create `OE2EmpireTracker/Models/ReadOnlyStockProfileEntry.cs` — expose: `GroupID`, `StockPlanUUID`
    - _Requirements: 6.21, 6.23_

  - [x] 2.10 Create ReadOnlySupplyChainStage, ReadOnlyWarehouseOverflowRule wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlySupplyChainStage.cs` — wrap all public properties as read-only
    - Create `OE2EmpireTracker/Models/ReadOnlyWarehouseOverflowRule.cs` — wrap all public properties as read-only
    - _Requirements: 6.25, 6.26_

  - [x] 2.11 Create ReadOnlyAsteroidReserve wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyAsteroidReserve.cs`
    - Expose: `ResourceName`, `Purity`, `MaxReserve`, `CurrentReserve`, `ResetTimestamp`
    - _Requirements: 6.30_

- [x] 3. Implement top-level entity wrappers (with UUID, owned by a player)
  - [x] 3.1 Create ReadOnlyBlueprint wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyBlueprint.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `BaseBlueprintUUID`, `LegacyUUID`, `BluePrintType`, `Evolution`, `TechLevel`, `Class`, `CopyCost`, `NickName`, `Description`
    - Computed properties: `ExtendedName`, `OutputItemName`
    - `Properties` as `ReadOnlyPropertyBag`, `Resources` as `IReadOnlyDictionary<string, string>`
    - Override `Equals`/`GetHashCode` by UUID, `ToString` returns `ExtendedName`
    - _Requirements: 1.1-1.12, 2.1-2.4, 4.1, 4.2, 6.1, 15.1-15.4_

  - [x] 3.2 Create ReadOnlyColony wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyColony.cs`
    - Expose: `UUID`, `OwnerUUID`, `LegacyUUID`, `PlanetName`, `SystemName`, `ColonyName`, `LastImportDateTime`
    - `Items` as `ReadOnlyItemBag`, `Structures` as `IReadOnlyList<ReadOnlyColonyStructure>`, `Commodities` as `IReadOnlyList<ReadOnlyCommodityRequested>`, `Locks` as `ReadOnlyLockTracking`
    - Override `Equals`/`GetHashCode` by UUID, `ToString` returns `Name`
    - _Requirements: 1.8, 1.9, 6.2, 15.1-15.4_

  - [x] 3.3 Create ReadOnlySurvey wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlySurvey.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `PlanetName`, `SystemName`, `SurveyID`, `NickName`, `SurveyType`, `AsteroidUUID`, `ExtendedName`
    - `Resources` as `IReadOnlyDictionary<string, ReadOnlySurveyResource>`
    - _Requirements: 4.3, 6.4, 13.1, 15.1-15.4_

  - [x] 3.4 Create ReadOnlyPlayerProfile wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyPlayerProfile.cs`
    - Expose: `UUID`, `Name`, `FactionUUID`
    - Read-only access to skills: `GetSkill` returning `ReadOnlyPlayerSkill`, `GetSkillGroup` returning bool
    - `Public`, `Private`, `Military` ranks as `ReadOnlyPlayerRank`
    - _Requirements: 6.6, 15.1-15.4_

  - [x] 3.5 Create ReadOnlyDeliveryRoute wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyDeliveryRoute.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `Stops` as `IReadOnlyList<ReadOnlyRouteStop>`
    - _Requirements: 6.9, 15.1-15.4_

  - [x] 3.6 Create ReadOnlyDeliveryPlan wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyDeliveryPlan.cs`
    - Expose all public properties as read-only, `Stops` as `IReadOnlyList<ReadOnlyDeliveryPlanStop>`
    - _Requirements: 6.11, 15.1-15.4_

  - [x] 3.7 Create ReadOnlyBuildPlan wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyBuildPlan.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `Description`, `DeliveryPlanUUID`, `IsActive`, `Items` as `IReadOnlyList<ReadOnlyBuildItem>`
    - _Requirements: 6.12, 15.1-15.4_

  - [x] 3.8 Create ReadOnlyShipTemplate wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyShipTemplate.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `HullBlueprintUUID`, `Components` as `IReadOnlyList<ReadOnlyShipComponentSlot>`
    - _Requirements: 6.14, 15.1-15.4_

  - [x] 3.9 Create ReadOnlyShip wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyShip.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `TemplateUUID`, `HullBlueprintUUID`, `Components` as `IReadOnlyList<ReadOnlyShipComponentSlot>`, `LocationType`, `LocationUUID`, `Cargo` as `ReadOnlyItemBag`, `Hopper` as `ReadOnlyItemBag`, `HullCurrentHP`, `HullMaxHP`, `HullMaxRepairPercent`
    - _Requirements: 6.16, 15.1-15.4_

  - [x] 3.10 Create ReadOnlyStation wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyStation.cs`
    - Expose: `UUID`, `Name`, `StationType`, `Ownership`, `OwnerUUID`, `Holds` as `IReadOnlyDictionary<string, ReadOnlyItemBag>`, `Components` as `IReadOnlyList<ReadOnlyShipComponentSlot>`, `StationBlueprintUUID`, `MunitionsHold` as `ReadOnlyItemBag`, `HullCurrentHP`, `HullMaxHP`, `HullMaxRepairPercent`
    - _Requirements: 6.17, 1.11, 15.1-15.4_

  - [x] 3.11 Create ReadOnlyMarketListing and ReadOnlyMarketTransaction wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyMarketListing.cs` — all public properties as read-only
    - Create `OE2EmpireTracker/Models/ReadOnlyMarketTransaction.cs` — all public properties as read-only
    - _Requirements: 6.18, 6.19, 13.1, 15.1-15.4_

  - [x] 3.12 Create ReadOnlyStockPlan and ReadOnlyStockProfile wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyStockPlan.cs` — expose: `UUID`, `Name`, `OwnerUUID`, `ReplenishmentBuildPlanUUID`, `IsActive`, `Targets` as `IReadOnlyList<ReadOnlyStockTarget>`
    - Create `OE2EmpireTracker/Models/ReadOnlyStockProfile.cs` — expose: `UUID`, `Name`, `OwnerUUID`, `IsActive`, `Entries` as `IReadOnlyList<ReadOnlyStockProfileEntry>`
    - _Requirements: 6.20, 6.22, 15.1-15.4_

  - [x] 3.13 Create ReadOnlySupplyChain wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlySupplyChain.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `IsActive`, `Stages` as `IReadOnlyList<ReadOnlySupplyChainStage>`
    - _Requirements: 6.24, 15.1-15.4_

  - [x] 3.14 Create ReadOnlyFaction, ReadOnlyExternalCharacter wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyFaction.cs` — expose: `UUID`, `Name`, `Description`
    - Create `OE2EmpireTracker/Models/ReadOnlyExternalCharacter.cs` — expose: `UUID`, `Name`, `FactionUUID`
    - _Requirements: 6.27, 6.28, 15.1-15.4_

  - [x] 3.15 Create ReadOnlyAsteroid wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyAsteroid.cs`
    - Expose: `UUID`, `Name`, `SystemName`, `Reserves` as `IReadOnlyList<ReadOnlyAsteroidReserve>`
    - _Requirements: 6.29, 15.1-15.4_

  - [x] 3.16 Create ReadOnlyPricingPlan wrapper
    - Create `OE2EmpireTracker/Models/ReadOnlyPricingPlan.cs`
    - Expose: `UUID`, `Name`, `OwnerUUID`, `Description`, `FixedCostPerItem`, `HourlyCostRate`, `ResourcePrices` as `IReadOnlyDictionary<string, decimal>`
    - _Requirements: 6.31, 1.10, 15.1-15.4_

  - [x] 3.17 Create ReadOnlyCommodity and ReadOnlyResource wrappers
    - Create `OE2EmpireTracker/Models/ReadOnlyCommodity.cs` — expose: `Name`, `ExtendedName`, `Industry`, `Group`, `ConstructionResources` as `IReadOnlyDictionary`
    - Create `OE2EmpireTracker/Models/ReadOnlyResource.cs` — expose: `Name`, `Group`, `Synthetic`
    - _Requirements: 6.33, 6.34_

- [x] 4. Checkpoint - Verify all wrapper classes compile
  - Build the solution and verify zero warnings, zero errors
  - Ensure all 40 wrapper classes (4 utility + 36 entity/nested) are present in `OE2EmpireTracker/Models/`
  - Ensure all tests pass, ask the user if questions arise

- [x] 5. Add GetReadOnly methods to PlayerContext
  - [x] 5.1 Add full-list GetReadOnly methods to PlayerContext
    - Add `GetReadOnlyBlueprintList()`, `GetReadOnlyColonyList()`, `GetReadOnlySurveyList()`, `GetReadOnlyPlayerProfileList()`, `GetReadOnlyDeliveryRouteList()`, `GetReadOnlyDeliveryPlanList()`, `GetReadOnlyPricingPlanList()`, `GetReadOnlyBuildPlanList()`, `GetReadOnlyShipTemplateList()`, `GetReadOnlyShipList()`, `GetReadOnlyStationList()`, `GetReadOnlyMarketListingList()`, `GetReadOnlyMarketTransactionList()`, `GetReadOnlyStockPlanList()`, `GetReadOnlyStockProfileList()`, `GetReadOnlySupplyChainList()`, `GetReadOnlyWarehouseOverflowRuleList()`, `GetReadOnlyFactionList()`, `GetReadOnlyExternalCharacterList()`, `GetReadOnlyAsteroidList()`
    - Each method: acquire `_listLock`, iterate backing list, wrap each element with `new ReadOnly{Entity}(e)`, return as `IReadOnlyList<ReadOnly{Entity}>`
    - _Requirements: 7.1-7.21, 14.1, 14.3_

  - [x] 5.2 Add FindReadOnly methods to PlayerContext
    - Add `FindReadOnlyBlueprint(string id)`, `FindReadOnlyColony(string id)`, `FindReadOnlySurvey(string id)`, `FindReadOnlyPlayerProfile(string id)`, `FindReadOnlyDeliveryRoute(string id)`, `FindReadOnlyDeliveryPlan(string id)`, `FindReadOnlyPricingPlan(string id)`, `FindReadOnlyBuildPlan(string id)`, `FindReadOnlyShipTemplate(string id)`, `FindReadOnlyShip(string id)`, `FindReadOnlyStation(string id)`, `FindReadOnlyMarketListing(string id)`, `FindReadOnlyMarketTransaction(string id)`, `FindReadOnlyStockPlan(string id)`, `FindReadOnlyStockProfile(string id)`, `FindReadOnlySupplyChain(string id)`, `FindReadOnlyWarehouseOverflowRule(string id)`, `FindReadOnlyFaction(string id)`, `FindReadOnlyExternalCharacter(string id)`, `FindReadOnlyAsteroid(string id)`
    - Each method: delegate to existing `Find{Entity}(id)`, wrap result if non-null, return null otherwise
    - _Requirements: 7.22, 7.23_

  - [x] 5.3 Add current-player filtered read-only methods to PlayerContext
    - Add `GetCurrentPlayerReadOnlyColonies()`, `GetCurrentPlayerReadOnlyBlueprints()`, `GetCurrentPlayerReadOnlySurveys()`, `GetCurrentPlayerReadOnlyRoutes()`, `GetCurrentPlayerReadOnlyPlans()`, `GetCurrentPlayerReadOnlyPricingPlans()`, `GetCurrentPlayerReadOnlyBuildPlans()`, `GetCurrentPlayerReadOnlyShipTemplates()`, `GetCurrentPlayerReadOnlyShips()`, `GetCurrentPlayerReadOnlyStations()`, `GetCurrentPlayerReadOnlyListings()`, `GetCurrentPlayerReadOnlyTransactions()`, `GetCurrentPlayerReadOnlyStockPlans()`, `GetCurrentPlayerReadOnlyStockProfiles()`, `GetCurrentPlayerReadOnlySupplyChains()`, `GetCurrentPlayerReadOnlyOverflowRules()`
    - Add `GetAllReadOnlyBlueprints()` combining player and global blueprints
    - Each method: acquire `_listLock`, filter by `OwnerUUID == CurrentPlayerUUID`, wrap each element, return `List<ReadOnly{Entity}>`
    - _Requirements: 9.1-9.17, 14.1_

- [x] 6. Add GetReadOnly methods to EmpireContext
  - [x] 6.1 Add GetReadOnly and FindReadOnly methods to EmpireContext
    - Add `GetReadOnlyCommodityList()` returning `IReadOnlyList<ReadOnlyCommodity>`
    - Add `GetReadOnlyResourceList()` returning `IReadOnlyList<ReadOnlyResource>`
    - Add `GetReadOnlyGlobalBlueprintList()` returning `IReadOnlyList<ReadOnlyBlueprint>`
    - Add `FindReadOnlyCommodity(string name)` returning `ReadOnlyCommodity` or null
    - Add `FindReadOnlyGlobalBlueprint(string id)` returning `ReadOnlyBlueprint` or null
    - EmpireContext commodity methods use `_commodityLock` for thread safety
    - _Requirements: 8.1-8.6, 14.2_

- [x] 7. Checkpoint - Verify context methods compile and existing tests pass
  - Build the solution and verify zero warnings, zero errors
  - Run existing test suite to confirm no regressions (Requirement 10: mutable path unchanged)
  - Ensure all tests pass, ask the user if questions arise

- [x] 8. Create FsCheck generators for property-based tests
  - [x] 8.1 Create ReadOnlyWrapperGenerators.cs with FsCheck Arbitrary generators
    - Create `OE2EmpireTracker.Tests/Models/ReadOnlyWrapperGenerators.cs`
    - Implement custom `Arbitrary<T>` generators for: `PropertyBag`, `ItemBag`, `LockTracking`, `CountDownTime`, `Item`, `Blueprint`, `Colony`, `ColonyStructure`, `Survey`, `SurveyResource`, `PlayerProfile`, `PlayerRank`, `PlayerSkill`, `DeliveryRoute`, `RouteStop`, `DeliveryPlan`, `BuildPlan`, `BuildItem`, `ShipTemplate`, `ShipComponentSlot`, `Ship`, `Station`, `MarketListing`, `MarketTransaction`, `StockPlan`, `StockTarget`, `StockProfile`, `StockProfileEntry`, `SupplyChain`, `SupplyChainStage`, `WarehouseOverflowRule`, `Faction`, `ExternalCharacter`, `Asteroid`, `AsteroidReserve`, `PricingPlan`, `Commodity`, `Resource`, `CommodityRequested`, `ColonyStructureStatus`
    - Each generator produces random but valid instances: random UUIDs, random strings, random numeric values, random enum values, random nested collections (0-5 elements), random nullable nested objects (null 50% of the time)
    - Minimum 100 iterations per property test
    - _Requirements: Design Testing Strategy_

- [x] 9. Implement property-based tests for wrapper correctness
  - [x] 9.1 Create ReadOnlyWrapperPropertyTests.cs with property tests P1-P4, P7-P9
    - Create `OE2EmpireTracker.Tests/Models/ReadOnlyWrapperPropertyTests.cs`
    - Use FsCheck.NUnit `[Property]` attribute with `MaxTest = 100`
    - _Requirements: Design Correctness Properties_

  - [x] 9.2 Write property test for Property 1: Value and Enum Passthrough
    - **Property 1: Value and Enum Passthrough**
    - For randomly generated entities, wrap in ReadOnly wrapper, verify all value-type/string/enum properties return identical values
    - Test representative entity types: Blueprint, Colony, DeliveryRoute, Ship, MarketListing
    - **Validates: Requirements 1.5, 1.7, 4.1, 13.1**

  - [x] 9.3 Write property test for Property 2: Nested Recursive Wrapping
    - **Property 2: Nested Recursive Wrapping**
    - For entities with nested types, verify children are wrapped as ReadOnly types with matching properties, lists have same count and element-wise equality, dictionaries have same keys and wrapped values
    - Test: Colony (Structures, Items, Locks), Survey (Resources), Ship (Components, Cargo)
    - **Validates: Requirements 1.8, 1.9, 1.10, 1.11, 5.9, 6.1-6.36**

  - [x] 9.4 Write property test for Property 3: Live Read-Through
    - **Property 3: Live Read-Through**
    - Generate entity, wrap, mutate a property on the mutable entity, verify wrapper reflects the new value
    - Test with Blueprint.Name, Colony.ColonyName, Ship.HullCurrentHP
    - **Validates: Requirements 3.2, 3.3**

  - [x] 9.5 Write property test for Property 4: Utility Container Query Delegation
    - **Property 4: Utility Container Query Delegation**
    - For randomly populated PropertyBag/ItemBag/LockTracking/CountDownTime, wrap and verify each query method returns same result as calling directly on mutable container
    - **Validates: Requirements 5.1, 5.3, 5.5, 5.7**

  - [x] 9.6 Write property test for Property 7: Nullable Nested Handling
    - **Property 7: Nullable Nested Handling**
    - For entities with nullable nested properties (ColonyStructure.BuildCompletionTime, Item.Contents), verify null returns null wrapper, non-null returns non-null wrapper with matching properties
    - **Validates: Requirements 12.1-12.5**

  - [x] 9.7 Write property test for Property 8: Equality by UUID
    - **Property 8: Equality by UUID**
    - Generate pairs of entities with same/different UUIDs, wrap, verify Equals returns true for same UUID and false for different, verify GetHashCode matches for same UUID
    - **Validates: Requirements 15.1, 15.2, 15.3**

  - [x] 9.8 Write property test for Property 9: ToString Matches Display Name
    - **Property 9: ToString Matches Display Name**
    - For entities with ExtendedName, verify wrapper ToString returns ExtendedName; for entities with only Name, verify ToString returns Name
    - **Validates: Requirements 15.4**

- [x] 10. Implement property-based tests for context methods
  - [x] 10.1 Create ReadOnlyContextMethodTests.cs with property tests P5-P6
    - Create `OE2EmpireTracker.Tests/Models/ReadOnlyContextMethodTests.cs`
    - Use FsCheck.NUnit `[Property]` attribute with `MaxTest = 100`
    - Reset PlayerContext and EmpireContext singletons in test setup
    - _Requirements: Design Correctness Properties_

  - [x] 10.2 Write property test for Property 5: GetReadOnly List Preservation
    - **Property 5: GetReadOnly List Preservation**
    - Populate PlayerContext backing lists with random entities, call GetReadOnly*List(), verify same count and UUID-wise match at each position
    - **Validates: Requirements 7.1-7.21, 8.1-8.6**

  - [x] 10.3 Write property test for Property 6: Current-Player Filtering
    - **Property 6: Current-Player Filtering**
    - Populate backing list with entities having mixed OwnerUUIDs, set CurrentPlayerUUID, call GetCurrentPlayerReadOnly*(), verify only matching entities returned and all are wrapped
    - **Validates: Requirements 9.1-9.17**

- [x] 11. Implement structural unit tests
  - [x] 11.1 Create ReadOnlyWrapperStructuralTests.cs with reflection-based tests
    - Create `OE2EmpireTracker.Tests/Models/ReadOnlyWrapperStructuralTests.cs`
    - _Requirements: 1.1-1.6, 2.1-2.4, 3.1, 12.1_

  - [x] 11.2 Write structural test: No Mutation Surface
    - For each wrapper type, use reflection to verify: no public setters, no public methods that mutate state (Add*, Remove*, Set*, Clear*), no public/internal property or field exposing the wrapped entity
    - _Requirements: 1.6, 2.3_

  - [x] 11.3 Write structural test: Single Field
    - For each wrapper type, use reflection to verify exactly one private readonly instance field
    - _Requirements: 3.1_

  - [x] 11.4 Write structural test: No JSON Attributes
    - For each wrapper type, use reflection to verify no `[JsonProperty]`, `[JsonConverter]`, `[JsonIgnore]` attributes on the wrapper class or its members
    - _Requirements: 1.12_

  - [x] 11.5 Write structural test: No Casting Path
    - For each wrapper type, use reflection to verify: no shared interfaces with the mutable entity (other than from `object`), no inheritance from mutable entity, no implicit/explicit conversion operators
    - _Requirements: 2.1, 2.2, 2.4_

  - [x] 11.6 Write structural test: Null Constructor Guard
    - For each wrapper type, verify `ArgumentNullException` is thrown when null is passed to the constructor
    - _Requirements: Design Error Handling_

  - [x] 11.7 Write structural test: Empty Collections
    - Wrap entities with empty lists/dictionaries, verify wrapper returns empty `IReadOnlyList`/`IReadOnlyDictionary` (not null)
    - _Requirements: 1.9, 1.10_

- [x] 12. Final checkpoint - Build, test, and verify
  - Build the solution with zero warnings
  - Run all tests (existing + new) and verify they pass
  - Verify existing mutable API is unchanged (Requirement 10)
  - Ensure all tests pass, ask the user if questions arise

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate structural requirements via reflection
- Consumer migration (switching read-only consumers from mutable to wrapper types) is a LATER phase — not part of these tasks
- The existing mutable API remains completely unchanged throughout
- All 40 wrapper classes are created in `OE2EmpireTracker/Models/` alongside their mutable counterparts
- FsCheck 2.16.6 and FsCheck.NUnit 2.16.6 are already installed in the test project
