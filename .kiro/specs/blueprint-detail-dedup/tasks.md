# Implementation Plan

## Overview

Fix the duplicate Name+Evolution API ID stomping bug in `CreateBlueprintDetailItem` using the exploratory bugfix workflow: write tests to confirm the bug, write preservation tests to protect existing behavior, then implement the three-tier resolution strategy.

## Task Dependency Graph

```json
{
  "waves": [
    { "tasks": ["1", "2"] },
    { "tasks": ["3.1"] },
    { "tasks": ["3.2", "3.3", "4"] },
    { "tasks": ["5"] }
  ]
}
```

## Tasks

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Duplicate Name+Evolution API ID Stomping
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate multiple API IDs collapse onto a single blueprint
  - **Scoped PBT Approach**: Create 3 blueprints with identical Name+Evolution, process 3 distinct API IDs sequentially through the assignment logic
  - Test that after processing API IDs 101, 102, 103 for blueprints all named "Reactor" Evo 3, each API ID maps to a DISTINCT local blueprint (from Bug Condition in design)
  - Assert: `FindBlueprintByApiId(101)`, `FindBlueprintByApiId(102)`, `FindBlueprintByApiId(103)` all return non-null AND all return different blueprint objects
  - Assert: no two blueprints share the same `GameApiBlueprintId` value
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (FirstOrDefault assigns all API IDs to blueprint[0], confirming the bug exists)
  - Document counterexamples: all 3 API IDs assigned to same blueprint, index only retains last ID
  - File: `OE2EmpireTracker.Tests/Services/BlueprintDetailDedupPropertyTests.cs`
  - Inputs: `QueueSyncService.cs`, `PlayerContext.cs`, `BlueprintService.cs`
  - Verification: test written, run via vstest.console, failure documented
  - _Requirements: 1.1, 1.2, 1.5_

- [-] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Single-Match and No-Match Behavior
  - **IMPORTANT**: Follow observation-first methodology
  - Observe: single blueprint "Laser Evo2" with API ID 200 assigned directly on unfixed code
  - Observe: no matching blueprint for Name+Evo "Unknown Evo5" produces no assignment on unfixed code
  - Observe: blueprint with recent `LastDetailImportUtc` is skipped on unfixed code
  - Observe: import producing `ImportAction.Skipped` produces no API ID assignment on unfixed code
  - Write property-based tests: for all inputs where exactly ONE local blueprint matches Name+Evolution (or zero match), the assignment result is identical to original code behavior (from Preservation Requirements in design)
  - Generate random single-match scenarios (unique Name+Evo combinations) and verify direct assignment
  - Generate random no-match scenarios and verify no assignment occurs
  - Verify tests pass on UNFIXED code
  - File: `OE2EmpireTracker.Tests/Services/BlueprintDetailDedupPropertyTests.cs` (append)
  - Inputs: `QueueSyncService.cs`, `PlayerContext.cs`, `bugfix.md` preservation requirements
  - Verification: tests written, run via vstest.console, all PASS on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3. Fix for duplicate Name+Evolution API ID assignment

  - [~] 3.1 Implement the three-tier resolution strategy
    - Extract the post-import API ID assignment block from `CreateBlueprintDetailItem` into a new private method `ResolveBlueprintForApiId(int blueprintId, string importedName, int importedEvo)`
    - Tier 1: Call `_playerContext.FindBlueprintByApiId(blueprintId)` — if non-null, return it directly (already assigned)
    - Tier 2: Filter `BlueprintList.Where(Name == importedName && Evo == importedEvo)` excluding blueprints where `GameApiBlueprintId.HasValue && GameApiBlueprintId.Value != blueprintId`
    - Tier 3: If multiple unassigned candidates remain, build a temporary Blueprint from API response properties and call `BlueprintService.FindBestMatch` to score and select the best candidate
    - Handle no-match: if no candidate remains after filtering, log a warning and return null (skip assignment)
    - Replace the inline `FirstOrDefault` in `CreateBlueprintDetailItem` with a call to `ResolveBlueprintForApiId`
    - Add NLog diagnostic logging at each tier decision point
    - _Bug_Condition: isBugCondition(input) where localMatches.Count() > 1 AND FirstOrDefault always returns same blueprint_
    - _Expected_Behavior: each API ID maps to a distinct local blueprint; index maintains 1:1 mappings_
    - _Preservation: single-match scenarios continue direct assignment without scoring_
    - File: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`
    - Inputs: `QueueSyncService.cs`, `BlueprintService.cs`, `PlayerContext.cs`
    - Verification: builds with zero errors/warnings via MSBuild
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1_

  - [~] 3.2 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Distinct API ID Assignment
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior (each API ID maps to distinct blueprint)
    - When this test passes, it confirms the three-tier resolution correctly distributes API IDs
    - Run bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - Verification: vstest.console runs `BlueprintDetailDedupPropertyTests`, Property 1 passes
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [~] 3.3 Verify preservation tests still pass
    - **Property 2: Preservation** - Single-Match and No-Match Behavior
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions for single-match, no-match, freshness, skipped)
    - Confirm all preservation tests still pass after fix (no regressions)
    - Verification: vstest.console runs `BlueprintDetailDedupPropertyTests`, Property 2 tests pass

- [~] 4. Write unit tests for three-tier resolution edge cases
  - Test already-assigned blueprint returned directly (Tier 1 pre-check via FindBlueprintByApiId)
  - Test candidates with different GameApiBlueprintId are excluded (Tier 2 filtering)
  - Test among multiple unassigned candidates, highest-scoring one is selected (Tier 3 scoring)
  - Test all candidates already assigned to other API IDs produces no assignment with logged warning
  - Test single unassigned candidate produces direct assignment without scoring (optimization)
  - File: `OE2EmpireTracker.Tests/Services/BlueprintDetailDedupUnitTests.cs`
  - Inputs: `QueueSyncService.cs`, `BlueprintService.cs`, `PlayerContext.cs`
  - Verification: all unit tests pass via vstest.console
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [~] 5. Checkpoint - Ensure all tests pass
  - Run full test suite: vstest.console against OE2EmpireTracker.Tests.dll
  - Run `node .kiro/tools/audit.js` — zero findings
  - Build solution with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise

## Notes

- The bug is deterministic: `FirstOrDefault` on `BlueprintList` always returns the same element for a given Name+Evolution
- `BlueprintService.FindBestMatch` already exists and provides property-based scoring — reuse it for Tier 3
- `PlayerContext.FindBlueprintByApiId` already exists — reuse it for Tier 1 pre-check
- FsCheck 2.16.6 is the installed version — use `[FsCheck.NUnit.Property]` attribute syntax, not 3.x APIs
- The fix is contained entirely within `QueueSyncService.cs` — no model changes, no new public APIs
