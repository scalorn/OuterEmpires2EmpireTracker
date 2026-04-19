# Colonies

The Colony form is the central hub for managing your colonies in OE2 Empire Tracker. Here you can import colony data, manage structures, track warehouse inventory, and handle commodity requests.

## Opening the Colony Form

Open **Forms → Colonies** from the menu bar. The form has two main areas:

- **Left panel** — A searchable list of all colonies belonging to the current player.
- **Right panel** — Detailed information for the selected colony, organized into tabs.

## Importing a Colony

To import colony data from the game:

1. In the game browser, navigate to your colony page.
2. Select and copy the colony HTML (Ctrl+C).
3. In the Colony form, click **Import**.
4. The parser extracts the planet name, colony name, system, structures, and warehouse contents.
5. If a colony with the same planet name already exists, it will be updated. Otherwise a new colony is created.
6. Click **Save** to persist the imported data.

### Automatic Miner and Refinery Setup

When you import (or reimport) a colony, the tracker automatically configures mining rigs and refineries so they're ready to go:

- **Survey assignment** — Each active miner is matched to the best survey for its planet, resource, and purity. "Best" means the survey whose amount is closest to the game's reported mining rate. If you haven't imported a survey for that planet yet, a temporary default survey (marked "DEFAULT") is created so the miner still works.
- **Timer start** — If the game says a miner or refinery is actively running (mining rate > 0, or refinery is built and online), the tracker starts a repeating hourly timer aligned to the next clock-hour boundary — same as clicking Start manually.
- **Warehouse seeding** — Any resource being mined or refined gets a warehouse slot created automatically (quantity 0) if one doesn't already exist. This keeps the refinery UI working correctly.
- **Reimport preservation** — If you already have a valid survey assigned to a miner, reimporting won't overwrite it. If a default survey was assigned and you've since imported a real survey, the miner upgrades to the real one automatically.
- **Default survey cleanup** — After setup, any resources in the default survey that are no longer being mined are removed. If the default survey ends up empty, it's deleted.

## Colony List

The left panel shows all colonies for the current player. You can:

- **Filter** — Type in the search box to filter colonies by planet name or colony name.
- **Sort** — Click column headers (Planet, Name) to sort ascending or descending.
- **Select** — Click a colony to load its details in the right panel.

## Colony Details

At the top of the right panel you will see:

- **Planet Name** — The name of the planet where the colony is located.
- **Colony Name** — Your name for this colony.
- **System Name** — The star system containing the planet.

## Structures Tab

The Structures tab shows all structures in the selected colony. Each structure is displayed as a control block showing:

- **Structure type** — The blueprint name of the flatpack used to build it.
- **Status** — Whether the structure is staged, building, built, or online.
- **Workers** — Worker assignments and capacity.
- **Processing timer** — Countdown for active mining, refining, research, or manufacturing operations.

### Adding a Structure

1. Use the flatpack filter and dropdown at the top of the Structures tab to find a blueprint.
2. Select the flatpack from the dropdown.
3. Click **Add Flatpack** to add the structure to the colony.

### Structure Lifecycle

Structures follow this lifecycle:

1. **Staged** — The flatpack has been delivered but not yet built.
2. **Building** — Construction is in progress (timer counting down).
3. **Built** — Construction is complete; the structure can be brought online.
4. **Online** — The structure is active and can process resources.

### Optimizing Build Order

Click **Optimize** to reorder structures for optimal build sequence. The optimizer considers dependencies and resource availability.

### Bootstrapping

Click **Bootstrap** to automatically add a standard set of starter structures to a new colony. The colony must have a planet name set before bootstrapping.

### Generate Build Plan

Click **Generate Build Plan** on the Administration tab to create manufacturing orders for a colony's unstaged structures. The dialog lets you create a new plan (auto-named after the colony) or add to an existing one. The planner scans for structures that haven't been staged or built yet and creates flatpack build items for each. Open the Build Planner form to allocate, check resources, and generate deliveries.

## Warehousing Tab

The Warehousing tab shows the colony's inventory of items, organized by type. Each row shows:

- **Item type** — Resource, commodity, flatpack, etc.
- **Name** — The item name.
- **Purity** — For resources, the purity level.
- **Quantity** — How many are in the warehouse.

## Workers Tab

The Workers tab manages commodity requests for the colony. Commodity requests tell the delivery system what commodities the colony needs. Each request includes:

- **Commodity name** — Which commodity is needed.
- **Quantity** — How many are requested.
- **Need By date** — When the commodity is needed.
- **Fulfilled** — Whether the request has been satisfied.

The tab title shows the count of active (unfulfilled, not expired) requests.

## Colony Status

The status panel on the Structures tab shows a summary of the colony's current state, including:

- Structure counts by status (staged, building, built, online).
- Resource production and consumption rates.
- Worker allocation summary.

## Import Staleness Tracking

The tracker records when each colony was last imported. This helps you spot colonies with outdated data that might be missing commodity requests or structure changes.

### Administration Tab Warning

The Administration tab on the Colony form changes color based on how long it's been since the colony was imported:

- **No color** — Imported within the last 5 days. Data is fresh.
- **Yellow** — 5 to 6 days since last import. Consider reimporting soon.
- **Red** — 6+ days since last import (or never imported). Data is stale — reimport this colony.

### Administration Tab — Colony Status Report

The Administration tab also displays a per-colony status report that gives you a quick overview of what's happening in the selected colony. The report is organized into sections, most actionable first:

- **Building** — Structures currently under construction, sorted by soonest completion. Shows countdown and estimated local time.
- **Commodity Requests** — Unfulfilled commodity requests with quantities and due dates.
- **Inactivity** — Idle structures grouped by type (stale imports, idle miners, idle refineries, idle manufactories, idle commodity factories, idle research labs, underutilized refiners). Tells you what needs attention.
- **Activity** — Active manufacturing, commodity manufacturing, and research processes with countdown timers. Multi-quantity manufacturing shows both next-item and full-batch completion times.
- **Mining** — Aggregated mining rates per resource and purity across all active miners.
- **Refining** — Aggregated refining rates per resource and purity across all active refiners.

The report refreshes every 60 seconds, when you select a different colony, and when colony data changes externally (e.g., from background processing or delivery fulfillment).

### Colony Activity — Inactivity Mode

The Colony Activity form's inactivity mode (toggle "Show Inactive") includes an "Import Staleness" filter. When enabled, colonies that haven't been imported in over 24 hours appear as staleness rows showing elapsed time since the last import (e.g., "5d 3h since last import"). Use this to quickly see which colonies across your empire need a fresh import.

## Related Topics

- [Delivery Routes](delivery-routes.md) — Plan deliveries between colonies
- [Supply Chains](supply-chains.md) — Resource pipeline modeling

## Overflow Tab

The Overflow tab lets you set rules for automatically moving excess resources out of a colony's warehouse. Each rule specifies:

- **Resource and Purity** — Which resource to monitor
- **Threshold** — The quantity above which overflow is triggered
- **Destination** — Where to send the excess (a colony or station)
- **Route** — The delivery route to use for the overflow delivery
- **Active** — Toggle to pause/resume the rule without deleting it

The Current column is color-coded: green when below threshold, yellow when within 20%, red when at or above threshold. Inactive rules are grayed out and skipped by background processing.

One rule per resource+purity per colony. The background processor checks overflow rules on each tick and logs when thresholds are exceeded.
- [Blueprints](blueprints.md) — Manage the flatpacks used to build structures
- [Background Processing](background-processing.md) — How structure timers are processed automatically
