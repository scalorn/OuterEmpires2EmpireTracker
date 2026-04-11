# Blueprints

The Blueprint form lets you manage all your discovered blueprints — from system object scanners and ship components to flatpacks for colony structures. You can import blueprints from the game, track evolution chains, and filter your collection.

## Opening the Blueprint Form

Open **Forms → Blueprints** from the menu bar. The form has two main areas:

- **Left panel** — A filterable list of all blueprints (player-owned and global).
- **Right panel** — Detailed information for the selected blueprint, organized into tabs.

The title bar shows the count of global blueprints and player-owned blueprints.

## Blueprint List and Filters

The left panel provides several ways to narrow down your blueprint collection:

- **Text filter** — Type in the search box to filter by blueprint name.
- **Type filter** — Select a blueprint type (e.g., Flatpack, Scanner, Ship Component).
- **Class filter** — Filter by ship class (for non-universal blueprints).
- **Tech Level filter** — Filter by tech level.
- **Evolution filter** — Filter by evolution level. Check "And Above" to include all evolutions at or above the selected level.
- **Clear Filters** — Reset all filters to show everything.

Click column headers to sort the list by Type, Name, Tech Level, Evolution, Nick Name, or reference count.

## Creating a Blueprint

1. Click **New** to clear the form.
2. Select a **Blueprint Type** from the dropdown.
3. Enter the **Name** of the blueprint.
4. For non-universal types, select the **Ship Class** and **Tech Level**.
5. Set the **Evolution** level (defaults to 0).
6. Optionally set a **Nick Name** for easy identification.
7. Fill in the **Statistics** tab with the blueprint's properties.
8. Add **Resources** required for manufacturing on the Resources tab.
9. Click **Save**.

## Importing Blueprints

### Scanning Import

The Blueprint Scanner can parse blueprint data copied from the game browser. This is useful for bulk-importing blueprints you have discovered in-game.

### Market Import

Market blueprints can be imported from the game's market interface. The market importer parses the HTML to extract blueprint details including type, stats, and resource requirements.

## Blueprint Properties

Each blueprint type defines a set of numeric properties (e.g., Hull Points, Shield Capacity, Cargo Space). These are shown in the **Statistics** tab as a grid where you can view and edit each property value.

Property values change as blueprints evolve. The Evolution Graph tab visualizes how properties change across evolution levels.

## Evolution Chains

Blueprints in OE2 can evolve through multiple levels (0–15). Each evolution level may change the blueprint's properties. The tracker supports:

- **Base Blueprint** — Link a blueprint to its base (evolution 0) version using the Base Blueprint dropdown.
- **Evolution Graph** — The Evolution Graph tab shows a line chart plotting each property's percentage change from the evolution 0 value across all evolution levels in the chain.
- **Property Checkboxes** — Toggle which properties are displayed on the graph.

If no property changes are found across the chain, a message is displayed instead of the graph.

## Global vs Player Blueprints

- **Player blueprints** are owned by a specific player profile and appear in that player's list.
- **Global blueprints** are shared across all players. Check the **Global** checkbox before saving to make a blueprint global.

Global blueprints are useful for shared game data like standard ship components that all players can reference.

## Reference Counting

The "Refs" column in the blueprint list shows how many times each blueprint is referenced by colony structures or other blueprints. This helps you identify which blueprints are actively in use.

## Related Topics

- [Colonies](colonies.md) — Flatpack blueprints are used to build colony structures
- [Surveys](surveys.md) — Scanner blueprints are used when conducting surveys
- [Getting Started](getting-started.md) — Initial setup and workflow overview
