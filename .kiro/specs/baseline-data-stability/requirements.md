# Requirements Document

## Introduction

BaselineData.json ships with the application and is modified by user imports, but has no versioning, no deterministic identity scheme, and embeds game-derived data in C# code that users cannot fix. This feature stabilizes baseline data by introducing deterministic UUIDs for global blueprints, a versioned migration framework, an idempotent rename table, a LegacyUUID safety net, and externalizing code-embedded game data into BaselineData.json. The goal is to make BaselineData.json upgradable by the developer, consistent across machines, and preserving of user data — all without breaking existing save files.

## Glossary

- **Tracker**: The OE2EmpireTracker application
- **BaselineRoot**: The root JSON object deserialized from BaselineData.json (contains ShipClass, BlueprintType, Blueprint, TechLevel arrays, and new sections added by this feature)
- **PlayerRoot**: The root JSON object deserialized from PlayerData.json (contains PlayerProfile, Blueprint, Survey, Colony, DeliveryRoute, DeliveryPlan arrays)
- **Global_Blueprint**: A Blueprint stored in BaselineData.json (Evolution 0, shared across all players)
- **Player_Blueprint**: A Blueprint stored in PlayerData.json (owned by a specific player, may have any Evolution)
- **Dedup_Key**: The composite identity of a Blueprint: Name + Evolution + BluePrintType + Class + TechLevel
- **Deterministic_UUID**: A UUID v5 value generated from the namespace `e0058083-0f64-b398-ed53-762f7d8b8eb2` and the Dedup_Key string `Name|Evolution|BluePrintType|Class|TechLevel`
- **UUID_Namespace**: The fixed UUID `e0058083-0f64-b398-ed53-762f7d8b8eb2` used as the namespace for UUID v5 generation
- **DataVersion**: An integer field on BaselineRoot and PlayerRoot indicating the schema/migration version of the file
- **Migration**: A numbered, non-idempotent transformation applied once when DataVersion is below the target version
- **Rename_Entry**: A record in the idempotent rename table mapping an old blueprint name to a new blueprint name along with the other Dedup_Key fields
- **LegacyUUID**: A write-once field on Blueprint that stores the original random UUID before migration to a Deterministic_UUID
- **RemapUUID**: A utility that walks all UUID references (Blueprint.UUID, Blueprint.BaseBlueprintUUID, ColonyStructure.FlatpackBlueprintUUID, ColonyStructure.ResearchingBlueprintUUID, ColonyStructure.ManufacturingBlueprintUUID) and replaces old UUID values with new UUID values
- **EmpireContext**: The singleton that loads and manages BaselineData.json
- **PlayerContext**: The singleton that loads and manages PlayerData.json
- **GameConstants_Section**: A JSON object in BaselineData.json containing game-derived numeric constants (RefiningBaseRate, CommoditiesPerCycle, CommodityCycleSeconds, StructureCap, WorkerVolume)
- **Commodity_Array**: A JSON array in BaselineData.json containing commodity definitions with Name, CommodityGroup, CommodityIndustry, and ConstructionResources
- **RefiningRecipe_Array**: A JSON array in BaselineData.json containing synthetic refining recipes with InputResource, InputPurity, OutputResource, ConsumeRate, ProduceRate, and Tier
- **ResearchTime_Array**: A JSON array in BaselineData.json containing evolution-to-research-time mappings

## Requirements

### Requirement 1: Deterministic UUID Generation for Global Blueprints

**User Story:** As a developer, I want global blueprints to have deterministic UUIDs derived from their Dedup_Key, so that the same blueprint has the same UUID on every user's machine and across application upgrades.

#### Acceptance Criteria

1. WHEN a Global_Blueprint is created or imported with Evolution equal to 0, THE Tracker SHALL generate a Deterministic_UUID using UUID v5 with the UUID_Namespace `e0058083-0f64-b398-ed53-762f7d8b8eb2` and the input string `Name|Evolution|BluePrintType|Class|TechLevel`.
2. WHEN a Player_Blueprint is created or imported with Evolution greater than 0, THE Tracker SHALL assign a random UUID (Guid.NewGuid).
3. THE Tracker SHALL produce identical Deterministic_UUID values for identical Dedup_Key inputs across all machines and application versions.
4. WHEN two Global_Blueprints have different Dedup_Key values, THE Tracker SHALL generate different Deterministic_UUID values for each.

### Requirement 2: DataVersion Field on Persistence Roots

**User Story:** As a developer, I want both PlayerData.json and BaselineData.json to carry a DataVersion integer, so that the application can detect which migrations need to run on load.

#### Acceptance Criteria

1. THE BaselineRoot SHALL include an integer field named DataVersion with a default value of 0.
2. THE PlayerRoot SHALL include an integer field named DataVersion with a default value of 0.
3. WHEN a JSON file is loaded that does not contain a DataVersion field, THE Tracker SHALL treat the DataVersion as 0.
4. WHEN the Tracker saves BaselineData.json, THE Tracker SHALL write the current DataVersion value to the file.
5. WHEN the Tracker saves PlayerData.json, THE Tracker SHALL write the current DataVersion value to the file.

### Requirement 3: Versioned Migration Framework

**User Story:** As a developer, I want a sequential migration framework gated by DataVersion, so that non-idempotent changes (schema changes, initial UUID migration) run exactly once per version bump.

#### Acceptance Criteria

1. WHEN the Tracker loads a data file with DataVersion less than the application's current target version, THE Tracker SHALL execute each pending Migration in sequential order from (DataVersion + 1) through the target version.
2. WHEN a Migration completes, THE Tracker SHALL increment the DataVersion to that Migration's version number before proceeding to the next Migration.
3. WHEN the Tracker loads a data file with DataVersion equal to the target version, THE Tracker SHALL skip all Migrations.
4. THE Tracker SHALL execute Migration version 1 (initial random-UUID-to-Deterministic_UUID migration) as the first versioned Migration for both BaselineData.json and PlayerData.json.
5. WHEN Migration version 1 runs, THE Tracker SHALL compute the Deterministic_UUID for each Global_Blueprint, call RemapUUID to update all references from the old UUID to the Deterministic_UUID, and store the old UUID in the LegacyUUID field.
6. WHEN the Tracker completes all pending Migrations, THE Tracker SHALL persist the updated DataVersion to the file.

### Requirement 4: Generic RemapUUID Utility

**User Story:** As a developer, I want a single reusable utility that remaps all UUID references across the data model, so that migration and rename code does not duplicate reference-walking logic.

#### Acceptance Criteria

1. WHEN RemapUUID is called with an old UUID and a new UUID, THE Tracker SHALL update Blueprint.UUID on any Global_Blueprint or Player_Blueprint whose UUID matches the old UUID.
2. WHEN RemapUUID is called, THE Tracker SHALL update Blueprint.BaseBlueprintUUID on any Blueprint (global or player) whose BaseBlueprintUUID matches the old UUID.
3. WHEN RemapUUID is called, THE Tracker SHALL update ColonyStructure.FlatpackBlueprintUUID on any ColonyStructure whose FlatpackBlueprintUUID matches the old UUID.
4. WHEN RemapUUID is called, THE Tracker SHALL update ColonyStructure.ResearchingBlueprintUUID on any ColonyStructure whose ResearchingBlueprintUUID matches the old UUID.
5. WHEN RemapUUID is called, THE Tracker SHALL update ColonyStructure.ManufacturingBlueprintUUID on any ColonyStructure whose ManufacturingBlueprintUUID matches the old UUID.
6. WHEN RemapUUID is called with an old UUID that does not match any reference, THE Tracker SHALL make no changes (no-op).

### Requirement 5: Idempotent Rename Table

**User Story:** As a developer, I want an append-only rename table that runs on every load, so that blueprint renames are applied safely regardless of which version the user is upgrading from.

#### Acceptance Criteria

1. THE Tracker SHALL maintain a flat, append-only list of Rename_Entry records, each containing OldName, NewName, Evolution, BluePrintType, Class, and TechLevel.
2. WHEN the Tracker loads data, THE Tracker SHALL process every Rename_Entry by computing the Deterministic_UUID from the old Dedup_Key and the Deterministic_UUID from the new Dedup_Key.
3. WHEN a Blueprint with the old Deterministic_UUID exists, THE Tracker SHALL update the Blueprint's Name to NewName, update the Blueprint's UUID to the new Deterministic_UUID, and call RemapUUID to update all references.
4. WHEN a Blueprint with the old Deterministic_UUID does not exist, THE Tracker SHALL skip that Rename_Entry without error (no-op).
5. THE Tracker SHALL execute the rename table on every load, before versioned Migrations.
6. WHEN the same Rename_Entry is processed on consecutive loads, THE Tracker SHALL produce no changes on the second and subsequent loads (idempotent behavior).

### Requirement 6: LegacyUUID Field

**User Story:** As a developer, I want a LegacyUUID field on Blueprint that preserves the original random UUID after migration, so that external references and debugging have a record of the old identity.

#### Acceptance Criteria

1. THE Blueprint model SHALL include a nullable string field named LegacyUUID with a default value of null.
2. WHEN Migration version 1 replaces a Global_Blueprint's random UUID with a Deterministic_UUID, THE Tracker SHALL set LegacyUUID to the old random UUID value.
3. WHILE a Blueprint's LegacyUUID is non-null, THE Tracker SHALL preserve the LegacyUUID value across all subsequent saves and loads (read-only after initial write).
4. THE Tracker SHALL serialize LegacyUUID to JSON and deserialize LegacyUUID from JSON.
5. THE Tracker SHALL NOT use LegacyUUID for any lookup or matching operations during normal application use.

### Requirement 7: Externalize GameConstants to BaselineData.json

**User Story:** As a developer, I want the five game-derived constants (RefiningBaseRate, CommoditiesPerCycle, CommodityCycleSeconds, StructureCap, WorkerVolume) stored in BaselineData.json, so that users can fix game balance changes without code modifications.

#### Acceptance Criteria

1. THE BaselineRoot SHALL include a GameConstants_Section JSON object containing integer fields RefiningBaseRate, CommoditiesPerCycle, StructureCap, and CommodityCycleSeconds, and a decimal field WorkerVolume.
2. WHEN the Tracker loads BaselineData.json, THE Tracker SHALL read the GameConstants_Section and make the values available to all code that currently references the hardcoded GameConstants fields.
3. WHEN the GameConstants_Section is missing from BaselineData.json, THE Tracker SHALL use the current hardcoded default values (RefiningBaseRate=25, CommoditiesPerCycle=10, CommodityCycleSeconds=600, StructureCap=65, WorkerVolume=50).
4. THE Tracker SHALL continue to define internal constants (SecondsPerHour, PropBuilt, PropStaged, PropOnline, StatusActual, StatusIdeal, PurityRefined) in code, not in BaselineData.json.

### Requirement 8: Externalize Commodities to BaselineData.json

**User Story:** As a developer, I want the commodity definitions (Name, CommodityGroup, CommodityIndustry, ConstructionResources) stored in BaselineData.json, so that users can fix commodity data without code modifications and the developer can push updates via the migration framework.

#### Acceptance Criteria

1. THE BaselineRoot SHALL include a Commodity_Array containing commodity objects, each with string Name, string CommodityGroup, string CommodityIndustry, and a dictionary of ConstructionResources (resource name to quantity string).
2. WHEN the Tracker loads BaselineData.json, THE Tracker SHALL deserialize the Commodity_Array and make the commodity data available to all code that currently references the hardcoded Commodity list.
3. THE Tracker SHALL load all 209 commodities from the Commodity_Array with their CommodityGroup, CommodityIndustry, and ConstructionResources values matching the current hardcoded data.
4. IF the Commodity_Array is missing from BaselineData.json, THEN THE Tracker SHALL fall back to the current hardcoded commodity list.

### Requirement 9: Externalize RefiningRecipes to BaselineData.json

**User Story:** As a developer, I want the synthetic refining recipes stored in BaselineData.json, so that users can fix recipe data without code modifications and the developer can push updates via the migration framework.

#### Acceptance Criteria

1. THE BaselineRoot SHALL include a RefiningRecipe_Array containing recipe objects, each with string InputResource, string InputPurity, string OutputResource, integer ConsumeRate, integer ProduceRate, and integer Tier.
2. WHEN the Tracker loads BaselineData.json, THE Tracker SHALL deserialize the RefiningRecipe_Array and make the recipe data available to all code that currently references the hardcoded RefiningRecipes list.
3. THE Tracker SHALL load all 6 synthetic refining recipes from the RefiningRecipe_Array with values matching the current hardcoded data.
4. IF the RefiningRecipe_Array is missing from BaselineData.json, THEN THE Tracker SHALL fall back to the current hardcoded refining recipe list.

### Requirement 10: Externalize ResearchTimeLookup to BaselineData.json

**User Story:** As a developer, I want the research time lookup table stored in BaselineData.json, so that users can fix research time data without code modifications and the developer can push updates via the migration framework.

#### Acceptance Criteria

1. THE BaselineRoot SHALL include a ResearchTime_Array containing objects, each with integer Evolution and long ResearchTimeSeconds.
2. WHEN the Tracker loads BaselineData.json, THE Tracker SHALL deserialize the ResearchTime_Array and make the lookup data available to all code that currently references the hardcoded ResearchTimeLookup.
3. THE Tracker SHALL load all 15 evolution-to-time entries from the ResearchTime_Array with values matching the current hardcoded data.
4. IF the ResearchTime_Array is missing from BaselineData.json, THEN THE Tracker SHALL fall back to the current hardcoded research time lookup.

### Requirement 11: Backward Compatibility

**User Story:** As a user, I want my existing PlayerData.json and BaselineData.json files to load and migrate cleanly when I upgrade, so that I do not lose any data.

#### Acceptance Criteria

1. WHEN the Tracker loads a BaselineData.json file that predates this feature (no DataVersion, no GameConstants_Section, no Commodity_Array, no RefiningRecipe_Array, no ResearchTime_Array), THE Tracker SHALL load the file, apply default values for missing sections, and run all pending Migrations.
2. WHEN the Tracker loads a PlayerData.json file that predates this feature (no DataVersion), THE Tracker SHALL load the file, treat DataVersion as 0, and run all pending Migrations.
3. WHEN Migration version 1 runs on existing data, THE Tracker SHALL remap all Global_Blueprint UUIDs from random to deterministic without losing any Blueprint data, colony structure references, or evolution chain references.
4. WHEN the Tracker completes migration of existing data, THE Tracker SHALL persist the migrated data so that subsequent loads do not re-run completed Migrations.
5. THE Tracker SHALL preserve all Player_Blueprint random UUIDs unchanged during migration (player blueprints are not subject to deterministic UUID generation).

### Requirement 12: Deterministic UUID Scope — Player Blueprints Excluded

**User Story:** As a developer, I want player blueprints to keep random UUIDs, so that research branching (where the same Dedup_Key can produce multiple distinct blueprints with different properties) is correctly supported.

#### Acceptance Criteria

1. WHEN a Blueprint has a non-empty OwnerUUID (player-owned), THE Tracker SHALL retain the Blueprint's existing random UUID and SHALL NOT replace the UUID with a Deterministic_UUID.
2. WHEN a Blueprint has Evolution greater than 0 and is stored in PlayerData.json, THE Tracker SHALL assign a random UUID on creation.
3. THE Tracker SHALL NOT apply the idempotent rename table to Player_Blueprints (rename entries target Global_Blueprints by computing Deterministic_UUIDs from Dedup_Keys, which are non-unique for player blueprints).


### Requirement 13: Replace double with decimal for Game Data Numeric Types

**User Story:** As a developer, I want all game-derived numeric values to use `decimal` instead of `double`, so that base-10 game values (volumes, power, habitation, food, entertainment, warehouse capacity, mining rates, refining rates) are represented exactly without binary floating-point precision errors.

#### Acceptance Criteria

1. THE Tracker SHALL use `decimal` for all numeric fields on ColonyStructureStatus (PowerProvided, PowerRequired, HabitationProvision, HabitationRequired, FoodProvision, FoodRequired, EntertainmentProvided, EntertainmentRequired, WarehouseCapacity, WarehouseRequired).
2. THE Tracker SHALL use `decimal` for Item.Volume.
3. THE Tracker SHALL use `decimal` for ItemProperty.BaseValue and ItemProperty.AdjustedValue.
4. THE Tracker SHALL use `decimal` for PropertyBag.getDouble (renamed to getDecimal) and the corresponding setProperty overload.
5. THE Tracker SHALL use `decimal` for all game-data calculation variables in ColonyStatusCalculator, Colony.ProcessColony, ColonyBootstrap, ColonyInactivityCollector, and EvolutionChainService.
6. THE Tracker SHALL use `decimal` for all GameConstants_Section numeric fields in BaselineData.json (RefiningBaseRate, CommoditiesPerCycle, CommodityCycleSeconds, StructureCap, WorkerVolume).
7. THE Tracker SHALL continue to use `double` for system performance metrics (memory usage, CPU percentage) in MainWindow, as these are system measurements where binary floating-point is appropriate.
8. THE Tracker SHALL NOT use `float` anywhere in the codebase for game data.
9. WHEN existing JSON files contain double-serialized values, THE Tracker SHALL deserialize them correctly into `decimal` fields (Newtonsoft.Json handles this automatically).
