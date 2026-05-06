# Delivery User Interaction Flows

### Route Builder — Create and Edit Route

```mermaid
sequenceDiagram
    actor User
    participant Form as FormDeliveryRoute
    participant VM as DeliveryRouteViewModel
    participant PC as PlayerContext

    User->>Form: Type in route filter
    Form->>Form: Filter lvwRoutes by name

    User->>Form: Click route in list
    Form->>VM: Load route data
    Form->>Form: Populate txtRouteName, dgvStops

    User->>Form: Select colony from cmbColony
    User->>Form: Click [Add Stop]
    Form->>VM: AddStop(colonyUUID)
    Form->>Form: Refresh dgvStops (seq, colony, planet, system)

    User->>Form: Select stop row, click [▲]/[▼]
    Form->>VM: ReorderStop(index, direction)
    Form->>Form: Refresh dgvStops

    User->>Form: Click [Save]
    Form->>VM: Save route
    VM->>PC: WriteContext()
```

### Delivery Plan — Create and Populate

```mermaid
sequenceDiagram
    actor User
    participant Form as FormDeliveryRoute (Plan tab)
    participant VM as DeliveryPlanViewModel
    participant PC as PlayerContext

    User->>Form: Click [New] on Plan tab
    Form->>VM: Create plan (auto-name: RouteName - YYYY-MM-DD)
    Form->>Form: Show plan in cmbPlan

    User->>Form: Select stop on Stops tab
    Form->>Form: Show stop context on Plan tab (lblPlanStop)

    User->>Form: Select item type, filter, item, qty
    User->>Form: Click [Add] under Drop Off
    Form->>VM: AddDropOffItem(stopIndex, item)
    Form->>Form: Refresh dgvDropOff

    User->>Form: Click [Auto-Fill]
    Form->>Form: Open FormAutoFill dialog
    Note over Form: Select: Commodities, Workers,<br/>Resources, Flatpacks
    Form->>VM: AutoFill(options)
    Form->>Form: Refresh all stop grids
```

### Delivery Execution

```mermaid
sequenceDiagram
    actor User
    participant Form as FormDeliveryExecution
    participant PC as PlayerContext

    User->>Form: Select route from cmbRoute
    User->>Form: Select plan from cmbPlan
    Form->>Form: Compute and display Load List
    Form->>Form: Display all stops with drop-off/pick-up items

    loop For each stop
        User->>Form: Check item checkbox (delivered)
        Form->>PC: Auto-save immediately
        alt Commodity delivered
            Form->>PC: Update CommodityRequested.Delivered
        else Flatpack delivered
            Form->>PC: Mark structure as Staged
        end
    end

    Note over Form: All items checked
    Form->>Form: Mark plan as Completed
    Form->>PC: WriteContext()
```

## Data Flow Diagrams

### Route → Plan → Execution Pipeline

```mermaid
flowchart LR
    subgraph RouteBuilder["Route Builder Form"]
        R[Route<br/>Name + ordered stops]
        P[Plan<br/>Items per stop]
    end

    subgraph Execution["Execution Form"]
        LL[Load List<br/>Pre-departure items]
        ST[Stop-by-stop<br/>checkboxes]
    end

    subgraph Effects["Side Effects"]
        CR[CommodityRequested<br/>Delivered++, Fulfilled]
        CS[ColonyStructure<br/>Staged=true]
        WH[Colony Warehouse<br/>Item quantities]
    end

    R -->|defines stops| P
    P -->|items per stop| LL
    P -->|items per stop| ST
    ST -->|commodity check| CR
    ST -->|flatpack check| CS
    ST -->|resource check| WH
```

### Auto-Fill Data Sources

```mermaid
flowchart TD
    subgraph Sources["Data Sources"]
        CRQ[CommodityRequested<br/>unfulfilled entries]
        WG[Worker Gaps<br/>ideal vs actual]
        MR[Manufacturing Resources<br/>active blueprint needs]
        FP[Flatpacks<br/>staged/unbuilt structures]
    end

    AF[Auto-Fill Engine]

    subgraph Output
        DI[DeliveryItem entries<br/>added to plan stops]
    end

    CRQ --> AF
    WG --> AF
    MR --> AF
    FP --> AF
    AF --> DI
```
