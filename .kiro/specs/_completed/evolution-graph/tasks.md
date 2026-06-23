# Implementation Plan: Evolution Graph

## Overview

Add an "Evolution Graph" tab to FormBlueprint that visualizes how blueprint numeric properties change across evolution levels. Implementation follows the layered architecture: a pure-logic `EvolutionChainService` for chain resolution and data preparation, then UI integration in `FormBlueprint` with a `Chart` control and property checkboxes. The Chart and tab are created in code-behind (not the Designer) per the design.

## Tasks

- [x] 1. Set up project infrastructure for Evolution Graph
  - [x] 1.1 Add `System.Windows.Forms.DataVisualization` GAC reference to `OE2EmpireTracker.csproj`
    - Add a `<Reference Include="System.Windows.Forms.DataVisualization" />` entry in the `<ItemGroup>` containing other `<Reference>` elements
    - This assembly is in the GAC — no NuGet package needed
    - _Requirements: 7.1_

  - [x] 1.2 Create `EvolutionChainService.cs` with `ResolveChain` and `BuildGraphData` methods
    - Create file at `OE2EmpireTracker/Services/EvolutionChainService.cs`
    - Add `<Compile Include="Services\EvolutionChainService.cs" />` to `OE2EmpireTracker.csproj`
    - Implement `EvolutionChainService` as a static class with:
      - `ResolveChain(Blueprint start, Func<string, Blueprint> resolver)` — walks `baseBlueprintUUID` backward to Ev0, tracks visited UUIDs in a `HashSet<string>` to detect cycles, terminates on missing UUID, returns list sorted by `Evolution` ascending
      - `EvolutionGraphData` class with `Dictionary<string, List<(int Evolution, double Percent)>> Series` and `bool NoChanges`
      - `BuildGraphData(IReadOnlyList<Blueprint> chain, string[] blueprintTypeProperties)` — filters to numeric properties (Integer, Decimal, Time via `BlueprintPropertyValidation.GetPropertyType`), parses Time strings to total seconds, excludes properties with zero Ev0 value, excludes unchanged properties, normalizes as `(value / ev0Value) * 100`
    - Time parsing: extract d/h/m/s components from strings like `"1d 2h 30m 15s"` and convert to total seconds
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3_

  - [x] 1.3 Write property test: chain resolution produces complete, ordered ancestor list
    - Create file `OE2EmpireTracker.Tests/Services/EvolutionChainServiceTests.cs`
    - Add `<Compile Include="Services\EvolutionChainServiceTests.cs" />` to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - **Property 1: Chain resolution produces a complete, ordered ancestor list**
    - Generate random chains of 1–16 blueprints with valid `baseBlueprintUUID` links; verify output is sorted ascending by `Evolution` and contains all chain members including the start blueprint
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 1.1, 1.2**

  - [x] 1.4 Write property test: graph data contains only numeric properties from the BlueprintType
    - Add to `EvolutionChainServiceTests.cs`
    - **Property 2: Graph data contains only numeric properties from the BlueprintType**
    - Generate random `BlueprintType.Properties` arrays mixing numeric and non-numeric types; build graph data and verify all output keys are numeric and in the source array
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 2.1, 2.2, 2.3**

  - [x] 1.5 Write property test: unchanged properties are excluded and NoChanges flag is correct
    - Add to `EvolutionChainServiceTests.cs`
    - **Property 3: Unchanged properties are excluded and NoChanges flag is correct**
    - Generate chains where some/all numeric properties are identical; verify unchanged properties are excluded and `NoChanges` flag is correct
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 2.4, 2.5**

  - [x] 1.6 Write property test: percentage normalization formula
    - Add to `EvolutionChainServiceTests.cs`
    - **Property 4: Percentage normalization formula**
    - Generate chains with random numeric property values; verify each percentage equals `(value / ev0Value) * 100` and Ev0 is always 100%
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 3.1, 3.3**

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Implement Evolution Graph tab UI in FormBlueprint
  - [x] 3.1 Add `InitEvolutionGraphTab()` method to FormBlueprint
    - Add private fields: `TabPage tabPEvolutionGraph`, `Chart chartEvolution`, `FlowLayoutPanel pnlPropertyCheckboxes`, `Label lblNoChanges`
    - Add `using System.Windows.Forms.DataVisualization.Charting;` to FormBlueprint
    - Create `InitEvolutionGraphTab()` called from the constructor (after `InitializeComponent()` and existing setup):
      - Create `tabPEvolutionGraph` with `Text = "Evolution Graph"` and add it to `tabDetailedData.TabPages` (after existing tabs)
      - Create `chartEvolution` with `Dock = DockStyle.Fill`; configure chart area: X-axis range 0–15, title "Evolution Level", interval 1; Y-axis range 50–150, title "% Change from Evolution 0 Value", gridline interval 10%
      - Create `pnlPropertyCheckboxes` as a `FlowLayoutPanel` with `Dock = DockStyle.Right`, `AutoScroll = true`, `FlowDirection = TopDown`, `WrapContents = false`
      - Create `lblNoChanges` with centered text "No property changes found across the evolution chain.", initially hidden
      - Add controls to `tabPEvolutionGraph`: checkbox panel, chart, label
    - Define the 16-color extended Wong palette as a `static readonly Color[]` array
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 7.1, 7.2, 7.3_

  - [x] 3.2 Add `RefreshEvolutionGraph()` and `ClearEvolutionGraph()` methods
    - `RefreshEvolutionGraph()`:
      - Resolve chain via `EvolutionChainService.ResolveChain(viewModel.Data, uuid => playerContext.FindBlueprint(uuid) ?? EmpireContext.getInstance()?.FindGlobalBlueprint(uuid))`
      - Look up `BlueprintType` from `empireContext` to get `Properties` array
      - Call `EvolutionChainService.BuildGraphData(chain, blueprintTypeProperties)`
      - If `NoChanges`, show `lblNoChanges`, hide chart and checkbox panel; return
      - Otherwise hide `lblNoChanges`, show chart and checkbox panel
      - Clear existing chart series and checkbox panel controls
      - For each property in `Series`: assign color from Wong palette, create chart series segments (solid for consecutive evolution levels, dashed for gaps), add checkbox with label colored to match
      - All checkboxes checked by default; wire `CheckedChanged` to show/hide corresponding series
    - `ClearEvolutionGraph()`: clear chart series, clear checkbox panel, hide `lblNoChanges`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 6.4, 6.5, 8.3, 8.4_

  - [x] 3.3 Wire refresh calls into existing FormBlueprint event handlers
    - In `lvwBlueprints_ItemSelectionChanged`: call `RefreshEvolutionGraph()` after `PopulateForm()` when a blueprint is selected
    - In `OnBlueprintDataChanged`: call `RefreshEvolutionGraph()` when the changed UUID is in the current chain (or always refresh if the evolution tab is visible)
    - In `ClearForm()`: call `ClearEvolutionGraph()`
    - When no blueprint is selected (else branch in selection changed): call `ClearEvolutionGraph()`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 3.4 Write property test: segment dash style matches evolution gap classification
    - Create file `OE2EmpireTracker.Tests/Forms/EvolutionGraphRenderingTests.cs`
    - Add `<Compile Include="Forms\EvolutionGraphRenderingTests.cs" />` to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - **Property 5: Segment dash style matches evolution gap classification**
    - Generate chains with gaps (non-consecutive evolution levels); build segments and verify solid/dashed classification matches gap detection (consecutive = solid, gap > 1 = dashed)
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 5.1, 5.2**

  - [x] 3.5 Write property test: distinct color assignment per property
    - Add to `EvolutionGraphRenderingTests.cs`
    - **Property 6: Distinct color assignment per property**
    - Generate random property name lists of size 1–16; verify all assigned colors are distinct and from the extended Wong palette
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 5.3**

- [x] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Integration wiring and final validation
  - [x] 5.1 Add `<Compile Include>` entries for all new test files in test csproj
    - Verify `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj` has `<Compile Include>` entries for:
      - `Services\EvolutionChainServiceTests.cs`
      - `Forms\EvolutionGraphRenderingTests.cs`
    - These should already be added in earlier tasks, but verify and fix if missing
    - _Requirements: all_

  - [x] 5.2 Write unit tests for edge cases
    - Add unit tests (NUnit `[Test]`) to `EvolutionChainServiceTests.cs` covering:
      - Ev0 blueprint with no base (chain of 1) — returns single-element list
      - Broken chain (missing UUID mid-chain) — terminates at last resolved blueprint
      - Circular reference detection — stops when visited UUID is re-encountered
      - Zero base value exclusion — property with Ev0 value of 0 is excluded from graph data
      - Time property parsing — `"1d 2h 30m 15s"` → 93615 seconds
      - All properties unchanged — `NoChanges` is true, `Series` is empty
    - _Requirements: 1.3, 1.4, 1.5, 2.4, 2.5, 3.2_

- [x] 6. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The Chart control and tab page are created in code-behind, not in the Designer — do not edit `FormBlueprint.Designer.cs`
- `System.Windows.Forms.DataVisualization.Charting` is in the GAC; only a csproj reference is needed
- FsCheck 2.16.6 with FsCheck.NUnit is already referenced in the test project
- New `.cs` files require `<Compile Include>` entries in the old-style csproj files
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
- Checkpoints ensure incremental validation
