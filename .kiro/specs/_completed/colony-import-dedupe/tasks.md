# Implementation Plan: Colony Import Dedup

## Overview

Implement colony clipboard import dedup by parsing into a temporary colony first, searching by name, then merging or creating. Add duplicate-name validation on the colony name text field. Extract testable logic into a static `ColonyImportHelper` class. All property tests use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`.

## Tasks

- [x] 1. Create ColonyImportHelper static class
  - [x] 1.1 Create `OE2EmpireTracker/Services/ColonyImportHelper.cs` with `FindByName`, `MergeIdentity`, `CreateFromTemp`, and `IsDuplicateName` methods
    - `FindByName(IEnumerable<Colony>, string)` — case-insensitive search by ColonyName, returns matching colony or null
    - `MergeIdentity(Colony target, Colony source)` — copies PlanetName and SystemName from source to target
    - `CreateFromTemp(Colony tempColony, string ownerUUID)` — assigns new UUID, sets OwnerUUID, copies all parsed data (Structures, Commodities, ColonyName, PlanetName, SystemName)
    - `IsDuplicateName(IEnumerable<Colony>, string name, string excludeUUID)` — returns true if any colony (excluding excludeUUID) has a case-insensitive ColonyName match
    - Add `<Compile Include="Services\ColonyImportHelper.cs" />` to `OE2EmpireTracker.csproj`
    - _Requirements: 2.1, 2.3, 3.3, 3.4, 4.1, 4.2, 4.3, 8.1_

  - [x] 1.2 Write property test: Case-insensitive colony name search (Property 1)
    - **Property 1: Case-insensitive colony name search**
    - **Validates: Requirements 2.1**
    - Create `OE2EmpireTracker.Tests/Services/ColonyImportHelperPropertyTests.cs`
    - Add `<Compile Include="Services\ColonyImportHelperPropertyTests.cs" />` to test `.csproj`
    - Generate random colony lists and search strings (including case-shuffled versions of existing names)
    - Assert `FindByName` returns correct result for both match and no-match cases

  - [x] 1.3 Write property test: No-match import grows colony list by one (Property 2)
    - **Property 2: No-match import grows colony list by one**
    - **Validates: Requirements 2.3, 4.4**
    - Generate random colony lists and a temp colony with a name not in the list
    - Assert list count increases by exactly one after `CreateFromTemp` + add

  - [x] 1.4 Write property test: MergeIdentity updates identity while preserving local state (Property 3)
    - **Property 3: MergeIdentity updates identity while preserving local state**
    - **Validates: Requirements 3.3, 3.4**
    - Generate random existing colony (with structures, items, UUID) and random temp colony
    - Assert identity fields updated, UUID/OwnerUUID/Items/Structures references unchanged

  - [x] 1.5 Write property test: CreateFromTemp produces a valid colony with all parsed data (Property 4)
    - **Property 4: CreateFromTemp produces a valid colony with all parsed data**
    - **Validates: Requirements 4.1, 4.2, 4.3**
    - Generate random temp colony and random owner UUID
    - Assert UUID non-empty, OwnerUUID correct, all data fields match temp colony

  - [x] 1.6 Write property test: Duplicate name detection is case-insensitive and excludes self (Property 5)
    - **Property 5: Duplicate name detection is case-insensitive and excludes self**
    - **Validates: Requirements 8.1**
    - Generate random colony lists, names, and exclude UUIDs
    - Assert `IsDuplicateName` returns true iff a non-excluded colony has a case-insensitive name match

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Add ColonyParser.ParseClipboardToTemp method
  - [x] 3.1 Add `ParseClipboardToTemp(EmpireContext empireContext)` method to `OE2EmpireTracker/Parsers/ColonyParser.cs`
    - Create a new `Colony()` instance
    - Read HTML from clipboard (same as existing `ProcessClipboard`)
    - Call `ProcessHtml(tempColony, html, empireContext)` on the new instance
    - Return the populated temporary colony, or null if no HTML on clipboard
    - _Requirements: 1.1, 1.2_

- [x] 4. Rewrite FormColony.cmdImportColony_Click
  - [x] 4.1 Rewrite `cmdImportColony_Click` in `OE2EmpireTracker/Forms/Colony/FormColony.cs`
    - Guard: no HTML on clipboard → show MessageBox, return
    - Guard: no current player → show MessageBox, return
    - Call `parser.ParseClipboardToTemp(empireContext)` to get `tempColony`
    - If `tempColony.ColonyName` is empty → fall back to current behavior (parse into `selectedColony`)
    - Otherwise call `ColonyImportHelper.FindByName` on current player colonies
    - If match found → call `ColonyImportHelper.MergeIdentity`, then re-run `parser.ProcessHtml(existingColony, html, empireContext)` for structure/commodity merge
    - If no match → call `ColonyImportHelper.CreateFromTemp`, add to `playerContext.colonyList`
    - Persist via `playerContext.writeContext()`
    - Fire `playerContext.OnColonyDataChanged(colony.UUID)`
    - Refresh list view, select imported colony, call `PopulateForm()`
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 6.1, 6.2, 7.1, 7.2, 7.3_

- [x] 5. Add duplicate name validation on txtColonyName
  - [x] 5.1 Extend `txtColonyName_TextChanged` in `OE2EmpireTracker/Forms/Colony/FormColony.cs`
    - Skip if `_isProgrammaticUpdate > 0`
    - Call `ColonyImportHelper.IsDuplicateName` with current player colonies, entered name, and `selectedColony.UUID`
    - If duplicate → `txtColonyName.SetError("Colony name already in use")` and `cmdSave.Enabled = false`
    - If no duplicate → `txtColonyName.ClearError()` and `cmdSave.Enabled = true`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 6. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- New `.cs` files require `<Compile Include="...">` entries in the old-style `.csproj`
- Build with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Run tests with vstest.console: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll`
