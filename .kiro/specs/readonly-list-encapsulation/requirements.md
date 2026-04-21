# Requirements Document

## Introduction

Encapsulate all entity lists on PlayerContext and EmpireContext behind `IReadOnlyList<T>` public properties with private `List<T>` backing fields. All mutations (Add/Remove) go through dedicated methods that maintain UUID lookup caches inline at O(1) cost, eliminating full cache invalidation and fallback linear scans. BindingSource objects continue to wrap the underlying `List<T>` for WinForms data binding. All existing callers that directly call `.Add()` or `.Remove()` on the lists are migrated to the new mutation methods.

## Glossary

- **PlayerContext**: Singleton service that holds all player-specific entity lists (blueprints, surveys, colonies, ships, etc.) and persists them to PlayerData.json
- **EmpireContext**: Singleton service that holds shared game data lists (blueprint types, ship classes, tech levels, resources, etc.) and persists them to BaselineData.json
- **Entity_List**: A `List<T>` field on PlayerContext or EmpireContext that stores a collection of domain model objects
- **Backing_Field**: A private `List<T>` field that stores the actual mutable list data, not directly accessible to consumers
- **ReadOnly_Property**: A public `IReadOnlyList<T>` property that exposes the backing field as read-only to consumers
- **UUID_Cache**: A `Dictionary<string, T>` that maps entity UUIDs to entity objects for O(1) lookup
- **Mutation_Method**: A public method on PlayerContext or EmpireContext (e.g., `AddBlueprint`, `RemoveBlueprint`) that modifies the backing field and maintains associated caches
- **BindingSource**: A WinForms `System.Windows.Forms.BindingSource` component that wraps a `List<T>` to enable data binding in UI controls
- **Fallback_Linear_Scan**: A `FirstOrDefault` LINQ query used as a safety net in Find methods when the cache misses — architecturally incorrect and to be removed
- **WriteContext**: The serialization method on PlayerContext/EmpireContext that persists all lists to JSON files using `.ToArray()`

## Requirements

### Requirement 1: Read-Only Public List Properties on PlayerContext

**User Story:** As a developer, I want all entity lists on PlayerContext exposed as `IReadOnlyList<T>` properties, so that no consumer can directly mutate the lists and bypass cache maintenance.

#### Acceptance Criteria

1. THE PlayerContext SHALL expose each entity list (BlueprintList, SurveyList, ColonyList, DeliveryRouteList, DeliveryPlanList, PricingPlanList, BuildPlanList, ShipTemplateList, ShipList, StationList, MarketListingList, MarketTransactionList, StockPlanList, StockProfileList, SupplyChainList, WarehouseOverflowRuleList, FactionList, ExternalCharacterList, AsteroidList, PlayerProfileList) as a public `IReadOnlyList<T>` property backed by a private `List<T>` field
2. THE PlayerContext SHALL prevent external code from calling `.Add()`, `.Remove()`, `.Clear()`, or any other mutating method directly on the list properties
3. THE PlayerContext SHALL maintain the same property names as the current public fields so that read-only consumers (LINQ queries, iteration, indexing, `.Count`) require no code changes

### Requirement 2: Read-Only Public List Properties on EmpireContext

**User Story:** As a developer, I want all entity lists on EmpireContext exposed as `IReadOnlyList<T>` properties, so that shared game data lists are protected from uncontrolled mutation.

#### Acceptance Criteria

1. THE EmpireContext SHALL expose each entity list (GlobalBlueprintList, BlueprintTypeList, ShipClassList, TechLevelList, EvolutionList, ResourceList, ResourceGroupList, ResourcePurityList, CommodityList) as a public `IReadOnlyList<T>` property backed by a private `List<T>` field
2. THE EmpireContext SHALL prevent external code from calling `.Add()`, `.Remove()`, `.Clear()`, or any other mutating method directly on the list properties
3. THE EmpireContext SHALL maintain the same property names as the current public fields so that read-only consumers require no code changes

### Requirement 3: Mutation Methods on PlayerContext

**User Story:** As a developer, I want dedicated Add/Remove methods on PlayerContext for each entity list, so that all mutations go through a single controlled path.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide an `Add{Entity}({Entity} item)` method for each entity list that adds the item to the backing list
2. THE PlayerContext SHALL provide a `Remove{Entity}({Entity} item)` method for each entity list that removes the item from the backing list
3. WHEN an Add method is called, THE PlayerContext SHALL add the item to the backing list within the existing `_listLock` synchronization
4. WHEN a Remove method is called, THE PlayerContext SHALL remove the item from the backing list within the existing `_listLock` synchronization
5. WHEN an Add method is called for an entity type that has a UUID cache, THE PlayerContext SHALL insert the item into the UUID cache dictionary inline (O(1) operation)
6. WHEN a Remove method is called for an entity type that has a UUID cache, THE PlayerContext SHALL remove the item from the UUID cache dictionary inline (O(1) operation)
7. WHEN an Add or Remove method is called for an entity type that has a BindingSource, THE PlayerContext SHALL call `ResetBindings(false)` on the BindingSource so that bound UI controls refresh

### Requirement 4: Mutation Methods on EmpireContext

**User Story:** As a developer, I want dedicated Add/Remove methods on EmpireContext for each entity list, so that shared game data mutations are controlled and caches are maintained.

#### Acceptance Criteria

1. THE EmpireContext SHALL provide an `Add{Entity}({Entity} item)` method for each entity list that adds the item to the backing list
2. THE EmpireContext SHALL provide a `Remove{Entity}({Entity} item)` method for each entity list that removes the item from the backing list
3. WHEN an Add method is called for an entity type that has a UUID cache (GlobalBlueprintList, CommodityList), THE EmpireContext SHALL insert the item into the cache dictionary inline (O(1) operation)
4. WHEN a Remove method is called for an entity type that has a UUID cache, THE EmpireContext SHALL remove the item from the cache dictionary inline (O(1) operation)
5. WHEN an Add or Remove method is called for an entity type that has a BindingSource, THE EmpireContext SHALL call `ResetBindings(false)` on the BindingSource so that bound UI controls refresh

### Requirement 5: Inline O(1) Cache Maintenance

**User Story:** As a developer, I want UUID caches maintained inline during Add/Remove operations, so that lookups remain O(1) without requiring full cache rebuilds.

#### Acceptance Criteria

1. WHEN an entity with a UUID is added via a mutation method, THE PlayerContext SHALL insert the entity into the corresponding UUID cache dictionary using the entity UUID as the key (O(1) dictionary insert)
2. WHEN an entity with a UUID is removed via a mutation method, THE PlayerContext SHALL remove the entity from the corresponding UUID cache dictionary using the entity UUID as the key (O(1) dictionary remove)
3. THE PlayerContext SHALL maintain UUID caches for all entity types that have a UUID property (Blueprint, Survey, Colony, Station, ShipTemplate, Ship, BuildPlan, Asteroid, Faction, MarketListing, PlayerProfile, DeliveryRoute, DeliveryPlan, PricingPlan, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule, ExternalCharacter)
4. IF the UUID cache has not been initialized when a mutation method is called, THEN THE PlayerContext SHALL initialize the cache from the full list before performing the inline update
5. THE EmpireContext SHALL apply the same inline cache maintenance for GlobalBlueprintList and CommodityList caches

### Requirement 6: Remove Fallback Linear Scans

**User Story:** As a developer, I want the fallback linear scans removed from FindBlueprint, FindSurvey, and FindColony, so that the architecture relies on correct O(1) cache maintenance rather than safety-net scans.

#### Acceptance Criteria

1. THE PlayerContext FindBlueprint method SHALL return the result from the UUID cache dictionary lookup only, without falling back to a `FirstOrDefault` linear scan of BlueprintList
2. THE PlayerContext FindSurvey method SHALL return the result from the UUID cache dictionary lookup only, without falling back to a `FirstOrDefault` linear scan of SurveyList
3. THE PlayerContext FindColony method SHALL return the result from the UUID cache dictionary lookup only, without falling back to a `FirstOrDefault` linear scan of ColonyList
4. WHEN a Find method cache lookup returns no match, THE PlayerContext SHALL return null (or delegate to EmpireContext for blueprints as currently done)

### Requirement 7: BindingSource Compatibility

**User Story:** As a developer, I want BindingSource objects to continue working with the encapsulated lists, so that WinForms data binding in forms is not broken.

#### Acceptance Criteria

1. THE PlayerContext SHALL continue to expose BindingSource objects (BindingSourcePlayerProfile, BindingSourceBlueprint, BindingSourceSurvey, BindingSourceColony) that wrap the private backing `List<T>` fields
2. THE EmpireContext SHALL continue to expose BindingSource objects (BindingSourceBlueprintType, BindingSourceShipClass, BindingSourceTechLevel, BindingSourceEvolution, BindingSourceResource, BindingSourceResourceGroup, BindingSourceResourcePurity) that wrap the private backing `List<T>` fields
3. THE BindingSource DataSource SHALL reference the same `List<T>` instance as the private backing field, so that mutations through Add/Remove methods are reflected in bound UI controls
4. WHEN a mutation method modifies a list that has an associated BindingSource, THE context SHALL call `ResetBindings(false)` on the BindingSource to notify bound controls of the change

### Requirement 8: Serialization Compatibility

**User Story:** As a developer, I want WriteContext serialization to continue working unchanged, so that data persistence is not affected by the encapsulation change.

#### Acceptance Criteria

1. THE PlayerContext WriteContext method SHALL continue to serialize all entity lists by calling `.ToArray()` on the backing list fields within the `_listLock`
2. THE EmpireContext WriteContext method SHALL continue to serialize all entity lists by calling `.ToArray()` on the backing list fields
3. THE serialization output format SHALL remain identical to the current format (no changes to PlayerRoot or BaselineRoot structures)

### Requirement 9: Caller Migration

**User Story:** As a developer, I want all existing code that directly calls `.Add()` or `.Remove()` on context lists to be migrated to the new mutation methods, so that no code bypasses the encapsulation.

#### Acceptance Criteria

1. THE codebase SHALL contain no direct `.Add()` calls on any PlayerContext or EmpireContext list property outside of the context classes themselves
2. THE codebase SHALL contain no direct `.Remove()` calls on any PlayerContext or EmpireContext list property outside of the context classes themselves
3. WHEN a form, parser, importer, migration, background processor, or test previously called `context.SomeList.Add(item)`, THE code SHALL be changed to call `context.AddSomeEntity(item)` instead
4. WHEN a form, parser, importer, migration, background processor, or test previously called `context.SomeList.Remove(item)`, THE code SHALL be changed to call `context.RemoveSomeEntity(item)` instead

### Requirement 10: Invalidation Method Cleanup

**User Story:** As a developer, I want the Invalidate cache methods to remain available for bulk operations (like Init methods during load), but inline maintenance to be the primary cache update path during normal operation.

#### Acceptance Criteria

1. THE PlayerContext SHALL retain Invalidate methods (InvalidateBlueprintCache, InvalidateSurveyCache, InvalidateColonyCache, etc.) for use during initialization and bulk reload operations
2. WHILE normal application operation is occurring (after initialization), THE mutation methods SHALL be the primary mechanism for cache updates, not Invalidate methods
3. THE Invalidate methods SHALL continue to set the cache dictionary to null, triggering a full rebuild on the next Find call

### Requirement 11: Test Migration

**User Story:** As a developer, I want all tests that directly manipulate context lists to be updated to use the new mutation methods, so that tests exercise the same code paths as production code.

#### Acceptance Criteria

1. THE test project SHALL contain no direct `.Add()` calls on any PlayerContext or EmpireContext list property
2. THE test project SHALL contain no direct `.Remove()` calls on any PlayerContext or EmpireContext list property
3. WHEN a test previously set up data by calling `context.SomeList.Add(item)`, THE test SHALL be changed to call `context.AddSomeEntity(item)` instead
4. WHEN a test previously cleaned up data by calling `context.SomeList.Remove(item)`, THE test SHALL be changed to call `context.RemoveSomeEntity(item)` instead

### Requirement 12: Snapshot Methods Compatibility

**User Story:** As a developer, I want the existing Snapshot methods to continue working, so that thread-safe list copies remain available.

#### Acceptance Criteria

1. THE PlayerContext Snapshot methods (SnapshotColonyList, SnapshotBuildPlanList, SnapshotShipTemplateList, etc.) SHALL continue to return a new `List<T>` copy of the backing field within the `_listLock`
2. THE Snapshot methods SHALL read from the private backing field, not from the public `IReadOnlyList<T>` property

### Requirement 13: Derived Cache Invalidation on Mutation

**User Story:** As a developer, I want derived caches (BlueprintTypeCountCache, BuildItemIndexes, AllBlueprintsCache) to be invalidated when their source lists are mutated, so that stale derived data is not served.

#### Acceptance Criteria

1. WHEN AddBlueprint or RemoveBlueprint is called, THE PlayerContext SHALL invalidate the BlueprintTypeCountCache and AllBlueprintsCache
2. WHEN AddBuildPlan or RemoveBuildPlan is called, THE PlayerContext SHALL invalidate the BuildItemIndexes
3. THE derived cache invalidation SHALL occur within the same lock scope as the list mutation to prevent race conditions
