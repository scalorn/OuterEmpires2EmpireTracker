# User Interaction Flows

Index of all user flow documents. Each file contains Mermaid sequence/flow diagrams
showing how users interact with the system for a specific domain.

## Files

| File | Domain | Flows |
|------|--------|-------|
| [build-planner-flows.md](build-planner-flows.md) | Build Planner | Manufacturing Workflow, Ship Build Template to Assembly, Queue Calculator, Colony Plan → Build Plan Generation, Multi-Plan Consolidated Delivery |
| [market-flows.md](market-flows.md) | Market | Market Sale Cascade, Market Purchase Resource Fulfillment |
| [supply-chain-flows.md](supply-chain-flows.md) | Supply Chain | Mining to Manufacturing, Define and Monitor Pipeline, Warehouse Overflow Colony Tab |
| [ship-flows.md](ship-flows.md) | Ships | Ship Template Modification Stock Target Cascade, Ship Instance Create Configure Load Cargo |
| [station-flows.md](station-flows.md) | Stations | Station Create Manage Holds |
| [stock-target-flows.md](stock-target-flows.md) | Stock Targets | Plan Creation and Order Generation, Stock Profile AND/OR Composition |
| [contacts-flows.md](contacts-flows.md) | Contacts | Factions and External Characters |
| [asteroid-flows.md](asteroid-flows.md) | Asteroids | Asteroid Create and Track Reserves |
| [pre-empire-flows.md](pre-empire-flows.md) | Pre-Empire Systems | Colony Import, Blueprint Import, Survey Import, Player Profile Import, Colony Daily Build, Delivery Execution, Pricing Plan, Blueprint Evolution, Colony Bootstrap, Build Order Optimization |

## Flow Index

### Build Planner

| # | Name | Description |
|---|------|-------------|
| 1 | Build Planner — Manufacturing Workflow | Create plan, add items, allocate, check resources, generate delivery |
| 2 | Ship Build — Template to Assembly | Create template, order build, manufacture components, assemble |
| 6 | Queue Calculator | Compute runs to fill target duration |
| 16 | Colony Plan → Build Plan Generation | Generate build items from unstaged colony structures |
| 17 | Multi-Plan Consolidated Delivery | Consolidated resource + flatpack delivery across plans |

### Market

| # | Name | Description |
|---|------|-------------|
| 3 | Market Sale — Cascade to Manufacturing | Sale → stock check → build items → resource check → delivery |
| 4 | Market Purchase — Resource Fulfillment | Purchase → station hold → re-check shortfalls → status update |

### Supply Chain

| # | Name | Description |
|---|------|-------------|
| 5 | Supply Chain — Mining to Manufacturing | Multi-source mining → pickup → refine → deliver |
| 13 | Supply Chain — Define and Monitor Pipeline | Define stages, set thresholds, background monitoring |
| 14 | Warehouse Overflow — Colony Tab | Set overflow rules, background delivery generation |

### Ships

| # | Name | Description |
|---|------|-------------|
| 7 | Ship Template Modification — Stock Target Cascade | Template change → stock target re-evaluation |
| 8 | Ship Instance — Create, Configure, Load Cargo | Create from template, swap components, manage cargo |

### Stations

| # | Name | Description |
|---|------|-------------|
| 9 | Station — Create, Manage Holds | Create station, manage per-player holds with crates |

### Stock Targets

| # | Name | Description |
|---|------|-------------|
| 10 | Stock Targets — Plan Creation and Order Generation | Create plans, check levels, generate replenishment |
| 15 | Stock Profile — AND/OR Composition | OR within groups, AND across groups |

### Contacts

| # | Name | Description |
|---|------|-------------|
| 11 | Contacts — Factions and External Characters | Create factions, add characters, combo lookups |

### Asteroids

| # | Name | Description |
|---|------|-------------|
| 12 | Asteroid — Create and Track Reserves | Create asteroid, track reserves, link surveys |

### Pre-Empire Systems

| # | Name | Description |
|---|------|-------------|
| 1 | Colony Import | Copy HTML → parse → dedup → merge/create → miner setup → persist |
| 2 | Blueprint Import (Individual) | Copy HTML → parse → dedup → route global/player → persist |
| 3 | Blueprint Import (Market/Bulk) | Copy market HTML → loop parse listings → dedup → route → persist |
| 4 | Survey Import | Copy HTML → parse → dedup → merge/create → auto-create asteroid → persist |
| 5 | Player Profile Import | Copy HTML → parse → match by name → update/create → persist |
| 6 | Colony Daily Build | Select route → show staged structures → Build All → timers start |
| 7 | Delivery Execution | Select route+plan → load list → check off items → fulfillment → persist |
| 8 | Pricing Plan Calculation | Set prices → select item → roll up BOM → display breakdown |
| 9 | Blueprint Evolution Graph | Select blueprint → Evolution tab → resolve chain → plot properties |
| 10 | Colony Bootstrap | Click Bootstrap → check surveys → create starter structures → auto-configure |
| 11 | Colony Build Order Optimization | Click Optimize → reorder structures → consider dependencies |
