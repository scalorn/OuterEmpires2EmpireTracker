# Implementation Plan

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Max Reserves Not Extracted From Asteroid Survey HTML
  - **CRITICAL**: This test MUST FAIL on unfixed code — failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior — it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists
  - **Scoped PBT Approach**: Scope the property to concrete failing cases — asteroid survey HTML with known `ScanDetailOutputMaxReserve` node values
  - Create test file `OE2EmpireTracker.Tests/Parsers/SurveyParserMaxReserveTests.cs`
  - Build asteroid survey HTML fixture containing `ScanDetailOutputMaxReserve` div nodes with known values (e.g., "15000", "8500", "22000") alongside matching `ScanDetailOutputResourceName` nodes
  - Call `SurveyParser.ProcessHtml` with the fixture HTML
  - Assert `survey.ParsedMaxReserves` is non-null and contains the expected resource-to-value mappings
  - Also test the downstream path: given a survey with `ParsedMaxReserves` populated, call `SurveyImportHelper.LinkOrCreateAsteroid` and assert `asteroid.Reserves` is non-empty with correct `MaxReserve` values
  - Bug condition from design: `isBugCondition(input)` where `maxReserveNodes.Count > 0 AND resourceNodes.Count > 0 AND survey.SurveyType == Asteroid AND linkedAsteroid.Reserves.Count == 0`
  - Expected behavior from design: `expectedBehavior(result)` where `asteroid.Reserves.Count == survey.ParsedMaxReserves.Count` and each reserve's `MaxReserve` matches the parsed value
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (this is correct — it proves the bug exists: parser discards max reserve values, asteroid gets empty Reserves)
  - Document counterexamples found (e.g., "ProcessHtml produces survey with null ParsedMaxReserves despite HTML containing ScanDetailOutputMaxReserve nodes")
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 1.1, 2.1_

- [x] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Planet Survey Import and Resource Parsing Unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - Create test file `OE2EmpireTracker.Tests/Parsers/SurveyParserPreservationTests.cs`
  - Observe on UNFIXED code: `ProcessHtml` with planet survey HTML (no `ScanDetailOutputMaxReserve` nodes) produces a Survey with correct resources, no asteroid type
  - Observe on UNFIXED code: `ProcessHtml` with asteroid survey HTML still parses resource names, purities, and amounts correctly
  - Observe on UNFIXED code: `MergeData` preserves UUID, OwnerUUID, and NickName on re-import
  - Observe on UNFIXED code: `/cycle` rate unit detection continues to set `SurveyType = Asteroid`
  - Write property-based tests capturing observed behavior:
    - Planet survey HTML → no `ParsedMaxReserves`, resources parsed identically, no asteroid entity created
    - Asteroid survey HTML → resource name/purity/amount parsing unchanged (only max reserve extraction is new)
    - Survey dedup merge → UUID, OwnerUUID, NickName preserved across all inputs
    - Asteroid type detection via `/cycle` rate units → `SurveyType = Asteroid` still set
  - Preservation scope from design: all inputs that do NOT contain `ScanDetailOutputMaxReserve` HTML nodes should be completely unaffected
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3. Fix for asteroid survey import not extracting and displaying max reserve data

  - [x] 3.1 Add transient ParsedMaxReserves property to Survey model
    - In `OE2EmpireTracker/Models/Survey.cs`, add `[JsonIgnore] public Dictionary<string, int> ParsedMaxReserves { get; set; }`
    - This carries max reserve data from parser to import helper without affecting JSON serialization
    - _Bug_Condition: isBugCondition(input) where maxReserveNodes.Count > 0 AND no mechanism to pass reserve data from parser to import helper_
    - _Expected_Behavior: Survey object can carry per-resource max reserve values transiently during import_
    - _Preservation: Existing Survey serialization/deserialization unchanged — property is [JsonIgnore]_
    - _Requirements: 2.1_

  - [x] 3.2 Extract max reserve values in SurveyParser.ProcessHtml
    - In `OE2EmpireTracker/Parsers/SurveyParser.cs`, in the `ProcessHtml` method
    - After selecting `ScanDetailOutputMaxReserve` nodes (existing code that sets SurveyType), iterate nodes in parallel with resource nodes
    - Parse each node's `InnerText` to extract the integer max reserve value (handle commas, whitespace)
    - Populate `survey.ParsedMaxReserves` dictionary keyed by resource name
    - _Bug_Condition: isBugCondition(input) where parser reads node count but never reads node InnerText_
    - _Expected_Behavior: survey.ParsedMaxReserves contains one entry per resource with correct integer value_
    - _Preservation: Existing resource name/purity/amount parsing in ParseResource unchanged; planet surveys unaffected_
    - _Requirements: 2.1, 3.2, 3.3_

  - [x] 3.3 Populate Asteroid.Reserves in SurveyImportHelper.LinkOrCreateAsteroid
    - In `OE2EmpireTracker/Services/SurveyImportHelper.cs`, in the `LinkOrCreateAsteroid` method
    - After creating or finding the asteroid, read `survey.ParsedMaxReserves`
    - For each entry, find matching `SurveyResource` in `survey.Resources` to get purity
    - Create `AsteroidReserve` with `ResourceName`, `Purity`, and `MaxReserve`; replace asteroid's `Reserves` list
    - On re-import (asteroid already exists), update `Reserves` with latest parsed data
    - _Bug_Condition: isBugCondition(input) where LinkOrCreateAsteroid creates asteroid with empty Reserves_
    - _Expected_Behavior: asteroid.Reserves.Count == survey.ParsedMaxReserves.Count AND each reserve.MaxReserve matches parsed value_
    - _Preservation: Planet surveys (no ParsedMaxReserves) skip reserve population entirely_
    - _Requirements: 2.1, 2.2, 3.1_

  - [x] 3.4 Add FindLinkedAsteroid method to SurveyViewModel
    - In `OE2EmpireTracker/ViewModels/SurveyViewModel.cs`
    - Add method `FindLinkedAsteroid()` that returns the `Asteroid` entity from `PlayerContext` using `_survey.AsteroidUUID`, or null if not an asteroid survey
    - _Expected_Behavior: Returns linked Asteroid when AsteroidUUID is set, null otherwise_
    - _Preservation: No changes to existing SurveyViewModel properties or behavior_
    - _Requirements: 2.3_

  - [x] 3.5 Add MaxReserve column and populate in FormSurvey
    - In `OE2EmpireTracker/Forms/Survey/FormSurvey.cs`
    - Add a read-only "Max Reserve" column to `dgvResources` (programmatically in constructor or Designer)
    - In `PopulateFormFromViewModel`, after populating resource rows, call `viewModel.FindLinkedAsteroid()`
    - If asteroid found, match each resource row to an `AsteroidReserve` by resource name and purity, populate MaxReserve cell
    - For planet surveys, leave the column empty
    - _Bug_Condition: PopulateFormFromViewModel never looks up linked asteroid to display max reserve data_
    - _Expected_Behavior: Asteroid survey shows max reserve values per resource row; planet survey shows empty column_
    - _Preservation: Planet survey form display unchanged — no asteroid-specific information shown_
    - _Requirements: 2.3, 3.4_

  - [x] 3.6 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Max Reserves Extracted and Stored
    - **IMPORTANT**: Re-run the SAME test from task 1 — do NOT write a new test
    - The test from task 1 encodes the expected behavior from design
    - When this test passes, it confirms: `expectedBehavior(result)` where `asteroid.Reserves.Count == survey.ParsedMaxReserves.Count` and each reserve's `MaxReserve` matches the parsed value
    - Run bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - _Requirements: 2.1, 2.2_

  - [x] 3.7 Verify preservation tests still pass
    - **Property 2: Preservation** - Planet Survey Import and Resource Parsing Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Confirm all tests still pass after fix (no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [-] 4. Checkpoint — Ensure all tests pass
  - Build the solution and run all tests
  - Ensure all tests pass, ask the user if questions arise
