# Colony Form Mockups

### FormColonyV2 — Main Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ #1 - Manage Colonies                                                    [_][□][X] │
├──────────────────┬──────────────────────────────────────────────────────────┤
│ [Filter_________]│  Planet Name [________________]  Colony Name [________________] │
│                  │  System Name [________________]                                 │
│ ┌──────────────┐ │  ┌─────────────────────────────────────────────────────────┐    │
│ │ Colony List   │ │  │ Administration │ Structures │ Workers │ Warehousing │   │    │
│ │              │ │  ├─────────────────────────────────────────────────────────┤    │
│ │ Helorix M1   │◄├──┤                                                       │    │
│ │ Zeh Vaz M1   │ │  │  (tab content — see below)                            │    │
│ │ Proxima M2   │ │  │                                                       │    │
│ │              │ │  │                                                       │    │
│ │              │ │  │                                                       │    │
│ │              │ │  │                                                       │    │
│ │              │ │  │                                                       │    │
│ └──────────────┘ │  └─────────────────────────────────────────────────────────┘    │
│                  │  [New] [Save] [Delete] [Import Colony] [Import Clipboard]       │
├──────────────────┴────────────────────────────────────────────────────────────────┤
│  ◄ splitter (8px, draggable) ►                                                    │
└───────────────────────────────────────────────────────────────────────────────────┘
```

### Structures Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Structures tab                                                              │
├──────────────────┬──────────────────────────────────────────────────────────┤
│ ☑ Power Plant    │  Actual: Power:70/72 Hab:75/34 Food:75/34 Ent:150/70    │
│ ☑ Habitation     │         WH:2350.0/260000                                │
│ ☑ Hydroponics    │  Ideal:  Power:70/78 Hab:31/34 Food:31/34 Ent:62/70    │
│ ☑ Entertainment  │         WH:2350.0/260000                                │
│ ☑ Mining Rig     │ ┌──────────────────────────────────────────────────────┐ │
│ ☑ Refinery       │ │ Colony Command Centre #1  ☐Staged ☑Built ☑Online   │ │
│ ☑ Manufactory    │ │ Power:2/0 Hab:0/1 Food:0/1 Ent:0/2 WH:0           │ │
│ ☑ Research Lab   │ │ ☑BC1 ☐WC1                                          │ │
│ ☑ Comm Factory   │ │ [▲] [▼] [Delete]                                   │ │
│ ☑ Remote Ops     │ ├──────────────────────────────────────────────────────┤ │
│ ☑ Colony CC      │ │ Power Plant #1            ☐Staged ☑Built ☑Online   │ │
│                  │ │ Power:24/2 Hab:0/1 Food:0/1 Ent:0/2 WH:0          │ │
│ (structure type  │ │ ☑BC1                                                │ │
│  filter list)    │ │ [▲] [▼] [Delete]                                   │ │
│                  │ ├──────────────────────────────────────────────────────┤ │
│                  │ │ Mining Rig #1             ☐Staged ☑Built ☑Online   │ │
│                  │ │ Power:0/4 Hab:0/2 Food:0/2 Ent:0/4 WH:0           │ │
│                  │ │ ☑BC1 ☑BC2                                          │ │
│                  │ │ Survey [filter] [▼ Helorix Survey 1234        ]    │ │
│                  │ │ Resource [filter] [▼ Alkali Metals (High)     ]    │ │
│                  │ │ Qty [1] ☐Stage  [Start] [Done]                    │ │
│                  │ │ ▌▌▌▌▌▌▌▌░░░░ 45m 12s  Completion: 14:32          │ │
│                  │ │ [▲] [▼] [Delete]                                   │ │
│                  │ └──────────────────────────────────────────────────────┘ │
│                  │  Filter [________] [▼ Mining Rig Flatpack    ] [Add]    │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

### Warehousing Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Warehousing tab                                                             │
│ ┌──────────┬──────────────────────────┬────────┬──────────┐                 │
│ │ Type     │ Name                     │ Locked │ Quantity │                 │
│ ├──────────┼──────────────────────────┼────────┼──────────┤                 │
│ │ Resource │ Alkali Metals (Refined)  │ 0      │ 1250     │                 │
│ │ Resource │ Alkali Metals (High)     │ 0      │ 500      │                 │
│ │ Commodity│ Health Scanners          │ 0      │ 42       │                 │
│ │ WorkDetail│ Blue Collar Detail      │ 2      │ 5        │                 │
│ └──────────┴──────────────────────────┴────────┴──────────┘                 │
│                                                                             │
│ [▼ Resource  ] [filter___] [▼ Alkali Metals        ] [▼ Refined] [1] [Add] │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Administration Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Administration tab                                                          │
│                                                                             │
│ [Bootstrap Colony] [Optimize Build Order]                                   │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ BUILDING                                                                │ │
│ │   Habitation Block #3    23h 14m 02s    completes 2026-04-19 13:32     │ │
│ │                                                                        │ │
│ │ COMMODITY REQUESTS                                                     │ │
│ │   Health Scanners x35    due 2026-04-21                                │ │
│ │                                                                        │ │
│ │ INACTIVITY                                                             │ │
│ │   Colony Import Staleness: 6d 2h (last import 2026-04-12)             │ │
│ │   Idle Mining: Mining Rig #2                                           │ │
│ │                                                                        │ │
│ │ ACTIVITY                                                               │ │
│ │   Manufacturing: (2/5) C3 Ev(4) Mining Rig (TL5)                      │ │
│ │     next: 1h 23m 45s    batch: 5h 12m 00s                             │ │
│ │   Mining: Alkali Metals 125.50/h (3 rigs)                              │ │
│ │   Refining: Alkali Metals (High) 75:375/h                             │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```
