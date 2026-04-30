# Build Planner

The Build Planner helps you organize and track manufacturing work across your empire. Open it from **Manage → Build Planner**.

## Build Plans

A build plan is a named batch of manufacturing work. Create as many plans as you need — one per colony expansion, one for ship components, one for market restocking, whatever makes sense for your workflow.

- **New** — creates a plan for the current player
- **Delete** — removes the plan and all its items (delivery plans are left in place)
- **Active** checkbox — inactive plans are grayed out and skip background processing

## Adding Items

Use the Add Item panel at the bottom of the form:

1. Pick a **Type** (Manufactory or Commodity)
2. Use the **Filter** box to narrow the item list
3. Select the **Item** from the dropdown
4. Set the **Quantity** (manufacturing runs)
5. Optionally set a **Recipient** (free text — who it's for or what to do with it)
6. Click **Add Item**

## Queue Calculator

Click **Queue Calc** to compute how many runs to queue for a target duration. Enter a time like "2d 12h 0m 0s" and the calculator figures out the runs needed to keep a structure busy for that long. The result populates the Quantity field.

## Structure Allocation

Each item needs to be allocated to a structure at a colony before manufacturing can begin. Double-click an item in the grid (or click **Allocate**) to open the allocation dialog. Filter by colony or structure name, toggle "Show idle only" to hide busy structures.

## Resource Shortfalls

Select an item in the grid to see its resource requirements. The shortfall panel shows what's needed, what's available at the target colony, and what's missing. Green checkmark means you're good to go. Red means you need to deliver resources first.

## Generating Delivery Plans

Click **Generate Delivery ▼** for three options:

- **Resource Delivery (This Plan)** — creates a delivery plan for missing resources in the current plan
- **Consolidated Resource Delivery** — merges resource needs across multiple plans into one delivery
- **Flatpack Delivery** — creates a delivery plan for completed flatpacks, grouped by destination colony

Each option asks you to pick a delivery route first.

## Auto-Assign

Click **Auto-Assign** to automatically distribute unallocated items across idle structures on a delivery route. The planner respects blueprint copy limits — if you only have 2 copies of a reactor blueprint, at most 2 manufactories can produce it in parallel. Review the proposed assignments before applying.

## Manufacturing Execution

Once items reach Ready status, you can track the manufacturing lifecycle directly from the build planner.

### Start Manufacturing

Select a Ready item and click **Start Manufacturing** (or right-click → Start Manufacturing). This pre-configures the structure in the tracker with the correct blueprint, quantity, and timer so the tool knows what to expect when you start the job in-game. The item advances to InProgress immediately.

Requirements for starting:
- Item must be Ready with an assigned structure
- Structure must be built and online with no active job
- Item must be the lowest sequence on that structure (earlier items go first)
- Any dependency must be completed

### Start All Ready

Click **Start All Ready** to batch-start manufacturing on all eligible Ready items in the plan. For each structure, only the first item in sequence order is started. A summary shows how many were started and how many were skipped (with reasons).

### Automatic Status Detection

The background processor automatically detects manufacturing progress:

- **Staged → Ready** — when resources are already present at the colony (skips the delivery step)
- **Ready → InProgress** — when the structure has a matching active job running
- **InProgress → Completed** — when the structure's job finishes

This means you can also start jobs in-game without using the build planner buttons, and the tracker will pick up the status change automatically.

### Status Summary

A summary line above the grid shows item counts per status: "Staged: 2 | Delivering: 1 | Ready: 3 | InProgress: 1 | Completed: 5". When all items finish, it shows "Complete (12 items)".

### Busy Indicators

Ready items whose assigned structure is currently busy show a pink **[BUSY]** indicator in the Location column. If another build plan is using the same structure, it shows **[BUSY: Plan Name]** so you know what's occupying it.

## Status Colors

Items in the grid are color-coded by status:

- **Staged** — default (no color), waiting for allocation or resources
- **Delivering** — blue, resources are being delivered
- **Ready** — green, resources available, ready to start in-game
- **In Progress** — yellow, manufacturing is running
- **Completed** — gray, finished
