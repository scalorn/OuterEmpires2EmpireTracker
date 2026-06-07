# Implementation Plan: Asset Type Constants

## Overview

Centralise all known game API `typeC` string codes into a single `AssetTypeCodes` static constants class in `OE2EmpireTracker.Common/Constants/`, replace raw string literals across production and test code, and extend the magic-strings audit tool to cover the new constants.

## Tasks

- [x] 1. Create the AssetTypeCodes constants class
  - [x] 1.1 Create `OE2EmpireTracker.Common/Constants/AssetTypeCodes.cs`
    - Define `public static class AssetTypeCodes` in namespace `OE2EmpireTracker.Constants`
    - Add `public const string` fields for all swagger-confirmed cargo codes: Blueprint ("Bp"), Crate ("Cr"), Survey ("Sc"), ShipPart ("S"), Flatpack ("F"), Workforce ("W"), Resource ("R"), Ammunition ("A"), Deployable ("D"), Share ("Sh"), CommodityL ("L")
    - Add fields for discovered-in-data codes: Commodity ("C"), ShipHull ("SH")
    - Add fields for location codes: Colony ("Co"), Station ("St"), Ship ("Sh")
    - Add XML documentation comments on each field and on the class (case-sensitivity note, SH vs Sh disambiguation)
    - All constant values must be trimmed (no trailing spaces)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 7.3, 7.4_
    - Inputs: `design.md` (AssetTypeCodes class section)
    - Output: `OE2EmpireTracker.Common/Constants/AssetTypeCodes.cs`
    - Verification: `getDiagnostics` — zero errors/warnings

- [x] 2. Replace raw literals in AssetMergeService
  - [x] 2.1 Replace typeC literals in `AssetMergeService.MapAssetTypeC`
    - Replace all `string.Equals(trimmed, "Bp", ...)` with `string.Equals(trimmed, AssetTypeCodes.Blueprint, ...)`
    - Replace all other raw typeC literals in the switch/comparison chain (Cr, Sc, S, F, W, R, A, D, Sh, L, C, SH)
    - Ensure `StringComparison.OrdinalIgnoreCase` is preserved on all comparisons
    - Add `using OE2EmpireTracker.Constants;` if not already present
    - _Requirements: 3.1, 3.2, 7.1_
    - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
    - Output: `OE2EmpireTracker.Common/Services/AssetMergeService.cs` (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

- [x] 3. Replace raw literals in QueueSyncService and ColonyMergeService
  - [x] 3.1 Replace typeC literals in `QueueSyncService.CascadeCargoDetailItems`
    - Replace switch case labels (`case "Cr":`, etc.) with AssetTypeCodes constants
    - Remove the `case "Crate":` label (API never sends this value)
    - Ensure switch expression operates on trimmed value
    - Add `using OE2EmpireTracker.Constants;` if not already present
    - _Requirements: 3.3, 3.6, 2.2, 6.2_
    - Inputs: `OE2EmpireTracker.Common/Services/QueueSyncService.cs`
    - Output: `OE2EmpireTracker.Common/Services/QueueSyncService.cs` (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

  - [x] 3.2 Replace typeC literals in `ColonyMergeService`
    - Replace `string.Equals(typeC, "Bp", ...)` with `AssetTypeCodes.Blueprint` in `HasBlueprintProperties`
    - Replace `string.Equals(..., "Sc", ...)` with `AssetTypeCodes.Survey` in `IsSurveyItem`
    - Add `using OE2EmpireTracker.Constants;` if not already present
    - _Requirements: 3.5, 7.1_
    - Inputs: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`
    - Output: `OE2EmpireTracker.Common/Services/ColonyMergeService.cs` (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

- [x] 4. Replace raw literals in GameApiSyncScheduler
  - [x] 4.1 Replace location typeC literals in `GameApiSyncScheduler`
    - Replace `string.Equals(location.LocationType, "Co", ...)` with `AssetTypeCodes.Colony`
    - Replace `"St"` with `AssetTypeCodes.Station`
    - Replace `"Sh"` with `AssetTypeCodes.Ship`
    - Ensure `StringComparison.OrdinalIgnoreCase` is preserved
    - Add `using OE2EmpireTracker.Constants;` if not already present
    - _Requirements: 3.4, 7.1_
    - Inputs: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
    - Output: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs` (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

- [x] 5. Checkpoint - Build verification
  - Build full solution with zero errors and zero warnings
  - Ensure all production code replacements compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Replace raw literals in test code
  - [x] 6.1 Replace typeC literals in `QueueSyncServicePropertyTests.cs`
    - Replace `Gen.Elements("Bp", "Cr", ...)` with `Gen.Elements(AssetTypeCodes.Blueprint, AssetTypeCodes.Crate, ...)`
    - Replace any raw typeC assignments on test fixtures
    - Add `using OE2EmpireTracker.Constants;`
    - _Requirements: 4.1, 4.2_
    - Inputs: `OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs`
    - Output: `OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs` (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

  - [x] 6.2 Replace typeC literals in `AssetMergeServiceTests.cs` and related test files
    - Replace raw typeC string assignments (e.g. `TypeC = "Bp"`) with AssetTypeCodes constants
    - Replace any raw typeC values in `AssetMergeServicePropertyTests.cs`, `AssetCascadePropertyTests.cs`
    - Add `using OE2EmpireTracker.Constants;` to each modified file
    - _Requirements: 4.1, 4.3, 4.4_
    - Inputs: `OE2EmpireTracker.Tests/Services/AssetMergeServiceTests.cs`, `AssetMergeServicePropertyTests.cs`, `AssetCascadePropertyTests.cs`
    - Output: Same files (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

  - [x] 6.3 Replace typeC literals in remaining test files
    - Scan `GameApiSyncSchedulerAssetTests.cs` and `ColonyMergeServiceTests.cs` for raw typeC literals
    - Replace with AssetTypeCodes constants
    - Add `using OE2EmpireTracker.Constants;` to each modified file
    - _Requirements: 4.1, 4.3_
    - Inputs: `OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerAssetTests.cs`, `ColonyMergeServiceTests.cs`
    - Output: Same files (modified)
    - Verification: `getDiagnostics` — zero errors/warnings

- [x] 7. Write unit and property tests for AssetTypeCodes
  - [x] 7.1 Write unit tests verifying each constant has its expected value
    - Create `OE2EmpireTracker.Tests/Constants/AssetTypeCodesTests.cs`
    - Test each constant equals its expected string value (Blueprint == "Bp", Crate == "Cr", etc.)
    - Test MapAssetTypeC returns correct enum for each cargo constant
    - _Requirements: 1.1, 1.2, 1.3, 6.1_
    - Inputs: `OE2EmpireTracker.Common/Constants/AssetTypeCodes.cs`, `AssetMergeService.cs`
    - Output: `OE2EmpireTracker.Tests/Constants/AssetTypeCodesTests.cs`
    - Verification: tests pass via vstest.console

  - [x] 7.2 Write property test: all constants are trimmed
    - **Property 1: All constants are trimmed**
    - **Validates: Requirements 1.4, 2.3**
    - Use reflection to get all `public const string` fields from `AssetTypeCodes`
    - Assert each field value equals `value.Trim()`
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - File: `OE2EmpireTracker.Tests/Constants/AssetTypeCodesPropertyTests.cs`
    - Verification: tests pass via vstest.console

  - [x] 7.3 Write property test: trailing-space resilience in MapAssetTypeC
    - **Property 2: Trailing-space resilience in MapAssetTypeC**
    - **Validates: Requirements 2.1, 2.2**
    - For each cargo AssetTypeCodes constant, generate 1-3 trailing spaces
    - Assert `MapAssetTypeC(value + spaces)` returns same result as `MapAssetTypeC(value)`
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - File: `OE2EmpireTracker.Tests/Constants/AssetTypeCodesPropertyTests.cs`
    - Verification: tests pass via vstest.console

- [x] 8. Extend magic-strings audit tool
  - [x] 8.1 Update `magic-strings.js` to scan Common project constants and sources
    - Add `OE2EmpireTracker.Common/Constants` as a second constants source directory
    - Add `OE2EmpireTracker.Common` to the source file scan (excluding its own Constants directory)
    - Implement single-character false-positive exclusion: for constants with 1-char values, only flag matches where the literal is exactly `"X"` (not a substring of a longer literal)
    - _Requirements: 5.1, 5.2, 5.4_
    - Inputs: `.kiro/tools/magic-strings.js`
    - Output: `.kiro/tools/magic-strings.js` (modified)
    - Verification: `node .kiro/tools/magic-strings.js` exits with code 0

- [x] 9. Final checkpoint - Full verification
  - Build full solution with zero errors and zero warnings
  - Run `node .kiro/tools/magic-strings.js` — zero findings (confirms Req 5.3)
  - Run `node .kiro/tools/audit.js` — zero findings
  - Run vstest.console against `OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll` — all tests pass
  - Confirm refactoring is behaviour-preserving (Req 6.1, 6.3, 6.4)
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The refactoring is purely mechanical — no runtime behaviour changes
- Constants store trimmed values; comparison sites already trim before comparing
- The "Crate" case label removal (task 3.1) is safe — the API has never sent "Crate"
- FsCheck 2.16.6 is the installed version — use `[FsCheck.NUnit.Property]` attribute syntax
- Property 1 (trimmed constants) uses reflection, not generated inputs — but FsCheck wrapper validates it
- Property 2 (trailing-space resilience) generates whitespace suffixes to verify MapAssetTypeC handles padding

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "3.1", "3.2", "4.1"] },
    { "id": 2, "tasks": ["6.1", "6.2", "6.3", "7.1"] },
    { "id": 3, "tasks": ["7.2", "7.3", "8.1"] }
  ]
}
```
