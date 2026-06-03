# Colony Import Fix — Bugfix Design

## Overview

The `ColonyMergeService.MergeBuildings` method incorrectly reconciles Game API building data with the local colony structure pool. The current implementation processes each API building independently without tracking which pool entries have already been consumed, resulting in: (1) multiple structures simultaneously marked as "building," (2) spurious duplicate entries when multiple pool entries share the same `ColonyBuildingTypeId`, and (3) no staged-state assignment from warehouse flatpack inventory for unmatched pool entries.

The fix restructures `MergeBuildings` into a three-phase algorithm: ordered matching with pool consumption tracking, warehouse-based staged assignment, and deterministic `BuildQueueSequence` reassignment.

## Glossary

- **Bug_Condition (C)**: The set of inputs where the current `MergeBuildings` produces incorrect state — specifically any colony whose API buildings list plus local structure pool triggers duplicate "building" marks, incorrect pool consumption, or missing staged assignments
- **Property (P)**: The desired post-merge state — at most 1 building structure, correct one-to-one pool consumption, staged flags from warehouse, and deterministic sequence numbering
- **Preservation**: Existing field-level merge behavior (property data, FlatpackBlueprintUUID resolution, BuildCompletionTime creation, MiningSurveyResource, BuildingID correlation) that must remain unchanged
- **MergeBuildings**: The static method in `ColonyMergeService.cs` that reconciles API building data with local `Colony.Structures`
- **Pool**: The colony's `List<ColonyStructure>` — the local structure entries to be matched against API data
- **Warehouse**: `Colony.Items` — the colony's `ItemBag` containing flatpack items (ItemType=Flatpack) with `BaseItemTypeID` equal to the blueprint UUID
- **BuildQueueSequence**: Integer field on `ColonyStructure` controlling display and processing order
- **FlatpackBlueprintUUID**: The blueprint UUID on a `ColonyStructure` identifying its building type for matching purposes
- **ColonyBuildingTypeId**: Integer type identifier from the Game API used as secondary match key

## Bug Details

### Bug Condition

The bug manifests when the Game API returns multiple buildings for a colony and the local structure pool contains entries of matching types. The current `FindLocalStructure` method is called independently per API building without tracking which pool entries have already been consumed, and no post-match processing assigns staged or planned states.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type { apiBuildings: List<GameApiColonyBuilding>, colony: Colony }
  OUTPUT: boolean

  LET pool = input.colony.Structures
  LET api = input.apiBuildings sorted by ConstructingBuildingFinish ascending

  // Condition 1: Multiple future-completion dates → multiple "building" marks
  LET futureCount = COUNT(api WHERE ConstructingBuildingFinish > now)
  IF futureCount > 1 THEN RETURN true

  // Condition 2: Multiple unassigned pool entries of same type → consumption ambiguity
  LET unassignedByType = GROUP(pool WHERE BuildingID == 0, BY ColonyBuildingTypeId)
  IF ANY group IN unassignedByType HAS count > 1
     AND api HAS entry matching that type
  THEN RETURN true

  // Condition 3: Warehouse has flatpacks matching remaining pool entries → missing staged
  LET matchedPoolUUIDs = RunCurrentMatchingAlgorithm(api, pool)
  LET remaining = pool WHERE UUID NOT IN matchedPoolUUIDs
  LET warehouseFlatpacks = input.colony.Items.Items.Values
      WHERE ItemType == Flatpack AND Quantity > 0
  IF ANY remaining entry HAS FlatpackBlueprintUUID IN warehouseFlatpacks.BaseItemTypeID
  THEN RETURN true

  // Condition 4: Remaining entries not explicitly set to planned
  IF ANY remaining entry HAS Properties["Built"] != false
     OR Properties["Staged"] != false
     OR BuildCompletionTime != null
  THEN RETURN true

  RETURN false
END FUNCTION
```

### Examples

- **Multiple "building" marks**: Colony has 3 structures in pool. API returns 3 buildings with completion dates [past, past, future]. Current code marks all 3 with Built status from API — the future one correctly gets Built=false, but does NOT prevent a second pool entry from also ending up as "building" if two API entries have future dates (game normally prevents this, but the code doesn't enforce the invariant).
- **Consumption collision**: Colony has 2 Mining Rigs in pool (both `ColonyBuildingTypeId=4`, both `BuildingID=0`). API returns 2 built Mining Rigs. Current `FindLocalStructure` picks the same "first by DisplaySequence" entry for both API buildings because the first call doesn't mark the match as consumed.
- **Missing staged**: Colony has 5 structures in pool, API returns 3 as built. Warehouse has 1 flatpack matching one of the 2 remaining pool entries. Current code never examines the warehouse, so that entry stays without `Staged=true`.
- **Corrupted planned state**: After an incorrect merge, a remaining pool entry may have `Built=true` from a prior mismatch. Current code never resets unmatched entries.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Field-level merge of API properties onto matched structures (ResourceId, ResourceIcon, DurabilityCurrent, DurabilityMax, ManufactureAmountPerRun, OpsStatusEffects, Industries, DetailsRequired, BuildingAttributes, ExtraProperties)
- FlatpackBlueprintUUID resolution from BlueprintDesignName via flatpack lookup when local is empty
- BuildCompletionTime creation from ConstructingBuildingFinish when local is null
- MiningSurveyResource assignment from ResourceName when local is empty
- BuildingID correlation (setting local BuildingID from API BuildingId)
- New structure creation when no pool entry matches an API building's type
- BuildingID as primary match key before type-based fallback

**Scope:**
All operations that do NOT involve pool consumption order, staged assignment, planned state normalization, or BuildQueueSequence reassignment should be completely unaffected by this fix. The `MergeExistingStructure` and `CreateStructureFromApi` helper methods remain unchanged.


## Hypothesized Root Cause

Based on the bug description and code analysis, the root causes are:

1. **No pool consumption tracking**: `FindLocalStructure` is called independently for each API building in a `foreach` loop. Once it returns a match, there is no mechanism to remove or mark that pool entry as "consumed." The next API building of the same `ColonyBuildingTypeId` will match the same pool entry again (or create a spurious new entry).

2. **No post-match phase**: After the API building loop completes, the method returns immediately. There is no second pass to:
   - Examine remaining unmatched pool entries
   - Check the colony warehouse for flatpacks
   - Assign `Staged=true` to entries with matching flatpacks
   - Explicitly reset unmatched entries to planned state

3. **Incorrect BuildQueueSequence assignment**: The current code assigns `BuildQueueSequence` only to newly created structures (at end of list) and never reassigns existing structures' sequences based on the authoritative API order plus staged/planned ordering.

4. **No "at most 1 building" enforcement**: The `MergeExistingStructure` method sets `Built=false` for any API building with a future `ConstructingBuildingFinish`. While the game enforces one-at-a-time, the current code would mark multiple structures as "building" if the API ever returned multiple future dates (or if consumption errors cause mismatches).

## Correctness Properties

Property 1: Bug Condition - At Most One Building Structure

_For any_ colony merge input where the API returns structures, the fixed `MergeBuildings` function SHALL result in at most 1 structure in the pool having `Properties["Built"]=false` AND `BuildCompletionTime != null` (the "building" state). All other structures must be either built (`Properties["Built"]=true`), staged (`Properties["Staged"]=true, Built=false, BuildCompletionTime=null`), or planned (`Properties["Built"]=false, Staged=false, BuildCompletionTime=null`).

**Validates: Requirements 2.1**

Property 2: Bug Condition - One-to-One Pool Consumption

_For any_ colony merge input where API buildings are matched to pool entries, the fixed function SHALL consume each pool entry at most once. No two API buildings shall match the same pool entry, and the matching order SHALL be ascending by `ConstructingBuildingFinish` with `BuildingID` as primary key and `FlatpackBlueprintUUID` as secondary key.

**Validates: Requirements 2.2, 3.7**

Property 3: Bug Condition - Staged Assignment From Warehouse

_For any_ colony merge input where remaining unmatched pool entries exist and the colony warehouse contains flatpacks with `BaseItemTypeID` matching those entries' `FlatpackBlueprintUUID`, the fixed function SHALL mark the first N such entries (by `BuildQueueSequence` order) as `Staged=true`, where N equals the warehouse flatpack quantity for that type.

**Validates: Requirements 2.3, 2.4, 2.6**

Property 4: Bug Condition - Planned State Normalization

_For any_ remaining pool entry that has no API match and no warehouse flatpack match, the fixed function SHALL set `Properties["Built"]=false`, `Properties["Staged"]=false`, and `BuildCompletionTime=null`.

**Validates: Requirements 2.5**

Property 5: Bug Condition - BuildQueueSequence Reassignment

_For any_ colony merge input, the fixed function SHALL assign `BuildQueueSequence` values as: built/building entries in API completion-date order (1, 2, 3...), followed by staged entries preserving their prior relative order, followed by planned entries preserving their prior relative order.

**Validates: Requirements 2.7**

Property 6: Preservation - Field-Level Merge Unchanged

_For any_ API building matched to a pool entry, the fixed function SHALL produce the same field-level merge results as the original function for all property fields (ResourceId, ResourceIcon, DurabilityCurrent, DurabilityMax, ManufactureAmountPerRun, OpsStatusEffects, Industries, DetailsRequired, BuildingAttributes, ExtraProperties, MiningSurveyResource, FlatpackBlueprintUUID, BuildCompletionTime, BuildingID).

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

Property 7: Preservation - New Structure Creation

_For any_ API building with no matching pool entry (neither by BuildingID nor by FlatpackBlueprintUUID/ColonyBuildingTypeId), the fixed function SHALL create a new structure entry with `BuildQueueSequence` at the end of the sequence, preserving existing creation behavior.

**Validates: Requirements 3.6**


## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`

**Function**: `MergeBuildings`

**Specific Changes**:

1. **Restructure into three phases**: Replace the single foreach loop with:
   - **Phase 1 (Match)**: Sort API buildings by `ConstructingBuildingFinish` ascending. Walk them in order, consuming pool entries one-by-one. Track consumed entries in a `HashSet<string>` (by UUID). Match priority: BuildingID first, then FlatpackBlueprintUUID (via ColonyBuildingTypeId for unassigned entries). Call `MergeExistingStructure` on matches; call `CreateStructureFromApi` for unmatched API buildings.
   - **Phase 2 (Stage)**: After Phase 1, collect remaining unmatched pool entries (not in consumed set). Query `colony.Items` for flatpack items (`ItemType == Flatpack`, `Quantity > 0`). Group remaining entries by `FlatpackBlueprintUUID`. For each group, find matching warehouse flatpack by `BaseItemTypeID == FlatpackBlueprintUUID`. Mark the first N entries (by current `BuildQueueSequence` order) as `Staged=true`, where N = warehouse flatpack quantity. Do NOT create new pool entries for unmatched warehouse flatpacks.
   - **Phase 3 (Sequence)**: Assign `BuildQueueSequence` to all structures: built/building entries in API completion-date order (starting at 1), then staged entries in their prior relative `BuildQueueSequence` order, then planned entries in their prior relative order.

2. **Update FindLocalStructure to accept consumed set**: Add a `HashSet<string> consumed` parameter. Exclude consumed UUIDs from matching candidates. This prevents the same pool entry from being matched twice.

3. **Add staged/planned normalization**: After matching, explicitly set remaining entries:
   - If warehouse match: `Properties["Staged"] = true`, `Properties["Built"] = false`, `BuildCompletionTime = null`
   - If no warehouse match: `Properties["Staged"] = false`, `Properties["Built"] = false`, `BuildCompletionTime = null`

4. **Invariant enforcement**: After Phase 1, verify at most 1 structure has `Built=false` AND `BuildCompletionTime != null`. Log an error if violated (game should prevent this, but defensive check).

5. **Preserve MergeExistingStructure unchanged**: The field-level merge logic in `MergeExistingStructure` and `CreateStructureFromApi` remains exactly as-is. Only the orchestration in `MergeBuildings` and the match-finding in `FindLocalStructure` change.


## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Write tests that create colonies with multiple pool entries of the same type, API building lists with various completion dates, and warehouse flatpacks — then assert the expected post-merge state. Run these tests on the UNFIXED code to observe failures and understand the root cause.

**Test Cases**:
1. **Multiple Building Marks Test**: Create colony with 3 pool entries. API returns 3 buildings, 2 with future dates. Assert only 1 has building state. (will fail on unfixed code)
2. **Consumption Collision Test**: Create colony with 2 pool entries of same ColonyBuildingTypeId. API returns 2 buildings of that type. Assert each matches a different pool entry. (will fail on unfixed code)
3. **Missing Staged Test**: Create colony with 5 pool entries, API returns 3 as built. Warehouse has 1 matching flatpack. Assert remaining entry is staged. (will fail on unfixed code)
4. **Planned Normalization Test**: Create colony with pool entries that have corrupted state from prior merge. Run merge with fewer API buildings. Assert unmatched entries reset to planned. (will fail on unfixed code)

**Expected Counterexamples**:
- Two pool entries end up with the same BuildingID (consumption collision)
- Multiple entries have Built=false + BuildCompletionTime != null (multiple "building")
- Possible causes: `FindLocalStructure` returning same entry twice, no post-match staged/planned pass

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := MergeBuildings_fixed(input.apiBuildings, input.colony)
  ASSERT atMostOneBuildingState(input.colony)
  ASSERT uniquePoolConsumption(input.colony)
  ASSERT stagedMatchesWarehouse(input.colony)
  ASSERT unmatched entries are planned
  ASSERT buildQueueSequenceIsCorrect(input.colony)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  colony1 := DeepCopy(input.colony)
  colony2 := DeepCopy(input.colony)
  MergeBuildings_original(input.apiBuildings, colony1)
  MergeBuildings_fixed(input.apiBuildings, colony2)
  ASSERT fieldLevelMergeEqual(colony1.Structures, colony2.Structures)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many colony configurations with varying pool sizes, types, and warehouse contents
- It catches edge cases (empty pools, empty API lists, single-entry colonies) that manual tests miss
- It provides strong guarantees that field-level merge is unchanged for all non-buggy inputs

**Test Plan**: Observe behavior on UNFIXED code first for simple single-match scenarios, then write property-based tests capturing that behavior.

**Test Cases**:
1. **Field Merge Preservation**: Generate colonies with 1 pool entry, API with 1 matching building. Verify all property fields are merged identically to original.
2. **New Structure Preservation**: Generate API buildings with no pool match. Verify new structure creation matches original behavior.
3. **BuildingID Primary Key Preservation**: Generate colonies with BuildingID-assigned structures. Verify these still match by BuildingID first.
4. **Empty Input Preservation**: Verify null/empty API or colony returns false without side effects.

### Unit Tests

- Test three-phase algorithm with specific scenarios (2 of same type, 5 pool + 3 API + warehouse)
- Test consumption order (ascending by ConstructingBuildingFinish)
- Test staged assignment respects warehouse quantity limits
- Test planned normalization clears stale state
- Test BuildQueueSequence reassignment order (built → staged → planned)
- Test invariant: at most 1 building state after merge
- Test edge cases: empty API list, empty pool, null warehouse (Colony.Items empty)

### Property-Based Tests

- Generate random colony configurations (1-20 structures, varying types) with random API building lists and warehouse contents. Assert post-conditions: unique consumption, at-most-one building, correct staged count, deterministic sequence.
- Generate single-match scenarios (1 pool entry, 1 API building, matching type). Assert field-level merge matches original behavior across many random property values.
- Generate edge cases: warehouse with more flatpacks than pool entries of that type, pool entries with pre-existing stale state, API buildings with null ConstructingBuildingFinish.

### Integration Tests

- End-to-end test: call MergeBuildings with realistic multi-structure colony data from test fixtures, verify final state matches expected.
- Test that MergeWarehouse + MergeBuildings sequence works correctly (warehouse populated before building merge).
- Test idempotency: calling MergeBuildings twice with same API data produces same result.
