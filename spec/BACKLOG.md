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

### Delivery Routes — Phase 8: Ship Integration (REQ-DEL-070-071)
Ship cargo capacity modeling for delivery planning. Superseded by the broader "Ships" and "Ship-Aware Delivery Execution" items below.

### Delivery Routes — Phase 9: Space Station Hubs (REQ-DEL-080-082)
Space station hub modeling for delivery route optimization. Superseded by the broader "Stations" and "Station Destinations in Routes" items below.

### Delivery Auto-Fill — Time Horizon Parameter (REQ-DEL-061)
Add a time horizon parameter to flatpack auto-fill so it only includes structures expected to be built within a configurable window.

### ~~Commodity Fulfillment Unit Tests~~
~~Optional task from commodity-delivery-loop spec (task 5.2). Extract fulfillment logic from the Form handler into a testable static helper and add unit tests.~~
**Status: Complete** — Extracted `DeliveryFulfillment` static helper with `FulfillCommodity`, `StageFlatpack`, `DeliverWorkers`. 17 unit tests covering all three operations.

### Global Blueprint Enhancements
From Recommendations.md #14:
- Visual indicator in blueprint list to distinguish global vs player-specific
- Permission/setting to gate global blueprint editing
- Separate file for global blueprints if BaselineData.json grows too large

### Player Transfer UI
From Recommendations.md #13: UI for transferring colonies/blueprints/surveys between player profiles.

### ~~Skill Multipliers — Timer Processing~~
~~From Recommendations.md #13 Phase 4: ProductionFocus/Builder/ResearchFocus time reductions deferred until timer processing is fully implemented.~~
**Status: Complete** — ProductionFocus applied to manufacturing and commodity factory cycle times. ResearchFocus applied to research times. Builder was already applied to build times.

---

## New

### Activity Inactivity Mode
**Dependencies:** None

Add an inactivity/idle highlighting mode to the Colony Activity window. Surfaces colonies or structures that are idle or underutilized — e.g., manufactories with no active job, mining rigs with no survey assigned, research labs not researching. Helps the player spot where capacity is being wasted.

### Manufacturing Queue
**Dependencies:** None

A new form for recording what needs to be manufactured, for whom (any player in the game, not just tracked profiles), and in what quantity. Items in the queue can then be allocated to open manufactories, commodity factories, or research labs to be staged and worked on. Acts as a central work order system across all colonies.

### Ships
**Dependencies:** None (but enables Ship-Aware Delivery Execution, and feeds into Stations)

Introduce the concept of ships as a core data model. Includes:
- Ship data model (name, owner, class, cargo capacity, installed components, etc.)
- Ship Designer form for configuring ship loadouts
- Per-character ship ownership tracking
- Ship blueprints (already partially modeled via blueprint types with ShipClass)

### Ship-Aware Delivery Execution
**Dependencies:** Ships

Extend delivery execution plans to utilize specific ships. Ship cargo capacity limits how much can be picked up at each stop, which changes the planning algorithm — plans may need to be split across multiple trips or prioritized by cargo weight/volume.

### Stations
**Dependencies:** Ships (for player-owned station components/features)

Introduce the concept of stations. Stations have holds that store items. Two types:
- Government (game-owned): fixed locations, no capacity limits currently
- Player-owned: eventually will have features similar to ships (installable components, permissions, etc.)

### Station Destinations in Routes
**Dependencies:** Stations

Once stations exist, they become valid destinations in delivery routes alongside planets. Picking up and dropping off cargo at stations follows the same patterns as colony stops.

### Pricing Plans
**Dependencies:** None

Define pricing models for resources and items. Should be able to calculate the price of a manufactured item based on the raw resources and time invested. Supports multiple pricing plans for different purposes (cost basis, market value, etc.).

### Market
**Dependencies:** Pricing Plans (for valuation context)

Track in-game market activity: what was bought/sold, the price, and who the counterparty was. Provides a transaction history for the player's market dealings. Pricing Plans feed into understanding whether a trade was profitable.

### Mass Blueprint Importer
**Dependencies:** None

Import blueprints in bulk from the in-game market HTML. The market sells global base blueprints — this tool would parse that HTML and automatically create blueprint entries. Key challenge: deduplication. Blueprints with the same name + type + evolution can have different properties (since you can research the same blueprint multiple times). Need to compare property values to detect true duplicates vs distinct evolutions. There may or may not be a unique game ID hidden in the HTML data.

### Systems & Planets Model
**Dependencies:** None (but enables Route Auto-Sequencing)

Model star systems and planets more completely with coordinate data. Planet and jump point coordinates are available in the game UI. System-level coordinates may not be directly visible but could potentially be parsed from game JSON data. Once modeled, this enables travel time approximation and automatic delivery route sequencing.

### Route Auto-Sequencing
**Dependencies:** Systems & Planets Model

With coordinate data for systems and planets, automatically sequence delivery route stops to minimize travel time. Approximate travel distances from coordinates and optimize stop order.

### Player Profile Importer
**Dependencies:** None

Import player profile data from game HTML. Parse the in-game profile page to extract player name, faction, ranks, skill levels, credits, and other profile fields. Update existing profiles or create new ones. Follows the same HTML parsing pattern used by SurveyParser and the colony importer.
