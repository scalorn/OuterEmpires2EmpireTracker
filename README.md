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
Build delivery routes between colonies, stations, and asteroids, then auto-fill plans based on what each location actually needs — commodities, flatpacks, resources, workers. Each stop has a purpose (Cargo, Refuel, or both) and an optional fuel estimate. The Delivery Execution form walks you through each stop with checkboxes — when you check off a delivery at a station, the station's inventory updates automatically. Assign a ship to any delivery plan to see cargo volume and mass at a glance — the form warns you in red when you're over capacity and offers to split the load into multiple trips automatically.

### Build Planner
Plan and track your entire production pipeline. Create build plans with manufactory, commodity, mining, refining, and research items. Allocate items to specific structures at your colonies — the planner checks resource availability and highlights shortfalls. Items on the same structure run in sequence (time-splitting), and you can set dependencies between items so one waits for another to complete. Generate delivery plans for missing resources with one click, or consolidate across multiple plans. A queue calculator figures out how many runs to queue for a target duration. Auto-assign distributes items across idle structures while respecting blueprint copy limits. Pause plans with the Active toggle when you need to focus elsewhere.

### Contacts
Track factions and external characters you interact with. Open from **Manage → Contacts**. Create factions to organize characters, then add external characters and assign them to factions. Characters appear in combo lookups across the app (Recipient, Counterparty fields). Factions with active references can't be deleted — the Refs column shows how many characters and profiles reference each faction.

### Ship Templates
Design reusable ship configurations. Open from **Manage → Ship Templates**. Pick a hull, then fill component slots (reactors, drives, weapons, cargo pods, shields, etc.) from your blueprint collection. The stats panel updates live as you install components — see mass, power balance, cargo capacity, defence ratings, and propulsion at a glance. When you're ready to build, click Order Build to generate manufacturing items for the hull and every component, feeding directly into the Build Planner.

### Ships
Track your fleet. Open from **Manage → Ships**. Create ships manually or from a template (copies the hull and all components). The Overview tab shows installed components with editable condition and max repair fields for tracking damage. The Cargo tab lets you manage what's loaded — toggle between the main cargo hold and the ore hopper (mining ships only, restricted to unrefined purities). Ships referenced by delivery plans or build items can't be deleted.

### Stations
Manage the stations you use across the galaxy. Open from **Manage → Stations**. Create government or player-owned stations with type (Outpost, Station, Starbase). The Hold tab tracks your inventory at each station with editable condition and max repair fields. Player-owned stations get a Components tab for installed equipment (same slot-based system as ships) and a Munitions tab for armed stations. Stations referenced by delivery routes, plans, or build items can't be deleted.

### Market
Track your market activity. Open from **Manage → Market**. The Listings tab shows what you have for sale at each station, with condition tracking for damaged components. Record sales with one click — the listing quantity decrements and a transaction is created with condition and faction snapshots. The Transactions tab gives you a filterable history of all buys and sells (by type, item, counterparty, faction, station, date range). The Summary tab computes profit/loss totals with a per-item breakdown so you can see which items are actually making you money.

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
