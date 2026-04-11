# Delivery Routes

The Delivery Route system helps you plan and execute commodity and resource deliveries between your colonies. It consists of two forms: the Route Builder for planning, and the Delivery Execution form for carrying out deliveries step by step.

## Opening the Delivery Route Form

Open **Forms → Delivery Routes** from the menu bar.

## Route Builder

The Route Builder form has two main areas:

- **Left panel** — A filterable list of all delivery routes for the current player.
- **Right panel** — Route details with two tabs: Stops and Plan.

### Creating a Route

1. Click **New** to start a fresh route.
2. Enter a **Route Name**.
3. Use the colony dropdown to select a colony, then click **Add Stop** to add it to the route.
4. Repeat for each colony on the route.
5. Click **Save** to persist the route.

### Managing Stops

The Stops tab shows all colonies in the route as an ordered list:

- **Reorder** — Select a stop and use the **Up** / **Down** buttons to change its position.
- **Remove** — Select a stop and click **Remove** to delete it from the route.
- **Prevent Duplicates** — Check this option to hide colonies already in the route from the colony picker.

Each stop shows the colony name, planet name, and system name.

### Delivery Plans

A delivery plan specifies what items to pick up and drop off at each stop along a route. You can have multiple plans per route (e.g., one per delivery run).

#### Creating a Plan

1. Select a route from the list.
2. On the Plan tab, click **New Plan**. A plan is created with a default name based on the route name and current date.
3. Enter a **Plan Name** if you want to customize it.

#### Adding Items to a Plan

1. Select a stop on the Stops tab.
2. The Plan tab shows the selected stop's colony name.
3. Use the **Drop Off** section to add items you will deliver to this colony:
   - Select an item type (Resource, Commodity, Flatpack, etc.).
   - Filter and select the specific item.
   - For resources, select a purity level.
   - Click **Add** to add the item to the drop-off list.
4. Use the **Pick Up** section similarly for items you will collect from this colony.

#### Auto-Fill

Click **Auto-Fill** to automatically populate the plan based on colony needs. The auto-fill dialog lets you choose which categories to include:

- **Commodities** — Adds unfulfilled commodity requests from each colony.
- **Flatpacks** — Adds flatpacks needed for staged structures.
- **Resources** — Adds manufacturing resources needed by colony structures.
- **Workers** — Adds worker details needed by colony structures.

Auto-fill analyzes each colony along the route and generates appropriate pick-up and drop-off entries.

#### Managing Plans

- **Filter** — Use the plan filter to search by name.
- **Show Completed** — Check this to include completed plans in the dropdown.
- **Delete Plan** — Remove the selected plan.

## Delivery Execution

Open **Forms → Delivery Execution** from the menu bar, or click **Execute** on the Plan tab of the Route Builder.

### Executing a Delivery

1. Select a **Route** from the dropdown (use the filter to search).
2. Select a **Plan** from the dropdown.
3. The execution view shows:
   - **Load List** — A consolidated list of all items you need to load onto your ship before departing.
   - **Per-Stop Sections** — Each stop shows its drop-off and pick-up items as checkboxes.

4. As you deliver items in-game, check off each item in the execution view.
5. When all items at a stop are checked, a **Complete Stop** button appears. Click it to mark the stop as done and remove it from the view.
6. When all stops are complete, the plan is automatically marked as completed.

### Commodity Fulfillment

When you check off a commodity drop-off item, the tracker automatically marks the corresponding commodity request on the target colony as fulfilled. This keeps your colony data in sync with your deliveries.

### Flatpack Staging

When you check off a flatpack drop-off item, the corresponding colony structure is marked as staged, ready for building.

### Completing a Plan

- Plans are automatically completed when all items at all stops are delivered.
- You can also click **Complete Plan** to manually mark a plan as done.
- Completed plans are hidden from the plan dropdown by default (use "Show Completed" to see them).

## Related Topics

- [Colonies](colonies.md) — Colonies are the stops on your delivery routes
- [Blueprints](blueprints.md) — Flatpack blueprints are delivered as part of plans
- [Background Processing](background-processing.md) — Structure timers continue while you deliver
