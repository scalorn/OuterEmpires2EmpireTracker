# Implementation Plan: Colony Planner Reorder

## Overview

This plan implements manual structure reordering (move up/down with CC protection), an "Optimize Build Order" button calling the server, and the server endpoint enhancement to wire the real `BuildOrderOptimizer`. Tasks are ordered: store logic first, then UI components, then server, then integration wiring.

## Tasks

- [x] 1. Implement store reorder actions
  - [x] 1.1 Add `moveStructureUp` and `moveStructureDown` actions to plannerStore
    - Add `moveStructureUp(id)` and `moveStructureDown(id)` to `PlannerState` interface
    - Implement swap logic: find target structure, find neighbor by `buildQueuePosition`, swap positions
    - No-op when structure is CC (`subType === 'ColonyCommandCentre'`), at boundary, or at position 2 with CC at position 1
    - Recalculate `computeColonyStatus` after swap
    - _Requirements: 1.1, 1.2, 1.8_
    - _Inputs: `plannerStore.ts`, `computeColonyStatus.ts`_
    - _Output: `plannerStore.ts` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

  - [x] 1.2 Add `applyOptimizedOrder` action to plannerStore
    - Add `applyOptimizedOrder(optimizedOrder: OptimizedOrderEntry[])` to `PlannerState` interface
    - For each entry, match `flatpackBlueprintUUID` to local structure's `blueprintUUID`, update `buildQueuePosition` to `buildQueueSequence`
    - Ignore entries with no local match (optimizer-inserted support structures)
    - Recalculate `computeColonyStatus` after applying
    - _Requirements: 2.3, 2.4, 2.9_
    - _Inputs: `plannerStore.ts`, `computeColonyStatus.ts`_
    - _Output: `plannerStore.ts` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

  - [x] 1.3 Write property tests for store reorder actions
    - **Property 1: Move swap preserves all other positions**
    - **Property 3: Status invariant after reorder**
    - **Property 6: Apply optimized order correctness**
    - **Validates: Requirements 1.1, 1.2, 1.8, 2.3, 2.4, 2.9**
    - _Inputs: `plannerStore.ts`_
    - _Output: `plannerStore.property.test.ts` created_
    - _Verification: `npx vitest run` from OE2EmpireTracker.Web/_

- [ ] 2. Implement StructureList sorting and disable logic
  - [x] 2.1 Add sort-by-`buildQueuePosition` and compute disable flags in StructureList
    - Sort structures by `buildQueuePosition` ascending before rendering
    - Compute `isMoveUpDisabled` and `isMoveDownDisabled` for each structure based on: CC status, position, CC protection, single-item list
    - Pass `onMoveUp`, `onMoveDown`, `isMoveUpDisabled`, `isMoveDownDisabled` props to `StructureItem`
    - Wire `moveStructureUp` and `moveStructureDown` from store
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 1.7, 1.9_
    - _Inputs: `StructureList.tsx`, `plannerStore.ts`_
    - _Output: `StructureList.tsx` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

  - [-] 2.2 Write property test for disable logic correctness
    - **Property 2: Disable logic correctness**
    - **Validates: Requirements 1.3, 1.4, 1.5, 1.6, 1.7**
    - _Inputs: `StructureList.tsx`, `plannerStore.ts`_
    - _Output: `StructureList.property.test.ts` created_
    - _Verification: `npx vitest run` from OE2EmpireTracker.Web/_

- [ ] 3. Add Up/Down buttons to StructureItem
  - [-] 3.1 Extend StructureItem props and render move buttons
    - Add `onMoveUp`, `onMoveDown`, `isMoveUpDisabled`, `isMoveDownDisabled` to `StructureItemProps`
    - Render Up (▲) and Down (▼) buttons with disabled states and appropriate aria-labels
    - Buttons disabled when corresponding `isMove*Disabled` prop is true
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_
    - _Inputs: `StructureItem.tsx`_
    - _Output: `StructureItem.tsx` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

- [~] 4. Checkpoint - Verify manual reorder works end-to-end
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Add Optimize Build Order button to ColonyPlanner
  - [x] 5.1 Add `OptimizedOrderEntry` type and update `BuildOrderResult` in generated types
    - Add `OptimizedOrderEntry` interface (`flatpackBlueprintUUID: string`, `buildQueueSequence: number`)
    - Add optional `optimizedOrder?: OptimizedOrderEntry[]` field to `BuildOrderResult`
    - _Requirements: 2.3_
    - _Inputs: `generated.ts`_
    - _Output: `generated.ts` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

  - [x] 5.2 Add "Optimize Build Order" button with loading/error/disable states
    - Add button in planner actions area with text "Optimize Build Order"
    - Disable when structures list is empty or has only 1 item
    - Show loading indicator and disable button while request is in progress
    - On success: call `applyOptimizedOrder` with response's `optimizedOrder`
    - On error: show error banner, do not modify structure list
    - Map structures to wire format: `blueprintUUID` → `flatpackBlueprintUUID`, state → `isBuilt`/`isStaged`/`isOnline`, `buildQueuePosition` → `buildQueueSequence`
    - _Requirements: 2.1, 2.2, 2.5, 2.6, 2.7, 2.8_
    - _Inputs: `ColonyPlanner.tsx`, `plannerStore.ts`, `colony-planner.ts` (API client)_
    - _Output: `ColonyPlanner.tsx` modified_
    - _Verification: `npm run build` from OE2EmpireTracker.Web/_

  - [~] 5.3 Write unit tests for Optimize Build Order button
    - Test button exists with correct text
    - Test button disabled when list empty or has 1 item
    - Test button disabled + spinner during request
    - Test error message shown on API failure, list unchanged
    - _Requirements: 2.1, 2.5, 2.6, 2.7, 2.8_
    - _Inputs: `ColonyPlanner.tsx`, `ColonyPlanner.test.tsx`_
    - _Output: `ColonyPlanner.test.tsx` modified_
    - _Verification: `npx vitest run` from OE2EmpireTracker.Web/_

  - [~] 5.4 Write property test for wire format mapping
    - **Property 5: Wire format mapping correctness**
    - **Validates: Requirements 2.2**
    - _Inputs: `plannerStore.ts`, `ColonyPlanner.tsx`_
    - _Output: `plannerStore.property.test.ts` updated_
    - _Verification: `npx vitest run` from OE2EmpireTracker.Web/_

- [~] 6. Checkpoint - Verify frontend optimize flow
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Enhance server endpoint to use real BuildOrderOptimizer
  - [~] 7.1 Add `OptimizedOrderEntryResponse` model and update `BuildOrderResponse`
    - Add `OptimizedOrderEntryResponse` class with `FlatpackBlueprintUUID` and `BuildQueueSequence`
    - Add `OptimizedOrder` list property to `BuildOrderResponse`
    - Add empty-structures validation (return 400 when `Structures` is empty list)
    - _Requirements: 3.2, 3.6_
    - _Inputs: `ColonyPlannerEndpoints.cs`_
    - _Output: `ColonyPlannerEndpoints.cs` modified_
    - _Verification: `dotnet build OE2EmpireTracker.Server`_

  - [~] 7.2 Wire `BuildOrderOptimizer` into `OptimizeBuildOrder` method
    - Replace stub `CalculateBuildOrder` with real optimizer invocation
    - Map `PlannerStructureDto` list → `Colony` object with `ColonyStructure` entries
    - Call `BuildOrderOptimizer.Optimize(colony)` via DI-registered `PlayerContext`
    - Map result to `OptimizedOrderEntryResponse[]` (UUID + 0-based sequence)
    - Preserve `steps` and `totalTimeEstimate` in response for backward compatibility
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.8_
    - _Inputs: `ColonyPlannerEndpoints.cs`, `BuildOrderOptimizer.cs`_
    - _Output: `ColonyPlannerEndpoints.cs` modified_
    - _Verification: `dotnet build OE2EmpireTracker.Server`_

  - [~] 7.3 Write server integration tests for build-order endpoint
    - Test valid request invokes optimizer and returns `optimizedOrder`
    - Test response contains `steps` + `totalTimeEstimate` for backward compat
    - Test missing body → 400, invalid JSON → 400, empty structures → 400
    - Test no auth required (200 without token)
    - **Property 7: Server validation rejects invalid requests**
    - **Validates: Requirements 3.1, 3.2, 3.4, 3.5, 3.6, 3.7**
    - _Inputs: `ColonyPlannerEndpoints.cs`_
    - _Output: `OE2EmpireTracker.Server.Tests/Endpoints/ColonyPlannerBuildOrderTests.cs` created_
    - _Verification: `dotnet test OE2EmpireTracker.Server.Tests --no-build`_

- [~] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The server already has a project reference to `OE2EmpireTracker.Common` (no new reference needed)
- The API client (`colony-planner.ts`) already has `optimizeBuildOrder` — no changes needed there
- The `useColonyPlannerBuildOrder` hook already exists — can be used directly in `ColonyPlanner.tsx`
- File paths are relative to `OE2EmpireTracker.Web/src/pages/planner/` for frontend files

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "2.1", "5.1"] },
    { "id": 2, "tasks": ["2.2", "3.1", "5.2"] },
    { "id": 3, "tasks": ["5.3", "5.4", "7.1"] },
    { "id": 4, "tasks": ["7.2"] },
    { "id": 5, "tasks": ["7.3"] }
  ]
}
```
