# Implementation Plan: Blueprint Form Fixes (BL-062, BL-066)

## Overview

Two localized bug fixes in the Blueprint form. BL-062 adds resources-only import detection and routing to the selected blueprint. BL-066 assigns Name properties to dynamically created filter controls so WindowStateHelper can persist their state. Both fixes stay within the existing architecture — no new layers or services.

## Tasks

- [x] 1. Add IsResourcesOnlyImport and MergeResourcesOnly to MarketBlueprintImporter
  - [x] 1.1 Implement `IsResourcesOnlyImport` static method in `MarketBlueprintImporter`
    - Add `internal static bool IsResourcesOnlyImport(Blueprint bp)` to `OE2EmpireTracker/Services/MarketBlueprintImporter.cs`
    - Returns `true` when `bp.Resources != null && bp.Resources.Count > 0` AND `string.IsNullOrEmpty(bp.BluePrintType)` AND `bp.Class == 0` AND `string.IsNullOrEmpty(bp.TechLevel)`
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.2 Implement `MergeResourcesOnly` static method in `MarketBlueprintImporter`
    - Add `internal static void MergeResourcesOnly(Blueprint target, Blueprint incoming)` to `OE2EmpireTracker/Services/MarketBlueprintImporter.cs`
    - Replace `target.Resources` with `incoming.Resources`
    - If incoming has properties, merge them using the same protected-property logic as `UpdateExisting` (preserving "Manufacture Run Time" and "Power Required")
    - Do NOT overwrite any scalar fields (Name, BluePrintType, Class, TechLevel, Evolution, UUID, OwnerUUID, NickName, CopyCost, Description, BaseBlueprintUUID)
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3_

  - [x] 1.3 Write property test for IsResourcesOnlyImport classification
    - **Property 1: IsResourcesOnlyImport classification**
    - Create `OE2EmpireTracker.Tests/Services/ResourcesOnlyImportPropertyTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - Generate random `Blueprint` objects with varying Resources (0–5 entries), BluePrintType (null/empty/non-empty), Class (0/positive), TechLevel (null/empty/non-empty)
    - Assert `IsResourcesOnlyImport` returns `true` iff all three dedup fields are missing AND resources are non-empty
    - Use FsCheck.NUnit `[Property(MaxTest = 100)]` following the pattern in `IndividualImportDedupPropertyTests.cs`
    - **Validates: Requirements 1.1, 1.2, 1.3**

  - [x] 1.4 Write property test for MergeResourcesOnly
    - **Property 2: MergeResourcesOnly replaces resources and preserves all other fields**
    - Add to `OE2EmpireTracker.Tests/Services/ResourcesOnlyImportPropertyTests.cs`
    - Generate random target Blueprint (with UUID, all scalar fields, Properties including protected keys) and random incoming Blueprint (with Resources and Properties)
    - Snapshot target's scalar fields and protected properties before calling `MergeResourcesOnly`
    - Assert: resources replaced, scalars unchanged, protected properties preserved, non-protected incoming properties present
    - **Validates: Requirements 2.1, 2.2, 3.1, 3.2, 3.3**

- [x] 2. Checkpoint — Verify IsResourcesOnlyImport and MergeResourcesOnly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Modify cmdImport_Click to handle resources-only imports
  - [x] 3.1 Add resources-only branch to `cmdImport_Click` in `FormBlueprint.cs`
    - Insert a new branch after `ParseClipboardToTemp()` succeeds and before the existing "Fallback: if no name was parsed" block
    - Call `MarketBlueprintImporter.IsResourcesOnlyImport(tempBP)` to detect resources-only clipboard data
    - If resources-only AND `viewModel.Data.UUID` is null/empty, show `MessageBox` with "Please select or import a blueprint first, then import the resources tab." and return
    - If resources-only AND selected blueprint exists, call `MergeResourcesOnly(viewModel.Data, tempBP)`
    - Determine which list contains the selected blueprint (global or player) and persist accordingly
    - Raise `BlueprintDataChanged`, refresh the list view, re-select the blueprint, and call `PopulateForm()`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3_

  - [x] 3.2 Write unit tests for resources-only import edge cases
    - Add `OE2EmpireTracker.Tests/Services/ResourcesOnlyImportTests.cs` with `Compile Include` in test csproj
    - Test: `IsResourcesOnlyImport` returns `false` when Resources is empty even though dedup fields are missing (Req 1.3 edge case)
    - Test: `IsResourcesOnlyImport` returns `false` when Resources has entries but BluePrintType is non-empty (Req 1.2 edge case)
    - Test: `MergeResourcesOnly` preserves protected properties when incoming has different values for them
    - Test: `MergeResourcesOnly` replaces resources completely (no leftover keys from target)
    - _Requirements: 1.2, 1.3, 3.1, 3.2, 3.3_

- [x] 4. Checkpoint — Verify resources-only import flow
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Assign Name properties to filter combo boxes (BL-066)
  - [x] 5.1 Add `Name` property assignments in `InitFilterPanel` in `FormBlueprint.cs`
    - Set `Name = "cmbFilterType"` on the Type filter ComboBox
    - Set `Name = "cmbFilterClass"` on the Class filter ComboBox
    - Set `Name = "cmbFilterTechLevel"` on the Tech Level filter ComboBox
    - Set `Name = "cmbFilterEvolution"` on the Evolution filter ComboBox
    - Set `Name = "chkEvolutionAndAbove"` on the "And Above" CheckBox
    - No changes to `WindowStateHelper` needed — it already saves/restores ComboBox and CheckBox controls by name
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 6.1, 6.2, 6.3, 6.4, 6.5_

  - [-] 5.2 Write property test for WindowStateHelper ComboBox/CheckBox save-restore round trip
    - **Property 3: WindowStateHelper ComboBox/CheckBox save-restore round trip**
    - Create `OE2EmpireTracker.Tests/Persistence/WindowStateHelperPropertyTests.cs` with `Compile Include` in test csproj
    - Generate a `Form` containing a `ComboBox` with a random `Name`, random string items, and a random valid `SelectedIndex`, plus a `CheckBox` with a random `Name` and random `Checked` state
    - Call `SaveControlStates` then `RestoreControlStates` on a fresh `FormControlState`
    - Assert the ComboBox's `SelectedIndex` and CheckBox's `Checked` match the originals
    - Test edge case: saved value no longer in items list → ComboBox stays at default
    - Use FsCheck.NUnit `[Property(MaxTest = 100)]`
    - **Validates: Requirements 5.1, 5.2, 6.1, 6.2, 6.3, 6.4**

- [~] 6. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- New `.cs` files require `Compile Include` entries in the old-style `.csproj`
- Do NOT use `semanticRename` — manual find-and-replace only
- Build with MSBuild, test with vstest.console against the built test DLL
- Property tests use FsCheck.NUnit following existing patterns in `IndividualImportDedupPropertyTests.cs`
