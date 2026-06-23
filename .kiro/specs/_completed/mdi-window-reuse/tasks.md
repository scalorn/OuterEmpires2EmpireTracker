# Implementation Plan: MDI Window Number Reuse (BL-067)

## Overview

Replace the monotonic `_windowNumberCounters` dictionary in `MainWindow.OpenMdiChild<T>()` with a scan of `this.MdiChildren` to find the lowest unused positive integer per form type. Single-method, single-file fix. Property tests validate the number-assignment algorithm using FsCheck.

## Tasks

- [x] 1. Implement the fix in MainWindow.OpenMdiChild<T>()
  - [x] 1.1 Remove `_windowNumberCounters` field from `MainWindow.cs`
    - Delete the `private readonly Dictionary<string, int> _windowNumberCounters = new Dictionary<string, int>();` field declaration
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.2 Replace counter logic with gap-scanning logic in `OpenMdiChild<T>()`
    - Replace the counter increment block with a scan of `this.MdiChildren.OfType<T>()` to collect used numbers via `Tag` property
    - Find the smallest positive integer not in the used set: start at 1, increment while contained
    - Keep all downstream code unchanged (Tag assignment, Text formatting, RestoreState call, Show)
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 2. Write property tests for window number assignment
  - [x] 2.1 Create `OE2EmpireTracker.Tests/Forms/MdiWindowNumberPropertyTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 2.2 Write property test: Lowest Unused Number Assignment (Property 1)
    - **Property 1: Bug Condition - Lowest Unused Number Assignment**
    - Generate random `HashSet<int>` of used positive integers (0–20 elements, values 1–50)
    - Replicate the gap-scanning algorithm: `int n = 1; while (used.Contains(n)) n++;`
    - Assert result is >= 1, not in the used set, and all integers from 1 to result-1 are in the used set
    - Use FsCheck.NUnit `[Property(MaxTest = 200)]`
    - **Validates: Requirements 2.1, 2.2, 2.3**

  - [x] 2.3 Write property test: Unique Numbering Across Sequences (Property 2)
    - **Property 2: Preservation - Unique Numbering and Form Setup**
    - Generate random sequences of "open" and "close" operations (5–20 steps)
    - Simulate: maintain a `HashSet<int>` of open window numbers; on open, assign lowest unused and add; on close, remove a random open number
    - After each open, assert: assigned number >= 1, not previously in the set, and all open numbers are unique
    - Use FsCheck.NUnit `[Property(MaxTest = 200)]`
    - **Validates: Requirements 3.1, 3.2, 3.4, 3.5**

- [x] 3. Write unit tests for edge cases
  - [x] 3.1 Add edge case unit tests to `MdiWindowNumberPropertyTests.cs`
    - Test: no open windows → assigns 1 (Req 2.3)
    - Test: contiguous {1, 2, 3} → assigns 4 (no gap, preservation)
    - Test: gap at start {2, 3} → assigns 1 (Req 2.1)
    - Test: gap in middle {1, 3, 4} → assigns 2 (Req 2.2)
    - Test: multiple gaps {2, 5, 8} → assigns 1 (lowest gap first, Req 2.2)
    - Test: single window {1} closed (empty set) → assigns 1 (Req 2.3)
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 4. Build and verify
  - Build solution with MSBuild and run tests with vstest.console
  - Verify all existing tests continue to pass (preservation)
  - Verify new property tests pass

## Notes

- The fix is localized to `MainWindow.OpenMdiChild<T>()` — one method, one file
- No changes needed to WindowStateHelper or PreferencesStore
- Window number reuse means the new window inherits saved state from the previous window with that number — this is desired behavior
- All form types benefit since they all go through `OpenMdiChild<T>()`
- New `.cs` files require `Compile Include` entries in the old-style `.csproj`
- Do NOT use `semanticRename` — manual find-and-replace only
- Build with MSBuild, test with vstest.console against the built test DLL
- Property tests use FsCheck.NUnit following existing patterns
