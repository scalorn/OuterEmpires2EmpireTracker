# Stations

The Stations form lets you manage the stations you use across the galaxy — government outposts you trade at, player-owned bases you've built, and everything in between.

## Opening the Form

Open from **Manage → Stations**.

## Creating a Station

1. Click **New** to start a fresh station entry.
2. Enter the **Station Name** and **System**.
3. Set the **Ownership** — Government or Player-owned. This determines which tabs are available.
4. Set the **Type** — Outpost, Station, or Starbase.
5. Click **Save** to persist.

## Hold Tab

The Hold tab tracks your inventory at each station. This is your per-player view of what you've got stored there.

- **Adding Items** — use the item picker to add resources, commodities, flatpacks, or manufactured items.
- **Removing Items** — select an item and click Remove, or set quantity to zero.
- **Condition Tracking** — each item has editable Condition and Max Repair fields. Useful for tracking damaged components or equipment you've stashed at a station.
- **Volume** — the form shows total volume stored so you can keep an eye on capacity.

When you check off a delivery at a station during Delivery Execution, the station's hold updates automatically.

## Components Tab (Player-Owned Only)

Player-owned stations get a Components tab for installed equipment. This uses the same slot-based system as ships — each slot type accepts specific component blueprints. Install reactors, shields, weapons, and other equipment to track what's fitted to your station.

Each component has editable Condition and Max Repair fields for tracking wear and damage.

## Munitions Tab (Armed Stations)

Armed player-owned stations get a Munitions tab for tracking loaded ammunition. Add munition types and quantities to keep tabs on your station's defensive readiness.

## Stations as Delivery Route Stops

Stations can be added as stops on delivery routes alongside colonies and asteroids. When planning deliveries, stations appear in the stop picker. The Hold tab inventory is used for resource availability checks during delivery planning.

## Reference Counting and Delete Protection

Stations referenced by delivery routes, delivery plans, or build items can't be deleted. The Refs column in the station list shows how many active references exist. You'll need to clear those references before removing a station.

## Related Topics

- [Delivery Routes](delivery-routes.md) — Stations as delivery stops
- [Market](market.md) — Market listings are tied to stations
- [Stock Targets](stock-targets.md) — Station-scoped inventory targets
