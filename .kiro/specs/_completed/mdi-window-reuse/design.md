# MDI Window Number Reuse Bugfix Design

## Overview

When MDI child windows are closed and new ones opened, the system assigns ever-increasing window numbers instead of reusing gaps left by closed windows. The fix replaces the simple incrementing counter (`_windowNumberCounters`) with a scan of `this.MdiChildren` to find the lowest unused positive integer for each form type. This is a single-method, single-file change in `MainWindow.OpenMdiChild<T>()`.

## Glossary

- **Bug_Condition (C)**: The condition that triggers the bug — when a new MDI child is opened after one or more windows of the same form type have been closed, leaving gaps in the numbering sequence
- **Property (P)**: The desired behavior — the new window receives the lowest positive integer not currently in use by any open window of the same form type
- **Preservation**: Existing behaviors that must remain unchanged — Tag assignment, title bar formatting, WindowStateHelper integration, unique numbering per form type, independent numbering across form types
- **OpenMdiChild\<T\>()**: The generic method in `MainWindow.cs` that creates and configures MDI child windows, including number assignment
- **_windowNumberCounters**: The `Dictionary<string, int>` field that tracks the next window number per form type (to be removed)
- **formTypeKey**: The `typeof(T).Name` string used to key window numbers and WindowStateHelper state

## Bug Details

### Bug Condition

The bug manifests when a user closes one or more MDI child windows and then opens a new window of the same form type. The `_windowNumberCounters` dictionary increments monotonically per form type and never checks for gaps left by closed windows.

**Formal Specification:**
```
FUNCTION isBugCondition(state)
  INPUT: state = { formType: string, openWindowNumbers: Set<int>, counterValue: int }
  OUTPUT: boolean
  
  LET nextFromCounter = counterValue + 1
  LET lowestUnused = smallestPositiveIntegerNotIn(openWindowNumbers)
  
  RETURN nextFromCounter != lowestUnused
END FUNCTION
```

In other words, the bug triggers whenever there is a gap in the open window numbers for a form type — the counter skips over the gap and assigns a higher number.

### Examples

- Open Blueprint #1, #2, #3. Close #2. Open new Blueprint → gets #4 (bug). Expected: #2.
- Open Colony #1, #2. Close both. Open new Colony → gets #3 (bug). Expected: #1.
- Open Survey #1, #2, #3. Close #1 and #3. Open new Survey → gets #4 (bug). Expected: #1.
- Open Blueprint #1, #2, #3 (none closed). Open new Blueprint → gets #4 (correct, no bug condition).

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- `form.Tag` must continue to be set to the assigned window number (as `int`)
- `form.Text` must continue to be formatted as `"#N - FormTitle"` where N is the assigned number
- `WindowStateHelper.RestoreState` must continue to be called with `(form, formTypeKey, windowNumber)`
- Each simultaneously open window of the same form type must have a unique window number
- Different form types must continue to be numbered independently (Blueprint #1 and Colony #1 can coexist)
- The form must continue to be shown via `form.Show()` after configuration

**Scope:**
All inputs that do NOT involve window number assignment are completely unaffected. The fix changes only how the number is computed — everything downstream (Tag, Text, RestoreState, Show) remains identical.

## Hypothesized Root Cause

Based on the bug description and code inspection, the root cause is confirmed:

1. **Monotonic Counter**: `_windowNumberCounters` is a `Dictionary<string, int>` that stores a single incrementing integer per form type. It is incremented on every `OpenMdiChild<T>()` call and never decremented or reset when windows close.

2. **No Gap Detection**: The counter has no awareness of which windows are currently open. It does not inspect `this.MdiChildren` to find gaps in the numbering sequence.

3. **No Close Hook**: There is no `FormClosed` handler that would update the counter when a window is closed. The counter only goes up.

The fix is straightforward: replace the counter lookup with a scan of `this.MdiChildren.OfType<T>()` to collect used numbers, then find the smallest positive integer not in that set.

## Correctness Properties

Property 1: Bug Condition - Lowest Unused Number Assignment

_For any_ set of currently open MDI child windows of a given form type (with window numbers stored in their `Tag` properties), when a new window of that form type is opened, the assigned window number SHALL be the smallest positive integer not present in the set of currently used numbers. When no windows are open, the assigned number SHALL be 1.

**Validates: Requirements 2.1, 2.2, 2.3**

Property 2: Preservation - Unique Numbering and Form Setup

_For any_ sequence of open/close operations on MDI child windows, the fixed `OpenMdiChild<T>()` SHALL produce windows where: (a) `form.Tag` equals the assigned window number, (b) `form.Text` starts with `"#N - "` where N is the assigned number, and (c) no two simultaneously open windows of the same form type share the same window number.

**Validates: Requirements 3.1, 3.2, 3.4, 3.5**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `OE2EmpireTracker/Forms/MainWindow.cs`

**Function**: `OpenMdiChild<T>()`

**Specific Changes**:

1. **Remove `_windowNumberCounters` field**: Delete the `private readonly Dictionary<string, int> _windowNumberCounters = new Dictionary<string, int>();` field declaration.

2. **Replace counter logic with gap-scanning logic**: Replace the counter increment block:
   ```csharp
   string formTypeKey = typeof(T).Name;
   if (!_windowNumberCounters.ContainsKey(formTypeKey))
       _windowNumberCounters[formTypeKey] = 0;
   _windowNumberCounters[formTypeKey]++;
   int windowNumber = _windowNumberCounters[formTypeKey];
   ```
   With a scan of open MDI children:
   ```csharp
   string formTypeKey = typeof(T).Name;
   var usedNumbers = this.MdiChildren
       .OfType<T>()
       .Select(f => (int)f.Tag)
       .ToHashSet();
   int windowNumber = 1;
   while (usedNumbers.Contains(windowNumber)) windowNumber++;
   ```

3. **No other changes**: The rest of `OpenMdiChild<T>()` (Tag assignment, Text formatting, RestoreState call, Show) remains identical.

4. **No changes to other files**: `WindowStateHelper`, `PreferencesStore`, and all form types are unaffected.

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

Since `OpenMdiChild<T>()` is tightly coupled to WinForms (creates forms, sets MdiParent, calls Show), the property tests will target the extracted number-assignment logic rather than the full method. We'll extract the core algorithm into a testable static helper or test it via a pure function that mirrors the logic.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis.

**Test Plan**: Write a unit test that simulates the counter-based assignment and verifies it produces incorrect numbers when gaps exist. Run on UNFIXED code to observe failures.

**Test Cases**:
1. **Gap After Close**: Simulate open #1, #2, #3, close #2, open new → counter gives #4, expected #2 (will fail on unfixed code)
2. **All Closed Restart**: Simulate open #1, #2, close both, open new → counter gives #3, expected #1 (will fail on unfixed code)
3. **Multiple Gaps**: Simulate open #1–#5, close #1, #3, #5, open new → counter gives #6, expected #1 (will fail on unfixed code)

**Expected Counterexamples**:
- Counter always returns `previousMax + 1` regardless of gaps
- Root cause confirmed: no gap detection in counter logic

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FOR ALL usedNumbers: Set<int> (subset of positive integers) DO
  result := findLowestUnused(usedNumbers)
  ASSERT result == smallestPositiveIntegerNotIn(usedNumbers)
  ASSERT result >= 1
  ASSERT result NOT IN usedNumbers
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold (no gaps), the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL usedNumbers: Set<int> WHERE usedNumbers == {1, 2, ..., N} (contiguous from 1) DO
  ASSERT findLowestUnused(usedNumbers) == N + 1
  // Same result as the old counter would have produced
END FOR
```

**Testing Approach**: Property-based testing with FsCheck is ideal here because:
- The input domain (sets of positive integers) is easy to generate
- The expected output (smallest missing positive integer) has a clear oracle
- Edge cases (empty set, single element, large gaps) are naturally explored

**Test Plan**: Write a pure `FindLowestUnusedNumber` helper (or test the logic inline) that takes a `HashSet<int>` of used numbers and returns the lowest unused. Test this with FsCheck generators.

### Unit Tests

- Test the number assignment with no open windows → returns 1
- Test with contiguous open windows {1, 2, 3} → returns 4
- Test with gap {1, 3} → returns 2
- Test with multiple gaps {2, 5, 8} → returns 1
- Test that different form types are independent

### Property-Based Tests

- Generate random sets of positive integers representing used window numbers; verify the assigned number is the smallest positive integer not in the set
- Generate random sequences of open/close operations; verify all open windows have unique numbers at every step
- Generate contiguous sets {1..N}; verify the result matches what the old counter would have produced (preservation)

### Integration Tests

- Open multiple MDI children, close some, open new ones, verify title bars show correct numbers
- Verify WindowStateHelper.RestoreState is called with the reused number (inherits saved state)
- Verify different form types maintain independent numbering through open/close cycles
