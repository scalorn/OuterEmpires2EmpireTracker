# Implementation Plan: Preferences Form

## Overview

Implement a Preferences form for configuring nine threshold/interval values currently hardcoded across TabWarningService, BackgroundProcessor, FormColony, and four countdown-timer forms. Values are persisted in UIPreferences.json via PreferencesStore and take effect immediately. The implementation proceeds model-first, then parser, then service wiring, then UI, with property tests validating each layer.

## Tasks

- [x] 1. Add ThresholdPreferences model and update UIPreferences
  - [x] 1.1 Create ThresholdPreferences class and add Thresholds property to UIPreferences
    - Add `ThresholdPreferences` class to `OE2EmpireTracker/Models/UIPreferences.cs` with nine properties and defaults matching the hardcoded constants
    - Add `public ThresholdPreferences Thresholds { get; set; } = new ThresholdPreferences();` to `UIPreferences`
    - Add `Compile Include` entry in `OE2EmpireTracker/OE2EmpireTracker.csproj` if needed (ThresholdPreferences is in the same file as UIPreferences, so no new entry needed)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10_

  - [x] 1.2 Add null-guard in PreferencesStore.Load for backward compatibility
    - After deserialization in `PreferencesStore.Load()`, add: `if (_preferences.Thresholds == null) _preferences.Thresholds = new ThresholdPreferences();`
    - _Requirements: 1.11, 9.1, 9.2_

  - [x] 1.3 Add ThresholdPreferences validation logic
    - Add a static `Validate(ThresholdPreferences prefs, out string error)` method (either on ThresholdPreferences or as a separate helper) that checks: all values positive, StructureCountYellow < StructureCountRed, WorkerRequestYellowSeconds > WorkerRequestRedSeconds, ColonyImportStalenessYellowSeconds < ColonyImportStalenessRedSeconds, CountdownRefreshRateSeconds >= 1
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x] 1.4 Write unit tests for ThresholdPreferences defaults and validation
    - Create `OE2EmpireTracker.Tests/Models/ThresholdPreferencesTests.cs`
    - Add `Compile Include` entry in test `.csproj`
    - Test all nine default values match previously hardcoded constants
    - Test validation edge cases: CountdownRefreshRateSeconds = 0 rejected, StructureCountYellow == StructureCountRed rejected, WorkerRequestYellowSeconds == WorkerRequestRedSeconds rejected
    - _Requirements: 1.1–1.9, 7.1–7.6_

  - [x] 1.5 Write property test for validation correctness
    - **Property 6: Preferences validation accepts valid configurations and rejects invalid ones**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6**

- [x] 2. Checkpoint - Verify model layer compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Implement CountdownFormatParser
  - [x] 3.1 Create CountdownFormatParser static class
    - Create `OE2EmpireTracker/Parsers/CountdownFormatParser.cs`
    - Add `Compile Include` entry in `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - Implement `public static bool TryParse(string input, out long totalSeconds)` — inverse of `ActivityRow.FormatSeconds`
    - Accept space-separated tokens with unit suffixes (d, h, m, s), partial formats, any order, no duplicate units
    - Return false for null/empty/whitespace, unrecognized suffixes, negative numbers, duplicate units, overflow
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [x] 3.2 Write unit tests for CountdownFormatParser
    - Create `OE2EmpireTracker.Tests/Parsers/CountdownFormatParserTests.cs`
    - Add `Compile Include` entry in test `.csproj`
    - Test specific examples: "5d 0h 0m 0s" → 432000, "1h 30m" → 5400, "60s" → 60, "0s" → 0
    - Test failures: "", "abc", "5d 3d" (duplicate unit), negative numbers
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 3.3 Write property test for countdown format round-trip
    - **Property 1: Countdown format round-trip**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.5**

  - [x] 3.4 Write property test for countdown format rejects invalid input
    - **Property 2: Countdown format rejects invalid input**
    - **Validates: Requirements 8.4**

- [x] 4. Checkpoint - Verify parser compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Wire TabWarningService to read from PreferencesStore
  - [x] 5.1 Replace hardcoded constants in TabWarningService with PreferencesStore reads
    - Replace `const int StructureYellowThreshold` with a static property reading from `PreferencesStore.GetInstance().Preferences.Thresholds.StructureCountYellow`
    - Replace `const int StructureRedThreshold` similarly
    - Replace `readonly TimeSpan WorkerYellowWindow` with a property computing `TimeSpan.FromSeconds(...)` from PreferencesStore
    - Replace `readonly TimeSpan WorkerRedWindow` similarly
    - Replace `const int ColonyImportStalenessYellowDays` with a property computing `TimeSpan.FromSeconds(...)` from PreferencesStore
    - Replace `const int ColonyImportStalenessRedDays` similarly
    - Update `EvaluateColonyImportStalenessWarning` to compare against TimeSpan instead of `TimeSpan.FromDays(int)`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [x] 5.2 Write property test for structure warning respects configured thresholds
    - **Property 3: Structure warning respects configured thresholds**
    - **Validates: Requirements 4.1, 4.2, 4.7**

  - [x] 5.3 Write property test for worker warning respects configured thresholds
    - **Property 4: Worker warning respects configured thresholds**
    - **Validates: Requirements 4.3, 4.4**

  - [x] 5.4 Write property test for colony import staleness warning respects configured thresholds
    - **Property 5: Colony import staleness warning respects configured thresholds**
    - **Validates: Requirements 4.5, 4.6**

  - [x] 5.5 Create TabWarningServicePreferencesTests.cs for property tests 3–5
    - Create `OE2EmpireTracker.Tests/Services/TabWarningServicePreferencesTests.cs`
    - Add `Compile Include` entry in test `.csproj`
    - Each test configures PreferencesStore with random thresholds, calls the evaluation method, and verifies the result matches expected warning level
    - _Requirements: 4.1–4.7_

- [x] 6. Wire BackgroundProcessor to read interval from PreferencesStore
  - [x] 6.1 Add GetTickIntervalMs method to BackgroundProcessor
    - Add `private int GetTickIntervalMs()` that reads from `PreferencesStore.GetInstance().Preferences.Thresholds.BackgroundProcessingIntervalSeconds * 1000`
    - Enforce minimum of 1000ms
    - Replace usages of the `TickIntervalMs` constant in `Start()` and timer rescheduling with `GetTickIntervalMs()`
    - Retain the `const` field for backward compatibility
    - _Requirements: 5.1, 5.2_

- [x] 7. Wire FormColony admin refresh to read from PreferencesStore
  - [x] 7.1 Update FormColony timer to read interval from PreferencesStore
    - On timer tick, set `timerAdminRefresh.Interval` from `PreferencesStore.GetInstance().Preferences.Thresholds.AdminRefreshIntervalSeconds * 1000`
    - Enforce minimum of 1000ms
    - _Requirements: 6.1, 6.2_

- [x] 8. Wire countdown timer forms to read refresh rate from PreferencesStore
  - [x] 8.1 Update ColonyStructure control countdown timer
    - Read `CountdownRefreshRateSeconds` from PreferencesStore when starting/rescheduling `timerCountdown`
    - Enforce minimum of 1000ms via `Math.Max(intervalMs, 1000)`
    - _Requirements: 10.1, 10.5_

  - [x] 8.2 Update FormColonyActivity countdown timer
    - Read `CountdownRefreshRateSeconds` from PreferencesStore when starting/rescheduling `timerRefresh`
    - Enforce minimum of 1000ms
    - _Requirements: 10.2, 10.5_

  - [x] 8.3 Update PlayerSkillBlock countdown timer
    - Read `CountdownRefreshRateSeconds` from PreferencesStore when starting/rescheduling `timerCountdown`
    - Enforce minimum of 1000ms
    - _Requirements: 10.3, 10.5_

  - [x] 8.4 Update MainWindow countdown timer
    - Read `CountdownRefreshRateSeconds` from PreferencesStore when starting/rescheduling `timerNextProcess`
    - Enforce minimum of 1000ms
    - _Requirements: 10.4, 10.5_

- [x] 9. Checkpoint - Verify all service wiring compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Create FormPreferences dialog
  - [x] 10.1 Create FormPreferences form files
    - Create `OE2EmpireTracker/Forms/FormPreferences/` directory
    - Create `FormPreferences.cs`, `FormPreferences.Designer.cs`, and `FormPreferences.resx`
    - Add `Compile Include` and `EmbeddedResource` entries in `OE2EmpireTracker/OE2EmpireTracker.csproj` with proper `DependentUpon` and `SubType` attributes
    - Implement modal form with six GroupBox sections (Structure Count, Worker Request Due Window, Colony Import Staleness, Background Processing, Administration Report, Countdown Display)
    - Nine labeled TextBox inputs — integer inputs for structure counts, countdown format inputs for time-based values
    - OK, Cancel, and Reset to Defaults buttons
    - _Requirements: 2.1, 2.2, 2.4, 2.5, 2.7, 2.8, 2.9_

  - [x] 10.2 Implement FormPreferences load, save, and validation logic
    - On load: populate fields from `PreferencesStore.GetInstance().Preferences.Thresholds` — use `ActivityRow.FormatSeconds()` for time-based display
    - On OK: validate all fields using `ThresholdPreferences.Validate()`, parse countdown format inputs via `CountdownFormatParser.TryParse()`, write to PreferencesStore, call `Save()`, close
    - On Cancel: close without saving
    - On Reset: restore all fields to `new ThresholdPreferences()` default values
    - Show `MessageBox` with descriptive error on validation failure, prevent closing
    - _Requirements: 2.3, 2.6, 2.10, 7.5, 7.7_

- [x] 11. Add Preferences menu item to MainWindow
  - [x] 11.1 Add "Preferences..." menu item to MainWindow File menu
    - Add `preferencesToolStripMenuItem` to the File menu's DropDownItems, inserted before `toolStripSeparatorFileExit`
    - Click handler opens `FormPreferences` as a modal dialog
    - Update `MainWindow.Designer.cs` for the menu item declaration
    - Add `Compile Include` entry if not already present (MainWindow already exists)
    - _Requirements: 3.1, 3.2_

- [x] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Old-style csproj requires explicit `Compile Include` entries for all new `.cs` files in both main and test projects
- FsCheck 2.16.6 with FsCheck.NUnit adapter is already available in the test project
- Do NOT use `semanticRename` — it doesn't work with old-style csproj
- Use `getDiagnostics` for compile checks, `vstest.console` with `/Logger:trx` for running tests
- CountdownFormatParser is the inverse of `ActivityRow.FormatSeconds` in `ColonyActivityCollector.cs`
- FormPreferences goes in its own directory under `Forms/` per the steering file
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
