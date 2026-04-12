# Background Processing

OE2 Empire Tracker runs a background processor that automatically advances colony structure timers. This simulates the passage of time for mining, refining, research, and manufacturing operations without requiring you to manually trigger each cycle.

## How It Works

The background processor runs on a configurable timer cycle (default 60 seconds — adjust via **File → Preferences**). Each cycle:

1. Scans all colonies for structures with expired timers.
2. Processes each colony that has at least one expired timer.
3. Fires data-changed events so open forms refresh automatically.
4. Persists the updated data to disk.

Processing happens on a background thread, so the UI remains responsive during cycles.

## Status Bar Indicators

The main window status bar at the bottom shows two indicators:

### Next Process Timer

The **Next Process** label shows a countdown to the next processing cycle. The format adapts to the time remaining:

- `Next Process: 45s` — Less than a minute remaining.
- `Next Process: 2m 30s` — Minutes and seconds.
- `Next Process: 1h 15m 0s` — Hours, minutes, and seconds.
- `Next Process: --` — The processor is not running.

If the most recent cycle encountered an error, the timer text turns **red** to alert you.

### Performance Monitor

The **Mem / CPU** label shows current resource usage:

- **Mem** — Working set memory in megabytes.
- **CPU** — Approximate CPU usage percentage since the last check.

This helps you monitor whether the application is consuming excessive resources.

## What Gets Processed

The background processor handles colony structure timers. When a structure's timer expires, the colony's `ProcessColony` method runs, which may:

- Complete a mining cycle and add resources to the warehouse.
- Complete a refining operation and produce refined materials.
- Complete a research project.
- Complete a manufacturing job and produce the item.
- Complete a commodity production run.

Each processed colony triggers a `ColonyDataChanged` event, which causes any open Colony form or Colony Activity form to refresh and show the updated state.

## Error Handling

If an error occurs while processing a specific colony, the processor:

- Logs the error via NLog.
- Continues processing remaining colonies.
- Sets the `LastCycleHadError` flag, which turns the status bar timer red.

If an error occurs while saving data after processing, it is also logged and flagged.

## Lifecycle

- The processor **starts** automatically when the main window opens.
- The processor **stops** when the main window closes.
- If you use **File → New** or **File → Open**, the processor is stopped and restarted with the new data context.

## Related Topics

- [Colonies](colonies.md) — Colony structures are what the processor advances
- [Player Profiles](player-profiles.md) — Skill training timers are also tracked
- [Window State](window-state.md) — The main window manages the processor lifecycle
