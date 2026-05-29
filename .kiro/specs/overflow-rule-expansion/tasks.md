# Implementation Plan: Overflow Rule Expansion

## Overview

Expand the colony warehouse overflow rule system to support multiple rule types (SpecificResource, TotalWarehouse) with volume-based decimal thresholds. Implementation proceeds model-first, then validation, then evaluation logic, then UI, then tests.

## Tasks

- [x] 1. Define OverflowRuleType enum and update WarehouseOverflowRule model
  - [x] 1.1 Create OverflowRuleType enum
    - Create `OE2EmpireTracker.Common/Models/OverflowRuleType.cs`
    - Define `SpecificResource = 0` and `TotalWarehouse = 1`
    - No JsonConverter attribute on the enum itself (applied at property level)
    - _Satisfies: Req 1, Criteria 1.2, 1.3; Req 8, Criterion 8.3_
    - _Inputs: design.md (Section 1)_
    - _Output: OE2EmpireTracker.Common/Models/OverflowRuleType.cs_
    - _Verification: getDiagnostics on new file_

  - [x] 1.2 Add RuleType property and change TriggerThreshold to decimal on WarehouseOverflowRule
    - Add `RuleType` property with `[JsonConverter(typeof(StringEnumConverter))]` and default `SpecificResource`
    - Change `TriggerThreshold` from `int` to `decimal` with default `0m`
    - _Satisfies: Req 1, Criteria 1.1, 1.4; Req 2, Criterion 2.1; Req 5, Criteria 5.1, 5.2_
    - _Inputs: WarehouseOverflowRule.cs, design.md (Section 2)_
    - _Output: OE2EmpireTracker.Common/Models/WarehouseOverflowRule.cs_
    - _Verification: getDiagnostics, build solution_

  - [x] 1.3 Update ReadOnlyWarehouseOverflowRule to expose RuleType and decimal TriggerThreshold
    - Add `RuleType` getter property
    - Change `TriggerThreshold` return type from `int` to `decimal`
    - _Satisfies: Req 1, Criterion 1.1; Req 2, Criterion 2.1_
    - _Inputs: ReadOnlyWarehouseOverflowRule.cs, design.md (Section 3)_
    - _Output: OE2EmpireTracker.Common/Models/ReadOnlyWarehouseOverflowRule.cs_
    - _Verification: getDiagnostics, build solution_


- [x] 2. Fix compilation errors from TriggerThreshold type change
  - [x] 2.1 Update all consumers of TriggerThreshold from int to decimal
    - Search for all references to `TriggerThreshold` across the solution
    - Update any casts, comparisons, or assignments that assume `int`
    - Update any UI text parsing (e.g., `int.Parse` → `decimal.Parse`)
    - _Satisfies: Req 2, Criterion 2.1; Req 5, Criterion 5.2_
    - _Inputs: grep for TriggerThreshold across solution_
    - _Output: All files referencing TriggerThreshold (≤5 files)_
    - _Verification: Full solution build with zero errors_

- [x] 3. Implement OverflowRuleValidator
  - [x] 3.1 Create OverflowRuleValidator static class
    - Create `OE2EmpireTracker.Common/Services/OverflowRuleValidator.cs`
    - Implement `Validate(WarehouseOverflowRule)` returning `List<string>` errors
    - Validate: TriggerThreshold > 0, DestinationUUID non-empty, DeliveryRouteUUID non-empty
    - For SpecificResource: require ResourceName and ResourcePurity non-empty
    - For TotalWarehouse: skip ResourceName/ResourcePurity validation
    - _Satisfies: Req 7, Criteria 7.1–7.6; Req 3, Criterion 3.4; Req 4, Criterion 4.4_
    - _Inputs: design.md (Section 4), WarehouseOverflowRule.cs_
    - _Output: OE2EmpireTracker.Common/Services/OverflowRuleValidator.cs_
    - _Verification: getDiagnostics, build solution_

- [x] 4. Checkpoint - Ensure model and validator compile cleanly
  - Ensure all tests pass, ask the user if questions arise.


- [x] 5. Refactor BackgroundProcessor overflow evaluation with switch/dispatch
  - [x] 5.1 Refactor CheckWarehouseOverflow to use switch on RuleType
    - Replace existing evaluation logic with switch/dispatch pattern
    - Extract `EvaluateSpecificResourceRule` method (existing logic, now volume-based)
    - Add `default` case that logs warning and skips unknown RuleType
    - _Satisfies: Req 8, Criteria 8.1, 8.2; Req 3, Criterion 3.1_
    - _Inputs: BackgroundProcessor.cs (CheckWarehouseOverflow method), design.md (Section 5)_
    - _Output: OE2EmpireTracker.Common/Services/BackgroundProcessor.cs_
    - _Verification: getDiagnostics, build solution_

  - [x] 5.2 Implement EvaluateSpecificResourceRule with volume computation
    - Filter items by ResourceName and ResourcePurity
    - Compute volume as sum of (quantity × per-unit volume) using GameConstants
    - Compare against TriggerThreshold, log excess if overflow detected
    - _Satisfies: Req 2, Criteria 2.2, 2.3, 2.4; Req 3, Criteria 3.1, 3.2, 3.3_
    - _Inputs: BackgroundProcessor.cs, GameConstants.cs, design.md (Section 5)_
    - _Output: OE2EmpireTracker.Common/Services/BackgroundProcessor.cs_
    - _Verification: getDiagnostics, build solution_

  - [x] 5.3 Implement EvaluateTotalWarehouseRule with aggregate volume computation
    - Compute total volume of all items using per-type volume constants
    - Resources = 1, Commodities = 10, Workers = 50, Blueprints/Surveys = 0, Manufactured = item.Volume
    - Compare against TriggerThreshold, log excess if overflow detected
    - _Satisfies: Req 4, Criteria 4.1, 4.2, 4.3, 4.5_
    - _Inputs: BackgroundProcessor.cs, GameConstants.cs, ItemType enum, design.md (Section 5)_
    - _Output: OE2EmpireTracker.Common/Services/BackgroundProcessor.cs_
    - _Verification: getDiagnostics, build solution_

- [x] 6. Checkpoint - Ensure evaluation logic compiles and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 7. Update FormColonyV2 Overflow tab UI for rule type support
  - [x] 7.1 Add RuleType column to dgvOverflowRules and cmbOverflowRuleType ComboBox
    - Add `colOverflowRuleType` DataGridViewTextBoxColumn as first column in dgvOverflowRules
    - Add `cmbOverflowRuleType` ComboBox with items "SpecificResource" and "TotalWarehouse"
    - Wire SelectedIndexChanged to enable/disable resource+purity inputs
    - _Satisfies: Req 6, Criteria 6.1, 6.5_
    - _Inputs: FormColonyV2.cs, FormColonyV2.Designer.cs, design.md (Section 7)_
    - _Output: FormColonyV2.cs, FormColonyV2.Designer.cs_
    - _Verification: getDiagnostics, build solution_

  - [x] 7.2 Update PopulateOverflowGrid and CmdAddOverflowRule_Click for RuleType
    - Include RuleType in grid row data
    - Show empty resource/purity for TotalWarehouse rules in grid
    - Read selected RuleType when adding new rule
    - Validate via OverflowRuleValidator before persisting
    - Accept decimal input for threshold (change validation pattern)
    - _Satisfies: Req 6, Criteria 6.2, 6.3, 6.4_
    - _Inputs: FormColonyV2.cs, OverflowRuleValidator.cs, design.md (Section 7)_
    - _Output: FormColonyV2.cs_
    - _Verification: getDiagnostics, build solution_

- [x] 8. Checkpoint - Ensure full solution builds with zero warnings
  - Ensure all tests pass, ask the user if questions arise.


- [x] 9. Write property-based tests for serialization and validation
  - [x] 9.1 Write property test: Serialization Round-Trip Preserves Rule Data
    - **Property 1: Serialization Round-Trip Preserves Rule Data**
    - Generate random WarehouseOverflowRule with varying RuleType, thresholds, resource fields
    - Serialize to JSON, deserialize back, assert equivalence
    - Assert RuleType field in JSON is a string (not integer)
    - **Validates: Requirements 1.4, 5.3**
    - _Inputs: design.md (Correctness Properties), WarehouseOverflowRule.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

  - [x] 9.2 Write property test: SpecificResource Validation Requires Resource Fields
    - **Property 5: SpecificResource Validation Requires Resource Fields**
    - Generate random rules with RuleType = SpecificResource
    - Assert: empty ResourceName OR empty ResourcePurity → validation rejects
    - Assert: both non-empty + valid common fields → validation accepts
    - **Validates: Requirements 3.4, 7.1, 7.2**
    - _Inputs: design.md (Correctness Properties), OverflowRuleValidator.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

  - [x] 9.3 Write property test: TotalWarehouse Validation Ignores Resource Fields
    - **Property 6: TotalWarehouse Validation Ignores Resource Fields**
    - Generate random rules with RuleType = TotalWarehouse
    - Assert: validation does NOT reject due to empty ResourceName/ResourcePurity
    - Assert: accepted if common fields (threshold > 0, destination, route) are valid
    - **Validates: Requirements 4.4, 7.6**
    - _Inputs: design.md (Correctness Properties), OverflowRuleValidator.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

  - [x] 9.4 Write property test: Common Validation Rejects Invalid Rules
    - **Property 7: Common Validation Rejects Invalid Rules**
    - Generate random rules with threshold ≤ 0 OR empty DestinationUUID OR empty DeliveryRouteUUID
    - Assert: validation always rejects with appropriate error messages
    - **Validates: Requirements 7.3, 7.4, 7.5**
    - _Inputs: design.md (Correctness Properties), OverflowRuleValidator.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_


- [x] 10. Write property-based tests for volume computation and evaluation
  - [x] 10.1 Write property test: SpecificResource Volume Equals Quantity for Resources
    - **Property 2: SpecificResource Volume Computation Equals Quantity for Resources**
    - Generate random ItemBag with resource items (volume = 1 per unit)
    - Assert: computed volume equals sum of quantities of matching items
    - **Validates: Requirements 2.2, 2.3, 2.4, 3.1**
    - _Inputs: design.md (Correctness Properties), BackgroundProcessor.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

  - [x] 10.2 Write property test: TotalWarehouse Volume Uses Correct Per-Unit Volumes
    - **Property 3: TotalWarehouse Volume Computation Uses Correct Per-Unit Volumes**
    - Generate random ItemBag with mixed item types
    - Assert: total volume = sum of (quantity × per-unit volume) per type
    - Per-unit volumes: resources=1, commodities=10, workers=50, blueprints/surveys=0, manufactured=item.Volume
    - **Validates: Requirements 4.1, 4.5**
    - _Inputs: design.md (Correctness Properties), BackgroundProcessor.cs, GameConstants.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

  - [x] 10.3 Write property test: Excess Volume Calculation
    - **Property 4: Excess Volume Calculation**
    - Generate random volume and threshold values
    - Assert: when volume > threshold, excess = volume − threshold
    - Assert: when volume ≤ threshold, no overflow detected
    - **Validates: Requirements 3.2, 4.2**
    - _Inputs: design.md (Correctness Properties), BackgroundProcessor.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionPropertyTests.cs_
    - _Verification: vstest.console run_

- [x] 11. Write unit tests for backward compatibility and edge cases
  - [x] 11.1 Write unit tests for deserialization backward compatibility
    - Test: JSON without RuleType field deserializes as SpecificResource
    - Test: JSON with integer TriggerThreshold deserializes as decimal
    - Test: All existing JSON property names preserved exactly
    - _Satisfies: Req 5, Criteria 5.1, 5.2, 5.3_
    - _Inputs: design.md (Testing Strategy), WarehouseOverflowRule.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionTests.cs_
    - _Verification: vstest.console run_

  - [x] 11.2 Write unit tests for unknown RuleType handling and default values
    - Test: Unknown RuleType logs warning and skips rule (no exception)
    - Test: New rule defaults to SpecificResource
    - Test: Evaluation skips rules where colony not found
    - _Satisfies: Req 8, Criterion 8.2; Req 1, Criterion 1.3_
    - _Inputs: design.md (Error Handling), BackgroundProcessor.cs_
    - _Output: OE2EmpireTracker.Tests/Services/OverflowRuleExpansionTests.cs_
    - _Verification: vstest.console run_

- [x] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements and criteria for traceability
- Checkpoints ensure incremental validation after each logical group
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples, edge cases, and backward compatibility
- Task 2 exists because changing TriggerThreshold from int to decimal may break existing consumers
- UI tasks (7.x) are split into Designer/control changes vs logic changes to stay within file limits
- All property tests go in a single file (OverflowRuleExpansionPropertyTests.cs) per design spec
- All unit tests go in a single file (OverflowRuleExpansionTests.cs) per design spec

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2"] },
    { "id": 2, "tasks": ["1.3", "2.1"] },
    { "id": 3, "tasks": ["3.1"] },
    { "id": 4, "tasks": ["5.1"] },
    { "id": 5, "tasks": ["5.2", "5.3"] },
    { "id": 6, "tasks": ["7.1"] },
    { "id": 7, "tasks": ["7.2"] },
    { "id": 8, "tasks": ["9.1", "9.2", "9.3", "9.4"] },
    { "id": 9, "tasks": ["10.1", "10.2", "10.3", "11.1", "11.2"] }
  ]
}
```
