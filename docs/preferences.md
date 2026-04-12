# Preferences

The Preferences form lets you customize warning thresholds, processing intervals, and display refresh rates. Open it from **File → Preferences** in the main menu.

Changes take effect immediately — no restart required.

## Settings

### Structure Count Thresholds
Controls when the Structures tab turns yellow or red based on how many structures a colony has.

- **Yellow Threshold** (default: 60) — Tab turns yellow at this count.
- **Red Threshold** (default: 66) — Tab turns red at this count.

Yellow must be less than Red.

### Worker Request Due Window
Controls when the Workers tab warns you about upcoming commodity request deadlines.

- **Yellow Threshold** (default: 2d 0h 0m 0s) — Warns when a request is due within this window.
- **Red Threshold** (default: 1d 0h 0m 0s) — Urgent warning when due within this window.

Yellow must be greater than Red (yellow triggers earlier).

### Colony Import Staleness
Controls when the Administration tab warns you that a colony's imported data is getting old.

- **Yellow Threshold** (default: 5d 0h 0m 0s) — Warning after this much time since last import.
- **Red Threshold** (default: 6d 0h 0m 0s) — Urgent warning after this much time.

Yellow must be less than Red.

### Background Processing Interval
How often the background processor runs its timer cycle (default: 1m 0s). Lower values mean more frequent processing but slightly higher CPU usage.

### Administration Report Refresh
How often the colony Administration tab refreshes its status report (default: 1m 0s).

### Countdown Display Refresh Rate
How often countdown timers update across all forms — colony structures, colony activity, player skills, and the main window status bar (default: 1s). Minimum 1 second.

## Input Format

- Structure count thresholds accept plain integers (e.g. "60", "66").
- All time-based values use countdown format: `Xd Yh Zm Ws` (e.g. "2d 0h 0m 0s", "1m 0s", "30s"). Partial formats are fine — "5d" or "2h 30m" both work.

## Reset to Defaults

Click the **Reset** button to restore all values to their original defaults.
