# Implementation Plan: Survey DateTime Normalization

## Overview

Normalize survey scan date/time values from the game's non-standard format (`"27JUL24-11:44p"`) into ISO 8601 (`"2024-07-27T23:44:00"`) for internal storage, while displaying the game format in the UI. Implementation touches: a new parsing utility, the import pipeline, the view model, the list view comparer, the survey form (DateTimePicker), and a data migration.

## Tasks

- [x] 1. Create SurveyDateTimeParser utility class
  - [x] 1.1 Create `OE2EmpireTracker/Services/SurveyDateTimeParser.cs` with static methods: `TryParseGameFormat`, `TryParseIso`, `ToIsoString`, `ToGameFormat`, `FormatForDisplay`, `TryParseAny`
    - Regex pattern `^(\d{2})([A-Z]{3})(\d{2})-(\d{1,2}):(\d{2})([ap])$` for game format
    - Month lookup dictionary JAN→1 through DEC→12, reverse lookup for formatting
    - 12-hour to 24-hour conversion: `12a`→`0`, `12p`→`12`, other `p`→`+12`
    - Two-digit year: add `2000`
    - `FormatForDisplay`: ISO → game format, passthrough on failure
    - `TryParseAny`: try game format, then ISO, then `DateTime.TryParse`
    - Add `Compile Include` entry in `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 3.1, 3.3_

  - [x] 1.2 Write property tests for SurveyDateTimeParser (Properties 1–6)
    - Create `OE2EmpireTracker.Tests/Services/SurveyDateTimeParserPropertyTests.cs`
    - Add `Compile Include` entry in `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - **Property 1: Game format round-trip** — parse game format → ISO → parse ISO → compare DateTime values
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 5.1, 7.1**
    - **Property 2: ISO round-trip through display format** — parse ISO → game format → parse game format → compare DateTime values
    - **Validates: Requirements 3.1, 7.2**
    - **Property 3: DateTime format path consistency** — ToGameFormat → TryParseGameFormat → ToIsoString == direct ToIsoString
    - **Validates: Requirements 6.3, 7.3**
    - **Property 4: Invalid game format returns false without throwing** — non-matching strings return false, no exception
    - **Validates: Requirements 1.8**
    - **Property 5: Unparseable ISO passthrough in display** — FormatForDisplay returns original string unchanged
    - **Validates: Requirements 3.3**
    - **Property 6: ISO strings sort chronologically via string comparison** — lexicographic order of ToIsoString matches DateTime order
    - **Validates: Requirements 4.1**
    - Use FsCheck `[Property(MaxTest = 200)]` with custom Arbitrary generators for valid game-format strings, valid ISO strings, valid DateTimes (minute precision), and invalid game-format strings

  - [x] 1.3 Write unit tests for SurveyDateTimeParser edge cases
    - Create `OE2EmpireTracker.Tests/Services/SurveyDateTimeParserTests.cs`
    - Add `Compile Include` entry in `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - Test `12a` → midnight (hour 0), `12p` → noon (hour 12)
    - Test all 12 months JAN–DEC parse to correct month number
    - Test known game strings: `"27JUL24-11:44p"` → `"2024-07-27T23:44:00"`, `"19FEB26-08:41p"` → `"2026-02-19T20:41:00"`
    - Test `FormatForDisplay` passthrough for non-ISO strings
    - Test `TryParseGameFormat` returns false for null, empty, and malformed strings
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.7, 1.8, 3.3_

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Update SurveyParser to store ISO format on import
  - [x] 3.1 Modify `SurveyParser.ParseDescription` in `OE2EmpireTracker/Parsers/SurveyParser.cs`
    - After extracting the raw date string via regex, call `SurveyDateTimeParser.TryParseGameFormat`
    - If successful, store `SurveyDateTimeParser.ToIsoString(parsed)` in `survey.DateTime`
    - If parsing fails, preserve the raw string unchanged (existing behavior)
    - _Requirements: 1.1, 2.1_

  - [x] 3.2 Update existing SurveyParser tests to expect ISO output
    - In `OE2EmpireTracker.Tests/Parsers/SurveyParserTests.cs`, update expected `DateTime` values:
      - `"27JUL24-11:44p"` → `"2024-07-27T23:44:00"`
      - `"19FEB26-08:41p"` → `"2026-02-19T20:41:00"`
      - `"19FEB26-09:39p"` → `"2026-02-19T21:39:00"`
    - Update all tests that assert on `survey.DateTime` (ParseDescription tests, ProcessHtml tests, ZehVazoran tests, Quogar tests)
    - _Requirements: 2.1_

- [x] 4. Update SurveyViewModel with DisplayDateTime property
  - [x] 4.1 Add `DisplayDateTime` property to `OE2EmpireTracker/ViewModels/SurveyViewModel.cs`
    - `public string DisplayDateTime => SurveyDateTimeParser.FormatForDisplay(_survey.DateTime);`
    - The existing `DateTime` property continues to read/write the raw ISO string on the model
    - _Requirements: 3.1, 3.3_

- [x] 5. Update ListViewItemComparer for date-aware sorting
  - [x] 5.1 Modify `Compare` in `OE2EmpireTracker/Controls/ListViewItemComparer.cs`
    - Check if both SubItems have non-null `Tag` values; if so, compare Tags as strings instead of display text
    - This enables ISO-string-based chronological sorting without hardcoding column indices
    - Fall back to existing display-text comparison if Tags are null
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 5.2 Write unit tests for ListViewItemComparer date sorting
    - Add tests to `OE2EmpireTracker.Tests/Controls/ListViewItemComparerTests.cs` (create if needed, add Compile Include)
    - Create ListViewItems with ISO strings in SubItem Tags, verify sort order is chronological
    - Test fallback to display-text comparison when Tags are null
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 6. Update FormSurvey for DateTimePicker and display formatting
  - [x] 6.1 Add DateTimePicker to FormSurvey Designer
    - In `OE2EmpireTracker/Forms/Survey/FormSurvey.Designer.cs`, add `dtpScanDateTime` DateTimePicker control to `flpScanDateTime` panel
    - Set `txtScanDateTime.ReadOnly = true` to prevent manual editing
    - _Requirements: 6.1, 6.5_

  - [x] 6.2 Wire DateTimePicker and update form logic in `OE2EmpireTracker/Forms/Survey/FormSurvey.cs`
    - Wire `dtpScanDateTime.ValueChanged`: convert picker value to ISO via `ToIsoString`, store on viewModel, update text box with game format via `ToGameFormat`
    - Update `PopulateFormFromViewModel`: set text box to `viewModel.DisplayDateTime`, parse ISO to set DateTimePicker value
    - Update `ClearForm`: set DateTimePicker to `DateTime.Now`, text box to current time in game format
    - Remove `txtScanDateTime.TextChanged` write-through handler (editing now goes through the picker)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x] 6.3 Update `PopulateListView` to store ISO string in SubItem Tag
    - In `PopulateListView`, set `item.SubItems[4].Tag = survey.DateTime` (the ISO string)
    - Set `item.SubItems[4].Text` to `SurveyDateTimeParser.FormatForDisplay(survey.DateTime)` for display
    - _Requirements: 3.2, 4.2_

- [x] 7. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Create Migration003 for existing survey data
  - [x] 8.1 Create `OE2EmpireTracker/Services/Migration/Migrations/Migration003_SurveyDateTimeNormalization.cs`
    - Static class with `Run(EmpireContext ec, PlayerContext pc)` method
    - Iterate all surveys in `pc.SurveyList`
    - Skip if already ISO (`TryParseIso` succeeds)
    - Try `TryParseGameFormat` → `ToIsoString`
    - Try `DateTime.TryParse` fallback → `ToIsoString`
    - Replace unparseable/null/empty with `DateTime.Now` in ISO format, log warning with planet name and original value
    - Add `Compile Include` entry in `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

  - [x] 8.2 Register Migration003 in MigrationRunner
    - In `OE2EmpireTracker/Services/Migration/MigrationRunner.cs`, bump `CurrentVersion` from `2` to `3`
    - Add `{ 3, Migration003_SurveyDateTimeNormalization.Run }` to the `Migrations` dictionary
    - _Requirements: 5.7_

  - [x] 8.3 Write property tests for migration logic (Properties 7–9)
    - Create `OE2EmpireTracker.Tests/Services/Migration/Migration003PropertyTests.cs`
    - Add `Compile Include` entry in `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - **Property 7: Migration is idempotent on ISO values** — valid ISO strings are left unchanged
    - **Validates: Requirements 2.2, 5.6**
    - **Property 8: Migration produces valid ISO for unparseable input** — null, empty, and garbage strings produce valid ISO output
    - **Validates: Requirements 5.4, 5.5**
    - **Property 9: Migration common-format fallback produces valid ISO** — standard .NET formatted dates are parsed and converted to ISO
    - **Validates: Requirements 5.2, 5.3**
    - Use FsCheck `[Property(MaxTest = 200)]` following existing patterns

  - [x] 8.4 Write unit tests for Migration003
    - Create `OE2EmpireTracker.Tests/Services/Migration/Migration003Tests.cs`
    - Add `Compile Include` entry in `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - Test migration on already-ISO data (no change)
    - Test migration on game-format data (converts to ISO)
    - Test migration on garbage string (replaces with valid ISO)
    - Test migration on null/empty DateTime (replaces with valid ISO)
    - _Requirements: 5.1, 5.2, 5.4, 5.5, 5.6_

- [x] 9. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New `.cs` files require `Compile Include` entries in the old-style `.csproj` files
- The `Survey.DateTime` property type remains `string` — only the stored content changes
- `MigrationRunner.CurrentVersion` bumps from 2 to 3
