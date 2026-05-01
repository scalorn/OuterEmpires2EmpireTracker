# Data Change Events Requirements

## User Goal

The user expects that when data changes (via background processing, import, or another form), all open forms automatically refresh to show current state — no manual refresh needed.

## Event Types

**REQ-DCE-001** PlayerContext SHALL expose the following events:
- `CurrentPlayerChanged` — fired when the selected player changes
- `ColonyDataChanged(ColonyUUID)` — fired when colony data is modified
- `BlueprintDataChanged(BlueprintUUID)` — fired when blueprint data is modified
- `SurveyDataChanged(SurveyUUID)` — fired when survey data is modified
- `DeliveryDataChanged` — fired when delivery route or plan data is modified
- `PlayerProfileDataChanged(PlayerUUID)` — fired when player profile data is modified
- `PlayerProfilesChanged` — fired when the profile list changes (add/remove)
- `PricingDataChanged` — fired when pricing plan data is modified (resource prices, plan settings)
- `BuildPlanDataChanged(BuildPlanUUID)` — fired when build plan data is modified
- `MarketDataChanged` — fired when market listing or transaction data is modified
- `StationDataChanged` — fired when station data is modified
- `AsteroidDataChanged(AsteroidUUID)` — fired when asteroid data is modified (created, updated, deleted)
- `ShipTemplateDataChanged(ShipTemplateUUID)` — fired when ship template data is modified
- `ShipDataChanged(ShipUUID)` — fired when ship instance data is modified
- `StockDataChanged(StockPlanUUID)` — fired when stock plan or profile data is modified
- `SupplyChainDataChanged(SupplyChainUUID)` — fired when supply chain data is modified
- `ContactDataChanged(CharacterUUID)` — fired when external character (contact) data is modified

## Event Args

**REQ-DCE-010** ColonyDataChanged SHALL use `ColonyDataChangedEventArgs` containing the colony UUID.
**REQ-DCE-011** BlueprintDataChanged SHALL use `BlueprintDataChangedEventArgs` containing the blueprint UUID.
**REQ-DCE-012** SurveyDataChanged SHALL use `SurveyDataChangedEventArgs` containing the survey UUID.
**REQ-DCE-013** PlayerProfileDataChanged SHALL use `PlayerProfileDataChangedEventArgs` containing the player UUID.
**REQ-DCE-014** DeliveryDataChanged, MarketDataChanged, StationDataChanged, and CurrentPlayerChanged SHALL use plain `EventArgs`.

**REQ-DCE-015** ColonyStructureDataChanged SHALL use `ColonyStructureDataChangedEventArgs` containing the colony UUID and structure UUID, fired when an individual colony structure is modified.

**REQ-DCE-016** BuildPlanDataChanged SHALL use `BuildPlanDataChangedEventArgs` containing the build plan UUID.
**REQ-DCE-017** AsteroidDataChanged SHALL use `AsteroidDataChangedEventArgs` containing the asteroid UUID.
**REQ-DCE-018** ShipTemplateDataChanged SHALL use `ShipTemplateDataChangedEventArgs` containing the ship template UUID.
**REQ-DCE-019** ShipDataChanged SHALL use `ShipDataChangedEventArgs` containing the ship UUID.
**REQ-DCE-01A** StockDataChanged SHALL use `StockDataChangedEventArgs` containing the stock plan UUID.
**REQ-DCE-01B** SupplyChainDataChanged SHALL use `SupplyChainDataChangedEventArgs` containing the supply chain UUID.
**REQ-DCE-01C** ContactDataChanged SHALL use `ContactDataChangedEventArgs` containing the character UUID.

## Form Subscriptions

**REQ-DCE-020** All MDI child forms SHALL subscribe to relevant data change events in their constructor or load handler.
**REQ-DCE-021** All MDI child forms SHALL unsubscribe from events in OnFormClosed.
**REQ-DCE-022** Event handlers SHALL check `IsDisposed` before accessing form controls to prevent ObjectDisposedException.
**REQ-DCE-023** When a data change event fires, the form SHALL refresh its display if the changed entity is currently selected or visible.

## Write-Through Pattern

**REQ-DCE-030** All editable form controls SHALL write to the data model immediately on change (TextChanged, SelectedIndexChanged, CheckedChanged).
**REQ-DCE-031** Save buttons SHALL only call `WriteContext()` — they SHALL NOT re-read form fields into the model.

## Data Flow Diagram

### Event Propagation

```mermaid
flowchart TD
    subgraph Sources["Event Sources"]
        UI[Form saves<br/>user edits]
        BG[BackgroundProcessor<br/>timer-driven processing]
        IMP[Import operations<br/>clipboard/file]
    end

    subgraph PC["PlayerContext Events"]
        CPC[CurrentPlayerChanged]
        CDC[ColonyDataChanged<br/>colonyUUID]
        BDC[BlueprintDataChanged<br/>blueprintUUID]
        SDC[SurveyDataChanged<br/>surveyUUID]
        DDC[DeliveryDataChanged]
        PDC[PlayerProfileDataChanged<br/>playerUUID]
        PPC[PlayerProfilesChanged]
        PRDC[PricingDataChanged]
        BPDC[BuildPlanDataChanged<br/>buildPlanUUID]
        MDC[MarketDataChanged]
        STDC[StationDataChanged]
        ADC[AsteroidDataChanged<br/>asteroidUUID]
        STPDC[ShipTemplateDataChanged<br/>shipTemplateUUID]
        SHDC[ShipDataChanged<br/>shipUUID]
        SKDC[StockDataChanged<br/>stockPlanUUID]
        SCDC[SupplyChainDataChanged<br/>supplyChainUUID]
        CTDC[ContactDataChanged<br/>characterUUID]
    end

    subgraph Subscribers["Form Subscribers"]
        FC[FormColonyV2]
        FB[FormBlueprintV2]
        FS[FormSurvey]
        FCA[FormColonyActivity]
        FDB[FormColonyDailyBuild]
        FDR[FormDeliveryRoute]
        FDE[FormDeliveryExecution]
        FPP[FormPlayerProfile]
        FPR[FormPricingPlan]
        FBP[FormBuildPlanner]
        FMK[FormMarket]
        FST[FormStation]
        FA[FormAsteroid]
        FSH[FormShipTemplate]
        FSI[FormShipInstance]
        FTG[FormStockTargets]
        FSC[FormSupplyChain]
        FCT[FormContacts]
    end

    UI --> PC
    BG --> CDC
    IMP --> PC

    CPC --> FC & FB & FS & FCA & FDB & FDR & FDE & FPP & FPR & FBP & FMK & FST & FA & FSH & FSI & FTG & FSC & FCT
    CDC --> FC & FCA & FDB & FDE
    BDC --> FB
    SDC --> FS
    DDC --> FDR & FDE
    PDC --> FPP
    PRDC --> FPR
    BPDC --> FBP
    MDC --> FMK
    STDC --> FST
    ADC --> FA
```

### Write-Through Pattern

```mermaid
sequenceDiagram
    actor User
    participant Form as Any Form
    participant Model as Data Model
    participant PC as PlayerContext

    User->>Form: Edit TextBox / ComboBox / CheckBox
    Form->>Form: Check _isProgrammaticUpdate > 0? Skip if yes
    Form->>Model: Write value immediately (TextChanged etc.)
    Note over Model: In-memory model always current

    User->>Form: Click [Save]
    Form->>PC: WriteContext()
    Note over PC: Persist to disk only — no re-read from UI
```
