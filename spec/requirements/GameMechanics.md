# Game Mechanics Requirements

## User Goal

This file documents the game's production formulas and timing rules so the tracker can accurately simulate colony processing (mining rates, refining yields, manufacturing times, research durations, build times).

## Colony Processing Order

```mermaid
flowchart TD
    A[1. Structure Building] -->|structures become Built| B[2. Mining]
    B -->|raw resources added to warehouse| C[3. Refining Base Resources]
    C -->|refined resources produced| D[4. Refining S1 Synthetics]
    D -->|S1 synthetics produced| E[5. Refining S2 Synthetics]
    E -->|S2 synthetics produced| F[6. Manufacturing]
    F -->|items produced, resources consumed| G[7. Research]

    style A fill:#e6f3ff
    style B fill:#fff3e6
    style C fill:#e6ffe6
    style D fill:#e6ffe6
    style E fill:#e6ffe6
    style F fill:#ffe6e6
    style G fill:#f3e6ff
```

Each step's output feeds the next step's input within the same cycle.

## Mining

**REQ-GM-001** Mining rigs produce resources once per hour (3600-second repeating timer).
**REQ-GM-002** Mined quantity per interval = `floor(SurveyAmount + MiningLeftOvers)`. The fractional remainder is accumulated in `MiningLeftOvers` for the next cycle.
**REQ-GM-003** ExtractionFocus skill multiplier: mined quantity × `(1 + level × 0.01)`.
**REQ-GM-004** MiningLeftOvers SHALL be reset to 0 when MiningSurvey or MiningSurveyResource changes.

## Refining — Normal Resources

**REQ-GM-010** Refineries consume raw resources and produce refined resources once per hour.
**REQ-GM-011** Base refining consume rate: 25 units per cycle (`GameConstants.RefiningBaseRate`).
**REQ-GM-012** Refining output rate depends on input purity:
- Low: 1× base rate (25 consumed → 25 produced)
- Medium: 3× base rate (25 consumed → 75 produced)
- High: 5× base rate (25 consumed → 125 produced)
**REQ-GM-013** RefiningFocus skill multiplier: output rate × `(1 + level × 0.02)`.

## Refining — Synthetic Resources

**REQ-GM-020** Synthetic resources are produced via fixed recipes, not purity-based rates.
**REQ-GM-021** S1 Synthetic recipes (Tier 1, consume 1250 refined natural → produce 25 S1):
- Lanthanides (Refined) → S1. Translanthanic Exotics
- Superheavy Exotics (Refined) → S1. Translivermoric Exotics
- Transuranic Volatiles (Refined) → S1. Transuranic Exotics
**REQ-GM-022** S2 Synthetic recipes (Tier 2, consume 500 refined S1 → produce 25 S2):
- S1. Translanthanic Exotics (Refined) → S2. Element 126
- S1. Translivermoric Exotics (Refined) → S2. Element 127
- S1. Transuranic Exotics (Refined) → S2. Superactinides
**REQ-GM-023** Processing order within a cycle: normal refining (Tier 0) before S1 (Tier 1) before S2 (Tier 2).

## Manufacturing

**REQ-GM-030** Manufactories produce items from blueprints. Each cycle consumes resources defined by the blueprint's resource list.
**REQ-GM-031** Manufacturing display shows 1-based progress: `(completed+1)/(total)` so the user sees "1/3" instead of "0/3" when starting.
**REQ-GM-032** ProductionFocus skill multiplier: manufacture time × `(1 - level × 0.03)`, minimum 1 second.

### Data Model Difference: Manufacturing Quantity

The game and the tracker model manufacturing batch progress differently:

| Aspect | Game (JSON) | Tracker |
|--------|-------------|---------|
| Field | `manufactureNumber` | `ManufacturingQuantity` + `ManufacturingCompleted` |
| Semantics | Remaining runs (decreases as items complete) | Total runs + completed count |
| Example: 5 total, 2 done | `manufactureNumber: 3` | `Quantity: 5, Completed: 2` |

The tracker's model is richer: it preserves the user's original intent (total queued) and tracks progress separately. The game only exposes remaining. On colony reimport, the parser reconciles these two models (see REQ-CI-025 in ColonyImport.md).

## Commodity Manufacturing

**REQ-GM-040** Commodity factories produce 10 commodities per cycle (`GameConstants.CommoditiesPerCycle`).
**REQ-GM-041** Commodity manufacturing cycle time: 600 seconds (10 minutes, `GameConstants.CommodityCycleSeconds`).
**REQ-GM-042** Each cycle consumes resources defined by the commodity's `ConstructionResources` list.
**REQ-GM-043** Commodity display shows 1-based progress, same as manufacturing (REQ-GM-031).

## Research

**REQ-GM-050** Research labs evolve blueprints from one evolution level to the next.
**REQ-GM-051** Research time by current evolution level (in days):
- Evo 0→1: 2d, 1→2: 4d, 2→3: 6d, 3→4: 8d, 4→5: 10d, 5→6: 12d, 6→7: 14d
- Evo 7→8: 16d, 8→9: 18d, 9→10: 20d, 10→11: 22d, 11→12: 24d, 12→13: 26d, 13→14: 28d, 14→15: 30d
**REQ-GM-052** Maximum evolution level is 15. Evolution 15 cannot be researched further.
**REQ-GM-053** ResearchFocus skill multiplier: research time × `(1 - level × 0.03)`, minimum 1 second.

## Structure Building

**REQ-GM-060** Base build time: 86400 seconds (1 day).
**REQ-GM-061** Builder skill multiplier: build time = `86400 × (1 - level × 0.02)`, minimum 1 second.
**REQ-GM-062** At Builder level 50, build time reaches the minimum of 1 second.
**REQ-GM-063** When BuildCompletionTime expires, the structure is marked Built=true and the timer is cleared.

## Colony Build Eligibility

**REQ-GM-070** A structure is "staged" when IsStaged=true AND IsBuilt=false.
**REQ-GM-071** A structure is "building" when IsStaged=false AND IsBuilt=false AND BuildCompletionTime has time remaining > 0.
**REQ-GM-072** A colony is eligible for building when it has at least one staged structure AND no currently building structure.
**REQ-GM-073** GetFirstStagedStructure returns the first staged structure in list order.

## Data Flow Diagrams

### Mining Calculation

```mermaid
flowchart LR
    subgraph Input
        SA[SurveyAmount]
        ML[MiningLeftOvers<br/>fractional accumulator]
        EF[ExtractionFocus skill level]
    end

    subgraph Calculation
        RAW["rawQty = SurveyAmount × (1 + EF × 0.01)"]
        FLR["minedQty = floor(rawQty + MiningLeftOvers)"]
        REM["newLeftOvers = (rawQty + MiningLeftOvers) - minedQty"]
    end

    subgraph Output
        WH[Colony Warehouse<br/>+minedQty of resource]
        NML[MiningLeftOvers updated]
    end

    SA & ML & EF --> RAW --> FLR --> WH
    RAW --> REM --> NML
```

### Refining Tiers

```mermaid
flowchart TD
    subgraph "Tier 0 — Base Resources"
        R0["25 raw → refined<br/>Low: ×1 (25)<br/>Med: ×3 (75)<br/>High: ×5 (125)<br/>× RefiningFocus skill"]
    end

    subgraph "Tier 1 — S1 Synthetics"
        R1["1250 refined natural → 25 S1<br/>Lanthanides → S1. Translanthanic<br/>Superheavy → S1. Translivermoric<br/>Transuranic → S1. Transuranic"]
    end

    subgraph "Tier 2 — S2 Synthetics"
        R2["500 refined S1 → 25 S2<br/>S1. Translanthanic → S2. Element 126<br/>S1. Translivermoric → S2. Element 127<br/>S1. Transuranic → S2. Superactinides"]
    end

    R0 -->|"refined output feeds"| R1 -->|"S1 output feeds"| R2
```

### Build Time Calculation

```mermaid
flowchart LR
    BL[Builder skill level] --> CALC["buildTime = 86400 × (1 - level × 0.02)<br/>minimum 1 second"]
    CALC --> BT[BuildCompletionTime<br/>set on structure]
    Note["At Builder Lv 50:<br/>86400 × (1 - 1.0) = 0 → clamped to 1s"]
```
