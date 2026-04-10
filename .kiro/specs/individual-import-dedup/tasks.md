# Implementation Plan: Individual Import Dedup

## Overview

Implement individual blueprint clipboard import dedup by parsing into a temporary blueprint first, checking the selected blueprint's dedup key, then routing via the existing `MarketBlueprintImporter` logic to update or create. Reuse `FindByDedupKey` and `UpdateExisting` by changing their visibility to `internal`. Extract a pure `IsGlobalRoute` static method for testable routing logic. All property tests use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`.

## Tasks

- [x] 1. Make FindByDedupKey and UpdateExisting internal on MarketBlueprintImporter
  - [x] 1.1 Change `FindByDedupKey` and `UpdateExisting` from `private` to `internal` in `OE2EmpireTracker/Services/MarketBlueprintImporter.cs`
    - No logic changes — only the access modifier changes
    - `FindByDedupKey(BindingList<Blueprint>, Blueprint)` returns matching blueprint or null
    - `UpdateExisting(Blueprint existing, Blueprint incoming)` overwrites data fields, preserves protected fields
    - _Requirements: 4.1, 4.2_

- [x] 2. Add IsGlobalRoute static method to MarketBlueprintImporter
  - [x] 2.1 Add `internal static bool IsGlobalRoute(int evolution, bool hasCurrentPlayer)` to `OE2EmpireTracker/Services/MarketBlueprintImporter.cs`
    - Returns `true` if evolution == 0 or no current player is selected
    - Returns `false` if evolution != 0 and a current player is selected
    - Pure function, no dependencies — directly testable
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 3. Write property tests for correctness properties
  - [x] 3.1 Create `OE2EmpireTracker.Tests/Services/IndividualImportDedupPropertyTests.cs` with test class scaffold
    - Add `<Compile Include="Services\IndividualImportDedupPropertyTests.cs" />` to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - _Requirements: 4.2, 2.1, 2.2, 3.1, 3.2, 3.3, 3.5, 3.6_

  - [ ]* 3.2 Write property test: UpdateExisting overwrites data while preserving protected fields (Property 1)
    - **Property 1: UpdateExisting overwrites data while preserving protected fields**
    - **Validates: Requirements 2.1, 2.2**
    - Generate random existing blueprint (with UUID, OwnerUUID, NickName, CopyCost, Properties including protected keys) and random incoming blueprint (with Properties and Resources)
    - Call `UpdateExisting`. Assert data fields overwritten, UUID/OwnerUUID/NickName/CopyCost preserved, protected property keys preserved

  - [ ]* 3.3 Write property test: Routing logic is determined by Evolution and player presence (Property 2)
    - **Property 2: Routing logic is determined by Evolution and player presence**
    - **Validates: Requirements 3.1, 3.2, 3.3**
    - Generate random Evolution values (int) and random player-selected booleans
    - Call `IsGlobalRoute`. Assert: evo 0 → true, evo != 0 + player → false, evo != 0 + no player → true

  - [ ]* 3.4 Write property test: FindByDedupKey returns the correct match or null (Property 3)
    - **Property 3: FindByDedupKey returns the correct match or null**
    - **Validates: Requirements 4.2**
    - Generate random `BindingList<Blueprint>` and a random search blueprint
    - Call `FindByDedupKey`. Assert: if result is non-null, all five key fields match exactly; if null, no blueprint in the list has all five fields matching

  - [ ]* 3.5 Write property test: Create path produces a valid blueprint with correct ownership (Property 4)
    - **Property 4: Create path produces a valid blueprint with correct ownership**
    - **Validates: Requirements 3.5, 3.6**
    - Generate random temporary blueprint and random owner UUID string
    - Simulate the create path (assign UUID, set OwnerUUID). Assert UUID is non-empty, OwnerUUID matches, all data fields match the temp blueprint

- [x] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Add ParseClipboardToTemp method to BlueprintScanner
  - [x] 5.1 Add `public Models.Blueprint ParseClipboardToTemp()` method to `OE2EmpireTracker/Forms/Blueprint/BlueprintScanner.cs`
    - Check `Clipboard.ContainsText(TextDataFormat.Html)` — return null if no HTML
    - Read HTML from clipboard (same as existing `processClipboard`)
    - Create a new `Blueprint()` instance
    - Call `ProcessHtml(tempBlueprint, html)` on it
    - Return the populated temporary blueprint
    - _Requirements: 1.1, 1.2_

- [x] 6. Rewrite cmdImport_Click in FormBlueprint
  - [x] 6.1 Rewrite `cmdImport_Click` in `OE2EmpireTracker/Forms/Blueprint/FormBlueprint.cs`
    - Guard: no HTML on clipboard → show MessageBox, return (existing behavior)
    - Call `scanner.ParseClipboardToTemp()` to get `tempBP`
    - Fallback guard: if `tempBP` is null or `tempBP.Name` is empty → fall back to current behavior (`scanner.processClipboard(viewModel.Data)` + `PopulateForm()`)
    - Check selected blueprint first: if `viewModel.Data` has a UUID and its dedup key matches `tempBP` → call `MarketBlueprintImporter.UpdateExisting(viewModel.Data, tempBP)`, determine persistence target
    - Route via market logic: call `MarketBlueprintImporter.IsGlobalRoute(tempBP.Evolution, hasCurrentPlayer)` to determine target list
    - Search target list: call `MarketBlueprintImporter.FindByDedupKey(targetList, tempBP)`
    - If match found → call `MarketBlueprintImporter.UpdateExisting(existing, tempBP)`
    - If no match → assign `tempBP.UUID = Guid.NewGuid().ToString()`, set `OwnerUUID` if player list, add to target list
    - Persist: call `empireContext.writeContext()` if global, `playerContext.writeContext()` if player
    - Notify: call `playerContext.OnBlueprintDataChanged(importedBP.UUID)`
    - Refresh UI: call `RefreshBlueprintList()`, select imported blueprint, `PopulateForm()`
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3_

- [x] 7. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- New `.cs` files require `<Compile Include="...">` entries in the old-style `.csproj`
- Build with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Run tests with vstest.console: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll`
