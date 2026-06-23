# Requirements Document

## Introduction

Window State Persistence adds UI polish to OE2EmpireTracker by remembering window positions, sizes, and form control state across application restarts. The feature covers the main MDI container, all MDI child forms, per-type window numbering, and per-window form state (filters, combo selections, grid column layout, and grid sorting). All preferences are stored in a single JSON file in the user's local application data folder.

## Glossary

- **Main_Window**: The top-level MDI container form (`MainWindow`) that hosts all child windows.
- **MDI_Child**: Any child form opened inside Main_Window. The set of MDI child form types is: FormColony, FormBlueprint, FormSurvey, FormPlayerProfile, FormDeliveryRoute, FormDeliveryExecution, FormColonyDailyBuild, FormColonyActivity.
- **Window_Number**: A per-form-type sequential integer assigned to each MDI_Child instance at open time, starting at 1. The number is fixed for the lifetime of the window.
- **Form_Type_Key**: A string identifier derived from the MDI_Child class name (e.g. "FormColony", "FormBlueprint") used as a top-level key in the preferences file.
- **Preferences_Store**: A service responsible for loading, saving, and providing access to the UIPreferences.json file.
- **UIPreferences_File**: The JSON file located at `%LOCALAPPDATA%\OE2EmpireTracker\UIPreferences.json` that persists all window state data.
- **Visible_Screen_Area**: The union of all active monitor working areas as reported by `System.Windows.Forms.Screen.AllScreens`.
- **Minimum_Window_Size**: The smallest allowed dimensions for any persisted window: 320 pixels wide by 200 pixels tall.
- **Screen_Edge_Margin**: A 100-pixel inset from the right and bottom edges of the relevant bounding area used during bounds validation.
- **Parent_Client_Area**: The MDI client area of Main_Window, used as the bounding area for MDI_Child bounds validation.

## Requirements

### Requirement 1: Main Window Position and Size Persistence

**User Story:** As a user, I want Main_Window to reopen at the same position and size I last used, so that I do not have to rearrange my workspace every time I launch the application.

#### Acceptance Criteria

1. WHEN the user closes Main_Window, THE Preferences_Store SHALL save the current Left, Top, Width, and Height of Main_Window to the UIPreferences_File.
2. WHEN the application starts and a saved Main_Window position and size exist in the UIPreferences_File, THE Main_Window SHALL restore its position and size to the saved values.
3. WHEN the application starts and no saved Main_Window state exists in the UIPreferences_File, THE Main_Window SHALL use its default position and size as defined by the WinForms designer.

### Requirement 2: Main Window Bounds Validation

**User Story:** As a user, I want Main_Window to always appear on a visible monitor, so that I can access the application even if my monitor configuration has changed since the last session.

#### Acceptance Criteria

1. WHEN Main_Window restores a saved position, IF the restored bounds place Main_Window entirely outside the Visible_Screen_Area, THEN THE Main_Window SHALL reposition itself to the default position on the primary monitor.
2. WHEN Main_Window restores a saved position, IF the restored bounds place the right edge of Main_Window within Screen_Edge_Margin pixels of the right edge of the Visible_Screen_Area, THEN THE Main_Window SHALL shift its Left position so that at least Screen_Edge_Margin pixels remain between the right edge of Main_Window and the right edge of the Visible_Screen_Area.
3. WHEN Main_Window restores a saved position, IF the restored bounds place the bottom edge of Main_Window within Screen_Edge_Margin pixels of the bottom edge of the Visible_Screen_Area, THEN THE Main_Window SHALL shift its Top position so that at least Screen_Edge_Margin pixels remain between the bottom edge of Main_Window and the bottom edge of the Visible_Screen_Area.
4. WHEN Main_Window restores a saved size, IF the saved Width is less than 320 pixels or the saved Height is less than 200 pixels, THEN THE Main_Window SHALL clamp the dimension to the Minimum_Window_Size.

### Requirement 3: MDI Child Window Numbering

**User Story:** As a user, I want each MDI child window to display a unique number in its title bar per form type, so that I can distinguish between multiple windows of the same type.

#### Acceptance Criteria

1. WHEN an MDI_Child is opened, THE Main_Window SHALL assign the next available Window_Number for that Form_Type_Key, starting at 1.
2. THE MDI_Child SHALL display its title in the format "#N - <Current_Title>" where N is the assigned Window_Number and Current_Title is the existing title text of the form.
3. WHEN an MDI_Child is closed, THE Main_Window SHALL retain the Window_Number counter for that Form_Type_Key so that subsequent windows of the same type receive the next sequential number.
4. WHEN an MDI_Child with Window_Number 1 is closed and an MDI_Child with Window_Number 2 remains open, THE MDI_Child with Window_Number 2 SHALL continue to display "#2" in its title bar.
5. THE Main_Window SHALL maintain a separate Window_Number counter for each Form_Type_Key.

### Requirement 4: MDI Child Position and Size Persistence

**User Story:** As a user, I want each MDI child window to reopen at the same position and size I last used for that specific window number, so that my workspace layout is preserved.

#### Acceptance Criteria

1. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the current Left, Top, Width, and Height of the MDI_Child to the UIPreferences_File, keyed by Form_Type_Key and Window_Number.
2. WHEN an MDI_Child is opened and a saved position and size exist for the matching Form_Type_Key and Window_Number, THE MDI_Child SHALL restore its position and size to the saved values.
3. WHEN an MDI_Child is opened and no saved state exists for the matching Form_Type_Key and Window_Number, THE MDI_Child SHALL use its default position and size as defined by the WinForms designer.

### Requirement 5: MDI Child Bounds Validation

**User Story:** As a user, I want MDI child windows to always appear within the visible client area of Main_Window, so that I can access them without manual repositioning.

#### Acceptance Criteria

1. WHEN an MDI_Child restores a saved position, IF the restored bounds place the MDI_Child entirely outside the Parent_Client_Area, THEN THE MDI_Child SHALL reposition itself to the default cascaded position within the Parent_Client_Area.
2. WHEN an MDI_Child restores a saved position, IF the restored bounds place the right edge of the MDI_Child within Screen_Edge_Margin pixels of the right edge of the Parent_Client_Area, THEN THE MDI_Child SHALL shift its Left position so that at least Screen_Edge_Margin pixels remain between the right edge of the MDI_Child and the right edge of the Parent_Client_Area.
3. WHEN an MDI_Child restores a saved position, IF the restored bounds place the bottom edge of the MDI_Child within Screen_Edge_Margin pixels of the bottom edge of the Parent_Client_Area, THEN THE MDI_Child SHALL shift its Top position so that at least Screen_Edge_Margin pixels remain between the bottom edge of the MDI_Child and the bottom edge of the Parent_Client_Area.
4. WHEN an MDI_Child restores a saved size, IF the saved Width is less than 320 pixels or the saved Height is less than 200 pixels, THEN THE MDI_Child SHALL clamp the dimension to the Minimum_Window_Size.

### Requirement 6: Filter Text Box State Persistence

**User Story:** As a user, I want filter text boxes on each form to remember their last entered text, so that I do not have to retype filters every time I reopen a window.

#### Acceptance Criteria

1. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the current text of each filter TextBox on the form to the UIPreferences_File, keyed by Form_Type_Key, Window_Number, and control name.
2. WHEN an MDI_Child is opened and saved filter text exists for the matching Form_Type_Key, Window_Number, and control name, THE MDI_Child SHALL restore the text of each filter TextBox to the saved value.
3. WHEN an MDI_Child is opened and no saved filter text exists for a given control, THE MDI_Child SHALL leave the filter TextBox at its default value.

### Requirement 7: Combo Box Selection State Persistence

**User Story:** As a user, I want combo box selections on each form to remember their last selected value, so that my preferred selections are preserved across sessions.

#### Acceptance Criteria

1. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the selected value or selected index of each ComboBox on the form to the UIPreferences_File, keyed by Form_Type_Key, Window_Number, and control name.
2. WHEN an MDI_Child is opened and a saved combo box selection exists for the matching Form_Type_Key, Window_Number, and control name, THE MDI_Child SHALL restore the ComboBox selection to the saved value if the value still exists in the ComboBox items, or to the saved index if the value is not found.
3. IF a saved ComboBox value no longer exists in the ComboBox items and the saved index exceeds the item count, THEN THE MDI_Child SHALL leave the ComboBox at its default selection.

### Requirement 8: Grid Column Width Persistence

**User Story:** As a user, I want DataGridView column widths to be remembered, so that I do not have to resize columns every time I reopen a window.

#### Acceptance Criteria

1. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the width of each column in every DataGridView on the form to the UIPreferences_File, keyed by Form_Type_Key, Window_Number, grid name, and column name.
2. WHEN an MDI_Child is opened and saved column widths exist for the matching DataGridView, THE MDI_Child SHALL restore each column width to the saved value.
3. WHEN an MDI_Child is opened and no saved column widths exist for a given DataGridView, THE MDI_Child SHALL use the default column widths as defined by the form designer or code.

### Requirement 9: Grid Column Order Persistence

**User Story:** As a user, I want DataGridView column order to be remembered, so that my preferred column arrangement is preserved across sessions.

#### Acceptance Criteria

1. THE MDI_Child SHALL set AllowUserToOrderColumns to true on every DataGridView within the form.
2. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the DisplayIndex of each column in every DataGridView on the form to the UIPreferences_File, keyed by Form_Type_Key, Window_Number, grid name, and column name.
3. WHEN an MDI_Child is opened and saved column display indices exist for the matching DataGridView, THE MDI_Child SHALL restore each column DisplayIndex to the saved value.
4. IF a saved column name no longer exists in the DataGridView, THEN THE Preferences_Store SHALL skip that column during restoration and log a debug-level message.

### Requirement 10: Grid Sort State Persistence

**User Story:** As a user, I want DataGridView sort column and direction to be remembered, so that my preferred data ordering is preserved across sessions.

#### Acceptance Criteria

1. THE MDI_Child SHALL enable sorting on every DataGridView column where the underlying data source supports sorting.
2. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the current sort column name and sort direction (ascending or descending) for every DataGridView on the form to the UIPreferences_File, keyed by Form_Type_Key, Window_Number, and grid name.
3. WHEN an MDI_Child is opened and a saved sort state exists for the matching DataGridView, THE MDI_Child SHALL apply the saved sort column and direction.
4. IF a saved sort column name no longer exists in the DataGridView, THEN THE MDI_Child SHALL leave the DataGridView unsorted and log a debug-level message.

### Requirement 11: UIPreferences File Storage

**User Story:** As a user, I want my UI preferences stored in a dedicated file in my local application data folder, so that preferences are isolated from game data and survive data file changes.

#### Acceptance Criteria

1. THE Preferences_Store SHALL read and write the UIPreferences_File at the path `%LOCALAPPDATA%\OE2EmpireTracker\UIPreferences.json`.
2. THE Preferences_Store SHALL use Newtonsoft.Json for serialization and deserialization of the UIPreferences_File.
3. THE Preferences_Store SHALL organize the JSON structure as a nested object keyed first by Form_Type_Key, then by Window_Number (as a string), then by property category (e.g. "Position", "FormState").
4. WHEN the UIPreferences_File does not exist, THE Preferences_Store SHALL create the directory and file on first save.
5. IF the UIPreferences_File contains malformed JSON, THEN THE Preferences_Store SHALL log an error, discard the corrupted data, and start with an empty preferences set.
6. IF a file I/O error occurs during save, THEN THE Preferences_Store SHALL log the error and continue application operation without crashing.

### Requirement 12: Preferences Save Timing

**User Story:** As a user, I want my preferences saved automatically at the right moments, so that I do not lose my layout if the application closes unexpectedly.

#### Acceptance Criteria

1. WHEN an MDI_Child is closed, THE Preferences_Store SHALL save the complete state of that MDI_Child (position, size, and form state) to the UIPreferences_File immediately.
2. WHEN Main_Window is closing, THE Preferences_Store SHALL save the Main_Window position and size to the UIPreferences_File before the application exits.
3. THE Preferences_Store SHALL write the entire UIPreferences_File atomically on each save to prevent partial writes.

### Requirement 13: Round-Trip Integrity of Preferences

**User Story:** As a developer, I want the preferences file to survive a full save-load-save cycle without data loss, so that I can trust the persistence layer.

#### Acceptance Criteria

1. FOR ALL valid UIPreferences data, serializing to JSON and then deserializing back SHALL produce an equivalent data structure (round-trip property).
2. THE Preferences_Store SHALL preserve preferences for Form_Type_Key and Window_Number combinations that are not currently open when saving preferences for other windows.
3. WHEN the UIPreferences_File is loaded, THE Preferences_Store SHALL ignore unknown keys without error, enabling forward compatibility.

### Requirement 14: Extensibility for New Forms and Controls

**User Story:** As a developer, I want the window state persistence system to be easy to adopt for any new UI form or control, so that all future forms automatically participate in state persistence without special effort.

#### Acceptance Criteria

1. THE Preferences_Store SHALL provide a simple API that any new Form can call to save and restore its state, requiring minimal boilerplate code.
2. ANY new MDI child form added to the application SHALL participate in the window numbering, position/size persistence, and form state persistence systems by following the established pattern.
3. ANY new DataGridView added to any form SHALL have AllowUserToOrderColumns set to true and sorting enabled, and its column widths, order, and sort state SHALL be persisted automatically through the same mechanism.
4. ANY new filter TextBox or ComboBox added to any form SHALL have its state persisted through the same mechanism.
5. THE implementation SHALL be documented in a steering file so that future development follows the pattern consistently.
