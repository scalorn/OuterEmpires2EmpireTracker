<!-- Extracted from .kiro/specs/empire-systems/design.md — User Interaction Flows -->
# User Interaction Flows

Index of all flows with brief descriptions.

| Flow | Name | Description |
|---|---|---|
| [1](#flow-1-build-planner--manufacturing-workflow) | Build Planner — Manufacturing Workflow | Create plan, add items, allocate, check resources, generate delivery |
| [2](#flow-2-ship-build--template-to-assembly) | Ship Build — Template to Assembly | Create template, order build, manufacture components, assemble |
| [3](#flow-3-market-sale--cascade-to-manufacturing) | Market Sale — Cascade to Manufacturing | Sale → stock check → build items → resource check → delivery |
| [4](#flow-4-market-purchase--resource-fulfillment) | Market Purchase — Resource Fulfillment | Purchase → station hold → re-check shortfalls → status update |
| [5](#flow-5-supply-chain--mining-to-manufacturing) | Supply Chain — Mining to Manufacturing | Multi-source mining → pickup → refine → deliver |
| [6](#flow-6-queue-calculator) | Queue Calculator | Compute runs to fill target duration |
| [7](#flow-7-ship-template-modification--stock-target-cascade) | Ship Template Modification — Stock Target Cascade | Template change → stock target re-evaluation |
| [8](#flow-8-ship-instance--create-configure-load-cargo) | Ship Instance — Create, Configure, Load Cargo | Create from template, swap components, manage cargo |
| [9](#flow-9-station--create-manage-holds) | Station — Create, Manage Holds | Create station, manage per-player holds with crates |
| [10](#flow-10-stock-targets--plan-creation-and-order-generation) | Stock Targets — Plan Creation and Order Generation | Create plans, check levels, generate replenishment |
| [11](#flow-11-contacts--factions-and-external-characters) | Contacts — Factions and External Characters | Create factions, add characters, combo lookups |
| [12](#flow-12-asteroid--create-and-track-reserves) | Asteroid — Create and Track Reserves | Create asteroid, track reserves, link surveys |
| [13](#flow-13-supply-chain--define-and-monitor-pipeline) | Supply Chain — Define and Monitor Pipeline | Define stages, set thresholds, background monitoring |
| [14](#flow-14-warehouse-overflow--colony-tab) | Warehouse Overflow — Colony Tab | Set overflow rules, background delivery generation |
| [15](#flow-15-stock-profile--andor-composition) | Stock Profile — AND/OR Composition | OR within groups, AND across groups |
| [16](#flow-16-colony-plan--build-plan-generation) | Colony Plan → Build Plan Generation | Generate build items from unstaged colony structures |
| [17](#flow-17-multi-plan-consolidated-delivery) | Multi-Plan Consolidated Delivery | Consolidated resource + flatpack delivery across plans |