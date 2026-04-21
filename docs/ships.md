# Ships

The Ships system covers two related forms: Ship Templates for designing reusable configurations, and Ships for tracking your actual fleet.

## Ship Templates

Open from **Manage → Ship Templates**.

### Creating a Template

1. Click **New** to start a fresh template.
2. Pick a **Hull** from the dropdown — this determines the available component slots.
3. Fill the component slots from your blueprint collection. Slots are typed (reactors, drives, weapons, cargo pods, shields, etc.), so only compatible blueprints appear in each slot's dropdown. Each slot has a filtered combo box — type part of a name in the filter field to narrow the list instantly.
4. Click **Save** to persist the template.

### Stats Panel

The stats panel updates live as you install components. At a glance you can see:

- **Mass** — total mass of hull plus all installed components
- **Power Balance** — reactor output vs component draw
- **Cargo Capacity** — available cargo volume
- **Defence Ratings** — shield and armour values
- **Propulsion** — drive output and resulting speed

This lets you experiment with loadouts without doing the math yourself.

### Order Build

When you're happy with a template, click **Order Build**. This generates manufacturing items for the hull and every installed component, feeding them directly into the Build Planner. One click and your entire ship is queued for production.

## Ships

Open from **Manage → Ships**.

### Creating a Ship

You can create ships two ways:

- **From Template** — select a template and click Create From Template. The hull and all components are copied over automatically.
- **Manually** — click New and configure the hull and components by hand.

### Overview Tab

The Overview tab shows all installed components in a grid. Each row has:

- **Component name and slot**
- **Condition** — current condition (editable). Track battle damage or wear over time.
- **Max Repair** — the maximum condition the component can be repaired to (editable). Some damage is permanent.
- **Hull Damage** — shown at the top, tracks damage to the ship's hull itself.

### Cargo Tab

The Cargo tab lets you manage what's loaded on the ship:

- **Cargo Hold** — the main hold for commodities, flatpacks, manufactured items, and refined resources.
- **Ore Hopper** — mining ships only. Restricted to unrefined purities (raw ore from mining operations).
- **Volume Tracking** — the form shows current volume vs capacity so you know how much space is left.

Toggle between the cargo hold and ore hopper using the selector at the top of the tab.

### Reference Counting and Delete Protection

Ships referenced by delivery plans or build items can't be deleted. The Refs column shows how many active references exist. Clear those references first if you need to remove a ship.

## Related Topics

- [Build Planner](build-planner.md) — Where Order Build sends manufacturing items
- [Blueprints](blueprints.md) — Component blueprints used in templates
- [Delivery Routes](delivery-routes.md) — Assign ships to delivery plans
