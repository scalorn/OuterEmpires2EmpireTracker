# Requirements Document

## Introduction

The application persists player and baseline game data to JSON files (PlayerData.json and BaselineData.json). These files contain many arrays of entities whose order is currently non-deterministic — items appear in whatever order they happen to be in the in-memory lists at serialization time. When entities are added, removed, or modified, the resulting JSON diff shows large spurious changes because array elements shift positions even though only one entity actually changed. This feature introduces deterministic sorting of all entity arrays at serialization time so that git diffs reflect only the actual data changes.

## Glossary

- **Serializer**: The component responsible for converting in-memory data structures to JSON text via `JsonConvert.SerializeObject`, invoked by `PlayerContext.WriteContext()` and `EmpireContext.WriteContext()`
- **PlayerRoot**: The root serialization object for PlayerData.json, containing all player-specific entity arrays
- **BaselineRoot**: The root serialization object for BaselineData.json, containing shared game data arrays
- **Top_Level_Array**: An entity array that is a direct property of PlayerRoot or BaselineRoot (e.g. `PlayerRoot.Colony`, `BaselineRoot.BlueprintType`)
- **Nested_Array**: An entity array that is a property of an entity within a Top_Level_Array (e.g. `Colony.Structures`, `DeliveryPlan.Stops`, `ShipTemplate.Components`)
- **Sort_Key**: The property used to order entities within an array — typically UUID for entities that have one, or a composite of identifying fields for entities that do not
- **ItemBag**: A dictionary-keyed container (`Dictionary<string, Item>`) with a custom JSON converter that serializes entries as JSON object properties keyed by UUID
- **Sequence_Field**: An integer field on ordered entities (e.g. `RouteStop.Sequence`, `DeliveryPlanStop.Sequence`, `ColonyStructure.DisplaySequence`) that defines a user-meaningful ordering

## Requirements

### Requirement 1: Sort Top-Level PlayerRoot Arrays by UUID

**User Story:** As a developer, I want all top-level entity arrays in PlayerRoot to be sorted by UUID before serialization, so that git diffs only show actual data changes.

#### Acceptance Criteria

1. WHEN PlayerContext.WriteContext() serializes PlayerRoot, THE Serializer SHALL sort each of the following arrays by UUID in ascending ordinal string order before writing JSON: PlayerProfile, Blueprint, Survey, Colony, DeliveryRoute, DeliveryPlan, PricingPlan, BuildPlan, ShipTemplate, Ship, Station, MarketListing, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule, Faction, ExternalCharacter, Asteroid
2. WHEN two entities in the same array have identical UUID values, THE Serializer SHALL maintain their relative order (stable sort)

### Requirement 2: Sort Top-Level BaselineRoot Arrays by Primary Key

**User Story:** As a developer, I want all top-level entity arrays in BaselineRoot to be sorted by their primary key before serialization, so that baseline data diffs are minimal.

#### Acceptance Criteria

1. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the BlueprintType array by Id in ascending ordinal string order
2. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the Blueprint array by UUID in ascending ordinal string order
3. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the ShipClass array by Id in ascending numeric order
4. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the TechLevel array by Id in ascending ordinal string order
5. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the Commodity array by ID in ascending ordinal string order
6. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the RefiningRecipe array by OutputResource in ascending ordinal string order
7. WHEN EmpireContext.WriteContext() serializes BaselineRoot, THE Serializer SHALL sort the ResearchTime array by BlueprintType in ascending ordinal string order

### Requirement 3: Sort Nested Arrays Within Colony

**User Story:** As a developer, I want nested arrays within each Colony to be sorted deterministically, so that colony diffs are minimal.

#### Acceptance Criteria

1. WHEN a Colony is serialized, THE Serializer SHALL sort the Structures list by UUID in ascending ordinal string order
2. WHEN a Colony is serialized, THE Serializer SHALL sort the Commodities list by Name in ascending ordinal string order

### Requirement 4: Sort Nested Arrays Within DeliveryRoute

**User Story:** As a developer, I want delivery route stops to be sorted by their Sequence field, so that route diffs are minimal.

#### Acceptance Criteria

1. WHEN a DeliveryRoute is serialized, THE Serializer SHALL sort the Stops list by Sequence in ascending numeric order

### Requirement 5: Sort Nested Arrays Within DeliveryPlan

**User Story:** As a developer, I want delivery plan stops and their item lists to be sorted deterministically, so that delivery plan diffs are minimal.

#### Acceptance Criteria

1. WHEN a DeliveryPlan is serialized, THE Serializer SHALL sort the Stops list by Sequence in ascending numeric order
2. WHEN a DeliveryPlanStop is serialized, THE Serializer SHALL sort the DropOff list by Name in ascending ordinal string order
3. WHEN a DeliveryPlanStop is serialized, THE Serializer SHALL sort the PickUp list by Name in ascending ordinal string order

### Requirement 6: Sort Nested Arrays Within BuildPlan

**User Story:** As a developer, I want build plan items to be sorted deterministically, so that build plan diffs are minimal.

#### Acceptance Criteria

1. WHEN a BuildPlan is serialized, THE Serializer SHALL sort the Items list by UUID in ascending ordinal string order

### Requirement 7: Sort Nested Arrays Within ShipTemplate, Ship, and Station

**User Story:** As a developer, I want component slot lists to be sorted deterministically, so that ship and station diffs are minimal.

#### Acceptance Criteria

1. WHEN a ShipTemplate is serialized, THE Serializer SHALL sort the Components list by SlotType ascending then SlotIndex ascending
2. WHEN a Ship is serialized, THE Serializer SHALL sort the Components list by SlotType ascending then SlotIndex ascending
3. WHEN a Station is serialized, THE Serializer SHALL sort the Components list by SlotType ascending then SlotIndex ascending

### Requirement 8: Sort Nested Arrays Within StockPlan and StockProfile

**User Story:** As a developer, I want stock plan targets and stock profile entries to be sorted deterministically, so that stock diffs are minimal.

#### Acceptance Criteria

1. WHEN a StockPlan is serialized, THE Serializer SHALL sort the Targets list by UUID in ascending ordinal string order
2. WHEN a StockProfile is serialized, THE Serializer SHALL sort the Entries list by GroupID in ascending ordinal string order

### Requirement 9: Sort Nested Arrays Within SupplyChain

**User Story:** As a developer, I want supply chain stages to be sorted by their Sequence field, so that supply chain diffs are minimal.

#### Acceptance Criteria

1. WHEN a SupplyChain is serialized, THE Serializer SHALL sort the Stages list by Sequence in ascending numeric order

### Requirement 10: Sort Nested Arrays Within Asteroid

**User Story:** As a developer, I want asteroid reserves to be sorted deterministically, so that asteroid diffs are minimal.

#### Acceptance Criteria

1. WHEN an Asteroid is serialized, THE Serializer SHALL sort the Reserves list by ResourceName ascending then Purity ascending (ordinal string order)

### Requirement 11: Sort ItemBag Entries by UUID Key

**User Story:** As a developer, I want ItemBag dictionary entries to be serialized in sorted key order, so that cargo and inventory diffs are minimal.

#### Acceptance Criteria

1. WHEN an ItemBag is serialized by ItemBagJSONConverter, THE Serializer SHALL iterate the Items dictionary in ascending ordinal string order of the UUID key
2. WHEN a Station Holds dictionary is serialized, THE Serializer SHALL iterate the Holds dictionary in ascending ordinal string order of the hold name key

### Requirement 12: Sort Dictionary Entries Deterministically

**User Story:** As a developer, I want all serialized dictionaries to have deterministic key order, so that dictionary diffs are minimal.

#### Acceptance Criteria

1. WHEN a PricingPlan is serialized, THE Serializer SHALL write the ResourcePrices dictionary entries in ascending ordinal string order of the key
2. WHEN a Survey is serialized, THE Serializer SHALL write the Properties dictionary entries in ascending ordinal string order of the key
3. WHEN a Survey is serialized, THE Serializer SHALL write the Resources dictionary entries in ascending ordinal string order of the key
4. WHEN a Blueprint is serialized, THE Serializer SHALL write the Resources dictionary entries in ascending ordinal string order of the key
5. WHEN a Commodity is serialized, THE Serializer SHALL write the ConstructionResources dictionary entries in ascending ordinal string order of the key
6. WHEN a PlayerProfile is serialized, THE Serializer SHALL write the Skills dictionary entries in ascending ordinal string order of the key

### Requirement 13: Preserve In-Memory Order

**User Story:** As a developer, I want the sorting to happen only at serialization time, so that in-memory list order and UI display order are not affected.

#### Acceptance Criteria

1. THE Serializer SHALL sort copies of the entity arrays, not the original in-memory lists
2. WHEN WriteContext() completes, THE PlayerContext SHALL retain the original in-memory list order unchanged
3. WHEN WriteContext() completes, THE EmpireContext SHALL retain the original in-memory list order unchanged

### Requirement 14: Handle Null and Empty Sort Keys

**User Story:** As a developer, I want the sort to handle null or empty sort keys gracefully, so that entities with missing keys do not cause errors.

#### Acceptance Criteria

1. IF an entity has a null or empty UUID, THEN THE Serializer SHALL sort that entity before entities with non-empty UUIDs (nulls first)
2. IF a nested entity has a null or empty sort key, THEN THE Serializer SHALL sort that entity before entities with non-empty sort keys (nulls first)
