# Implementation Plan: Structure Name Normalization

## Overview

Add `OutputItemName` computed property to `Blueprint`, update `ExtendedName` to use it, and refactor `ColonyParser.BuildFlatpackLookup` to use `OutputItemName` instead of inline suffix stripping. All other consumers get the fix for free via `ExtendedName`. Property-based tests with FsCheck validate the 5 correctness properties from the design.

## Tasks

- [x] 1. Add OutputItemName property and update ExtendedName
  - [x] 1.1 Add `OutputItemName` computed property to `Blueprint.cs`
    - Add `[JsonIgnore]` `OutputItemName` property that strips " Flatpack" suffix for flatpack blueprints (case-insensitive), returns `Name` unchanged for non-flatpacks, returns empty string for null/empty `Name`
    - Uses `BluePrintType.IsFlatpack()` guard and `StringComparison.OrdinalIgnoreCase`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_
  - [x] 1.2 Update `ExtendedName` getter in `Blueprint.cs` to use `OutputItemName` instead of `Name`
    - Change the single line `extendedName += Name + " "` to `extendedName += OutputItemName + " "`
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2_
  - [x] 1.3 Update existing `BlueprintTests.cs` assertions that are affected by the `ExtendedName` change
    - Non-flatpack blueprint tests should pass unchanged since `OutputItemName` == `Name` for them
    - Verify no existing test creates a flatpack blueprint with " Flatpack" suffix in the name — if any do, update expected values
    - _Requirements: 2.1_

- [x] 2. Refactor BuildFlatpackLookup to use OutputItemName
  - [x] 2.1 Replace inline suffix-stripping in `ColonyParser.BuildFlatpackLookup` with `bp.OutputItemName`
    - Remove the `if (designName.EndsWith(" Flatpack", ...))` block
    - Use `bp.OutputItemName` as the design name key
    - Keep registering both `OutputItemName` and `bp.Name` as lookup keys
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 3. Checkpoint — Verify compilation and existing tests
  - Ensure all code compiles cleanly via `getDiagnostics`. Ensure existing tests still pass. Ask the user if questions arise.

- [x] 4. Write property-based tests (FsCheck) and unit tests
  - [x] 4.1 Write property test for suffix stripping round-trip
    - **Property 1: Suffix stripping round-trip**
    - For flatpack blueprints whose Name ends with " Flatpack": `OutputItemName + " Flatpack"` == original `Name` (preserving original casing). For non-flatpacks or names not ending with " Flatpack": `OutputItemName` == `Name`. For null/empty Name: `OutputItemName` == empty string.
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 5.3**
  - [x] 4.2 Write property test for ExtendedName uses OutputItemName
    - **Property 2: ExtendedName uses OutputItemName**
    - For flatpack blueprints with non-null UUID whose Name ends with " Flatpack": `ExtendedName` contains `OutputItemName` and does NOT contain the full Name with " Flatpack" suffix. For non-flatpacks: `ExtendedName` contains `Name`.
    - **Validates: Requirements 2.1**
  - [x] 4.3 Write property test for BuildFlatpackLookup behavioral equivalence
    - **Property 3: BuildFlatpackLookup behavioral equivalence**
    - The refactored `BuildFlatpackLookup` produces the same key-value pairs as the original inline implementation for any set of flatpack blueprints.
    - **Validates: Requirements 4.1, 4.2, 4.3**
  - [x] 4.4 Write property test for serialization round-trip
    - **Property 4: Serialization round-trip preserves Name**
    - Serializing then deserializing a Blueprint preserves `Name`. Serialized JSON does not contain "OutputItemName" key.
    - **Validates: Requirements 1.5, 5.1, 5.2**
  - [x] 4.5 Write property test for read-only invariant
    - **Property 5: Read-only invariant**
    - Accessing `OutputItemName` and `ExtendedName` does not modify `Name`, `BluePrintType`, `Class`, `Evolution`, `TechLevel`, `NickName`, or `UUID`.
    - **Validates: Requirements 5.1**
  - [x] 4.6 Write unit tests for specific examples and edge cases
    - "Mining Rig Flatpack" → "Mining Rig", "Habitation Block Flatpack" → "Habitation Block"
    - Non-flatpack blueprint Name returned unchanged
    - Name exactly " Flatpack" → empty string after stripping
    - Name "Flatpack" without leading space → NOT stripped
    - Null Name → empty string, empty Name → empty string
    - ExtendedName for flatpack contains stripped name, not full market name
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.3_

- [x] 5. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The core implementation is just 2 code changes in `Blueprint.cs` (tasks 1.1, 1.2) and 1 refactor in `ColonyParser.cs` (task 2.1)
- All other consumers (colony structure UI, activity collectors, flatpack dropdown) get the fix for free via `ExtendedName`
- Test file: `OE2EmpireTracker.Tests/Models/BlueprintOutputNameTests.cs`
- FsCheck 2.16.6 with FsCheck.NUnit, NUnit 4.5.1
- Do NOT use `dotnet test` — use `getDiagnostics` for compile checks, `vstest.console` for test execution
