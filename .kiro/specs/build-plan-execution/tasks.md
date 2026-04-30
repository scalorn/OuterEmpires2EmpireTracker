# Implementation Plan: Build Plan Execution

## Overview

Extend the Build Planner from a resource-delivery tracker into a full manufacturing lifecycle tracker. A new stateless `BuildPlanExecutionService` handles all detection and pre-configuration logic. The `BackgroundProcessor` calls it during cascade cycles. `FormBuildPlanner` adds UI actions for starting manufacturing and viewing progress.

## Tasks

- [x] 1. Create BuildPlanExecutionService with result types and status helpers
  - [x] 1.1 Create `OE2EmpireTracker/Services/BuildPlanExecutionService.cs` with the static class, NLog logger, and nested result types (`StartManufacturingResult`, `BatchStartResult`, `ContentionInfo`)
    - Implement `ComputeStatusSummary(BuildPlan)` returning `Dictionary<BuildItemStatus, int>` with counts per status
    - Implement `IsPlanComplete(BuildPlan)` returning true only when all items are Completed
    - Implement `DetectContention(BuildItem, IEnumerable<BuildPlan>)` returning items from other plans on the same structure
    - _Requirements: 5.1, 5.3, 13.1, 14.1, 14.5_

  - [x] 1.2 Write property test for status summary consistency
    - **Property 10: Status summary counts are consistent**
    - **Validates: Requirements 5.1, 5.3, 13.1, 13.3**

  - [x] 1.3 Write property test for cross-plan contention detection
    - **Property 11: Cross-plan contention detection**
    - **Validates: Requirements 14.1, 14.5**

- [x] 2. Implement AdvanceBuildItemStatuses detection logic
  - [x] 2.1 Implement `AdvanceBuildItemStatuses(BuildPlan, colonyFinder, blueprintFinder, shipFinder, stationFinder, currentPlayerUUID)` with three detection phases
    - Phase 1: Staged+allocated items with zero shortfalls advance to Ready (calls `ResourceCheckService.ComputeShortfalls`)
    - Phase 2: Ready items advance to InProgress when structure has matching active job, item is lowest sequence among Ready items on that structure, and dependency (if any) is Completed
    - Phase 3: InProgress items advance to Completed when structure job finishes (type-specific checks per design)
    - Log each transition with item UUID, plan name, and structure UUID
    - Wrap each item in try/catch, log errors, continue with remaining items
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 7.1, 7.2, 7.3, 8.1, 10.1, 10.2, 10.3, 11.1, 11.2, 11.3, 12.1, 12.2_

  - [x] 2.2 Write property test for forward-only status transitions
    - **Property 1: Forward-only status transitions**
    - **Validates: Requirements 1.5, 2.6**

  - [x] 2.3 Write property test for Staged-to-Ready skip
    - **Property 4: Staged-to-Ready skip when resources present**
    - **Validates: Requirements 11.1, 11.2, 11.3**

  - [x] 2.4 Write property test for Ready-to-InProgress detection
    - **Property 2: Ready-to-InProgress detection for matching structure state**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 8.1**

  - [x] 2.5 Write property test for InProgress-to-Completed detection
    - **Property 3: InProgress-to-Completed detection for cleared structure state**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 10.1, 10.2**

  - [x] 2.6 Write property test for dependency blocking
    - **Property 5: Dependency blocks InProgress advancement**
    - **Validates: Requirements 7.1, 7.2, 7.3**

  - [x] 2.7 Write property test for sequence ordering
    - **Property 6: Sequence ordering restricts manufacturing start**
    - **Validates: Requirements 8.1, 8.2**

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement CanStartManufacturing and StartManufacturing
  - [x] 4.1 Implement `CanStartManufacturing(BuildItem, BuildPlan, colonyFinder, blueprintFinder)` eligibility check
    - Item must be Ready with valid StructureUUID
    - Structure must exist, be built and online, have no active ProcessCompletionTime
    - Item must be lowest SequenceInStructure among Ready items on that structure in the plan
    - Dependency (if any) must be Completed
    - _Requirements: 3.1, 3.7, 3.8, 6.1, 7.1, 8.1_

  - [x] 4.2 Implement `StartManufacturing(BuildItem, BuildPlan, colonyFinder, blueprintFinder)` pre-configuration
    - Per item type: set type-specific fields on ColonyStructure (ManufacturingBlueprintUUID, ManufacturingCommodityName, ResearchingBlueprintUUID, MiningSurvey/Resource, RefiningResource/Purity)
    - Set ManufacturingQuantity and ManufacturingCompleted where applicable
    - Compute and set ProcessCompletionTime using appropriate time calculation (blueprint ManufactureRunTime, CommodityCycleSeconds, ResearchTimeLookup, mining/refining intervals) with ProductionFocus skill multiplier
    - Call TryAdvanceStatus to InProgress on success
    - Return StartManufacturingResult with Success=false and ErrorMessage for busy/missing/not-built structures
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [x] 4.3 Write property test for StartManufacturing configures correctly
    - **Property 7: StartManufacturing configures structure correctly**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

  - [x] 4.4 Write property test for StartManufacturing rejects busy structures
    - **Property 8: StartManufacturing rejects busy structures**
    - **Validates: Requirements 3.7, 6.1**

- [ ] 5. Implement StartAllReady batch operation
  - [~] 5.1 Implement `StartAllReady(BuildPlan, colonyFinder, blueprintFinder)` batch start
    - Iterate Ready items grouped by StructureUUID, process in SequenceInStructure order
    - Start only the first eligible Ready item per structure via CanStartManufacturing + StartManufacturing
    - Skip items whose structure is busy, record skip reason
    - Return BatchStartResult with StartedCount, SkippedCount, SkippedReasons
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 8.2_

  - [~] 5.2 Write property test for batch start first-in-sequence
    - **Property 9: Batch start processes only first-in-sequence per structure**
    - **Validates: Requirements 4.1, 4.2, 4.3, 8.2**

- [~] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Integrate with BackgroundProcessor cascade
  - [~] 7.1 Add CascadeBuildPlanExecution step in `BackgroundProcessor.ProcessCascades()`
    - Insert after the existing `resourceCheckDirty` block and before `stockTargetsDirty`
    - Iterate active plans, call `BuildPlanExecutionService.AdvanceBuildItemStatuses` for each
    - Track modified plan UUIDs and add to modifiedPlanUUIDs list
    - Set CascadeResourceCheckDirty and CascadeStockTargetsDirty when any status changes
    - Wrap in try/catch with error logging, check `_stopping.IsSet` between plans
    - _Requirements: 1.1, 2.1, 9.1, 9.2, 9.3, 11.1, 12.3_

  - [~] 7.2 Write property test for cascade dirty flags
    - **Property 12: Cascade dirty flags set on status changes**
    - **Validates: Requirements 9.1, 9.2, 12.3**

  - [~] 7.3 Write unit tests for BackgroundProcessor integration
    - Test empty plan returns no modifications
    - Test single item lifecycle: Staged to Ready to InProgress to Completed across cascade cycles
    - Test that modified plan UUIDs are collected correctly
    - _Requirements: 9.1, 9.3, 12.1, 12.2, 12.3_

- [~] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Add FormBuildPlanner command buttons and context menu
  - [~] 9.1 Add "Start Manufacturing" button (`cmdStartManufacturing`) and context menu item (`tsmiStartManufacturing`) to the build planner
    - Add button to the command bar alongside existing cmdAllocate, cmdAutoAssign, cmdGenerateDelivery
    - Add context menu strip (`cmsBuildItems`) to dgvBuildItems with "Start Manufacturing" item
    - Both share the same click handler
    - Enable when selected item is Ready with valid StructureUUID and `CanStartManufacturing` returns true
    - On click: call `StartManufacturing`, show error MessageBox on failure, persist changes on success
    - Show warning if structure is busy (has active ProcessCompletionTime)
    - Show error if structure not found or not built and online
    - _Requirements: 3.1, 3.6, 3.7, 3.8, 8.1_

  - [~] 9.2 Add "Start All Ready" button (`cmdStartAllReady`) and context menu item (`tsmiStartAllReady`) to the build planner
    - Add button to the command bar
    - Add to the cmsBuildItems context menu strip
    - Both share the same click handler
    - Enable when selected plan has any Ready items
    - On click: call `StartAllReady`, show summary dialog with started/skipped counts and reasons, persist changes
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 10. Add status summary and busy indicators to FormBuildPlanner
  - [~] 10.1 Add status summary label (`lblStatusSummary`) to FormBuildPlanner
    - Display format: "Staged: X | Delivering: Y | Ready: Z | InProgress: W | Completed: V"
    - When all items Completed: "Complete (N items)"
    - Update on PopulateBuildItemsGrid and OnBuildPlanDataChanged
    - Call `BuildPlanExecutionService.ComputeStatusSummary` for data
    - _Requirements: 5.1, 5.2, 5.3, 13.1, 13.2, 13.3_

  - [~] 10.2 Add busy indicator formatting to build items grid
    - During PopulateBuildItemsGrid, for each Ready item check if assigned structure has active ProcessCompletionTime
    - If busy: set location cell BackColor to `Color.MistyRose` and append " [BUSY]" to location text
    - For cross-plan contention: call `DetectContention` and append competing plan name " [BUSY: Plan X]"
    - _Requirements: 6.1, 6.2, 6.3, 14.4_

- [~] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate the 12 universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The service follows the existing static-class pattern (like ResourceCheckService) with dependencies passed as delegates
- All property tests use the existing NUnit + randomized iteration pattern from BuildPlannerPropertyTests.cs (100 iterations minimum)
- Test file: `OE2EmpireTracker.Tests/Services/BuildPlanExecutionServiceTests.cs` for unit tests
- Test file: `OE2EmpireTracker.Tests/Services/BuildPlanExecutionPropertyTests.cs` for property tests
