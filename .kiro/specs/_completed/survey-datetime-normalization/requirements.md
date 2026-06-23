# Requirements Document

## Introduction

The survey scan date/time is currently stored as a raw game-format string (e.g. `"27JUL24-11:44p"`) in `Survey.DateTime`. This non-standard format prevents correct chronological sorting in the survey list view, makes date comparisons unreliable, and couples the internal data model to the game's display format. This feature normalizes the scan date/time into a proper `System.DateTime` on import, stores it in ISO 8601 format internally, displays it in the game format in the UI (so it matches what the player sees in-game), and migrates existing saved surveys from the raw game format. Editing is done via a DateTimePicker control that updates a read-only text box showing the game format.

## Glossary

- **Survey**: A data object representing a planet resource scan, stored in `PlayerData.json` and managed via `FormSurvey`.
- **Game_DateTime_Format**: The non-standard date/time format used in the game HTML: `ddMMMyy-hh:mmt` where `MMM` is a three-letter uppercase month abbreviation and `t` is `a` or `p` for AM/PM (e.g. `"27JUL24-11:44p"`).
- **ISO_Format**: ISO 8601 date/time string format `yyyy-MM-ddTHH:mm:ss` used for internal storage (e.g. `"2024-07-27T23:44:00"`).
- **Display_Format**: The game-style date/time format shown in the UI, matching what the player sees in-game: `ddMMMyy-hh:mmt` with uppercase month abbreviation (e.g. `"27JUL24-11:44p"`).
- **SurveyParser**: The class responsible for parsing survey HTML clipboard data into `Survey` objects.
- **SurveyDateTimeParser**: A new utility class responsible for parsing the Game_DateTime_Format into `System.DateTime` and formatting for display.
- **FormSurvey**: The WinForms form for viewing and editing survey data.
- **ListViewItemComparer**: The comparer used to sort the survey list view columns.
- **SurveyImportHelper**: The helper class that creates and merges survey data during clipboard import.
- **DateTimePicker**: A standard WinForms `DateTimePicker` control used to select date/time values via a calendar dropdown.

## Requirements

### Requirement 1: Parse Game DateTime Format on Import

**User Story:** As a player, I want the survey scan date/time to be parsed into a proper DateTime when imported from the clipboard, so that the date is stored in a standard format from the moment of import.

#### Acceptance Criteria

1. WHEN the SurveyParser extracts a date/time string matching the Game_DateTime_Format, THE SurveyDateTimeParser SHALL parse the string into a `System.DateTime` value.
2. WHEN the Game_DateTime_Format suffix is `p`, THE SurveyDateTimeParser SHALL interpret the time as PM (12-hour clock).
3. WHEN the Game_DateTime_Format suffix is `a`, THE SurveyDateTimeParser SHALL interpret the time as AM (12-hour clock).
4. WHEN the hour value is `12` with suffix `p`, THE SurveyDateTimeParser SHALL interpret the time as 12:00 PM (noon).
5. WHEN the hour value is `12` with suffix `a`, THE SurveyDateTimeParser SHALL interpret the time as 12:00 AM (midnight).
6. WHEN the two-digit year is parsed, THE SurveyDateTimeParser SHALL interpret it relative to the century (e.g. `24` → `2024`, `26` → `2026`).
7. THE SurveyDateTimeParser SHALL support all twelve three-letter uppercase month abbreviations (JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV, DEC).
8. IF the date/time string does not match the Game_DateTime_Format, THEN THE SurveyDateTimeParser SHALL return a failure result without throwing an exception.

### Requirement 2: Store DateTime in ISO 8601 Format

**User Story:** As a developer, I want the scan date/time stored in ISO 8601 format in the JSON data file, so that dates sort correctly and are unambiguous.

#### Acceptance Criteria

1. WHEN a survey is saved to `PlayerData.json`, THE Survey model SHALL store the `DateTime` field as an ISO_Format string (e.g. `"2024-07-27T23:44:00"`).
2. WHEN a survey is loaded from `PlayerData.json` containing an ISO_Format string, THE Survey model SHALL preserve the value without modification.
3. THE Survey model SHALL continue to use a `string` type for the `DateTime` property to maintain JSON serialization compatibility.

### Requirement 3: Display DateTime in Game Format

**User Story:** As a player, I want the scan date/time displayed in the game format in the survey form and list view, so that it matches what I see in-game and I can quickly understand when a scan was taken.

#### Acceptance Criteria

1. WHEN a survey is displayed in the FormSurvey text field, THE SurveyViewModel SHALL format the stored ISO_Format value into the Display_Format (e.g. `"27JUL24-11:44p"`).
2. WHEN a survey is displayed in the survey list view DateTime column, THE FormSurvey SHALL show the Display_Format value (e.g. `"27JUL24-11:44p"`).
3. IF the stored DateTime string cannot be parsed as ISO_Format, THEN THE SurveyViewModel SHALL display the raw stored string unchanged.

### Requirement 4: Correct Chronological Sorting in List View

**User Story:** As a player, I want the survey list to sort by date correctly across months and years, so that I can find the most recent or oldest surveys.

#### Acceptance Criteria

1. WHEN the user clicks the DateTime column header in the survey list view, THE ListViewItemComparer SHALL sort surveys in chronological order (ascending or descending).
2. WHILE the survey list view displays the Display_Format in the DateTime column, THE ListViewItemComparer SHALL use the underlying ISO_Format value stored in the ListViewItem Tag (or equivalent stored data) for date comparison rather than parsing the display text.
3. IF the underlying ISO_Format value cannot be parsed as a date, THEN THE ListViewItemComparer SHALL fall back to lexicographic comparison for that value.

### Requirement 5: Migrate Existing Saved Surveys

**User Story:** As a player with existing saved surveys, I want my old survey dates automatically converted to the new format, so that I do not lose data or need to re-import surveys.

#### Acceptance Criteria

1. WHEN `PlayerData.json` is loaded and a survey's DateTime field contains a Game_DateTime_Format string, THE migration logic SHALL parse it and convert the value to ISO_Format in memory.
2. WHEN the migration encounters a value that is not Game_DateTime_Format, THE migration logic SHALL attempt to parse it using common date/time formats (e.g. `DateTime.TryParse` with standard .NET format detection).
3. IF the migration successfully parses a value via any format (game or common), THE migration logic SHALL convert it to ISO_Format and persist the converted value on the next save operation.
4. IF a survey's DateTime field contains a value that cannot be parsed by any format, THE migration logic SHALL replace the value with the current date/time in ISO_Format so the survey remains sortable and the user can find and correct it.
5. IF a survey's DateTime field is null or empty, THE migration logic SHALL replace the value with the current date/time in ISO_Format.
6. IF a survey's DateTime field already contains a valid ISO_Format string, THE migration logic SHALL leave it unchanged.
7. THE migration logic SHALL process all surveys for all player profiles in a single pass during data load.
8. THE migration logic SHALL log a warning for each survey whose DateTime was replaced with the current date/time, including the original value and the survey's PlanetName for traceability.

### Requirement 6: DateTime Editing via DateTimePicker

**User Story:** As a player, I want to edit the scan date/time using a DateTimePicker control, so that I can select valid dates without manual typing errors.

#### Acceptance Criteria

1. THE FormSurvey SHALL display a read-only text box showing the scan date/time in Display_Format next to a DateTimePicker control.
2. WHEN the user selects a new date/time using the DateTimePicker, THE FormSurvey SHALL update the read-only text box to show the selected value in Display_Format.
3. WHEN the user saves a survey, THE SurveyViewModel SHALL convert the DateTimePicker value to ISO_Format for storage.
4. WHEN a survey is loaded into FormSurvey, THE FormSurvey SHALL set the DateTimePicker value from the stored ISO_Format date/time.
5. THE read-only text box SHALL not accept manual keyboard input (all date/time editing goes through the DateTimePicker).
6. WHEN a new survey is created in FormSurvey, THE DateTimePicker SHALL default to the current date/time and the text box SHALL show the current date/time in Display_Format.

### Requirement 7: Round-Trip Consistency

**User Story:** As a developer, I want parsing and formatting to be round-trip consistent, so that no data is lost through format conversions.

#### Acceptance Criteria

1. FOR ALL valid Game_DateTime_Format strings, parsing to `System.DateTime` then formatting to ISO_Format then parsing back to `System.DateTime` SHALL produce an equivalent DateTime value.
2. FOR ALL valid ISO_Format strings, parsing to `System.DateTime` then formatting to Display_Format then parsing back via Display_Format to `System.DateTime` SHALL produce an equivalent DateTime value.
3. FOR ALL valid `System.DateTime` values, formatting to Display_Format then parsing the Display_Format back to `System.DateTime` then formatting to ISO_Format SHALL produce the same ISO_Format string as formatting the original DateTime directly to ISO_Format.
