# Requirements Document

## Introduction

The colony form uses a TabControl with tabs for Structures, Workers, and other colony aspects. Currently there are no visual indicators on the tab selectors to warn the player about approaching limits or deadlines. This feature adds background color warnings to two tab selectors:

1. **Structures tab** (BL-030): Yellow at 60+ structures, red at 66+ (game cap is 65).
2. **Worker tab** (BL-031): Yellow when any unfulfilled request is due within 2 days, red when due within 1 day.

Both use the same pattern — a shared tab warning evaluation service that sets tab background colors based on colony state.

## Glossary

- **Colony_Form**: The `FormColony` WinForms form that manages colony data, structures, warehousing, and worker/commodity requests.
- **Structures_Tab**: The tab page within the Colony_Form's `tabDetailedData` TabControl that displays colony structures.
- **Worker_Tab**: The `tabPWorkers` tab page within the Colony_Form's `tabDetailedData` TabControl. Displays commodity request data including worker contracts.
- **Structure_Count**: The number of structures in `colony.Structures`.
- **Structure_Cap**: The game's maximum structure limit per colony (65).
- **Commodity_Request**: A `CommodityRequested` record on a colony, representing a pending worker or commodity need with a `Name`, `Requested` quantity, `Delivered` quantity, `NeedBy` date, and `Fulfilled` flag.
- **Due_Window**: The time remaining between now and a Commodity_Request's `NeedBy` date.
- **Tab_Warning_Service**: The logic responsible for evaluating colony state and determining the appropriate tab background colors for both Structures and Worker tabs.

## Requirements

### Requirement 7: Yellow Warning for Structure Count at 60+

**User Story:** As a player, I want the Structures tab to turn yellow when my colony has 60 or more structures, so that I get early warning that I'm approaching the game's structure cap.

#### Acceptance Criteria

1. WHILE the selected colony's Structure_Count is 60 or greater AND less than 66, THE Structures_Tab SHALL display a yellow background color.
2. WHEN the selected colony's Structure_Count is less than 60, THE Structures_Tab SHALL NOT display a yellow background color.

### Requirement 8: Red Warning for Structure Count at 66+

**User Story:** As a player, I want the Structures tab to turn red when my colony has 66 or more structures, so that I can see I've exceeded the game's structure cap.

#### Acceptance Criteria

1. WHILE the selected colony's Structure_Count is 66 or greater, THE Structures_Tab SHALL display a red background color.
2. THE red background SHALL take priority over the yellow background when Structure_Count is 66+.

### Requirement 9: Default Structures Tab Appearance

**User Story:** As a player, I want the Structures tab to look normal when I'm well below the structure cap.

#### Acceptance Criteria

1. WHEN the selected colony's Structure_Count is less than 60, THE Structures_Tab SHALL use the default tab appearance (UseVisualStyleBackColor = true, no custom background).

### Requirement 10: Structure Warning Updates on Data Changes

**User Story:** As a player, I want the Structures tab warning to update whenever structures are added or removed.

#### Acceptance Criteria

1. WHEN a colony is selected from the colony list, THE Tab_Warning_Service SHALL evaluate the Structures_Tab background color.
2. WHEN a structure is added or removed from the colony, THE Tab_Warning_Service SHALL re-evaluate the Structures_Tab background color.
3. WHEN colony HTML is imported (adding/merging structures), THE Tab_Warning_Service SHALL re-evaluate the Structures_Tab background color.

### Requirement 1: Red Warning for Requests Due Within 1 Day

**User Story:** As a player, I want the Worker tab to turn red when any worker request is due within 1 day, so that I can immediately see critical deadlines.

#### Acceptance Criteria

1. WHILE any unfulfilled Commodity_Request on the selected colony has a Due_Window of 1 day or less (and the NeedBy date is not `DateTime.MinValue`), THE Worker_Tab SHALL display a red background color.
2. WHEN all unfulfilled Commodity_Requests have a Due_Window greater than 1 day, THE Worker_Tab SHALL NOT display a red background color.
3. THE Tab_Warning_Service SHALL treat Commodity_Requests with a `Fulfilled` flag of true as excluded from due date evaluation.

### Requirement 2: Yellow Warning for Requests Due Within 2 Days

**User Story:** As a player, I want the Worker tab to turn yellow when any worker request is due within 2 days (but more than 1 day), so that I get early warning of approaching deadlines.

#### Acceptance Criteria

1. WHILE any unfulfilled Commodity_Request on the selected colony has a Due_Window of 2 days or less (and greater than 1 day, and the NeedBy date is not `DateTime.MinValue`), THE Worker_Tab SHALL display a yellow background color.
2. WHEN all unfulfilled Commodity_Requests have a Due_Window greater than 2 days, THE Worker_Tab SHALL NOT display a yellow background color.

### Requirement 3: Red Takes Priority Over Yellow

**User Story:** As a player, I want the most urgent warning to take precedence, so that I always see the most critical status.

#### Acceptance Criteria

1. WHEN both red and yellow conditions are met simultaneously (one request due within 1 day, another due within 2 days), THE Worker_Tab SHALL display the red background color.

### Requirement 4: Default Appearance When No Warnings Apply

**User Story:** As a player, I want the Worker tab to look normal when no requests are approaching their due date, so that I can tell at a glance that nothing needs attention.

#### Acceptance Criteria

1. WHEN no unfulfilled Commodity_Request has a Due_Window of 2 days or less, THE Worker_Tab SHALL use the default tab appearance (UseVisualStyleBackColor = true, no custom background).
2. WHEN the selected colony has no Commodity_Requests, THE Worker_Tab SHALL use the default tab appearance.

### Requirement 5: Warning Updates on Data Changes

**User Story:** As a player, I want the tab warning to update whenever I change request data, so that the visual indicator always reflects the current state.

#### Acceptance Criteria

1. WHEN a colony is selected from the colony list, THE Tab_Warning_Service SHALL evaluate the Worker_Tab background color.
2. WHEN a Commodity_Request's NeedBy date is changed, THE Tab_Warning_Service SHALL re-evaluate the Worker_Tab background color.
3. WHEN a Commodity_Request's Fulfilled flag is changed, THE Tab_Warning_Service SHALL re-evaluate the Worker_Tab background color.
4. WHEN a Commodity_Request is added or removed, THE Tab_Warning_Service SHALL re-evaluate the Worker_Tab background color.

### Requirement 6: Overdue Requests Treated as Red

**User Story:** As a player, I want overdue (past-due) requests to also trigger the red warning, so that I notice contracts that have already lapsed.

#### Acceptance Criteria

1. WHILE any unfulfilled Commodity_Request has a NeedBy date in the past (Due_Window is negative), THE Worker_Tab SHALL display a red background color.
