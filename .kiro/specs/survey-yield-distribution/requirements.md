# Requirements Document

## Introduction

The Survey Yield Distribution feature adds a new tab to the existing FormSurvey that displays a smooth curve histogram (kernel density estimate style) showing how resource yields are distributed across surveys. Users can overlay multiple resource+purity combinations as separate colored lines to compare yield consistency and ranges. The feature follows the same multi-line charting pattern established by the Evolution Graph (EvolutionChainService).

## Glossary

- **Yield_Distribution_Tab**: The new tab on FormSurvey that hosts the yield distribution graph and its controls.
- **Distribution_Service**: The service responsible for computing yield distribution data from survey resources, including binning and frequency calculation.
- **Yield_Value**: The numeric Amount field of a SurveyResource, representing the resource output per cycle.
- **Resource_Purity_Combo**: A unique combination of ResourceName and Purity (e.g., "Medium Alkaline Earth Metals") that identifies a distribution series.
- **Bin**: A range of yield values grouped together for frequency counting. Bin width is user-adjustable.
- **Distribution_Curve**: A smooth curve (kernel density estimate style) representing the frequency distribution of yield values for a given Resource_Purity_Combo.
- **Series**: A single colored line on the graph representing one Resource_Purity_Combo's distribution curve.
- **Filtered_Surveys**: The set of surveys matching the current filter criteria on FormSurvey (name, resource, survey type, purity, minimum amount).

## Requirements

### Requirement 1: Tab Placement

**User Story:** As a player, I want a Yield Distribution tab on the survey form, so that I can access distribution analysis alongside my survey data.

#### Acceptance Criteria

1. THE Yield_Distribution_Tab SHALL appear as a tab on FormSurvey alongside existing survey detail content.
2. WHEN the Yield_Distribution_Tab is selected, THE Yield_Distribution_Tab SHALL display the distribution graph area and series selection controls.

### Requirement 2: Survey Type Segregation

**User Story:** As a player, I want the distribution graph to show only one survey type at a time, so that planet and asteroid yields are not mixed in the same analysis.

#### Acceptance Criteria

1. THE Distribution_Service SHALL compute distribution data using only surveys of a single SurveyType (Planet or Asteroid).
2. WHEN the survey type filter on FormSurvey is set to "Planet", THE Distribution_Service SHALL include only Planet surveys in the distribution calculation.
3. WHEN the survey type filter on FormSurvey is set to "Asteroid", THE Distribution_Service SHALL include only Asteroid surveys in the distribution calculation.
4. WHEN the survey type filter on FormSurvey is set to "All", THE Yield_Distribution_Tab SHALL prompt the user to select a specific survey type before displaying distribution data.

### Requirement 3: Input Data Source

**User Story:** As a player, I want the distribution to reflect my current filter settings, so that I can analyze subsets of my surveys without changing the graph separately.

#### Acceptance Criteria

1. THE Distribution_Service SHALL use the Filtered_Surveys (surveys matching the current FormSurvey filter criteria) as the input data set.
2. WHEN the FormSurvey filter criteria change, THE Yield_Distribution_Tab SHALL recalculate and redraw all active distribution curves using the updated Filtered_Surveys.

### Requirement 4: Series Selection

**User Story:** As a player, I want to select multiple resource+purity combinations to overlay on the graph, so that I can compare yield distributions side by side.

#### Acceptance Criteria

1. THE Yield_Distribution_Tab SHALL provide a mechanism for the user to add a Resource_Purity_Combo as a new series on the graph.
2. THE Yield_Distribution_Tab SHALL allow multiple series to be displayed simultaneously as separate colored lines.
3. WHEN a series is added, THE Yield_Distribution_Tab SHALL assign a distinct color to the new series line.
4. THE Yield_Distribution_Tab SHALL provide a mechanism for the user to remove a previously added series from the graph.
5. THE Yield_Distribution_Tab SHALL display a legend identifying each series by its Resource_Purity_Combo name and assigned color.

### Requirement 5: Distribution Calculation

**User Story:** As a player, I want the yield values binned and displayed as a frequency percentage curve, so that I can understand how yields are distributed across my surveys.

#### Acceptance Criteria

1. THE Distribution_Service SHALL extract the numeric Yield_Value from each SurveyResource matching the selected Resource_Purity_Combo across all Filtered_Surveys.
2. THE Distribution_Service SHALL group extracted yield values into bins of equal width.
3. THE Distribution_Service SHALL calculate the percentage of surveys falling into each bin (count in bin divided by total surveys with that Resource_Purity_Combo, multiplied by 100).
4. THE Distribution_Service SHALL return bin midpoints as X-axis values and percentage frequencies as Y-axis values.
5. IF fewer than 2 surveys contain the selected Resource_Purity_Combo, THEN THE Yield_Distribution_Tab SHALL display a message indicating insufficient data for that series.

### Requirement 6: Bin Size Configuration

**User Story:** As a player, I want to adjust the bin size, so that I can control the granularity of the distribution curve.

#### Acceptance Criteria

1. THE Yield_Distribution_Tab SHALL provide a numeric input for the user to set the bin width.
2. THE Yield_Distribution_Tab SHALL default the bin width to 10.
3. WHEN the user changes the bin width, THE Yield_Distribution_Tab SHALL recalculate and redraw all active distribution curves using the new bin width.
4. THE Yield_Distribution_Tab SHALL accept bin width values between 1 and 100 inclusive.
5. IF the user enters a bin width outside the valid range, THEN THE Yield_Distribution_Tab SHALL clamp the value to the nearest valid boundary.

### Requirement 7: Graph Axes

**User Story:** As a player, I want clearly labeled axes, so that I can interpret the distribution graph correctly.

#### Acceptance Criteria

1. THE Yield_Distribution_Tab SHALL label the X-axis as "Yield" with bin range values.
2. THE Yield_Distribution_Tab SHALL label the Y-axis as "% of Surveys".
3. THE Yield_Distribution_Tab SHALL scale the X-axis to encompass the full range of yield values across all active series.
4. THE Yield_Distribution_Tab SHALL scale the Y-axis from 0 to the maximum percentage value across all active series (with appropriate headroom).

### Requirement 8: Smooth Curve Rendering

**User Story:** As a player, I want the distribution displayed as a smooth curve rather than a stepped histogram, so that the visualization is easy to read and compare across series.

#### Acceptance Criteria

1. THE Yield_Distribution_Tab SHALL render each series as a smooth interpolated curve through the bin frequency points.
2. THE Yield_Distribution_Tab SHALL use distinct colors for each series line to enable visual differentiation.
3. THE Yield_Distribution_Tab SHALL render curves with sufficient line width for readability.

### Requirement 9: Empty State

**User Story:** As a player, I want clear feedback when no data is available, so that I understand why the graph is empty.

#### Acceptance Criteria

1. WHEN no surveys match the current filter, THE Yield_Distribution_Tab SHALL display a message indicating no surveys are available.
2. WHEN no series have been added, THE Yield_Distribution_Tab SHALL display a message prompting the user to add a resource+purity combination.
3. WHEN the survey type filter is set to "All", THE Yield_Distribution_Tab SHALL display a message instructing the user to select Planet or Asteroid.

### Requirement 10: Asteroid Cycle Rate Context

**User Story:** As a player, I want asteroid survey yields displayed with awareness of the CycleRate unit, so that the axis labeling is accurate for asteroid data.

#### Acceptance Criteria

1. WHILE the distribution is computed from Asteroid surveys, THE Yield_Distribution_Tab SHALL append "/cycle" to the X-axis label to indicate the yield unit.
2. WHILE the distribution is computed from Planet surveys, THE Yield_Distribution_Tab SHALL display the X-axis label as "Yield" without a unit suffix.

### Requirement 11: Service Architecture

**User Story:** As a developer, I want the distribution calculation logic separated from the UI, so that it can be tested independently and follows the existing service pattern.

#### Acceptance Criteria

1. THE Distribution_Service SHALL be a static class following the same pattern as EvolutionChainService.
2. THE Distribution_Service SHALL accept a collection of ReadOnlySurvey objects, a Resource_Purity_Combo identifier, and a bin width as inputs.
3. THE Distribution_Service SHALL return a data structure containing the series points (bin midpoint, percentage) suitable for graph rendering.
4. THE Distribution_Service SHALL have no dependency on UI components (System.Windows.Forms).
