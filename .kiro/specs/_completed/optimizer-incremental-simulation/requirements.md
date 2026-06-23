# Requirements Document

## Introduction

BuildOrderOptimizer currently uses a SimulateAll approach that re-simulates the entire result list from scratch on every status query. SimulateAll is O(n) per call and is called O(n) times during optimization (Step A deficit check, Step B look-ahead, Step C final check per primary, plus FixDeficits loops), producing O(n²) overall cost. For a colony with 60 structures this means ~18,000 FindBlueprint calls and redundant re-accumulation of resource totals.

This spec replaces SimulateAll calls inside the optimizer with incremental delta tracking: maintain a running ColonyStructureStatus accumulator and update it with O(1) deltas when structures are added. The existing SimulateOneMore method already computes single-structure contributions; the optimizer needs to accumulate rather than recompute.

This is a pure internal refactor. The public API of BuildOrderOptimizer does not change. All 17 existing optimizer tests (13 with pinned exact sequences) must produce identical output.

**Backlog reference:** BL-072

## Glossary

- **Optimizer**: BuildOrderOptimizer — the service that reorders colony structures so Power, Habitation, Food, and Entertainment constraints are satisfied at every build step.
- **SimulateAll**: The current private method that iterates the entire result list, calling CalculateBuilt for each structure, to produce a cumulative ColonyStructureStatus. O(n) per call.
- **SimulateOneMore**: The existing private method that computes the status after adding one more structure to a given previous status. O(1) per call.
- **Running_Accumulator**: A ColonyStructureStatus instance maintained across the optimization loop, updated incrementally as structures are appended to the result list.
- **Delta**: The resource contribution of a single structure (power, habitation, food, entertainment, worker counts) computed by SimulateOneMore or CalculateBuilt.
- **Deficit**: A state where any Required field exceeds its corresponding Provided/Provision field in ColonyStructureStatus (Power, Habitation, Food, or Entertainment).
- **Primary**: A colony structure that is not a support structure and not a Colony Command Centre — the structures being optimized around.
- **Support_Structure**: A structure that provides Power, Habitation, Food, or Entertainment (Reactor, Hab Block, Hydro Bay, Entertainment Centre).
- **Pinned_Test**: A test that asserts the exact sequence of structure types AND the exact resource values at every position in the optimized output.
- **Unallocated_Worker**: A worker type (Blue Collar, White Collar, Specialist) counted once per type for the first structure that needs it, tracked via boolean flags on ColonyStructureStatus.
- **ColonyStatusCalculator**: The service class containing CalculateBuilt (cumulative per-structure calculation) and ComputeStructureDelta (per-structure delta computation).
- **CalculateBuilt**: The method on ColonyStatusCalculator that computes cumulative resource status given a previous status and a new structure. Handles worker counting and unallocated worker tracking.

## Requirements

### Requirement 1: Replace SimulateAll with Running Accumulator

**User Story:** As a developer, I want the optimizer to maintain a running resource accumulator instead of re-simulating the entire list on every status query, so that optimization runs in O(n) total time instead of O(n²).

#### Acceptance Criteria

1. THE Optimizer SHALL maintain a Running_Accumulator of type ColonyStructureStatus that tracks the cumulative resource state of all structures in the result list.
2. WHEN a structure is appended to the result list, THE Optimizer SHALL update the Running_Accumulator by computing the Delta of the new structure and adding it to the accumulator, rather than re-simulating the entire list.
3. THE Optimizer SHALL initialize the Running_Accumulator to a zero-valued ColonyStructureStatus before the bootstrap phase begins.
4. WHEN the bootstrap phase places CC, Reactor, Hab, Hydro, and Ent structures, THE Optimizer SHALL update the Running_Accumulator after each placement.
5. THE Optimizer SHALL use the Running_Accumulator in place of every SimulateAll call within the primary processing loop (Step A, Step B, Step C, and FixDeficits).
6. THE Optimizer SHALL use the Running_Accumulator in place of every SimulateAll call within the leftover support append phase.

### Requirement 2: Preserve Exact Output Behavior

**User Story:** As a developer, I want the incremental simulation to produce identical output to the current SimulateAll approach, so that the refactor introduces no behavioral changes.

#### Acceptance Criteria

1. FOR ALL colony configurations, THE Optimizer SHALL produce the same structure sequence (same types in the same order) as the current SimulateAll-based implementation.
2. FOR ALL colony configurations, THE Optimizer SHALL produce the same cumulative resource values (PowerRequired, PowerProvided, HabitationRequired, HabitationProvision, FoodRequired, FoodProvision, EntertainmentRequired, EntertainmentProvided) at every position in the output as the current implementation.
3. THE Optimizer SHALL handle Unallocated_Worker tracking identically to the current implementation: each worker type (Blue Collar, White Collar, Specialist) is counted once for the first structure that needs it, adding 1 to habitation required, 1 to food required, and 2 to entertainment required.
4. THE Optimizer SHALL preserve the deficit resolution priority order: Power, then Habitation, then Food, then Entertainment.
5. THE Optimizer SHALL preserve the look-ahead logic: before placing a Primary, check whether adding the Primary plus a future Hab and Hydro would cause a hab, food, or entertainment deficit, and pre-place support if needed.
6. All 17 existing Pinned_Tests SHALL pass with identical output after the refactor.

### Requirement 3: Correct Accumulator Updates for Support Placement

**User Story:** As a developer, I want the accumulator to stay correct when FixDeficits and PlaceSupportSafe insert support structures, so that deficit resolution decisions are based on accurate resource totals.

#### Acceptance Criteria

1. WHEN FixDeficits appends a support structure to the result list, THE Optimizer SHALL update the Running_Accumulator to reflect the new structure's contribution before the next deficit check iteration.
2. WHEN PlaceSupportSafe appends a support structure to the result list, THE Optimizer SHALL update the Running_Accumulator to reflect the new structure's contribution.
3. WHEN PlaceSupportSafe recursively places prerequisite support (e.g., a Reactor before an Entertainment Centre that needs power), THE Optimizer SHALL update the Running_Accumulator after each recursive placement.
4. IF PlaceSupportSafe detects that placing a support structure would cause a new deficit (e.g., power deficit from an Entertainment Centre), THEN THE Optimizer SHALL use the Running_Accumulator to evaluate the new deficit rather than calling SimulateAll.

### Requirement 4: SimulateOneMore Remains Stateless for Look-Ahead

**User Story:** As a developer, I want look-ahead simulations to remain stateless projections that do not modify the accumulator, so that speculative checks do not corrupt the running total.

#### Acceptance Criteria

1. WHEN the Optimizer performs a look-ahead simulation (Step B: projecting the effect of placing a Primary, or projecting the effect of a future Hab and Hydro), THE Optimizer SHALL use SimulateOneMore against the Running_Accumulator without modifying the accumulator.
2. THE SimulateOneMore method SHALL remain a pure function that takes a previous status and returns a new status without side effects.
3. WHEN multiple look-ahead projections are chained (Primary, then Hab, then Hydro), THE Optimizer SHALL chain SimulateOneMore calls on the projected status, not on the Running_Accumulator.

### Requirement 5: Remove SimulateAll from Optimizer

**User Story:** As a developer, I want the SimulateAll method removed from the optimizer after the refactor, so that there is no dead code and no temptation to regress to O(n squared) behavior.

#### Acceptance Criteria

1. WHEN the refactor is complete, THE Optimizer SHALL contain no calls to SimulateAll within the Optimize method or any of its helper methods (FixDeficits, PlaceSupportSafe).
2. THE Optimizer SHALL remove or mark as unused the private SimulateAll method after all call sites have been replaced with the Running_Accumulator.

### Requirement 6: Public API Unchanged

**User Story:** As a developer, I want the public interface of BuildOrderOptimizer to remain unchanged, so that no callers need to be updated.

#### Acceptance Criteria

1. THE Optimizer SHALL retain the same public constructor signature: BuildOrderOptimizer(PlayerContext playerContext).
2. THE Optimizer SHALL retain the same public method signature: List<ColonyStructure> Optimize(Colony colony).
3. THE Optimizer SHALL retain the same public method signature: bool IsSupportStructure(Blueprint blueprint).
4. THE Optimizer SHALL introduce no new public methods or properties.

### Requirement 7: Complexity Reduction Verification

**User Story:** As a developer, I want to verify that the refactor achieves the expected complexity reduction, so that the performance improvement is confirmed.

#### Acceptance Criteria

1. WHEN the refactor is complete, THE Optimizer SHALL make zero calls to SimulateAll during the Optimize method execution.
2. THE Optimizer SHALL make at most O(n) calls to SimulateOneMore or CalculateBuilt during the Optimize method execution, where n is the number of structures in the output.
3. WHEN a structure is appended to the result list, THE Optimizer SHALL perform O(1) work to update the Running_Accumulator (one SimulateOneMore call or equivalent field-level addition).