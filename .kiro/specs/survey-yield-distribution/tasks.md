# Tasks

## Task 1: Create Models

- [x] 1.1 Create `ResourcePurityCombo.cs`
- [x] 1.2 Create `DistributionPoint.cs`
- [x] 1.3 Create `YieldDistributionResult.cs`

## Task 2: Create YieldDistributionService

- [x] 2.1 Create `YieldDistributionService.cs`
  - [x] 2.1.1 Implement `ComputeDistribution` method with bin alignment, yield extraction, and percentage calculation
  - [x] 2.1.2 Implement `GetAvailableCombos` method for discovering resource+purity pairs

## Task 3: Create ChartColors shared constant

- [x] 3.1 Create `Constants/ChartColors.cs` with WongPalette array
- [x] 3.2 Update `FormBlueprintV2.cs` to reference `ChartColors.WongPalette` instead of local field

## Task 4: Property-Based Tests for YieldDistributionService

- [ ] 4.1 Create `YieldDistributionServiceTests.cs` with FsCheck generators for surveys
  - [~] 4.1.1 Property 1: Type Filtering Correctness — filtered surveys only contain the specified SurveyType
  - [~] 4.1.2 Property 2: Resource+Purity Extraction Accuracy — MatchingSurveyCount equals expected count
  - [~] 4.1.3 Property 3: Percentage Sum Invariant — sum of all bin percentages equals 100% (±0.01%)
  - [~] 4.1.4 Property 4: Bin Midpoint Correctness — midpoint equals lower + binWidth/2
  - [~] 4.1.5 Property 5: Bin Coverage Completeness — every yield maps to exactly one bin
  - [~] 4.1.6 Property 6: Bin Width Clamping — effective width always in [1, 100]
  - [~] 4.1.7 Property 7: Insufficient Data Detection — InsufficientData true and Points empty when < 2 matches
  - [~] 4.1.8 Property 8: Axis Range Coverage — all yields fall within bin range
  - [~] 4.1.9 Property 9: GetAvailableCombos Completeness — returns exact distinct set of resource+purity pairs
- [ ] 4.2 Example-based edge-case tests
  - [~] 4.2.1 Empty survey list returns empty result
  - [~] 4.2.2 Single survey returns InsufficientData
  - [~] 4.2.3 All yields identical produces single bin at 100%
  - [~] 4.2.4 Non-numeric Amount values are skipped gracefully
  - [~] 4.2.5 Yields at bin boundaries assigned correctly

## Task 5: UI — Add TabControl and Yield Distribution tab to FormSurvey

- [ ] 5.1 Add TabControl to FormSurvey.Designer.cs wrapping flpSurveyData
  - [~] 5.1.1 Create tabSurveyContent TabControl
  - [~] 5.1.2 Move existing flpSurveyData into "Survey Details" tab
  - [~] 5.1.3 Create "Yield Distribution" tab with chart and controls
- [ ] 5.2 Add distribution tab controls in Designer.cs
  - [~] 5.2.1 pnlDistControls FlowLayoutPanel with resource/purity combos and buttons
  - [~] 5.2.2 lstDistSeries ListBox for series legend
  - [~] 5.2.3 nudBinWidth NumericUpDown (min=1, max=100, default=10)
  - [~] 5.2.4 chartDistribution Chart control with Spline ChartArea
  - [~] 5.2.5 lblDistMessage Label for empty state messages

## Task 6: UI — FormSurvey distribution logic and event wiring

- [ ] 6.1 Implement RefreshDistributionGraph method
  - [~] 6.1.1 Empty state checks (no survey type, no surveys, no series)
  - [~] 6.1.2 Iterate active series, call service, add Chart Series
  - [~] 6.1.3 X-axis label conditional on SurveyType (Yield vs Yield/cycle)
  - [~] 6.1.4 PERF timing with Stopwatch
- [ ] 6.2 Implement series management
  - [~] 6.2.1 PopulateDistResourceCombo — populate from GetAvailableCombos
  - [~] 6.2.2 PopulateDistPurityCombo — filter purities by selected resource
  - [~] 6.2.3 BtnAddSeries_Click — add combo to _activeSeries, assign color, refresh
  - [~] 6.2.4 BtnRemoveSeries_Click — remove selected from _activeSeries, refresh
- [ ] 6.3 Wire existing filter events to also call RefreshDistributionGraph
  - [~] 6.3.1 cmbSurveyType, txtSurveyFilter, cmbResource, cmbPurityFilter, txtMinAmount
- [ ] 6.4 Wire new distribution control events
  - [~] 6.4.1 nudBinWidth.ValueChanged → RefreshDistributionGraph
  - [~] 6.4.2 cmbDistResource.SelectedIndexChanged → PopulateDistPurityCombo

## Task 7: Build verification and commit

- [~] 7.1 Build solution with zero errors and zero warnings
- [~] 7.2 Run all tests and verify pass
- [~] 7.3 Run audit (node .kiro/tools/audit.js)
- [~] 7.4 Commit all changes
