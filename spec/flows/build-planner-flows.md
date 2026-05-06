# User Flows — Build Planner

### Flow 1: Build Planner â€” Manufacturing Workflow

```mermaid
sequenceDiagram
    actor User
    participant BP as Build Planner
    participant RC as Resource Check
    participant DG as Delivery Gen
    participant DE as Delivery Execution
    participant BG as Background Processor

    User->>BP: Create build plan
    User->>BP: Add items (blueprint/commodity, qty)
    User->>BP: Allocate items to structures
    BP->>RC: Check resource availability
    RC-->>BP: Shortfalls per item
    BP-->>User: Display shortfalls

    User->>BP: Generate delivery plan
    BP->>DG: Create plan for shortfalls
    DG-->>BP: Delivery plan created
    BP->>BP: Update item status â†’ Delivering

    User->>DE: Execute delivery (in-game)
    DE->>DE: Mark items delivered
    Note over BG: Next tick
    BG->>RC: Re-check resource availability
    RC-->>BG: Shortfalls resolved
    BG->>BG: Update item status â†’ Ready
    BG-->>User: Inactivity: "Ready to start"

    User->>BP: Mark item In Progress (started in-game)
    Note over BG: Timer ticking
    BG-->>User: Activity: "Completes in 2h 15m"

    User->>BP: Mark item Completed
```

### Flow 2: Ship Build â€” Template to Assembly

```mermaid
sequenceDiagram
    actor User
    participant ST as Ship Template
    participant BP as Build Planner
    participant RC as Resource Check
    participant DG as Delivery Gen

    User->>ST: Create ship template (hull + components)
    User->>ST: Order build â†’ select assembly station

    ST->>ST: Check stock for each component
    ST->>BP: Create build items for missing components
    Note right of BP: Hull, reactor, drive,<br/>weapons, cargo pods...

    loop For each build item
        BP->>RC: Check resource availability
        RC-->>BP: Shortfalls
    end

    User->>BP: Generate delivery plan (resources)
    BP->>DG: Create plan for resource shortfalls

    Note over User: Manufacture components in-game
    User->>BP: Mark components completed

    BP->>DG: Generate delivery plan (components â†’ assembly station)
    Note over User: Deliver components, assemble ship in-game
    User->>BP: Mark ship build completed
```

### Flow 6: Queue Calculator

```mermaid
flowchart LR
    A[User enters target duration<br/>'2d 12h 0m 0s'] --> B[Parse to seconds<br/>216000s]
    B --> C{Item type?}
    C -->|Manufactory| D[Blueprint mfg time: 9h = 32400s]
    D --> E["ceiling(216000 / 32400) = 7 runs"]
    E --> E2["7 runs Ã— items per run"]
    C -->|Commodity| F[Cycle time: 600s]
    F --> G["ceiling(216000 / 600) = 360 runs"]
    G --> H["360 Ã— 10 = 3600 items produced"]
    E2 --> I[Populate quantity field with runs]
    H --> I
```

### Flow 16: Colony Plan â†’ Build Plan Generation

```mermaid
sequenceDiagram
    actor User
    participant CF as Colony Form (Admin tab)
    participant BPS as BuildPlanService
    participant BP as Build Planner
    participant PC as PlayerContext

    Note over User: Colony has unstaged structures<br/>(added via Bootstrap or manually,<br/>flatpacks not yet delivered)

    User->>CF: Click [Generate Build Plan]
    CF->>CF: Prompt: create new plan or add to existing?
    alt New plan
        CF->>CF: Auto-name: "Colony Name - Build Plan"
    else Existing plan
        CF->>CF: Show plan picker (filtered combo)
    end

    CF->>BPS: GenerateColonyBuildItems(colony, targetPlan)
    BPS->>BPS: Scan colony.Structures for unstaged (not Staged, not Built)
    BPS->>BPS: For each: create Manufactory BuildItem
    Note right of BPS: BlueprintUUID = structure.FlatpackBlueprintUUID<br/>Quantity = 1 per structure<br/>BuildLocationType = Colony (any mfg colony)<br/>BuildLocationUUID = empty (unallocated)<br/>Status = Staged
    BPS->>BPS: Skip structures already covered by existing items in plan
    BPS-->>CF: Return count of items added

    CF->>PC: WriteContext()
    CF-->>User: "5 flatpack build items added to plan"

    Note over User: Open Build Planner to allocate,<br/>check resources, generate deliveries
```

### Flow 17: Multi-Plan Consolidated Delivery

```mermaid
sequenceDiagram
    actor User
    participant BP as Build Planner
    participant RC as Resource Check
    participant DG as Delivery Gen
    participant DE as Delivery Execution

    Note over User: Has per-colony build plans:<br/>Plan A (Colony Alpha flatpacks)<br/>Plan B (Colony Beta flatpacks)<br/>Plan C (Colony Gamma flatpacks)<br/>All items allocated to mfg colonies

    Note over User: Phase 1: Resource Delivery
    User->>BP: Generate Delivery â–¼ â†’ Consolidated Resource Delivery
    BP->>BP: Show plan checklist (check A, B, C)
    BP->>RC: ComputePlanShortfalls for each selected plan
    RC-->>BP: Shortfalls per plan
    BP->>DG: GenerateConsolidatedDeliveryPlan(plans, route)
    DG->>DG: Merge shortfalls by mfg colony
    Note right of DG: Colony X needs 800 Titanium<br/>(500 from Plan A + 300 from Plan B)
    DG-->>BP: One consolidated delivery plan
    BP-->>User: "Resource delivery plan created"
    User->>DE: Execute resource delivery in-game

    Note over User: Phase 2: Manufacturing
    Note over User: Manufacture flatpacks at mfg colonies
    User->>BP: Mark items Completed as they finish

    Note over User: Phase 3: Flatpack Delivery
    User->>BP: Generate Delivery â–¼ â†’ Flatpack Delivery
    BP->>BP: Show plan checklist (check A, B, C)
    BP->>DG: GenerateFlatpackDeliveryPlan(plans, route)
    DG->>DG: Scan Completed items, group by destination colony
    Note right of DG: Colony Alpha gets 3 flatpacks<br/>Colony Beta gets 2 flatpacks<br/>Colony Gamma gets 4 flatpacks
    DG-->>BP: One flatpack delivery plan
    BP-->>User: "Flatpack delivery plan created"
    User->>DE: Execute flatpack delivery in-game
    User->>DE: Mark flatpacks delivered â†’ structures staged
```

