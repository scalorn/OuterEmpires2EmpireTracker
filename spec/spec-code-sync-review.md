# spec/ Directory vs Codebase — Sync Review

> **Note (April 2026):** This document was created during an earlier audit pass. Most items below have been resolved. See `Ambiguities.md` AMB-052 through AMB-057 for the current open items from the latest spec-vs-code audit.

## Discrepancies Found (Status)

### 1. GOALS.md test count is stale

GOALS.md says "750 total tests, all passing." The actual count is 842.

**Fix:** Update to "842 total tests, all passing."

---

### 2. GOALS.md feature count is stale

GOALS.md says "78 features completed." COMPLETED.md ends at item 78, but two more features have been implemented since: Mass Blueprint Importer and Parser Idempotency Tests.

**Fix:** Update to "80 features completed."

---

### 3. REQ-CI-020 merge key description is wrong

ColonyImport.md states:
> **REQ-CI-020** When importing into an existing colony, structures SHALL be merged by UUID

The code merges by `buildingID` (stored in `gameSequence`), which is the game's per-building unique ID — not the structure's local UUID. The `gameSequence` field name is confusing because the game UI displays a per-type sequence number (e.g. Reactor Core #1, #2), but the actual value stored comes from `buildingID` in the JSON, which is globally unique within a colony.

The workers fallback path merges by `FlatpackBlueprintUUID` count (no buildingID available).

**Fix:** Update REQ-CI-020 to: "When importing into an existing colony, structures SHALL be merged by buildingID (stored as gameSequence) — existing structures with matching buildingID are updated, new structures are added. When buildingID is unavailable (workers fallback path), structures SHALL be merged by FlatpackBlueprintUUID count — structures are only added if fewer exist than the HTML shows."

---

### 4. No merge requirement for commodity demands

REQ-CI-012 says the parser extracts commodity demands, but there's no requirement describing the merge semantics. The code has explicit merge-by-Name logic in `ParseCommodityDemands`.

**Fix:** Add: "**REQ-CI-023** When importing commodity demands into an existing colony, demands SHALL be merged by commodity Name — existing demands are updated (Requested, NeedBy, Fulfilled refreshed from game data; Delivered preserved), new demands are added."

---

### 5. BACKLOG.md lists Mass Blueprint Importer as open work — it's complete

All tasks in `.kiro/specs/mass-blueprint-importer/tasks.md` are checked off. The feature is fully implemented with UI, service, and tests.

**Fix:** Remove from BACKLOG.md open items. Add entry to COMPLETED.md.

---

### 6. Parser idempotency tests not recorded in spec/

14 new tests across 2 files, fully implemented. Not mentioned in COMPLETED.md or GOALS.md.

**Fix:** Add entry to COMPLETED.md.

---

### 7. COMPLETED.md missing entries for features 79-80

Items 79 (Mass Blueprint Importer) and 80 (Parser Idempotency Tests) need to be added.

**Fix:** Add both entries with test counts.

---

### 8. GOALS.md Open Work section lists Mass Blueprint Importer

The "Independent (can start anytime)" list includes Mass Blueprint Importer, which is now complete.

**Fix:** Remove from the Open Work list.

---

### 9. Colony import duplicates manually-added structures (Bug / Ambiguity)

When a user manually builds out a colony in the app (via Add Structure), each structure gets a local UUID but `gameSequence` stays at 0 (default). When the user then imports from the game HTML, `ParseColonyBuildingsFromJson` indexes existing structures by `gameSequence` to find merge candidates. The manually-added structures have `gameSequence = 0`, so they never match any parsed building (which has a real `buildingID` like 47, 48, etc.). The parsed buildings are all added as new entries, duplicating every manually-added structure.

The workers fallback path (`ParseColonyBuildingsFromWorkers`) is partially protected — it counts existing structures by `FlatpackBlueprintUUID`, so manually-added structures with the same blueprint UUID are counted and won't duplicate. But the JSON path (primary path for local colonies) has no such protection.

The idempotency tests don't catch this because they always start with a fresh `Colony()` — they never test the "manual structures exist, then import" scenario.

**Resolution:** AMB-032 in spec/Ambiguities.md. Key decisions:
- Rename `gameSequence` to `displaySequence` (per-type UI sequence, calculated same as game)
- Add new `buildingID` field on ColonyStructure for the game's unique building identifier
- Merge by FlatpackBlueprintUUID + per-type displaySequence
- CommodityManufactory special case: best-guess match by build order vs UI order within each sub-type
- Form code calculates displaySequence on manual add
- Requires implementation as a new spec/feature

---

## Items That Are In Sync

- DataModel.md accurately describes Survey, SurveyResource, Colony, ColonyStructure, CommodityRequested
- GameMechanics.md formulas match the code
- Architecture.md testing requirements are being followed
- SafeFileWriter.md matches the implementation
- ColonyImport.md REQ-CI-003, REQ-CI-010 through REQ-CI-013 match the code
- Delivery.md requirements match the delivery implementation
- BlueprintProperties.md matches BlueprintPropertyValidation.cs
- Survey.md matches SurveyParser and Survey model
- Colony.md matches Colony model and ColonyStatusCalculator
