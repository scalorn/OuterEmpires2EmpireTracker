# Feature Backlog

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Activity Inactivity Mode ──── (standalone)
Manufacturing Queue ────────── (standalone)
Pricing Plans ──────────────── (standalone)
Mass Blueprint Importer ────── (standalone)
Systems & Planets Model ────── (standalone)

Ships ─────────────────┬──── Ship-Aware Delivery Execution
                       └──── (feeds into Stations)

Stations ──────────────┬──── Station Destinations in Routes
                       └──── (depends on Ships for player-owned station components)

Market ────────────────────── (depends on Pricing Plans for valuation)

Systems & Planets Model ───── Route Auto-Sequencing (depends on coordinates)
```

Suggested build order:
1. Activity Inactivity Mode, Manufacturing Queue, Pricing Plans, Mass Blueprint Importer, Systems & Planets Model (independent, any order)
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

### BL-010: Activity Inactivity Mode
**Dependencies:** None

Add an inactivity/idle highlighting mode to the Colony Activity window. Surfaces colonies or structures that are idle or underutilized — e.g., manufactories with no active job, mining rigs with no survey assigned, research labs not researching. Helps the player spot where capacity is being wasted.

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

### BL-022: JSON Serialization — Skip Default Values
**Dependencies:** None

JSON files (PlayerData.json, BaselineData.json) are growing large. As a space-saving measure, configure Newtonsoft.Json serialization to skip fields that have default values: null or empty strings, false booleans, and zero-value integers or decimals. This reduces file size without losing any data — deserialization already initializes these fields to their defaults.

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

