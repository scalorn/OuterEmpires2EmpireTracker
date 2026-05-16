# .NET 8 + Avalonia Port — Comprehensive Task List

## Status Key
- [x] Complete
- [ ] Not started
- [~] Partial (basic implementation done, advanced features remaining)

---

## Phase A: Core Infrastructure (must be done first)

### A1: Data Persistence Layer
- [x] A1.1 Implement SafeFileWriter equivalent (atomic write via temp + rename)
- [x] A1.2 Implement WriteContext() — serialize PlayerRoot to JSON, write via SafeFileWriter
- [x] A1.3 Implement File > New (clear all data, reset state)
- [x] A1.4 Implement File > Save (write to Last_Opened_Path)
- [x] A1.5 Implement File > Save As (file picker, write, update path)
- [x] A1.6 Implement auto-open on launch (REQ-MM-010/011/012)
- [x] A1.7 Implement title bar update with current file path
- [x] A1.8 Implement exit confirmation dialog (REQ-MM-006)

### A2: Data Change Event System
- [x] A2.1 Implement full event/message system matching REQ-DCE-001 (17 event types)
- [x] A2.2 Implement event firing from DataService on all mutation operations
- [x] A2.3 Implement CurrentPlayerChanged — fire when player combo changes
- [x] A2.4 Implement view refresh on message receipt (all open views re-query data)
- [x] A2.5 Implement write-through pattern in ViewModels (edit → immediate model update)

### A3: Background Processing
- [x] A3.1 Implement BackgroundProcessor with configurable timer interval
- [x] A3.2 Implement colony processing cycle (find expired timers, process each)
- [x] A3.3 Implement ColonyDataChanged firing after processing
- [x] A3.4 Implement WriteContext after processing cycle
- [x] A3.5 Implement error handling (log, set LastCycleHadError, continue)
- [x] A3.6 Implement status bar with "Next Process" countdown
- [x] A3.7 Implement memory/CPU display in status bar
- [ ] A3.8 Implement warehouse overflow checks (REQ-BP-050/051/052)
- [ ] A3.9 Implement supply chain threshold checks (REQ-BP-060/061/062)
- [ ] A3.10 Implement stock target cascade processing (REQ-BP-070/071/072/073)

### A4: UI State Persistence
- [x] A4.1 Implement PreferencesStore (load/save UIPreferences.json)
- [x] A4.2 Implement window position/size save and restore
- [ ] A4.3 Implement DataGrid column width/order/sort persistence
- [ ] A4.4 Implement filter text and combo selection persistence
- [x] A4.5 Implement bounds validation (off-screen detection, minimum size)
- [x] A4.6 Implement dock layout save/restore on close/open


### A5: Service Layer
- [x] A5.1 Implement ColonyService (create, update, delete, import, process)
- [x] A5.2 Implement BlueprintService (create, update, delete, import from market)
- [x] A5.3 Implement SurveyService (create, update, delete, import from clipboard)
- [x] A5.4 Implement PlayerProfileService (create, update, delete, import)
- [x] A5.5 Implement DeliveryRouteService (create, update, delete)
- [x] A5.6 Implement DeliveryPlanService (create, update, delete)
- [x] A5.7 Implement PricingPlanService (create, update, delete, compute prices)
- [x] A5.8 Implement BuildPlanService (create, update, delete, validate, generate items)
- [x] A5.9 Implement MarketService (create listing, record sale, record purchase, P&L)
- [x] A5.10 Implement StationService (create, update, delete, manage holds)
- [x] A5.11 Implement ShipTemplateService (create, update, delete)
- [x] A5.12 Implement ShipService (create from template, update, delete, manage cargo)
- [x] A5.13 Implement StockTargetService (check targets, generate replenishment)
- [x] A5.14 Implement SupplyChainService (create, update, delete, evaluate thresholds)
- [x] A5.15 Implement ContactsService (create/update/delete factions and characters)
- [x] A5.16 Implement AsteroidService (create, update, delete)
- [x] A5.17 Implement SystemRepository (load SystemData.json, O(1) lookups, search)

### A6: Import/Parse Infrastructure
- [x] A6.1 Port ColonyParser (HTML → colony structures, merge semantics)
- [x] A6.2 Port SurveyParser (HTML → survey resources)
- [x] A6.3 Port BlueprintScanner (HTML → blueprint properties/resources)
- [~] A6.4 Port PlayerProfileParser (HTML → profile/skills/ranks) — stub only
- [x] A6.5 Implement clipboard HTML extraction (StartFragment/EndFragment markers)
- [x] A6.6 Port SGML reader integration for HTML → XML conversion

### A7: Reference Counting
- [x] A7.1 Implement BlueprintReferenceCounter
- [x] A7.2 Implement ColonyReferenceCounter
- [x] A7.3 Implement StationReferenceCounter
- [ ] A7.4 Implement BuildPlanReferenceCounter
- [ ] A7.5 Implement FactionReferenceCounter
- [x] A7.6 Implement delete protection (disable delete when refs > 0)

### A8: Validation Infrastructure
- [x] A8.1 Implement duplicate name checking per entity type
- [x] A8.2 Implement required field validation
- [x] A8.3 Implement numeric range validation (non-negative, min/max)
- [x] A8.4 Implement unsaved changes prompt (Save/Discard/Cancel on navigation)

---

## Phase B: Main Window & Navigation

- [x] B1 Implement File menu (New, Open, Save, Save As, Preferences, Exit) per REQ-MM-001-007
- [x] B2 Implement Manage menu with alphabetically sorted feature items (REQ-MM-020-022)
- [ ] B3 Implement Window menu equivalent (list open tabs, focus tab)
- [~] B4 Implement Help menu with F1 context-sensitive help (REQ-MM-030-045) — Help Topics done, F1 not wired
- [x] B5 Implement player selector combo with CurrentPlayerChanged event (REQ-DCE-024)
- [x] B6 Implement status bar (next process countdown, memory, CPU) (REQ-BP-030-032)
- [x] B7 Implement auto-open last file on startup (REQ-MM-010-012)

---

## Phase C: Preferences (REQ-PRF-001 through PRF-050)

- [x] C1 Implement ThresholdPreferences model with 9 configurable values
- [x] C2 Implement Preferences form with 6 sections, validation, OK/Cancel/Reset
- [x] C3 Implement countdown format parser (Xd Yh Zm Ws ↔ seconds)
- [x] C4 Implement immediate effect on save (no restart required)
- [x] C5 Implement backward compatibility (missing Thresholds → defaults)

---

## Phase D: Colony (REQ-CI, REQ-DCE, BackgroundProcessing)

### D1: Colony List & Header
- [~] D1.1 Colony list with filter (name, system), sortable columns — list done, no filter
- [x] D1.2 Colony detail header (name, planet, system — editable)
- [x] D1.3 New/Save/Delete colony operations via ColonyService
- [x] D1.4 Subscribe to ColonyDataChanged, CurrentPlayerChanged — refresh on event

### D2: Structures Tab
- [x] D2.1 Structures DataGrid (name, type, state, workers, timer countdown)
- [x] D2.2 Structure state display (Built/Staged/Online/Offline)
- [x] D2.3 Structure add/remove operations
- [ ] D2.4 Mining rig assignment (survey, resource selection)
- [ ] D2.5 Refinery configuration (resource, purity)
- [ ] D2.6 Manufacturing assignment (blueprint, quantity)
- [ ] D2.7 Research assignment (blueprint)
- [ ] D2.8 Timer countdown display (live update every 1s per REQ-PRF-033)

### D3: Commodities Tab
- [x] D3.1 Commodity requests DataGrid (name, requested, delivered, fulfilled)
- [ ] D3.2 Add/edit/remove commodity requests

### D4: Items/Warehouse Tab
- [x] D4.1 Items DataGrid (name, type, purity, quantity, volume)
- [x] D4.2 Add/remove items from warehouse

### D5: Overflow Rules Tab
- [x] D5.1 Overflow rules DataGrid (resource, purity, threshold, destination, route)
- [x] D5.2 Add/edit/remove overflow rules

### D6: Colony Import
- [x] D6.1 Import Clipboard button — read HTML, parse via ColonyParser
- [ ] D6.2 Import File button — read HTML file from disk
- [x] D6.3 Merge semantics (update existing structures, add new)
- [ ] D6.4 Manufacturing reconciliation (REQ-CI-025a/b/c)
- [ ] D6.5 Miner/Refinery setup after import

### D7: Admin/Reports Tab
- [ ] D7.1 Colony status summary (structure counts, warnings)
- [ ] D7.2 Build queue display
- [ ] D7.3 Generate Build Plan button (REQ-BPL-070/071)

---

## Phase E: Blueprint
- [x] E1.1-E1.3 Blueprint list, filters, event subscription
- [x] E2.1-E2.2 Detail fields editable, New/Save/Delete
- [x] E3.1-E3.3 Stats DataGrid editable with add/remove
- [x] E4.1-E4.3 Resources DataGrid editable with add/remove
- [~] E5.1 Evolution chart — AvaPlot placed, needs code-behind wiring
- [x] E5.2 Pricing display (computed price from plan)
- [x] E6.1 BlueprintScanner full implementation
- [ ] E6.2-E6.4 Idempotent import, protected fields, government distinction

## Phase F: Survey
- [x] F1.1-F1.3 Survey list, event subscription
- [x] F2.1-F2.4 Detail fields, New/Save/Delete
- [x] F3.1-F3.2 Resources DataGrid editable with add/remove
- [ ] F3.3 Max Reserve column for asteroid surveys
- [x] F4.1 Import clipboard via SurveyParser
- [ ] F4.2-F4.3 Asteroid auto-detection, max reserve extraction
- [ ] F5.1-F5.3 Yield distribution chart

## Phase G: Player Profile
- [x] G1 Profile list
- [x] G2 Detail fields (name, faction, credits, skill points)
- [x] G3 Skills DataGrid with inline level editing
- [ ] G4 22 PlayerSkillBlock controls with training countdown
- [x] G5 New/Save/Delete via service
- [ ] G6 Import via PlayerProfileParser
- [x] G7 Event subscription

## Phase H: Delivery
- [x] H1.1-H1.7 Route list, detail, stops DataGrid, add/remove, New/Save/Delete, events
- [ ] H2.1-H2.5 Delivery plans (basic CRUD done, auto-fill not implemented)
- [x] H3.1-H3.3 Execution: plan selector, load items, mark delivered
- [ ] H3.4-H3.6 Cargo volume, flatpack delivery

## Phase I: Market
- [x] I1-I2 Listings DataGrid with CRUD
- [ ] I3-I4 Record Sale/Purchase dialogs
- [ ] I5-I6 Transactions tab, Summary tab
- [x] I7 Event subscription

## Phase J: Ships
- [x] J1.1-J1.4 Template list, slot grid with add/remove
- [ ] J1.5-J1.6 Stats computation, pricing display
- [x] J1.7-J1.8 New/Save/Delete, events
- [x] J2.1 Ship list with CRUD
- [ ] J2.2-J2.8 Overview/Cargo/Hopper tabs, create from template, component swap

## Phase K: Build Planner
- [x] K1-K4 Plan list, detail, build items DataGrid with add/remove/save
- [ ] K5-K12 Status color coding, auto-assign, delivery gen, manufacturing, queue calc

## Phase L: Stations
- [x] L1-L4 Station list, detail, hold items DataGrid with add/remove
- [ ] L5-L6 Components tab, munitions tab
- [x] L7-L8 New/Save/Delete, events
- [ ] L9 Reference counter

## Phase M: Stock Targets
- [x] M1 Plans tab with target DataGrid
- [ ] M2-M4 Add/edit targets, template expansion, profiles tab
- [x] M5 Check & Generate Orders
- [ ] M6-M7 Color coding, IsActive toggle
- [x] M8-M9 New/Save/Delete, events

## Phase N: Supply Chains
- [x] N1-N4 Chain list, detail, stages DataGrid with add/remove/save
- [ ] N5 Flow summary display
- [x] N6-N7 New/Save/Delete, events

## Phase O: Contacts
- [x] O1-O2 Factions + Characters tabs with CRUD
- [ ] O3 Deterministic UUID generation
- [ ] O4 Delete protection
- [x] O5 Event subscription

## Phase P: Systems
- [~] P1-P2 System list DataGrid (display only, no SystemData.json loading yet)
- [ ] P3-P7 Detail panel, editing, re-import, distance calc

## Phase Q: Pricing Plans
- [x] Q1-Q4 Plan list, detail, resource price DataGrid with inline editing
- [x] Q5-Q6 PriceCalculator integration, incomplete flagging
- [x] Q7-Q8 New/Save/Delete, events

## Phase R: Colony Activity & Daily Build
- [~] R1.1 Activity DataGrid (sample data only)
- [ ] R1.2-R1.4 Live countdown, filters
- [ ] R2.1-R2.3 Daily build display

## Phase S: Help System
- [x] S1 HelpTopicRegistry
- [x] S2 HelpRenderer (raw markdown)
- [x] S3 Help view with topic tree + content panel
- [ ] S4 F1 context-sensitive help
- [ ] S5 Internal link navigation

## Phase T: Packaging & Platform Testing
- [x] T1 Windows self-contained publish
- [x] T2 Linux self-contained publish
- [x] T3 .deb packaging script
- [x] T4 .rpm packaging script
- [ ] T5 Linux testing (Debian)
- [ ] T6 RHEL/Fedora testing
- [ ] T7 Performance profiling
- [ ] T8 macOS testing

---

## Summary: ~155/228 tasks complete (~68%)

### Remaining work (73 tasks):
- A3.8-A3.10: Background processing advanced (overflow, supply chain, stock cascade)
- A4.3-A4.4: DataGrid column/filter persistence
- A7.4-A7.5: BuildPlan and Faction reference counters
- B3: Window menu (list open tabs)
- D2.4-D2.8: Structure assignment (mining/refining/manufacturing/research/timers)
- D3.2, D6.2, D6.4-D6.5, D7: Colony advanced features
- E5.1, E6.2-E6.4: Evolution chart wiring, import refinements
- F3.3, F4.2-F4.3, F5: Survey asteroid features, yield distribution
- G4, G6: Skill blocks, profile import
- H2, H3.4-H3.6: Delivery plans, cargo/flatpack
- I3-I6: Market sale/purchase dialogs, transactions, summary
- J1.5-J1.6, J2.2-J2.8: Ship stats, cargo, hopper, create from template
- K5-K12: Build planner advanced (color coding, auto-assign, manufacturing)
- L5-L6, L9: Station components, munitions, reference counter
- M2-M4, M6-M7: Stock target editing, profiles, color coding
- N5: Supply chain flow summary
- O3-O4: Deterministic UUIDs, delete protection
- P3-P7: Systems detail/editing
- R1.2-R2.3: Activity live countdown, daily build
- S4-S5: F1 help, link navigation
- T5-T8: Platform testing
