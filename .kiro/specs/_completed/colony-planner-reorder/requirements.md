# Requirements Document

## Introduction

The Colony Planner web UI page (`ColonyPlanner.tsx`) currently allows users to add and remove structures from a plan, but provides no way to reorder them. Additionally, the "Optimize Build Order" action — which exists in the authenticated WinForms ColonyForm — is missing from the public planner page. This feature adds:

1. Manual reordering (move up/down) with Colony Command Centre protection
2. An "Optimize Build Order" button that calls the server's `/api/v1/colony-planner/build-order` endpoint
3. Server endpoint enhancement to wire the real `BuildOrderOptimizer` and return the optimized structure order

The planner page is public (no authentication required) and operates on a local in-memory plan — structures are not persisted server-side.

## Glossary

- **Colony_Planner**: The public web UI page at `/planner` that allows users to plan colony structures without authentication
- **Planner_Store**: The Zustand state store (`plannerStore.ts`) managing the list of planned structures and colony status
- **Structure_List**: The UI component (`StructureList.tsx`) rendering the ordered list of planned structures
- **Structure_Item**: The UI component (`StructureItem.tsx`) rendering a single structure with action buttons
- **Build_Order_Optimizer**: The server-side algorithm (`BuildOrderOptimizer.cs` in OE2EmpireTracker.Common) that reorders structures to satisfy resource constraints (power, habitation, food, entertainment) at every build step
- **Build_Order_Endpoint**: The server endpoint at `POST /api/v1/colony-planner/build-order` that accepts a list of structures and returns an optimized build order
- **Build_Queue_Position**: The numeric position (1-based) of a structure in the build order, stored as `buildQueuePosition` in `PlannedStructure`
- **Colony_Command_Centre**: The first structure in every colony plan (blueprint type `ColonyCommandCentre`); it must always remain at position 1 and cannot be moved
- **CC_Protection**: The rule that prevents any structure from moving above the Colony Command Centre and prevents the CC from moving down

## Wire Format

### Request: `POST /api/v1/colony-planner/build-order`

```json
{
  "structures": [
    {
      "flatpackBlueprintUUID": "77360093-351a-5af3-917a-c7f041c67814",
      "isBuilt": false,
      "isStaged": true,
      "isOnline": false,
      "buildQueueSequence": 1,
      "assignedWorkers": { "miner": true, "refiner": false }
    }
  ]
}
```

### Response (current stub — to be replaced):

```json
{
  "steps": [
    {
      "sequence": 1,
      "structureName": "ReactorCore",
      "blueprintType": "Structure",
      "resourcesRequired": [{ "resourceName": "Construction Materials", "quantity": 100 }],
      "timeEstimate": "1h 30m"
    }
  ],
  "totalTimeEstimate": "90m"
}
```

### Response (required — optimized order):

The endpoint must return the reordered structure list so the UI can reorder its local plan. The response adds an `optimizedOrder` array containing the `flatpackBlueprintUUID` and new `buildQueueSequence` for each structure in the optimized order:

```json
{
  "optimizedOrder": [
    { "flatpackBlueprintUUID": "cc-uuid-here", "buildQueueSequence": 0 },
    { "flatpackBlueprintUUID": "reactor-uuid", "buildQueueSequence": 1 },
    { "flatpackBlueprintUUID": "hab-uuid", "buildQueueSequence": 2 },
    { "flatpackBlueprintUUID": "primary-uuid", "buildQueueSequence": 3 }
  ],
  "steps": [
    {
      "sequence": 1,
      "structureName": "Reactor Core",
      "blueprintType": "Flatpacks/ReactorCore",
      "resourcesRequired": [{ "resourceName": "Construction Materials", "quantity": 100 }],
      "timeEstimate": "1h 30m"
    }
  ],
  "totalTimeEstimate": "4h 30m"
}
```

The `optimizedOrder` array may contain more structures than the request if the optimizer inserts support structures (reactors, habitation blocks, etc.) to satisfy resource constraints.

## Requirements

### Requirement 1: Manual Structure Reordering

**User Story:** As a player, I want to reorder structures in my colony plan using Up and Down buttons, so that I can control the build sequence manually.

#### Acceptance Criteria

1. WHEN the user clicks a "move up" button on a structure, THE Planner_Store SHALL swap the `buildQueuePosition` of that structure with the structure immediately above it in the ordered list
2. WHEN the user clicks a "move down" button on a structure, THE Planner_Store SHALL swap the `buildQueuePosition` of that structure with the structure immediately below it in the ordered list
3. WHILE a structure is at position 1 (or position 2 when a Colony_Command_Centre occupies position 1), THE Structure_Item SHALL disable the "move up" button for that structure
4. WHILE a structure is at the last position in the list, THE Structure_Item SHALL disable the "move down" button for that structure
5. WHILE a structure is the only item in the queue, THE Structure_Item SHALL disable both the "move up" and "move down" buttons for that structure
6. WHILE a structure is the Colony_Command_Centre, THE Structure_Item SHALL disable both the "move up" and "move down" buttons for that structure
7. WHEN any structure other than the Colony_Command_Centre is at position 2 and the Colony_Command_Centre is at position 1, THE Structure_Item SHALL disable the "move up" button for that structure (CC_Protection: nothing can move above CC)
8. WHEN a structure is reordered, THE Planner_Store SHALL recalculate and update the colony status using `computeColonyStatus`
9. THE Structure_List SHALL render structures sorted by `buildQueuePosition` ascending

### Requirement 2: Optimize Build Order

**User Story:** As a player, I want to optimize the build order of my planned structures, so that the server can determine a construction sequence that satisfies resource constraints at every build step.

#### Acceptance Criteria

1. THE Colony_Planner SHALL display an "Optimize Build Order" button in the planner actions area
2. WHEN the user clicks the "Optimize Build Order" button, THE Colony_Planner SHALL send a POST request to the Build_Order_Endpoint with the current structures mapped to the wire format (`flatpackBlueprintUUID`, `isBuilt`, `isStaged`, `isOnline`, `buildQueueSequence`, `assignedWorkers`)
3. WHEN the Build_Order_Endpoint returns a successful response containing `optimizedOrder`, THE Planner_Store SHALL reorder its local structures by matching each entry's `flatpackBlueprintUUID` to the corresponding `PlannedStructure` and updating its `buildQueuePosition` to the returned `buildQueueSequence`
4. WHEN the Build_Order_Endpoint returns structures in `optimizedOrder` that do not exist in the local plan (optimizer-inserted support structures), THE Colony_Planner SHALL ignore those entries (the local plan only reorders existing structures)
5. WHILE the optimize request is in progress, THE Colony_Planner SHALL disable the button and display a loading indicator
6. WHILE the structure list is empty, THE Colony_Planner SHALL disable the "Optimize Build Order" button
7. WHILE the structure list contains only one structure, THE Colony_Planner SHALL disable the "Optimize Build Order" button
8. IF the Build_Order_Endpoint returns an HTTP error or network failure, THEN THE Colony_Planner SHALL hide the loading indicator and display an error message to the user without modifying the structure list
9. WHEN the structures are reordered by the optimizer response, THE Planner_Store SHALL recalculate and update the colony status using `computeColonyStatus`

### Requirement 3: Server Endpoint Enhancement

**User Story:** As a developer, I want the `/api/v1/colony-planner/build-order` endpoint to use the real `BuildOrderOptimizer` algorithm, so that the web planner returns genuinely optimized build orders.

#### Acceptance Criteria

1. WHEN the Build_Order_Endpoint receives a valid request, THE Build_Order_Endpoint SHALL invoke the `BuildOrderOptimizer.Optimize` method from OE2EmpireTracker.Common with the submitted structures
2. WHEN the optimizer produces a reordered list, THE Build_Order_Endpoint SHALL return an `optimizedOrder` array containing each structure's `flatpackBlueprintUUID` and its new `buildQueueSequence` (0-based position in the optimized list)
3. WHEN the optimizer inserts support structures not present in the original request, THE Build_Order_Endpoint SHALL include those structures in the `optimizedOrder` array with their generated `flatpackBlueprintUUID`
4. THE Build_Order_Endpoint SHALL continue to return the `steps` array and `totalTimeEstimate` field in successful responses for backward compatibility
5. IF the request body is missing, contains invalid JSON, or has validation failures, THEN THE Build_Order_Endpoint SHALL return HTTP 400 with an error message identifying all validation failures present in the request (without `steps` or `totalTimeEstimate` fields)
6. IF the `structures` field is missing or empty, THEN THE Build_Order_Endpoint SHALL return HTTP 400 with an error message
7. THE Build_Order_Endpoint SHALL remain publicly accessible (no authentication required)
8. THE Build_Order_Endpoint SHALL reference the `BuildOrderOptimizer` from the OE2EmpireTracker.Common project (the server project must add a project reference to Common if not already present)
