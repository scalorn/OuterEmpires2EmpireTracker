# Asteroid Survey Import Fix — Bugfix Design

## Overview

When importing an asteroid survey, the `SurveyParser.ProcessHtml` method detects `ScanDetailOutputMaxReserve` HTML nodes and uses them to flag the survey as asteroid type, but never extracts the actual max reserve values from those nodes. Downstream, `SurveyImportHelper.LinkOrCreateAsteroid` creates an `Asteroid` entity with an empty `Reserves` list because no reserve data was parsed. Additionally, `FormSurvey.PopulateFormFromViewModel` never looks up the linked asteroid to display max reserve data, and `SurveyViewModel` has no asteroid-related properties.

The fix involves three changes: (1) extract max reserve values in the parser, (2) populate `Asteroid.Reserves` during import, and (3) surface max reserve data on the survey form.

## Glossary

- **Bug_Condition (C)**: An asteroid survey is imported from HTML containing `ScanDetailOutputMaxReserve` nodes — the parser detects asteroid type but discards the max reserve values
- **Property (P)**: Max reserve values are extracted, stored on the linked `Asteroid.Reserves`, and displayed on the survey form
- **Preservation**: Planet survey import, existing resource parsing, survey dedup merge identity preservation, and all non-asteroid survey form behavior must remain unchanged
- **SurveyParser.ProcessHtml**: Method in `Parsers/SurveyParser.cs` that parses HTML clipboard data into a `Survey` object
- **SurveyImportHelper.LinkOrCreateAsteroid**: Method in `Services/SurveyImportHelper.cs` that auto-creates or links an `Asteroid` entity for asteroid surveys
- **PopulateFormFromViewModel**: Method in `Forms/Survey/FormSurvey.cs` that populates form fields from the current `SurveyViewModel`
- **AsteroidReserve**: Model class with `ResourceName`, `Purity`, `MaxReserve`, `CurrentReserve`, `ResetTimestamp`

## Bug Details

### Bug Condition

The bug manifests when an asteroid survey is imported from clipboard HTML that contains `ScanDetailOutputMaxReserve` div nodes. The `SurveyParser.ProcessHtml` method counts these nodes and sets `SurveyType = Asteroid`, but never reads their `InnerText` to extract the numeric max reserve values. The downstream `LinkOrCreateAsteroid` method then creates an `Asteroid` with an empty `Reserves` list.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type { htmlFragment: string, survey: Survey }
  OUTPUT: boolean

  LET maxReserveNodes = htmlFragment.SelectNodes("//div[contains(@class,'ScanDetailOutputMaxReserve')]")
  LET resourceNodes = htmlFragment.SelectNodes("//div[contains(@class,'ScanDetailOutputResourceName')]")

  RETURN maxReserveNodes.Count > 0
         AND resourceNodes.Count > 0
         AND survey.SurveyType == Asteroid
         AND linkedAsteroid.Reserves.Count == 0
END FUNCTION
```

### Examples

- **Import asteroid survey with 3 resources**: HTML contains 3 `ScanDetailOutputMaxReserve` nodes with values "15000", "8500", "22000". Current behavior: `Asteroid.Reserves` is empty. Expected: 3 `AsteroidReserve` entries with `MaxReserve` = 15000, 8500, 22000 respectively.
- **Re-import asteroid survey with updated reserves**: HTML now shows "16000" for a resource that was "15000". Current behavior: existing `Asteroid.Reserves` unchanged (still empty). Expected: reserves updated to reflect new values.
- **View asteroid survey in FormSurvey**: User selects an asteroid survey with a linked asteroid that has reserves. Current behavior: no max reserve column visible. Expected: max reserve values shown alongside each resource row.
- **Import planet survey (no max reserve nodes)**: HTML has no `ScanDetailOutputMaxReserve` nodes. Current and expected: no asteroid entity created, no reserve extraction attempted.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Planet survey import must continue to work without creating asteroid entities or extracting reserves
- Resource name, purity, and amount parsing in `ParseResource` must remain unchanged
- Asteroid type detection via `/cycle` rate units must continue to work
- Survey dedup merge must continue to preserve UUID, OwnerUUID, and NickName
- FormSurvey display for planet surveys must show no asteroid-specific information
- Existing `SurveyParser.ParseDescription` and `ParseTitle` behavior must be unchanged

**Scope:**
All inputs that do NOT contain `ScanDetailOutputMaxReserve` HTML nodes should be completely unaffected by this fix. This includes:
- Planet surveys (no max reserve nodes in HTML)
- Manual survey creation via the form
- Survey editing (save button)
- Survey deletion
- Survey list filtering

## Hypothesized Root Cause

Based on the code analysis, the issues are:

1. **Parser does not extract max reserve values**: In `SurveyParser.ProcessHtml`, lines 80-86 select `ScanDetailOutputMaxReserve` nodes and check their count to set `SurveyType = Asteroid`, but the loop never reads `maxReserveNodes[i].InnerText` to extract the numeric values. The parsed `Survey` object has no field to carry per-resource max reserve data to the import helper.

2. **No mechanism to pass reserve data from parser to import helper**: The `Survey` model has no property for carrying parsed max reserve values. The parser produces a `Survey` with resources (name, purity, amount) but no max reserve data. `SurveyImportHelper.LinkOrCreateAsteroid` has no reserve data to populate.

3. **LinkOrCreateAsteroid does not populate Reserves**: Even if reserve data were available, the method creates a bare `Asteroid` with only `UUID`, `Name`, and `SystemName` — it never sets `Reserves`. On re-import, it finds the existing asteroid but does not update its reserves.

4. **FormSurvey does not display reserves**: `PopulateFormFromViewModel` populates the resource grid with Resource/Purity/Amount columns but never looks up the linked asteroid via `survey.AsteroidUUID` to add max reserve data. The `dgvResources` grid has no MaxReserve column.

5. **SurveyViewModel has no asteroid access**: The view model wraps a `Survey` but provides no method to look up the linked `Asteroid` entity from `PlayerContext`.


## Correctness Properties

Property 1: Bug Condition - Max Reserves Extracted and Stored

_For any_ asteroid survey import where the clipboard HTML contains `ScanDetailOutputMaxReserve` nodes alongside `ScanDetailOutputResourceName` nodes, the fixed `SurveyParser.ProcessHtml` SHALL extract each max reserve value and the fixed `SurveyImportHelper.LinkOrCreateAsteroid` SHALL populate the linked `Asteroid.Reserves` list with one `AsteroidReserve` per resource, matching resource name and purity from the survey, with `MaxReserve` set to the parsed integer value.

**Validates: Requirements 2.1, 2.2**

Property 2: Preservation - Planet Survey Import Unchanged

_For any_ survey import where the clipboard HTML does NOT contain `ScanDetailOutputMaxReserve` nodes (planet surveys), the fixed code SHALL produce exactly the same `Survey` object as the original code, with identical resource parsing, no asteroid entity creation, and no reserve extraction attempted.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker/Parsers/SurveyParser.cs`

**Method**: `ProcessHtml`

**Specific Changes**:
1. **Extract max reserve values from HTML nodes**: After selecting `ScanDetailOutputMaxReserve` nodes, iterate them in parallel with the resource nodes. Parse each node's `InnerText` to extract the integer max reserve value. Store these values in a temporary list or dictionary keyed by resource index.
2. **Store parsed reserves on the Survey temporarily**: Add a `[JsonIgnore]` property to `Survey` (e.g., `ParsedMaxReserves`) as a `Dictionary<string, int>` keyed by resource name. The parser populates this during `ProcessHtml`. This is transient data used only during import — not persisted.

**File**: `OE2EmpireTracker/Models/Survey.cs`

**Specific Changes**:
3. **Add transient ParsedMaxReserves property**: Add `[JsonIgnore] public Dictionary<string, int> ParsedMaxReserves { get; set; }` to carry max reserve data from parser to import helper without affecting serialization.

**File**: `OE2EmpireTracker/Services/SurveyImportHelper.cs`

**Method**: `LinkOrCreateAsteroid`

**Specific Changes**:
4. **Populate Asteroid.Reserves from parsed data**: After creating or finding the asteroid, read `survey.ParsedMaxReserves`. For each entry, find the matching `SurveyResource` in `survey.Resources` to get the purity, then create an `AsteroidReserve` with `ResourceName`, `Purity`, and `MaxReserve`. Replace the asteroid's `Reserves` list.
5. **Handle re-import (update reserves)**: On re-import when the asteroid already exists, update its `Reserves` list with the latest parsed max reserve data from the HTML.

**File**: `OE2EmpireTracker/ViewModels/SurveyViewModel.cs`

**Specific Changes**:
6. **Add asteroid lookup method**: Add a method `FindLinkedAsteroid()` that returns the `Asteroid` entity from `PlayerContext` using `_survey.AsteroidUUID`, or null if not an asteroid survey.

**File**: `OE2EmpireTracker/Forms/Survey/FormSurvey.cs`

**Method**: `PopulateFormFromViewModel`

**Specific Changes**:
7. **Add MaxReserve column to dgvResources**: Add a read-only "MaxReserve" column to the resource grid (in the Designer or programmatically in the constructor).
8. **Populate max reserve values**: In `PopulateFormFromViewModel`, after populating resource rows, look up the linked asteroid via `viewModel.FindLinkedAsteroid()`. If found, match each resource row to an `AsteroidReserve` by resource name and purity, and populate the MaxReserve cell. For planet surveys, leave the column empty.


## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute, we will need to re-hypothesize.

**Test Plan**: Write tests that call `SurveyParser.ProcessHtml` with HTML containing `ScanDetailOutputMaxReserve` nodes and assert that max reserve values are extracted. Run these tests on the UNFIXED code to observe failures and confirm the root cause.

**Test Cases**:
1. **Parser Max Reserve Extraction Test**: Call `ProcessHtml` with HTML containing 3 `ScanDetailOutputMaxReserve` nodes with known values. Assert `survey.ParsedMaxReserves` contains the expected values. (will fail on unfixed code — property doesn't exist yet)
2. **LinkOrCreateAsteroid Reserves Test**: Call `LinkOrCreateAsteroid` with a survey that has `ParsedMaxReserves` populated. Assert the created asteroid has non-empty `Reserves`. (will fail on unfixed code — reserves not populated)
3. **Re-import Reserve Update Test**: Import an asteroid survey, then re-import with different max reserve values. Assert the asteroid's reserves are updated. (will fail on unfixed code)

**Expected Counterexamples**:
- `SurveyParser.ProcessHtml` produces a survey with no max reserve data despite HTML containing `ScanDetailOutputMaxReserve` nodes
- `LinkOrCreateAsteroid` creates an asteroid with empty `Reserves` list
- Possible causes: parser reads node count but not node text, no transport mechanism for reserve data

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FUNCTION expectedBehavior(result)
  INPUT: result of type { survey: Survey, asteroid: Asteroid }
  OUTPUT: boolean

  IF survey.ParsedMaxReserves == null OR survey.ParsedMaxReserves.Count == 0
    RETURN false

  FOR EACH resourceName IN survey.ParsedMaxReserves.Keys DO
    LET expectedMax = survey.ParsedMaxReserves[resourceName]
    LET reserve = asteroid.Reserves.Find(r => r.ResourceName == resourceName)
    IF reserve == null OR reserve.MaxReserve != expectedMax
      RETURN false
  END FOR

  RETURN asteroid.Reserves.Count == survey.ParsedMaxReserves.Count
END FUNCTION
```

```
FOR ALL input WHERE isBugCondition(input) DO
  result := importAsteroidSurvey_fixed(input)
  ASSERT expectedBehavior(result)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT importSurvey_original(input) = importSurvey_fixed(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for all non-buggy inputs

**Test Plan**: Observe behavior on UNFIXED code first for planet survey imports and resource parsing, then write property-based tests capturing that behavior.

**Test Cases**:
1. **Planet Survey Parsing Preservation**: Verify that `ProcessHtml` with planet survey HTML (no max reserve nodes) produces identical `Survey` objects before and after the fix
2. **Resource Parsing Preservation**: Verify that resource name, purity, and amount extraction is unchanged for both planet and asteroid surveys
3. **Survey Dedup Preservation**: Verify that `MergeData` continues to preserve UUID, OwnerUUID, and NickName
4. **Asteroid Type Detection Preservation**: Verify that `/cycle` rate unit detection continues to set `SurveyType = Asteroid`

### Unit Tests

- Test `SurveyParser.ProcessHtml` extracts max reserve values from HTML with `ScanDetailOutputMaxReserve` nodes
- Test `SurveyParser.ProcessHtml` with HTML containing no max reserve nodes produces no `ParsedMaxReserves`
- Test `LinkOrCreateAsteroid` populates `Asteroid.Reserves` from `survey.ParsedMaxReserves`
- Test `LinkOrCreateAsteroid` updates existing asteroid reserves on re-import
- Test `LinkOrCreateAsteroid` with planet survey (no reserves) does nothing
- Test max reserve value parsing handles edge cases: "15,000" with commas, "0", empty text

### Property-Based Tests

- Generate random asteroid survey HTML with varying numbers of resources and max reserve values; verify reserves are correctly extracted and stored on the asteroid
- Generate random planet survey HTML (no max reserve nodes); verify no reserves are extracted and no asteroid is created
- Generate random survey data and verify `MergeData` preserves UUID, OwnerUUID, NickName across all inputs

### Integration Tests

- Test full import flow: clipboard HTML → parser → import helper → asteroid creation with reserves
- Test re-import flow: import asteroid survey, re-import with updated reserves, verify asteroid reserves updated
- Test FormSurvey display: select asteroid survey, verify max reserve column shows values from linked asteroid
- Test FormSurvey display: select planet survey, verify max reserve column is empty
