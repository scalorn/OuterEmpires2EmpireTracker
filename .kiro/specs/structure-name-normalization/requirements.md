# Requirements Document

## Introduction

Blueprint names imported from the game market include a "Flatpack" suffix (e.g. "Mining Rig Flatpack", "Habitation Block Flatpack") because that is how they appear as purchasable items. However, once a flatpack is built on a colony, the game refers to the resulting structure by its base name without the suffix (e.g. "Mining Rig", "Habitation Block"). The colony HTML parser's `blueprintDesignName` field already uses the base name.

Currently the app stores the full market name (with "Flatpack" suffix) on the `Blueprint.Name` property and uses it everywhere — UI display, activity/inactivity collectors, colony structure labels, and flatpack dropdowns. The only place the suffix is stripped today is inside `ColonyParser.BuildFlatpackLookup`, which creates a temporary lookup for matching parsed colony buildings to blueprint UUIDs. This means:

- Colony structure labels in the UI show "Mining Rig Flatpack" instead of "Mining Rig"
- Activity and inactivity collector `SourceName` values include the "Flatpack" suffix
- The flatpack dropdown in FormColony lists names with the suffix

A centralized name normalization is needed so that flatpack blueprints expose a structure-oriented display name throughout the app. The approach follows the existing `OutputItemType` naming convention on `BlueprintType`: a new `OutputItemName` property on `Blueprint` is computed for now (stripping the " Flatpack" suffix for flatpack blueprints), but the design accommodates a future upgrade where `OutputItemName` becomes a real serialized property that defaults to the blueprint name if not set explicitly. The existing `ExtendedName` property is updated to use `OutputItemName` instead of `Name`, so all consumers that already use `ExtendedName` automatically display the correct structure name with no code changes at the call sites.

## Glossary

- **Blueprint**: A data object representing a game blueprint, stored with its market name in `Blueprint.Name`. Extends `Item`.
- **Flatpack_Blueprint**: A Blueprint whose `BluePrintType` starts with `"Flatpacks/"`. These are buildable colony structures.
- **OutputItemName**: The name of the item produced by a blueprint. For Flatpack_Blueprints, this is the base structure name after removing the " Flatpack" suffix (e.g. "Mining Rig Flatpack" → "Mining Rig"). For other blueprints, this returns `Name`. Follows the `OutputItemType` naming convention on `BlueprintType`.
- **Colony_Parser**: The `ColonyParser` class that parses colony HTML and matches buildings to blueprints via `BuildFlatpackLookup`.
- **Blueprint_Scanner**: The `BlueprintScanner` class that parses market HTML to extract blueprint data.
- **Market_Importer**: The `MarketBlueprintImporter` service that imports parsed market blueprints into the global or player blueprint lists.
- **Colony_Structure_UI**: The `Forms.Colony.ColonyStructure` user control that displays a single colony structure's details.
- **Activity_Collector**: The `ColonyActivityCollector` and `ColonyInactivityCollector` services that build activity rows using blueprint names.
- **ExtendedName**: A computed property on `Blueprint` that formats the display string including class, evolution, name, tech level, and nickname. Updated to use `OutputItemName` instead of `Name`, so all existing consumers automatically display the normalized structure name.

## Requirements

### Requirement 1: OutputItemName Property (Hybrid Approach)

**User Story:** As a developer, I want a single canonical way to get the output item name from a blueprint, so that all consumers use the same normalized name, following the existing `OutputItemType` naming convention on `BlueprintType`.

#### Acceptance Criteria

1. THE Blueprint class SHALL expose an `OutputItemName` property that returns the blueprint Name with the " Flatpack" suffix removed for Flatpack_Blueprints, and returns `Name` unchanged for all other blueprints.
2. WHEN the Blueprint Name does not end with " Flatpack", THE `OutputItemName` property SHALL return the Blueprint Name unchanged.
3. WHEN the Blueprint Name is null or empty, THE `OutputItemName` property SHALL return an empty string.
4. THE `OutputItemName` property SHALL perform a case-insensitive comparison when checking for the " Flatpack" suffix.
5. THE `OutputItemName` property SHALL use a `[JsonIgnore]` attribute in this phase because the property is computed from `Name`.
6. THE design SHALL accommodate a future upgrade path where `OutputItemName` becomes a real serialized property (with `[JsonProperty]`) that defaults to the blueprint name if not explicitly set, without requiring changes to consumers of the property.

### Requirement 2: Colony Structure UI Display

**User Story:** As a player, I want colony structures to display their structure name (e.g. "Mining Rig") instead of the blueprint market name (e.g. "Mining Rig Flatpack"), so that the UI matches the game.

#### Acceptance Criteria

1. SINCE `ExtendedName` now uses `OutputItemName`, THE Colony_Structure_UI, Activity_Collector (activity rows), and Activity_Collector (inactivity rows) SHALL automatically display the correct structure name with no code changes needed at the consumer sites.
2. THE Colony_Structure_UI SHALL continue to use `ExtendedName` when rendering the structure label in `PopulateStats`.
3. THE Activity_Collector SHALL continue to use `ExtendedName` when building the `SourceName` for colony activity and inactivity rows.

### Requirement 3: Flatpack Dropdown Display

**User Story:** As a player, I want the flatpack selection dropdown in the colony form to show structure names, so that I can identify structures by their in-game colony name.

#### Acceptance Criteria

1. SINCE `ExtendedName` now uses `OutputItemName` and all flatpacks are currently ev0, universal (no class), and have no tech level, THE FormColony flatpack dropdown SHALL use `ExtendedName` as the visible text for each flatpack blueprint, which automatically displays the structure name. This is also future-proof if flatpacks ever gain class/evolution/tech attributes.
2. THE FormColony flatpack filter SHALL match against `ExtendedName` when filtering the dropdown list.

### Requirement 4: BuildFlatpackLookup Consolidation

**User Story:** As a developer, I want `BuildFlatpackLookup` to use the new `OutputItemName` property, so that the suffix-stripping logic is not duplicated.

#### Acceptance Criteria

1. THE Colony_Parser `BuildFlatpackLookup` method SHALL use `Blueprint.OutputItemName` instead of inline suffix-stripping logic when building the design name lookup.
2. WHEN a Flatpack_Blueprint has an `OutputItemName` that differs from its `Name`, THE lookup SHALL register both the `OutputItemName` and the original `Name` as keys mapping to the blueprint UUID.
3. FOR ALL valid Flatpack_Blueprints, the lookup produced by the refactored `BuildFlatpackLookup` SHALL contain the same key-value pairs as the current implementation (behavioral equivalence).

### Requirement 5: Round-Trip and Serialization Integrity

**User Story:** As a developer, I want to ensure that name normalization does not corrupt persisted data, so that blueprint names survive save/load cycles unchanged.

#### Acceptance Criteria

1. THE `OutputItemName` property SHALL be a computed read-only property that does not modify `Blueprint.Name`.
2. FOR ALL Blueprints, serializing then deserializing a Blueprint SHALL produce an object with the same `Name` value (round-trip property). THE `OutputItemName` property SHALL NOT appear in the serialized JSON output in this phase because it uses `[JsonIgnore]`.
3. FOR ALL Flatpack_Blueprints, THE `OutputItemName` property SHALL satisfy: if `Name` ends with " Flatpack" (case-insensitive), then `OutputItemName` + " Flatpack" equals the original `Name` (with original casing of the suffix preserved in the reconstruction).
