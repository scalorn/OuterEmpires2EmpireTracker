# Delivery Execution

The Delivery Execution form guides you through a delivery plan stop by stop. It shows what to load, where to deliver it, and tracks your progress with checkboxes. As you check off items, the tracker automatically handles commodity fulfillment, flatpack staging, and worker delivery.

## Opening the Delivery Execution Form

Open **Manage → Delivery Execution** from the menu bar, or click **Execute** on the Plan tab of the Delivery Route form.

## Setup

1. **Select a route** — Use the route dropdown (with text filter) to pick a delivery route.
2. **Select a plan** — Use the plan dropdown to pick which delivery plan to execute.
3. **Assign a ship** — Select a ship from the dropdown. The ship's cargo capacity is shown below.

## Load List

The top section shows a consolidated list of everything you need to load onto your ship before departing. This is the combined total of all drop-off items across all stops. The header shows item count, total quantity, and calculated volume.

### Cargo Volume

The form shows current cargo volume and mass against your ship's capacity. If the total exceeds your ship's capacity, the **Split Trips** button appears. Click it to automatically split the plan into multiple trips that fit your ship.

## Stop-by-Stop Execution

Below the load list, each stop on the route is shown as a section with:

- **Drop Off** — Items to deliver at this stop. Check each item as you deliver it in-game.
- **Pick Up** — Items to collect from this stop. Check each item as you pick it up.

### What Happens When You Check Items

- **Commodity drop-off** — The corresponding commodity request on the target colony is marked as fulfilled.
- **Flatpack drop-off** — The corresponding colony structure is marked as staged, ready for building.
- **Worker drop-off** — Worker items are added to the colony's warehouse.
- **Unchecking** reverses these actions.

### Completing a Stop

When all items at a stop are checked, a **Complete Stop** button appears. Click it to mark the stop as done.

## Completing a Plan

Plans auto-complete when all stops are done. You can also click **Complete Plan** to manually mark a plan as finished. Completed plans are hidden from the plan dropdown by default — use "Show Completed" on the Delivery Route form to see them.

## Related Topics

- [Delivery Routes](delivery-routes.md) — Create routes and plans before executing
- [Colonies](colonies.md) — Delivery targets and commodity fulfillment
- [Ships](ships.md) — Ship cargo capacity affects trip splitting
- [Stations](stations.md) — Stations can be stops on delivery routes
