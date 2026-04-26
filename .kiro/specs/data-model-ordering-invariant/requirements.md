# Requirements Document

## Introduction

The data model persists collections (`List<T>`, `Dictionary<K,V>`) as JSON arrays and objects via `SerializationSorter`, which reorders them for deterministic JSON diffs (typically by UUID or Sequence). After a save/load cycle the live list order may differ from the order the application originally established. Any code that assumes a meaningful order from the data model breaks silently after deserialization.

This spec establishes an architectural invariant: **data model collections are unordered bags**. Code that needs a specific order must sort explicitly at the point of consumption, receive pre-sorted data from its caller, or maintain a sorted cache in a ViewModel.

## Glossary

- **Data_Model**: The set of POCO classes in `OE2EmpireTracker/Models/` that are serialized to and deserialized from JSON. Includes all `List<T>` and `Dictionary<K,V>` properties on these classes.
- **Ordered_Collection**: A `List<T>` or `Dictionary<K,V>` property on a Data_Model entity whose elements have a domain-meaningful ordering defined by a sort key (e.g. `BuildQueueSequence`, `Sequence`).
- **Sort_Key**: A property on a collection element that defines its domain-meaningful order (e.g. `ColonyStructure.BuildQueueSequence`, `RouteStop.Sequence`, `SupplyChainStage.Sequence`).
- **Consumer**: Any code (service, ViewModel, form, test) that reads or iterates an Ordered_Collection.
- **SerializationSorter**: The static class `OE2EmpireTracker.Services.SerializationSorter` that reorders Data_Model collections before JSON serialization for deterministic output.
- **Sort_At_Consumption**: The pattern where a Consumer sorts an Ordered_Collection by its Sort_Key immediately before use, rather than relying on the list's current position order.
- **Sorted_Cache**: A ViewModel-maintained `IReadOnlyList<T>` that is built by sorting the underlying Data_Model collection and is invalidated when the collection changes.
- **Parameter_Pattern**: The pattern where a method receives a pre-sorted `IReadOnlyList<T>` or `IEnumerable<T>` from its caller, documented via XML doc comments or parameter naming.

## Requirements

### Requirement 1: Unordered Bag Invariant

**User Story:** As a developer, I want a clear architectural rule that data model collections are unordered, so that I never write code that silently depends on list position.

#### Acceptance Criteria

1. THE Data_Model SHALL treat all `List<T>` and `Dictionary<K,V>` properties as unordered bags whose element order may change at any time due to serialization, deserialization, migration, or concurrent access.
2. WHEN the SerializationSorter reorders a collection for JSON output, THE Data_Model collection's live order SHALL be considered undefined after that point.
3. IF a Consumer iterates an Ordered_Collection without first sorting by the documented Sort_Key, THEN THE Consumer SHALL be considered non-compliant with this invariant.

### Requirement 2: Sort-At-Consumption Rule

**User Story:** As a developer, I want a defined set of patterns for obtaining ordered data, so that every consumption site handles ordering explicitly and correctly.

#### Acceptance Criteria

1. WHEN a Consumer needs elements in domain order, THE Consumer SHALL use one of three approved patterns: Sort_At_Consumption (via Sort_Helper), Sorted_Cache, or Parameter_Pattern.
2. WHEN using Sort_At_Consumption, THE Consumer SHALL call the appropriate Sort_Helper method (see Requirement 8) rather than inlining `.OrderBy()` calls directly. This ensures sort logic is defined in a single place.
3. WHEN using Sorted_Cache, THE ViewModel SHALL build an `IReadOnlyList<T>` sorted via the appropriate Sort_Helper method and SHALL invalidate the cache when the underlying collection changes.
4. WHEN using Parameter_Pattern, THE method SHALL accept a pre-sorted `IReadOnlyList<T>` or `IEnumerable<T>` parameter, and THE caller SHALL be responsible for providing correctly sorted data via a Sort_Helper.
5. WHEN using Parameter_Pattern, THE method's XML doc comment or parameter name SHALL document that the parameter is expected to be sorted and by which Sort_Key.

### Requirement 3: Sort Key Registry

**User Story:** As a developer, I want a single reference listing every ordered collection and its sort key, so that I know how to sort each collection correctly.

#### Acceptance Criteria

1. THE following Ordered_Collections and their Sort_Keys SHALL be documented and enforced:
   - `PlayerRoot.PlayerProfile` (`PlayerProfile[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.Blueprint` (`Blueprint[]`) — Sort_Key: `ExtendedName` (string, ascending)
   - `PlayerRoot.Survey` (`Survey[]`) — Sort_Key: `PlanetName` (string, ascending) then `GameID` (string, ascending)
   - `PlayerRoot.Colony` (`Colony[]`) — Sort_Key: `SystemName` (string, ascending) then `PlanetName` (string, ascending) then `ColonyName` (string, ascending)
   - `PlayerRoot.DeliveryRoute` (`DeliveryRoute[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.DeliveryPlan` (`DeliveryPlan[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.PricingPlan` (`PricingPlan[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.BuildPlan` (`BuildPlan[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.ShipTemplate` (`ShipTemplate[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.Ship` (`Ship[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.Station` (`Station[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.MarketListing` (`MarketListing[]`) — Sort_Key: `ItemName` (string, ascending)
   - `PlayerRoot.MarketTransaction` (`MarketTransaction[]`) — Sort_Key: `ItemName` (string, ascending)
   - `PlayerRoot.StockPlan` (`StockPlan[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.StockProfile` (`StockProfile[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.SupplyChain` (`SupplyChain[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.WarehouseOverflowRule` (`WarehouseOverflowRule[]`) — Sort_Key: `ResourceName` (string, ascending)
   - `PlayerRoot.Faction` (`Faction[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.ExternalCharacter` (`ExternalCharacter[]`) — Sort_Key: `Name` (string, ascending)
   - `PlayerRoot.Asteroid` (`Asteroid[]`) — Sort_Key: `Name` (string, ascending)
   - `Colony.Structures` (`List<ColonyStructure>`) — Sort_Key: `BuildQueueSequence` (int, ascending)
   - `Colony.Commodities` (`List<CommodityRequested>`) — Sort_Key: `Name` (string, ascending); specific consumers may sort by `NeedBy` (DateTime) instead
   - `Colony.Items` (`ItemBag` / `Dictionary<string, Item>`) — Sort_Key: `ExtendedName` (string, ascending)
   - `DeliveryRoute.Stops` (`List<RouteStop>`) — Sort_Key: `Sequence` (int, ascending)
   - `DeliveryPlan.Stops` (`List<DeliveryPlanStop>`) — Sort_Key: `Sequence` (int, ascending)
   - `DeliveryPlanStop.DropOff` and `DeliveryPlanStop.PickUp` (`List<DeliveryItem>`) — Sort_Key: `Name` (string, ascending)
   - `SupplyChain.Stages` (`List<SupplyChainStage>`) — Sort_Key: `Sequence` (int, ascending)
   - `BuildPlan.Items` (`List<BuildItem>`) — Sort_Key: `ItemName` (string, ascending)
   - `StockPlan.Targets` (`List<StockTarget>`) — Sort_Key: `ItemType` (enum, ascending) then item-type-specific name (string, ascending)
   - `StockProfile.Entries` (`List<StockProfileEntry>`) — Sort_Key: `GroupID` (string, ascending) then StockPlan name (resolved via lookup, ascending)
   - `Asteroid.Reserves` (`List<AsteroidReserve>`) — Sort_Key: `ResourceName` (string, ascending)
   - `Station.Holds` (`Dictionary<string, ItemBag>`) — Sort_Key: player `Name` (resolved via lookup, ascending)
   - `Station.Components` (`List<ShipComponentSlot>`) — Sort_Key: `SlotType` (string, ascending) then `SlotIndex` (int, ascending)
   - `ShipTemplate.Components` (`List<ShipComponentSlot>`) — Sort_Key: `SlotType` (string, ascending) then `SlotIndex` (int, ascending)
   - `Ship.Components` (`List<ShipComponentSlot>`) — Sort_Key: `SlotType` (string, ascending) then `SlotIndex` (int, ascending)
   - `PropertyBag` internal dictionary — Sort_Key: property name (string, ascending)
2. WHEN a new `List<T>` or `Dictionary<K,V>` property is added to a Data_Model entity and the collection has a domain-meaningful order, THE developer SHALL add the collection and its Sort_Key to this registry.
3. THE Sort_Key for each Ordered_Collection SHALL match the key used by SerializationSorter for that collection.

### Requirement 4: Existing Consumer Compliance

**User Story:** As a developer, I want confidence that all existing code that consumes ordered collections already follows the sort-at-consumption rule, so that no silent ordering bugs remain.

#### Acceptance Criteria

1. THE `ColonyViewModel.StructureViewModels` property SHALL sort `Colony.Structures` by `BuildQueueSequence` when building its Sorted_Cache.
2. THE `ColonyStatusCalculator.CalculateBuilt()` method SHALL sort `Colony.Structures` by `BuildQueueSequence` before iterating.
3. THE `ColonyStatusCalculator.CalculateIdeal()` method SHALL sort `Colony.Structures` by `BuildQueueSequence` before iterating.
4. THE `BuildOrderOptimizer.Optimize()` method SHALL sort `Colony.Structures` by `BuildQueueSequence` before processing.
5. THE `ColonyBuildEligibility.GetFirstStagedStructure()` method SHALL sort `Colony.Structures` by `BuildQueueSequence` before finding the first staged structure.
6. THE `DeliveryPlan.CalculateLoadList()` method SHALL sort `DeliveryPlan.Stops` by `Sequence` before iterating.
7. THE `DeliveryPlanViewModel` methods that iterate route stops SHALL sort `RouteStop` collections by `Sequence` before processing.
8. THE `DeliveryGenerationService` methods that iterate route stops SHALL sort `RouteStop` collections by `Sequence` before processing.
9. THE `FormSupplyChain` methods that display or reorder stages SHALL sort `SupplyChain.Stages` by `Sequence` before use.
10. THE `FormDeliveryExecution` methods that display plan stops SHALL sort `DeliveryPlan.Stops` by `Sequence` before use.
11. THE `FormColonyDailyBuild` methods that iterate route stops SHALL sort `RouteStop` collections by `Sequence` before use.

### Requirement 5: Steering Rule for AI Compliance

**User Story:** As a developer using AI assistance, I want a steering rule that instructs the AI to follow the unordered-bag invariant, so that AI-generated code never introduces ordering bugs.

#### Acceptance Criteria

1. THE project SHALL include a steering file at `.kiro/steering/` that documents the unordered-bag invariant and the three approved consumption patterns.
2. THE steering file SHALL list all Ordered_Collections and their Sort_Keys from the Sort Key Registry.
3. THE steering file SHALL instruct that any new code iterating an Ordered_Collection must sort by the documented Sort_Key first, or receive pre-sorted data via the Parameter_Pattern.
4. THE steering file SHALL instruct that the Data_Model itself must not be modified to enforce ordering (no `SortedList`, no auto-sort on deserialization).

### Requirement 6: Completeness of the Sort Key Registry

**User Story:** As a developer, I want every data model collection to be accounted for in the Sort Key Registry, so that no collection is left ambiguous.

#### Acceptance Criteria

1. EVERY `List<T>` property on a Data_Model entity and every top-level array in `PlayerRoot` SHALL have an entry in the Sort Key Registry (Requirement 3).
2. EVERY `Dictionary<K,V>` property on a Data_Model entity SHALL have an entry in the Sort Key Registry or be documented as keyed-lookup-only (no iteration order needed).
3. WHEN a new collection property is added to any Data_Model entity, THE developer SHALL add it to the Sort Key Registry before the code is merged.

### Requirement 7: Dictionary Collections

**User Story:** As a developer, I want the invariant to cover dictionary-based collections too, so that code iterating `Dictionary<K,V>` does not assume key order.

#### Acceptance Criteria

1. THE `Colony.Items` (`ItemBag` / `Dictionary<string, Item>`) SHALL be sorted by `ExtendedName` at the point of consumption as documented in the Sort Key Registry.
2. THE `Station.Holds` (`Dictionary<string, ItemBag>`) SHALL be sorted by player `Name` (resolved via lookup) at the point of consumption as documented in the Sort Key Registry.
3. THE `PropertyBag` internal dictionary SHALL be sorted by property name at the point of consumption as documented in the Sort Key Registry.
4. WHEN a Consumer needs dictionary entries in a specific order not covered by the Sort Key Registry, THE Consumer SHALL sort the entries at the point of consumption and SHALL document the sort key.

### Requirement 8: Centralized Sort Helpers

**User Story:** As a developer, I want a single static helper class that provides the canonical sort for each ordered collection, so that sort logic is defined once and consumers never inline their own `.OrderBy()` expressions.

#### Acceptance Criteria

1. THE project SHALL provide a static class (e.g. `CollectionSortHelper`) in the `Services` namespace that exposes one or more methods per Ordered_Collection from the Sort Key Registry.
2. EACH Sort_Helper method SHALL accept the raw collection (e.g. `IEnumerable<ColonyStructure>`) and return an `IReadOnlyList<T>` sorted by the documented Sort_Key.
3. THE Sort_Helper methods SHALL be the single source of truth for sort order. If the sort key or direction changes, only the helper method needs to be updated.
4. ALL sorting of data model collections SHALL go through `CollectionSortHelper`. There SHALL be NO inline `.OrderBy()`, `.Sort()`, `.ThenBy()`, or equivalent LINQ/List sort calls on data model collection types anywhere in the codebase outside of `CollectionSortHelper` and `SerializationSorter`.
5. WHEN a collection type has multiple valid sort orders for different contexts, THE Sort_Helper class SHALL provide a separate named method for each sort order (e.g. `OrderCommodityRequests` by Name, `OrderCommodityRequestsByNeedBy` by NeedBy date).
6. THE Sort_Helper class SHALL include methods for at least the following collections:
   - `OrderStructures(IEnumerable<ColonyStructure>)` — by `BuildQueueSequence`
   - `OrderRouteStops(IEnumerable<RouteStop>)` — by `Sequence`
   - `OrderPlanStops(IEnumerable<DeliveryPlanStop>)` — by `Sequence`
   - `OrderSupplyChainStages(IEnumerable<SupplyChainStage>)` — by `Sequence`
   - `OrderColonies(IEnumerable<Colony>)` — by `SystemName`, `PlanetName`, `ColonyName`
   - `OrderBlueprints(IEnumerable<Blueprint>)` — by `ExtendedName`
   - `OrderPlayerProfiles(IEnumerable<PlayerProfile>)` — by `Name`
   - `OrderSurveys(IEnumerable<Survey>)` — by `PlanetName`, `GameID`
   - `OrderCommodityRequests(IEnumerable<CommodityRequested>)` — by `Name`
   - `OrderCommodityRequestsByNeedBy(IEnumerable<CommodityRequested>)` — by `NeedBy` (DateTime, ascending)
   - `OrderDeliveryItems(IEnumerable<DeliveryItem>)` — by `Name`
   - `OrderBuildItems(IEnumerable<BuildItem>)` — by `ItemName`
   - `OrderAsteroidReserves(IEnumerable<AsteroidReserve>)` — by `ResourceName`
   - `OrderComponents(IEnumerable<ShipComponentSlot>)` — by `SlotType`, `SlotIndex`
   - Additional methods for remaining registry entries as needed
7. WHEN a new Ordered_Collection is added to the Sort Key Registry, THE developer SHALL add a corresponding Sort_Helper method before the code is merged.
8. THE steering rule (Requirement 5) SHALL instruct that consumers must use Sort_Helper methods rather than inline `.OrderBy()` calls.

### Requirement 9: Automated Audit Check for Inline Sorting

**User Story:** As a developer, I want an automated audit check that detects inline sorting of data model collections, so that violations of the centralized sort helper rule are caught before they ship.

#### Acceptance Criteria

1. THE project SHALL include an audit script (e.g. `inline-sort-check.js`) in `.kiro/tools/` that scans all `.cs` files for inline `.OrderBy()`, `.ThenBy()`, `.Sort()`, and `.OrderByDescending()` calls on data model collection types.
2. THE audit script SHALL report any inline sort call that is NOT inside `CollectionSortHelper.cs` or `SerializationSorter.cs` as a finding.
3. THE audit script SHALL be integrated into `audit.js` so it runs as part of the standard audit process.
4. THE audit script SHALL have zero findings on a compliant codebase (all inline sorts refactored to use `CollectionSortHelper`).
