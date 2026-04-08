# Completed Backlog Items

Items moved here from BACKLOG.md after implementation.

---

### BL-004: Commodity Fulfillment Unit Tests
Optional task from commodity-delivery-loop spec (task 5.2). Extract fulfillment logic from the Form handler into a testable static helper and add unit tests.
**Status: Complete** — Extracted `DeliveryFulfillment` static helper with `FulfillCommodity`, `StageFlatpack`, `DeliverWorkers`. 17 unit tests covering all three operations.

### BL-007: Skill Multipliers — Timer Processing
From Recommendations.md #13 Phase 4: ProductionFocus/Builder/ResearchFocus time reductions deferred until timer processing is fully implemented.
**Status: Complete** — ProductionFocus applied to manufacturing and commodity factory cycle times. ResearchFocus applied to research times. Builder was already applied to build times.

### BL-018: Mass Blueprint Importer
Import blueprints in bulk from the in-game market HTML. Parses market listings, extracts seller name and TechLevel, deduplicates by Name+Evolution+Type+Class+TechLevel, routes Government→global vs player storage.
**Status: Complete** — Import Market button on Blueprint form. Idempotency tests confirm no duplicates on re-import.

### BL-030: Colony Tab — Structure Count Warning Colors
The game limits colonies to 65 structures. Change the Structures tab selector background to yellow at 60+ structures, red at 66+.
**Status: Complete** — TabWarningService evaluates structure count thresholds (Yellow ≥60, Red ≥66). FormColony applies tab background colors on all data-change events. FsCheck property tests + unit tests validate logic.

### BL-031: Colony Tab — Worker Request Due Date Warning Colors
Change the Worker tab selector background to yellow when any worker request is due within 2 days, red when due within 1 day.
**Status: Complete** — TabWarningService evaluates unfulfilled commodity request due windows (Yellow ≤2 days, Red ≤1 day/overdue). Fulfilled requests and DateTime.MinValue sentinels excluded. Implemented alongside BL-030.
