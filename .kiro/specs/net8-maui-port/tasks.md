# .NET 8 + Avalonia Port — Task List (Updated)

## Status: ~210/228 tasks complete (~92%)

## Status Key
- [x] Complete
- [~] Partial (API/infrastructure done, per-view wiring deferred)
- [ ] Not started (requires user testing or game HTML samples)

---

## Phase A: Core Infrastructure — COMPLETE

- [x] A1.1-A1.8: Data Persistence (SafeFileWriter, WriteContext, File menu, auto-open, title bar, exit dialog)
- [x] A2.1-A2.5: Data Change Events (17 message types, auto-refresh, write-through)
- [x] A3.1-A3.7: Background Processing (timer, colony processing, events, persist, error handling, status bar)
- [x] A3.8-A3.10: Advanced processing (overflow checks, supply chain thresholds, stock cascade — with TODOs for delivery generation)
- [x] A4.1-A4.2, A4.5-A4.6: UI State (PreferencesStore, window position, bounds validation, dock layout)
- [~] A4.3-A4.4: DataGrid column/filter persistence (GridStateService API done, per-view wiring deferred)
- [x] A5.1-A5.17: Service Layer (all 17 CRUD services)
- [x] A6.1-A6.3, A6.5-A6.6: Parsers (ColonyParser, SurveyParser, BlueprintScanner, clipboard helper, SGML)
- [~] A6.4: PlayerProfileParser (stub — needs game HTML samples)
- [x] A7.1-A7.6: Reference Counting (colony, blueprint, station, build plan, faction + delete protection)
- [x] A8.1-A8.4: Validation (name, duplicate, positive, unsaved changes dialog)

## Phase B: Main Window — COMPLETE

- [x] B1-B2: File menu + Manage menu (alphabetical, all items)
- [x] B3: Window menu (Close All Tabs)
- [x] B4: Help menu (Help Topics, About, F1 context-sensitive)
- [x] B5-B7: Player selector, status bar, auto-open

## Phase C: Preferences — COMPLETE

- [x] C1-C5: ThresholdPreferences, form with validation, countdown parser, PreferencesStore

## Phase D: Colony — 90% COMPLETE

- [x] D1.1-D1.4: Colony list, header editing, CRUD, events
- [x] D2.1-D2.3: Structures DataGrid, state display, add/remove
- [x] D2.4-D2.7: Structure assignments (mining, refining, manufacturing, research)
- [x] D2.8: Timer countdown display (1-second refresh)
- [x] D3.1: Commodity requests DataGrid
- [x] D3.2: Add/remove commodity requests
- [x] D4.1-D4.2: Items DataGrid, add/remove
- [x] D5.1-D5.2: Overflow rules DataGrid, add/remove
- [x] D6.1, D6.3: Clipboard import, merge semantics
- [x] D6.2: File import
- [ ] D6.4-D6.5: Manufacturing reconciliation, miner/refinery setup after import
- [x] D7.1-D7.3: Admin tab (summary, build queue, generate build plan)

## Phase E: Blueprint — 90% COMPLETE

- [x] E1.1-E1.3: List, filters, events
- [x] E2.1-E2.2: Detail editing, CRUD
- [x] E3.1-E3.3: Stats DataGrid editable with add/remove
- [x] E4.1-E4.3: Resources DataGrid editable with add/remove
- [~] E5.1: Evolution chart (AvaPlot placed, needs code-behind wiring)
- [x] E5.2-E5.3: Pricing display, per-unit cost
- [x] E6.1-E6.4: BlueprintScanner, idempotent import, preserve fields, government distinction

## Phase F: Survey — 85% COMPLETE

- [x] F1.1-F1.3: List, events
- [x] F2.1-F2.4: Detail editing, CRUD
- [x] F3.1-F3.3: Resources DataGrid, add/remove, Max Reserve column
- [x] F4.1-F4.3: Clipboard import, asteroid auto-detection, max reserve extraction
- [ ] F5.1-F5.3: Yield distribution chart (needs ScottPlot wiring)

## Phase G: Player Profile — 85% COMPLETE

- [x] G1-G3: List, detail, skill groups with checkboxes
- [x] G4: Skill group display (grouped ItemsControl with 10 groups)
- [x] G5, G7: CRUD, events
- [ ] G6: Profile HTML import (stub — needs game HTML samples)

## Phase H: Delivery — 90% COMPLETE

- [x] H1.1-H1.7: Route list, detail, stops, add/remove, CRUD, events
- [x] H2.1-H2.3, H2.5: Plan list, detail, stop details, CRUD
- [ ] H2.4: Auto-fill dialog
- [x] H3.1-H3.5: Execution, mark delivered, cargo volume, flatpack staging
- [x] H3.6: Event subscription

## Phase I: Market — COMPLETE

- [x] I1-I7: Listings CRUD, Record Sale/Purchase, Transactions tab, Summary tab, events

## Phase J: Ships — COMPLETE

- [x] J1.1-J1.8: Template list, slot grid, stats, CRUD, events
- [x] J2.1-J2.8: Ship list, components, cargo, hopper, create from template, events

## Phase K: Build Planner — COMPLETE

- [x] K1-K12: Plan list, items, status colors, auto-assign, delivery gen, manufacturing, resource check, queue calc, events, reference counter

## Phase L: Stations — COMPLETE

- [x] L1-L9: List, detail, hold items, components, munitions, CRUD, events, reference counter

## Phase M: Stock Targets — COMPLETE

- [x] M1-M9: Plans, targets with add/remove, check & generate, color coding, IsActive, CRUD, events

## Phase N: Supply Chains — COMPLETE

- [x] N1-N7: Chain list, stages with add/remove, flow summary, CRUD, events

## Phase O: Contacts — COMPLETE

- [x] O1-O5: Factions + Characters CRUD, deterministic UUIDs, delete protection, events

## Phase P: Systems — PARTIAL

- [x] P1-P2: System list DataGrid
- [ ] P3-P7: Detail panel, editing, re-import, distance calc (needs SystemData.json loading)

## Phase Q: Pricing Plans — COMPLETE

- [x] Q1-Q8: Plan list, detail, resource price grid, PriceCalculator, incomplete flagging, CRUD, events

## Phase R: Colony Activity & Daily Build — COMPLETE

- [x] R1.1-R1.4: Activity DataGrid, live countdown, filters, events
- [x] R2.1-R2.3: Daily build display, events

## Phase S: Help System — 80% COMPLETE

- [x] S1-S4: HelpTopicRegistry, HelpRenderer, topic tree + content, F1 context help
- [~] S5: Internal link navigation (infrastructure ready, needs HTML rendering)

## Phase T: Packaging — REQUIRES USER TESTING

- [x] T1-T4: Windows/Linux publish, .deb/.rpm scripts
- [ ] T5-T8: Linux/RHEL/macOS testing, performance profiling

---

## Remaining Items (~18 tasks):

1. D6.4-D6.5: Manufacturing reconciliation + miner/refinery setup after import
2. E5.1: ScottPlot evolution chart code-behind wiring
3. F5.1-F5.3: Yield distribution chart (ScottPlot + YieldDistributionService)
4. G6: Player profile HTML import (needs game HTML samples)
5. H2.4: Delivery auto-fill dialog
6. P3-P7: Systems detail/editing (needs SystemData.json loading)
7. S5: Help internal link navigation (needs HTML rendering)
8. T5-T8: Platform testing (requires user's Linux machines)
9. A4.3-A4.4: Per-view DataGrid column/filter wiring
