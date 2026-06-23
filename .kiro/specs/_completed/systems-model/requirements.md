# Requirements Document — Systems Model (BL-019)

## Introduction

The Systems Model provides star system coordinate data for the OE2 Empire Tracker. The game galaxy contains 23,631 star systems, each with spatial coordinates, a grid hierarchy (Quadrant/Sector/Region/Locality), a spectral class, faction ownership, and infrastructure flags (orbital, spaceport, starbase). This data is static reference data extracted from the game's galaxy map.

Modeling this data enables:
- Distance calculations between systems (Pythagorean on normalized coordinates)
- System lookup by name or ID for colony/station location context
- Faction territory awareness
- Identification of systems with refueling infrastructure (for future route optimization)
- Grid-based filtering and grouping of systems

This feature is a prerequisite for BL-020 (Route Auto-Sequencing) and BL-141 (Fuel-Constrained Route Optimization).

## Glossary

- **Star_System**: A single star system in the galaxy with a unique game ID, name, coordinates, and metadata.
- **System_Coordinate**: The normalized (x, y) position of a system used for distance calculations and rendering.
- **Grid_Location**: The hierarchical position of a system: Quadrant (1-4), Sector (1-4), Region (1-4), Locality (1-4).
- **Spectral_Class**: The star type classification (M, K, G, F, W, X).
- **Faction**: A player organization that controls territory. Systems may be unclaimed (FactionId = 0) or owned by a faction.
- **System_Repository**: The in-memory store of all star systems with indexed lookups.
- **EmpireContext**: The singleton that loads and manages shared game data (BaselineData.json).
- **Distance_Calculator**: A utility that computes Euclidean distance between two systems using their normalized coordinates.

## Requirements

### Requirement 1: Star System Data Model

**User Story:** As a player, I want star systems modeled with their coordinates and metadata, so that the tracker can calculate distances and display system information.

#### Acceptance Criteria

1. THE Star_System SHALL have an integer Id property representing the game database identifier.
2. THE Star_System SHALL have a string Name property representing the system's display name.
3. THE Star_System SHALL have decimal X and Y properties representing normalized coordinates for distance calculation.
4. THE Star_System SHALL have integer Quadrant, Sector, Region, and Locality properties representing the grid hierarchy (each 1-4).
5. THE Star_System SHALL have a string SpectralClass property representing the star type (M, K, G, F, W, X).
6. THE Star_System SHALL have an integer FactionId property (0 = unclaimed).
7. THE Star_System SHALL have a string FactionName property (empty when unclaimed).
8. THE Star_System SHALL have a string FactionColor property storing the hex color code (empty when unclaimed).
9. THE Star_System SHALL have boolean HasOrbital, HasSpaceport, and HasStarbase properties indicating infrastructure presence.
10. THE Star_System SHALL serialize to JSON using Newtonsoft.Json with property names matching the source data format for import compatibility.


### Requirement 2: System Data Storage

**User Story:** As a player, I want system data loaded from a dedicated reference file, so that the 23,631 systems do not bloat BaselineData.json.

#### Acceptance Criteria

1. THE Application SHALL store star system data in a separate file named `SystemData.json` alongside BaselineData.json, rather than embedding 23,631 records inside BaselineData.json.
2. THE SystemData.json file SHALL contain a JSON array of Star_System objects.
3. THE Application SHALL load SystemData.json at startup as part of EmpireContext initialization.
4. IF SystemData.json is missing or empty, THEN THE Application SHALL log a warning and initialize the System_Repository with zero systems (graceful degradation).
5. IF SystemData.json contains malformed JSON, THEN THE Application SHALL log an error with the exception details and initialize the System_Repository with zero systems.
6. THE Application SHALL provide an import mechanism to populate SystemData.json from the extracted galaxy data file (oe2-galaxy-systems.json source format).

### Requirement 3: System Repository and Lookup

**User Story:** As a player, I want fast system lookups by ID and by name, so that the tracker can resolve system references efficiently.

#### Acceptance Criteria

1. THE System_Repository SHALL provide O(1) lookup by system Id using a Dictionary keyed by integer Id.
2. THE System_Repository SHALL provide O(1) lookup by system Name using a case-insensitive Dictionary keyed by name (StringComparer.OrdinalIgnoreCase).
3. THE System_Repository SHALL expose the total system count.
4. THE System_Repository SHALL provide a method to retrieve all systems in a given Grid_Location (filtered by any combination of Quadrant, Sector, Region, Locality).
5. THE System_Repository SHALL provide a method to find systems by partial name match (case-insensitive contains) for search/autocomplete scenarios.
6. THE System_Repository SHALL be accessible via EmpireContext as a read-only property.
7. WHEN multiple systems share the same name, THE System_Repository name lookup SHALL return the first match and log a warning at load time about duplicates.


### Requirement 4: Distance Calculation

**User Story:** As a player, I want to calculate distances between star systems, so that delivery routes can be optimized by travel distance.

#### Acceptance Criteria

1. THE Distance_Calculator SHALL compute Euclidean distance between two systems using the formula: sqrt((x2-x1)² + (y2-y1)²) on their normalized X and Y coordinates.
2. THE Distance_Calculator SHALL accept two Star_System objects and return a decimal distance value.
3. THE Distance_Calculator SHALL accept two system Ids and resolve them via the System_Repository before computing distance.
4. IF either system Id cannot be resolved, THEN THE Distance_Calculator SHALL return -1 to indicate an invalid calculation.
5. THE Distance_Calculator SHALL be a static utility class (no state, pure computation) for use across services without instantiation.
6. THE Distance_Calculator SHALL provide a method to compute the total route distance given an ordered list of system Ids (sum of consecutive leg distances).
7. IF any system in the route list cannot be resolved, THEN THE Distance_Calculator SHALL skip that leg and log a warning, returning the sum of resolvable legs.

### Requirement 5: Colony-to-System Association

**User Story:** As a player, I want colonies linked to their star system, so that the tracker can determine colony locations for distance calculations.

#### Acceptance Criteria

1. THE Colony model already has a SystemName property. THE System_Repository SHALL provide a method to resolve a Colony's system by matching Colony.SystemName to Star_System.Name (case-insensitive).
2. WHEN a Colony's SystemName matches a Star_System, THE Application SHALL be able to compute distances between any two colonies by resolving their systems and using the Distance_Calculator.
3. IF a Colony's SystemName does not match any Star_System, THEN THE Application SHALL treat the colony as having an unknown location (distance calculations return -1 for routes involving that colony).
4. THE Application SHALL NOT modify the Colony model to add a system ID field. System resolution SHALL be performed at runtime via name lookup to avoid data migration and keep the Colony model unchanged.


### Requirement 6: Infrastructure Query

**User Story:** As a player, I want to find systems with stations and starbases, so that future route planning can identify refueling points.

#### Acceptance Criteria

1. THE System_Repository SHALL provide a method to retrieve all systems that have a spaceport (HasSpaceport = true).
2. THE System_Repository SHALL provide a method to retrieve all systems that have a starbase (HasStarbase = true).
3. THE System_Repository SHALL provide a method to retrieve all systems that have any orbital infrastructure (HasOrbital = true OR HasSpaceport = true OR HasStarbase = true).
4. WHEN filtering by infrastructure, THE System_Repository SHALL return results as an enumerable collection (not a pre-materialized list) for memory efficiency given the 23,631 system dataset.

### Requirement 7: Faction Query

**User Story:** As a player, I want to find systems controlled by a specific faction, so that I can understand territorial boundaries.

#### Acceptance Criteria

1. THE System_Repository SHALL provide a method to retrieve all systems belonging to a given FactionId.
2. THE System_Repository SHALL provide a method to retrieve all distinct faction entries (FactionId, FactionName, FactionColor) present in the dataset, excluding unclaimed (FactionId = 0).
3. THE System_Repository SHALL provide a method to count systems per faction for summary display.

### Requirement 8: Data Import from Galaxy Extract

**User Story:** As a developer, I want to import the extracted galaxy data into the tracker's SystemData.json format, so that the 23,631 systems are available for use.

#### Acceptance Criteria

1. THE Application SHALL provide a one-time import utility (or startup migration) that reads the source `oe2-galaxy-systems.json` file and writes `SystemData.json` in the application directory.
2. THE import SHALL map source fields to Star_System properties: `id`→Id, `n`→Name, `x`→X, `y`→Y, `q`→Quadrant, `s`→Sector, `r`→Region, `l`→Locality, `st`→SpectralClass, `fid`→FactionId, `fn`→FactionName, `fc`→FactionColor, `o`→HasOrbital, `sp`→HasSpaceport, `sb`→HasStarbase.
3. THE import SHALL convert integer flags (0/1) to boolean values for HasOrbital, HasSpaceport, and HasStarbase.
4. THE import SHALL preserve decimal precision on X and Y coordinates (at least 6 decimal places).
5. IF the source file is missing, THEN THE import SHALL log a warning and skip without error.
6. THE import SHALL be idempotent — running it again overwrites SystemData.json with the same content.


### Requirement 9: Serialization Round-Trip

**User Story:** As a developer, I want system data to serialize and deserialize without data loss, so that the reference data survives application restarts.

#### Acceptance Criteria

1. FOR ALL valid Star_System objects, serializing to JSON then deserializing back SHALL produce an equivalent object (round-trip property).
2. THE serialized JSON SHALL use compact property names matching the source format (`id`, `n`, `x`, `y`, `q`, `s`, `r`, `l`, `st`, `fid`, `fn`, `fc`, `o`, `sp`, `sb`) to minimize file size given 23,631 records.
3. THE deserialization SHALL handle missing optional string fields (FactionName, FactionColor) by defaulting to empty string.
4. THE serialization SHALL omit default-value properties where appropriate (empty strings, zero integers, false booleans) using `DefaultValueHandling.Ignore` to reduce file size.

### Requirement 10: Performance

**User Story:** As a player, I want system data to load quickly and lookups to be fast, so that the application remains responsive.

#### Acceptance Criteria

1. THE System_Repository SHALL load 23,631 systems from SystemData.json in under 2 seconds on a standard desktop machine.
2. THE System_Repository SHALL use Dictionary-based indexes for Id and Name lookups, providing O(1) average-case access.
3. THE Distance_Calculator SHALL compute a single distance in constant time (no allocations beyond the return value).
4. THE System_Repository SHALL be initialized once at startup. Mutable properties (faction, infrastructure) SHALL be updatable at runtime without reloading the full dataset.


### Requirement 11: System Editor Form

**User Story:** As a player, I want a form to view and edit star system data, so that I can update faction ownership and infrastructure status as the game evolves.

#### Acceptance Criteria

1. THE Application SHALL provide a FormSystem MDI child window accessible from the Manage menu.
2. THE FormSystem SHALL display a searchable list of systems (partial name match, filterable by grid location).
3. WHEN a system is selected, THE FormSystem SHALL display all system properties in a detail panel.
4. THE following properties SHALL be read-only (not editable): Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass.
5. THE following properties SHALL be editable: FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase.
6. THE FormSystem SHALL provide a faction dropdown or combo box populated from the distinct factions in the dataset, plus a manual entry option for new factions.
7. THE FormSystem SHALL provide checkboxes for HasOrbital, HasSpaceport, and HasStarbase.
8. WHEN the user saves changes, THE Application SHALL update the Star_System in the System_Repository and persist the change to SystemData.json.
9. THE FormSystem SHALL support bulk re-import from a fresh galaxy extract file (oe2-galaxy-systems.json) to refresh all system data, overwriting any manual edits.
10. THE FormSystem SHALL warn the user before bulk re-import that manual faction/infrastructure edits will be overwritten.


### Requirement 12: System Data Mutability

**User Story:** As a player, I want faction and infrastructure changes to persist, so that the tracker reflects the current state of the galaxy.

#### Acceptance Criteria

1. THE System_Repository SHALL support updating FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, and HasStarbase on individual systems.
2. WHEN a system's mutable properties are updated, THE System_Repository SHALL persist the full SystemData.json file (atomic write via SafeFileWriter).
3. THE System_Repository SHALL fire a SystemDataChanged event when any system is modified, so that open forms can refresh.
4. THE Application SHALL NOT allow modification of immutable properties (Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass) through the UI or service layer.
5. WHEN a bulk re-import is performed, THE System_Repository SHALL replace all system data with the imported data and fire SystemDataChanged.
