# Feature Backlog

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Activity Inactivity Mode ──── (complete)
Manufacturing Queue ────────── (standalone)
Pricing Plans ──────────────── (standalone)
Mass Blueprint Importer ────── (complete)
Systems & Planets Model ────── (standalone)

Ships ─────────────────┬──── Ship-Aware Delivery Execution
                       └──── (feeds into Stations)

Stations ──────────────┬──── Station Destinations in Routes
                       └──── (depends on Ships for player-owned station components)

Market ────────────────────── (depends on Pricing Plans for valuation)

Systems & Planets Model ───── Route Auto-Sequencing (depends on coordinates)
```

Suggested build order:
1. Manufacturing Queue, Pricing Plans, Systems & Planets Model (independent, any order)
2. Ships (enables ship-aware delivery and station components)
3. Ship-Aware Delivery Execution (depends on Ships)
4. Stations (depends on Ships for player-owned station features)
5. Station Destinations in Routes (depends on Stations)
6. Market (benefits from Pricing Plans)
7. Route Auto-Sequencing (depends on Systems & Planets Model)

---

## Open (Existing)

### BL-001: Delivery Routes — Phase 8: Ship Integration (REQ-DEL-070-071)
Ship cargo capacity modeling for delivery planning. Superseded by the broader "Ships" and "Ship-Aware Delivery Execution" items below.

### BL-002: Delivery Routes — Phase 9: Space Station Hubs (REQ-DEL-080-082)
Space station hub modeling for delivery route optimization. Superseded by the broader "Stations" and "Station Destinations in Routes" items below.

### BL-003: Delivery Auto-Fill — Time Horizon Parameter (REQ-DEL-061)
Add a time horizon parameter to flatpack auto-fill so it only includes structures expected to be built within a configurable window.

### BL-005: Global Blueprint Enhancements
From Recommendations.md #14:
- Visual indicator in blueprint list to distinguish global vs player-specific
- Permission/setting to gate global blueprint editing
- Separate file for global blueprints if BaselineData.json grows too large

### BL-006: Player Transfer UI
From Recommendations.md #13: UI for transferring colonies/blueprints/surveys between player profiles.

---

## New

### BL-011: Manufacturing Queue
**Dependencies:** None

A new form for recording what needs to be manufactured, for whom (any player in the game, not just tracked profiles), and in what quantity. Items in the queue can then be allocated to open manufactories, commodity factories, or research labs to be staged and worked on. Acts as a central work order system across all colonies.

### BL-012: Ships
**Dependencies:** None (but enables Ship-Aware Delivery Execution, and feeds into Stations)

Introduce the concept of ships as a core data model. Includes:
- Ship data model (name, owner, class, cargo capacity, installed components, etc.)
- Ship Designer form for configuring ship loadouts
- Per-character ship ownership tracking
- Ship blueprints (already partially modeled via blueprint types with ShipClass)

### BL-013: Ship-Aware Delivery Execution
**Dependencies:** Ships (BL-012)

Extend delivery execution plans to utilize specific ships. Ship cargo capacity limits how much can be picked up at each stop, which changes the planning algorithm — plans may need to be split across multiple trips or prioritized by cargo weight/volume.

### BL-014: Stations
**Dependencies:** Ships (BL-012) for player-owned station components/features

Introduce the concept of stations. Stations have holds that store items. Two types:
- Government (game-owned): fixed locations, no capacity limits currently
- Player-owned: eventually will have features similar to ships (installable components, permissions, etc.)

### BL-015: Station Destinations in Routes
**Dependencies:** Stations (BL-014)

Once stations exist, they become valid destinations in delivery routes alongside planets. Picking up and dropping off cargo at stations follows the same patterns as colony stops.

### BL-016: Pricing Plans
**Dependencies:** None

Define pricing models for resources and items. Should be able to calculate the price of a manufactured item based on the raw resources and time invested. Supports multiple pricing plans for different purposes (cost basis, market value, etc.).

### BL-017: Market
**Dependencies:** Pricing Plans (BL-016) for valuation context

Track in-game market activity: what was bought/sold, the price, and who the counterparty was. Provides a transaction history for the player's market dealings. Pricing Plans feed into understanding whether a trade was profitable.

### BL-019: Systems & Planets Model
**Dependencies:** None (but enables Route Auto-Sequencing)

Model star systems and planets more completely with coordinate data. Planet and jump point coordinates are available in the game UI. System-level coordinates may not be directly visible but could potentially be parsed from game JSON data. Once modeled, this enables travel time approximation and automatic delivery route sequencing.

### BL-020: Route Auto-Sequencing
**Dependencies:** Systems & Planets Model (BL-019)

With coordinate data for systems and planets, automatically sequence delivery route stops to minimize travel time. Approximate travel distances from coordinates and optimize stop order.

### BL-021: Player Profile Importer
**Dependencies:** None

Import player profile data from game HTML. Parse the in-game profile page to extract player name, faction, ranks, skill levels, credits, and other profile fields. Update existing profiles or create new ones. Follows the same HTML parsing pattern used by SurveyParser and the colony importer.

### BL-023: Explore OE2 Wiki as Data Source
**Dependencies:** None

Explore whether AI can read the OE2 wiki at https://atlasgamingcorp.com/outer-empires-2/ and pull useful game information from it. Could be a good source of data to enrich the spec and tool — game mechanics, structure types, resource details, etc.

### BL-024: AI-Generated User Help Documentation
**Dependencies:** None

Investigate whether AI can write user-facing help documentation for the tool based on the available spec files and codebase. Would generate how-to guides, feature descriptions, and workflow explanations from existing code and design docs.

### BL-025: Read Game Status Updates
**Dependencies:** None

Explore whether AI can read the game status/dev updates at https://game.dev.outerempires.net/ to stay current on game changes, new features, and balance adjustments that might affect the tracker.

### BL-026: Read outerempires.net for Game Information
**Dependencies:** None

Explore whether AI can read https://outerempires.net/ to pull in additional game information — lore, mechanics, community resources, etc.

### BL-027: Read Discord Channels for Game Info
**Dependencies:** None

Explore whether AI can read the Discord channels for the game. Discord is often where the latest game info, patch notes, and community knowledge lives. Would need to investigate API access or bot integration.

### BL-028: Organize Completed Specs into Subdirectories
**Dependencies:** None
**Status: Deprioritized** — Kiro's spec tooling references specs by path under `.kiro/specs/{feature_name}/`. Moving into subdirectories (`completed/`, `in-progress/`) would change paths and likely break `.config.kiro` references, task status tracking, and subagent delegation. Currently 16 spec folders — manageable. Revisit if count exceeds 30+ or Kiro adds explicit subdirectory support. The backlog already tracks completion status; no need to duplicate that via folder structure.

### BL-029: BaselineData.json — Separate User File with Merge Strategy
**Dependencies:** None

Investigate the consequences of splitting BaselineData.json into a read-only application-installed file and a user-editable copy. The user file starts as a copy of the application file. Key question: how to handle merging when the application adds a global blueprint that the player has also added manually — they'd have different UUIDs but matching data. How complicated would deduplication and merge logic get? Consider upgrade scenarios, conflict resolution, and whether a three-way merge is feasible.


### BL-030: Colony Administration Summary Report
**Dependencies:** None

The colony form's Administration tab should become a per-colony summary report. We already gather activity and inactivity data across all colonies — show the same data scoped to the individual colony on its Administration tab.

### BL-031: Colony Import Timestamp Tracking
**Dependencies:** None

When we import a colony, record the import timestamp inside the Colony data structure. Display this on the inactivity form as "you have not imported this colony for <countdown format> time." Notify if it has been more than a day, since stale imports miss commodity requests. This information also feeds into the colony Administration report.

### BL-032: Manufacturing Build Queue Calculator
**Dependencies:** None

Add a form/feature to calculate how many items to queue that would keep a manufactory busy for at least <countdown format> time.

### BL-033: RtfBuilder Font Style Support
**Dependencies:** None

Add support for font styles to RtfBuilder — bold, italics, strikethrough, etc.

### BL-034: Codebase Duplication Scan & Cleanup
**Dependencies:** None

We recently had a bug caused by duplicated blocks of code (e.g. `ExtractHtmlFragmentFromClipboardData` existed in both `FormBlueprint` and `BlueprintScanner`). Do a complete scan of the codebase looking for other instances of code duplication. Also look for code cleanliness opportunities, refactoring candidates, and general best-practices improvements.

### BL-035: Blueprint Evolution Graph
**Dependencies:** None

Add a chart to the Blueprint form showing the evolution chain for the selected blueprint. Use `System.Windows.Forms.DataVisualization.Charting` (already available in .NET Framework 4.8.1). Visualize the evolution path — e.g. base blueprint through each evolution level — showing key stats at each stage. Helps players understand the progression and plan which evolution level to target.

### BL-036: Colony Mining/Refining Production Graph
**Dependencies:** None

Add a chart to the Colony form showing mining and refining production over time. Use `System.Windows.Forms.DataVisualization.Charting`. Visualize resource output rates from mining rigs and refineries, helping players see production throughput at a glance and identify bottlenecks or underperforming structures.

### BL-037: Spreadsheet Data Migration Tool
**Dependencies:** None

Build a migration utility to import colony data from an existing OpenOffice Calc (.ods) spreadsheet (~80 tabs) into the application's PlayerData.json format. Use ExcelDataReader (NuGet) to read the .ods file programmatically — it supports .ods natively and returns a DataSet with one DataTable per sheet. The tool needs to map each tab's layout to the app's data model (Colony, ColonyStructure, Blueprint, Survey, etc.) and produce valid PlayerData.json output. This is likely a one-time or infrequent migration, so a simple console app or a dedicated form with a file picker would work. The main complexity is mapping the spreadsheet's ad-hoc layout to the structured data model — will need the actual spreadsheet to design the column mappings.


---

### BL-038: Survey Import Dedup
**Dependencies:** None

The survey clipboard import (`FormSurvey.cmdImport_Click`) currently writes parsed HTML directly into the selected survey with no dedup, no clipboard guard, and no error handling. Colony and Blueprint imports both have dedup logic; Survey is the outlier. Importing a survey for a different planet into the wrong selected survey silently corrupts data. Add survey import dedup following the same pattern as colony-import-dedupe: parse into temp, search by PlanetName+SurveyID, merge or create. Add a clipboard HTML guard and error handling. See AMB-038.


---

## MarketSample Coverage Gaps

Uncovered BlueprintTypes identified by the IconPositionExtractor's coverage gap report. These types exist in BaselineData.json but have no MarketSample HTML file to verify or populate their icon positions. Capture a MarketSample page for each type next time it appears in the in-game market.
