# Requirements Document

## Introduction

The Colony Activity Form (ColonyActivityForm) is a read-only master list view that aggregates all active countdown timers across all colonies owned by the current player. It provides a single consolidated view of every in-progress activity — building, manufacturing, commodity manufacturing, commodity requests, research, mining, and refining — sorted by time remaining so the player can see what finishes soonest. The form supports multi-select filtering by activity type and auto-refreshes the countdown display every second.

## Glossary

- **Activity_Form**: The ColonyActivityForm WinForms form that displays the consolidated countdown timer list.
- **Activity_Row**: A single row in the Activity_Form grid representing one active countdown timer or pending commodity request.
- **Activity_Type**: An enumeration classifying the source of a countdown: Building, Manufacturing, CommodityManufacturing, CommodityRequest, Research, Mining, Refining.
- **CountDownTime**: The existing data class that tracks countdown timers with StartTime, EndTime, RepeatIntervalSeconds, and computed TimeRemaining/TimeRemainingString properties.
- **Colony_Structure**: A ColonyStructure instance within a colony, identified by its FlatpackBlueprintUUID and gameSequence.
- **Commodity_Request**: A CommodityRequested instance within a colony that has not yet been fulfilled.
- **Player_Context**: The PlayerContext singleton that provides access to the current player's colonies and fires data change events.
- **Source_Name**: The formatted display name for a structure, following the pattern "#<gameSequence> <FlatpackBlueprint.ExtendedName>" as used by ColonyStructure.PopulateStats; for commodity requests the value is "Commodity Request".
- **Process_Details**: A text summary of what the structure is actively doing, matching the content of the rtbProgressStatus display in the ColonyStructure control for each structure type.

## Requirements

### Requirement 1: Data Collection — Structure Timers

**User Story:** As a player, I want to see all colony structures that have active countdown timers, so that I can monitor progress across all my colonies in one place.

#### Acceptance Criteria

1. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null BuildCompletionTime with TimeRemaining greater than zero.
2. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null ProcessCompletionTime with TimeRemaining greater than zero and a FlatpackBlueprint.BluePrintType equal to BlueprintTypes.Manufactory.
3. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null ProcessCompletionTime with TimeRemaining greater than zero and a FlatpackBlueprint.BluePrintType equal to BlueprintTypes.CommodityFactory.
4. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null ProcessCompletionTime with TimeRemaining greater than zero and a FlatpackBlueprint.BluePrintType equal to BlueprintTypes.ResearchLaboratory.
5. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null ProcessCompletionTime with TimeRemaining greater than zero and a FlatpackBlueprint.BluePrintType equal to BlueprintTypes.MiningRig.
6. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Colony_Structure that has a non-null ProcessCompletionTime with TimeRemaining greater than zero and a FlatpackBlueprint.BluePrintType equal to BlueprintTypes.Refinery.

### Requirement 2: Data Collection — Commodity Requests

**User Story:** As a player, I want to see unfulfilled commodity requests with their time-until-NeedBy displayed as a countdown, so that I can prioritize deliveries.

#### Acceptance Criteria

1. WHEN the Activity_Form loads, THE Activity_Form SHALL scan all colonies returned by Player_Context.GetCurrentPlayerColonies() and collect every Commodity_Request where Fulfilled equals false.
2. THE Activity_Form SHALL compute the time remaining for each Commodity_Request as the difference between the NeedBy DateTime and DateTime.Now, expressed in seconds.
3. WHEN a Commodity_Request has a NeedBy date in the past, THE Activity_Form SHALL display the time remaining as "0s".

### Requirement 3: Activity Type Classification

**User Story:** As a player, I want each activity row classified by type, so that I can filter the list to focus on specific activity categories.

#### Acceptance Criteria

1. THE Activity_Form SHALL classify a Colony_Structure with a non-null BuildCompletionTime (TimeRemaining > 0) as Activity_Type "Building".
2. THE Activity_Form SHALL classify a Colony_Structure with BluePrintType equal to BlueprintTypes.Manufactory and a non-null ProcessCompletionTime (TimeRemaining > 0) as Activity_Type "Manufacturing".
3. THE Activity_Form SHALL classify a Colony_Structure with BluePrintType equal to BlueprintTypes.CommodityFactory and a non-null ProcessCompletionTime (TimeRemaining > 0) as Activity_Type "CommodityManufacturing".
4. THE Activity_Form SHALL classify a Colony_Structure with BluePrintType equal to BlueprintTypes.ResearchLaboratory and a non-null ProcessCompletionTime (TimeRemaining > 0) as Activity_Type "Research".
5. THE Activity_Form SHALL classify a Colony_Structure with BluePrintType equal to BlueprintTypes.MiningRig and a non-null ProcessCompletionTime (TimeRemaining > 0) as Activity_Type "Mining".
6. THE Activity_Form SHALL classify a Colony_Structure with BluePrintType equal to BlueprintTypes.Refinery and a non-null ProcessCompletionTime (TimeRemaining > 0) as Activity_Type "Refining".
7. THE Activity_Form SHALL classify a Commodity_Request (Fulfilled = false) as Activity_Type "CommodityRequest".

### Requirement 4: Activity Type Filtering

**User Story:** As a player, I want to filter the activity list by type using a multi-select control, so that I can focus on the activities I care about.

#### Acceptance Criteria

1. THE Activity_Form SHALL display a multi-select filter control with one checkbox per Activity_Type: Building, Manufacturing, CommodityManufacturing, CommodityRequest, Research, Mining, Refining.
2. WHEN the Activity_Form first loads, THE Activity_Form SHALL have all Activity_Type checkboxes selected EXCEPT Mining and Refining, which SHALL be deselected.
3. WHEN the user toggles an Activity_Type checkbox, THE Activity_Form SHALL immediately update the grid to show only Activity_Rows whose Activity_Type matches a selected checkbox.
4. THE Activity_Form SHALL retain the filter state for the lifetime of the form instance.

### Requirement 5: Text Filter

**User Story:** As a player, I want to type a search term to filter the activity list across all columns, so that I can quickly find a specific resource, colony, or activity.

#### Acceptance Criteria

1. THE Activity_Form SHALL display a ValidatedTextBox text filter field in the filter panel.
2. WHEN the user types in the text filter, THE Activity_Form SHALL immediately filter the grid to show only Activity_Rows where any visible column value contains the filter text (case-insensitive substring match).
3. THE text filter SHALL be applied in combination with the Activity_Type checkbox filter — a row must pass both filters to be displayed.
4. WHEN the text filter is cleared, THE Activity_Form SHALL show all rows that pass the Activity_Type checkbox filter.

### Requirement 6: Grid Display Columns

**User Story:** As a player, I want to see countdown time, system, colony, source, and process details for each activity, so that I have full context at a glance.

#### Acceptance Criteria

1. THE Activity_Form SHALL display a DataGridView with the following columns in order: CountDownTime, System Name, Colony Name, Activity Type, Source, Process Details.
2. THE Activity_Form SHALL display the CountDownTime column value as CountDownTime.TimeRemainingString for structure timer activities (Building, Manufacturing, CommodityManufacturing, Research, Mining, Refining).
3. WHEN the Activity_Row is a Building activity, THE Activity_Form SHALL use BuildCompletionTime.TimeRemainingString as the CountDownTime column value.
4. WHEN the Activity_Row is a CommodityRequest activity, THE Activity_Form SHALL compute the time remaining as (NeedBy - DateTime.Now) and format the value using the same "Xd Yh Zm Ws" format as CountDownTime.TimeRemainingString.
5. THE Activity_Form SHALL display the System Name column value as the owning colony's SystemName property.
6. THE Activity_Form SHALL display the Colony Name column value as the owning colony's ColonyName property.
7. THE Activity_Form SHALL display the Activity Type column value as the Activity_Type classification string.
8. THE Activity_Form SHALL display the Source column value as "#<gameSequence> <FlatpackBlueprint.ExtendedName>" for structure-based activities, matching the ColonyStructure.PopulateStats naming pattern.
9. WHEN the Activity_Row is a CommodityRequest activity, THE Activity_Form SHALL display the Source column value as "Commodity Request".

### Requirement 7: Process Details Column Content

**User Story:** As a player, I want the process details column to show what each structure is actively doing, so that I can understand the activity without opening the colony form.

#### Acceptance Criteria

1. WHEN the Activity_Row is a Building activity, THE Activity_Form SHALL display the Process Details as "Building".
2. WHEN the Activity_Row is a Mining activity, THE Activity_Form SHALL display the Process Details as "<Amount>/h <Resource> (<Purity>)" using the structure's MiningSurvey and MiningSurveyResource to look up the SurveyResource, matching the PopulateProgressStatus format.
3. WHEN the Activity_Row is a Refining activity with no synthetic recipe match, THE Activity_Form SHALL display the Process Details as "<BaseRate>:<OutputRate> <RefiningResource> (<RefiningResourcePurity>)" matching the PopulateRefineryProgressStatus format.
4. WHEN the Activity_Row is a Refining activity with a synthetic recipe match, THE Activity_Form SHALL display the Process Details as "<ConsumeRate>:<ProduceRate> <OutputResource>" matching the PopulateRefineryProgressStatus format.
5. WHEN the Activity_Row is a Research activity, THE Activity_Form SHALL display the Process Details as "Evo <Evolution>-><Evolution+1> <BlueprintName>" using the structure's ResearchingBlueprintUUID to look up the Blueprint, matching the PopulateResearchLabProgressStatus format.
6. WHEN the Activity_Row is a Manufacturing activity, THE Activity_Form SHALL display the Process Details as "(<ManufacturingCompleted>/<ManufacturingQuantity>) <Blueprint.ExtendedName>" using the structure's ManufacturingBlueprintUUID to look up the Blueprint, matching the PopulateManufactoryProgressStatus format.
7. WHEN the Activity_Row is a CommodityManufacturing activity, THE Activity_Form SHALL display the Process Details as "(<ManufacturingCompleted>/<ManufacturingQuantity>) <ManufacturingCommodityName> x<CommoditiesPerCycle>" matching the PopulateCommodityFactoryProgressStatus format.
8. WHEN the Activity_Row is a CommodityRequest activity, THE Activity_Form SHALL display the Process Details as "<CommodityName> x<Requested>" showing the commodity name and requested quantity.

### Requirement 8: Default Sorting and Column Sort

**User Story:** As a player, I want the list sorted by time remaining (soonest first) by default, and I want to be able to sort by any column, so that I can organize the view as needed.

#### Acceptance Criteria

1. WHEN the Activity_Form grid is populated, THE Activity_Form SHALL sort rows by the CountDownTime column in ascending order (lowest time remaining first) as the default sort.
2. WHEN the user clicks a column header, THE Activity_Form SHALL sort the grid by that column, toggling between ascending and descending order on repeated clicks.
3. THE Activity_Form SHALL support sorting on all six columns: CountDownTime, System Name, Colony Name, Activity Type, Source, Process Details.
4. WHEN sorting by the CountDownTime column, THE Activity_Form SHALL sort by the numeric seconds remaining value, not by the formatted string.

### Requirement 9: Auto-Refresh Countdown Display

**User Story:** As a player, I want the countdown times to update every second, so that I can see real-time progress without manually refreshing.

#### Acceptance Criteria

1. THE Activity_Form SHALL start a System.Windows.Forms.Timer with a 1-second interval when the form loads.
2. WHEN the timer ticks, THE Activity_Form SHALL update the CountDownTime column value for every visible Activity_Row by recalculating the time remaining.
3. WHEN the Activity_Form is closed, THE Activity_Form SHALL stop and dispose the timer.
4. THE Activity_Form SHALL use ProgrammaticUpdateGuard when updating cell values during the timer tick to suppress grid event handlers.

### Requirement 10: Player Context Event Subscription

**User Story:** As a player, I want the activity list to refresh automatically when I switch players or when colony data changes, so that the view always reflects current data.

#### Acceptance Criteria

1. WHEN the Activity_Form loads, THE Activity_Form SHALL subscribe to Player_Context.CurrentPlayerChanged and Player_Context.ColonyDataChanged events.
2. WHEN Player_Context.CurrentPlayerChanged fires, THE Activity_Form SHALL perform a full data refresh by re-scanning all colonies for the new current player and repopulating the grid.
3. WHEN Player_Context.ColonyDataChanged fires, THE Activity_Form SHALL perform a full data refresh by re-scanning all colonies and repopulating the grid.
4. WHEN the Activity_Form is closed, THE Activity_Form SHALL unsubscribe from Player_Context.CurrentPlayerChanged and Player_Context.ColonyDataChanged events using named methods.

### Requirement 11: MainWindow Menu Integration

**User Story:** As a player, I want to open the Colony Activity Form from the MainWindow Edit menu, so that I can access the consolidated timer view.

#### Acceptance Criteria

1. THE MainWindow SHALL have a menu item labeled "Colony Activity" in the Edit menu strip.
2. WHEN the user clicks the "Colony Activity" menu item, THE MainWindow SHALL create a new ColonyActivityForm instance, set its MdiParent to the MainWindow, and call Show().

### Requirement 12: Read-Only Grid and IProgrammaticUpdateSource

**User Story:** As a player, I want the activity grid to be read-only and follow the application's programmatic update patterns, so that the form behaves consistently with other forms.

#### Acceptance Criteria

1. THE Activity_Form SHALL set the DataGridView ReadOnly property to true.
2. THE Activity_Form SHALL set the DataGridView AllowUserToAddRows property to false.
3. THE Activity_Form SHALL set the DataGridView AllowUserToDeleteRows property to false.
4. THE Activity_Form SHALL implement IProgrammaticUpdateSource with BeginProgrammaticUpdate(), EndProgrammaticUpdate(), and a private integer _isProgrammaticUpdate field.
5. THE Activity_Form SHALL use ProgrammaticUpdateGuard in all methods that programmatically modify grid contents.

### Requirement 13: Form Layout

**User Story:** As a player, I want the form to have a filter panel and a resizable grid, so that the layout is usable at different window sizes.

#### Acceptance Criteria

1. THE Activity_Form SHALL display the Activity_Type filter checkboxes in a panel above or to the side of the DataGridView.
2. THE Activity_Form SHALL resize the DataGridView when the form is resized, using a layout event handler consistent with the application's existing resize patterns.
3. THE Activity_Form files SHALL be placed in the directory Forms/ColonyActivity/ following the one-directory-per-form convention.
