# Delivery Form Mockups

### FormDeliveryRoute — Stops Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ #1 - Delivery Routes                                                    [_][□][X] │
├──────────────────┬──────────────────────────────────────────────────────────┤
│ Filter [________]│  Route Name [______________________________]            │
│                  │  ┌────────────────────────────────────────────────────┐  │
│ ┌──────────────┐ │  │ Stops │ Plan │                                    │  │
│ │ Route List   │ │  ├────────────────────────────────────────────────────┤  │
│ │              │ │  │ ┌───┬──────────────┬──────────────┬─────────────┐ │  │
│ │ Alpha Run    │ │  │ │ # │ Colony       │ Planet       │ System      │ │  │
│ │ Beta Circuit │ │  │ ├───┼──────────────┼──────────────┼─────────────┤ │  │
│ │ Gamma Loop   │ │  │ │ 0 │ Helorix M1   │ Helorix-Zeta │ Zeta Sys   │ │  │
│ │              │ │  │ │ 1 │ Proxima M2   │ Proxima-B    │ Alpha Sys  │ │  │
│ │              │ │  │ │ 2 │ Zeh Vaz M1   │ Zeh Vazoran  │ Delta Sys  │ │  │
│ │              │ │  │ └───┴──────────────┴──────────────┴─────────────┘ │  │
│ │              │ │  │                                                    │  │
│ │              │ │  │ Add Stop [▼ Helorix M1 - Helorix-Zeta (Zeta) ]   │  │
│ │              │ │  │ [Add] [▲] [▼] [Remove] ☐ Prevent Duplicates      │  │
│ └──────────────┘ │  └────────────────────────────────────────────────────┘  │
│                  │  [New] [Save] [Delete]                                   │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

### FormDeliveryRoute — Plan Tab

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Plan tab                                                                     │
│ ☐Show Completed [filter] [▼ Alpha Run - 2026-04-18] [New][Delete][Execute][Auto-Fill] │
│ Plan Name [Alpha Run - 2026-04-18_________________________]                  │
│                                                                              │
│ Stop #0: Helorix M1 (Helorix-Zeta, Zeta Sys)                               │
│                                                                              │
│ Drop Off:                                                                    │
│ ┌──────────┬──────────────────────────────┬──────┐                           │
│ │ Type     │ Item                         │ Qty  │                           │
│ ├──────────┼──────────────────────────────┼──────┤                           │
│ │ Commodity│ Health Scanners              │ 35   │                           │
│ │ WorkDetail│ Blue Collar Detail          │ 3    │                           │
│ └──────────┴──────────────────────────────┴──────┘                           │
│ [▼ Commodity] [filter] [▼ Health Scanners] [▼ purity] [35] [Add] [Remove]   │
│                                                                              │
│ Pick Up:                                                                     │
│ ┌──────────┬──────────────────────────────┬──────┐                           │
│ │ Type     │ Item                         │ Qty  │                           │
│ ├──────────┼──────────────────────────────┼──────┤                           │
│ │ Resource │ Alkali Metals (Refined)      │ 500  │                           │
│ └──────────┴──────────────────────────────┴──────┘                           │
│ [▼ Resource] [filter] [▼ Alkali Metals] [▼ Refined] [500] [Add] [Remove]    │
└──────────────────────────────────────────────────────────────────────────────┘
```

### FormDeliveryExecution

Route dropdown population is shared with FormColonyDailyBuild via `RouteDropdownHelper.Populate()` in `OE2EmpireTracker/Controls/RouteDropdownHelper.cs`.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ #1 - Delivery Execution                                                 [_][□][X] │
├──────────────────┬──────────────────────────────────────────────────────────┤
│ Route            │  LOAD LIST (items to load before departure)             │
│ [filter________] │  ┌──────────┬──────────────────────────┬──────┐         │
│ [▼ Alpha Run   ] │  │ Type     │ Item                     │ Qty  │         │
│                  │  ├──────────┼──────────────────────────┼──────┤         │
│ Plan             │  │ Commodity│ Health Scanners           │ 35   │         │
│ [filter________] │  │ WorkDetail│ Blue Collar Detail       │ 3    │         │
│ [▼ 2026-04-18  ] │  └──────────┴──────────────────────────┴──────┘         │
│                  │                                                          │
│ [Complete Plan]  │  ── Stop #0: Helorix M1 ──────────────────────          │
│ [Delete Plan]    │  Drop Off:                                               │
│                  │    ☑ Commodity  Health Scanners         x35              │
│                  │    ☐ WorkDetail Blue Collar Detail      x3               │
│                  │  Pick Up:                                                │
│                  │    ☐ Resource   Alkali Metals (Refined) x500             │
│                  │                                                          │
│                  │  ── Stop #1: Proxima M2 ──────────────────────           │
│                  │  Drop Off:                                               │
│                  │    ☐ Flatpack  Mining Rig Flatpack      x1               │
│                  │  Pick Up:                                                │
│                  │    (none)                                                │
└──────────────────┴──────────────────────────────────────────────────────────┘
```
