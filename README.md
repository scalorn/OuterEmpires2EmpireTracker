# OE2 Empire Tracker

A desktop companion app for **Outer Empires 2** that replaces your spreadsheets with a real tool built for managing a multi-colony empire.

If you're juggling colony structures, delivery logistics, blueprint evolution, and resource surveys across a dozen browser tabs and a Google Sheet that's held together with prayers — this is for you.

## What It Does

OE2 Empire Tracker pulls data straight from the game (copy-paste the HTML) and organizes everything into a single workspace where you can actually see what's going on across your empire.

### Colony Management
Import your colonies directly from the game browser. The tracker parses structures, warehouse inventory, and worker assignments automatically. No more manually typing structure names into cells. You get a live view of every colony's status — what's staged, building, online — and can optimize build order with one click. The tracker also records when each colony was last imported and warns you when data gets stale — the Administration tab turns yellow at 5 days and red at 6 days, so you always know which colonies need a fresh import.

The Administration tab now includes a per-colony status report that shows you everything at a glance: what's building and when it finishes, unfulfilled commodity requests, idle structures that need attention, active manufacturing with batch completion estimates, and aggregated mining/refining rates. It refreshes automatically so the numbers stay current.

Mining rigs and refineries are fully configured on import. The tracker reads the game's mining rate, picks the best matching survey for each miner, starts the mining/refining timer, and seeds the warehouse with any missing resource slots. If you haven't imported a survey for that planet yet, a temporary default survey is created so everything still works. When you import a real survey later, miners automatically upgrade to it on the next reimport.

### Background Timer Processing
The app simulates game timers locally. Mining cycles, refining jobs, research projects, and manufacturing runs all tick down in the background (default 60-second cycle, configurable via Preferences). Open forms refresh automatically when timers expire. You see what's finishing next without alt-tabbing back to the game every few minutes.

### Preferences
Fine-tune the tracker to your play style. Open **File → Preferences** to configure warning thresholds (structure count, worker request due windows, colony import staleness), background processing interval, admin report refresh rate, and countdown display refresh rate. All values take effect immediately — no restart needed. Time-based values use the same countdown format you see everywhere else in the app (e.g. "2d 0h 0m 0s").

### Blueprint Tracking & Evolution
Import blueprints from scanning or the market. Track evolution chains from level 0 through 15 with a visual graph showing how stats change at each level. Filter your collection by type, tech level, ship class, or evolution. Reference counting tells you which blueprints are actually in use across your colonies.

### Delivery Route Planning
Build delivery routes between colonies, then auto-fill plans based on what each colony actually needs — commodities, flatpacks, resources, workers. The Delivery Execution form walks you through each stop with checkboxes. When you check off a commodity delivery, the colony's request is automatically marked fulfilled. No more forgetting what goes where.

### Build Planner
Plan and track manufacturing work across your empire. Create build plans, add manufactory and commodity items, and allocate them to specific structures at your colonies. The planner checks resource availability at each colony and highlights shortfalls so you know what needs delivering before you can start. Generate delivery plans for missing resources with one click, or consolidate across multiple plans for a single delivery run. A queue calculator figures out how many runs to queue to keep a structure busy for a target duration. Auto-assign distributes items across idle structures while respecting blueprint copy limits. Pause plans with the Active toggle when you need to focus elsewhere.

### Pricing Plans
Create pricing plans to assign credit values to resources and see what your commodities and manufactured items actually cost. Set base prices for Refined, S1, and S2 resources, add optional time costs (fixed per item + hourly rate), and the tracker computes rolled-up prices through the bill-of-materials chain. Create multiple plans to compare market value vs cost basis vs pessimistic estimates — whatever helps you decide if that manufacturing run is worth it.

### Planet Surveys
Paste survey HTML and the tracker extracts planet name, resources, purities, and abundance data. Duplicate surveys are merged automatically. Scan dates are displayed in the game's format but stored internally as proper dates, so the survey list sorts chronologically when you click the DateTime column. Edit dates with a calendar picker — no more typos. Use surveys to plan where to drop your next colony.

### Player Profiles & Skills
Track multiple characters with their skills, ranks, and faction. Import your profile directly from the game — copy the profile panel HTML and click Import. The tracker extracts your name, faction, credits, all three rank tracks, skill points, skill group states, individual skill levels, and training status. Skills like Builder, Extraction Focus, and Research Focus affect colony operations — the tracker accounts for them. Switch between players with a dropdown and all your data follows.

### Window State Persistence
Every form remembers its position, size, column widths, sort order, and filter state. Open multiple instances of the same form with different layouts. Your workspace is exactly how you left it next time you launch.

## Why Not a Spreadsheet?

| Spreadsheet | Empire Tracker |
|---|---|
| Manual data entry for every structure, resource, blueprint | Copy-paste import from the game browser |
| No timer simulation — you check the game manually | Background processing ticks down all timers automatically |
| Delivery planning is a nightmare of cross-references | Auto-fill delivery plans based on colony needs |
| Blueprint evolution is a wall of numbers | Visual evolution graph with stat comparisons |
| One tab per colony, gets unwieldy fast | All colonies in one searchable, filterable list |
| Breaks when you reorganize columns | Persistent window state, column order, and filters |
| Sharing data between characters? Good luck | Multi-profile support with one-click switching |

## Getting Started

1. Download the latest release and run `OE2EmpireTracker.exe`.
2. Create a player profile (Forms → Player Profiles).
3. Import your first colony by copying the colony page HTML from the game and clicking Import.
4. Explore from there — blueprints, surveys, delivery routes all work the same way.

Data is saved automatically to JSON files. No database, no cloud account, no setup beyond running the exe.

## In-App Help

Press **F1** on any form for context-sensitive help, or use **Help → Contents** (Ctrl+F1) to browse all topics. The full documentation is also available in the [docs/](docs/) folder.

## Documentation

- [Getting Started](docs/getting-started.md)
- [Colonies](docs/colonies.md)
- [Blueprints](docs/blueprints.md)
- [Surveys](docs/surveys.md)
- [Delivery Routes](docs/delivery-routes.md)
- [Build Planner](docs/build-planner.md)
- [Pricing Plans](docs/pricing-plans.md)
- [Player Profiles](docs/player-profiles.md)
- [Background Processing](docs/background-processing.md)
- [Window State](docs/window-state.md)
- [Preferences](docs/preferences.md)

## About

Built by a developer who's been writing code since getting a Commodore Vic-20 with 5K of RAM at age 8. I'm a coder, not a designer — so yeah, it's not going to win any beauty contests. But it works, and it solves real problems I kept running into while managing my own empire. If you play OE2 and you're tired of spreadsheet hell, hopefully you'll find it useful too.

## Requirements

- Windows 10 or later
- .NET Framework 4.8.1 (included with Windows 10 May 2019 Update and later)

## License

This project is not affiliated with or endorsed by the developers of Outer Empires 2.
