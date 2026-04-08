# Requirements Document

## Introduction

JSON data files (PlayerData.json, BaselineData.json, UIPreferences.json) grow large over time because every field is serialized, including fields that hold default values (null, empty strings, false booleans, zero integers/decimals). This feature configures Newtonsoft.Json serialization to skip default-valued fields on write, reducing file size without data loss. Deserialization already initializes omitted fields to their defaults via constructors and field initializers.

## Glossary

- **Serialization_Settings**: A shared `JsonSerializerSettings` instance configured with `DefaultValueHandling.Ignore` and `NullValueHandling.Ignore`, used by all write call sites
- **Write_Site**: A location in the codebase that calls `JsonConvert.SerializeObject` to persist data to a JSON file. The three write sites are `EmpireContext.writeContext()`, `PlayerContext.writeContext()`, and `PreferencesStore.Save()`
- **Default_Value**: A field value that matches the type's default: `null` for reference types, `string.Empty` or `""` for strings initialized to empty, `false` for booleans, `0` for integers, `0m` for decimals, `0.0` for doubles
- **Round_Trip**: The process of serializing an object to JSON and deserializing it back, expecting the resulting object to be semantically equivalent to the original
- **Custom_Converter**: A `JsonConverter` subclass that overrides default serialization behavior for a specific type. The codebase has `ItemBagJSONConverter` and `PropertyBagJSONConverter`
- **PlayerData_File**: The `PlayerData.json` file containing player profiles, blueprints, surveys, colonies, delivery routes, and delivery plans
- **BaselineData_File**: The `BaselineData.json` file containing shared game data (blueprint types, ship classes, tech levels, global blueprints)
- **UIPreferences_File**: The `UIPreferences.json` file containing window positions and form control state

## Requirements

### Requirement 1: Shared Serialization Settings

**User Story:** As a developer, I want a single shared serialization settings object, so that all write sites use consistent default-skipping behavior.

#### Acceptance Criteria

1. THE Serialization_Settings SHALL configure `DefaultValueHandling.Ignore` to skip fields with default values during serialization
2. THE Serialization_Settings SHALL configure `NullValueHandling.Ignore` to skip fields with null values during serialization
3. THE Serialization_Settings SHALL configure `Formatting.Indented` to maintain human-readable JSON output
4. THE Serialization_Settings SHALL be defined in a single shared location accessible to all Write_Sites

### Requirement 2: Write Site Integration

**User Story:** As a developer, I want all three JSON write sites to use the shared settings, so that all persisted files benefit from reduced size.

#### Acceptance Criteria

1. WHEN `EmpireContext.writeContext()` serializes the BaselineData_File, THE EmpireContext SHALL use the Serialization_Settings
2. WHEN `PlayerContext.writeContext()` serializes the PlayerData_File, THE PlayerContext SHALL use the Serialization_Settings
3. WHEN `PreferencesStore.Save()` serializes the UIPreferences_File, THE PreferencesStore SHALL use the Serialization_Settings

### Requirement 3: Deserialization Remains Permissive

**User Story:** As a developer, I want deserialization to remain unchanged, so that existing JSON files (with or without default-valued fields) load correctly.

#### Acceptance Criteria

1. THE PlayerContext SHALL deserialize PlayerData_File without applying DefaultValueHandling or NullValueHandling restrictions
2. THE EmpireContext SHALL deserialize BaselineData_File without applying DefaultValueHandling or NullValueHandling restrictions
3. THE PreferencesStore SHALL deserialize UIPreferences_File without applying DefaultValueHandling or NullValueHandling restrictions
4. WHEN a JSON file contains fields with default values (written before this change), THE deserialization SHALL produce the same object state as when those fields are omitted

### Requirement 4: Custom Converter Compatibility

**User Story:** As a developer, I want custom JsonConverters to continue working correctly, so that ItemBag and PropertyBag serialization is not broken.

#### Acceptance Criteria

1. WHEN an ItemBag is serialized using the Serialization_Settings, THE ItemBagJSONConverter SHALL produce valid JSON that deserializes back to an equivalent ItemBag
2. WHEN a PropertyBag is serialized using the Serialization_Settings, THE PropertyBagJSONConverter SHALL produce valid JSON that deserializes back to an equivalent PropertyBag
3. WHEN the ItemBagJSONConverter calls `JsonConvert.SerializeObject` internally for nested Item objects, THE ItemBagJSONConverter SHALL pass the Serialization_Settings to maintain consistent default-skipping behavior

### Requirement 5: Round-Trip Fidelity

**User Story:** As a developer, I want serialization followed by deserialization to produce semantically equivalent objects, so that no data is lost by skipping defaults.

#### Acceptance Criteria

1. FOR ALL valid PlayerRoot objects, serializing with Serialization_Settings then deserializing SHALL produce an object where every field matches the original or holds the correct default value
2. FOR ALL valid BaselineRoot objects, serializing with Serialization_Settings then deserializing SHALL produce an object where every field matches the original or holds the correct default value
3. FOR ALL valid Colony objects containing structures with default-valued fields (ManufacturingCompleted=0, StagingResources=false, MiningLeftOvers=0), serializing with Serialization_Settings then deserializing SHALL produce an object with those fields restored to their default values
4. FOR ALL valid DeliveryPlan objects containing items with default-valued fields (Delivered=false, Quantity=0), serializing with Serialization_Settings then deserializing SHALL produce an object with those fields restored to their default values
5. FOR ALL valid UIPreferences objects, serializing with Serialization_Settings then deserializing SHALL produce an equivalent object

### Requirement 6: File Size Reduction

**User Story:** As a user, I want smaller JSON files, so that disk usage is reduced and file I/O is faster.

#### Acceptance Criteria

1. WHEN a PlayerData_File is serialized with Serialization_Settings, THE output file size SHALL be smaller than or equal to the same data serialized without Serialization_Settings
2. WHEN a BaselineData_File is serialized with Serialization_Settings, THE output file size SHALL be smaller than or equal to the same data serialized without Serialization_Settings
