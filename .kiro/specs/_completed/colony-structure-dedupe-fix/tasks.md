# Implementation Plan

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** — Manual structures duplicated on import
  - **CRITICAL**: This test MUST FAIL on unfixed code — failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior — it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists
  - **Scoped PBT Approach**: Scope the property to concrete failing cases — manually-added structures with gameSequence=0 that share a FlatpackBlueprintUUID with parsed buildings
  - Create a new test file `OE2EmpireTracker.Tests/Parsers/ColonyParserDedupeTests.cs` and add `<Compile Include>` to the test `.csproj`
  - Test setup: use the same `EmpireContext`/`TestHelper` pattern as `ColonyParserTests.cs`
  - Test case 1 — **Full overlap**: Add N structures manually (via `new ColonyStructure { UUID, FlatpackBlueprintUUID }` with `gameSequence=0`) for each blueprint type present in M1 HTML, matching the exact counts from a fresh M1 parse (43 structures). Import M1 HTML. Assert `colony.Structures.Count == 43` (not 86). On unfixed code this will FAIL because manual structures (gameSequence=0) never match parsed buildings (buildingID > 0), so all 43 are appended as duplicates
  - Test case 2 — **Partial overlap**: Add 1 Mining Rig manually, import M1 HTML. Assert total Mining Rig count equals the count from a fresh M1 parse (not +1). On unfixed code this will FAIL
  - Test case 3 — **No manual structures (control)**: Import M1 HTML into empty colony. Assert count == 43. This should PASS on unfixed code (confirms the non-bug path works)
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Test cases 1 and 2 FAIL (proves the bug exists), test case 3 PASSES
  - Document counterexamples found (e.g., "colony.Structures.Count == 86 instead of 43")
  - Mark task complete when tests are written, run, and failures are documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** — Empty colony and idempotent import behavior unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - These tests verify that existing non-buggy behavior is captured BEFORE the fix is applied
  - Create tests in the same `ColonyParserDedupeTests.cs` file
  - Observe on UNFIXED code: importing M1 HTML into an empty colony produces 43 structures with specific FlatpackBlueprintUUIDs, gameSequence values > 0, properties, mining resources, and worker assignments
  - Observe on UNFIXED code: importing M1 HTML twice produces the same 43 structures (idempotency — existing tests already cover this, but we add a focused preservation assertion)
  - Observe on UNFIXED code: importing VI-1 HTML (non-local, workers fallback) produces structures with correct flatpack UUIDs
  - Observe on UNFIXED code: importing M2-2 HTML produces commodity demands with correct names, amounts, and fulfilled status
  - Write property-based tests: for all colony HTML test files, importing into an empty colony and then re-importing produces identical structure counts, FlatpackBlueprintUUIDs, and property values
  - Write property-based test: for all colony HTML test files with commodity demands, re-importing preserves commodity count, names, requested amounts, and fulfilled status
  - Verify all preservation tests PASS on UNFIXED code
  - **EXPECTED OUTCOME**: All tests PASS (confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3. Fix colony structure deduplication

  - [x] 3.1 Rename `gameSequence` to `displaySequence` on `ColonyStructure`
    - Use `semanticRename` to rename `gameSequence` → `displaySequence` in `OE2EmpireTracker/Models/ColonyStructure.cs` — this updates all references across ColonyParser.cs, ColonyParserTests.cs, ColonyParserIdempotencyTests.cs, ColonyParserDedupeTests.cs, ColonyStructureViewModel.cs, and any other files
    - Add `[JsonProperty("gameSequence")]` attribute on the renamed `displaySequence` property for backward compatibility with existing save files
    - Verify with `getDiagnostics` that all files compile cleanly after rename
    - _Requirements: 2.3_

  - [x] 3.2 Add `buildingID` field to `ColonyStructure`
    - Add `public int buildingID { get; set; } = 0;` to `ColonyStructure.cs`
    - This is the game's unique building identifier, distinct from `displaySequence` (per-type UI sequence)
    - _Requirements: 2.3_

  - [x] 3.3 Update `ParseBuilding` to store `buildingID` and derive `displaySequence`
    - In `ColonyParser.ParseBuilding`: store `building["buildingID"]` into `structure.buildingID` (instead of `structure.displaySequence`)
    - Do NOT set `displaySequence` in `ParseBuilding` — it will be calculated in a second pass in `ParseColonyBuildingsFromJson`
    - _Requirements: 2.3_

  - [x] 3.4 Update `ParseColonyBuildingsFromJson` merge logic
    - **Two-pass approach**: First pass — parse all buildings via `ParseBuilding`, group by `FlatpackBlueprintUUID`, assign `displaySequence` per type (first Mining Rig = 1, second = 2, etc.)
    - **Commodity factory special case**: For buildings whose blueprint resolves to `BlueprintTypes.CommodityFactory` type, use positional matching within each sub-type (first Agridome matches first existing Agridome by list position) rather than `displaySequence`
    - **Change merge key**: Replace `existingByGameSeq` dictionary with a dictionary keyed by compound key `FlatpackBlueprintUUID + ":" + displaySequence`
    - For commodity factory types, build a separate lookup keyed by `FlatpackBlueprintUUID` with a list of existing structures sorted by list position, and match parsed structures 1:1 by position
    - When no match is found, add the structure as new (handles case where import has more structures than manual)
    - _Bug_Condition: isBugCondition(colony, parsedBuildings) where colony has manual structures with displaySequence > 0 and parsed buildings have matching FlatpackBlueprintUUID + displaySequence_
    - _Expected_Behavior: structureCount == max(manualCount, parsedCount) per blueprint type, no duplicates_
    - _Preservation: Empty colony imports, idempotent re-imports, workers fallback, commodity demands all unchanged_
    - _Requirements: 2.1, 2.2, 2.3, 2.5_

  - [x] 3.5 Update `MergeStructure` to include `buildingID` and `displaySequence`
    - Add `existing.buildingID = parsed.buildingID;` to `MergeStructure`
    - Add `existing.displaySequence = parsed.displaySequence;` to `MergeStructure`
    - _Requirements: 2.3_

  - [x] 3.6 Update `ColonyViewModel.AddStructure` to calculate `displaySequence`
    - After creating the new `ColonyStructure`, count existing structures in `_colony.Structures` with the same `FlatpackBlueprintUUID` and set `displaySequence = count + 1`
    - This ensures manually-added structures get a meaningful `displaySequence` that matches the game UI convention (per-type sequential numbering starting at 1)
    - _Requirements: 2.4_

  - [x] 3.7 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** — Manual structures merge with parsed buildings
    - **IMPORTANT**: Re-run the SAME tests from task 1 — do NOT write new tests
    - The tests from task 1 encode the expected behavior (structure count == parsed count, not doubled)
    - When these tests pass, it confirms the bug is fixed
    - Run bug condition exploration tests from step 1
    - **EXPECTED OUTCOME**: All test cases PASS (confirms bug is fixed — manual structures merge correctly)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 3.8 Verify preservation tests still pass
    - **Property 2: Preservation** — Empty colony and idempotent import behavior unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Also run all existing `ColonyParserTests` and `ColonyParserIdempotencyTests` to confirm no regressions
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Checkpoint — Ensure all tests pass
  - Build the solution: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
  - Run all tests: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
  - Verify all existing tests pass (ColonyParserTests, ColonyParserIdempotencyTests, and all other test suites)
  - Verify all new dedupe tests pass (bug condition + preservation)
  - If any test fails, investigate and fix before proceeding
  - Ensure all tests pass, ask the user if questions arise
