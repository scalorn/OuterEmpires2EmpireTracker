# Requirements Document

## Introduction

When the game API asset sync encounters items in colony warehouses, cargo items for manufactured items (blueprints, ship parts) include full blueprint properties (stats, slot counts, defence values, etc.). Currently the sync only creates inventory Item objects in the ItemBag — it does not create or update Blueprint entities in the player's blueprint collection, nor does it link Survey items to existing Survey entities.

This feature extends the asset sync to:
1. Create or update Blueprint entities from blueprint items (typeC="Bp") with properties
2. Create or update Blueprint entities from ship part/hull items (typeC="S", with shipPartType="Hu" for hulls) whose properties represent the source blueprint stats
3. Link survey items (typeC="Sc") to existing Survey entities by parsing the planet name from the resource name
4. Set BaseItemTypeID on warehouse items to the blueprint UUID when a matching blueprint is found

## Glossary

- **Asset_Sync**: The process that fetches cargo items from the game API and merges them into local colony/station/ship ItemBag collections via AssetMergeService and ColonyMergeService.
- **Blueprint_Entity**: A Blueprint object stored in PlayerContext's blueprint list, containing full stats (PropertyBag), evolution, type classification, and resources.
- **Blueprint_Item**: A cargo item in a colony warehouse with typeC="Bp" that represents a blueprint in the player's inventory.
- **Ship_Part_Item**: A cargo item in a colony warehouse with typeC="S" that represents a manufactured ship component whose properties derive from the source blueprint. Hull items have typeC="S" with shipPartType="Hu".
- **Survey_Item**: A cargo item in a colony warehouse with typeC="Sc" that represents a survey report.
- **Survey_Entity**: A Survey object stored in PlayerContext's survey list, containing planet name, resources, and scan metadata.
- **PropertyBag**: A key-value store on Blueprint entities that holds blueprint stats (e.g. "Defence", "Slots", "Speed").
- **ItemProperties**: A list of ItemProperty objects on Item entities that stores the raw API property data (modTypeId, propertyName, propertyValue, unit).
- **Blueprint_Linkage_Service**: A new service responsible for creating/updating Blueprint entities from asset sync data and linking items to their source blueprints.
- **Survey_Linkage_Service**: A new service (or extension of Blueprint_Linkage_Service) responsible for linking Survey items to Survey entities.
- **Dedup_Key**: The combination of fields used to match an incoming blueprint to an existing one: Name + Evolution + BluePrintType + Class + TechLevel (as used by BlueprintService.FindUnambiguousMatch).
- **TypeC**: The type code field on API cargo items that classifies the item category (e.g. "Bp", "S", "Sc", "R", "C").
- **Property_Type_Registry**: A global collection stored in BaselineData.json (EmpireContext) that defines property type metadata: modTypeId, propertyName, friendlyPropertyName, unit, researchPositive, canResearch. Keyed by modTypeId.

## Requirements

### Requirement 1: Blueprint Creation from Blueprint Items

**User Story:** As a player, I want blueprint items in my colony warehouses to automatically create or update Blueprint entities in my blueprint collection, so that I have full blueprint stats available without manual HTML scanning.

#### Acceptance Criteria

1. WHEN the Asset_Sync encounters a Blueprint_Item (typeC="Bp") with a non-empty properties array in a colony warehouse, THE Blueprint_Linkage_Service SHALL create or update a Blueprint_Entity in the player's blueprint collection with the stats from the properties array.
2. WHEN a Blueprint_Item has properties, THE Blueprint_Linkage_Service SHALL map each ItemProperty to the Blueprint_Entity's PropertyBag using the propertyName as the key and propertyValue as the value.
3. WHEN a matching Blueprint_Entity already exists (matched by Dedup_Key), THE Blueprint_Linkage_Service SHALL update the existing entity's PropertyBag with the API-provided properties (API wins strategy).
4. WHEN no matching Blueprint_Entity exists and the item's evolution is greater than 0, THE Blueprint_Linkage_Service SHALL create a new Blueprint_Entity with a generated UUID, set its OwnerUUID to the current player, and add it to PlayerContext via AddBlueprint.
5. WHEN no matching Blueprint_Entity exists and the item's evolution is 0 (or null), THE Blueprint_Linkage_Service SHALL create a new Blueprint_Entity with a generated UUID, set its OwnerUUID to empty string, and add it to EmpireContext as a global blueprint.
6. WHEN a Blueprint_Item has an evolution field, THE Blueprint_Linkage_Service SHALL set the Blueprint_Entity's Evolution property to match.
7. WHEN a Blueprint_Item has an empty properties array, THE Blueprint_Linkage_Service SHALL skip blueprint creation (no-op for that item).


### Requirement 2: Blueprint Creation from Ship Part Items

**User Story:** As a player, I want manufactured ship parts and hulls in my colony warehouses to create or update the corresponding Blueprint entity, so that I can track the blueprint stats that produced each manufactured item.

#### Acceptance Criteria

1. WHEN the Asset_Sync encounters a Ship_Part_Item (typeC="S") with a non-empty properties array in a colony warehouse, THE Blueprint_Linkage_Service SHALL create or update a Blueprint_Entity representing the source blueprint that produced the item. Hull items are identified by shipPartType="Hu" on the same typeC="S" code.
2. WHEN a Ship_Part_Item has a shipPartType field, THE Blueprint_Linkage_Service SHALL use the shipPartType combined with the item's resourceName to infer the BluePrintType classification for the Blueprint_Entity.
3. WHEN a matching Blueprint_Entity already exists (matched by Dedup_Key using the item's resourceName as the blueprint Name), THE Blueprint_Linkage_Service SHALL update the existing entity's PropertyBag with the API-provided properties.
4. WHEN no matching Blueprint_Entity exists for a Ship_Part_Item, THE Blueprint_Linkage_Service SHALL create a new Blueprint_Entity with the item's resourceName as the Name, the item's evolution as Evolution, and the inferred BluePrintType.
5. WHEN a Ship_Part_Item has an empty properties array, THE Blueprint_Linkage_Service SHALL skip blueprint creation for that item.

### Requirement 3: Blueprint Type Classification

**User Story:** As a player, I want blueprints created from API data to have the correct BluePrintType classification, so that they appear in the correct category in the blueprint management UI.

#### Acceptance Criteria

1. WHEN a Blueprint_Item (typeC="Bp") has a shipPartType field value of "Hu", THE Blueprint_Linkage_Service SHALL classify the Blueprint_Entity with a hull-category BluePrintType (e.g. "Hull/{size}").
2. WHEN a Ship_Part_Item has a shipPartType field, THE Blueprint_Linkage_Service SHALL map the shipPartType code to the corresponding BlueprintTypes constant (e.g. "Sh" maps to Shield, "Re" maps to Reactor, "Wp" maps to a weapon type).
3. WHEN a Blueprint_Item has no shipPartType and its resourceName contains "Flatpack", THE Blueprint_Linkage_Service SHALL classify it as a Flatpacks/ type blueprint.
4. IF the shipPartType code cannot be mapped to a known BlueprintTypes constant, THEN THE Blueprint_Linkage_Service SHALL log a warning and set BluePrintType to the raw shipPartType value as a fallback.


### Requirement 4: Survey Item Linkage

**User Story:** As a player, I want survey items in my colony warehouses to be linked to existing Survey entities, so that I can see which surveys I have in storage and trace them back to their scan data.

#### Acceptance Criteria

1. WHEN the Asset_Sync encounters a Survey_Item (typeC="Sc") in a colony warehouse, THE Survey_Linkage_Service SHALL parse the planet name from the resourceName using the format "Survey Report: {PlanetName} ({HexCode})".
2. WHEN a Survey_Entity exists with a matching PlanetName (case-insensitive), THE Survey_Linkage_Service SHALL set the Survey_Item's BaseItemTypeID to the Survey_Entity's UUID.
3. WHEN no Survey_Entity exists with a matching PlanetName, THE Survey_Linkage_Service SHALL create a stub Survey_Entity with the parsed PlanetName, a generated UUID, the current player as OwnerUUID, and add it to PlayerContext.
4. WHEN the resourceName does not match the expected format "Survey Report: {PlanetName} ({HexCode})", THE Survey_Linkage_Service SHALL log a warning and skip linkage for that item.
5. WHEN a stub Survey_Entity is created, THE Survey_Linkage_Service SHALL set its SurveyID to the parsed hex code from the resourceName.

### Requirement 5: Item-to-Blueprint Linkage via BaseItemTypeID

**User Story:** As a player, I want warehouse items to reference their source blueprint via BaseItemTypeID, so that the tool can trace manufactured items back to the blueprint that created them.

#### Acceptance Criteria

1. WHEN the Blueprint_Linkage_Service creates or updates a Blueprint_Entity from a Blueprint_Item, THE Blueprint_Linkage_Service SHALL set the Blueprint_Item's BaseItemTypeID to the Blueprint_Entity's UUID.
2. WHEN the Blueprint_Linkage_Service creates or updates a Blueprint_Entity from a Ship_Part_Item, THE Blueprint_Linkage_Service SHALL set the Ship_Part_Item's BaseItemTypeID to the Blueprint_Entity's UUID.
3. WHEN a Blueprint_Item or Ship_Part_Item has an empty properties array (no blueprint created), THE Asset_Sync SHALL retain the existing BaseItemTypeID behavior (set to the item's resourceName).

### Requirement 6: Coexistence with HTML Blueprint Import

**User Story:** As a player, I want the API-based blueprint sync to work alongside the existing HTML-based blueprint scanner, so that I retain all blueprint data regardless of source.

#### Acceptance Criteria

1. WHEN the Blueprint_Linkage_Service finds an existing Blueprint_Entity via Dedup_Key matching, THE Blueprint_Linkage_Service SHALL update its PropertyBag with API data without removing fields not present in the API response (additive merge for properties the API provides).
2. THE Blueprint_Linkage_Service SHALL preserve existing Blueprint_Entity fields that the API does not provide (Resources dictionary, CopyCost, NickName, BaseBlueprintUUID).
3. WHEN both the HTML scanner and the API sync provide properties for the same Blueprint_Entity, THE Blueprint_Linkage_Service SHALL use the API values as authoritative (API wins).


### Requirement 7: Property Type Registry

**User Story:** As a player, I want the tool to learn property type metadata (friendly names, units, research direction, researchability) from the game API, so that the tool has accurate, up-to-date property definitions without hardcoding them.

#### Acceptance Criteria

1. WHEN the Blueprint_Linkage_Service processes a cargo item with properties, THE Blueprint_Linkage_Service SHALL extract property type metadata (modTypeId, propertyName, friendlyPropertyName, unit, researchPositive, canResearch) from each property and store it in a global Property_Type_Registry.
2. THE Property_Type_Registry SHALL be stored in BaselineData.json as part of EmpireContext (global data, not per-player).
3. THE Property_Type_Registry SHALL use modTypeId as the primary key for each property type definition.
4. WHEN a property type with the same modTypeId already exists in the registry, THE Blueprint_Linkage_Service SHALL update its metadata if the API provides different values (API wins).
5. WHEN a property type is encountered for the first time (new modTypeId), THE Blueprint_Linkage_Service SHALL add it to the registry.
6. THE Property_Type_Registry SHALL store for each property type: modTypeId, propertyName (code name), friendlyPropertyName (display name), unit, researchPositive (bool), and canResearch (bool).

### Requirement 8: Enriched Property Data on Blueprint Entities

**User Story:** As a player, I want each blueprint's properties to include the original (base) property value alongside the current value, so that I can see how much research has improved each stat.

#### Acceptance Criteria

1. WHEN the Blueprint_Linkage_Service maps properties to a Blueprint_Entity's PropertyBag, THE Blueprint_Linkage_Service SHALL store both the current propertyValue and the originalPropertyValue for each property.
2. THE Blueprint_Entity's PropertyBag SHALL store property values keyed by propertyName, with the value containing both current and original values (e.g. as a structured entry or paired keys).
3. WHEN the API provides an originalPropertyValue of 0 and a propertyValue of 0, THE Blueprint_Linkage_Service SHALL still store the property (zero is a valid base value).

### Requirement 9: DTO Enrichment

**User Story:** As a developer, I want the GameApiAssetItemProperty DTO to capture all fields provided by the game API, so that no property metadata is lost during deserialization.

#### Acceptance Criteria

1. THE GameApiAssetItemProperty DTO SHALL include fields for: originalPropertyValue (decimal), researchPositive (bool), and canResearch (bool).
2. WHEN the API response includes these fields, THE DTO SHALL deserialize them correctly.
3. WHEN the API response omits these fields (e.g. station items with empty properties), THE DTO SHALL default to: originalPropertyValue=0, researchPositive=false, canResearch=false.

### Requirement 10: Integration Point

**User Story:** As a developer, I want the blueprint and survey linkage to run as part of the existing colony warehouse merge, so that no additional API calls or sync steps are needed.

#### Acceptance Criteria

1. WHEN ColonyMergeService.MergeWarehouse processes colony warehouse items, THE ColonyMergeService SHALL invoke the Blueprint_Linkage_Service for each item with typeC="Bp" or typeC="S" that has a non-empty properties array.
2. WHEN ColonyMergeService.MergeWarehouse processes colony warehouse items, THE ColonyMergeService SHALL invoke the Survey_Linkage_Service for each item with typeC="Sc".
3. THE Blueprint_Linkage_Service SHALL accept the current player's OwnerUUID as a parameter to correctly assign ownership of newly created Blueprint entities.
4. THE Survey_Linkage_Service SHALL accept the current player's OwnerUUID as a parameter to correctly assign ownership of newly created stub Survey entities.
5. THE Blueprint_Linkage_Service SHALL accept a reference to EmpireContext to update the global Property_Type_Registry during processing.

### Requirement 11: Idempotency

**User Story:** As a player, I want repeated asset syncs to produce the same result, so that running the sync multiple times does not create duplicate blueprints or surveys.

#### Acceptance Criteria

1. WHEN the Asset_Sync runs multiple times with the same API data, THE Blueprint_Linkage_Service SHALL produce exactly one Blueprint_Entity per unique Dedup_Key (no duplicates created on repeated syncs).
2. WHEN the Asset_Sync runs multiple times with the same survey items, THE Survey_Linkage_Service SHALL link to the same Survey_Entity each time (no duplicate stub surveys created).
3. WHEN a Blueprint_Entity's PropertyBag already contains the same values as the API provides, THE Blueprint_Linkage_Service SHALL report no changes (avoid unnecessary persistence writes).

### Requirement 12: Logging and Diagnostics

**User Story:** As a developer, I want the linkage process to log its actions, so that I can diagnose issues with blueprint creation and survey linking.

#### Acceptance Criteria

1. WHEN the Blueprint_Linkage_Service creates a new Blueprint_Entity, THE Blueprint_Linkage_Service SHALL log at Info level: the blueprint name, evolution, type, and generated UUID.
2. WHEN the Blueprint_Linkage_Service updates an existing Blueprint_Entity, THE Blueprint_Linkage_Service SHALL log at Info level: the blueprint name, UUID, and number of properties updated.
3. WHEN the Survey_Linkage_Service links a Survey_Item to an existing Survey_Entity, THE Survey_Linkage_Service SHALL log at Info level: the parsed planet name and matched Survey UUID.
4. WHEN the Survey_Linkage_Service creates a stub Survey_Entity, THE Survey_Linkage_Service SHALL log at Info level: the parsed planet name, hex code, and generated UUID.
5. IF the Blueprint_Linkage_Service encounters an error processing a single item, THEN THE Blueprint_Linkage_Service SHALL log the error at Error level and continue processing remaining items (no fail-fast).
