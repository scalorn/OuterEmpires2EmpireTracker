# OE2 Empire Tracker

A desktop companion app for **Outer Empires 2** that replaces your spreadsheets with a real tool built for managing a multi-colony empire.

If you're juggling colony structures, delivery logistics, blueprint evolution, and resource surveys across a dozen browser tabs and a Google Sheet that's held together with prayers — this is for you.

## What It Does

OE2 Empire Tracker pulls data straight from the game (copy-paste the HTML) and organizes everything into a single workspace where you can actually see what's going on across your empire.

### Colony Management
Import your colonies directly from the game browser. The tracker parses structures, warehouse inventory, and worker assignments automatically. No more manually typing structure names into cells. You get a live view of every colony's status — what's staged, building, online — and can optimize build order with one click.

### Background Timer Processing
The app simulates game timers locally. Mining cycles, refining jobs, research projects, and manufacturing runs all tick down in the background on a 60-second cycle. Open forms refresh automatically when timers expire. You see what's finishing next without alt-tabbing back to the game every few minutes.

### Blueprint Tracking & Evolution
Import blueprints from scanning or the market. Track evolution chains from level 0 through 15 with a visual graph showing how stats change at each level. Filter your collection by type, tech level, ship class, or evolution. Reference counting tells you which blueprints are actually in use across your colonies.

### Delivery Route Planning
Build delivery routes between colonies, then auto-fill plans based on what each colony actually needs — commodities, flatpacks, resources, workers. The Delivery Execution form walks you through each stop with checkboxes. When you check off a commodity delivery, the colony's request is automatically marked fulfilled. No more forgetting what goes where.

### Planet Surveys
Paste survey HTML and the tracker extracts planet name, resources, purities, and abundance data. Duplicate surveys are merged automatically. Use this to plan where to drop your next colony.

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
- [Player Profiles](docs/player-profiles.md)
- [Background Processing](docs/background-processing.md)
- [Window State](docs/window-state.md)

## About

Built by a developer who's been writing code since getting a Commodore Vic-20 with 5K of RAM at age 8. I'm a coder, not a designer — so yeah, it's not going to win any beauty contests. But it works, and it solves real problems I kept running into while managing my own empire. If you play OE2 and you're tired of spreadsheet hell, hopefully you'll find it useful too.

## Requirements

- Windows 10 or later
- .NET Framework 4.8.1 (included with Windows 10 May 2019 Update and later)

## License

This project is not affiliated with or endorsed by the developers of Outer Empires 2.
