# Game Constraints & Design Tradeoffs

## User Goal

This file documents hard limits imposed by the game, design tradeoffs we've accepted, and UI constraints — providing context for why certain design decisions were made and what boundaries the tracker operates within.

## Game-Mechanic Hard Limits

These values are defined by the game. The game can change them at any time; if it does, update this file and any affected logic.

**REQ-GC-001** Max structures per colony SHALL be 65.
**REQ-GC-002** Max ships per player: no game-imposed limit.
**REQ-GC-003** Colony warehouse capacity is determined by the sum of warehouse structure capacities (no global cap beyond what structures provide).
**REQ-GC-004** Station hold capacity: no limit.
**REQ-GC-005** Max colonies per player: no hard limit, but commodity requests create a practical soft cap — unfulfilled requests eventually cause colony revolt and independence.
**REQ-GC-006** Max blueprints per player: no game-imposed limit.
**REQ-GC-007** Max blueprint evolution level: 15 (if the blueprint type supports research).
**REQ-GC-008** Max workers per structure: defined by the blueprint's worker slot properties (no global cap).
**REQ-GC-009** Max skill level: 10 per skill.
**REQ-GC-010** Ship class range: 2 through 8. There are no class 1 ships.
**REQ-GC-011** Max components/slots on a ship: defined by the hull blueprint (slot types, counts, plus power/engineering capacity fields). No global cap beyond hull definition.
**REQ-GC-012** Purity levels: High, Medium, Low, Refined, S1, S2. This is the complete set.
**REQ-GC-013** Resource types: fixed set defined by the game. New resources are not dynamically added.

## Design Tradeoffs

Decisions we've made with known trade-offs, documented for future reference.

**REQ-GC-020** Timer simulation is local, not synced with the game. Accepted trade-off: timers may drift from actual game state. Mitigation: colony re-import resets state from the game's truth.
**REQ-GC-021** Data import is clipboard-based (copy HTML from browser, paste into tracker). Accepted trade-off: manual process, no real-time sync. Benefit: no authentication, no API dependency, works offline.
**REQ-GC-022** All player data is stored in a single JSON file (PlayerData.json). Accepted trade-off: file grows large with many entities. Benefit: atomic saves via SafeFileWriter, single-file backup, no database dependency.
**REQ-GC-023** Background processor processes colonies sequentially (one at a time). Accepted trade-off: slower with many colonies. Benefit: simpler locking model, no concurrent colony mutation, predictable behavior.
**REQ-GC-024** The tracker does not enforce game rules strictly (e.g. you can add more structures than the game allows). Accepted trade-off: data may not match game state exactly. Benefit: flexibility for planning and what-if scenarios.

## UI Constraints

**REQ-GC-030** Minimum supported screen resolution: 1920x1080. Forms are designed for this resolution and may require scrolling at lower resolutions.
**REQ-GC-031** MDI child form minimum practical size: 800x600. Forms with complex layouts (colony, build planner) may need more.
**REQ-GC-032** DataGridView column count should not exceed 15 per grid to avoid horizontal scrolling at 1920x1080.
**REQ-GC-033** List views (left-panel entity lists) should remain usable with up to 200 items without requiring pagination.
