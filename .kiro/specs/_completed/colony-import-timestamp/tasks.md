# Implementation Plan: Colony Import Timestamp

## Overview

This plan implements colony import timestamp tracking, UTC retrofit of SurveyDateTimeParser, DateTime.Now→DateTime.UtcNow sweep, migration, and UI surfacing across the application. Tasks are ordered so each builds on the previous: UTC foundation first, then mechanical sweep, then new feature code, then migration, then UI consumers.

## Tasks

- [x] 1. UTC retrofit of SurveyDateTimeParser and update existing tests
  - [x] 1.1 Update `SurveyDateTimeParser.IsoFormat` from `"yyyy-MM-ddTHH:mm:ss"` to `"yyyy-MM-ddTHH:mm:ssZ"`
    - Change the `IsoFormat` constant
    - Update `TryParseIso` to accept both `Z` and non-`Z` suffixes using `DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal`
    - Update `FormatForDisplay` to convert UTC DateTime to local time via `.ToLocalTime()` before formatting to game format
    - _Requirements: 7.3, 7.4, 7.5, 7.6_

  - [x] 1.2 Update `FormSurvey` DateTimePicker UTC conversion
    - In `PopulateFormFromViewModel`, convert parsed UTC DateTime to local via `.ToLocalTime()` before setting the picker value
    - In `dtpScanDateTime_ValueChanged`, convert the picker's local value to UTC via `.ToUniversalTime()` before calling `ToIsoString`
    - _Requirements: 7.7_

  - [x] 1.3 Update `Migration003_SurveyDateTimeNormalization` fallback to use `DateTime.UtcNow`
    - Change the fallback `DateTime.Now` to `DateTime.UtcNow` in the unparseable branch
    - _Requirements: 7.8_

  - [x] 1.4 Update existing SurveyDateTimeParser tests for UTC
    - Update `SurveyDateTimeParserTests` expected ISO strings to include `Z` suffix
    - Update `SurveyDateTimeParserPropertyTests` generators to produce UTC DateTimes (`DateTimeKind.Utc`) and expected ISO strings with `Z` suffix
    - Update `SurveyParserTests` expected ISO strings (e.g. `"2024-07-27T23:44:00"` → `"2024-07-27T23:44:00Z"`)
    - Update `Migration003Tests` expected ISO strings and inline `MigrateDateTime` replica to use `DateTime.UtcNow`
    - Update `Migration003PropertyTests` inline `MigrateDateTime` replica to use `DateTime.UtcNow`
    - _Requirements: 7.3, 7.4, 7.5_

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. DateTime.Now → DateTime.UtcNow sweep
  - [x] 3.1 Update `CountDownTime.cs` — replace all `DateTime.Now` with `DateTime.UtcNow`
    - `TimeRemaining` getter and setter
    - `IntervalsPassed` getter
    - `ConsumeIntervals`
    - `StartRepeating` (both overloads)
    - `GetNextIntervalBoundary`
    - _Requirements: 8.2_

  - [x] 3.2 Update `ColonyStructure.cs` — replace `DateTime.Now` with `DateTime.UtcNow` in timer start code
    - All `StartTime = DateTime.Now` → `DateTime.UtcNow`
    - Clock-hour boundary calculations
    - _Requirements: 8.3_

  - [x] 3.3 Update `PlayerSkillBlock.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - `CompletionTime.StartTime = DateTime.Now` → `DateTime.UtcNow`
    - _Requirements: 8.3_

  - [x] 3.4 Update `MinerSetupHelper.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - Clock-hour boundary calculation in `SetupTimer`
    - _Requirements: 8.3_

  - [x] 3.5 Update `RefinerySetupHelper.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - Clock-hour boundary calculation
    - _Requirements: 8.3_

  - [x] 3.6 Update `BackgroundProcessor.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - `NextProcessTime` scheduling in `Start` and `OnTimerTick`
    - _Requirements: 8.5_

  - [x] 3.7 Update `ColonyActivityCollector.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - `GetSecondsRemaining` NeedBy elapsed calculation in `ActivityRow`
    - _Requirements: 8.4_

  - [x] 3.8 Update `ColonyViewModel.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - `CleanupExpiredCommodityRequests` expired check
    - _Requirements: 8.7_

  - [x] 3.9 Update `FormColony.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - Commodity request staleness, tab warning, NeedBy countdown display, build timer creation
    - _Requirements: 8.6_

  - [x] 3.10 Update `FormDeliveryRoute.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - Plan name date stamp in `cmdNewPlan_Click`
    - _Requirements: 8.1_

  - [x] 3.11 Update `MainWindow.cs` — replace `DateTime.Now` with `DateTime.UtcNow`
    - `OnTimerNextProcessTick` next process time display
    - _Requirements: 8.1_

  - [x] 3.12 Update `FormSurvey.cs` — keep `DateTime.Now` for DateTimePicker defaults (local display), ensure ISO storage uses `DateTime.UtcNow`
    - `ClearForm` DateTimePicker default stays `DateTime.Now` (local display)
    - _Requirements: 8.1_

- [x] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Colony LastImportDateTime property and import stamping
  - [x] 5.1 Add `LastImportDateTime` string property to `Colony.cs`
    - Add `public string LastImportDateTime { get; set; }` to the Colony class
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 5.2 Stamp `LastImportDateTime` in `ColonyImportHelper.CreateFromTemp`
    - Add `colony.LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow);` after copying fields
    - _Requirements: 2.1, 2.3_

  - [x] 5.3 Stamp `LastImportDateTime` in `ColonyImportHelper.MergeIdentity`
    - Add `target.LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow);` at the end
    - _Requirements: 2.2, 2.3_

  - [x] 5.4 Write property test for Colony LastImportDateTime JSON round-trip
    - **Property 1: Colony LastImportDateTime JSON round-trip**
    - **Validates: Requirements 1.1, 1.2**

  - [x] 5.5 Write property test for import operations producing valid ISO timestamps
    - **Property 2: Import operations produce valid ISO timestamps**
    - **Validates: Requirements 2.1, 2.2, 2.3**

- [x] 6. Migration004 — colony timestamp backfill and CountDownTime UTC conversion
  - [x] 6.1 Create `Migration004_ColonyImportTimestampBackfill.cs`
    - New file in `OE2EmpireTracker/Services/Migration/Migrations/`
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - Implement `Run(EmpireContext ec, PlayerContext pc)`:
      - Backfill null/empty `LastImportDateTime` on all colonies with `SurveyDateTimeParser.ToIsoString(DateTime.UtcNow)`
      - Convert all `CountDownTime.StartTime` and `CountDownTime.EndTime` from local to UTC via `.ToUniversalTime()`, skipping `DateTime.MinValue`
      - Process `ColonyStructure.ProcessCompletionTime` and `ColonyStructure.BuildCompletionTime` across all colonies
      - Process `PlayerSkill.CompletionTime` across all player profiles
    - _Requirements: 3.1, 3.2, 3.3, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 6.2 Register Migration004 in `MigrationRunner.cs`
    - Bump `CurrentVersion` from 3 to 4
    - Add `{ 4, Migration004_ColonyImportTimestampBackfill.Run }` to the `Migrations` dictionary
    - _Requirements: 3.3_

  - [x] 6.3 Write property test for migration backfill
    - **Property 3: Migration backfills empty and preserves existing**
    - **Validates: Requirements 3.1, 3.2**

  - [x] 6.4 Write property test for CountDownTime UTC migration
    - **Property 8: Migration converts local times to UTC correctly**
    - **Validates: Requirements 9.1, 9.2**

- [x] 7. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. ColonyInactivityCollector staleness rows
  - [x] 8.1 Add `ColonyImportStaleness` to `ActivityType` enum in `ColonyActivityCollector.cs`
    - Add new enum value after `Refining`
    - _Requirements: 4.1_

  - [x] 8.2 Add `CollectColonyImportStaleness` method to `ColonyInactivityCollector.cs`
    - Parse `colony.LastImportDateTime` via `SurveyDateTimeParser.TryParseIso`
    - If parse fails, treat as maximally stale — emit row with "Unknown" details
    - Compute elapsed seconds: `(DateTime.UtcNow - parsed).TotalSeconds`
    - If elapsed > 86400 (1 day), emit `ActivityRow` with `Type = ActivityType.ColonyImportStaleness`, `SourceName = "Colony Import"`, `ProcessDetails = "{formatted elapsed} since last import"`
    - Call from `CollectInactivities` for each colony
    - _Requirements: 4.2, 4.3, 4.5, 6.1, 6.2, 6.3_

  - [x] 8.3 Write property test for inactivity collector staleness rows
    - **Property 4: Inactivity collector produces correct ColonyImportStaleness rows**
    - **Validates: Requirements 4.2, 4.3, 4.5**

- [x] 9. TabWarningService and FormColony tab warning
  - [x] 9.1 Add staleness constants and `EvaluateColonyImportStalenessWarning` to `TabWarningService.cs`
    - Add `ColonyImportStalenessYellowDays = 5` and `ColonyImportStalenessRedDays = 6` constants
    - Implement `EvaluateColonyImportStalenessWarning(string lastImportDateTime, DateTime now)` returning `TabWarningLevel`
    - Null/empty → Red, parse fail → Red, >= 6 days → Red, >= 5 days → Yellow, else → None
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.7_

  - [x] 9.2 Wire `EvaluateColonyImportStalenessWarning` into `FormColony.UpdateTabWarnings`
    - Add call to `ApplyTabWarning(tabPAdministration, TabWarningService.EvaluateColonyImportStalenessWarning(selectedColony?.LastImportDateTime, DateTime.UtcNow))`
    - _Requirements: 5.6_

  - [x] 9.3 Write property test for TabWarningService import staleness
    - **Property 5: TabWarningService returns correct warning level for import staleness**
    - **Validates: Requirements 5.2, 5.3, 5.4, 5.5**

- [x] 10. FormColonyActivity checkbox for ColonyImportStaleness
  - [x] 10.1 Add `chkColonyImportStaleness` checkbox to `FormColonyActivity.Designer.cs`
    - Add new checkbox in `flpFilters` panel, positioned after `chkRefining` and before `chkShowInactive`
    - Default checked, text "Import Staleness"
    - Add field declaration in the Designer fields section
    - _Requirements: 4.4_

  - [x] 10.2 Wire `chkColonyImportStaleness` in `FormColonyActivity.cs`
    - Wire `CheckedChanged` to `chkFilter_CheckedChanged`
    - Show/hide based on inactivity mode: visible only when `chkShowInactive.Checked` is true
    - Add `ActivityType.ColonyImportStaleness` to `GetSelectedActivityTypes()` when `chkShowInactive.Checked && chkColonyImportStaleness.Checked`
    - _Requirements: 4.4_

- [x] 11. SurveyDateTimeParser UTC property tests
  - [x] 11.1 Write property test for SurveyDateTimeParser UTC storage and local display
    - **Property 6: SurveyDateTimeParser stores UTC and displays local**
    - **Validates: Requirements 7.3, 7.4, 7.5, 7.6**

  - [x] 11.2 Write property test for CountDownTime UTC consistency
    - **Property 7: CountDownTime uses UTC consistently**
    - **Validates: Requirements 8.2**

- [x] 12. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- New .cs files need `Compile Include` entries in the old-style csproj
- MigrationRunner.CurrentVersion bumps from 3 to 4
- The SurveyDateTimeParser UTC changes are direct code fixes (no migration) since survey datetime hasn't shipped
- Migration004 handles both colony import timestamp backfill AND CountDownTime local→UTC conversion
