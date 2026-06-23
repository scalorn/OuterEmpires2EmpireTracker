# Requirements Document

## Introduction

This feature enhances the FormBlueprint form with two capabilities: (1) a title bar that displays live counts of global and player blueprints, and (2) structured filtering controls (Blueprint Type, Class, Tech Level, Evolution) for the blueprint list view, complementing the existing text search.

## Glossary

- **FormBlueprint**: The WinForms form that manages blueprint data entry, display, and list browsing.
- **Blueprint_List**: The ListView control (`lvwBlueprints`) that displays all merged global and player blueprints.
- **Title_Bar**: The `Text` property of the FormBlueprint window, displayed in the window title bar.
- **Filter_Panel**: A new UI region above the Blueprint_List containing structured filter controls (combo boxes) for Blueprint Type, Class, Tech Level, and Evolution.
- **Text_Filter**: The existing text box (`txtBlueprintListFilter`) that searches ExtendedName and BluePrintType.
- **Blueprint_Type_Filter**: A ComboBox in the Filter_Panel bound to `EmpireContext.bindingSourceBlueprintType`, filtering by `Blueprint.BluePrintType`.
- **Class_Filter**: A ComboBox in the Filter_Panel bound to `EmpireContext.bindingSourceShipClass`, filtering by `Blueprint.Class`.
- **Tech_Level_Filter**: A ComboBox in the Filter_Panel bound to `EmpireContext.bindingSourceTechLevel`, filtering by `Blueprint.TechLevel`.
- **Evolution_Filter**: A ComboBox in the Filter_Panel bound to `EmpireContext.bindingSourceEvolution`, filtering by `Blueprint.Evolution`.
- **BlueprintViewModel**: The ViewModel that wraps Blueprint data and provides `GetFilteredBlueprints()` for list population.
- **EmpireContext**: Singleton service holding shared game data including global blueprints and binding sources for combo boxes.
- **PlayerContext**: Singleton service holding player-specific data including player blueprints.

## Requirements

### Requirement 1: Title Bar Blueprint Counts

**User Story:** As a player, I want the FormBlueprint title bar to show the count of global and player blueprints, so that I can see at a glance how many blueprints exist in each category.

#### Acceptance Criteria

1. WHEN FormBlueprint loads, THE Title_Bar SHALL display text in the format "Blueprints - Global: X Player: Y" where X is the count of blueprints in `EmpireContext.globalBlueprintList` and Y is the count of blueprints returned by `PlayerContext.GetCurrentPlayerBlueprints()`.
2. WHEN a blueprint is added, removed, or imported, THE Title_Bar SHALL update the displayed counts to reflect the current state of global and player blueprint lists.
3. WHEN the current player selection changes, THE Title_Bar SHALL update the Player count to reflect the newly selected player's blueprint count.
4. WHILE no player is selected, THE Title_Bar SHALL display "Blueprints - Global: X Player: 0".

### Requirement 2: Filter Panel Layout

**User Story:** As a player, I want structured filter controls above the blueprint list, so that I can narrow down the list by specific blueprint attributes without relying solely on text search.

#### Acceptance Criteria

1. THE Filter_Panel SHALL be created in code-behind (not in the Designer file) and placed between the existing Text_Filter and the Blueprint_List within the `flpSearchList` FlowLayoutPanel.
2. THE Filter_Panel SHALL contain four labeled ComboBox controls: Blueprint_Type_Filter, Class_Filter, Tech_Level_Filter, and Evolution_Filter.
3. THE Filter_Panel SHALL include a "Clear Filters" button that resets all four filter ComboBoxes to their unselected state.
4. WHEN FormBlueprint loads, THE Filter_Panel SHALL display all four filter ComboBoxes with no selection (unselected state), indicating no structured filter is active.

### Requirement 3: Blueprint Type Filtering

**User Story:** As a player, I want to filter the blueprint list by Blueprint Type, so that I can quickly find blueprints of a specific type (e.g. Reactor, Hull, Flatpacks/MiningRig).

#### Acceptance Criteria

1. THE Blueprint_Type_Filter SHALL use `EmpireContext.bindingSourceBlueprintType` as its data source, displaying `BlueprintType.Name` and using `BlueprintType.Id` as the value.
2. WHEN a Blueprint Type is selected in the Blueprint_Type_Filter, THE Blueprint_List SHALL display only blueprints whose `BluePrintType` field matches the selected `BlueprintType.Id`.
3. WHEN the Blueprint_Type_Filter selection is cleared, THE Blueprint_List SHALL remove the Blueprint Type filter constraint.

### Requirement 4: Class Filtering

**User Story:** As a player, I want to filter the blueprint list by ship class number, so that I can find blueprints for a specific ship class.

#### Acceptance Criteria

1. THE Class_Filter SHALL use `EmpireContext.bindingSourceShipClass` as its data source, displaying `ShipClass.Name` and using `ShipClass.Id` as the value.
2. WHEN a Class is selected in the Class_Filter, THE Blueprint_List SHALL display only blueprints whose `Class` field matches the selected `ShipClass.Id`.
3. WHEN the Class_Filter selection is cleared, THE Blueprint_List SHALL remove the Class filter constraint.

### Requirement 5: Tech Level Filtering

**User Story:** As a player, I want to filter the blueprint list by Tech Level, so that I can find blueprints of a specific tech level (e.g. MilSpec, Hi-Tech, Junker).

#### Acceptance Criteria

1. THE Tech_Level_Filter SHALL use `EmpireContext.bindingSourceTechLevel` as its data source, displaying `TechLevel.Name` and using `TechLevel.Name` as the value.
2. WHEN a Tech Level is selected in the Tech_Level_Filter, THE Blueprint_List SHALL display only blueprints whose `TechLevel` field matches the selected `TechLevel.Name`.
3. WHEN the Tech_Level_Filter selection is cleared, THE Blueprint_List SHALL remove the Tech Level filter constraint.

### Requirement 6: Evolution Filtering

**User Story:** As a player, I want to filter the blueprint list by evolution level, so that I can find blueprints at a specific evolution stage (0 through 15).

#### Acceptance Criteria

1. THE Evolution_Filter SHALL use `EmpireContext.bindingSourceEvolution` as its data source, displaying evolution values "0" through "15".
2. WHEN an Evolution level is selected in the Evolution_Filter, THE Blueprint_List SHALL display only blueprints whose `Evolution` field matches the selected integer value.
3. WHEN the Evolution_Filter selection is cleared, THE Blueprint_List SHALL remove the Evolution filter constraint.

### Requirement 7: Combined Filter Behavior

**User Story:** As a player, I want all filters (text search and structured filters) to work together, so that I can combine multiple criteria to find specific blueprints.

#### Acceptance Criteria

1. WHEN multiple filters are active simultaneously, THE Blueprint_List SHALL display only blueprints that satisfy all active filter constraints (logical AND).
2. WHEN any filter value changes, THE Blueprint_List SHALL immediately refresh to reflect the updated filter combination.
3. WHEN the "Clear Filters" button is clicked, THE Filter_Panel SHALL reset all four structured filter ComboBoxes to unselected state and THE Blueprint_List SHALL refresh using only the Text_Filter value.
4. WHEN the current player changes, THE Filter_Panel SHALL retain its current filter selections and THE Blueprint_List SHALL refresh with the new player's blueprints filtered by the existing criteria.
