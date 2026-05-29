# Implementation Plan: Colony Admin Warehouse Utilization

## Overview

This plan implements warehouse volume display, corrected underutilized refining detection, resource depletion ETAs, and overflow rule trigger predictions. Tasks are ordered to build shared infrastructure first (rate calculator, model changes), then service logic, then UI wiring. Each task is sized to ≤5 files, ≤200 new lines, and ≤3 acceptance criteria.

## Tasks

- [x] 1. Add ThresholdPreferences properties and ActivityType enum value
  - [x] 1.1 Add OverflowPrediction to ActivityType enum
    - Add `OverflowPrediction` value to the `ActivityType` enum in `OE2EmpireTracker.Common/Services/ActivityType.cs`
    - _Requirements: 5.8_
    - _Inputs: ActivityType.cs_
    - _Output: ActivityType.cs modified_
    - _Verification: getDiagnostics clean compile_

  - [x] 1.2 Add two new ThresholdPreferences properties with validation
    - Add `OverflowPredictionHorizonHours` (int, default 48, range 1-336) to ThresholdPreferences
    - Add `UnderutilizedRefiningStockpileHours` (int, default 24, range 1-168) to ThresholdPreferences
    - Add validation logic in `ThresholdPreferences.Validate()`
    - _Requirements: 6.2, 6.3, 7.2, 7.3_
    - _Inputs: UIPreferences.cs_
    - _Output: UIPreferences.cs modified_
    - _Verification: getDiagnostics clean compile_


- [x] 2. Create ColonyResourceRateCalculator static utility class
  - [x] 2.1 Implement ComputeWarehouseVolume and GetNetWarehouseVolumeGrowthRate
    - Create new file `OE2EmpireTracker.Common/Services/ColonyResourceRateCalculator.cs`
    - Implement `ComputeWarehouseVolume(Colony)` using volume constants (Resources=1, Commodities=10, Workers=50, Blueprints/Surveys=0)
    - Implement `GetNetWarehouseVolumeGrowthRate(Colony, PlayerContext)` summing net rates across all resource+purity combos
    - _Requirements: 1.4, 5.3_
    - _Inputs: design.md (volume constants table), GameConstants.cs, Colony.cs, ItemBag.cs_
    - _Output: ColonyResourceRateCalculator.cs (new)_
    - _Verification: getDiagnostics clean compile_

  - [x] 2.2 Implement GetTotalMiningRate, GetTotalRefiningConsumption, and GetNetHourlyRate
    - Add `GetTotalMiningRate(Colony, PlayerContext, string resource, string purity)` — sum mining output across active miners
    - Add `GetTotalRefiningConsumption(Colony, PlayerContext, string resource, string purity)` — sum refining consumption across active refiners
    - Add `GetNetHourlyRate(Colony, PlayerContext, string resource, string purity)` — mining minus refining
    - Extract logic from existing duplication in ColonyInactivityCollector/ColonyAdminReportBuilder
    - _Requirements: 3.2, 3.8, 5.2_
    - _Inputs: ColonyInactivityCollector.cs, ColonyAdminReportBuilder.cs (existing rate logic), PlayerContext.cs_
    - _Output: ColonyResourceRateCalculator.cs modified_
    - _Verification: getDiagnostics clean compile_


  - [x] 2.3 Write unit tests for ColonyResourceRateCalculator
    - Test ComputeWarehouseVolume with mixed item types (resources, commodities, workers, blueprints)
    - Test GetNetHourlyRate with known colony setups (multiple miners, multiple refiners, mixed purities)
    - Test GetTotalMiningRate and GetTotalRefiningConsumption with ExtractionFocus skill bonus
    - Test edge cases: empty colony, zero miners, zero refiners
    - **Property 1: Warehouse Volume Accuracy**
    - **Validates: Requirements 1.4, 1.5**
    - _Inputs: ColonyResourceRateCalculator.cs, test fixtures_
    - _Output: ColonyResourceRateCalculatorTests.cs (new)_
    - _Verification: vstest.console passes_

- [~] 3. Checkpoint - Verify rate calculator compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Implement stockpile-aware underutilization detection in ColonyInactivityCollector
  - [x] 4.1 Refactor underutilization check to use rate calculator and stockpile threshold
    - Replace per-refiner `stockpile >= consumeRate` check with per-group sustainability check
    - Compute `excessConsumption = totalGroupConsumption - totalGroupMiningOutput` using ColonyResourceRateCalculator
    - Compute `requiredStockpile = excessConsumption × UnderutilizedRefiningStockpileHours`
    - Exempt entire group when `stockpile >= requiredStockpile`
    - Read threshold from `PreferencesStore.GetInstance().Preferences.Thresholds.UnderutilizedRefiningStockpileHours`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
    - _Inputs: ColonyInactivityCollector.cs, ColonyResourceRateCalculator.cs, UIPreferences.cs_
    - _Output: ColonyInactivityCollector.cs modified_
    - _Verification: getDiagnostics clean compile_


  - [-] 4.2 Write unit tests for stockpile-aware underutilization
    - Test refining group exempt when stockpile sustains excess consumption for threshold hours
    - Test refining group flagged when stockpile insufficient
    - Test group not flagged when mining meets or exceeds consumption
    - **Property 3: Underutilization Threshold Correctness**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
    - _Inputs: ColonyInactivityCollector.cs, test fixtures_
    - _Output: ColonyInactivityCollectorTests.cs (new or modified)_
    - _Verification: vstest.console passes_

- [ ] 5. Implement resource depletion ETA in ColonyInactivityCollector
  - [x] 5.1 Add CollectDepletionETAs method to ColonyInactivityCollector
    - Implement `CollectDepletionETAs(Colony, PlayerContext, List<ActivityRow>)`
    - For each refining group where consumption > mining: compute `depletionHours = stockpile / (consumption - mining)`
    - Emit ActivityRow with Type=Refining, ProcessDetails showing formatted ETA
    - Handle edge cases: stockpile==0 → "Depleted", mining>=consumption → no row emitted
    - Wire into existing collection flow for inactivity mode
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
    - _Inputs: ColonyInactivityCollector.cs, ColonyResourceRateCalculator.cs, ActivityRow.cs_
    - _Output: ColonyInactivityCollector.cs modified_
    - _Verification: getDiagnostics clean compile_

  - [-] 5.2 Write unit tests for depletion ETA calculation
    - Test correct ETA when consumption exceeds mining with known stockpile
    - Test "Depleted" when stockpile is zero and consumption exceeds mining
    - Test no row emitted when mining meets or exceeds consumption ("Sustained")
    - **Property 2: Depletion ETA Consistency**
    - **Validates: Requirements 3.2, 3.3, 3.4**
    - _Inputs: ColonyInactivityCollector.cs, test fixtures_
    - _Output: ColonyInactivityCollectorTests.cs modified_
    - _Verification: vstest.console passes_


- [ ] 6. Implement overflow prediction in ColonyActivityCollector
  - [x] 6.1 Add CollectOverflowPredictions method to ColonyActivityCollector
    - Implement `CollectOverflowPredictions(Colony, PlayerContext, List<ActivityRow>)`
    - For SpecificResource rules: compute time until stockpile reaches threshold using net accumulation rate
    - For TotalWarehouse rules: compute time until total volume reaches threshold using net volume growth rate
    - Emit "triggered" row when threshold already exceeded
    - Emit prediction row when trigger time is within configured horizon
    - Skip when net rate is zero or negative
    - Read horizon from `PreferencesStore.GetInstance().Preferences.Thresholds.OverflowPredictionHorizonHours`
    - Set ActivityRow Type to `ActivityType.OverflowPrediction`
    - Only evaluate active rules (IsActive = true)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
    - _Inputs: ColonyActivityCollector.cs, ColonyResourceRateCalculator.cs, WarehouseOverflowRule.cs, OverflowRuleType.cs_
    - _Output: ColonyActivityCollector.cs modified_
    - _Verification: getDiagnostics clean compile_

  - [-] 6.2 Write unit tests for overflow prediction
    - Test SpecificResource rule: correct time calculation with known rate and threshold
    - Test TotalWarehouse rule: correct time calculation with known volume growth rate
    - Test "triggered" when threshold already exceeded
    - Test no row when net rate is zero or negative
    - Test horizon filtering (prediction beyond horizon not emitted)
    - **Property 4: Overflow Prediction Accuracy**
    - **Validates: Requirements 5.2, 5.3, 5.4, 5.5, 5.6**
    - _Inputs: ColonyActivityCollector.cs, test fixtures_
    - _Output: ColonyActivityCollectorTests.cs (new or modified)_
    - _Verification: vstest.console passes_


- [~] 7. Checkpoint - Verify all service-layer logic compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Add Warehouse section to ColonyAdminReportBuilder
  - [-] 8.1 Implement RenderWarehouseSection in ColonyAdminReportBuilder
    - Add `RenderWarehouseSection(RtfBuilder, Colony, PlayerContext, bool) → bool`
    - Display "{used} / {max}" volume using `ColonyResourceRateCalculator.ComputeWarehouseVolume`
    - Omit section entirely when WarehouseCapacity == 0
    - Display "0 / {max}" when colony has no items but has capacity
    - Include overflow predictions from active rules (triggered + predicted within horizon)
    - Format predicted triggers as "Overflow at {datetime}" using local time
    - Format already-triggered as "Overflow triggered -- {resource} ({purity})" or "Overflow triggered -- Total Warehouse"
    - Wire section call between Inactivity and Activity sections in report ordering
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 1.7, 1.8_
    - _Inputs: ColonyAdminReportBuilder.cs, ColonyResourceRateCalculator.cs, RtfBuilder.cs_
    - _Output: ColonyAdminReportBuilder.cs modified_
    - _Verification: getDiagnostics clean compile_

- [ ] 9. Add Resource Depletion section to ColonyAdminReportBuilder
  - [~] 9.1 Implement RenderResourceDepletionSection in ColonyAdminReportBuilder
    - Add `RenderResourceDepletionSection(RtfBuilder, Colony, PlayerContext, bool) → bool`
    - For each refining group: show depletion ETA using `ColonyResourceRateCalculator` rates
    - Display "Sustained" when mining meets or exceeds consumption
    - Display "Depleted" when stockpile is zero and consumption exceeds mining
    - Format ETA using same time formatting as other countdown displays (days, hours, minutes)
    - Wire section call after Refining section in report ordering
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_
    - _Inputs: ColonyAdminReportBuilder.cs, ColonyResourceRateCalculator.cs, RtfBuilder.cs_
    - _Output: ColonyAdminReportBuilder.cs modified_
    - _Verification: getDiagnostics clean compile_


- [ ] 10. Wire overflow checkbox into FormColonyActivity
  - [~] 10.1 Add chkOverflow checkbox to FormColonyActivity
    - Add `chkOverflow` checkbox to `flpFilters` in Designer.cs
    - Wire `chkOverflow.CheckedChanged += ChkFilter_CheckedChanged`
    - Set `chkOverflow.Visible = !inactivityMode` (visible only in active mode)
    - In `GetActiveTypes()`: include `ActivityType.OverflowPrediction` when `chkOverflow.Checked` and not in inactivity mode
    - Hide checkbox when switching to inactivity mode, show when switching to active mode
    - _Requirements: 5.9, 5.10_
    - _Inputs: FormColonyActivity.cs, FormColonyActivity.Designer.cs_
    - _Output: FormColonyActivity.cs modified, FormColonyActivity.Designer.cs modified_
    - _Verification: getDiagnostics clean compile_

- [ ] 11. Add preference fields to FormPreferences
  - [~] 11.1 Add nudOverflowHorizon and nudUnderutilizedStockpile to FormPreferences
    - Add `nudOverflowHorizon` NumericUpDown (Min=1, Max=336, Default=48) with label "Overflow prediction horizon hours"
    - Add `nudUnderutilizedStockpile` NumericUpDown (Min=1, Max=168, Default=24) with label "Underutilized refining stockpile hours"
    - Place both in the Thresholds section
    - Wire load/save to ThresholdPreferences properties
    - _Requirements: 6.1, 6.2, 6.3, 7.1, 7.2, 7.3_
    - _Inputs: FormPreferences.cs, FormPreferences.Designer.cs, UIPreferences.cs_
    - _Output: FormPreferences.cs modified, FormPreferences.Designer.cs modified_
    - _Verification: getDiagnostics clean compile_

- [~] 12. Checkpoint - Verify full solution builds with zero warnings
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 13. Wire depletion ETA display in Colony Activity form
  - [~] 13.1 Ensure depletion ETA rows display when Refining checkbox is checked in inactivity mode
    - Verify ColonyInactivityCollector.CollectDepletionETAs is called during inactivity collection
    - Ensure ActivityRow Type=Refining rows from depletion are included when Refining filter is active
    - Verify "Depleted" rows are emitted for zero-stockpile groups
    - _Requirements: 4.1, 4.5, 4.6_
    - _Inputs: ColonyInactivityCollector.cs, FormColonyActivity.cs_
    - _Output: ColonyInactivityCollector.cs modified (if wiring needed)_
    - _Verification: getDiagnostics clean compile_

- [ ] 14. Verify preference live-reload behavior
  - [~] 14.1 Confirm preferences are read fresh on each collector/builder invocation
    - Verify ColonyInactivityCollector reads `UnderutilizedRefiningStockpileHours` fresh each call (no caching)
    - Verify ColonyActivityCollector reads `OverflowPredictionHorizonHours` fresh each call (no caching)
    - Verify ColonyAdminReportBuilder reads both preferences fresh each call
    - No application restart required for preference changes to take effect
    - _Requirements: 6.4, 7.4_
    - _Inputs: ColonyInactivityCollector.cs, ColonyActivityCollector.cs, ColonyAdminReportBuilder.cs_
    - _Output: No changes if pattern already followed; fix if caching detected_
    - _Verification: getDiagnostics clean compile, code review confirms no caching_

- [~] 15. Final checkpoint - Full build, all tests pass, audit clean
  - Ensure all tests pass, ask the user if questions arise.
  - Run `node .kiro/tools/audit.js` and verify no new findings.
  - Build solution with zero warnings.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The ColonyResourceRateCalculator is built first because it's consumed by tasks 4, 5, 6, 8, and 9
- Model changes (task 1) have no consumer dependencies and can be done in parallel with the rate calculator
- UI tasks (10, 11) are independent of each other and depend only on the service layer being complete
- Task 13 ensures the wiring between ColonyInactivityCollector depletion rows and the Colony Activity form filter works end-to-end
- Task 14 is a verification task confirming the existing preference-reading pattern is followed (no restart needed)


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "2.1"] },
    { "id": 1, "tasks": ["2.2"] },
    { "id": 2, "tasks": ["2.3", "4.1", "5.1", "6.1"] },
    { "id": 3, "tasks": ["4.2", "5.2", "6.2", "8.1"] },
    { "id": 4, "tasks": ["9.1", "10.1", "11.1"] },
    { "id": 5, "tasks": ["13.1", "14.1"] }
  ]
}
```
