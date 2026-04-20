<!-- Extracted from .kiro/specs/empire-systems/design.md — User Interaction Flows -->
# User Interaction Flows

Index of all flows with brief descriptions.

## Files

| File | Description |
|---|---|
| [user-flows.md](user-flows.md) | Empire-systems flows (Build Planner, Ships, Market, Supply Chain, etc.) |
| [pre-empire-flows.md](pre-empire-flows.md) | Pre-empire-systems flows (Import, Daily Build, Delivery, Pricing, Evolution, Bootstrap) |

## Empire-Systems Flows (user-flows.md)

| Flow | Name | Description |
|---|---|---|
| [1](user-flows.md#flow-1-build-planner--manufacturing-workflow) | Build Planner — Manufacturing Workflow | Create plan, add items, allocate, check resources, generate delivery |
| [2](user-flows.md#flow-2-ship-build--template-to-assembly) | Ship Build — Template to Assembly | Create template, order build, manufacture components, assemble |
| [3](user-flows.md#flow-3-market-sale--cascade-to-manufacturing) | Market Sale — Cascade to Manufacturing | Sale → stock check → build items → resource check → delivery |
| [4](user-flows.md#flow-4-market-purchase--resource-fulfillment) | Market Purchase — Resource Fulfillment | Purchase → station hold → re-check shortfalls → status update |
| [5](user-flows.md#flow-5-supply-chain--mining-to-manufacturing) | Supply Chain — Mining to Manufacturing | Multi-source mining → pickup → refine → deliver |
| [6](user-flows.md#flow-6-queue-calculator) | Queue Calculator | Compute runs to fill target duration |
| [7](user-flows.md#flow-7-ship-template-modification--stock-target-cascade) | Ship Template Modification — Stock Target Cascade | Template change → stock target re-evaluation |
| [8](user-flows.md#flow-8-ship-instance--create-configure-load-cargo) | Ship Instance — Create, Configure, Load Cargo | Create from template, swap components, manage cargo |
| [9](user-flows.md#flow-9-station--create-manage-holds) | Station — Create, Manage Holds | Create station, manage per-player holds with crates |
| [10](user-flows.md#flow-10-stock-targets--plan-creation-and-order-generation) | Stock Targets — Plan Creation and Order Generation | Create plans, check levels, generate replenishment |
| [11](user-flows.md#flow-11-contacts--factions-and-external-characters) | Contacts — Factions and External Characters | Create factions, add characters, combo lookups |
| [12](user-flows.md#flow-12-asteroid--create-and-track-reserves) | Asteroid — Create and Track Reserves | Create asteroid, track reserves, link surveys |
| [13](user-flows.md#flow-13-supply-chain--define-and-monitor-pipeline) | Supply Chain — Define and Monitor Pipeline | Define stages, set thresholds, background monitoring |
| [14](user-flows.md#flow-14-warehouse-overflow--colony-tab) | Warehouse Overflow — Colony Tab | Set overflow rules, background delivery generation |
| [15](user-flows.md#flow-15-stock-profile--andor-composition) | Stock Profile — AND/OR Composition | OR within groups, AND across groups |
| [16](user-flows.md#flow-16-colony-plan--build-plan-generation) | Colony Plan → Build Plan Generation | Generate build items from unstaged colony structures |
| [17](user-flows.md#flow-17-multi-plan-consolidated-delivery) | Multi-Plan Consolidated Delivery | Consolidated resource + flatpack delivery across plans |

## Pre-Empire-Systems Flows (pre-empire-flows.md)

| Flow | Name | Description |
|---|---|---|
| [1](pre-empire-flows.md#flow-1-colony-import) | Colony Import | Copy HTML → parse → dedup → merge/create → miner setup → persist |
| [2](pre-empire-flows.md#flow-2-blueprint-import-individual) | Blueprint Import (Individual) | Copy HTML → parse → dedup → route global/player → persist |
| [3](pre-empire-flows.md#flow-3-blueprint-import-marketbulk) | Blueprint Import (Market/Bulk) | Copy market HTML → loop parse listings → dedup → route → persist |
| [4](pre-empire-flows.md#flow-4-survey-import) | Survey Import | Copy HTML → parse → dedup → merge/create → auto-create asteroid → persist |
| [5](pre-empire-flows.md#flow-5-player-profile-import) | Player Profile Import | Copy HTML → parse → match by name → update/create → persist |
| [6](pre-empire-flows.md#flow-6-colony-daily-build) | Colony Daily Build | Select route → show staged structures → Build All → timers start |
| [7](pre-empire-flows.md#flow-7-delivery-execution) | Delivery Execution | Select route+plan → load list → check off items → fulfillment → persist |
| [8](pre-empire-flows.md#flow-8-pricing-plan-calculation) | Pricing Plan Calculation | Set prices → select item → roll up BOM → display breakdown |
| [9](pre-empire-flows.md#flow-9-blueprint-evolution-graph) | Blueprint Evolution Graph | Select blueprint → Evolution tab → resolve chain → plot properties |
| [10](pre-empire-flows.md#flow-10-colony-bootstrap) | Colony Bootstrap | Click Bootstrap → check surveys → create starter structures → auto-configure |
| [11](pre-empire-flows.md#flow-11-colony-build-order-optimization) | Colony Build Order Optimization | Click Optimize → reorder structures → consider dependencies |
