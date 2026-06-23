# Requirements Document

## Introduction

BL-061: Preferences Form — Configurable Thresholds and Intervals. The application currently hardcodes six warning threshold values in TabWarningService (structure count yellow/red, worker request due window yellow/red, colony import staleness yellow/red), a background processing interval in BackgroundProcessor (60 seconds), an administration status report refresh interval in FormColony (60 seconds), and a countdown display refresh rate used by four forms (ColonyStructure control, FormColonyActivity, PlayerSkillBlock, MainWindow) all hardcoded at 1000ms (1 second). This feature adds a Preferences form accessible from the main menu that lets the user configure all nine values. Time-based values are stored internally as seconds (long) and displayed/entered in countdown format (e.g. "5d 0h 0m 0s") using the existing ActivityRow.FormatSeconds() for display and a corresponding parser for input. Structure count thresholds are stored as integers. Values are persisted in the existing UIPreferences.json file alongside window state data and take effect immediately without restarting the application.

## Glossary

- **Preferences_Form**: A modal WinForms dialog accessible from the main menu that displays labeled inputs for each configurable value.
- **TabWarningService**: A static service that evaluates colony state and returns a TabWarningLevel (None, Yellow, Red) for tab background colors.
- **BackgroundProcessor**: A service that runs periodic background processing cycles on a timer, currently hardcoded to a 60-second interval (TickIntervalMs = 60_000).
- **FormColony**: The colony management form containing an administration status report that refreshes on a timer, currently hardcoded to a 60-second interval (timerAdminRefresh.Interval = 60000).
- **PreferencesStore**: A singleton service that loads and saves UIPreferences.json from %LOCALAPPDATA%\OE2EmpireTracker\.
- **UIPreferences**: The model class representing the contents of UIPreferences.json, currently holding window state data.
- **ThresholdPreferences**: A new model class within UIPreferences that holds the nine configurable values.
- **MainWindow**: The main MDI parent window containing the application menu bar and a status bar "next process" countdown timer (timerNextProcess.Interval = 1000).
- **ColonyStructure_Control**: A WinForms user control for individual colony structures containing a countdown display timer (timerCountdown.Interval = 1000).
- **FormColonyActivity**: The colony activity form containing an activity grid with countdown refresh (timerRefresh.Interval = 1000).
- **PlayerSkillBlock**: A WinForms user control for player skill training containing a countdown display timer (timerCountdown.Interval = 1000).
- **CountdownFormat**: A human-readable time format using the pattern "Xd Yh Zm Ws" (e.g. "5d 0h 0m 0s" for 432000 seconds), produced by ActivityRow.FormatSeconds() and parsed back to seconds for storage.
- **ActivityRow**: A model class in ColonyActivityCollector containing the static FormatSeconds(long) method that converts seconds to countdown format strings.

## Requirements

### Requirement 1: Threshold Preferences Model

**User Story:** As a developer, I want configurable values stored in a dedicated model class within UIPreferences, so that thresholds and intervals are cleanly separated from window state data.

#### Acceptance Criteria

1. THE ThresholdPreferences SHALL contain an integer property for the structure count yellow threshold with a default value of 60.
2. THE ThresholdPreferences SHALL contain an integer property for the structure count red threshold with a default value of 66.
3. THE ThresholdPreferences SHALL contain a long property for the worker request due window yellow threshold stored as seconds with a default value of 172800 (2 days).
4. THE ThresholdPreferences SHALL contain a long property for the worker request due window red threshold stored as seconds with a default value of 86400 (1 day).
5. THE ThresholdPreferences SHALL contain a long property for the colony import staleness yellow threshold stored as seconds with a default value of 432000 (5 days).
6. THE ThresholdPreferences SHALL contain a long property for the colony import staleness red threshold stored as seconds with a default value of 518400 (6 days).
7. THE ThresholdPreferences SHALL contain a long property for the background processing interval stored as seconds with a default value of 60.
8. THE ThresholdPreferences SHALL contain a long property for the administration status report refresh time stored as seconds with a default value of 60.
9. THE ThresholdPreferences SHALL contain a long property for the countdown display refresh rate stored as seconds with a default value of 1.
10. THE UIPreferences SHALL contain a ThresholdPreferences property named Thresholds.
11. WHEN the Thresholds property is null during deserialization, THE PreferencesStore SHALL initialize the Thresholds property with a new ThresholdPreferences instance containing default values.

### Requirement 2: Preferences Form UI

**User Story:** As a user, I want a Preferences form with labeled inputs for each configurable value, so that I can adjust warning thresholds and processing intervals to my play style.

#### Acceptance Criteria

1. THE Preferences_Form SHALL display nine labeled input controls, one for each configurable value.
2. THE Preferences_Form SHALL group the inputs into six labeled sections: Structure Count, Worker Request Due Window, Colony Import Staleness, Background Processing, Administration Report, and Countdown Display.
3. THE Preferences_Form SHALL display the current values from PreferencesStore when opened.
4. THE Preferences_Form SHALL display structure count thresholds as plain integer inputs.
5. THE Preferences_Form SHALL display time-based values (worker request due window yellow/red, colony import staleness yellow/red, background processing interval, administration status report refresh time, countdown display refresh rate) in countdown format using ActivityRow.FormatSeconds() for rendering.
6. THE Preferences_Form SHALL accept time-based value input in countdown format (e.g. "5d 0h 0m 0s") and parse the input back to seconds for storage.
7. THE Preferences_Form SHALL include an OK button that saves changes and closes the form.
8. THE Preferences_Form SHALL include a Cancel button that discards changes and closes the form.
9. THE Preferences_Form SHALL include a Reset to Defaults button that restores all nine values to the hardcoded defaults.
10. WHEN the user clicks OK, THE Preferences_Form SHALL write the updated values to PreferencesStore and trigger a save to UIPreferences.json.

### Requirement 3: Menu Integration

**User Story:** As a user, I want to access the Preferences form from the main menu, so that I can find and adjust settings without searching.

#### Acceptance Criteria

1. THE MainWindow SHALL display a "Preferences..." menu item under the File menu, positioned before the Exit separator.
2. WHEN the user clicks the "Preferences..." menu item, THE MainWindow SHALL open the Preferences_Form as a modal dialog.

### Requirement 4: TabWarningService Reads from Preferences

**User Story:** As a user, I want TabWarningService to use my configured thresholds instead of hardcoded constants, so that changes I make in the Preferences form affect warning behavior.

#### Acceptance Criteria

1. THE TabWarningService SHALL read the structure count yellow threshold from PreferencesStore instead of the hardcoded constant 60.
2. THE TabWarningService SHALL read the structure count red threshold from PreferencesStore instead of the hardcoded constant 66.
3. THE TabWarningService SHALL read the worker request due window yellow threshold in seconds from PreferencesStore instead of the hardcoded constant of 2 days.
4. THE TabWarningService SHALL read the worker request due window red threshold in seconds from PreferencesStore instead of the hardcoded constant of 1 day.
5. THE TabWarningService SHALL read the colony import staleness yellow threshold in seconds from PreferencesStore instead of the hardcoded constant of 5 days.
6. THE TabWarningService SHALL read the colony import staleness red threshold in seconds from PreferencesStore instead of the hardcoded constant of 6 days.
7. WHEN the user saves new threshold values via the Preferences_Form, THE TabWarningService SHALL use the updated values on the next evaluation call without requiring an application restart.

### Requirement 5: BackgroundProcessor Reads Interval from Preferences

**User Story:** As a user, I want the background processing interval to use my configured value instead of the hardcoded 60-second constant, so that I can control how frequently background processing runs.

#### Acceptance Criteria

1. THE BackgroundProcessor SHALL read the processing interval in seconds from PreferencesStore instead of the hardcoded constant TickIntervalMs of 60000 milliseconds.
2. WHEN the user saves a new background processing interval via the Preferences_Form, THE BackgroundProcessor SHALL use the updated interval on the next timer reschedule without requiring an application restart.

### Requirement 6: FormColony Admin Refresh Reads from Preferences

**User Story:** As a user, I want the administration status report refresh interval to use my configured value instead of the hardcoded 60-second constant, so that I can control how frequently the admin report refreshes.

#### Acceptance Criteria

1. THE FormColony SHALL read the administration status report refresh time in seconds from PreferencesStore instead of the hardcoded timerAdminRefresh.Interval of 60000 milliseconds.
2. WHEN the user saves a new administration status report refresh time via the Preferences_Form, THE FormColony SHALL use the updated interval on the next timer cycle without requiring an application restart.

### Requirement 7: Input Validation

**User Story:** As a user, I want the Preferences form to prevent me from entering invalid values, so that the warning system and processing intervals continue to function correctly.

#### Acceptance Criteria

1. THE Preferences_Form SHALL enforce that all threshold and interval values are positive numbers.
2. THE Preferences_Form SHALL enforce that the structure count yellow threshold is less than the structure count red threshold.
3. THE Preferences_Form SHALL enforce that the worker request due window yellow threshold is greater than the worker request due window red threshold (yellow triggers at a larger remaining window than red).
4. THE Preferences_Form SHALL enforce that the colony import staleness yellow threshold is less than the colony import staleness red threshold.
5. THE Preferences_Form SHALL enforce that time-based input strings conform to countdown format and parse to a positive number of seconds.
6. THE Preferences_Form SHALL enforce that the countdown display refresh rate is at least 1 second.
7. IF the user enters values that violate a validation rule, THEN THE Preferences_Form SHALL display a descriptive error message and prevent saving.

### Requirement 8: Countdown Format Parsing

**User Story:** As a developer, I want a parser that converts countdown format strings (e.g. "5d 0h 0m 0s") back to seconds, so that time-based preferences can be entered in a human-readable format and stored as seconds.

#### Acceptance Criteria

1. THE CountdownFormat parser SHALL accept strings in the format "Xd Yh Zm Ws" where X, Y, Z, W are non-negative integers.
2. THE CountdownFormat parser SHALL return the total number of seconds represented by the input string.
3. THE CountdownFormat parser SHALL accept partial formats where one or more components are omitted (e.g. "5d" or "2h 30m").
4. IF an invalid countdown format string is provided, THEN THE CountdownFormat parser SHALL return an error indication.
5. FOR ALL valid seconds values, parsing the output of ActivityRow.FormatSeconds() SHALL produce the original seconds value (round-trip property).

### Requirement 9: Backward Compatibility

**User Story:** As a user upgrading from a previous version, I want the application to work correctly with an existing UIPreferences.json that has no threshold data, so that the upgrade is seamless.

#### Acceptance Criteria

1. WHEN UIPreferences.json exists but contains no Thresholds property, THE PreferencesStore SHALL use default values matching the previously hardcoded constants.
2. WHEN UIPreferences.json does not exist, THE PreferencesStore SHALL use default values matching the previously hardcoded constants.
3. THE PreferencesStore SHALL serialize the Thresholds property into UIPreferences.json only after the user explicitly saves preferences via the Preferences_Form or after any other save trigger.

### Requirement 10: Countdown Display Forms Read Refresh Rate from Preferences

**User Story:** As a user, I want the countdown display refresh rate to use my configured value instead of the hardcoded 1-second interval, so that I can control how frequently countdown timers update across all four forms.

#### Acceptance Criteria

1. THE ColonyStructure_Control SHALL read the countdown display refresh rate in seconds from PreferencesStore instead of the hardcoded timerCountdown.Interval of 1000 milliseconds.
2. THE FormColonyActivity SHALL read the countdown display refresh rate in seconds from PreferencesStore instead of the hardcoded timerRefresh.Interval of 1000 milliseconds.
3. THE PlayerSkillBlock SHALL read the countdown display refresh rate in seconds from PreferencesStore instead of the hardcoded timerCountdown.Interval of 1000 milliseconds.
4. THE MainWindow SHALL read the countdown display refresh rate in seconds from PreferencesStore instead of the hardcoded timerNextProcess.Interval of 1000 milliseconds.
5. WHEN the user saves a new countdown display refresh rate via the Preferences_Form, THE ColonyStructure_Control, FormColonyActivity, PlayerSkillBlock, and MainWindow SHALL use the updated interval on the next timer cycle without requiring an application restart.
6. THE FormColonyDailyBuild and FormPlayerProfile forms SHALL inherit the configured countdown display refresh rate through their hosted ColonyStructure_Control and PlayerSkillBlock controls respectively.
