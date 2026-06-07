# Blueprint Detail Dedup Bugfix Design

## Overview

When multiple local blueprints share the same Name + Evolution, `CreateBlueprintDetailItem` in `QueueSyncService.cs` assigns `GameApiBlueprintId` using a naive `FirstOrDefault` on Name + Evolution. This causes all API IDs for same-named blueprints to stomp onto a single local blueprint, corrupting the `_blueprintByApiIdIndex` and triggering perpetual re-imports. The fix introduces a three-tier resolution strategy: (1) use an already-assigned blueprint via `FindBlueprintByApiId`, (2) exclude candidates with a different `GameApiBlueprintId`, and (3) score remaining unassigned candidates by property similarity.

## Glossary

- **Bug_Condition (C)**: Multiple local blueprints share the same Name + Evolution and the detail import assigns `GameApiBlueprintId` to the first match regardless of existing assignments
- **Property (P)**: Each API ID maps to a distinct local blueprint; the `_blueprintByApiIdIndex` maintains correct 1:1 mappings
- **Preservation**: Single-match scenarios, freshness checks, skip behavior, and no-match behavior continue unchanged
- **CreateBlueprintDetailItem**: The method in `QueueSyncService.cs` that creates a `WorkItem` for fetching and importing blueprint detail from the game API
- **_blueprintByApiIdIndex**: Dictionary<int, Blueprint> in `PlayerContext` mapping API IDs to local blueprints
- **FindBlueprintByApiId**: Lookup method on `PlayerContext` that resolves an API ID to its assigned blueprint
- **FindBestMatch**: Existing method on `BlueprintService` that scores candidates by property similarity for dedup
- **CrateImporter.ImportFromJson**: Existing import pipeline that merges API response data into local blueprints

## Bug Details

### Bug Condition

The bug manifests when multiple local blueprints share the same Name + Evolution (e.g., six copies of "Reactor Evo3") and the detail import processes each API blueprint ID sequentially. The post-import assignment code uses `BlueprintList.FirstOrDefault(b => b.Name == name && b.Evolution == evo)` which always returns the same blueprint regardless of how many API IDs are being processed.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type { blueprintId: int, importedName: string, importedEvo: int }
  OUTPUT: boolean

  localMatches := PlayerContext.BlueprintList
    .Where(b => b.Name == input.importedName AND b.Evolution == input.importedEvo)

  RETURN localMatches.Count() > 1
         AND localMatches.First().GameApiBlueprintId != input.blueprintId
         AND (localMatches.First().GameApiBlueprintId IS NOT NULL
              OR EXISTS other API ID already assigned to localMatches.First())
END FUNCTION
```

### Examples

- API IDs 101, 102, 103 all for "Reactor Evo3": after processing, blueprint[0] has GameApiBlueprintId=103 (last write wins), index only contains {103 → blueprint[0]}, IDs 101 and 102 are lost
- Next sync cycle: `FindBlueprintByApiId(101)` returns null → triggers redundant re-import of ID 101
- Blueprint[0] gets properties merged from all three API responses; blueprints[1] and [2] remain stale with no properties
- A blueprint already assigned API ID 50 gets overwritten with API ID 101 because it matched Name+Evolution first


## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Single-match scenarios (only one blueprint with a given Name + Evolution) continue to assign directly without scoring
- `CrateImporter.ImportFromJson` skipped-action entries continue to bypass API ID assignment
- Freshness check (`LastDetailImportUtc` recent) continues to skip re-import
- Name + Evolution matching no local blueprint continues to leave API ID unassigned (no crash)
- Mouse/UI interactions with blueprint data remain unaffected

**Scope:**
All inputs where exactly one local blueprint matches the imported Name + Evolution, or where no blueprint matches, should be completely unaffected by this fix. This includes:
- Unique blueprints (single copy of each Name + Evolution)
- Blueprints with no API match (Name + Evolution not found locally)
- Already-fresh blueprints that pass the freshness check
- Import entries that produce a Skipped action from CrateImporter

## Hypothesized Root Cause

Based on the bug description, the most likely issues are:

1. **No Pre-check for Existing Assignment**: The code never calls `FindBlueprintByApiId(blueprintId)` after import to check if the API ID is already assigned to a specific local blueprint. It always falls through to the Name + Evolution search.

2. **FirstOrDefault Always Returns the Same Blueprint**: When multiple blueprints share Name + Evolution, `FirstOrDefault` is deterministic — it always returns the same one (the first in list order). There is no exclusion of blueprints that already have a different `GameApiBlueprintId`.

3. **No Candidate Filtering by Existing Assignment**: The LINQ query `b.Name == name && b.Evolution == evo` does not exclude blueprints where `b.GameApiBlueprintId.HasValue && b.GameApiBlueprintId != blueprintId`. This means a blueprint already "claimed" by another API ID is still eligible.

4. **No Scoring for Ambiguous Matches**: Unlike `CrateImporter` which uses `BlueprintService.FindBestMatch` with property-based scoring, `CreateBlueprintDetailItem` uses a blind `FirstOrDefault` with no disambiguation.

5. **Index Stomping**: When the same blueprint object gets a new `GameApiBlueprintId` value, `IndexBlueprintByApiId` inserts the new mapping but the old mapping (pointing to the same blueprint) becomes orphaned in the index — or was already overwritten if the blueprint's field was mutated first.


## Correctness Properties

Property 1: Bug Condition - Distinct API ID Assignment

_For any_ set of API blueprint IDs being imported where multiple local blueprints share the same Name + Evolution, the fixed assignment logic SHALL assign each API ID to a distinct local blueprint, ensuring no two API IDs map to the same local blueprint object.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

Property 2: Preservation - Single-Match and No-Match Behavior

_For any_ input where exactly one local blueprint matches the imported Name + Evolution (or zero match), the fixed code SHALL produce the same assignment result as the original code, preserving direct single-match assignment and no-match skip behavior.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`

**Function**: `CreateBlueprintDetailItem` (the lambda inside the `WorkItem.ExecuteAsync`)

**Specific Changes**:

1. **Add pre-check for existing assignment**: Before the Name + Evolution search, call `_playerContext.FindBlueprintByApiId(blueprintId)`. If it returns a non-null blueprint, use that directly — skip the search entirely. This handles re-imports and already-assigned IDs.

2. **Filter candidates by assignment status**: When searching by Name + Evolution, exclude blueprints where `GameApiBlueprintId.HasValue && GameApiBlueprintId.Value != blueprintId`. Only unassigned blueprints (or the one already assigned this exact ID) are eligible.

3. **Use property-based scoring for ambiguous matches**: When multiple unassigned candidates remain after filtering, use `BlueprintService.FindBestMatch` (or equivalent scoring) to pick the best candidate based on property similarity from the API response, rather than taking the first match.

4. **Handle no-match gracefully**: If after filtering no candidate remains, log a warning and skip assignment (do not crash, do not assign to an already-claimed blueprint).

5. **Ensure index consistency**: The existing `IndexBlueprintByApiId` call remains unchanged — but because we no longer overwrite the same blueprint repeatedly, the index naturally maintains 1:1 mappings.


## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Write unit tests that set up multiple local blueprints with the same Name + Evolution, then simulate the API ID assignment logic. Run these tests on the UNFIXED code to observe that all API IDs collapse onto the first blueprint.

**Test Cases**:
1. **Duplicate Assignment Test**: Create 3 blueprints with Name="Reactor" Evo=3, process API IDs 101, 102, 103 sequentially — observe all assigned to blueprint[0] (will fail on unfixed code with correct behavior assertion)
2. **Index Corruption Test**: After assigning 3 API IDs to the same blueprint, verify `FindBlueprintByApiId` returns null for IDs 101 and 102 (demonstrates the bug on unfixed code)
3. **Already-Assigned Overwrite Test**: Create a blueprint with GameApiBlueprintId=50, then process a different API ID with matching Name+Evo — observe it overwrites the existing assignment (will fail on unfixed code)
4. **Perpetual Re-import Test**: After the stomping occurs, simulate the freshness check — observe that previously-assigned IDs trigger re-import because lookup returns null (demonstrates the cycle)

**Expected Counterexamples**:
- All API IDs assigned to the same blueprint object (FirstOrDefault determinism)
- Index only retains the last-assigned API ID for the stomped blueprint
- Possible causes: no candidate filtering, no pre-check via FindBlueprintByApiId, no scoring

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := assignApiId_fixed(input.blueprintId, input.importedName, input.importedEvo)
  ASSERT result.assignedBlueprint IS UNIQUE per blueprintId
  ASSERT result.assignedBlueprint.GameApiBlueprintId == input.blueprintId
  ASSERT FindBlueprintByApiId(input.blueprintId) == result.assignedBlueprint
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT assignApiId_original(input) == assignApiId_fixed(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain (varying blueprint counts, names, evolutions, existing assignments)
- It catches edge cases that manual unit tests might miss (e.g., all blueprints already assigned, single blueprint with same ID)
- It provides strong guarantees that single-match behavior is unchanged for all non-duplicate scenarios

**Test Plan**: Observe behavior on UNFIXED code first for single-match and no-match scenarios, then write property-based tests capturing that behavior.

**Test Cases**:
1. **Single Match Preservation**: Generate random blueprints where each Name+Evo is unique — verify assignment works identically to original code
2. **No Match Preservation**: Generate API responses where Name+Evo doesn't match any local blueprint — verify no assignment occurs (same as original)
3. **Freshness Skip Preservation**: Generate blueprints with recent LastDetailImportUtc — verify they continue to be skipped
4. **Skipped Import Preservation**: Generate imports that produce Skipped action from CrateImporter — verify no API ID assignment occurs

### Unit Tests

- Test the three-tier resolution: pre-check via FindBlueprintByApiId → filter by assignment status → score by properties
- Test that already-assigned blueprints are returned directly without Name+Evo search
- Test that candidates with different GameApiBlueprintId are excluded from the search
- Test that among multiple unassigned candidates, the highest-scoring one is selected
- Test edge case: all candidates already assigned to other API IDs → no assignment, logged warning

### Property-Based Tests

- Generate random sets of N blueprints with same Name+Evo and N distinct API IDs; verify each API ID maps to a unique blueprint after all assignments
- Generate mixed scenarios (some blueprints pre-assigned, some not) and verify index consistency: for every blueprint with GameApiBlueprintId set, FindBlueprintByApiId returns that exact blueprint
- Generate single-match scenarios and verify the fix produces identical behavior to the original code (preservation property)

### Integration Tests

- Test full sync flow: queue discovery → detail fetch → assignment → freshness check passes on next cycle (no re-import)
- Test that after fixing duplicate assignments, `_blueprintByApiIdIndex.Count` equals the number of distinct API IDs processed
- Test that blueprint properties from each API response land on their correct distinct local blueprint (no data merging)

