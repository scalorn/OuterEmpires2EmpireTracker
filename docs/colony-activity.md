# Colony Activity

The Colony Activity form is a real-time dashboard showing all active timers and commodity requests across your entire empire. Instead of clicking through each colony to check what's running, this form aggregates everything in one sortable, filterable view.

## Opening the Colony Activity Form

Open **Manage → Colony Activity** from the menu bar.

## Activity Types

Use the checkboxes at the top to filter which activity types are displayed:

- **Building** — Structures currently under construction.
- **Manufacturing** — Manufactories producing items.
- **CommodityMfg** — Commodity factories producing commodities.
- **CommodityReq** — Unfulfilled commodity requests with due dates.
- **Research** — Research labs running research projects.
- **Mining** — Active mining rigs (off by default — mining runs continuously and produces many rows).
- **Refining** — Active refineries (off by default for the same reason).

## Countdown Display

The grid shows one row per active process. The CountDown column updates every second with a live countdown. Rows are sorted by countdown ascending — the soonest completions appear at the top.

Overdue commodity requests show "OVERDUE" in red.

## Text Filter

Type in the filter box to search across all columns — colony name, system, activity type, structure name, or item being processed. The filter applies instantly as you type.

## Inactivity Mode

Toggle **Show Inactive** to switch from active timers to idle structures. In this mode, the form shows:

- Idle miners (built and online but not mining).
- Idle refineries (built and online but not refining).
- Idle manufactories, commodity factories, and research labs.
- Underutilized refiners (running but with warehouse stockpile that could support more).

This helps you spot structures that should be working but aren't.

## Import Staleness

The **Import Staleness** checkbox (on by default) adds rows for colonies that haven't been reimported recently. Each row shows how long since the last import (e.g., "5d 3h since last import"). Use this to identify colonies with stale data that might be missing commodity requests or structure changes.

## Related Topics

- [Colonies](colonies.md) — The source data for all activity rows
- [Background Processing](background-processing.md) — How timers are processed automatically
