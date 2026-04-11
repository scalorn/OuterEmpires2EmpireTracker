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

## Related Topics

- [Delivery Routes](delivery-routes.md) — Plan deliveries between colonies
- [Blueprints](blueprints.md) — Manage the flatpacks used to build structures
- [Background Processing](background-processing.md) — How structure timers are processed automatically
