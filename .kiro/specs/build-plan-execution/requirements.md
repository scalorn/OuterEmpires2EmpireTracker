# Requirements Document

## Introduction

The Build Planner currently automates the early stages of the build lifecycle: planning items, allocating structures, checking resources, generating delivery plans, and detecting when resources arrive (Delivering to Ready). However, once a build item reaches Ready status, the lifecycle stalls. The user must manually navigate to the colony form, find the correct manufactory, select the blueprint, set the quantity, click Start, wait for completion, then handle staging and building -- all without any connection back to the build planner.

This feature closes the gap by automating the detection of manufacturing status transitions: Ready to InProgress (manufacturing detected on structure), InProgress to Completed (manufacturing finished). It also adds a UI action in the build planner to pre-configure a structure for manufacturing (so the tool knows what to expect when the user starts the job in-game), provides progress visibility, and respects dependencies and structure sequencing. The build planner becomes a status tracker for the full manufacturing lifecycle rather than stopping at resource delivery.

**Scope boundary:** This feature stops at Completed (manufacturing done, output exists in the source colony warehouse). Flatpack delivery to destination colonies, staging, and building are separate workflows handled via existing delivery generation and colony form UI. Full automation of downstream steps would be via stock level plans.

## Glossary

- **Build_Planner**: The FormBuildPlanner form and its associated services that manage build plans and build items across the player's empire.
- **Build_Item**: A single entry in a build plan (BuildItem model) representing work to be done -- manufacturing, commodity production, mining, refining, or research.
- **Build_Plan**: A named collection of build items (BuildPlan model) owned by a player, tracking a batch of manufacturing work.
- **Background_Processor**: The BackgroundProcessor service that runs on a configurable timer cycle (default 60 seconds) and processes colony timers, cascade resource checks, and status transitions.
- **Colony_Structure**: A ColonyStructure instance representing a physical structure at a colony (manufactory, refinery, mining rig, etc.).
- **Manufactory**: A colony structure of BlueprintType Manufactory that can manufacture flatpacks from blueprints.
- **Commodity_Factory**: A colony structure of a commodity factory BlueprintType that produces commodities.
- **Research_Lab**: A colony structure of BlueprintType ResearchLaboratory that researches blueprints.
- **Flatpack**: The output of manufacturing -- a packaged structure that can be staged and built at a colony.
- **ProcessCompletionTime**: A CountDownTime on a ColonyStructure tracking when the current manufacturing/refining/research job completes.
- **BuildCompletionTime**: A CountDownTime on a ColonyStructure tracking when a staged structure finishes building.
- **Resource_Check_Cascade**: The background process that recomputes resource shortfalls for active build plans and advances Delivering items to Ready when shortfalls resolve.
- **TryAdvanceStatus**: The BackgroundProcessor method that advances a BuildItem status forward (never backward) based on ordinal value: Staged(0) to Delivering(1) to Ready(2) to InProgress(3) to Completed(4).

## Complete Build Item Lifecycle Flow

The diagram below shows the full build item lifecycle. Transitions marked **(existing)** are already implemented. Transitions marked **(this feature)** are what this spec adds.

```
                                BUILD ITEM LIFECYCLE
                                ====================

    ┌──────────┐
    │  Staged  │  Item added to build plan, not yet allocated to a structure
    └────┬─────┘
         │
         │  (existing) User allocates item to a colony + manufactory
         │             via Allocate dialog or Auto-Assign
         │             Sets BuildLocationUUID + StructureUUID
         │             Item is now "staged" on a specific structure
         │
         │  NOTE: If the target structure is already busy (has an active
         │  ProcessCompletionTime) or has other build items queued ahead
         │  (lower SequenceInStructure), this item waits in the queue.
         │  Multiple items can be allocated to the same manufactory --
         │  they execute in SequenceInStructure order.
         │
         │  Resource delivery is only needed if resources are short.
         │  If resources are already present at the colony, the item
         │  can skip Delivering and go directly to Ready.
         │
         ├──── PATH A: Resources needed ────────────────────────────────────┐
         │     (existing) User clicks "Generate Resource Delivery"          │
         │     DeliveryGenerationService creates delivery plan              │
         │     Sets item status to Delivering                               │
         │                                                                  │
         │  ┌────────────────┐                                              │
         │  │  Delivering    │  Resources being shipped to build location   │
         │  └────┬───────────┘                                              │
         │       │                                                          │
         │       │  (existing) BackgroundProcessor cascade detects          │
         │       │  zero shortfalls → TryAdvanceStatus to Ready             │
         │       │                                                          │
         ├───────▼──────────────────────────────────────────────────────────┘
         │
         │  PATH B: Resources already present
         │  (this feature) BackgroundProcessor cascade detects zero
         │  shortfalls on a Staged+allocated item → advances to Ready
         │
    ┌────▼─────┐
    │  Ready   │  Resources available. Structure may or may not be free.
    └────┬─────┘
         │
         │  The user must start the manufacturing job in-game.
         │  The build planner can pre-configure the structure in the
         │  tracker so it knows what to expect, but the actual job
         │  must be started in the game UI (until API integration).
         │
         ├──── PATH A: Start in-game, detected by tool ───────────────────┐
         │     User starts the job in-game on the manufactory.             │
         │     (this feature) BackgroundProcessor detects matching          │
         │     active job on the structure (ProcessCompletionTime +        │
         │     matching BlueprintUUID/CommodityName) and advances          │
         │     to InProgress.                                              │
         │                                                                  │
         ├──── PATH B: Pre-configure from Build Planner (this feature) ────┤
         │     User clicks "Start Manufacturing" in the build planner.     │
         │     Build Planner pre-configures the Colony_Structure in the    │
         │     tracker: sets BlueprintUUID/CommodityName, Quantity,        │
         │     and computes ProcessCompletionTime.                         │
         │     Advances item to InProgress immediately in the tracker.     │
         │     User then starts the matching job in-game to sync up.      │
         │                                                                  │
    ┌────▼──────────┐                                                       │
    │  InProgress    │◄─────────────────────────────────────────────────────┘
    │                │  Manufacturing/refining/research is running
    │                │  Colony.ProcessColony handles the timer ticks
    └────┬──────────┘
         │
         │  (this feature) BackgroundProcessor cascade cycle detects:
         │    - ProcessCompletionTime is null (job finished), OR
         │    - ManufacturingCompleted >= Build_Item.Quantity
         │    - ManufacturingBlueprintUUID is empty (structure cleared)
         │  TryAdvanceStatus advances to Completed
         │
    ┌────▼──────────┐
    │  Completed     │  Manufacturing done. Flatpack/output exists.
    └────┬──────────┘
         │
         │  (this feature) LOOP BACK: When an item completes,
         │  the next item in SequenceInStructure on the same
         │  structure becomes eligible to start. The background
         │  processor checks if the structure is now free and
         │  the next Ready item can begin.
         │
         │  This continues until all items on the structure
         │  are Completed.
         │
         ▼
    ┌─────────────────────────────────────────────────────────────┐
    │  BUILD PLAN COMPLETION                                       │
    │                                                             │
    │  A Build_Plan is complete ONLY when ALL Build_Items in the  │
    │  plan have Status = Completed. Until then, the plan remains │
    │  active and the background processor continues monitoring.  │
    │                                                             │
    │  (future / out of scope for this feature)                   │
    │  Flatpack delivery to destination colony, staging, and      │
    │  building are handled via existing colony form UI and       │
    │  delivery generation (GenerateFlatpackDeliveryPlan).        │
    └─────────────────────────────────────────────────────────────┘
```

### Manufactory Contention: Multiple Items on Same Structure

```
    ┌──────────────────────────────────────────────────────────────┐
    │  Manufactory Alpha (colony: Outpost Beta)                    │
    │                                                              │
    │  Build Plan "Reactor Expansion":                             │
    │    Seq 0: Reactor Mk2 x5    (Ready)    ◄── can start now    │
    │    Seq 1: Reactor Mk3 x3    (Ready)        waits for Seq 0  │
    │    Seq 2: Power Cell x10    (Delivering)    resources coming │
    │                                                              │
    │  Current state: structure is idle (no ProcessCompletionTime) │
    │  "Start All Ready" would start Seq 0 only.                  │
    │                                                              │
    │  LOOP: After Seq 0 completes:                                │
    │    → Structure becomes free                                  │
    │    → Seq 1 is now lowest-seq Ready item → eligible to start  │
    │    → User starts Seq 1 (manual or via build planner)         │
    │    → After Seq 1 completes, Seq 2 must reach Ready first    │
    │      (resources must arrive), then it becomes eligible       │
    └──────────────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────────────────────────┐
    │  Manufactory Alpha (colony: Outpost Beta)                    │
    │                                                              │
    │  BUSY: Currently manufacturing "Shield Gen x2" from another  │
    │  build plan (or started manually in colony form)             │
    │                                                              │
    │  Build Plan "Reactor Expansion":                             │
    │    Seq 0: Reactor Mk2 x5    (Ready)    ◄── BLOCKED (busy)   │
    │    Seq 1: Reactor Mk3 x3    (Ready)        waits for Seq 0  │
    │                                                              │
    │  "Start Manufacturing" on Seq 0 shows warning: structure     │
    │  is busy. Grid shows busy indicator on location cell.        │
    │  When Shield Gen completes, Seq 0 becomes startable.         │
    └──────────────────────────────────────────────────────────────┘
```

### Dependency and Sequence Constraints

```
    Ready item with DependsOnUUID set:
    ┌─────────┐     depends on     ┌─────────────┐
    │ Item B  │ ──────────────────►│   Item A     │
    │ (Ready) │                    │ (must be     │
    │ BLOCKED │                    │  Completed)  │
    └─────────┘                    └──────────────┘
    Item B cannot advance to InProgress until Item A is Completed.
    This is independent of structure sequencing -- even if Item B
    is Seq 0 on its structure, it waits for Item A.
```

### Cross-Plan Contention

```
    ┌──────────────────────────────────────────────────────────────┐
    │  Manufactory Alpha (colony: Outpost Beta)                    │
    │                                                              │
    │  Build Plan "Reactor Expansion":                             │
    │    Seq 0: Reactor Mk2 x5    (Ready)                         │
    │                                                              │
    │  Build Plan "Shield Production":                             │
    │    Seq 0: Shield Gen x3     (Ready)                         │
    │                                                              │
    │  Both plans want the same manufactory!                       │
    │                                                              │
    │  Resolution: First-come, first-served.                       │
    │  User clicks "Start Manufacturing" on whichever plan they    │
    │  want to run first. The other plan's item shows as BLOCKED   │
    │  with a busy indicator showing which plan occupies it.       │
    │                                                              │
    │  Warning shown at allocation time: "This structure also has  │
    │  items from plan 'Shield Production'."                       │
    └──────────────────────────────────────────────────────────────┘
```

### Background Processor Cascade Integration

```
    ┌─────────────────────────────────────────────────────────────┐
    │  BackgroundProcessor.ExecuteCycle()                          │
    │                                                             │
    │  1. Process colony timers (mining, refining, manufacturing) │
    │     └─► Colony.ProcessColony() handles timer expiration     │
    │         Manufacturing completes → clears ProcessCompletion  │
    │         Time, increments ManufacturingCompleted              │
    │                                                             │
    │  2. Process cascades (existing + this feature)              │
    │     ├─► CascadeResourceCheckDirty:                          │
    │     │   ├─► (existing) Delivering → Ready                   │
    │     │   │   when shortfalls resolve                         │
    │     │   ├─► (this feature) Staged+allocated → Ready         │
    │     │   │   when resources already present (no delivery)    │
    │     │   ├─► (this feature) Ready → InProgress               │
    │     │   │   when structure has matching active job           │
    │     │   │   AND item is lowest seq on structure              │
    │     │   │   AND dependency (if any) is Completed             │
    │     │   └─► (this feature) InProgress → Completed           │
    │     │       when structure job finishes                      │
    │     │       → triggers loop: next seq item eligible          │
    │     │                                                       │
    │     └─► CascadeStockTargetsDirty:                           │
    │         └─► (existing) Replenishment item generation        │
    │                                                             │
    │  3. Check warehouse overflow (existing)                     │
    │  4. Check supply chain thresholds (existing)                │
    │  5. Persist and fire events                                 │
    └─────────────────────────────────────────────────────────────┘
```

## Requirements

### Requirement 1: Detect Manufacturing Start and Advance to InProgress

**User Story:** As a player, I want the build planner to detect when manufacturing has started on a structure assigned to a build item, so that the build item status automatically advances to InProgress without manual intervention.

#### Acceptance Criteria

1. WHILE the Background_Processor is running a cascade cycle, THE Background_Processor SHALL check each active Build_Plan for Build_Items with Status equal to Ready.
2. WHEN a Ready Manufactory Build_Item has a StructureUUID that references a Colony_Structure with a non-null ProcessCompletionTime and a ManufacturingBlueprintUUID matching the Build_Item BlueprintUUID, THE Background_Processor SHALL advance the Build_Item status to InProgress.
3. WHEN a Ready Commodity Build_Item has a StructureUUID that references a Colony_Structure with a non-null ProcessCompletionTime and a ManufacturingCommodityName matching the Build_Item CommodityName, THE Background_Processor SHALL advance the Build_Item status to InProgress.
4. WHEN a Ready Research Build_Item has a StructureUUID that references a Colony_Structure with a non-null ProcessCompletionTime and a ResearchingBlueprintUUID matching the Build_Item BlueprintUUID, THE Background_Processor SHALL advance the Build_Item status to InProgress.
5. THE Background_Processor SHALL use TryAdvanceStatus to ensure status transitions only move forward.
6. WHEN a Build_Item status is advanced to InProgress, THE Background_Processor SHALL log the transition including the Build_Item UUID, plan name, and structure UUID.

### Requirement 2: Detect Manufacturing Completion and Advance to Completed

**User Story:** As a player, I want the build planner to detect when manufacturing finishes on a structure assigned to a build item, so that the build item status automatically advances to Completed.

#### Acceptance Criteria

1. WHILE the Background_Processor is running a cascade cycle, THE Background_Processor SHALL check each active Build_Plan for Build_Items with Status equal to InProgress.
2. WHEN an InProgress Manufactory Build_Item has a StructureUUID that references a Colony_Structure where ProcessCompletionTime is null and ManufacturingBlueprintUUID is empty, THE Background_Processor SHALL advance the Build_Item status to Completed.
3. WHEN an InProgress Manufactory Build_Item has a StructureUUID that references a Colony_Structure where ManufacturingCompleted is greater than or equal to the Build_Item Quantity, THE Background_Processor SHALL advance the Build_Item status to Completed.
4. WHEN an InProgress Commodity Build_Item has a StructureUUID that references a Colony_Structure where ProcessCompletionTime is null and ManufacturingCommodityName is empty, THE Background_Processor SHALL advance the Build_Item status to Completed.
5. WHEN an InProgress Research Build_Item has a StructureUUID that references a Colony_Structure where ProcessCompletionTime is null and ResearchingBlueprintUUID is empty, THE Background_Processor SHALL advance the Build_Item status to Completed.
6. THE Background_Processor SHALL use TryAdvanceStatus to ensure status transitions only move forward.
7. WHEN a Build_Item status is advanced to Completed, THE Background_Processor SHALL log the transition including the Build_Item UUID, plan name, and structure UUID.

### Requirement 3: Pre-Configure Manufacturing from Build Planner

**User Story:** As a player, I want to pre-configure a manufactory from the build planner for Ready build items, so that the tool knows what blueprint and quantity to expect when I start the job in-game, and I do not have to navigate to the colony form to set it up.

**Note:** Until game API integration exists, the user must manually start the manufacturing job in the game UI. The "Start Manufacturing" action in the build planner configures the tracker's data model (sets the blueprint, quantity, and timer on the ColonyStructure) so the tool can track progress. The user then starts the matching job in-game to sync up.

#### Acceptance Criteria

1. WHEN a Build_Item has Status equal to Ready and a valid StructureUUID, THE Build_Planner SHALL enable a "Start Manufacturing" action for that item.
2. WHEN the user activates "Start Manufacturing" on a Ready Manufactory Build_Item, THE Build_Planner SHALL set the Colony_Structure ManufacturingBlueprintUUID to the Build_Item BlueprintUUID and ManufacturingQuantity to the Build_Item Quantity.
3. WHEN the user activates "Start Manufacturing" on a Ready Commodity Build_Item, THE Build_Planner SHALL set the Colony_Structure ManufacturingCommodityName to the Build_Item CommodityName and ManufacturingQuantity to the Build_Item Quantity.
4. WHEN the user activates "Start Manufacturing" on a Ready Research Build_Item, THE Build_Planner SHALL set the Colony_Structure ResearchingBlueprintUUID to the Build_Item BlueprintUUID.
5. WHEN "Start Manufacturing" sets up a Colony_Structure, THE Build_Planner SHALL compute the ProcessCompletionTime using the appropriate time calculation for the item type and start the timer in the tracker.
6. WHEN "Start Manufacturing" succeeds, THE Build_Planner SHALL advance the Build_Item status to InProgress and persist the changes.
7. IF the Colony_Structure already has an active ProcessCompletionTime when "Start Manufacturing" is activated, THEN THE Build_Planner SHALL display a warning that the structure is busy and not modify the structure.
8. IF the Colony_Structure referenced by StructureUUID is not found or not built and online, THEN THE Build_Planner SHALL display an error message and not modify the Build_Item status.

### Requirement 4: Batch Start Manufacturing

**User Story:** As a player, I want to start manufacturing on multiple Ready build items at once, so that I can kick off an entire build plan's manufacturing in one action.

#### Acceptance Criteria

1. THE Build_Planner SHALL provide a "Start All Ready" action that operates on all Ready Build_Items in the selected Build_Plan.
2. WHEN the user activates "Start All Ready", THE Build_Planner SHALL attempt to start manufacturing on each Ready Build_Item that has a valid StructureUUID.
3. WHEN a Build_Item assigned Colony_Structure already has an active ProcessCompletionTime, THE Build_Planner SHALL skip that item and continue with the remaining items.
4. WHEN "Start All Ready" completes, THE Build_Planner SHALL display a summary showing how many items were started and how many were skipped.
5. THE Build_Planner SHALL persist all changes after the batch operation completes.

### Requirement 5: Build Plan Status Summary

**User Story:** As a player, I want to see an at-a-glance summary of my build plan's progress, so that I can quickly understand how far along the plan is.

#### Acceptance Criteria

1. THE Build_Planner SHALL display a status summary for the selected Build_Plan showing the count of items in each status (Staged, Delivering, Ready, InProgress, Completed).
2. WHEN any Build_Item status changes in the selected plan, THE Build_Planner SHALL update the status summary.
3. WHEN all Build_Items in a Build_Plan have Status equal to Completed, THE Build_Planner SHALL display the plan as fully complete.

### Requirement 6: Structure Busy Detection

**User Story:** As a player, I want the build planner to show me which assigned structures are currently busy, so that I know which items are blocked waiting for a structure.

#### Acceptance Criteria

1. THE Build_Planner SHALL display a busy indicator in the build items grid for items whose assigned Colony_Structure has an active ProcessCompletionTime.
2. WHEN a Build_Item has Status equal to Ready and its assigned Colony_Structure has an active ProcessCompletionTime from a different job, THE Build_Planner SHALL display the item location cell with a visual indicator that the structure is occupied.
3. THE Build_Planner SHALL resolve the busy state by checking the Colony_Structure ProcessCompletionTime during grid population.

### Requirement 7: Dependency-Aware Status Transitions

**User Story:** As a player, I want build items with dependencies to wait until their dependency is completed before advancing, so that manufacturing happens in the correct order.

#### Acceptance Criteria

1. WHEN a Build_Item has a non-empty DependsOnUUID, THE Background_Processor SHALL verify the dependency item has Status equal to Completed before advancing the dependent item past Ready.
2. WHEN a Build_Item has a non-empty DependsOnUUID and the dependency item is not Completed, THE Background_Processor SHALL not advance the dependent item to InProgress even if all other conditions are met.
3. IF a Build_Item DependsOnUUID references an item that does not exist in the same Build_Plan, THEN THE Background_Processor SHALL log a warning and treat the dependency as satisfied.

### Requirement 8: Sequence-Aware Manufacturing Start

**User Story:** As a player, I want build items assigned to the same structure to be started in sequence order, so that the manufactory processes items in the planned order.

#### Acceptance Criteria

1. WHEN multiple Build_Items are assigned to the same Colony_Structure (same StructureUUID), THE Build_Planner SHALL only allow "Start Manufacturing" on the item with the lowest SequenceInStructure among Ready items for that structure.
2. WHEN "Start All Ready" processes items, THE Build_Planner SHALL process items grouped by StructureUUID in SequenceInStructure order, starting only the first Ready item per structure.
3. WHEN a Build_Item completes and the next item in sequence for the same structure is Ready, THE Background_Processor SHALL log that the next item is eligible for manufacturing.

### Requirement 9: Build Plan Cascade Dirty Flag Integration

**User Story:** As a player, I want build plan status transitions to trigger resource check cascades, so that downstream plans and stock targets stay up to date.

#### Acceptance Criteria

1. WHEN the Background_Processor advances any Build_Item status during a cascade cycle, THE Background_Processor SHALL set CascadeResourceCheckDirty to true so that subsequent cycles recheck resource availability.
2. WHEN a Build_Item status is advanced to Completed, THE Background_Processor SHALL set CascadeStockTargetsDirty to true so that stock target replenishment rechecks.
3. WHEN the Background_Processor modifies Build_Item statuses, THE Background_Processor SHALL fire BuildPlanDataChanged for each modified plan UUID.

### Requirement 10: Mining and Refining Build Item Completion Detection

**User Story:** As a player, I want mining and refining build items to be tracked through to completion, so that the build planner reflects the full status of all item types.

#### Acceptance Criteria

1. WHEN an InProgress Mining Build_Item has a StructureUUID that references a Colony_Structure where ProcessCompletionTime is null, THE Background_Processor SHALL advance the Build_Item status to Completed.
2. WHEN an InProgress Refining Build_Item has a StructureUUID that references a Colony_Structure where ProcessCompletionTime is null, THE Background_Processor SHALL advance the Build_Item status to Completed.
3. THE Background_Processor SHALL handle Mining and Refining item types using the same detection pattern as Manufactory items: check ProcessCompletionTime and active job state on the assigned structure.

### Requirement 11: Skip Delivering When Resources Already Present

**User Story:** As a player, I want allocated build items to advance directly to Ready when resources are already available at the colony, so that I do not have to generate a delivery plan for items that do not need one.

#### Acceptance Criteria

1. WHILE the Background_Processor is running a cascade cycle, THE Background_Processor SHALL check each active Build_Plan for Build_Items with Status equal to Staged that have a non-empty BuildLocationUUID and StructureUUID.
2. WHEN a Staged and allocated Build_Item has zero resource shortfalls at its build location, THE Background_Processor SHALL advance the Build_Item status to Ready.
3. THE Background_Processor SHALL not advance Staged items that have no BuildLocationUUID or no StructureUUID (unallocated items remain Staged).

### Requirement 12: Structure Queue Loop-Back on Completion

**User Story:** As a player, I want the next queued item on a structure to become eligible for manufacturing automatically when the current item completes, so that the manufacturing queue progresses without manual monitoring.

#### Acceptance Criteria

1. WHEN a Build_Item status is advanced to Completed, THE Background_Processor SHALL check the same Build_Plan for other Build_Items assigned to the same StructureUUID with Status equal to Ready.
2. WHEN the next Ready Build_Item in SequenceInStructure order exists for the same structure, THE Background_Processor SHALL log that the item is now eligible for manufacturing.
3. THE Background_Processor SHALL set CascadeResourceCheckDirty to true after completing an item so that the next cascade cycle evaluates the newly eligible item.

### Requirement 13: Build Plan Completion Tracking

**User Story:** As a player, I want the build plan to be marked complete only when every item in the plan has finished, so that I have a clear signal that all planned work is done.

#### Acceptance Criteria

1. THE Build_Planner SHALL consider a Build_Plan complete only when all Build_Items in the plan have Status equal to Completed.
2. WHEN all Build_Items in a Build_Plan reach Completed status, THE Build_Planner SHALL display the plan with a "Complete" indicator in the status summary.
3. WHILE any Build_Item in a Build_Plan has a Status other than Completed, THE Build_Planner SHALL display the plan as in-progress with the per-status item counts.

### Requirement 14: Cross-Plan Manufactory Contention

**User Story:** As a player, I want to be warned when multiple build plans compete for the same manufactory, so that I can make informed decisions about which work to prioritize.

#### Acceptance Criteria

1. WHEN the user allocates a Build_Item to a Colony_Structure that already has Build_Items from a different Build_Plan, THE Build_Planner SHALL display a warning identifying the competing plan and item names.
2. THE Build_Planner SHALL allow the allocation to proceed after the warning -- contention is informational, not blocking.
3. WHEN the user activates "Start Manufacturing" on a Ready Build_Item, THE Build_Planner SHALL use first-come-first-served ordering: whichever item is started first gets the structure.
4. WHEN a Colony_Structure is busy with a job from a different Build_Plan, THE Build_Planner SHALL display the busy indicator (Requirement 6) showing which plan and item currently occupies the structure.
5. THE Build_Planner SHALL resolve cross-plan contention by checking all active Build_Plans for items assigned to the same StructureUUID during allocation.
