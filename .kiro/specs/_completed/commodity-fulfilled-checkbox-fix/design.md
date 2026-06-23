# Commodity Fulfilled Checkbox Bugfix Design

## Overview

The "Fulfilled" checkbox on the commodity request grid in FormColonyV2 reverts to unchecked immediately after clicking. The root cause is that `ColonyService.UpdateCommodityRequest` does not persist the `Fulfilled` property on the `CommodityRequested` model. After the service call fires `OnColonyDataChanged`, the form repopulates the grid from the model — reading `Fulfilled = false` — and overwrites the checkbox.

The fix adds a `fulfilled` parameter to `UpdateCommodityRequest`, sets `existing.Fulfilled` in the service method, and passes the checkbox value from the form's `CellValueChanged` handler.

## Glossary

- **Bug_Condition (C)**: The user toggles the Fulfilled checkbox on a commodity request row, triggering `CellValueChanged` with `e.ColumnIndex == 2`
- **Property (P)**: After the service call and grid repopulation, the checkbox reflects the value the user set (checked or unchecked)
- **Preservation**: Editing Amount (column 1) or NeedBy (column 3) must continue to work without affecting the Fulfilled state; `DeliveryFulfillment.FulfillCommodity` must continue to set Fulfilled independently
- **UpdateCommodityRequest**: The method in `ColonyService` that persists changes to a `CommodityRequested` entity
- **CommodityRequested**: The model in `Models/CommodityRequested.cs` with properties: Name, Requested, Delivered, NeedBy, Fulfilled
- **PopulateCommodityRequestGrid**: The method in FormColonyV2 that reads model state and renders the grid rows

## Bug Details

### Bug Condition

The bug manifests when the user clicks the Fulfilled checkbox (column 2) on any commodity request row. The `CellValueChanged` handler calls `UpdateCommodityRequest` which updates `Delivered` but never sets `request.Fulfilled`. The subsequent `OnColonyDataChanged` event triggers `PopulateCommodityRequestGrid()` which reads `Fulfilled = false` from the model and overwrites the cell.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type CellValueChangedEvent
  OUTPUT: boolean

  RETURN input.ColumnIndex == 2
         AND input.RowIndex >= 0
         AND row.Tag IS CommodityRequested
         AND UpdateCommodityRequest does NOT set existing.Fulfilled
END FUNCTION
```


### Examples

- User checks Fulfilled on "Steel Plates" row → `UpdateCommodityRequest` is called → `existing.Fulfilled` remains `false` → grid repopulates → checkbox shows unchecked (BUG)
- User unchecks Fulfilled on "Copper Wire" row → same flow → `existing.Fulfilled` remains `true` (if previously set by DeliveryFulfillment) → checkbox shows checked despite user unchecking (BUG)
- User checks Fulfilled when `Delivered < Requested` → handler sets `delivered = Requested` and calls service → `Fulfilled` not persisted → checkbox reverts (BUG)
- User edits Amount column → `UpdateCommodityRequest` called with existing Fulfilled value → Fulfilled unchanged (CORRECT, not a bug condition)

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Editing the Amount (Requested) column must continue to update only the `Requested` field without affecting `Fulfilled`
- Editing the NeedBy column must continue to update only the `NeedBy` field without affecting `Fulfilled`
- `DeliveryFulfillment.FulfillCommodity` must continue to set both `Delivered = Requested` and `Fulfilled = true` independently of this fix
- Strikethrough styling on fulfilled rows must continue to render based on the `Fulfilled` property
- The `OnColonyDataChanged` → `PopulateCommodityRequestGrid` flow must continue to reflect model state accurately

**Scope:**
All inputs that do NOT involve toggling the Fulfilled checkbox (column 2) should be completely unaffected by this fix. This includes:
- Editing Amount (column 1)
- Editing NeedBy (column 3)
- Adding/removing commodity requests
- DeliveryFulfillment marking commodities as delivered

## Hypothesized Root Cause

Based on the bug description and code analysis, the root cause is confirmed:

1. **Missing parameter in service method**: `ColonyService.UpdateCommodityRequest(colonyUUID, commodityName, requested, delivered, needBy)` sets `existing.Requested`, `existing.Delivered`, and `existing.NeedBy` but has no `fulfilled` parameter and never sets `existing.Fulfilled`.

2. **Form handler does not pass Fulfilled**: The `CellValueChanged` handler for column 2 computes `bool fulfilled` and `int delivered` but only passes `delivered` to the service. The `fulfilled` boolean is used to derive `delivered` but is never persisted to the model.

3. **Grid repopulation overwrites UI state**: After `UpdateCommodityRequest` fires `OnColonyDataChanged`, `PopulateCommodityRequestGrid()` reads `request.Fulfilled` (still `false`) and sets the checkbox cell value accordingly, reverting the user's click.


## Correctness Properties

Property 1: Bug Condition - Fulfilled Checkbox Persists User Value

_For any_ `CellValueChanged` event where `e.ColumnIndex == 2` (Fulfilled checkbox) and the row's `Tag` is a valid `CommodityRequested`, the fixed `UpdateCommodityRequest` SHALL set `existing.Fulfilled` to the checkbox value, so that when `PopulateCommodityRequestGrid` re-reads the model, the checkbox reflects the user's choice.

**Validates: Requirements 2.1, 2.2**

Property 2: Preservation - Non-Fulfilled Column Edits Unchanged

_For any_ `CellValueChanged` event where `e.ColumnIndex != 2` (Amount or NeedBy edits), the fixed `UpdateCommodityRequest` SHALL produce the same result as the original function, preserving the existing `Fulfilled` value on the `CommodityRequested` model without modification.

**Validates: Requirements 3.1, 3.2, 3.3**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker/Services/ColonyService.cs`

**Function**: `UpdateCommodityRequest`

**Specific Changes**:
1. **Add `fulfilled` parameter**: Add a `bool fulfilled` parameter to the method signature after `DateTime needBy`
2. **Set `existing.Fulfilled`**: Inside the `if (existing != null)` block, add `existing.Fulfilled = fulfilled;` after setting `existing.NeedBy`
3. **Update log statement**: Include `fulfilled` in the log message for traceability

**File**: `OE2EmpireTracker/Forms/ColonyV2/FormColonyV2.cs`

**Function**: `DgvCommodityRequests_CellValueChanged`

**Specific Changes**:
4. **Column 2 handler — pass `fulfilled`**: Change the call from `_colonyService.UpdateCommodityRequest(_selectedColonyUUID, request.Name, request.Requested, delivered, request.NeedBy)` to include `fulfilled` as the last argument
5. **Column 1 handler — pass existing Fulfilled**: Add `request.Fulfilled` as the last argument to preserve the current value
6. **Column 3 handler — pass existing Fulfilled**: Add `request.Fulfilled` as the last argument to preserve the current value


## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm that `UpdateCommodityRequest` does not set `Fulfilled`.

**Test Plan**: Write a unit test that calls `UpdateCommodityRequest` with a commodity whose `Fulfilled` is initially `false`, then checks whether `Fulfilled` is set after the call. Run on UNFIXED code to observe the failure.

**Test Cases**:
1. **Fulfilled Not Set Test**: Call `UpdateCommodityRequest` and verify `Fulfilled` remains `false` even when the intent is to mark it fulfilled (will fail on unfixed code — confirms the bug)
2. **Unfulfill Not Set Test**: Set `Fulfilled = true` manually, call `UpdateCommodityRequest`, verify `Fulfilled` remains `true` even when intent is to unfulfill (will fail on unfixed code)

**Expected Counterexamples**:
- `existing.Fulfilled` is never modified by `UpdateCommodityRequest` regardless of input
- The `fulfilled` parameter does not exist on the method signature

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds (Fulfilled checkbox toggled), the fixed function persists the correct `Fulfilled` value.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := UpdateCommodityRequest_fixed(colonyUUID, name, requested, delivered, needBy, fulfilled)
  commodity := FindCommodity(colonyUUID, name)
  ASSERT commodity.Fulfilled == fulfilled
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold (Amount or NeedBy edits), the fixed function produces the same result as the original — specifically that `Fulfilled` is passed through unchanged.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT UpdateCommodityRequest_fixed(colonyUUID, name, requested, delivered, needBy, existingFulfilled).Fulfilled
         == existingFulfilled
END FOR
```

**Testing Approach**: Unit tests are sufficient here because the fix is a single parameter addition with deterministic behavior. Property-based testing could generate random `(requested, delivered, needBy, fulfilled)` tuples to verify the property is always persisted correctly.

**Test Cases**:
1. **Amount Edit Preserves Fulfilled**: Edit Amount on a fulfilled commodity → verify `Fulfilled` remains `true`
2. **NeedBy Edit Preserves Fulfilled**: Edit NeedBy on a fulfilled commodity → verify `Fulfilled` remains `true`
3. **Amount Edit Preserves Unfulfilled**: Edit Amount on an unfulfilled commodity → verify `Fulfilled` remains `false`

### Unit Tests

- Test `UpdateCommodityRequest` sets `Fulfilled = true` when `fulfilled` parameter is `true`
- Test `UpdateCommodityRequest` sets `Fulfilled = false` when `fulfilled` parameter is `false`
- Test `UpdateCommodityRequest` preserves `Fulfilled` when called from Amount/NeedBy edits (pass existing value)
- Test that `Delivered` is still set correctly alongside `Fulfilled`
- Test that commodity not found case still logs debug and does not throw

### Property-Based Tests

- Generate random `(requested, delivered, needBy, fulfilled)` tuples and verify `UpdateCommodityRequest` always persists all four fields correctly
- Generate random sequences of checkbox toggles and verify the model always reflects the last toggle value

### Integration Tests

- Test full flow: toggle checkbox → service call → `OnColonyDataChanged` → `PopulateCommodityRequestGrid` → verify checkbox state matches
- Test that strikethrough styling updates correctly after Fulfilled is toggled
- Test that `DeliveryFulfillment.FulfillCommodity` still works independently of the new parameter
