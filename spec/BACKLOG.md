# Feature Backlog

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Activity Inactivity Mode ──── (complete)
Manufacturing Queue ────────── (standalone)
Pricing Plans ──────────────── (complete)
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

### BL-069: Migrate Existing BindingList Fields to List on PlayerContext
**Dependencies:** None
**Status: Deferred** — New empire systems entities use `List<T>` from the start. Existing entities (Blueprint, Colony, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, PricingPlan) still use `BindingList<T>` on PlayerContext. Forms already build their own display lists from filtered queries, so the BindingList change notifications aren't providing value. Migrate the existing fields to `List<T>` for consistency. Touches many files — do as a standalone cleanup pass.

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

### BL-017: Market
**Dependencies:** Pricing Plans (BL-016) for valuation context

Track in-game market activity: what was bought/sold, the price, and who the counterparty was. Provides a transaction history for the player's market dealings. Pricing Plans feed into understanding whether a trade was profitable.

### BL-019: Systems & Planets Model
**Dependencies:** None (but enables Route Auto-Sequencing)

Model star systems and planets more completely with coordinate data. Planet and jump point coordinates are available in the game UI. System-level coordinates may not be directly visible but could potentially be parsed from game JSON data. Once modeled, this enables travel time approximation and automatic delivery route sequencing.

### BL-020: Route Auto-Sequencing
**Dependencies:** Systems & Planets Model (BL-019)

With coordinate data for systems and planets, automatically sequence delivery route stops to minimize travel time. Approximate travel distances from coordinates and optimize stop order.

### BL-023: Explore OE2 Wiki as Data Source
**Dependencies:** None

Explore whether AI can read the OE2 wiki at https://atlasgamingcorp.com/outer-empires-2/ and pull useful game information from it. Could be a good source of data to enrich the spec and tool — game mechanics, structure types, resource details, etc.

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
**Status: Rejected** — The split-file approach adds merge-on-load complexity, "which file wins" ambiguity, and user confusion. With deterministic UUIDs + versioned migrations + idempotent renames (see `spec/discussions/baseline-data-stability.md`), a single BaselineData.json handles upgrades cleanly without a separate user file.

### BL-032: Manufacturing Build Queue Calculator
**Dependencies:** None

Add a form/feature to calculate how many items to queue that would keep a manufactory busy for at least <countdown format> time.

### BL-033: RtfBuilder Font Style Support
**Dependencies:** None
**Status: Deprioritized** — Current consumers (ColonyStatusCalculator, ColonyStructure control, ColonyAdminReportBuilder) use color effectively to distinguish headers, values, and status. No current feature is limited by the lack of font styles.

Add support for font styles to RtfBuilder — bold, italics, strikethrough, etc.

**Implementation notes:** Add an overload `Append(string text, Color color, FontStyle style = FontStyle.Regular)` using `System.Drawing.FontStyle` flags. Wrap text in RTF control words (`\b...\b0`, `\i...\i0`, `\strike...\strike0`). Backward compatible via default parameter. ~30 minutes of work when a specific need arises.

### BL-036: Colony Mining/Refining Production Graph
**Dependencies:** None

Add a chart to the Colony form showing mining and refining production over time. Use `System.Windows.Forms.DataVisualization.Charting`. Visualize resource output rates from mining rigs and refineries, helping players see production throughput at a glance and identify bottlenecks or underperforming structures.

### BL-070: Blueprint Evolution — Capped Property Ranges
**Dependencies:** None

Some blueprint properties evolve with +/- 50% of the base value, but others are capped to a range of 0–100 (e.g. Max Repair — you wouldn't want more than 100% max repair). The evolution graph and any future evolution prediction need to account for both modes. Per the game developer: "sometimes it's +/- 50%, sometimes it's the +/- 50% of game to 100." Need to identify which properties use which mode and adjust the evolution graph normalization accordingly.

### BL-071: TreeDataGridView — Tree-in-Grid Custom Control for Inventory
**Dependencies:** None
**Status: Under consideration** — may or may not implement depending on how the master-detail crate UI pattern works out in practice.

Build a custom `TreeDataGridView` control extending DataGridView that supports expandable/collapsible parent-child rows. Primary use case: displaying crate contents inline in inventory grids instead of using a separate master-detail panel (which eats vertical space).

**Discussion:** The master-detail pattern (select a crate, see contents in a grid below) works but doubles the vertical space needed. A tree-in-grid keeps everything in one grid with expand/collapse glyphs on crate rows. WinForms DataGridView is fundamentally flat, so this requires manual bookkeeping: child rows are hidden/shown on expand/collapse, indented via cell padding, and grouped under their parent during sort/filter.

**Complexity:** ~200-300 lines for one-level deep (current crate model — no nesting). Crate rows get ▶/▼ glyph via CellPainting, child rows track their parent via tag/index, expand/collapse toggles Visible on children. Sorting must keep children grouped under parents. Filtering a parent hides its children.

**Future-proofing for nested crates:** If crates ever support nesting, use a `Depth` integer per row instead of a boolean IsChild flag. Indentation = depth × indent pixels. Expand/collapse hides all descendants recursively. Adds ~50 lines over the flat version. The game doesn't support nested crates today but the dev hasn't ruled it out.

**Decision point:** Try master-detail first during Iteration 4 (Stations). If vertical space is a problem in practice, build this control as a replacement. Design the control with depth support from the start so nesting is incremental if it comes.


---
### BL-045: GitHub MCP Integration
**Dependencies:** None (blocked by Docker installation issues)

Set up the GitHub MCP server so Kiro can read/write GitHub issues, PRs, and wiki pages directly. Enables intake of external issues, PR review, and maintaining user documentation in the repo wiki. Requires Docker Desktop (or the standalone Go binary from github/github-mcp-server releases) plus a fine-grained GitHub Personal Access Token scoped to the repo with Issues, Pull Requests, Contents, and Metadata permissions. Configuration goes in `.kiro/settings/mcp.json`. Currently blocked — Docker won't install on the dev machine. Revisit when Docker is available or try the standalone binary approach.

### BL-047: Game API Integration Planning
**Dependencies:** None

There will be a game API eventually. Plan the integration architecture now so we're ready when it arrives. Consider: authentication, polling vs push, data model mapping, how it replaces clipboard import, and what new capabilities it enables (real-time sync, automated queue management, etc.).

### BL-050: Default Survey Duplicate on Out-of-Order Import
**Dependencies:** None

Bug: When colonies and surveys are imported in the wrong order, duplicate default surveys can still be created (2 surveys for the same planet). Reimporting the colony removes one but not both. The cleanup logic in CleanupDefaultSurvey should handle all duplicates in a single pass, and CreateOrUpdateDefaultSurvey should be more aggressive about finding and consolidating existing defaults.

**Diagnostic logging added:** SetupMiners now logs a snapshot of all DEFAULT surveys for the colony's planet before and after processing (count + UUIDs). CreateOrUpdateDefaultSurvey logs the deterministic UUID it's searching for and whether a match was found. CleanupDefaultSurvey logs all DEFAULT surveys for the planet (regardless of owner) before cleanup. A `BL-050 DIAGNOSTIC` warning fires if multiple DEFAULT surveys remain after SetupMiners completes. Check the NLog output after importing colonies to reproduce.

### BL-055: Mining/Refining/Manufacturing/Research Queue System
**Dependencies:** BL-047 (Game API Integration Planning)

Build an empire-wide queue system for mining, refining, manufacturing, and research operations. Should support both per-colony and empire-wide views. Designed for when the game API becomes available — queued operations can be submitted automatically. Includes priority ordering, dependency tracking (e.g. refine before manufacture), and estimated completion times.

### BL-059: Manufacturing Build Queue — Auto-Create Orders from Fill Levels
**Dependencies:** BL-011 (Manufacturing Queue)

Add a feature to automatically create manufacturing orders based on target stock levels. Example: "always keep 10,000 2cm Coilgun Munitions on hand" or "always keep 2,000 Joybots on hand." The system checks current warehouse quantities across the empire and creates orders to replenish shortfalls.

---

## ~~MarketSample Coverage Gaps~~ — RESOLVED

All BlueprintTypes now have HTML coverage. No gaps detected (confirmed by IconPositionExtractor coverage gap report in test output).
