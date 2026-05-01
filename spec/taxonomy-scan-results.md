# Clarification Taxonomy Scan — Existing Spec Gaps

Scanned all 29 requirements files against the 8 taxonomy categories.
Date: 2026-04-30

## Summary

| Category | Status | Notes |
|----------|--------|-------|
| 1. Functional Scope & Behavior | Partial | Out-of-scope now declared on 5 domains; user goals still implicit |
| 2. Domain & Data Model | Clear | Well-covered across DataModel.md and domain files |
| 3. Interaction & UX Flow | Partial | Some forms lack step-by-step journeys |
| 4. Non-Functional Quality | Clear | NonFunctional.md created with 30 requirements |
| 5. Integration & Dependencies | Partial | Events documented; cross-form cascading now in NonFunctional.md |
| 6. Edge Cases & Failure Handling | Partial | Concurrent access and crash recovery now documented |
| 7. Constraints & Tradeoffs | Missing | Game constraints implicit, never documented |
| 8. Terminology & Consistency | Clear | term-consistency.js now enforces this |

---

## Detailed Findings

### Category 1: Functional Scope & Behavior

**What's good:** Each domain file clearly states what the feature does (REQ-xxx statements).

**Gaps found:**
- No requirements file explicitly declares what is OUT OF SCOPE. For example:
  - Market.md doesn't say "real-time price feeds are out of scope"
  - Ships.md doesn't say "combat simulation is out of scope"
  - SupplyChains.md doesn't say "automatic route optimization is out of scope"
- User goals are implicit in the requirements but never stated as goals. "The user wants to track manufacturing progress" is never written — you have to infer it from the REQ statements.
- No persona/role distinction documented. The app is single-player, but there's a multi-profile system (CurrentPlayerUUID). The relationship between "the human user" and "the in-game player profiles" is never explicitly stated.

### Category 2: Domain & Data Model

**Status: Clear.** This is the strongest area.
- DataModel.md covers all entities with field definitions
- UUID rules are documented (deterministic vs random)
- State transitions are documented (Colony structure lifecycle, BuildItem statuses)
- Serialization rules are explicit (DefaultValueHandling.Ignore, etc.)

### Category 3: Interaction & UX Flow

**What's good:** Delivery.md has excellent Mermaid sequence diagrams. Colony.md has detailed UI requirements.

**Gaps found:**
- Market.md has no user flow diagrams — just "the form SHALL have tabs"
- Ships.md has no user flow for creating a ship template or managing instances
- Stations.md has no user flow for managing holds or components
- SupplyChains.md has no user flow for creating/editing a chain
- StockTargets.md has no user flow for the "Check & Generate Orders" workflow
- No requirements file documents error states (what does the user see when something fails?)
- No requirements file documents empty states (what does a form look like with no data?)

### Category 4: Non-Functional Quality

**Status: Missing.** This is the weakest area across the entire spec.

**Gaps found:**
- No performance targets anywhere. How fast should colony import be? How fast should the background processor cycle? What's acceptable latency for form population with 100 colonies?
- No data scale assumptions. How many colonies can a player have? How many blueprints? How many market transactions before the grid becomes unusable?
- No reliability requirements. What happens if the app crashes during WriteContext()? (SafeFileWriter.md covers the mechanism but no requirement says "data loss SHALL NOT occur on crash")
- No observability requirements beyond the generic "log at Info/Debug/Warn" in Architecture.md. No specific requirements about what operations should be logged.
- BackgroundProcessing.md mentions a 1-second tick interval but doesn't specify what happens if processing takes longer than 1 second.

### Category 5: Integration & Dependencies

**What's good:** DataChangeEvents.md documents all events. Architecture.md documents the singleton pattern.

**Gaps found:**
- No requirements document which forms subscribe to which events. DataChangeEvents.md lists the events but not the subscribers.
- No requirements document cross-form interactions. For example: "When a colony is deleted in FormColony, FormDeliveryRoute SHALL remove routes referencing that colony" — this kind of cascading behavior is undocumented.
- No requirements document what happens when background processing modifies data that a form is currently displaying. The threading model is in design docs but not in requirements.

### Category 6: Edge Cases & Failure Handling

**What's good:** Delivery.md has REQ-DEL-093 (idempotent fulfillment). BuildPlanner.md has extensive edge case coverage.

**Gaps found:**
- Market.md: What happens if you try to sell more than the listing quantity? (Handled in code but not in requirements)
- Ships.md: What happens if you delete a blueprint that's used in a ship template? (Reference counter exists but no requirement states the behavior)
- Stations.md: What happens if you delete a station that's referenced in delivery routes?
- Colony.md: What happens if you import a colony that already exists? (ColonyImport.md covers this, but Colony.md doesn't cross-reference)
- No domain documents what happens with concurrent modification (background processor updates a colony while the user is editing it). The locking design exists in spec/design/ but no requirements state the expected user-visible behavior.

### Category 7: Constraints & Tradeoffs

**Status: Missing.** Game mechanics are documented in GameMechanics.md but constraints and tradeoffs are never explicitly stated.

**Gaps found:**
- No document states "we chose X over Y because Z." Decisions are in spec/decisions/ but not linked from requirements.
- Game constraints that limit design choices are implicit:
  - "Colonies can have at most ~66 structures" (implied by the warning thresholds in ColonyAdminSummary.md but never stated as a constraint)
  - "The game uses UTC for all timestamps" (stated in README.md conventions but not as a constraint)
  - "Blueprint evolution is a fixed tree defined by the game" (implied but never stated)
- No UI space constraints documented. How wide can a form be? What's the minimum resolution? How many columns can a grid have before horizontal scrolling becomes a problem?

### Category 8: Terminology & Consistency

**Status: Clear** (now enforced by term-consistency.js).
- The 3 drift findings were fixed in this session.
- GameMechanics.md uses game terminology consistently.
- The term-consistency.js tool will catch future drift.

---

## Recommended Actions

### High Priority (would prevent real bugs or confusion)

1. **Add non-functional requirements** — Create a new `spec/requirements/NonFunctional.md` with:
   - Performance targets for key operations (colony import, form population, background tick)
   - Data scale assumptions (max colonies, max blueprints, max transactions)
   - Crash recovery guarantees (link to SafeFileWriter)
   - Background processor timing constraints

2. **Document cross-form cascading behavior** — When entity X is deleted, what happens to forms/data referencing X? This is partially covered by reference counters but the user-visible behavior isn't specified.

3. **Document concurrent modification behavior** — What does the user see when background processing changes data they're viewing? Does the form auto-refresh? Does it show stale data until next interaction?

### Medium Priority (documentation completeness)

4. **Add out-of-scope declarations** to each domain file — One paragraph at the top stating what the feature explicitly does NOT do.

5. **Add user flow diagrams** to Market.md, Ships.md, Stations.md, SupplyChains.md, StockTargets.md.

6. **Add empty-state and error-state requirements** — What does each form show when there's no data? What message appears on validation failure?

### Low Priority (nice to have)

7. **Add game constraint documentation** — A section in GameMechanics.md listing hard limits from the game (max structures, max ships, etc.)

8. **Add UI constraint documentation** — Minimum form sizes, target resolution, grid column limits.

9. **State user goals explicitly** — One sentence per domain file: "The user's goal is to..."
