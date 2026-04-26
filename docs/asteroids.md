# Asteroids

The Asteroid form tracks asteroid reserves and links them to player surveys. Use it to monitor resource depletion and see which players are mining each asteroid.

## Opening the Asteroid Form

Open **Manage → Asteroids** from the menu bar.

## Layout

The form uses a left-list / right-detail layout:

- **Left panel** — A filterable list of all asteroids. Type in the filter box to search by name or system.
- **Right panel** — Details for the selected asteroid: name, system, resource reserves, and linked surveys.

## Creating an Asteroid

1. Click **New** to create a blank asteroid.
2. Enter the **Name** and **System** for the asteroid.
3. Click **Save** to persist it.

The asteroid's UUID is generated deterministically from its system and name. Changing either field regenerates the UUID (with a confirmation warning).

## Managing Reserves

The Reserves grid shows the resource deposits on the asteroid. Each row has:

- **Resource** — The resource type (e.g., Iron, Copper, Titanium).
- **Purity** — High, Medium, or Low.
- **Max Reserve** — The total amount available when the asteroid was first surveyed.
- **Current Reserve** — How much remains. This column is editable — update it as you mine.
- **Reset** — The date the reserve was last replenished (if the game resets asteroid reserves).

### Adding a Reserve

1. Use the resource filter and dropdown to select a resource.
2. Select a purity level.
3. Enter the max and current reserve quantities.
4. Click **Add Reserve**.

### Removing a Reserve

Select a row in the reserves grid and click **Remove Reserve**.

## Linked Surveys

The Linked Surveys grid (read-only) shows which players have surveyed this asteroid and their mining yield rates. This is populated automatically from surveys that reference the asteroid.

## Delete Protection

Asteroids referenced by surveys, build items, or delivery route stops cannot be deleted. The reference count is shown in the list. Clear those references first if you need to remove an asteroid.

## Related Topics

- [Surveys](surveys.md) — Surveys link to asteroids for mining data
- [Colonies](colonies.md) — Mining rigs on colonies extract from asteroid reserves
- [Supply Chains](supply-chains.md) — Resource pipelines that source from asteroids
