# Requirements Document

## Introduction

Track when each colony was last imported and surface that information across the application. When a colony import occurs (via `ColonyImportHelper.CreateFromTemp` or `ColonyImportHelper.MergeIdentity`), the system records the current UTC timestamp in ISO 8601 format on the Colony data model. This timestamp drives two consumer features: an import staleness row on the Colony Activity form's inactivity mode (staleness is something that should be done but hasn't — an inactivity concept), and a color-coded warning on the Colony form's Administration tab. Existing colonies without a timestamp are backfilled with the current time via migration. Merges backlog items BL-031 (Colony Import Timestamp Tracking) and BL-049 (Colony Import Timestamp — Surface on Activity Window).

This spec also retrofits the existing `SurveyDateTimeParser` to be UTC-aware. The game operates in UTC timezone, so all game-parsed datetimes and all application-generated timestamps are stored as UTC. Display conversions to the user's local timezone are applied at the UI layer. The survey datetime normalization code has not shipped to anyone yet, so the UTC fix is a direct code change with no migration concerns.

## Glossary

- **Colony**: The core data model representing a player's colony on a planet, defined in `Colony.cs`.
- **Colony_Import**: The act of parsing game clipboard HTML and creating or updating a Colony via `ColonyImportHelper.CreateFromTemp` (new colony) or `ColonyImportHelper.MergeIdentity` (existing colony).
- **LastImportDateTime**: A new string property on Colony storing the most recent import timestamp in ISO 8601 UTC format (`yyyy-MM-ddTHH:mm:ssZ`), following the same pattern as `SurveyDateTimeParser.IsoFormat`.
- **UTC**: All datetimes are stored as UTC. The game operates in UTC timezone. Application-generated timestamps use `DateTime.UtcNow`. Display in the UI converts to the user's local timezone via `DateTime.ToLocalTime()`.
- **Import_Staleness**: The elapsed time between the current time and the colony's LastImportDateTime, displayed as "Xd Yh since last import" rather than a countdown to zero. This is an inactivity concept — something that should be done but hasn't been.
- **Colony_Activity_Form**: The form (`FormColonyActivity`) that displays activity and inactivity rows across all colonies for the current player.
- **Colony_Form**: The form (`FormColony`) that displays and edits individual colony data, including the Administration tab.
- **Administration_Tab**: The first tab on the Colony_Form's tab control (`tabPAdministration`), which will receive staleness-based color warnings.
- **TabWarningService**: The pure static service that evaluates colony state and returns `TabWarningLevel` values (None, Yellow, Red) for tab background coloring.
- **Staleness_Yellow_Threshold**: The number of days since last import at or above which the Administration_Tab turns yellow. Default: 5 days. Will eventually be configurable via BL-061 (Preferences Form).
- **Staleness_Red_Threshold**: The number of days since last import at or above which the Administration_Tab turns red. Default: 6 days. Will eventually be configurable via BL-061 (Preferences Form).
- **ColonyActivityCollector**: The service that scans colonies and returns `ActivityRow` instances for active timers and commodity requests.
- **ColonyInactivityCollector**: The service that scans colonies and returns `ActivityRow` instances for idle or underutilized structures. Import staleness rows belong here because staleness is an inactivity concept.
- **ActivityType**: The enum categorizing activity rows (Building, Manufacturing, Mining, Refining, etc.).
- **Import_Staleness_ActivityType**: A new `ActivityType` enum value representing import staleness rows on the Colony_Activity_Form.

## Requirements

### Requirement 1: Record Import Timestamp on Colony Data Model

**User Story:** As a player, I want the system to record when each colony was last imported, so that I can see how fresh my colony data is.

#### Acceptance Criteria

1. THE Colony data model SHALL include a `LastImportDateTime` string property that stores the import timestamp in ISO 8601 UTC format (`yyyy-MM-ddTHH:mm:ssZ`).
2. THE Colony data model SHALL serialize `LastImportDateTime` to JSON and deserialize it from JSON using Newtonsoft.Json, consistent with existing Colony properties.
3. WHEN `LastImportDateTime` is null or empty, THE Colony data model SHALL treat the colony as never-imported without throwing exceptions.

### Requirement 2: Stamp Import Timestamp During Colony Import

**User Story:** As a player, I want the import timestamp to be set automatically when I import colony data, so that I do not need to track it manually.

#### Acceptance Criteria

1. WHEN `ColonyImportHelper.CreateFromTemp` creates a new Colony, THE ColonyImportHelper SHALL set `LastImportDateTime` on the new Colony to the current UTC date and time in ISO 8601 format.
2. WHEN `ColonyImportHelper.MergeIdentity` merges identity fields into an existing Colony, THE ColonyImportHelper SHALL update `LastImportDateTime` on the target Colony to the current UTC date and time in ISO 8601 format.
3. THE ColonyImportHelper SHALL format the timestamp using `SurveyDateTimeParser.ToIsoString(DateTime.UtcNow)` to ensure consistent UTC ISO formatting across the application.

### Requirement 3: Migrate Existing Colonies

**User Story:** As a player with existing colony data, I want the system to backfill import timestamps on my existing colonies, so that the feature works immediately after upgrade.

#### Acceptance Criteria

1. WHEN a Colony's `LastImportDateTime` is null or empty during migration, THE migration logic SHALL set it to the current UTC date and time in ISO 8601 format.
2. WHEN a Colony's `LastImportDateTime` already contains a valid ISO 8601 string, THE migration logic SHALL leave it unchanged.
3. THE migration SHALL process all colonies across all player profiles in a single pass during data load, following the existing MigrationRunner pattern.

### Requirement 4: Surface Import Staleness on Colony Activity Form (Inactivity Mode)

**User Story:** As a player, I want to see which colonies have stale imports on the Colony Activity form's inactivity mode, so that I can prioritize reimporting colonies that may have missed commodity requests.

#### Acceptance Criteria

1. THE ActivityType enum SHALL include an `ImportStaleness` value for import staleness rows.
2. WHEN collecting inactivity data, THE ColonyInactivityCollector SHALL generate an ActivityRow for each colony where `LastImportDateTime` is older than 1 day.
3. THE ActivityRow for import staleness SHALL display the elapsed time since last import in "Xd Yh since last import" format (e.g., "5d 3h since last import") in the ProcessDetails field.
4. THE Colony_Activity_Form SHALL include an `ImportStaleness` filter checkbox in the inactivity mode, consistent with existing inactivity type filter checkboxes.
5. THE ActivityRow for import staleness SHALL set the ColonyName and SystemName fields from the colony, and set SourceName to "Colony Import" for display consistency.

### Requirement 5: Color Administration Tab Based on Import Staleness

**User Story:** As a player, I want the Administration tab on the Colony form to change color based on how stale the colony's import is, so that I can see at a glance which colonies need reimporting.

#### Acceptance Criteria

1. THE TabWarningService SHALL include an `EvaluateImportStalenessWarning` method that accepts a `LastImportDateTime` string and the current DateTime, and returns a `TabWarningLevel`.
2. WHILE the elapsed time since `LastImportDateTime` is at or above the Staleness_Red_Threshold (6 days), THE TabWarningService SHALL return `TabWarningLevel.Red`.
3. WHILE the elapsed time since `LastImportDateTime` is at or above the Staleness_Yellow_Threshold (5 days) and below the Staleness_Red_Threshold, THE TabWarningService SHALL return `TabWarningLevel.Yellow`.
4. WHILE the elapsed time since `LastImportDateTime` is below the Staleness_Yellow_Threshold, THE TabWarningService SHALL return `TabWarningLevel.None`.
5. WHILE `LastImportDateTime` is null or empty, THE TabWarningService SHALL return `TabWarningLevel.Red`.
6. THE Colony_Form SHALL call `EvaluateImportStalenessWarning` in `UpdateTabWarnings` and apply the result to the Administration_Tab using the existing `ApplyTabWarning` method.
7. THE staleness thresholds SHALL be hardcoded constants for now, with the expectation that BL-061 (Preferences Form) will make them configurable in the future.

### Requirement 6: Display Elapsed Time Format

**User Story:** As a player, I want import staleness displayed as elapsed time since the last event, so that I can quickly understand how old my colony data is.

#### Acceptance Criteria

1. THE import staleness display SHALL format elapsed time as "Xd Yh since last import" where X is days and Y is hours (e.g., "5d 3h since last import").
2. WHEN the elapsed time is less than 1 day, THE import staleness display SHALL format as "Xh Ym since last import" (e.g., "3h 15m since last import").
3. THE import staleness display SHALL use the existing `ActivityRow.FormatSeconds` method for time formatting, appending " since last import" to the result.

### Requirement 7: UTC Storage and Local Display for All DateTimes

**User Story:** As a player, I want all timestamps stored in UTC so that my data is portable and correct regardless of timezone, and displayed in my local timezone so I can understand when things happened.

#### Acceptance Criteria

1. THE game operates in UTC timezone. ALL game-parsed datetimes (survey scan dates) SHALL be treated as UTC values.
2. ALL application-generated timestamps (colony import, migration fallbacks) SHALL use `DateTime.UtcNow` instead of `DateTime.Now`.
3. THE `SurveyDateTimeParser.IsoFormat` SHALL be updated to `yyyy-MM-ddTHH:mm:ssZ` to include the UTC timezone indicator.
4. THE `SurveyDateTimeParser.ToIsoString` SHALL format DateTime values with the `Z` suffix.
5. THE `SurveyDateTimeParser.TryParseIso` SHALL parse ISO strings with or without the `Z` suffix, using `DateTimeStyles.AdjustToUniversal` to produce UTC DateTime values.
6. THE `SurveyDateTimeParser.FormatForDisplay` SHALL convert the UTC DateTime to local time before formatting to game format.
7. THE FormSurvey DateTimePicker SHALL display and accept values in the user's local timezone, converting to/from UTC for storage.
8. THE survey datetime migration (`Migration003`) SHALL use `DateTime.UtcNow` for its fallback timestamp.
9. THESE changes to `SurveyDateTimeParser` require no data migration because the survey datetime normalization has not shipped to any users yet.

### Requirement 8: Migrate All DateTime.Now Usage to DateTime.UtcNow

**User Story:** As a player, I want all timers and timestamps in the application to use UTC consistently, so that my data is correct regardless of timezone changes.

#### Acceptance Criteria

1. ALL `DateTime.Now` references in the main project SHALL be replaced with `DateTime.UtcNow`, except where the value is used purely for local UI display (e.g. DateTimePicker default values shown to the user).
2. THE `CountDownTime` model SHALL use `DateTime.UtcNow` in all its internal calculations (`TimeRemaining` getter/setter, `IntervalsPassed`, `StartRepeating`, `ConsumeIntervals`, `GetNextIntervalBoundary`).
3. ALL callers that set `CountDownTime.StartTime` SHALL use `DateTime.UtcNow` instead of `DateTime.Now`. This includes `ColonyStructure.cs`, `PlayerSkillBlock.cs`, `MinerSetupHelper.cs`, `RefinerySetupHelper.cs`.
4. THE `ColonyActivityCollector` SHALL use `DateTime.UtcNow` for elapsed time calculations.
5. THE `BackgroundProcessor` SHALL use `DateTime.UtcNow` for `NextProcessTime` scheduling.
6. THE `FormColony` SHALL use `DateTime.UtcNow` for commodity request staleness checks and tab warning evaluations.
7. THE `ColonyViewModel` SHALL use `DateTime.UtcNow` for expired commodity request cleanup.

### Requirement 9: Migrate Existing CountDownTime Data to UTC

**User Story:** As a player with active timers, I want my existing timer data converted from local time to UTC during migration, so that my in-flight timers remain accurate after the upgrade.

#### Acceptance Criteria

1. THE migration SHALL convert all `CountDownTime.StartTime` and `CountDownTime.EndTime` values from local time to UTC by applying `DateTime.ToUniversalTime()`.
2. THE migration SHALL skip `DateTime.MinValue` values (uninitialized timers).
3. THE migration SHALL process all `ColonyStructure.ProcessCompletionTime` and `ColonyStructure.BuildCompletionTime` across all colonies.
4. THE migration SHALL process all `PlayerSkill.CompletionTime` across all player profiles.
5. THE migration SHALL be included in the same migration pass as the colony import timestamp backfill (Migration004).
