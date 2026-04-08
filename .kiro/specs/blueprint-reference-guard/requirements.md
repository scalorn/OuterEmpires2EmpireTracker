# Requirements Document

## Introduction

Over time the application has accumulated duplicate and obsolete blueprints. Deleting a blueprint that is still referenced by colony structures, other blueprints, or surveys causes data integrity issues. This feature adds reference counting and deletion protection so users can safely identify unused blueprints and are prevented from deleting blueprints that are still in use.

## Glossary

- **Reference_Counter**: A service that scans all data sources (colonies, blueprints, surveys) and counts how many entities reference a given blueprint UUID.
- **Blueprint**: A data object identified by UUID, stored in either the player blueprint list or the global blueprint list.
- **Colony_Structure**: A structure within a colony that may reference a blueprint UUID via FlatpackBlueprintUUID, ResearchingBlueprintUUID, or ManufacturingBlueprintUUID.
- **Survey**: A survey data object that may reference a blueprint UUID via ScannerBlueprintUUID.
- **Base_Blueprint_Reference**: A blueprint-to-blueprint reference via the baseBlueprintUUID field (evolution chain).
- **Reference_Source**: One of the five specific fields that can hold a blueprint UUID: ColonyStructure.FlatpackBlueprintUUID, ColonyStructure.ResearchingBlueprintUUID, ColonyStructure.ManufacturingBlueprintUUID, Blueprint.baseBlueprintUUID, Survey.ScannerBlueprintUUID.
- **Reference_Report**: A structured result containing the total reference count and a breakdown of which entities reference the blueprint, grouped by Reference_Source type.
- **Blueprint_List_View**: The ListView control in FormBlueprint that displays all blueprints for the current player and global blueprints.

## Requirements

### Requirement 1: Count Blueprint References

**User Story:** As a player, I want to know how many places reference a given blueprint, so that I can determine whether it is safe to delete.

#### Acceptance Criteria

1. WHEN a blueprint UUID is provided, THE Reference_Counter SHALL scan all Colony_Structure objects across all colonies (all players) for matches against FlatpackBlueprintUUID, ResearchingBlueprintUUID, and ManufacturingBlueprintUUID.
2. WHEN a blueprint UUID is provided, THE Reference_Counter SHALL scan all Blueprint objects (player and global) for matches against baseBlueprintUUID.
3. WHEN a blueprint UUID is provided, THE Reference_Counter SHALL scan all Survey objects (all players) for matches against ScannerBlueprintUUID.
4. THE Reference_Counter SHALL return a Reference_Report containing the total count of references and a per-source-type breakdown.
5. WHEN no references exist for a blueprint UUID, THE Reference_Counter SHALL return a Reference_Report with a total count of zero and an empty breakdown.
6. THE Reference_Counter SHALL not count a blueprint as referencing itself (the blueprint being checked SHALL be excluded from baseBlueprintUUID matches).

### Requirement 2: Delete Button State Reflects Reference Status

**User Story:** As a player, I want the Delete button to clearly show whether a blueprint is in use, so that I know at a glance whether it can be deleted.

#### Acceptance Criteria

1. WHEN a blueprint is selected in the Blueprint_List_View, THE FormBlueprint SHALL compute the Reference_Report for that blueprint.
2. WHEN the Reference_Report total count is greater than zero, THE Delete button SHALL be disabled and its text SHALL be set to "In Use ({count})" where {count} is the total reference count.
3. WHEN the Reference_Report total count is zero, THE Delete button SHALL be enabled and its text SHALL be set to "Delete".
4. WHEN no blueprint is selected, THE Delete button SHALL be disabled and its text SHALL be set to "Delete".
5. WHEN the user clicks the Delete button (enabled, zero references), THE FormBlueprint SHALL proceed with the existing deletion confirmation dialog.

### Requirement 3: Display Reference Count in Blueprint List View

**User Story:** As a player, I want to see a reference count column in the blueprint list, so that I can quickly identify which blueprints are unused and safe to delete.

#### Acceptance Criteria

1. THE Blueprint_List_View SHALL include a "Refs" column displaying the reference count for each blueprint.
2. WHEN the Blueprint_List_View is populated, THE FormBlueprint SHALL compute the reference count for each blueprint and display the value in the Refs column.
3. WHEN a blueprint has zero references, THE Blueprint_List_View SHALL display "0" in the Refs column for that blueprint.
4. WHEN a blueprint has one or more references, THE Blueprint_List_View SHALL display the numeric count in the Refs column for that blueprint.
