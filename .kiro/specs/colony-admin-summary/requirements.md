# Requirements Document

## Introduction

The Colony form's Administration tab (`tabPAdministration`) currently contains only Bootstrap and Optimize buttons. This feature transforms it into a per-colony status report by adding a formatted RichTextBox that summarizes the colony's active processes and idle structures. The existing `ColonyActivityCollector` and `ColonyInactivityCollector` services already collect this data and accept a colony list parameter — passing a single-colony list scopes the data to the selected colony.

The report is rendered using `RtfBuilder` (colored text runs assigned to `RichTextBox.Rtf` in a single operation) and refreshes every 60 seconds to match the background processor cycle. The report is ordered to surface the most actionable items first: building completion, then inactivities (things that need attention), then active processes (sorted by soonest completion). Non-repeating activities include the expected completion time in the user's local timezone.

This feature consolidates backlog items BL-030 and BL-039.

## Glossary

- **Colony_Form**: The form (`FormColony`) that displays and edits individual colony data, including the Administration tab.
- **Administration_Tab**: The first tab on the Colony_Form's tab control (`tabPAdministration`), which displays colony-level administrative controls and the new status report.
- **Status_Report**: A read-only `RichTextBox` embedded in the Administration_Tab that displays a formatted text summary of the colony's activity and inactivity state.
- **RtfBuilder**: The existing utility class (`OE2EmpireTracker.Controls.RtfBuilder`) that builds RTF strings from colored text runs. The full RTF is assigned to `RichTextBox.Rtf` in one operation to avoid flicker.
- **ColonyActivityCollector**: The static service (`ColonyActivityCollector.CollectActivities`) that scans colonies and returns `ActivityRow` instances for active timers and unfulfilled commodity requests.
- **ColonyInactivityCollector**: The static service (`ColonyInactivityCollector.CollectInactivities`) that scans colonies and returns `ActivityRow` instances for idle structures, underutilized refiners, and import staleness.
- **ActivityRow**: The data class representing a single activity or inactivity entry, containing type, system name, colony name, source name, process details, and countdown/NeedBy information.
- **Non_Repeating_Activity**: An activity whose `CountDown.IsRepeating` is false — Building, Manufacturing, CommodityManufacturing, Research. These have a definite completion time (`CountDown.EndTime`).
- **Repeating_Activity**: An activity whose `CountDown.IsRepeating` is true — Mining, Refining. These cycle hourly and have no meaningful completion time.
- **Completion_Time**: For Non_Repeating_Activity rows, the `CountDown.EndTime` converted to the user's local timezone via `.ToLocalTime()` and formatted for display.
- **PlayerContext**: The singleton service that manages player data and raises `ColonyDataChanged` and `CurrentPlayerChanged` events.
- **Refresh_Timer**: A `System.Windows.Forms.Timer` that periodically rebuilds the Status_Report content.

## Requirements

### Requirement 1: Status Report on Administration Tab

**User Story:** As a player, I want to see a formatted status report for the selected colony on its Administration tab, so that I can assess the colony's operational status at a glance without opening the empire-wide Colony Activity form.

#### Acceptance Criteria

1. WHEN a colony is selected in the Colony_Form, THE Administration_Tab SHALL display a Status_Report containing activity and inactivity data scoped to the selected colony.
2. THE Status_Report SHALL be a read-only `RichTextBox` control that the user cannot edit.
3. THE Status_Report SHALL display both activity data (active timers, commodity requests) and inactivity data (idle structures, import staleness) in a single combined view.
4. WHEN no colony is selected, THE Status_Report SHALL be empty.

### Requirement 2: Report Content — Building Section

**User Story:** As a player, I want building status to be the first thing I see, since building is a daily critical activity and I need to know when to come back.

#### Acceptance Criteria

1. THE Building section SHALL appear first in the report, before all other sections.
2. EACH Building row SHALL display the source name, countdown remaining, and Completion_Time in the user's local timezone (both relative countdown and local time).
3. WHEN there are multiple structures building, THE rows SHALL be sorted by soonest completion first.
4. WHEN there are no structures building, THE Building section SHALL be omitted.

### Requirement 2a: Report Content — Commodity Requests Section

**User Story:** As a player, I want to see unfulfilled commodity requests right after building status, since missing a delivery deadline can stall colony operations.

#### Acceptance Criteria

1. THE Commodity Requests section SHALL appear after the Building section and before the Inactivity section.
2. EACH Commodity Request row SHALL display the commodity name, requested quantity, and NeedBy date if set.
3. WHEN there are no unfulfilled commodity requests, THE Commodity Requests section SHALL be omitted.

### Requirement 3: Report Content — Inactivity Section

**User Story:** As a player, I want to see what needs my attention in the colony — idle structures and stale imports — right after building status, so I can act on them.

#### Acceptance Criteria

1. THE inactivity section SHALL appear after the Building section and before the remaining activity sections.
2. THE inactivity section SHALL display groups in this fixed order:
   a. Colony Import Staleness
   b. Idle Mining
   c. Idle Refining
   d. Idle Manufacturing
   e. Idle Commodity Manufacturing
   f. Idle Research
   g. Underutilized Refining
3. EACH inactivity group SHALL have a colored header identifying the group.
4. EACH inactivity row SHALL display the source name and process details.
5. WHEN an inactivity group has zero rows, THAT group SHALL be omitted.
6. WHEN the entire inactivity section has zero rows, THE section SHALL be omitted.

### Requirement 4: Report Content — Activity Section

**User Story:** As a player, I want to see what is actively running in my colony, sorted by what finishes soonest, so I know what processes are in progress and when they will complete.

#### Acceptance Criteria

1. THE activity section SHALL appear after the inactivity section.
2. THE activity section SHALL include all active processes except Building and Commodity Requests (which have their own sections): Manufacturing, Commodity Manufacturing, Research, Mining, and Refining.
3. Manufacturing, Commodity Manufacturing, and Research rows SHALL be sorted by seconds remaining in ascending order (soonest completion first).
4. FOR Manufacturing and Commodity Manufacturing rows with multiple quantity runs, THE row SHALL display both the next item completion (countdown + local time) and the full batch completion (countdown + local time).
5. FOR Research rows, THE row SHALL display the countdown remaining and Completion_Time in the user's local timezone.
6. FOR Mining rows, THE report SHALL display one summary row per resource being mined, showing the resource name, purity, and aggregate mining rate per hour across all miners on that resource to 2 decimal places (e.g. "Halogen (High) — 150.00/h").
7. FOR Refining rows, THE report SHALL display one summary row per resource+purity being refined, aggregating across all refiners on that combination, showing the input resource, purity, consume rate total, and produce rate total to 2 decimal places (e.g. "3x Halogen (High) — 75.00:375.00 Halogen"). For synthetic refining recipes where the output resource differs from the input, the output resource name SHALL be shown instead.
8. WHEN the activity section has zero rows, THE section SHALL be omitted.
9. ALL completion times (Building, Manufacturing, Commodity Manufacturing, Research) SHALL display both the relative countdown and the local user timezone time.

### Requirement 5: Report Formatting

**User Story:** As a player, I want the status report to be visually clear and easy to scan, so that I can quickly understand the colony's state.

#### Acceptance Criteria

1. THE Status_Report SHALL use colored text via RtfBuilder to distinguish section headers, activity types, and status information.
2. THE group headers SHALL use a distinct color to stand out from the row content.
3. THE countdown values SHALL use a color that distinguishes them from descriptive text.
4. THE Completion_Time values SHALL use a color that distinguishes them from countdown values.
5. THE entire RTF content SHALL be built using RtfBuilder and assigned to `RichTextBox.Rtf` in a single operation to avoid flicker.

### Requirement 6: Periodic Refresh

**User Story:** As a player, I want the status report to refresh periodically so that countdown values and completion times stay reasonably current.

#### Acceptance Criteria

1. WHILE the Colony_Form is open and a colony is selected, THE Refresh_Timer SHALL tick at a 60-second interval.
2. WHEN the Refresh_Timer ticks, THE Status_Report SHALL rebuild its content by re-collecting activity and inactivity data for the selected colony and regenerating the RTF.
3. IF the Colony_Form is disposed, THEN THE Refresh_Timer SHALL stop and release resources.

### Requirement 7: Data Refresh on Colony Change and External Events

**User Story:** As a player, I want the status report to refresh automatically when I select a different colony or when colony data changes externally, so that the report always reflects the current state.

#### Acceptance Criteria

1. WHEN the user selects a different colony in the Colony_Form list, THE Status_Report SHALL rebuild with data scoped to the newly selected colony.
2. WHEN the PlayerContext raises a ColonyDataChanged event for the selected colony, THE Status_Report SHALL rebuild its content.
3. WHEN the PlayerContext raises a CurrentPlayerChanged event, THE Status_Report SHALL clear its content.

### Requirement 8: Layout and Coexistence with Existing Controls

**User Story:** As a player, I want the status report to coexist with the existing Bootstrap and Optimize buttons on the Administration tab, so that I retain access to those functions.

#### Acceptance Criteria

1. THE Administration_Tab SHALL retain the existing Bootstrap and Optimize buttons in their current position at the top of the tab.
2. THE Status_Report SHALL fill the remaining vertical space below the existing buttons.
3. WHEN the Colony_Form is resized, THE Status_Report SHALL resize to fill available space within the Administration_Tab.
