# Requirements Document

## Introduction

The Colony Activity form currently displays active structures with running timers. This feature adds an Inactivity Mode that shows the opposite: structures that are idle (not working at all) or underutilized (working below capacity). A "Show Inactive" checkbox toggles between the existing Activity Mode and the new Inactivity Mode. The two modes are mutually exclusive. Inactivity Mode helps the player identify wasted capacity across all colonies.

## Glossary

- **Activity_Form**: The `FormColonyActivity` WinForms form that displays colony activity data in a `DataGridView`.
- **Activity_Mode**: The existing behavior of the Activity_Form, showing structures with active timers and unfulfilled commodity requests.
- **Inactivity_Mode**: The new behavior showing idle and underutilized structures.
- **Inactivity_Collector**: The service logic (within or alongside `ColonyActivityCollector`) that scans colonies for inactive and underutilized structures.
- **Structure**: A `ColonyStructure` within a colony, identified by its `FlatpackBlueprintUUID` and `BluePrintType`.
- **Built_Online_Structure**: A Structure whose `PropertyBag` contains `Built=True` and `Online=True`.
- **Idle_Structure**: A Built_Online_Structure that has no active process (no `ProcessCompletionTime` with `TimeRemaining > 0`, or no assigned work item).
- **Underutilized_Refiner**: A Built_Online_Structure of type Refinery that is actively refining but whose refining capacity exceeds the mining supply rate for that resource in the colony, and the colony warehouse does not have sufficient stockpile to cover the next cycle.
- **Mining_Output_Rate**: The `SurveyResource.Amount` per hour for a miner's assigned resource, multiplied by the ExtractionFocus skill bonus (+1% per level).
- **Refining_Consumption_Rate**: `GameConstants.RefiningBaseRate` (25) per hour per refiner for normal refining, or `RefiningRecipe.ConsumeRate` per hour for synthetic refining.
- **Warehouse_Stockpile**: The quantity of a raw resource in the colony's `ItemBag` matching the refiner's input resource and purity.
- **ActivityRow**: The existing data class representing one row in the Activity_Form grid, containing Type, SystemName, ColonyName, SourceName, ProcessDetails, CountDown, and NeedBy.

## Requirements

### Requirement 1: Inactivity Mode Toggle

**User Story:** As a player, I want a checkbox on the Colony Activity form to switch between Activity Mode and Inactivity Mode, so that I can see either active timers or idle structures.

#### Acceptance Criteria

1. THE Activity_Form SHALL display a "Show Inactive" checkbox in the filter panel alongside the existing activity type checkboxes.
2. WHEN the "Show Inactive" checkbox is unchecked, THE Activity_Form SHALL display Activity_Mode data using the existing behavior.
3. WHEN the "Show Inactive" checkbox is checked, THE Activity_Form SHALL display Inactivity_Mode data by querying the Inactivity_Collector.
4. WHEN the "Show Inactive" checkbox state changes, THE Activity_Form SHALL clear and repopulate the grid with the appropriate mode's data.

### Requirement 2: Inactivity Mode UI Adjustments

**User Story:** As a player, I want the form to hide irrelevant controls in Inactivity Mode, so that the interface only shows information meaningful to idle structures.

#### Acceptance Criteria

1. WHILE the Activity_Form is in Inactivity_Mode, THE Activity_Form SHALL hide the "Commodity Request" filter checkbox.
2. WHILE the Activity_Form is in Inactivity_Mode, THE Activity_Form SHALL hide the CountDown time column from the DataGridView.
3. WHEN the Activity_Form switches from Inactivity_Mode back to Activity_Mode, THE Activity_Form SHALL restore the "Commodity Request" filter checkbox to visible.
4. WHEN the Activity_Form switches from Inactivity_Mode back to Activity_Mode, THE Activity_Form SHALL restore the CountDown time column to visible.

### Requirement 3: Idle Miner Detection

**User Story:** As a player, I want to see miners that are not actively mining, so that I can assign them surveys or resources.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type MiningRig as idle if the Structure has no `ProcessCompletionTime` with `TimeRemaining` greater than zero.
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type MiningRig as idle if the Structure has no `MiningSurveyResource` assigned.
3. WHEN an idle miner has no `MiningSurveyResource` assigned, THE Inactivity_Collector SHALL set the ProcessDetails to "No survey assigned".
4. WHEN an idle miner has a `MiningSurveyResource` assigned but no active timer, THE Inactivity_Collector SHALL set the ProcessDetails to "Idle".

### Requirement 4: Idle Refiner Detection

**User Story:** As a player, I want to see refiners that are not actively refining, so that I can assign them resources.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type Refinery as idle if the Structure has no `ProcessCompletionTime` with `TimeRemaining` greater than zero.
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type Refinery as idle if the Structure has no `RefiningResource` assigned.
3. WHEN an idle refiner has no `RefiningResource` assigned, THE Inactivity_Collector SHALL set the ProcessDetails to "No resource assigned".
4. WHEN an idle refiner has a `RefiningResource` assigned but no active timer, THE Inactivity_Collector SHALL set the ProcessDetails to "Idle".

### Requirement 5: Underutilized Refiner Detection

**User Story:** As a player, I want to see refiners that are active but not being fully utilized, so that I can reallocate refining capacity.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL calculate the Mining_Output_Rate for each resource being mined in the colony by reading the miner's `SurveyResource.Amount` and applying the ExtractionFocus skill multiplier (+1% per level).
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL calculate the total Refining_Consumption_Rate for each resource by summing the per-refiner rate (25 per hour for normal refining, or `RefiningRecipe.ConsumeRate` per hour for synthetic refining) across all active refiners processing that resource.
3. WHEN the total Refining_Consumption_Rate for a resource exceeds the Mining_Output_Rate, THE Inactivity_Collector SHALL identify the excess refiners as underutilized, starting from the last refiner by `gameSequence` order.
4. IF the colony Warehouse_Stockpile for the refiner's input resource and purity contains at least one cycle's worth of material (25 units for normal refining, or `RefiningRecipe.ConsumeRate` units for synthetic refining), THEN THE Inactivity_Collector SHALL exclude that refiner from the underutilized list.
5. WHEN a refiner is identified as underutilized, THE Inactivity_Collector SHALL set the ProcessDetails to show the effective utilization (e.g., "Underutilized: 12/25 per cycle") where the numerator is the remaining mining supply available to that refiner and the denominator is the refiner's per-cycle consumption rate.

### Requirement 6: Idle Research Lab Detection

**User Story:** As a player, I want to see research labs that are not actively researching, so that I can assign them blueprints.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type ResearchLaboratory as idle if the Structure has no `ProcessCompletionTime` with `TimeRemaining` greater than zero.
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type ResearchLaboratory as idle if the Structure has no `ResearchingBlueprintUUID` assigned.
3. WHEN an idle research lab has no `ResearchingBlueprintUUID` assigned, THE Inactivity_Collector SHALL set the ProcessDetails to "No blueprint assigned".
4. WHEN an idle research lab has a `ResearchingBlueprintUUID` assigned but no active timer, THE Inactivity_Collector SHALL set the ProcessDetails to "Idle".

### Requirement 7: Idle Manufactory Detection

**User Story:** As a player, I want to see manufactories that are not actively manufacturing, so that I can assign them work orders.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type Manufactory as idle if the Structure has no `ProcessCompletionTime` with `TimeRemaining` greater than zero.
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type Manufactory as idle if the Structure has no `ManufacturingBlueprintUUID` assigned.
3. WHEN an idle manufactory has no `ManufacturingBlueprintUUID` assigned, THE Inactivity_Collector SHALL set the ProcessDetails to "No blueprint assigned".
4. WHEN an idle manufactory has a `ManufacturingBlueprintUUID` assigned but no active timer, THE Inactivity_Collector SHALL set the ProcessDetails to "Idle".

### Requirement 8: Idle Commodity Factory Detection

**User Story:** As a player, I want to see commodity factories that are not actively producing, so that I can assign them commodities.

#### Acceptance Criteria

1. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type CommodityFactory as idle if the Structure has no `ProcessCompletionTime` with `TimeRemaining` greater than zero.
2. WHEN the Inactivity_Collector scans a colony, THE Inactivity_Collector SHALL identify each Built_Online_Structure of type CommodityFactory as idle if the Structure has no `ManufacturingCommodityName` assigned.
3. WHEN an idle commodity factory has no `ManufacturingCommodityName` assigned, THE Inactivity_Collector SHALL set the ProcessDetails to "No commodity assigned".
4. WHEN an idle commodity factory has a `ManufacturingCommodityName` assigned but no active timer, THE Inactivity_Collector SHALL set the ProcessDetails to "Idle".

### Requirement 9: Inactivity Row Data Format

**User Story:** As a player, I want inactivity rows to use the same grid columns as activity rows (minus CountDown), so that the display is consistent and familiar.

#### Acceptance Criteria

1. THE Inactivity_Collector SHALL produce ActivityRow instances with the Type field set to the appropriate ActivityType (Mining, Refining, Research, Manufacturing, CommodityManufacturing).
2. THE Inactivity_Collector SHALL populate the SystemName and ColonyName fields from the colony data for each inactive structure.
3. THE Inactivity_Collector SHALL populate the SourceName field using the format "#{gameSequence} {blueprint.ExtendedName}" matching the existing Activity_Mode convention.
4. THE Inactivity_Collector SHALL set the CountDown field to null for all inactivity rows.
5. WHILE the Activity_Form is in Inactivity_Mode, THE Activity_Form SHALL apply the same activity type filter checkboxes (excluding CommodityRequest) and text filter to inactivity rows as Activity_Mode applies to activity rows.

### Requirement 10: Inactivity Data Refresh

**User Story:** As a player, I want the inactivity data to refresh when colony data changes, so that the display stays current.

#### Acceptance Criteria

1. WHEN colony data changes while the Activity_Form is in Inactivity_Mode, THE Activity_Form SHALL re-collect inactivity data and repopulate the grid.
2. WHEN the current player changes while the Activity_Form is in Inactivity_Mode, THE Activity_Form SHALL re-collect inactivity data for the new player's colonies and repopulate the grid.
