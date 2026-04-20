# Stock Targets

Stock Targets let you set inventory goals and have the tracker tell you what's short. Instead of manually checking every colony and station, define what you want to have on hand and let the system do the counting.

## Opening the Form

Open from **Manage → Stock Targets**.

## Creating a Stock Plan

1. Click **New** to create a stock plan.
2. Give it a name (e.g., "War Reserves" or "Market Restock").
3. Click **Save**.

A stock plan is a named group of targets. Create as many as you need — one for combat readiness, one for market inventory, one for colony expansion supplies.

## Adding Targets

Each target defines what you want and how much:

- **Target Type** — Commodities, Ship Parts, Resources, or Ship Templates.
- **Item** — the specific item to track.
- **Quantity Goal** — how many you want to have on hand.
- **Critical Threshold** — the level below which the shortfall is flagged as critical.
- **Scope** — where to check inventory:
  - **Empire-wide** — counts across all colonies and stations.
  - **Specific Colony** — only counts inventory at one colony.
  - **Specific Station** — only counts inventory at one station.

## Ship Template Targets

When you add a ship template as a target, the system expands it into per-component checks. Instead of just saying "I need 3 Corvettes," it checks whether you have the hull, reactors, drives, weapons, and every other component needed to build 3 complete ships. This way you know exactly which parts are missing.

## Check & Generate Orders

Click **Check & Generate Orders** to scan your inventory against all targets in the plan. The system:

1. Counts your current inventory at the target's scope.
2. Compares against the quantity goal.
3. Color-codes the results:
   - **Green** — you're at or above the target. No action needed.
   - **Yellow** — below target but above the critical threshold.
   - **Red** — below the critical threshold. Time to act.
4. Auto-generates build items in a designated replenishment build plan for any shortfalls.

The generated build items feed into the Build Planner, so you can allocate structures and start manufacturing right away.

## Active / Inactive Toggle

Each stock plan has an **Active** checkbox. Inactive plans are grayed out in the list and skipped by background processing. Use this to pause seasonal plans or plans you're not actively working on without deleting them.

## Replenishment Build Plan Linkage

Each stock plan can be linked to a specific build plan for replenishment. When Check & Generate Orders creates build items, they go into this linked plan. If no plan is linked, you'll be prompted to select or create one.

## Related Topics

- [Build Planner](build-planner.md) — Where replenishment orders are sent
- [Ships](ships.md) — Ship template targets expand into component checks
- [Stations](stations.md) — Station-scoped inventory targets
- [Colonies](colonies.md) — Colony-scoped inventory targets
