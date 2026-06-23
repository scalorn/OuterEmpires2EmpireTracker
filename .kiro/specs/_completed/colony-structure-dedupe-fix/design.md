# Colony Structure Dedupe Fix — Bugfix Design

## Overview

Colony import duplicates manually-added structures because the merge key (`gameSequence`) is 0 for manual structures and a real `buildingID` for parsed buildings, so no match is ever found. The fix renames `gameSequence` to `displaySequence` (per-type UI sequence number), adds a new `buildingID` field for the game's unique building identifier, and changes merge detection to use `FlatpackBlueprintUUID + displaySequence` as a compound key. Commodity manufactory types require special positional matching within each sub-type because their game sequence is not stable.

## Glossary

- **Bug_Condition (C)**: A colony has manually-added structures (displaySequence calculated, no buildingID) and the user imports colony HTML — the parser fails to match parsed buildings to existing structures, creating duplicates
- **Property (P)**: Parsed buildings merge into existing structures when `FlatpackBlueprintUUID + displaySequence` matches, updating in place rather than duplicating
- **Preservation**: All existing import behavior for empty colonies, idempotent re-imports, non-local colony workers fallback, commodity demands, mining resources, and building attributes must remain unchanged
- **displaySequence**: Per-type UI sequence number (e.g., Reactor Core #1 = 1, Reactor Core #2 = 2). Replaces the old `gameSequence` field
- **buildingID**: The game's unique building identifier from the JSON `buildingID` property. Stored in a new dedicated field on `ColonyStructure`
- **ParseColonyBuildingsFromJson**: The method in `ColonyParser.cs` that parses the colony-buildings JSON and merges structures into the colony
- **ColonyViewModel.AddStructure**: The method that creates a new `ColonyStructure` when a user manually adds one via the colony form

## Bug Details

### Bug Condition

The bug manifests when a user manually adds structures via the colony form (each gets `gameSequence=0`) and then imports colony HTML. `ParseColonyBuildingsFromJson` indexes existing structures by `gameSequence` to find merge candidates. Manually-added structures with `gameSequence=0` never match any parsed building (which carries a real `buildingID` like 47, 48), so every parsed building is appended as a new entry — duplicating the structures.

A secondary issue: `gameSequence` conflates two concepts — the game's unique building identifier (`buildingID`) and the per-type UI display sequence number. The field name and usage are inconsistent.

**Formal Specification:**
```
FUNCTION isBugCondition(colony, parsedBuildings)
  INPUT: colony with existing structures, parsedBuildings from JSON
  OUTPUT: boolean

  hasManualStructures := EXISTS s IN colony.Structures
    WHERE s.gameSequence == 0
    AND s.FlatpackBlueprintUUID IS NOT NULL

  hasParsedBuildingsMatchingManualTypes := EXISTS p IN parsedBuildings
    WHERE p.buildingID > 0
    AND EXISTS s IN colony.Structures
      WHERE s.FlatpackBlueprintUUID == p.FlatpackBlueprintUUID
      AND s.gameSequence == 0

  RETURN hasManualStructures AND hasParsedBuildingsMatchingManualTypes
END FUNCTION
```

### Examples

- User manually adds 2 Reactor Cores (both get `gameSequence=0`). Import finds Reactor Core buildingID=47 and buildingID=48. Neither matches `gameSequence=0` in the lookup. Result: 4 Reactor Cores instead of 2. **Expected**: 2 Reactor Cores, merged by `FlatpackBlueprintUUID + displaySequence`.
- User manually adds 1 Mining Rig. Import finds 3 Mining Rigs (buildingID=10,11,12). The manual one doesn't match any. Result: 4 Mining Rigs instead of 3. **Expected**: 3 Mining Rigs — manual one merged with first parsed, other 2 added.
- User manually adds 2 Agridomes (commodity factory type). Import finds 2 Agridomes. No match by `gameSequence`. Result: 4 Agridomes. **Expected**: 2 Agridomes, matched positionally within the Agridome sub-type.
- Colony has no manual structures, import runs twice — no duplication (existing behavior, not a bug condition).

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Importing into an empty colony creates one structure per parsed building with correct flatpack UUID, properties, worker assignments, and mining/manufacturing data
- Importing the same HTML twice (idempotency) produces the same structure count and values
- Non-local colony workers fallback path creates structures from colony-workers workforce detail with correct flatpack UUIDs
- Commodity demand parsing and merging by name continues unchanged
- Mining resource extraction, planet overview parsing, and building attribute population remain identical
- `MergeStructure` field-level merge logic (preserving local state, updating game state) remains unchanged

**Scope:**
All inputs that do NOT involve merging parsed buildings into a colony with pre-existing manually-added structures should be completely unaffected by this fix. This includes:
- Fresh imports into empty colonies
- Re-imports into colonies populated only by previous imports
- Non-local colony workers fallback imports
- Commodity demand parsing
- All non-parser code paths (colony processing, status calculation, delivery fulfillment)

## Hypothesized Root Cause

Based on the bug description and code analysis, the root causes are:

1. **Wrong merge key**: `ParseColonyBuildingsFromJson` indexes existing structures by `gameSequence` (line: `existingByGameSeq`). Manually-added structures have `gameSequence=0`, so they never appear in the lookup dictionary (or all collide on key 0). Parsed buildings with real `buildingID` values (47, 48, etc.) find no match and are added as new entries.

2. **Conflated field semantics**: The `gameSequence` field on `ColonyStructure` stores the game's `buildingID` (a unique per-colony identifier), but the field name suggests a per-type display sequence. The game UI shows structures as "Reactor Core #1", "Reactor Core #2" — that per-type numbering is what `displaySequence` should represent, and it's what enables matching manual structures to parsed ones.

3. **No displaySequence calculation on manual add**: `ColonyViewModel.AddStructure` creates a new `ColonyStructure` with default `gameSequence=0` and no per-type sequence calculation. There's no way for the merge logic to match a manual structure to a parsed building because the manual structure has no meaningful key.

4. **No commodity manufactory special handling**: Commodity factory types (Agridome, Administration Block, etc.) share a sequence space in the game, and their sequence numbers are not stable when new buildings are added. The current merge logic has no awareness of this, so even with a corrected merge key, commodity factories need positional matching within each sub-type.

## Correctness Properties

Property 1: Bug Condition — Manual structures merge with parsed buildings

_For any_ colony that has manually-added structures with calculated `displaySequence` values, when colony HTML is imported containing buildings with matching `FlatpackBlueprintUUID + displaySequence` pairs, the fixed `ParseColonyBuildingsFromJson` SHALL merge parsed buildings into existing structures (updating in place) rather than creating duplicates, and the final structure count SHALL equal the maximum of manual count and parsed count per blueprint type.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

Property 2: Preservation — Empty colony and idempotent import behavior unchanged

_For any_ colony HTML imported into an empty colony, or re-imported into a colony populated only by previous imports, the fixed code SHALL produce exactly the same structure count, flatpack UUIDs, displaySequence values, properties, worker assignments, and mining/manufacturing data as the original code, preserving all existing import and idempotency behavior.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker/Models/ColonyStructure.cs`

**Changes**:
1. **Rename `gameSequence` to `displaySequence`** — use semanticRename to update all references across the codebase. This field becomes the per-type UI sequence number (e.g., first Reactor Core = 1, second = 2).
2. **Add `buildingID` field** — new `public int buildingID { get; set; } = 0;` property, serialized to JSON.
3. **JSON backward compatibility** — add `[JsonProperty("gameSequence")]` attribute on `displaySequence` OR handle old field name during deserialization so existing save files with `"gameSequence"` in their JSON continue to load correctly. Alternatively, since `displaySequence` will be recalculated on next import, the old value can be silently ignored — but the field rename in JSON should be considered.

**File**: `OE2EmpireTracker/Parsers/ColonyParser.cs`

**Function**: `ParseBuilding`

**Changes**:
4. **Store `buildingID` in new field** — `structure.buildingID = buildingID;` instead of `structure.gameSequence = buildingID;`
5. **Derive `displaySequence` from position** — after parsing all buildings, calculate `displaySequence` for each building based on its position among same-type buildings in the JSON array (first Mining Rig = 1, second = 2, etc.)

**Function**: `ParseColonyBuildingsFromJson`

**Changes**:
6. **Change merge key** — replace `existingByGameSeq` dictionary (keyed by `gameSequence`) with a dictionary keyed by `FlatpackBlueprintUUID + displaySequence` compound key
7. **Two-pass parsing** — first pass: parse all buildings and calculate `displaySequence` per type. Second pass: merge into existing structures using the compound key.
8. **Commodity manufactory special case** — for buildings whose blueprint type is `BlueprintTypes.CommodityFactory`, match by position within each sub-type (first Agridome matches first existing Agridome, etc.) rather than by `displaySequence`
9. **Update `MergeStructure` call** — ensure `buildingID` is merged into existing structures

**Function**: `MergeStructure`

**Changes**:
10. **Merge `buildingID`** — add `existing.buildingID = parsed.buildingID;` to update the game's building identifier on merge
11. **Merge `displaySequence`** — add `existing.displaySequence = parsed.displaySequence;` to update the display sequence on merge

**File**: `OE2EmpireTracker/ViewModels/ColonyViewModel.cs`

**Function**: `AddStructure`

**Changes**:
12. **Calculate `displaySequence` on manual add** — after creating the new structure, count existing structures with the same `FlatpackBlueprintUUID` and set `displaySequence = count + 1`

**File**: `OE2EmpireTracker/ViewModels/ColonyStructureViewModel.cs`

**Changes**:
13. **Update `GameSequence` property** — rename to `DisplaySequence` (or keep as `GameSequence` mapping to `displaySequence` for backward compat with UI bindings)

**File**: `OE2EmpireTracker.Tests/Parsers/ColonyParserTests.cs`

**Changes**:
14. **Update `gameSequence` references** — all test assertions referencing `gameSequence` need updating to `displaySequence`

**File**: `OE2EmpireTracker.Tests/Parsers/ColonyParserIdempotencyTests.cs`

**Changes**:
15. **Update `gameSequence` references** — all snapshot/assertion code referencing `gameSequence` needs updating to `displaySequence`

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Write tests that create a colony with manually-added structures (gameSequence=0), then import colony HTML and assert that the structure count does not increase beyond the parsed count. Run these tests on the UNFIXED code to observe failures and understand the root cause.

**Test Cases**:
1. **Manual + Import Merge Test**: Add 2 Reactor Cores manually, import HTML with 2 Reactor Cores. Assert count = 2 (will fail on unfixed code — count will be 4)
2. **Partial Overlap Test**: Add 1 Mining Rig manually, import HTML with 3 Mining Rigs. Assert count = 3 (will fail on unfixed code — count will be 4)
3. **Commodity Factory Merge Test**: Add 2 Agridomes manually, import HTML with 2 Agridomes. Assert count = 2 (will fail on unfixed code — count will be 4)
4. **No Manual Structures Test**: Import into empty colony. Assert count matches parsed count (should pass on unfixed code — this is the non-bug path)

**Expected Counterexamples**:
- Structure count doubles when manual structures exist and HTML is imported
- Possible causes: merge key mismatch (gameSequence=0 vs real buildingID), no displaySequence calculation on manual add

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FOR ALL (colony, htmlInput) WHERE isBugCondition(colony, parsedBuildings(htmlInput)) DO
  result := ParseColonyBuildingsFromJson_fixed(colony, htmlInput)
  ASSERT structureCount(result) == max(manualCount, parsedCount) per blueprint type
  ASSERT each manual structure is merged (not duplicated)
  ASSERT buildingID is populated from parsed data
  ASSERT displaySequence matches per-type position
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL (colony, htmlInput) WHERE NOT isBugCondition(colony, parsedBuildings(htmlInput)) DO
  ASSERT ParseColonyBuildingsFromJson_fixed(colony, htmlInput)
         == ParseColonyBuildingsFromJson_original(colony, htmlInput)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for all non-buggy inputs

**Test Plan**: Run existing idempotency tests and colony parser tests against the fixed code to verify all existing behavior is preserved. Add new tests for the merge-with-manual-structures scenario.

**Test Cases**:
1. **Empty Colony Import Preservation**: Import M1 HTML into empty colony, verify same structure count (43), same flatpack UUIDs, same displaySequence values, same properties as before the fix
2. **Idempotent Re-import Preservation**: Import M1 HTML twice, verify structure count unchanged after second import (existing `M1_ParseTwice_StructureCountUnchanged` test)
3. **Workers Fallback Preservation**: Import VI-1 HTML (non-local colony), verify structures created from workers fallback with correct flatpack UUIDs
4. **Commodity Demand Preservation**: Import M2-2 HTML, verify commodity demands parsed and merged correctly
5. **Structure Values Preservation**: Import M1 HTML twice, verify displaySequence and FlatpackBlueprintUUID values unchanged (existing `M1_ParseTwice_StructureValuesPreserved` test, updated for displaySequence)

### Unit Tests

- Test `ParseBuilding` stores `buildingID` in new field and derives `displaySequence` from position
- Test `ParseColonyBuildingsFromJson` merges by `FlatpackBlueprintUUID + displaySequence` compound key
- Test `ColonyViewModel.AddStructure` calculates `displaySequence` correctly (count of same-type + 1)
- Test manual-add then import scenario: structures merge, no duplicates
- Test commodity factory positional matching within sub-types
- Test edge cases: import with more structures than manual, import with fewer, mixed types

### Property-Based Tests

- Generate random colony states with varying numbers of manual structures per blueprint type, import HTML, verify no duplicates and correct merge behavior
- Generate random re-import sequences and verify idempotency is preserved
- Test that displaySequence is always sequential per blueprint type after any combination of manual adds and imports

### Integration Tests

- Full flow: manually add structures via `ColonyViewModel.AddStructure`, then import real test HTML (M1), verify merge correctness
- Full flow: import HTML, then import again, verify idempotency
- Full flow: import HTML into colony with mixed manual and previously-imported structures
