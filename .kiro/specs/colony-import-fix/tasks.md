# Implementation Plan

## Overview

This task list implements the colony-import-fix bugfix using the exploratory bugfix workflow: write tests to confirm the bug (exploration), write tests to preserve existing behavior (preservation), implement the three-phase fix, then validate everything passes.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1", "2"] },
    { "id": 1, "tasks": ["3.1", "3.2", "3.3"] },
    { "id": 2, "tasks": ["3.4", "3.5"] },
    { "id": 3, "tasks": ["4"] },
    { "id": 4, "tasks": ["5"] }
  ]
}
```

## Tasks

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Pool Consumption and State Assignment
  - **CRITICAL**: This test MUST FAIL on unfixed code — failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior — it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists
  - **Scoped PBT Approach**: Scope the property to concrete failing cases:
    - Case A: Colony with 2+ pool entries of same ColonyBuildingTypeId (both BuildingID=0), API returns 2+ buildings of that type → assert each pool entry consumed at most once (unique BuildingIDs assigned)
    - Case B: Colony with 3+ pool entries, API returns structures where warehouse has flatpacks matching remaining entries → assert remaining entries have Staged=true
    - Case C: Colony with pool entries having stale state (Built=true from prior mismatch), API returns fewer buildings → assert unmatched entries reset to planned (Built=false, Staged=false, BuildCompletionTime=null)
  - Test file: `OE2EmpireTracker.Tests/Services/ColonyMergeBuildingsBugConditionTests.cs`
  - Generate test inputs using FsCheck 2.16.6 LINQ query syntax generators
  - Create helper to build Colony with N structures of specified ColonyBuildingTypeId
  - Create helper to build List<GameApiColonyBuilding> with matching types and completion dates
  - Assertions encode expected behavior from design: unique consumption, staged from warehouse, planned normalization
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (this is correct — it proves the bug exists)
  - Document counterexamples found (e.g., "Two pool entries ended up with same BuildingID" or "Remaining entry has Staged=false despite warehouse flatpack match")
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5_


- [x] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Field-Level Merge and Structure Creation Unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - **Observation Phase**: Run UNFIXED code with non-buggy inputs (single-match scenarios where isBugCondition returns false):
    - Observe: Colony with 1 pool entry (BuildingID=5), API returns 1 building (BuildingId=5) → field merge produces specific property values
    - Observe: Colony with 1 pool entry (BuildingID=0, ColonyBuildingTypeId=4), API returns 1 building of type 4 → fallback match works, BuildingID assigned
    - Observe: Colony with 0 pool entries, API returns 1 building → new structure created with BuildQueueSequence at end
    - Observe: Colony with 1 pool entry, API returns 1 building with ResourceId, ResourceIcon, DurabilityCurrent, DurabilityMax → all fields merged
  - **Property-Based Tests**: Write properties capturing observed behavior:
    - For all single-match inputs (1 pool entry matching 1 API building by BuildingID): field-level merge produces identical results to original code
    - For all new-structure inputs (API building with no pool match): new structure created with correct BuildQueueSequence
    - For all BuildingID-keyed inputs: primary key match still works before type fallback
  - Test file: `OE2EmpireTracker.Tests/Services/ColonyMergeBuildingsPreservationTests.cs`
  - Generate single-match colony configurations using FsCheck 2.16.6 LINQ query syntax
  - Compare field values on matched structure after MergeBuildings call
  - Verify tests PASS on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_


- [x] 3. Implement the three-phase fix for MergeBuildings

  - [x] 3.1 Add consumed-set parameter to FindLocalStructure and implement Phase 1 loop
    - Modify `FindLocalStructure` signature to accept `HashSet<string> consumed` parameter
    - Exclude consumed UUIDs from both primary (BuildingID) and fallback (ColonyBuildingTypeId) matching
    - Replace the existing `foreach` loop in `MergeBuildings` with Phase 1 logic:
      - Sort API buildings by ConstructingBuildingFinish ascending (already done, keep)
      - Walk sorted list, calling `FindLocalStructure` with consumed set
      - On match: add matched structure's UUID to consumed set, call `MergeExistingStructure`
      - On no match: call `CreateStructureFromApi` (existing behavior preserved)
    - Do NOT change `MergeExistingStructure` or `CreateStructureFromApi` internals
    - Files modified: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs` (MergeBuildings + FindLocalStructure)
    - _Bug_Condition: isBugCondition(input) where multiple unassigned pool entries of same type exist_
    - _Expected_Behavior: each pool entry consumed at most once, BuildingID as primary key, then ColonyBuildingTypeId fallback_
    - _Preservation: MergeExistingStructure and CreateStructureFromApi unchanged_
    - _Requirements: 2.2, 3.5, 3.6, 3.7_
    - Verification: `getDiagnostics` on ColonyMergeService.cs compiles clean

  - [x] 3.2 Implement Phase 2 — Warehouse-based staged assignment
    - After Phase 1 loop, collect remaining unmatched pool entries (UUID not in consumed set)
    - Query `colony.Items.Items.Values` for flatpack items (ItemType == Flatpack, Quantity > 0)
    - Group remaining entries by FlatpackBlueprintUUID
    - For each group, find matching warehouse flatpack where BaseItemTypeID == FlatpackBlueprintUUID
    - Order entries within each group by BuildQueueSequence ascending
    - Mark first N entries as Staged=true (Properties["Staged"]=true, Properties["Built"]=false, BuildCompletionTime=null), where N = warehouse flatpack Quantity
    - Do NOT create new pool entries for unmatched warehouse flatpacks (Req 2.6)
    - For remaining entries with no warehouse match: set Properties["Built"]=false, Properties["Staged"]=false, BuildCompletionTime=null (planned normalization)
    - Files modified: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs` (MergeBuildings — add Phase 2 block)
    - _Bug_Condition: warehouse flatpacks matching remaining pool entries_
    - _Expected_Behavior: staged count <= warehouse quantity per type, planned entries explicitly reset_
    - _Preservation: No new pool entries for unmatched warehouse flatpacks_
    - _Requirements: 2.3, 2.4, 2.5, 2.6, 2.8_
    - Verification: `getDiagnostics` on ColonyMergeService.cs compiles clean


  - [x] 3.3 Implement Phase 3 — BuildQueueSequence reassignment
    - After Phase 2, assign BuildQueueSequence to all structures in colony.Structures:
      - Partition structures into three groups: built/building (matched by API, in consumed set or newly created), staged (Properties["Staged"]=true), planned (remaining)
      - Built/building: assign sequences 1, 2, 3... in API completion-date order (ConstructingBuildingFinish ascending)
      - Staged: assign next sequences preserving their prior relative BuildQueueSequence order
      - Planned: assign next sequences preserving their prior relative BuildQueueSequence order
    - Add defensive invariant check: at most 1 structure with Built=false AND BuildCompletionTime != null; log error if violated
    - Files modified: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs` (MergeBuildings — add Phase 3 block)
    - _Bug_Condition: BuildQueueSequence assigned without considering built/staged/planned ordering_
    - _Expected_Behavior: deterministic sequence: built first (API order), then staged (user order), then planned (user order)_
    - _Preservation: newly created structures get sequence at end of built section_
    - _Requirements: 2.1, 2.7_
    - Verification: `getDiagnostics` on ColonyMergeService.cs compiles clean; build full solution with zero warnings

  - [x] 3.4 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Pool Consumption and State Assignment
    - **IMPORTANT**: Re-run the SAME test from task 1 — do NOT write a new test
    - The test from task 1 encodes the expected behavior
    - When this test passes, it confirms the expected behavior is satisfied
    - Run `ColonyMergeBuildingsBugConditionTests` via vstest.console
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - If test still fails, investigate and fix the implementation (tasks 3.1-3.3)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 3.5 Verify preservation tests still pass
    - **Property 2: Preservation** - Field-Level Merge and Structure Creation Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run `ColonyMergeBuildingsPreservationTests` via vstest.console
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - If any preservation test fails, the fix broke existing behavior — investigate and fix
    - Confirm all field-level merge, new structure creation, and BuildingID correlation still works
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_


- [-] 4. Write fix-checking property tests for full algorithm validation
  - Test file: `OE2EmpireTracker.Tests/Services/ColonyMergeBuildingsFixCheckTests.cs`
  - Generate random colony configurations (1-10 structures, varying types, 0-5 API buildings, 0-3 warehouse flatpacks)
  - Use FsCheck 2.16.6 LINQ query syntax for all generators
  - Property assertions (run on FIXED code):
    - At most 1 structure has Built=false AND BuildCompletionTime != null (at-most-one building invariant)
    - All matched structures have unique BuildingIDs (no consumption collision)
    - Staged count per type <= warehouse flatpack quantity for that type
    - All unmatched entries without warehouse match have Built=false, Staged=false, BuildCompletionTime=null
    - BuildQueueSequence is a contiguous 1..N assignment with built/building first, then staged, then planned
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.7_
  - Verification: Run via vstest.console, all properties pass

- [~] 5. Checkpoint — Ensure all tests pass
  - Build full solution: `MSBuild OE2EmpireTracker.sln /p:Configuration=Debug` — zero errors, zero warnings
  - Run all WinForms tests: `vstest.console OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx`
  - Parse results: `node .kiro/tools/trxparse.js` — zero failures
  - Run audit: `node .kiro/tools/audit.js` — zero findings
  - Confirm bug condition tests pass (task 1 tests now green)
  - Confirm preservation tests pass (task 2 tests still green)
  - Confirm fix-checking property tests pass (task 4 tests green)
  - Confirm no pre-existing tests broken by the changes
  - Ask user if questions arise

## Notes

- FsCheck 2.16.6 only — use LINQ query syntax (`from x in Gen.Choose(...)`) for generators, `[FsCheck.NUnit.Property]` attribute for property tests
- Do NOT use FsCheck.Fluent, Shrink.Default, or any FsCheck 3.x APIs
- Tests run via vstest.console (`"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"`)
- SystemClock.UtcNowFunc must be frozen in test SetUp for deterministic BuildCompletionTime checks
- The implementation modifies only `ColonyMergeService.cs` — specifically `MergeBuildings` and `FindLocalStructure`
- `MergeExistingStructure` and `CreateStructureFromApi` remain completely unchanged (preservation guarantee)
