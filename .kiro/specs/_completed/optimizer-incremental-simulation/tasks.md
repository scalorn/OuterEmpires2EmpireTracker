# Implementation Plan: Optimizer Incremental Simulation

## Overview

Refactor `BuildOrderOptimizer.Optimize()` to replace O(n²) `SimulateAll` calls with incremental delta tracking using a running `ColonyStructureStatus` accumulator. The optimizer currently re-simulates the entire result list from scratch on every status query. The refactored version maintains a single accumulator updated via `SimulateOneMore` each time a structure is appended, reducing overall complexity from O(n²) to O(n).

This is a pure internal refactor — the public API does not change and all 17 existing tests must produce identical output.

## Tasks

- [x] 1. Add running accumulator and update bootstrap phase
  - [x] 1.1 Add accumulator local variable in `Optimize()`, initialized to `new ColonyStructureStatus()`
    - After each `PlaceFromPool` call in the bootstrap section (CC, Reactor, Hab, Hydro, Ent), update the accumulator via `SimulateOneMore`
    - The accumulator must reflect the cumulative state of all 5 bootstrap structures before the primary loop begins
    - _Requirements: 1.1, 1.3, 1.4_

  - [x] 1.2 Write property test: Incremental accumulation equivalence (Property 1)
    - **Property 1: Incremental accumulation equivalence**
    - Generate a random sequence of 1–30 structures (each with a randomly chosen blueprint type from CC, Rx, Hb, Hy, En, Mi, Re, Mf, Wh, Rs, RO). Build cumulative status two ways: (1) fold `SimulateOneMore` one structure at a time, (2) call `CalculateBuilt` in a loop over the full sequence. Assert all fields of the final status are equal.
    - Use FsCheck with NUnit integration. Minimum 100 iterations.
    - **Validates: Requirements 1.1, 1.2, 2.1, 2.2, 2.3**

  - [x] 1.3 Write property test: SimulateOneMore purity (Property 2)
    - **Property 2: SimulateOneMore is pure (does not mutate input)**
    - Generate a random `ColonyStructureStatus` (random decimal values for all provision/required fields, random booleans for unallocated flags) and a random structure with a valid blueprint. Snapshot all fields of the input status before calling `SimulateOneMore`. Assert all fields are unchanged after the call.
    - Use FsCheck with NUnit integration. Minimum 100 iterations.
    - **Validates: Requirements 4.1, 4.2**

- [x] 2. Update FixDeficits to use accumulator
  - [x] 2.1 Change `FixDeficits` signature to accept `ref ColonyStructureStatus accumulator` as an additional parameter
    - Replace the `SimulateAll(result, workers)` call at the top of the loop with a read from `accumulator`
    - Replace deficit checks against `currentEnd` with checks against `accumulator`
    - Pass `ref accumulator` through to `PlaceSupportSafe` calls
    - Update all call sites in `Optimize()` to pass `ref accumulator`
    - _Requirements: 1.5, 3.1, 3.4_

- [x] 3. Update PlaceSupportSafe to use accumulator
  - [x] 3.1 Change `PlaceSupportSafe` signature to accept `ref ColonyStructureStatus accumulator` as an additional parameter
    - Replace `SimulateAll(result, workers)` call (which computed `beforeStatus`) with a read from `accumulator`
    - Use `accumulator` as `beforeStatus` for the new-deficit checks
    - After `result.Add(support)`, update the accumulator: `accumulator = SimulateOneMore(accumulator, support, bp, workers);`
    - Pass `ref accumulator` through to recursive `PlaceSupportSafe` calls
    - Update all call sites (in `FixDeficits` and `Optimize()`) to pass `ref accumulator`
    - _Requirements: 1.5, 3.2, 3.3_

- [x] 4. Checkpoint — Verify FixDeficits and PlaceSupportSafe refactoring
  - Ensure all 17 existing tests pass with identical output. Ask the user if questions arise.

- [x] 5. Replace SimulateAll calls in the primary loop
  - [x] 5.1 Replace Step A SimulateAll calls
    - Remove the `SimulateAll` call before `FixDeficits` — the accumulator is already current
    - Remove the `SimulateAll` call after `FixDeficits` — `FixDeficits` now updates the accumulator via `ref`
    - Use `accumulator` directly where `status` was used
    - _Requirements: 1.5, 2.4_

  - [x] 5.2 Replace Step B SimulateAll calls
    - Replace `SimulateOneMore(status, ...)` with `SimulateOneMore(accumulator, ...)` for look-ahead projections (these do NOT modify the accumulator)
    - Replace the three `currentEnd = SimulateAll(result, idealWorkers)` calls in the look-ahead section with reads from `accumulator`
    - Compare `afterFutureSupport` fields against `accumulator` fields instead of `currentEnd`
    - Pass `ref accumulator` to `PlaceSupportSafe` calls in the look-ahead section
    - _Requirements: 1.5, 4.1, 4.3_

  - [x] 5.3 Replace Step B final deficit check SimulateAll
    - Remove `status = SimulateAll(result, idealWorkers)` before the final `afterPrimary` check
    - Use `accumulator` directly: `afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers)`
    - Pass `ref accumulator` to `FixDeficits` in the final deficit check
    - _Requirements: 1.5, 2.5_

  - [x] 5.4 Update Step C to advance the accumulator
    - After `result.Add(primary)`, add: `accumulator = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);`
    - This keeps the accumulator in sync with the result list for the next iteration
    - _Requirements: 1.2, 7.3_

- [x] 6. Replace SimulateAll in the leftover support phase
  - [x] 6.1 Replace the `SimulateAll` call in the leftover support loop
    - Use `accumulator` instead of calling `SimulateAll(result, idealWorkers)` for each leftover
    - Use `SimulateOneMore(accumulator, leftover, lBp, idealWorkers)` for the look-ahead check (does not modify accumulator)
    - Pass `ref accumulator` to `FixDeficits` if the leftover would cause a deficit
    - After `result.Add(leftover)`, update: `accumulator = SimulateOneMore(accumulator, leftover, lBp, idealWorkers);`
    - _Requirements: 1.6_

- [x] 7. Remove SimulateAll method
  - [x] 7.1 Delete the `SimulateAll` private method from `BuildOrderOptimizer.cs`
    - Verify no remaining references to `SimulateAll` exist in the file
    - _Requirements: 5.1, 5.2_

- [x] 8. Checkpoint — Run all existing tests and verify behavioral equivalence
  - Run all 17 existing optimizer tests. All must pass with identical output (same structure sequences, same resource values at every position).
  - Verify via grep that `SimulateAll` is not called anywhere in `BuildOrderOptimizer.cs`.
  - Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 2.6, 5.1, 7.1_

- [x] 9. Add FsCheck property-based tests
  - [x] 9.1 Install FsCheck NuGet package
    - Add FsCheck and FsCheck.NUnit packages to `OE2EmpireTracker.Tests/packages.config`
    - Add assembly references to `OE2EmpireTracker.Tests.csproj`
    - Verify the project builds with the new dependency
    - _Requirements: 7.2_

  - [x] 9.2 Write property test: Optimizer output equivalence (Property 3)
    - **Property 3: Optimizer output equivalence**
    - Generate a random colony configuration (1 CC + 0–10 random primaries + 0–15 random support structures). Run the optimizer. Re-simulate the output sequence with `CalculateBuilt` and assert: (a) no deficits from the first primary onward, (b) all input structures are preserved in the output, (c) CC is first.
    - Use FsCheck with NUnit integration. Minimum 100 iterations.
    - **Validates: Requirements 2.1, 2.2, 2.4, 2.5, 2.6**

  - [x] 9.3 Write property test: Linear call count (Property 4)
    - **Property 4: Linear call count**
    - Generate a random colony of n input structures (1 CC + 0–20 random structures). Run the optimizer. Assert the output has at most n + 50 structures (safety limit), confirming bounded growth and no runaway support creation.
    - Use FsCheck with NUnit integration. Minimum 100 iterations.
    - **Validates: Requirements 7.1, 7.2, 7.3**

- [x] 10. Final checkpoint — Full regression
  - Run all tests (existing 17 + new property tests). All must pass.
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each major phase
- Property tests (1.2, 1.3, 9.2, 9.3) validate universal correctness properties from the design document
- The existing 17 pinned tests (13 with exact sequences) are the primary regression guarantee
- FsCheck property tests provide additional confidence across random colony configurations
- The refactoring is confined entirely to `BuildOrderOptimizer.cs` — no other production classes are modified
