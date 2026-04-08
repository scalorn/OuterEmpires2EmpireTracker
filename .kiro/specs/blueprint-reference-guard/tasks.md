# Implementation Plan: Blueprint Reference Guard

## Overview

Add a `BlueprintReferenceCounter` service and `ReferenceReport` model to count how many entities reference a given blueprint UUID. Surface the count in FormBlueprint's ListView via a "Refs" column and disable the Delete button for in-use blueprints. Includes FsCheck property tests, NUnit unit tests, and spec/ documentation updates.

## Tasks

- [x] 1. Create ReferenceReport model and BlueprintReferenceCounter service
  - [x] 1.1 Create `OE2EmpireTracker/Models/ReferenceReport.cs` — immutable value object with per-source counts (FlatpackCount, ResearchingCount, ManufacturingCount, BaseBlueprintCount, ScannerCount) and computed TotalCount. Include `ReferenceReport.Empty` static field. Add `<Compile Include="Models\ReferenceReport.cs"/>` to `OE2EmpireTracker.csproj`.
    - _Requirements: 1.4, 1.5_

  - [x] 1.2 Create `OE2EmpireTracker/Services/BlueprintReferenceCounter.cs` — constructor accepts `IEnumerable<Colony>`, `IEnumerable<Blueprint>`, `IEnumerable<Survey>`. Single method `CountReferences(string blueprintUUID)` scans all five reference source fields, excludes self-references on baseBlueprintUUID, handles null/empty UUID by returning `ReferenceReport.Empty`, handles null collections and null Structures lists gracefully. Add `<Compile Include="Services\BlueprintReferenceCounter.cs"/>` to `OE2EmpireTracker.csproj`.
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 1.3 Write NUnit unit tests in `OE2EmpireTracker.Tests/Services/BlueprintReferenceCounterTests.cs` — cover: empty data returns zero counts, single reference per source type (Flatpack, Researching, Manufacturing, BaseBlueprintUUID, ScannerBlueprintUUID), multiple references across types, self-referencing blueprint excluded, null/empty UUID returns empty report, colony with null Structures handled. Add `<Compile Include="Services\BlueprintReferenceCounterTests.cs"/>` to `OE2EmpireTracker.Tests.csproj`.
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

- [x] 2. Write FsCheck property tests for BlueprintReferenceCounter
  - [x] 2.1 Write property test for counting accuracy across all source types in `OE2EmpireTracker.Tests/Services/BlueprintReferenceCounterPropertyTests.cs`. Add `<Compile Include="Services\BlueprintReferenceCounterPropertyTests.cs"/>` to `OE2EmpireTracker.Tests.csproj`.
    - **Property 1: Counting accuracy across all source types**
    - **Validates: Requirements 1.1, 1.2, 1.3**

  - [x] 2.2 Write property test for TotalCount sum invariant in the same file.
    - **Property 2: TotalCount is the sum of per-source counts**
    - **Validates: Requirements 1.4**

  - [x] 2.3 Write property test for self-referencing blueprint exclusion in the same file.
    - **Property 3: Self-referencing blueprints are excluded**
    - **Validates: Requirements 1.6**

  - [x] 2.4 Write property test for delete button state mapping in the same file.
    - **Property 4: Delete button state is determined by reference count**
    - **Validates: Requirements 2.2, 2.3**

- [x] 3. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Integrate reference counting into FormBlueprint UI
  - [x] 4.1 Extract a static method `GetDeleteButtonState(ReferenceReport report)` returning `(bool enabled, string text)` — either on `FormBlueprint` or a small helper class. If `report` is null or `TotalCount == 0`: enabled=true, text="Delete". If `TotalCount > 0`: enabled=false, text="In Use ({TotalCount})". When no blueprint is selected (null report): enabled=false, text="Delete".
    - _Requirements: 2.2, 2.3, 2.4_

  - [x] 4.2 Add a "Refs" column to the ListView in `FormBlueprint.Designer.cs`. Update `PopulateListView` in `FormBlueprint.cs` to instantiate a `BlueprintReferenceCounter` (from current `PlayerContext`/`EmpireContext` data) and populate the Refs column with each blueprint's `TotalCount`.
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 4.3 Update `lvwBlueprints_ItemSelectionChanged` in `FormBlueprint.cs` to compute the `ReferenceReport` for the selected blueprint and call `GetDeleteButtonState` to set `cmdDelete.Enabled` and `cmdDelete.Text`. When no item is selected, disable the button with text "Delete".
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 4.4 Guard the existing `cmdDelete_Click` handler — if `cmdDelete.Enabled` is false, return early (defense in depth). The existing confirmation dialog remains unchanged.
    - _Requirements: 2.5_

  - [x] 4.5 Write NUnit unit tests for `GetDeleteButtonState` — test zero-count report returns enabled/"Delete", positive-count report returns disabled/"In Use (N)", null report returns disabled/"Delete".
    - _Requirements: 2.2, 2.3, 2.4_

- [x] 5. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Update spec/ requirements documentation
  - [x] 6.1 Update `spec/requirements/DataModel.md` — add a new "ReferenceReport" section documenting the ReferenceReport model (immutable value object, per-source counts, TotalCount computed property, ReferenceReport.Empty static field).
    - _Requirements: 1.4, 1.5_

  - [x] 6.2 Update `spec/requirements/Architecture.md` — add a new requirement documenting the `BlueprintReferenceCounter` service (stateless, constructor-injected data collections, scans five reference source fields, excludes self-references, returns ReferenceReport). Also document the delete button state logic and Refs column in FormBlueprint.
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2_

- [x] 7. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New `.cs` files require a `<Compile Include="..."/>` entry in the corresponding `.csproj`
- Use `getDiagnostics` for compile checks; use `vstest.console` for test execution (not `dotnet test`)
