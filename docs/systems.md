# Systems

The Systems form lets you browse, search, and edit the galaxy's 23,631 star systems. Each system has coordinates, a grid location, spectral class, and optional faction ownership and infrastructure flags.

## Opening the Form

Open from **Manage → Systems** in the main menu.

## Searching Systems

Type in the search box at the top of the left panel to filter systems by name. The filter is case-insensitive and matches any part of the name.

## Viewing System Details

Click a system in the grid to see its full details in the right panel:

- **Id** — Game database identifier
- **Name** — System display name
- **X / Y** — Normalized coordinates (used for distance calculations)
- **Quadrant / Sector / Region / Locality** — Grid hierarchy (each 1-4)
- **Spectral Class** — Star type (M, K, G, F, W, X)
- **Faction Name / Color** — Owning faction (empty if unclaimed)
- **Infrastructure** — Orbital, Spaceport, and Starbase flags

## Editing Systems

The following fields are editable:

- **Faction Name** — The faction that controls this system
- **Faction Color** — Hex color code for the faction
- **Has Orbital / Has Spaceport / Has Starbase** — Infrastructure checkboxes

After making changes, click **Save** to persist them to SystemData.json.

Read-only fields (Id, Name, coordinates, grid location, spectral class) cannot be changed through the UI — they come from the galaxy data import.

## Re-importing Galaxy Data

Click **Re-import** to refresh all system data from the `oe2-galaxy-systems.json` source file. This overwrites any manual faction and infrastructure edits you've made, so the form asks for confirmation first.

## Distance Calculations

The tracker can compute Euclidean distances between any two systems using their normalized X/Y coordinates. This is used internally for colony-to-colony distance lookups (resolving Colony.SystemName to a star system) and will power future route optimization features.

## Tips

- Systems with no faction ownership have empty Faction Name/Color fields and FactionId = 0.
- The grid shows Id, Name, Grid Location (e.g. "Q1-S2-R3-L4"), Spectral Class, and Faction Name.
- Infrastructure flags help identify refueling points for future route planning.
- System data is stored separately in SystemData.json to keep BaselineData.json lean.
