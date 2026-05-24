# Requirements Document

## Introduction

The Colony Planner Web is a lightweight, public "what-if" colony planning tool in the OE2EmpireTracker web application. It allows any user (no authentication required) to browse available flatpack structures, add them to a virtual colony plan, and see the resulting colony status (power, habitation, food, entertainment, warehouse capacity). This replaces the current StructurePanel which asks for a raw UUID — an unusable UX since no human knows blueprint UUIDs by heart.

The web planner is inspired by the WinForms ColonyStatusCalculator but is intentionally simplified: no persistence, no allocation, no delivery planning, no warehouse inventory. It is purely a local what-if tool for exploring colony compositions using the "Ideal" calculation mode (all worker slots filled when a structure is Online).

## Glossary

- **Colony_Planner**: The web page component that allows users to plan virtual colony structures
- **Flatpack_Dropdown**: A searchable dropdown control populated with available flatpack blueprints from the Server_API
- **Structure_List**: The list of structures the user has added to their virtual colony plan
- **Colony_Status_Display**: The panel showing computed colony resource status (power, habitation, food, entertainment, warehouse)
- **Flatpack_Blueprint**: A blueprint whose `bluePrintType` starts with "Flatpacks/" — represents a buildable colony structure. Sub-types are: MiningRig, Refinery, ResearchLaboratory, Manufactory, ColonyCommandCentre, and CommodityFactory/{Industry} (e.g. CommodityFactory/Agridome)
- **Blueprint_Properties**: The `properties` dictionary on a blueprint detail response containing key-value pairs such as "Power Provided", "Power Required", "Habitation Provision", "Food Provision", "Entertainment Provided", "Warehouse Capacity", and worker slot definitions
- **Structure_State**: One of three states a planned structure can be in: Staged (not built), Built (built but offline), or Online (built and online)
- **Planner_Store**: The client-side state store (Zustand) managing the virtual colony plan
- **Server_API**: The public data endpoints at /api/v1/public/ that serve blueprint and baseline data. The endpoint `/api/v1/public/blueprints/{uuid}` returns full blueprint detail including the `properties` dictionary
- **Status_Category**: One of the five resource categories tracked: Power, Habitation, Food, Entertainment, Warehouse

## Requirements

### Requirement 1: Flatpack Dropdown Population

**User Story:** As a user, I want to select flatpack structures from a dropdown instead of typing UUIDs, so that I can easily find and add structures to my colony plan.

#### Acceptance Criteria

1. WHEN the Colony_Planner page loads, THE Flatpack_Dropdown SHALL fetch available blueprints from the Server_API endpoint `/api/v1/public/blueprints`
2. THE Flatpack_Dropdown SHALL filter the blueprint list to only include blueprints whose `bluePrintType` starts with "Flatpacks/" (case-insensitive comparison)
3. THE Flatpack_Dropdown SHALL display each Flatpack_Blueprint by its human-readable name (the `extendedName` field), not by UUID
4. WHEN the user types in the Flatpack_Dropdown, THE Flatpack_Dropdown SHALL filter the displayed options to match the search text against blueprint name (case-insensitive substring match)
5. THE Flatpack_Dropdown SHALL group flatpack options by their sub-type extracted from the `bluePrintType` after the "Flatpacks/" prefix (groups: MiningRig, Refinery, ResearchLaboratory, Manufactory, ColonyCommandCentre, CommodityFactory)
6. IF the Server_API returns an error, THEN THE Flatpack_Dropdown SHALL display an error message and a retry option


### Requirement 2: Add Structure to Plan

**User Story:** As a user, I want to add a selected flatpack to my virtual colony plan, so that I can build up a colony composition for analysis.

#### Acceptance Criteria

1. WHEN the user selects a flatpack from the Flatpack_Dropdown and clicks Add, THE Colony_Planner SHALL fetch the full blueprint detail from `/api/v1/public/blueprints/{uuid}` to obtain the Blueprint_Properties
2. WHEN the blueprint detail is fetched, THE Colony_Planner SHALL add a new entry to the Structure_List containing the blueprint name, type, properties, and a default state of Staged
3. THE Structure_List SHALL display the structure name (from the blueprint `extendedName`) and the structure sub-type (MiningRig, Refinery, etc.) for each added entry
4. THE Colony_Planner SHALL allow the user to add multiple instances of the same Flatpack_Blueprint
5. WHEN a structure is added, THE Colony_Planner SHALL assign it a sequential build queue position
6. IF the blueprint detail fetch fails, THEN THE Colony_Planner SHALL display an error message and not add the structure to the plan

### Requirement 3: Remove Structure from Plan

**User Story:** As a user, I want to remove structures from my plan, so that I can adjust my colony composition.

#### Acceptance Criteria

1. WHEN the user clicks Remove on a structure in the Structure_List, THE Colony_Planner SHALL remove that structure from the plan
2. WHEN a structure is removed, THE Colony_Status_Display SHALL recompute the colony status automatically
3. THE Colony_Planner SHALL allow the user to remove any structure regardless of its position in the list


### Requirement 4: Colony Status Computation

**User Story:** As a user, I want to see the computed colony status after adding structures, so that I can evaluate whether my colony plan is viable.

#### Acceptance Criteria

1. WHEN the Structure_List changes (add, remove, or state toggle), THE Colony_Status_Display SHALL recompute and display the colony resource status
2. THE Colony_Status_Display SHALL compute Power status as: PowerProvided (sum of "Power Provided" from Online structures) versus PowerRequired (sum of "Power Required" from Online structures)
3. THE Colony_Status_Display SHALL compute Habitation status as: HabitationProvision (sum of "Habitation Provision" from Online structures) versus HabitationRequired (total workers across all Built and Online structures)
4. THE Colony_Status_Display SHALL compute Food status as: FoodProvision (sum of "Food Provision" from ALL structures regardless of state) versus FoodRequired (total workers across all Built and Online structures)
5. THE Colony_Status_Display SHALL compute Entertainment status as: EntertainmentProvided (sum of "Entertainment Provided" from Online structures) versus EntertainmentRequired (total workers multiplied by 2)
6. THE Colony_Status_Display SHALL compute Warehouse status as: WarehouseCapacity (sum of "Warehouse Capacity" from Online structures) versus WarehouseRequired (fixed at 0, since the web planner has no inventory)
7. THE Colony_Status_Display SHALL use the "Ideal" worker calculation: when a structure is Online, all worker slots defined in its Blueprint_Properties are considered filled
8. WHEN the Structure_List is empty, THE Colony_Status_Display SHALL display a prompt to add structures
9. THE Colony_Status_Display SHALL indicate deficit (red) or surplus (green) for each Status_Category


### Requirement 5: Structure State Toggle

**User Story:** As a user, I want to toggle structure states (Staged, Built, Online) in my plan, so that I can see how the colony status changes at different build stages.

#### Acceptance Criteria

1. THE Structure_List SHALL display the current Structure_State of each structure (Staged, Built, or Online)
2. WHEN the user sets a structure to Online, THE Colony_Planner SHALL include that structure's "Power Provided", "Power Required", "Habitation Provision", "Entertainment Provided", and "Warehouse Capacity" in the colony status computation
3. WHEN the user sets a structure to Staged, THE Colony_Planner SHALL exclude that structure from all status contributions except "Food Provision" (which accumulates regardless of state)
4. WHEN the user sets a structure to Built, THE Colony_Planner SHALL count that structure's ideal workers toward HabitationRequired, FoodRequired, and EntertainmentRequired, but SHALL NOT include its Power, Habitation, Entertainment, or Warehouse provisions
5. WHEN a structure state changes, THE Colony_Status_Display SHALL recompute automatically
6. THE Colony_Planner SHALL only count workers (for Habitation/Food/Entertainment Required) from structures in Built or Online state — Staged structures contribute zero workers

### Requirement 6: No Authentication Required

**User Story:** As a visitor, I want to use the colony planner without logging in, so that I can explore colony planning freely.

#### Acceptance Criteria

1. THE Colony_Planner SHALL be accessible without authentication
2. THE Colony_Planner SHALL use only public API endpoints (under `/api/v1/public/`) for fetching blueprint data
3. THE Colony_Planner SHALL store the plan only in client-side state (no server persistence)
4. WHEN the user navigates away from the Colony_Planner page, THE Planner_Store SHALL retain the plan state for the duration of the browser session


### Requirement 7: Clear Plan

**User Story:** As a user, I want to clear my entire plan and start over, so that I can quickly begin a new colony composition.

#### Acceptance Criteria

1. WHEN the user clicks a Clear/Reset button, THE Colony_Planner SHALL remove all structures from the Structure_List
2. WHEN the plan is cleared, THE Colony_Status_Display SHALL reset to the empty state prompt
3. THE Colony_Planner SHALL confirm the clear action before removing structures if the plan contains entries

### Requirement 8: Structure Display

**User Story:** As a user, I want to see detailed information about each structure in my plan, so that I can understand its contribution to colony status at a glance.

#### Acceptance Criteria

1. THE Structure_List SHALL display for each structure: name (from blueprint `extendedName`), type (MiningRig, Refinery, ResearchLaboratory, Manufactory, ColonyCommandCentre, or CommodityFactory/{Industry}), and current Structure_State (Staged, Built, or Online)
2. THE Structure_List SHALL display each structure's contribution to colony status: its "Power Provided", "Power Required", worker slot count, and "Food Provision" values from Blueprint_Properties
3. WHEN a structure is Online, THE Structure_List SHALL visually indicate it is actively contributing to colony provisions
4. WHEN a structure is Staged, THE Structure_List SHALL visually indicate it is not yet contributing (except food)
5. THE Colony_Planner SHALL display a summary showing the count of each structure sub-type in the plan and the total number of structures

### Requirement 9: Client-Side Status Computation

**User Story:** As a developer, I want all status computation to happen client-side, so that the planner works without server round-trips after initial data load.

#### Acceptance Criteria

1. THE Colony_Planner SHALL perform all colony status computation in the browser using Blueprint_Properties fetched during structure addition
2. THE Colony_Planner SHALL cache fetched blueprint details in the Planner_Store to avoid redundant API calls when the same blueprint type is added multiple times
3. THE Colony_Planner SHALL read the following Blueprint_Properties keys for status computation: "Power Provided", "Power Required", "Habitation Provision", "Food Provision", "Entertainment Provided", "Warehouse Capacity"
4. THE Colony_Planner SHALL determine ideal worker count per structure by summing the numeric values of worker slot properties (e.g. "Blue Collar Workers", "White Collar Workers", "Specialists") from the Blueprint_Properties
5. IF a Blueprint_Properties key is missing or non-numeric, THEN THE Colony_Planner SHALL treat that value as 0

