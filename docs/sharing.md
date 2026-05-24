# Sharing

The Sharing system lets you control who can see your tracked data — blueprints, surveys, and colonies. You can share with specific characters, entire factions, or make data publicly visible to anyone.

## Opening the Form

Open **Manage → Sharing** from the main menu. The form loads your current sharing rules from the server automatically.

## How Sharing Works

Each sharing rule defines:

- **Target Type** — Who you're sharing with:
  - **Faction** — All members of a specific faction
  - **Character** — A specific character (by UUID)
  - **Public** — Anyone, including unauthenticated visitors
- **Target UUID** — The UUID of the faction or character you're sharing with (auto-filled for Public rules)
- **Data Type** — What you're sharing:
  - **All** — Colonies, Blueprints, and Surveys
  - **Colonies** — Only colony data
  - **Blueprints** — Only blueprint data
  - **Surveys** — Only survey data

## Managing Rules

### Adding a Rule

1. Click **Add Rule** to insert a new row in the grid.
2. Select a Target Type from the dropdown.
3. Enter the Target UUID (not needed for Public rules).
4. Select a Data Type.
5. Click **Save** to persist all rules to the server.

### Deleting a Rule

1. Select the row(s) you want to remove.
2. Click **Delete Selected**.
3. Click **Save** to persist the change.

### Validation

The **Save** button is disabled (greyed out) when any non-Public rule has an empty Target UUID. Fill in all Target UUID fields to enable saving.

### Reloading

Click **Reload** to discard unsaved changes and re-fetch rules from the server.

## Player Switching

When you switch the active player (via the player dropdown in the main window), the Sharing form automatically reloads rules for the new character.

## Public Data

Rules with Target Type set to **Public** make your data available through the public data endpoints. These endpoints require no authentication — anyone can browse publicly shared data without an account.

## Cross-App Consistency

Sharing rules are stored on the server. Rules created in the desktop app are visible in the web UI, and vice versa. Both apps use the same API endpoints, so changes are immediately reflected everywhere.

## Error Handling

- If the server is not connected, the grid is cleared and Save is disabled.
- If loading fails, an error message is shown and the grid remains empty.
- If saving fails, an error message is shown and your unsaved changes are preserved for retry.

## Server Connection Required

Sharing rules are managed entirely through the Remote Faction Server. If you're not connected to a server, the form will show an empty grid and saving is not available.
