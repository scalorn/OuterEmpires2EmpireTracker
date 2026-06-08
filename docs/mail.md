# Mail

The Mail form provides a local, searchable archive of all in-game messages. Messages sync automatically from the game API in the background, and the tracker maintains its own read/unread status independent of the game.

## Opening the Form

Open **View → Mail** from the main menu. An initial sync runs immediately to fetch any new messages.

## Reading Messages

The form shows a split layout:

- **Left panel** — List of messages with From, Subject, and Date columns
- **Right panel** — Full content of the selected message

Unread messages appear in **bold**. Selecting a message marks it as read automatically.

## Filtering

### Read Status

Use the first dropdown to show: All, Unread only, or Read only.

### Mail Type

Filter by message category:

- **All** — All messages
- **Player/System** — General messages from players or the game system
- **Colony** — Colony notifications
- **Research** — Research completion notifications
- **Skill** — Skill training notifications

### Search

Type in the search box to filter messages by From name, Subject, or content (case-insensitive match). A message appears if the term matches any of these fields.

All filters combine with AND logic.

## Title Bar

The title shows your unread count: "Mail (3 unread)" when there are unread messages, or just "Mail" when all are read.

## Background Sync

While the Mail form is open, messages sync automatically every 5 minutes (configurable). The status bar at the bottom shows:

- **Last sync time** — When the last successful sync completed
- **Sync progress** — "Syncing..." while a sync is in progress
- **Sync result** — "Synced: N new messages" or "Sync complete: up to date"

Sync stops when the form is closed to avoid unnecessary API calls.

## Error Handling

- If the API returns an authentication error, sync stops for this cycle and retries on the next interval
- If fetching a specific message fails, it is stored with empty content and sync continues
- Previously synced messages are never lost due to sync errors
