# Colony User Interaction Flows

### Colony Selection and Display

```mermaid
sequenceDiagram
    actor User
    participant Form as FormColonyV2
    participant VM as ColonyViewModel
    participant Calc as ColonyStatusCalculator
    participant PC as PlayerContext

    User->>Form: Type in filter textbox
    Form->>PC: Filter ColonyList by name substring
    Form->>Form: Repopulate lvwColonies

    User->>Form: Click colony in list
    Form->>PC: Find colony by UUID
    Form->>VM: Create ColonyViewModel(colony)
    Form->>Form: Populate txtPlanetName, txtColonyName, txtSystemName
    Form->>VM: RecalculateStatus()
    VM->>Calc: CalculateBuilt(colony)
    VM->>Calc: CalculateIdeal(colony)
    Form->>Form: Create ColonyStructureV2 controls
    Form->>Form: Populate Items grid, Commodity Requests grid
    Form->>Form: Refresh Admin report
```

### Structure Add / Reorder / Delete

```mermaid
sequenceDiagram
    actor User
    participant Form as FormColonyV2
    participant VM as ColonyViewModel
    participant SVM as ColonyStructureViewModel
    participant Calc as ColonyStatusCalculator

    alt Add Structure
        User->>Form: Filter flatpacks, select from cmbFlatpacks
        User->>Form: Click [Add]
        Form->>VM: AddStructure(blueprintUUID)
        VM->>VM: Create ColonyStructure, set Staged=true
        VM->>Calc: RecalculateStatus()
        Form->>Form: Create new ColonyStructureV2 control
    else Reorder
        User->>Form: Click [▲] or [▼] on structure
        Form->>SVM: MoveUp() / MoveDown()
        Note over SVM: CC protected at position 0
        SVM->>Calc: RecalculateStatus()
        Form->>Form: Reorder controls in flpStructures
    else Delete
        User->>Form: Click [Delete] or press Del key
        Form->>VM: RemoveStructure(index)
        VM->>Calc: RecalculateStatus()
        Form->>Form: Remove control, refresh all
    end
```

### Mining Rig Workflow

```mermaid
sequenceDiagram
    actor User
    participant Ctrl as ColonyStructureV2
    participant SVM as ColonyStructureViewModel
    participant Colony as Colony

    User->>Ctrl: Select survey from cmbSurvey
    Ctrl->>SVM: Set MiningSurvey
    Note over SVM: MiningLeftOvers reset to 0

    User->>Ctrl: Select resource from cmbSelection
    Ctrl->>SVM: Set MiningSurveyResource

    User->>Ctrl: Click [Start]
    Ctrl->>SVM: StartMining(intervalSeconds=3600)
    SVM->>SVM: Start repeating countdown timer

    loop Every second
        Ctrl->>Ctrl: Update countdown display
    end

    User->>Ctrl: Click [Done]
    Ctrl->>Colony: ProcessColony()
    Colony->>Colony: Calculate mined qty, add to warehouse
    Ctrl->>SVM: Stop timer, clear process state
```

### Warehouse Item Management

```mermaid
sequenceDiagram
    actor User
    participant Form as FormColonyV2
    participant VM as ColonyViewModel

    User->>Form: Select item type from cmbItemType
    Form->>Form: Populate cmbItem based on type
    Note over Form: Resource → show cmbPurity<br/>Commodity → show ExtendedName<br/>WorkDetail → BC/WC/Spec

    User->>Form: Filter items, select item, enter quantity
    User->>Form: Click [Add]
    Form->>VM: AddItem(type, baseID, name, qty, purity)
    VM->>VM: Create Item, set Volume per REQ-DM-025
    VM->>VM: Add to colony ItemBag
    Form->>Form: Refresh dgvItems

    User->>Form: Select row, press Delete
    Form->>VM: RemoveItem(uuid)
    Form->>Form: Refresh dgvItems
```

## Data Flow Diagrams

### Colony Status Calculation Flow

```mermaid
flowchart LR
    subgraph Input
        S[Colony.Structures list]
        W[AssignedWorkers PropertyBag]
        IB[Colony.ItemBag]
    end

    subgraph Calculator["ColonyStatusCalculator"]
        CB[CalculateBuilt]
        CI[CalculateIdeal]
    end

    subgraph Output
        AS["Statuses[Actual] per structure"]
        IS["Statuses[Ideal] per structure"]
        FA[finalActualStatus]
        FI[finalIdealStatus]
    end

    S --> CB --> AS --> FA
    W --> CB
    S --> CI --> IS --> FI
    IB -->|WarehouseRequired = Σ qty×vol| FA
```

### Colony Processing Pipeline (per cycle)

```mermaid
flowchart TD
    subgraph "ProcessColony() — single cycle"
        B["1. Building<br/>BuildCompletionTime → Built=true"]
        M["2. Mining<br/>SurveyAmount × skill → warehouse"]
        R0["3. Refine Base<br/>25 raw → refined (purity rate)"]
        R1["4. Refine S1<br/>1250 refined → 25 S1"]
        R2["5. Refine S2<br/>500 S1 → 25 S2"]
        MF["6. Manufacturing<br/>consume resources → produce item"]
        RS["7. Research<br/>evolve blueprint"]
    end

    B --> M --> R0 --> R1 --> R2 --> MF --> RS

    WH[(Colony Warehouse<br/>ItemBag)] -.->|reads/writes| M
    WH -.->|reads/writes| R0
    WH -.->|reads/writes| R1
    WH -.->|reads/writes| R2
    WH -.->|reads/writes| MF
```
