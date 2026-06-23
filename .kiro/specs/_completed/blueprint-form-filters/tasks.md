# Implementation Plan: Blueprint Form Filters

## Overview

Add a dynamic title bar with global/player blueprint counts and a structured filter panel (Blueprint Type, Class, Tech Level, Evolution) to FormBlueprint. Filtering logic is extended in BlueprintViewModel with a new `BlueprintFilterCriteria` class. All controls are created in code-behind. FsCheck property tests validate title bar formatting and combined AND filter semantics.

## Tasks

- [x] 1. Create BlueprintFilterCriteria class and extend BlueprintViewModel
  - [x] 1.1 Create `BlueprintFilterCriteria` class in `OE2EmpireTracker/ViewModels/BlueprintFilterCriteria.cs`
    - Define class with nullable fields: `BlueprintTypeId` (string), `ShipClassId` (int?), `TechLevelName` (string), `Evolution` (int?)
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 3.1, 4.1, 5.1, 6.1_

  - [x] 1.2 Add `GetFilteredBlueprints(string nameFilter, BlueprintFilterCriteria criteria)` overload to `BlueprintViewModel`
    - Apply each non-null criteria field as an additional `.Where()` predicate after existing text filter logic
    - Delegate existing single-parameter `GetFilteredBlueprints(string)` to the new overload with `criteria: null`
    - _Requirements: 3.2, 4.2, 5.2, 6.2, 7.1_

  - [x] 1.3 Write property test for combined filter AND semantics
    - **Property 2: Combined filter AND semantics**
    - Generate random blueprint lists, random text filters, and random `BlueprintFilterCriteria` (each field independently null or set)
    - Verify every result satisfies all active constraints AND every input blueprint satisfying all constraints appears in the result
    - Test file: `OE2EmpireTracker.Tests/ViewModels/BlueprintFilterPropertyTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 3.2, 4.2, 5.2, 6.2, 7.1**

- [x] 2. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Add title bar counts and filter panel UI to FormBlueprint
  - [x] 3.1 Add `UpdateTitleBarCounts()` method to `FormBlueprint`
    - Format: `"Blueprints - Global: {globalCount} Player: {playerCount}"`
    - Use `EmpireContext.globalBlueprintList?.Count ?? 0` for global count
    - Use `PlayerContext.GetCurrentPlayerBlueprints().Count` for player count (0 when no player selected)
    - Call from constructor, `OnBlueprintDataChanged`, and `OnCurrentPlayerChanged`
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 3.2 Write property test for title bar format correctness
    - **Property 1: Title bar format correctness**
    - Generate random non-negative integer pairs, call the title-bar formatting logic, assert output matches expected format
    - Test file: `OE2EmpireTracker.Tests/ViewModels/BlueprintFilterPropertyTests.cs` (same file as Property 2)
    - **Validates: Requirements 1.1, 1.4**

  - [x] 3.3 Add `InitFilterPanel()` method to `FormBlueprint` creating filter controls in code-behind
    - Create `FlowLayoutPanel` with four labeled ComboBoxes (Blueprint Type, Class, Tech Level, Evolution) and a Clear Filters button
    - Bind each ComboBox to its respective `EmpireContext` binding source (`bindingSourceBlueprintType`, `bindingSourceShipClass`, `bindingSourceTechLevel`, `bindingSourceEvolution`)
    - Insert the filter panel into `flpSearchList` between the text filter and the ListView
    - All ComboBoxes start with `SelectedIndex = -1` (no filter active)
    - Call `InitFilterPanel()` from the constructor after `InitializeComponent()`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 4.1, 5.1, 6.1_

  - [x] 3.4 Add `RefreshBlueprintList()` method and wire all filter events
    - Read filter state from all ComboBoxes, build `BlueprintFilterCriteria`, call `GetFilteredBlueprints(nameFilter, criteria)`, call `PopulateListView`
    - Wire each filter ComboBox `SelectedIndexChanged` → `RefreshBlueprintList()`
    - Wire `btnClearFilters.Click` → reset all four ComboBoxes to `SelectedIndex = -1`, then `RefreshBlueprintList()`
    - Replace `txtBlueprintListFilter.TextChanged` handler to call `RefreshBlueprintList()`
    - Update `OnBlueprintDataChanged` and `OnCurrentPlayerChanged` to call `RefreshBlueprintList()` and `UpdateTitleBarCounts()`
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 4. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The design uses C# throughout; no language selection was needed
- New `.cs` files require `Compile Include` entries in the old-style `.csproj` files
- All new controls are created in code-behind to avoid modifying the Designer file
- Property tests use FsCheck 2.16.6 with FsCheck.NUnit (`[FsCheck.NUnit.Property(MaxTest = 100)]`)
- Run tests via: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll`
