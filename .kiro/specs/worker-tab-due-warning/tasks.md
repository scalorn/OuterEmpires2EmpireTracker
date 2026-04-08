# Implementation Plan: Worker Tab Due Warning

## Overview

Add background color warnings to the Structures and Worker tab selectors in `FormColony`. A pure static `TabWarningService` evaluates colony state and returns a `TabWarningLevel` enum. The form applies the resulting color at each data-change point. FsCheck property-based tests and NUnit unit tests validate the service logic.

## Tasks

- [ ] 1. Add StructureCap constant and create TabWarningService
  - [x] 1.1 Add `StructureCap = 65` constant to `GameConstants.cs`
    - Add `public const int StructureCap = 65;` with XML doc comment to the Structure section
    - _Requirements: 7, 8 (informational constant for the game's structure cap)_

  - [x] 1.2 Create `TabWarningService.cs` with `TabWarningLevel` enum and both evaluation methods
    - Create `OE2EmpireTracker/Services/TabWarningService.cs`
    - Define `TabWarningLevel` enum (`None`, `Yellow`, `Red`)
    - Define `TabWarningService` static class with threshold constants
    - Implement `EvaluateStructureWarning(int structureCount)` — returns `Red` if >= 66, `Yellow` if >= 60, else `None`
    - Implement `EvaluateWorkerWarning(IEnumerable<CommodityRequested> commodities, DateTime now)` — filters unfulfilled requests with `NeedBy != DateTime.MinValue`, returns `Red` if any due <= 1 day or overdue, `Yellow` if any due <= 2 days, else `None`
    - Add the new file to `OE2EmpireTracker.csproj` `<Compile>` ItemGroup
    - _Requirements: 7.1, 7.2, 8.1, 8.2, 9.1, 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 4.1, 4.2, 6.1_

  - [x] 1.3 Write property test: Structure warning level is determined by count thresholds
    - **Property 1: Structure warning level is determined by count thresholds**
    - **Validates: Requirements 7.1, 7.2, 8.1, 8.2, 9.1**
    - Add FsCheck 2.16.6 and FsCheck.NUnit NuGet packages to `OE2EmpireTracker.Tests/packages.config`
    - Add FsCheck assembly references to `OE2EmpireTracker.Tests.csproj`
    - Create `OE2EmpireTracker.Tests/Services/TabWarningServiceTests.cs`
    - Add the test file to `OE2EmpireTracker.Tests.csproj` `<Compile>` ItemGroup
    - Implement `[FsCheck.NUnit.Property]` test generating random non-negative ints (0–200), asserting return matches threshold logic

  - [x] 1.4 Write property test: Worker warning level is the most urgent unfulfilled due window
    - **Property 2: Worker warning level is the most urgent unfulfilled due window**
    - **Validates: Requirements 1.1, 1.2, 2.1, 2.2, 3.1, 4.1, 6.1**
    - Implement `[FsCheck.NUnit.Property]` test generating random lists of `CommodityRequested` with random `Fulfilled` flags, random `NeedBy` dates (including `DateTime.MinValue`, past, near-future, far-future), and a random `now`. Compute expected result from minimum due window among unfulfilled non-MinValue requests, assert match.

  - [x] 1.5 Write property test: Fulfilled requests are excluded from worker warning evaluation
    - **Property 3: Fulfilled requests are excluded from worker warning evaluation**
    - **Validates: Requirements 1.3**
    - Implement `[FsCheck.NUnit.Property]` test generating random lists where every `CommodityRequested` has `Fulfilled = true` with arbitrary `NeedBy` dates. Assert service always returns `None`.

  - [x] 1.6 Write unit tests for TabWarningService edge cases
    - Add NUnit `[Test]` methods to `TabWarningServiceTests.cs` covering:
      - 0 structures → `None`, 59 → `None`, 60 → `Yellow`, 65 → `Yellow`, 66 → `Red`
      - No commodity requests → `None`
      - All fulfilled (even urgent dates) → `None`
      - Single unfulfilled due in 3 days → `None`
      - Single unfulfilled due in 1.5 days → `Yellow`
      - Single unfulfilled due in 12 hours → `Red`
      - Single unfulfilled overdue → `Red`
      - `NeedBy == DateTime.MinValue` → excluded
      - Mix of red and yellow conditions → `Red` wins
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 4.1, 4.2, 6.1, 7.1, 7.2, 8.1, 8.2, 9.1_

- [~] 2. Checkpoint - Verify TabWarningService compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 3. Integrate TabWarningService into FormColony
  - [~] 3.1 Add `ApplyTabWarning` and `UpdateTabWarnings` private helpers to `FormColony.cs`
    - Add `using OE2EmpireTracker.Services;` if not already present
    - Add `ApplyTabWarning(TabPage tab, TabWarningLevel level)` — sets `BackColor` / `UseVisualStyleBackColor` per the design (`LightCoral` for Red, `Yellow` for Yellow, `SystemColors.Control` + `UseVisualStyleBackColor = true` for None)
    - Add `UpdateTabWarnings()` — calls both `EvaluateStructureWarning` and `EvaluateWorkerWarning`, applies results to `tabPStructures` and `tabPWorkers`
    - _Requirements: 7.1, 7.2, 8.1, 8.2, 9.1, 1.1, 2.1, 3.1, 4.1_

  - [~] 3.2 Call `UpdateTabWarnings()` from existing event handlers in FormColony
    - Add `UpdateTabWarnings()` call at end of `PopulateForm()` (after structures and commodities are populated) — covers colony selection and import
    - Add `UpdateTabWarnings()` call at end of `cmdAddFlatpack_Click()` (after adding structure)
    - Add `UpdateTabWarnings()` call in `structures_ColonyStructureDataChanged()` (after recalculating status)
    - Add `UpdateTabWarnings()` call at end of `cmdAddCommodityRequest_Click()` (after adding request)
    - Add `UpdateTabWarnings()` call in `dgvCommodityRequests_CellValueChanged()` (after updating Fulfilled or NeedBy)
    - Add `UpdateTabWarnings()` call at end of `dgvCommodityRequests_KeyDown()` (after removing request)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 10.1, 10.2, 10.3_

- [~] 4. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The design uses C# throughout — no language selection needed
- FsCheck + FsCheck.NUnit packages must be added to `packages.config` and `.csproj` (old-style NuGet, not PackageReference)
- Build with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Test with vstest: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
- Do NOT use `dotnet test` — incompatible with this project
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
