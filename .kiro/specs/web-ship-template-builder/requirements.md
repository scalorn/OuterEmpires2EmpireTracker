# Requirements Document

## Introduction

The Web Ship Template Builder is a public (no authentication required) page in the web UI that allows anyone to design ship loadouts by selecting a hull and filling component slots, with live-computed stats displayed as components are added or changed. It mirrors the core functionality of the WinForms FormShipTemplate but operates entirely client-side using the public blueprint API. The page follows the same architectural pattern as the Colony Planner (`/planner`): Zustand store, public API data fetching, React components, and blueprint property caching.

## Glossary

- **Ship_Template_Builder**: The public web page at `/ship-builder` that allows users to design ship loadouts
- **Hull**: A blueprint with `bluePrintType === "Hull"` that defines the ship's base stats and available component slots
- **Component_Slot**: A typed position on a hull where a compatible component blueprint can be installed
- **Slot_Type**: A string identifier (e.g. "Reactor", "MainDrive", "WeaponSmall") that determines which component blueprints are compatible with a slot
- **Blueprint_Properties**: A key-value dictionary on each blueprint containing numeric stats (Mass, Power Generated, Cargo Capacity, etc.)
- **Ship_Stats**: A computed summary of all stats derived from the hull blueprint plus all installed component blueprints
- **Ship_Class**: An integer (2-8) on hull and component blueprints; components must match the hull's class to be installable
- **Slot_Count**: The number of slots of a given type available on a hull, determined by hull blueprint properties (e.g. "Reactor Slots": 2)
- **Public_Blueprint_API**: The unauthenticated endpoints at `/api/v1/public/blueprints` and `/api/v1/public/blueprints/{uuid}` that provide blueprint data
- **Blueprint_Cache**: An in-memory cache of fetched blueprint detail (properties) to avoid redundant API calls
- **Engineering_Capacity**: A hull-defined limit; components consume engineering capacity and the total used must not exceed available

## Requirements

### Requirement 1: Page Routing and Access

**User Story:** As a visitor, I want to access the ship template builder without logging in, so that I can experiment with ship designs freely.

#### Acceptance Criteria

1. THE Ship_Template_Builder SHALL be accessible at the URL path `/ship-builder` without authentication, including when navigated to directly via browser address bar or external link
2. THE Ship_Template_Builder SHALL be listed in the application navigation within the "Public" section, after the existing public pages (Blueprints, Surveys, Colony Planner)
3. THE Ship_Template_Builder SHALL operate entirely in-memory with no server-side persistence; all build state SHALL exist only in the browser's JavaScript runtime
4. THE Ship_Template_Builder SHALL render its initial state (empty builder with hull selector) without requiring any authenticated API endpoints

### Requirement 2: Hull Selection

**User Story:** As a visitor, I want to select a hull from a searchable dropdown, so that I can start designing a ship loadout.

#### Acceptance Criteria

1. WHEN the page loads, THE Ship_Template_Builder SHALL fetch all hull blueprints from the Public_Blueprint_API filtered by `bluePrintType === "Hull"`
2. WHEN hull blueprints have been fetched, THE Ship_Template_Builder SHALL display a searchable dropdown showing hull name and class; the dropdown SHALL filter options by case-insensitive substring match against the hull name as the user types
3. WHEN a hull is selected, THE Ship_Template_Builder SHALL fetch the hull's full detail (properties) from the Public_Blueprint_API and cache it in the Blueprint_Cache
4. WHEN a hull is selected, THE Ship_Template_Builder SHALL generate the available Component_Slots by iterating the SlotTypes.HullPropertyToSlotType mapping and creating N slots of the corresponding Slot_Type for each hull property whose numeric value is greater than zero (e.g. "Reactor Slots": 2 generates 2 Reactor slots; properties with value 0 generate no slots)
5. WHEN a hull is selected (including reselecting the same hull), THE Ship_Template_Builder SHALL clear all previously installed components and regenerate slots for the selected hull

### Requirement 3: Component Slot Display

**User Story:** As a visitor, I want to see all available component slots for my selected hull, so that I know what I can install.

#### Acceptance Criteria

1. WHEN a hull is selected, THE Ship_Template_Builder SHALL display all available slots grouped by Slot_Type, showing only slot types where the hull defines a count of 1 or more
2. THE Ship_Template_Builder SHALL display each slot with its Slot_Type display name (as defined in Requirement 13) and slot index (0-based within that type, e.g. "Reactor 0", "Reactor 1")
3. WHILE no hull is selected, THE Ship_Template_Builder SHALL display a prompt instructing the user to select a hull; WHEN a hull is selected, THE Ship_Template_Builder SHALL render the slot display within the same UI update cycle as the hull selection without requiring additional user action
4. THE Ship_Template_Builder SHALL display slot types organized into visible group sections in this order: Core (Reactor, Drive, Thruster, Jump Drive, Nav Comp, Scanner), Defence (Shield, Hull Plating, Hull Reinforcement, Hull Sealant), Capacity (Cargo Pod, Fuel Tank, Ore Hopper, Coupler, GERTY), Weapons (Small, Medium, Large), Mining (Mining Laser, Mining Grapple); each group SHALL display a group heading label; groups containing no slots for the selected hull SHALL be hidden entirely
5. WHEN a different hull is selected, THE Ship_Template_Builder SHALL replace the previous slot display with the new hull's slots, reflecting any difference in slot types and counts

### Requirement 4: Component Installation

**User Story:** As a visitor, I want to install components into slots by selecting from compatible blueprints, so that I can build my ship loadout.

#### Acceptance Criteria

1. WHEN a slot dropdown is activated, THE Ship_Template_Builder SHALL display a searchable dropdown of compatible component blueprints filtered to that slot
2. THE Ship_Template_Builder SHALL filter component blueprints by Slot_Type using the SlotTypes.BlueprintTypeToSlotType mapping (only blueprints whose type maps to the slot's type are shown)
3. THE Ship_Template_Builder SHALL filter component blueprints by Ship_Class (only components matching the hull's class are shown)
4. WHEN a component is selected for a slot, THE Ship_Template_Builder SHALL fetch the component's full detail (properties) from the Public_Blueprint_API and cache it in the Blueprint_Cache; IF the detail already exists in the Blueprint_Cache, THEN THE Ship_Template_Builder SHALL use the cached version without making an API call
5. WHEN a component is installed, THE Ship_Template_Builder SHALL recompute Ship_Stats without requiring additional user action
6. THE Ship_Template_Builder SHALL allow clearing a slot (removing an installed component) by selecting an empty/none option from the slot dropdown
7. WHEN a component is removed from a slot, THE Ship_Template_Builder SHALL recompute Ship_Stats without requiring additional user action
8. IF no compatible blueprints exist for a slot after applying Slot_Type and Ship_Class filters, THEN THE Ship_Template_Builder SHALL display the dropdown with only the empty/none option and no component entries

### Requirement 5: Live Stats Computation

**User Story:** As a visitor, I want to see computed ship stats update live as I add or change components, so that I can evaluate my loadout without manual calculation.

#### Acceptance Criteria

1. WHEN a hull is selected or a component is installed or removed, THE Ship_Template_Builder SHALL recompute and display Ship_Stats within the same render cycle (no additional user action required)
2. THE Ship_Template_Builder SHALL compute stats by aggregating blueprint properties across the hull and all installed components, following the same algorithm as ShipBuildService.ComputeStats: additive summation for all numeric properties except ScanLevel which uses the maximum value across all blueprints
3. THE Ship_Template_Builder SHALL display the following stat categories: Engineering (capacity used vs available), Capacity (cargo, fuel, hopper), Defence (health, shield HP, shield regen, energy/kinetic/missile defence), Propulsion (acceleration factor, turn rate), Jump (max distance, fuel per JAS, fuel range, charge time), Power (provided, regen rate, shield draw, shield uptime, weapon draw, weapon sustain time), Mining (yield, cycle time), Weapons (sustain by type showing per-weapon-type sustainable count as PowerRegenRate divided by PowerDrawPerSecond), Scanning (scan level)
4. THE Ship_Template_Builder SHALL compute derived stats: AccelerationFactor as Acceleration divided by TotalMass (0 when TotalMass is 0), TurnRate as RotationalThrust divided by TotalMass (0 when TotalMass is 0), JumpFuelPerJAS as FuelPerJASPerMass multiplied by TotalMass, JumpFuelRange as FuelCapacity divided by JumpFuelPerJAS (0 when JumpFuelPerJAS is 0), ShieldUptime as PowerProvided divided by (ShieldPowerDraw minus PowerRegenRate) when ShieldPowerDraw exceeds PowerRegenRate (displayed as "infinite" when ShieldPowerDraw is less than or equal to PowerRegenRate), WeaponSustainTime as PowerProvided divided by (TotalWeaponPowerDraw minus PowerRegenRate) when TotalWeaponPowerDraw exceeds PowerRegenRate (displayed as "infinite" when TotalWeaponPowerDraw is less than or equal to PowerRegenRate); raw calculated values SHALL be displayed including negative numbers without clamping
5. THE Ship_Template_Builder SHALL display engineering capacity as a used/available indicator; IF engineering capacity used exceeds engineering capacity available, THEN THE Ship_Template_Builder SHALL visually distinguish the engineering capacity display from the normal state (e.g. color change or warning icon) to indicate the overrun condition
6. IF no components are installed and only a hull is selected, THEN THE Ship_Template_Builder SHALL display stats derived from the hull blueprint alone with all component-contributed values at zero

### Requirement 6: Blueprint Data Loading and Caching

**User Story:** As a visitor, I want the page to load blueprint data efficiently, so that the interface is responsive.

#### Acceptance Criteria

1. WHEN the page loads, THE Ship_Template_Builder SHALL fetch the full blueprint list from `GET /api/v1/public/blueprints?page=1&pageSize=10000` and require a successful response to obtain all blueprint summaries (uuid, name, bluePrintType, class)
2. THE Ship_Template_Builder SHALL cache fetched blueprint detail (properties) in the Blueprint_Cache to avoid redundant API calls for the same blueprint
3. WHEN a blueprint detail is needed and exists in the Blueprint_Cache, THE Ship_Template_Builder SHALL use the cached version without making an API call
4. IF the blueprint list API call fails, THEN THE Ship_Template_Builder SHALL display an error message indicating the failure and provide a retry button that re-attempts the fetch when activated
5. IF a blueprint detail API call fails, THEN THE Ship_Template_Builder SHALL display an error message on the affected component (hull selector or slot dropdown) without clearing or losing the current build state (selected hull and other installed components remain intact)
6. WHILE a blueprint detail API call is in progress, THE Ship_Template_Builder SHALL prevent duplicate concurrent requests for the same blueprint UUID

### Requirement 7: State Management

**User Story:** As a visitor, I want my ship build state to be maintained as I make changes, so that I can iteratively refine my design.

#### Acceptance Criteria

1. THE Ship_Template_Builder SHALL use a Zustand store to manage build state (selected hull, installed components, computed stats, blueprint cache)
2. THE Ship_Template_Builder SHALL store the following state: selected hull UUID, hull blueprint detail, component slots with installed blueprint UUIDs, blueprint detail cache, computed Ship_Stats, loading states, error states
3. WHEN the page is refreshed or navigated away from, THE Ship_Template_Builder SHALL lose all build state (no persistence); the blueprint list and all cached blueprint details SHALL be discarded and re-fetched on next page load
4. WHEN the "Clear Build" action is invoked, THE Ship_Template_Builder SHALL reset the hull selection to none, remove all component slots, reset computed Ship_Stats to empty, and clear any error states associated with the hull or components; the blueprint list and Blueprint_Cache SHALL be preserved

### Requirement 8: Empty and Error States

**User Story:** As a visitor, I want clear feedback when the builder is empty or encounters errors, so that I know what to do next.

#### Acceptance Criteria

1. WHILE no hull is selected, THE Ship_Template_Builder SHALL display a prompt instructing the user to select a hull to begin; the stats panel and component slot area SHALL be hidden or empty
2. WHILE the blueprint list is loading, THE Ship_Template_Builder SHALL display a loading indicator in place of the hull selector; the hull selector SHALL be disabled until loading completes or fails
3. IF no hull blueprints are available from the API, THEN THE Ship_Template_Builder SHALL display a message indicating no hulls are available in place of the hull selector dropdown
4. WHILE a blueprint detail is being fetched, THE Ship_Template_Builder SHALL display a loading state on the affected component (hull selector or slot dropdown); the affected control SHALL be disabled during the fetch
5. IF a selected hull's blueprint detail cannot be loaded, THEN THE Ship_Template_Builder SHALL display the hull name with an error indicator; error indicators SHALL only appear for the currently selected hull and SHALL clear when a different hull is selected
6. IF a component blueprint detail cannot be loaded for a slot, THEN THE Ship_Template_Builder SHALL display the component name with an error indicator on that slot; the slot SHALL remain changeable and the error SHALL clear when a different component is selected for that slot

### Requirement 9: Stats Computation Engine (Client-Side)

**User Story:** As a developer, I want the stats computation logic to be a pure function separate from UI, so that it can be unit tested independently.

#### Acceptance Criteria

1. THE Ship_Template_Builder SHALL implement stats computation as a pure function: `computeShipStats(hullProperties, componentProperties[]) → ShipStats`; the function SHALL have no side effects and SHALL not access global state or perform API calls
2. THE Ship_Template_Builder SHALL sum additive properties across hull and all installed components: Mass, PowerGenerated, PowerConsumed, CargoCapacity, FuelCapacity, HopperCapacity (Raw Material Capacity), Health, EnergyDefence, KineticDefence, MissileDefence, ShieldHitpoints, ShieldRegen, Acceleration, RotationalThrust, MaxJumpDistance, FuelPerJump, MiningYield, MiningCycleTime; missing properties SHALL be treated as 0
3. THE Ship_Template_Builder SHALL take the maximum value for ScanLevel across all blueprints (not sum); if no blueprint defines ScanLevel, the value SHALL be 0
4. THE Ship_Template_Builder SHALL accumulate EngCapacityUsed from component "Eng Capacity Required" properties (hull excluded) and read EngCapacityAvailable from the hull's "Eng Capacity Available" property
5. THE Ship_Template_Builder SHALL accumulate PowerProvided and PowerRegenRate from Reactor components, ShieldPowerDraw from Shield components (using PowerDrawPerSecond property), and TotalWeaponPowerDraw from weapon components (using PowerDrawPerSecond property); weapon types are identified by blueprint type prefix: Beamer, CoilGun, Railgun, MissileLauncher, TorpedoLauncher
6. THE Ship_Template_Builder SHALL use the last encountered JumpChargeTime from NavComp components and the last encountered FuelPerJASPerMass from JumpDrive components; if multiple components of these types are installed, the last one processed SHALL take precedence
7. FOR ALL valid hull and component property combinations, computing stats then extracting individual values SHALL produce results consistent with the ShipBuildService.ComputeStats algorithm (round-trip property for stat computation correctness)

### Requirement 10: Weapon Slot Handling

**User Story:** As a visitor, I want weapon slots to show all compatible weapon types for the slot size, so that I can choose between energy, kinetic, and missile weapons.

#### Acceptance Criteria

1. THE Ship_Template_Builder SHALL map weapon slot types to compatible blueprint types: WeaponSmall accepts Beamer/Small, CoilGun/Small, Railgun/Small, MissileLauncher/Small, TorpedoLauncher/Small; WeaponMedium accepts the /Medium variants; WeaponLarge accepts the /Large variants
2. WHEN displaying weapon options for a slot, THE Ship_Template_Builder SHALL show only weapon blueprints matching both the slot size AND the hull's Ship_Class; blueprints matching the class but incompatible with the slot size SHALL be hidden
3. THE Ship_Template_Builder SHALL display weapon counts by size (number of small, medium, and large weapons currently installed) in the stats display

### Requirement 11: URL Sharing

**User Story:** As a visitor, I want to share my ship build via URL, so that others can see and modify my design.

#### Acceptance Criteria

1. WHEN a hull is selected and components are installed, THE Ship_Template_Builder SHALL encode the build state into URL query parameters or a hash fragment
2. WHEN the page loads with build state in the URL, THE Ship_Template_Builder SHALL restore the hull selection and installed components from the URL state
3. THE Ship_Template_Builder SHALL update the URL as the build changes without triggering a page reload (using replaceState), only when encoding succeeds
4. THE Ship_Template_Builder SHALL encode the build compactly: hull UUID plus a list of component blueprint UUIDs indexed by slot position
5. IF the URL contains a hull UUID that does not exist in the blueprint list, THEN THE Ship_Template_Builder SHALL display an error message indicating the hull was not found and start with an empty build
6. IF the URL contains a component UUID that does not exist in the blueprint list, THEN THE Ship_Template_Builder SHALL skip that slot (leave it empty), restore all other valid components, and display a warning indicating which slot(s) could not be restored
7. IF the URL contains build state that cannot be decoded (malformed encoding, missing required fields, or non-UUID values), THEN THE Ship_Template_Builder SHALL ignore the URL state, start with an empty build, and display an error message indicating the shared link is invalid

### Requirement 12: Responsive Layout

**User Story:** As a visitor, I want the ship builder to be usable on different screen sizes, so that I can use it on desktop and tablet.

#### Acceptance Criteria

1. WHILE the viewport width is 1024px or wider (Tailwind `lg` breakpoint), THE Ship_Template_Builder SHALL use a two-column layout with slot configuration on the left and stats display on the right
2. WHILE the viewport width is below 1024px, THE Ship_Template_Builder SHALL stack the layout vertically with slots above stats
3. THE Ship_Template_Builder SHALL follow the existing web UI styling conventions (Tailwind CSS classes, dark theme, consistent spacing)

### Requirement 13: Slot Type Display Names

**User Story:** As a visitor, I want slot types to show human-readable names, so that I can understand what each slot is for.

#### Acceptance Criteria

1. THE Ship_Template_Builder SHALL display user-friendly slot type labels: "Reactor", "Main Drive", "Thruster", "Jump Drive", "Nav Comp", "Scanner", "Shield", "Cargo Pod", "Fuel Tank", "Coupler", "GERTY", "Hull Plating", "Hull Reinforcement", "Hull Sealant", "Mining Laser", "Mining Grapple", "Ore Hopper", "Small Weapon", "Medium Weapon", "Large Weapon"
2. THE Ship_Template_Builder SHALL map internal Slot_Type identifiers (e.g. "MainDrive", "WeaponSmall", "CargoPod") to their display names using a static lookup
