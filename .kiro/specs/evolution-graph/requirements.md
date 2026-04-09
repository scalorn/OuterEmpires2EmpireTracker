# Requirements Document

## Introduction

The Evolution Graph feature adds a new tab to the Blueprint form (FormBlueprint) that visualizes how blueprint properties change across evolution levels. By walking the baseBlueprintUUID chain from the selected blueprint back to Evolution 0, the graph plots each numeric property as a percentage of its Ev0 base value on a line chart. This helps players understand the progression curve and decide which evolution level to target.

## Glossary

- **Evolution_Chain**: The ordered sequence of blueprints linked by baseBlueprintUUID, starting from the current blueprint and walking backward to the Ev0 ancestor. Each blueprint in the chain represents one evolution level.
- **Evolution_Graph_Tab**: A new tab page within the existing tabDetailedData TabControl on FormBlueprint, displaying the evolution line chart and property checkboxes.
- **Chart_Control**: An instance of System.Windows.Forms.DataVisualization.Charting.Chart used to render the line graph.
- **Numeric_Property**: A blueprint property whose PropertyValueType is Integer, Decimal, or Time (as determined by BlueprintPropertyValidation.GetPropertyType). Boolean, CheckBox, ComboBox, and Unknown types are excluded.
- **Base_Value**: The value of a property at Evolution 0 in the evolution chain, used as the 100% reference for percentage normalization.
- **Property_Checkbox_Panel**: A panel of checkboxes (one per graphed property) allowing the user to show or hide individual property lines on the chart.
- **BlueprintType_Properties**: The Properties string array on the BlueprintType model, which defines the complete set of properties for a given blueprint type.
- **FormBlueprint**: The existing WinForms form for managing blueprint data, containing the tabDetailedData TabControl.
- **BlueprintViewModel**: The ViewModel wrapping a Blueprint data object, providing typed access to blueprint fields and evolution chain traversal.

## Requirements

### Requirement 1: Evolution Chain Resolution

**User Story:** As a player, I want the system to resolve the full evolution chain for the selected blueprint, so that the graph has accurate data points for each evolution level.

#### Acceptance Criteria

1. WHEN a blueprint is selected, THE Evolution_Chain resolver SHALL walk the baseBlueprintUUID links from the selected blueprint backward until a blueprint with no baseBlueprintUUID is reached (the Ev0 ancestor).
2. THE Evolution_Chain resolver SHALL produce an ordered list of blueprints sorted by Evolution level ascending (Ev0 first).
3. IF a baseBlueprintUUID references a blueprint that does not exist in the player or global blueprint lists, THEN THE Evolution_Chain resolver SHALL terminate the chain at the last successfully resolved blueprint.
4. IF the selected blueprint has no baseBlueprintUUID and Evolution equals 0, THEN THE Evolution_Chain resolver SHALL treat the single blueprint as a complete chain of length one.
5. THE Evolution_Chain resolver SHALL detect circular references in the baseBlueprintUUID chain and terminate traversal when a previously visited UUID is encountered.

### Requirement 2: Property Filtering

**User Story:** As a player, I want only meaningful numeric properties shown on the graph, so that the chart is not cluttered with irrelevant or unchanging data.

#### Acceptance Criteria

1. THE Property_Filter SHALL use the BlueprintType_Properties array of the selected blueprint's BlueprintType as the source of candidate properties.
2. THE Property_Filter SHALL exclude properties whose PropertyValueType (from BlueprintPropertyValidation.GetPropertyType) is CheckBox, ComboBox, Boolean, or Unknown.
3. THE Property_Filter SHALL include properties whose PropertyValueType is Integer, Decimal, or Time.
4. THE Property_Filter SHALL exclude any Numeric_Property whose value is identical across all blueprints in the Evolution_Chain (no change occurred).
5. IF no Numeric_Property values changed across the Evolution_Chain, THEN THE Evolution_Graph_Tab SHALL display a message indicating no property changes were found.

### Requirement 3: Percentage Normalization

**User Story:** As a player, I want property values normalized as percentages of the Ev0 base value, so that I can compare properties with different units on the same Y-axis.

#### Acceptance Criteria

1. THE Chart_Control SHALL normalize each property value as (value / Base_Value) * 100, where Base_Value is the property value at Evolution 0.
2. IF a Base_Value is zero, THEN THE Chart_Control SHALL exclude that property from the graph to avoid division by zero.
3. THE Chart_Control SHALL display the Ev0 data point at 100% for all graphed properties.

### Requirement 4: Chart Axes Configuration

**User Story:** As a player, I want the chart axes to show the full evolution range with clear gridlines, so that I can read the graph accurately.

#### Acceptance Criteria

1. THE Chart_Control SHALL display the X-axis with a range from 0 to 15, representing Evolution levels 0 through 15.
2. THE Chart_Control SHALL display the X-axis range of 0 to 15 regardless of how many data points exist in the Evolution_Chain.
3. THE Chart_Control SHALL label the X-axis as "Evolution Level".
4. THE Chart_Control SHALL display the Y-axis as percentage values with the Ev0 baseline at 100%.
5. THE Chart_Control SHALL display Y-axis gridlines at every 10% interval.
6. THE Chart_Control SHALL label the Y-axis as "% Change from Evolution 0 Value".

### Requirement 5: Line Rendering

**User Story:** As a player, I want the graph to distinguish between consecutive and non-consecutive evolution data points, so that I can see where data gaps exist.

#### Acceptance Criteria

1. THE Chart_Control SHALL draw solid lines between data points at consecutive evolution levels (e.g., Ev2 to Ev3).
2. THE Chart_Control SHALL draw dashed lines between data points where one or more evolution levels are missing from the chain (e.g., Ev2 to Ev5 with no Ev3 or Ev4).
3. THE Chart_Control SHALL render each property line in a distinct color from a colorblind-friendly palette.
4. THE Chart_Control SHALL auto-assign colors to property lines without requiring user configuration.

### Requirement 6: Property Visibility Checkboxes

**User Story:** As a player, I want checkboxes to toggle individual property lines on and off, so that I can reduce clutter and focus on specific properties.

#### Acceptance Criteria

1. THE Property_Checkbox_Panel SHALL display one checkbox per graphed Numeric_Property.
2. THE Property_Checkbox_Panel SHALL check all checkboxes by default when the graph is first populated.
3. WHEN a checkbox is unchecked, THE Chart_Control SHALL hide the corresponding property line.
4. WHEN a checkbox is checked, THE Chart_Control SHALL show the corresponding property line.
5. THE Property_Checkbox_Panel SHALL display each checkbox label in the same color as its corresponding chart line.

### Requirement 7: Tab Integration

**User Story:** As a player, I want the evolution graph on a tab alongside the existing Statistics and Resources tabs, so that it is easy to find and consistent with the form layout.

#### Acceptance Criteria

1. THE Evolution_Graph_Tab SHALL be added to the existing tabDetailedData TabControl on FormBlueprint.
2. THE Evolution_Graph_Tab SHALL appear after the existing Statistics and Resources tab pages.
3. THE Evolution_Graph_Tab SHALL have the tab text "Evolution Graph".

### Requirement 8: Data Refresh

**User Story:** As a player, I want the graph to update automatically when I select a different blueprint or import new data, so that the chart always reflects the current selection.

#### Acceptance Criteria

1. WHEN a different blueprint is selected in the blueprint list view, THE Evolution_Graph_Tab SHALL refresh the chart with the newly selected blueprint's evolution chain data.
2. WHEN blueprint data changes (via the BlueprintDataChanged event), THE Evolution_Graph_Tab SHALL refresh the chart if the changed blueprint is part of the currently displayed evolution chain.
3. WHEN the Evolution_Graph_Tab is refreshed, THE Property_Checkbox_Panel SHALL reset all checkboxes to checked.
4. WHEN no blueprint is selected, THE Evolution_Graph_Tab SHALL clear the chart and the Property_Checkbox_Panel.
