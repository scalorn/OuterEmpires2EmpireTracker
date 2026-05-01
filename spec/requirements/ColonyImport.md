# Colony Import Requirements

## User Goal

The user wants to import colony data from the game by copying HTML from the browser and pasting into the tracker, avoiding manual re-entry of structure lists, worker assignments, and resource inventories.

## Out of Scope

- Direct game API integration (import is clipboard-based only)
- Automatic periodic re-import (user must manually paste each time)
- Partial import (the entire colony page is imported as a unit)

## Import Flow

```mermaid
flowchart TD
    A[Clipboard HTML] --> B[ExtractFragment via markers]
    B --> C[SGML → well-formed XML]
    C --> D[ParseColonyBuildings from JSON in HTML]
    C --> E[BuildFlatpackLookup from blueprint list]
    D --> F[Parse structures: UUID, state, workers, timers]
    E --> F
    F --> G[Assign displaySequence per blueprint type]
    G --> H{For each structure}
    H --> I{UUID exists in colony?}
    I -->|yes| J[Update existing structure]
    I -->|no| K[Add new structure]
    J --> H
    K --> H
    H -->|done| L[MinerSetup — assign surveys to mining rigs]
    L --> M[RefinerySetup — configure refineries]
```

## HTML Clipboard Import

**REQ-CI-001** The colony form SHALL provide an "Import Clipboard" button that reads HTML from the system clipboard and parses it into the selected colony.
**REQ-CI-002** The colony form SHALL provide an "Import Colony" button that reads an HTML file from disk and parses it into the selected colony.
**REQ-CI-003** The parser SHALL use SGML reader to convert game HTML into well-formed XML for processing.

## Parsed Data

**REQ-CI-010** The parser SHALL extract PlanetName and SystemName from the colony overview section.
**REQ-CI-011** The parser SHALL extract colony structures from embedded JSON data in the HTML, including:
- Structure UUID and flatpack blueprint UUID
- Game sequence number
- Build state (Built, Staged, Online)
- Worker assignments
- Process timers (mining, refining, manufacturing, research)
**REQ-CI-012** The parser SHALL extract commodity demands (CommodityRequested entries) from the HTML.
**REQ-CI-013** The parser SHALL match flatpack blueprint UUIDs against the known blueprint list to resolve blueprint types.

## Merge Semantics

**REQ-CI-020** When importing into an existing colony, structures SHALL be merged by compound key (FlatpackBlueprintUUID + per-type displaySequence). Existing structures with a matching compound key are updated; new structures are added. For CommodityManufactory types, positional matching within each sub-type is used instead of displaySequence.
**REQ-CI-021** The parser SHALL preserve any locally-set data (nicknames, manual overrides) that is not present in the game HTML.
**REQ-CI-022** SystemName SHALL be extracted from the location bar data when available.

## Flatpack Lookup

**REQ-CI-030** ColonyParser.BuildFlatpackLookup SHALL use Blueprint.OutputItemName as the design name key when building the lookup dictionary, rather than inline suffix stripping.
**REQ-CI-031** The lookup SHALL register both OutputItemName and the original Blueprint.Name as keys mapping to the blueprint UUID, so both "Mining Rig" and "Mining Rig Flatpack" resolve to the same blueprint.

## Market Blueprint Import

**REQ-CI-040** The blueprint form SHALL provide an "Import Market" button that reads HTML from the system clipboard and parses market listing rows into blueprints.
**REQ-CI-041** The parser SHALL extract blueprint name, type, evolution, properties, resources, seller name, and TechLevel from each expanded market listing row.
**REQ-CI-042** Government-seller blueprints SHALL be imported as global blueprints (empty OwnerUUID). Player-seller blueprints SHALL be imported as player-specific blueprints.
**REQ-CI-043** Import SHALL be idempotent — re-importing the same market data SHALL update existing blueprints (matched by Name+Evolution+Type+Class+TechLevel) without creating duplicates.
**REQ-CI-044** Import SHALL preserve protected fields (NickName, CopyCost, BaseBlueprintUUID) on existing blueprints.
**REQ-CI-045** HTML fragment extraction SHALL use StartFragment/EndFragment markers when present in clipboard data, falling back to byte offset headers when markers are absent.

## User Interaction Flows

### Clipboard Import

```mermaid
sequenceDiagram
    actor User
    participant Form as FormColonyV2
    participant Parser as ColonyParser
    participant PC as PlayerContext

    User->>User: Copy colony page HTML in game browser
    User->>Form: Click [Import Clipboard]
    Form->>Form: Read HTML from system clipboard
    Form->>Parser: ExtractFragment(clipboardData)
    Parser->>Parser: SGML → well-formed XML
    Parser->>Parser: ParseColonyBuildings (JSON in HTML)
    Parser->>Parser: BuildFlatpackLookup (blueprint list)
    Parser->>Parser: Parse structures: UUID, state, workers, timers
    Parser->>Parser: Assign displaySequence per blueprint type
    Parser->>Parser: ParseCommodityDemands (scored matching)

    loop Each parsed structure
        alt UUID exists in colony
            Parser->>Parser: Update existing structure
        else New structure
            Parser->>Parser: Add new structure
        end
    end

    Parser->>Parser: MinerSetup — assign surveys to mining rigs
    Parser->>Parser: RefinerySetup — configure refineries
    Parser-->>Form: Return merged colony data

    Form->>PC: WriteContext()
    Form->>Form: Refresh all tabs
```

### Market Blueprint Import

```mermaid
sequenceDiagram
    actor User
    participant Form as FormBlueprintV2
    participant Parser as BlueprintScanner
    participant PC as PlayerContext

    User->>User: Copy market listing HTML in game browser
    User->>Form: Click [Import Market]
    Form->>Form: Read HTML from clipboard
    Form->>Parser: Parse market listing rows

    loop Each expanded listing row
        Parser->>Parser: Extract name, type, evolution, properties, resources
        Parser->>Parser: Determine seller (government → global, player → owned)
        Parser->>PC: Match by Name+Evolution+Type+Class+TechLevel
        alt Existing blueprint
            Parser->>PC: Update (preserve NickName, CopyCost, BaseBlueprintUUID)
        else New blueprint
            Parser->>PC: Add with new UUID
        end
    end

    Form->>PC: WriteContext()
    Form->>Form: Refresh blueprint list
```

## Data Flow Diagram

### Colony Import Merge Pipeline

```mermaid
flowchart TD
    subgraph Input
        CB[Clipboard HTML]
        BPL[Blueprint List<br/>flatpack lookup]
        EC[Existing Colony<br/>structures + items]
    end

    subgraph Parser["ColonyParser"]
        EXT[ExtractFragment<br/>StartFragment/EndFragment markers]
        SGML[SGML → XML]
        JSON[ParseColonyBuildings<br/>embedded JSON]
        FPL[BuildFlatpackLookup<br/>OutputItemName + Name keys]
        STR[Structure merge<br/>by FlatpackBlueprintUUID + displaySequence]
        CMD[ParseCommodityDemands<br/>scored matching for duplicates]
    end

    subgraph Output
        MC[Merged Colony<br/>updated structures + demands]
    end

    CB --> EXT --> SGML --> JSON
    BPL --> FPL
    JSON --> STR
    FPL --> STR
    EC --> STR --> MC
    SGML --> CMD --> MC
```
