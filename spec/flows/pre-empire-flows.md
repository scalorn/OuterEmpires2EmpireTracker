# Pre-Empire-Systems User Flows

User interaction flows for features implemented before the empire-systems spec.
These cover data import, daily operations, pricing, evolution, and colony management.

---

## Flow 1: Colony Import

```mermaid
sequenceDiagram
    actor User
    participant CF as Colony Form
    participant CP as ColonyParser
    participant DD as Dedup Check
    participant MSH as MinerSetupHelper
    participant PC as PlayerContext

    User->>CF: Copy colony HTML from game browser
    User->>CF: Click Import
    CF->>CP: ParseClipboardToTemp()
    CP-->>CF: tempColony (structures, items, workers)
    CF->>DD: FindByName(tempColony.PlanetName)
    alt Colony exists
        DD-->>CF: existing colony
        CF->>CF: MergeIdentity(existing, temp)
    else New colony
        DD-->>CF: no match
        CF->>CF: CreateFromTemp(temp, playerUUID)
    end
    CF->>MSH: SetupMiners + Refineries from surveys
    MSH-->>CF: configured
    CF->>PC: WriteContext()
    CF-->>User: Colony imported/updated
```

---

## Flow 2: Blueprint Import (Individual)

```mermaid
sequenceDiagram
    actor User
    participant BF as Blueprint Form
    participant BP as BlueprintParser
    participant DD as Dedup Check
    participant EC as EmpireContext
    participant PC as PlayerContext

    User->>BF: Copy blueprint HTML from game browser
    User->>BF: Click Import
    BF->>BP: ParseClipboard()
    BP-->>BF: parsed blueprint (name, type, properties)
    BF->>DD: FindByNameAndType()
    alt Global blueprint (base game)
        DD->>EC: Store in EmpireContext
        EC->>EC: WriteContext()
    else Player blueprint (scanned/evolved)
        DD->>PC: Store in PlayerContext
        PC->>PC: WriteContext()
    end
    BF-->>User: Blueprint imported
```


---

## Flow 3: Blueprint Import (Market/Bulk)

```mermaid
sequenceDiagram
    actor User
    participant BF as Blueprint Form
    participant BP as BlueprintParser
    participant DD as Dedup Check
    participant EC as EmpireContext
    participant PC as PlayerContext

    User->>BF: Copy market listing HTML
    User->>BF: Click Import Market
    BF->>BP: ParseMarketListings()
    loop Each listing in HTML
        BP->>BP: Extract blueprint from listing row
        BP-->>BF: parsed blueprint
        BF->>DD: FindByNameAndType()
        alt Global type
            BF->>EC: Merge into EmpireContext
        else Player type
            BF->>PC: Merge into PlayerContext
        end
    end
    EC->>EC: WriteContext()
    PC->>PC: WriteContext()
    BF-->>User: N blueprints imported
```

---

## Flow 4: Survey Import

```mermaid
sequenceDiagram
    actor User
    participant SF as Survey Form
    participant SP as SurveyParser
    participant DD as Dedup Check
    participant PC as PlayerContext

    User->>SF: Copy survey HTML from game browser
    User->>SF: Click Import
    SF->>SP: ParseClipboard()
    SP-->>SF: parsed survey (planet, resources, purities)
    SF->>DD: FindByPlanetName()
    alt Survey exists
        DD-->>SF: existing survey
        SF->>SF: MergeResources(existing, parsed)
    else New survey
        DD-->>SF: no match
        SF->>SF: CreateSurvey(parsed)
        SF->>PC: Auto-create Asteroid if missing
    end
    SF->>PC: WriteContext()
    SF-->>User: Survey imported/updated
```


---

## Flow 5: Player Profile Import

```mermaid
sequenceDiagram
    actor User
    participant PF as Profile Form
    participant PP as ProfileParser
    participant PC as PlayerContext

    User->>PF: Copy profile HTML from game browser
    User->>PF: Click Import
    PF->>PP: ParseClipboard()
    PP-->>PF: parsed profile (name, skills, rank)
    PF->>PC: FindProfileByName(parsed.Name)
    alt Profile exists
        PC-->>PF: existing profile
        PF->>PF: UpdateSkills(existing, parsed)
    else New profile
        PC-->>PF: no match
        PF->>PF: CreateProfile(parsed)
    end
    PF->>PC: WriteContext()
    PF-->>User: Profile imported/updated
```

---

## Flow 6: Colony Daily Build

```mermaid
sequenceDiagram
    actor User
    participant DB as Daily Build Form
    participant CS as ColonyStatus
    participant BT as BuildTimer
    participant PC as PlayerContext

    User->>DB: Select delivery route
    DB->>CS: GetColoniesWithStagedStructures(route)
    CS-->>DB: colonies + staged structure list
    DB-->>User: Display colonies and staged items
    User->>DB: Click Build All
    loop Each staged structure
        DB->>BT: StartBuildTimer(structure)
        BT->>BT: Calculate build duration
        BT-->>DB: timer started
    end
    DB->>PC: WriteContext()
    DB-->>User: Build timers running
```


---

## Flow 7: Delivery Execution

```mermaid
sequenceDiagram
    actor User
    participant DE as Delivery Execution Form
    participant DP as DeliveryPlan
    participant DF as DeliveryFulfillment
    participant PC as PlayerContext

    User->>DE: Select route + delivery plan
    DE->>DP: LoadPlanItems(route, plan)
    DP-->>DE: item list grouped by stop
    DE-->>User: Display checklist per stop
    loop Each stop on route
        User->>DE: Check off delivered items
        DE->>DF: MarkDelivered(item, qty)
        DF->>DF: Update fulfillment status
    end
    DE->>DF: RecalculateFulfillment()
    DF-->>DE: updated plan status
    DE->>PC: WriteContext()
    DE-->>User: Delivery progress saved
```

---

## Flow 8: Pricing Plan Calculation

```mermaid
sequenceDiagram
    actor User
    participant PP as Pricing Plan Form
    participant PR as PriceCalculator
    participant EC as EmpireContext

    User->>PP: Set resource base prices
    User->>PP: Select item to price
    PP->>EC: GetBlueprint(item)
    EC-->>PP: blueprint with BOM
    PP->>PR: CalculatePrice(blueprint, basePrices)
    PR->>PR: Recursively roll up BOM costs
    PR->>PR: Add labor/time multipliers
    PR-->>PP: computed price breakdown
    PP-->>User: Display price tree + total
```


---

## Flow 9: Blueprint Evolution Graph

```mermaid
sequenceDiagram
    actor User
    participant BF as Blueprint Form
    participant EV as Evolution Tab
    participant EC as EmpireContext
    participant PC as PlayerContext

    User->>BF: Select blueprint
    User->>EV: Click Evolution tab
    EV->>EC: GetBaseBlueprint(name)
    EV->>PC: GetPlayerBlueprints(name)
    EV->>EV: ResolveEvolutionChain(base, player copies)
    EV->>EV: Sort by generation/tier
    EV->>EV: Plot property progression
    EV-->>User: Display evolution graph with properties
```

---

## Flow 10: Colony Bootstrap

```mermaid
sequenceDiagram
    actor User
    participant CF as Colony Form
    participant CB as ColonyBootstrap
    participant PC as PlayerContext

    User->>CF: Select colony
    User->>CF: Click Bootstrap
    CF->>CB: Bootstrap(colony)
    CB->>PC: CheckSurveys(colony.Planet)
    PC-->>CB: available surveys
    CB->>CB: CreateStarterStructures(surveys)
    CB->>CB: AutoConfigureMining(surveys)
    CB->>CB: AutoConfigureRefining()
    CB-->>CF: bootstrap complete
    CF->>PC: WriteContext()
    CF-->>User: Colony bootstrapped with starter structures
```

---

## Flow 11: Colony Build Order Optimization

```mermaid
sequenceDiagram
    actor User
    participant CF as Colony Form
    participant BO as BuildOrderOptimizer
    participant BE as BuildEligibility
    participant PC as PlayerContext

    User->>CF: Select colony
    User->>CF: Click Optimize Build Order
    CF->>BO: Optimize(colony.Structures)
    BO->>BE: CheckDependencies(structures)
    BE-->>BO: dependency graph
    BO->>BO: TopologicalSort(structures)
    BO->>BO: ReorderByPriority(sorted)
    BO-->>CF: optimized build order
    CF->>PC: WriteContext()
    CF-->>User: Build order optimized
```
