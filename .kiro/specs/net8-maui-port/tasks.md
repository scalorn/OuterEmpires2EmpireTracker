# .NET 8 + Avalonia Port — Comprehensive Task List

## Status Key
- [x] Complete
- [ ] Not started

---

## Phase A: Core Infrastructure (must be done first)

### A1: Data Persistence Layer
- [ ] A1.1 Implement SafeFileWriter equivalent (atomic write via temp + rename)
- [ ] A1.2 Implement WriteContext() — serialize PlayerRoot to JSON, write via SafeFileWriter
- [ ] A1.3 Implement File > New (clear all data, reset state)
- [ ] A1.4 Implement File > Save (write to Last_Opened_Path)
- [ ] A1.5 Implement File > Save As (file picker, write, update path)
- [ ] A1.6 Implement auto-open on launch (REQ-MM-010/011/012)
- [ ] A1.7 Implement title bar update with current file path
- [ ] A1.8 Implement exit confirmation dialog (REQ-MM-006)

### A2: Data Change Event System
- [ ] A2.1 Implement full event/message system matching REQ-DCE-001 (17 event types)
- [ ] A2.2 Implement event firing from DataService on all mutation operations
- [ ] A2.3 Implement CurrentPlayerChanged — fire when player combo changes
- [ ] A2.4 Implement view refresh on message receipt (all open views re-query data)
- [ ] A2.5 Implement write-through pattern in ViewModels (edit → immediate model update)

### A3: Background Processing
- [ ] A3.1 Implement BackgroundProcessor with configurable timer interval
- [ ] A3.2 Implement colony processing cycle (find expired timers, process each)
- [ ] A3.3 Implement ColonyDataChanged firing after processing
- [ ] A3.4 Implement WriteContext after processing cycle
- [ ] A3.5 Implement error handling (log, set LastCycleHadError, continue)
- [ ] A3.6 Implement status bar with "Next Process" countdown
- [ ] A3.7 Implement memory/CPU display in status bar
- [ ] A3.8 Implement warehouse overflow checks (REQ-BP-050/051/052)
- [ ] A3.9 Implement supply chain threshold checks (REQ-BP-060/061/062)
- [ ] A3.10 Implement stock target cascade processing (REQ-BP-070/071/072/073)

### A4: UI State Persistence
- [ ] A4.1 Implement PreferencesStore (load/save UIPreferences.json)
- [ ] A4.2 Implement window position/size save and restore
- [ ] A4.3 Implement DataGrid column width/order/sort persistence
- [ ] A4.4 Implement filter text and combo selection persistence
- [ ] A4.5 Implement bounds validation (off-screen detection, minimum size)
- [ ] A4.6 Implement dock layout save/restore on close/open


### A5: Service Layer
- [ ] A5.1 Implement ColonyService (create, update, delete, import, process)
- [ ] A5.2 Implement BlueprintService (create, update, delete, import from market)
- [ ] A5.3 Implement SurveyService (create, update, delete, import from clipboard)
- [ ] A5.4 Implement PlayerProfileService (create, update, delete, import)
- [ ] A5.5 Implement DeliveryRouteService (create, update, delete)
- [ ] A5.6 Implement DeliveryPlanService (create, update, delete)
- [ ] A5.7 Implement PricingPlanService (create, update, delete, compute prices)
- [ ] A5.8 Implement BuildPlanService (create, update, delete, validate, generate items)
- [ ] A5.9 Implement MarketService (create listing, record sale, record purchase, P&L)
- [ ] A5.10 Implement StationService (create, update, delete, manage holds)
- [ ] A5.11 Implement ShipTemplateService (create, update, delete)
- [ ] A5.12 Implement ShipService (create from template, update, delete, manage cargo)
- [ ] A5.13 Implement StockTargetService (check targets, generate replenishment)
- [ ] A5.14 Implement SupplyChainService (create, update, delete, evaluate thresholds)
- [ ] A5.15 Implement ContactsService (create/update/delete factions and characters)
- [ ] A5.16 Implement AsteroidService (create, update, delete)
- [ ] A5.17 Implement SystemRepository (load SystemData.json, O(1) lookups, search)

### A6: Import/Parse Infrastructure
- [ ] A6.1 Port ColonyParser (HTML → colony structures, merge semantics)
- [ ] A6.2 Port SurveyParser (HTML → survey resources)
- [ ] A6.3 Port BlueprintScanner (HTML → blueprint properties/resources)
- [ ] A6.4 Port PlayerProfileParser (HTML → profile/skills/ranks)
- [ ] A6.5 Implement clipboard HTML extraction (StartFragment/EndFragment markers)
- [ ] A6.6 Port SGML reader integration for HTML → XML conversion

### A7: Reference Counting
- [ ] A7.1 Implement BlueprintReferenceCounter
- [ ] A7.2 Implement ColonyReferenceCounter
- [ ] A7.3 Implement StationReferenceCounter
- [ ] A7.4 Implement BuildPlanReferenceCounter
- [ ] A7.5 Implement FactionReferenceCounter
- [ ] A7.6 Implement delete protection (disable delete when refs > 0)

### A8: Validation Infrastructure
- [ ] A8.1 Implement duplicate name checking per entity type
- [ ] A8.2 Implement required field validation
- [ ] A8.3 Implement numeric range validation (non-negative, min/max)
- [ ] A8.4 Implement unsaved changes prompt (Save/Discard/Cancel on navigation)

---

## Phase B: Main Window & Navigation

- [ ] B1 Implement File menu (New, Open, Save, Save As, Preferences, Exit) per REQ-MM-001-007
- [ ] B2 Implement Manage menu with alphabetically sorted feature items (REQ-MM-020-022)
- [ ] B3 Implement Window menu equivalent (list open tabs, focus tab)
- [ ] B4 Implement Help menu with F1 context-sensitive help (REQ-MM-030-045)
- [ ] B5 Implement player selector combo with CurrentPlayerChanged event (REQ-DCE-024)
- [ ] B6 Implement status bar (next process countdown, memory, CPU) (REQ-BP-030-032)
- [ ] B7 Implement auto-open last file on startup (REQ-MM-010-012)

---

## Phase C: Preferences (REQ-PRF-001 through PRF-050)

- [ ] C1 Implement ThresholdPreferences model with 9 configurable values
- [ ] C2 Implement Preferences form with 6 sections, validation, OK/Cancel/Reset
- [ ] C3 Implement countdown format parser (Xd Yh Zm Ws ↔ seconds)
- [ ] C4 Implement immediate effect on save (no restart required)
- [ ] C5 Implement backward compatibility (missing Thresholds → defaults)

---


## Phase D: Colony (REQ-CI, REQ-DCE, BackgroundProcessing)

### D1: Colony List & Header
- [ ] D1.1 Colony list with filter (name, system), sortable columns
- [ ] D1.2 Colony detail header (name, planet, system — editable)
- [ ] D1.3 New/Save/Delete colony operations via ColonyService
- [ ] D1.4 Subscribe to ColonyDataChanged, CurrentPlayerChanged — refresh on event

### D2: Structures Tab
- [ ] D2.1 Structures DataGrid (name, type, state, workers, timer countdown)
- [ ] D2.2 Structure state display (Built/Staged/Online/Offline)
- [ ] D2.3 Structure add/remove operations
- [ ] D2.4 Mining rig assignment (survey, resource selection)
- [ ] D2.5 Refinery configuration (resource, purity)
- [ ] D2.6 Manufacturing assignment (blueprint, quantity)
- [ ] D2.7 Research assignment (blueprint)
- [ ] D2.8 Timer countdown display (live update every 1s per REQ-PRF-033)

### D3: Commodities Tab
- [ ] D3.1 Commodity requests DataGrid (name, requested, delivered, fulfilled)
- [ ] D3.2 Add/edit/remove commodity requests

### D4: Items/Warehouse Tab
- [ ] D4.1 Items DataGrid (name, type, purity, quantity, volume)
- [ ] D4.2 Add/remove items from warehouse

### D5: Overflow Rules Tab
- [ ] D5.1 Overflow rules DataGrid (resource, purity, threshold, destination, route)
- [ ] D5.2 Add/edit/remove overflow rules

### D6: Colony Import
- [ ] D6.1 Import Clipboard button — read HTML, parse via ColonyParser
- [ ] D6.2 Import File button — read HTML file from disk
- [ ] D6.3 Merge semantics (update existing structures, add new)
- [ ] D6.4 Manufacturing reconciliation (REQ-CI-025a/b/c)
- [ ] D6.5 Miner/Refinery setup after import

### D7: Admin/Reports Tab
- [ ] D7.1 Colony status summary (structure counts, warnings)
- [ ] D7.2 Build queue display
- [ ] D7.3 Generate Build Plan button (REQ-BPL-070/071)

---

## Phase E: Blueprint (REQ-SRV-050-057, REQ-CI-040-045)

### E1: Blueprint List & Search
- [ ] E1.1 Blueprint list with multi-filter (name, type, class, tech level, evolution)
- [ ] E1.2 Blueprint type/class/tech level filter combos from EmpireContext
- [ ] E1.3 Subscribe to BlueprintDataChanged, PricingDataChanged, CurrentPlayerChanged

### E2: Blueprint Detail
- [ ] E2.1 Detail fields (name, type, tech level, class, evolution — editable)
- [ ] E2.2 New/Save/Delete operations via BlueprintService
- [ ] E2.3 Unsaved changes prompt on selection change

### E3: Statistics Tab
- [ ] E3.1 Properties DataGrid (key-value pairs from PropertyBag)
- [ ] E3.2 Inline editing of property values
- [ ] E3.3 Add/remove properties

### E4: Resources Tab
- [ ] E4.1 Resources DataGrid (resource name, quantity)
- [ ] E4.2 Inline editing of resource quantities
- [ ] E4.3 Add/remove resources

### E5: Evolution/Pricing Tab
- [ ] E5.1 Evolution chart (ScottPlot line graph of stats across evolutions)
- [ ] E5.2 Pricing display (computed price from selected pricing plan)
- [ ] E5.3 Per-unit cost display (REQ-PRC-046/047/048)

### E6: Blueprint Import
- [ ] E6.1 Import Market button — parse clipboard HTML via BlueprintScanner
- [ ] E6.2 Idempotent import (match by Name+Evolution+Type+Class+TechLevel)
- [ ] E6.3 Preserve protected fields on existing blueprints
- [ ] E6.4 Government vs player blueprint distinction

---

## Phase F: Survey (REQ-SRV-001 through SRV-100)

### F1: Survey List & Filters
- [ ] F1.1 Survey list with filters (planet, resource, purity, type, min amount)
- [ ] F1.2 SurveyType filter (Planet/Asteroid/All)
- [ ] F1.3 Subscribe to SurveyDataChanged, ColonyDataChanged, CurrentPlayerChanged

### F2: Survey Detail
- [ ] F2.1 Detail fields (planet, system, survey ID, nickname, scanner BP, etc.)
- [ ] F2.2 Scanner blueprint combo filtered to SystemObjectScanner type
- [ ] F2.3 New/Save/Delete operations via SurveyService
- [ ] F2.4 Unsaved changes prompt

### F3: Resources Grid
- [ ] F3.1 Resources DataGrid (resource combo, purity combo, amount, max reserve)
- [ ] F3.2 Add/edit/remove resource rows
- [ ] F3.3 Max Reserve column for asteroid surveys (linked asteroid lookup)

### F4: Survey Import
- [ ] F4.1 Import button — parse clipboard HTML via BlueprintScanner/SurveyParser
- [ ] F4.2 Asteroid auto-detection and auto-creation (REQ-SRV-017)
- [ ] F4.3 Max reserve extraction for asteroid surveys (REQ-SRV-060-063)

### F5: Yield Distribution
- [ ] F5.1 Distribution chart (ScottPlot histogram/curve)
- [ ] F5.2 Resource/purity combo selector for chart
- [ ] F5.3 YieldDistributionService integration

---

## Phase G: Player Profile (REQ-PP-001 through PP-062)

- [ ] G1 Profile list with name filter
- [ ] G2 Profile detail fields (name, faction, credits, skill points, 3 rank blocks)
- [ ] G3 10 skill group checkboxes with immediate write-through
- [ ] G4 22 PlayerSkillBlock controls (level display, training countdown)
- [ ] G5 New/Save/Delete operations via PlayerProfileService
- [ ] G6 Import button — parse clipboard HTML via PlayerProfileParser
- [ ] G7 Subscribe to PlayerProfileDataChanged, CurrentPlayerChanged

---


## Phase H: Delivery Routes & Execution

### H1: Delivery Routes (REQ in spec/requirements/delivery/)
- [ ] H1.1 Route list with filter
- [ ] H1.2 Route detail fields (name — editable)
- [ ] H1.3 Stops DataGrid (sequence, destination type, destination, purpose)
- [ ] H1.4 Stop add/remove/reorder operations
- [ ] H1.5 Destination combo (colonies + stations, filtered)
- [ ] H1.6 New/Save/Delete route operations via DeliveryRouteService
- [ ] H1.7 Subscribe to DeliveryDataChanged, CurrentPlayerChanged

### H2: Delivery Plans
- [ ] H2.1 Plan list associated with routes
- [ ] H2.2 Plan detail (name, route assignment)
- [ ] H2.3 Plan stops with pickup/dropoff item lists
- [ ] H2.4 Auto-fill dialog for generating plan items from shortfalls
- [ ] H2.5 New/Save/Delete plan operations via DeliveryPlanService

### H3: Delivery Execution
- [ ] H3.1 Plan/route selector
- [ ] H3.2 Stop execution display (current stop, items to load/deliver)
- [ ] H3.3 Item delivery marking (mark delivered → fires ColonyDataChanged)
- [ ] H3.4 Cargo volume tracking
- [ ] H3.5 Flatpack delivery (stages structure at destination colony)
- [ ] H3.6 Subscribe to DeliveryDataChanged, CurrentPlayerChanged

---

## Phase I: Market (REQ-MKT-001 through MKT-045)

- [ ] I1 Listings tab — DataGrid with item, qty, price, station, condition
- [ ] I2 Add/edit/delete listing operations
- [ ] I3 Record Sale dialog (quantity, counterparty, faction, notes)
- [ ] I4 Record Purchase dialog (item details, counterparty)
- [ ] I5 Transactions tab — DataGrid with filters (type, item, station, counterparty, faction, date)
- [ ] I6 Summary tab — profit/loss aggregation
- [ ] I7 Subscribe to MarketDataChanged, CurrentPlayerChanged

---

## Phase J: Ships (REQ-SHP-001 through SHP-065)

### J1: Ship Templates
- [ ] J1.1 Template list with filter
- [ ] J1.2 Hull selection (filtered to hull blueprints)
- [ ] J1.3 Slot grid (generated from hull slot definitions)
- [ ] J1.4 Component selection per slot (filtered by slot type)
- [ ] J1.5 Stats computation and display
- [ ] J1.6 Pricing display (from selected pricing plan)
- [ ] J1.7 New/Save/Delete via ShipTemplateService
- [ ] J1.8 Subscribe to PricingDataChanged, CurrentPlayerChanged

### J2: Ship Instances
- [ ] J2.1 Ship list with filter
- [ ] J2.2 Overview tab (stats, components grid with damage, hull damage)
- [ ] J2.3 Cargo tab (items DataGrid, volume used/total, over-capacity warning)
- [ ] J2.4 Hopper tab (unrefined resources only)
- [ ] J2.5 Create from template (deep copy hull + components)
- [ ] J2.6 Component swap with stat recomputation
- [ ] J2.7 New/Save/Delete via ShipService
- [ ] J2.8 Subscribe to CurrentPlayerChanged

---

## Phase K: Build Planner (REQ-BPL-001 through BPL-084)

- [ ] K1 Plan list with filter, IsActive toggle
- [ ] K2 Plan detail (name, description, delivery plan link)
- [ ] K3 Build items DataGrid (type, name, qty, status, location, structure)
- [ ] K4 Add build item dialog (type selector, blueprint/commodity, qty, location)
- [ ] K5 Status color coding in grid
- [ ] K6 Auto-assign service integration (propose structure assignments)
- [ ] K7 Generate delivery plan from shortfalls
- [ ] K8 Start manufacturing (configure structure, advance status)
- [ ] K9 Resource check / shortfall display
- [ ] K10 Queue calculator (target duration → runs needed)
- [ ] K11 Subscribe to BuildPlanDataChanged, ColonyDataChanged, CurrentPlayerChanged
- [ ] K12 Reference counter display (Refs column)

---

## Phase L: Stations (REQ-STN-001 through STN-062)

- [ ] L1 Station list with filter
- [ ] L2 Station detail (name, type, ownership — editable)
- [ ] L3 Hold tab — current player's inventory DataGrid
- [ ] L4 Hold item add/remove operations (type, filter, item, purity, qty)
- [ ] L5 Components tab (player-owned stations — slot grid)
- [ ] L6 Munitions tab (weapon ammo DataGrid)
- [ ] L7 New/Save/Delete via StationService
- [ ] L8 Subscribe to StationDataChanged, CurrentPlayerChanged
- [ ] L9 Reference counter (routes, plans, listings, overflow, supply chains)

---

## Phase M: Stock Targets (REQ-STK-001 through STK-064)

- [ ] M1 Plans tab — plan list, target DataGrid (item, qty, critical, scope, location)
- [ ] M2 Add/edit/remove targets
- [ ] M3 Template target expansion (ShipTemplateUUID → component targets)
- [ ] M4 Profiles tab — profile list, entry DataGrid (group, plan reference)
- [ ] M5 Check & Generate Orders button (evaluate shortfalls, create build items)
- [ ] M6 Stock level display with color coding (green/yellow/red)
- [ ] M7 IsActive toggle for plans and profiles
- [ ] M8 New/Save/Delete via StockTargetService
- [ ] M9 Subscribe to CurrentPlayerChanged

---

## Phase N: Supply Chains (REQ-SCH-001 through SCH-053)

- [ ] N1 Chain list with filter, IsActive toggle
- [ ] N2 Chain detail (name)
- [ ] N3 Stages DataGrid (sequence, type, location, resource, purity, threshold, route)
- [ ] N4 Stage add/remove/reorder
- [ ] N5 Flow summary display (pipeline visualization)
- [ ] N6 New/Save/Delete via SupplyChainService
- [ ] N7 Subscribe to CurrentPlayerChanged

---

## Phase O: Contacts (REQ-CON-001 through CON-035)

- [ ] O1 Factions tab — list, detail (name, description), New/Save/Delete
- [ ] O2 Characters tab — list, detail (name, faction combo), New/Save/Delete
- [ ] O3 Deterministic UUID generation from names
- [ ] O4 Delete protection (faction reference counter)
- [ ] O5 Subscribe to ContactDataChanged, CurrentPlayerChanged

---

## Phase P: Systems (REQ-SYS-001 through SYS-012)

- [ ] P1 Load SystemData.json into SystemRepository
- [ ] P2 System list DataGrid with partial name search
- [ ] P3 System detail panel (coordinates, grid, spectral class, faction, infrastructure)
- [ ] P4 Editable faction/infrastructure fields with save
- [ ] P5 Re-import button (from oe2-galaxy-systems.json)
- [ ] P6 Distance calculator integration
- [ ] P7 Subscribe to SystemDataChanged

---

## Phase Q: Pricing Plans (REQ-PRC-001 through PRC-071)

- [ ] Q1 Plan list with New/Delete
- [ ] Q2 Plan detail (name, description, fixed cost, hourly rate)
- [ ] Q3 Resource price DataGrid (all Refined + S1 + S2 resources)
- [ ] Q4 Inline price editing with validation (non-negative, clear = remove)
- [ ] Q5 PriceCalculator integration (commodity and blueprint price computation)
- [ ] Q6 Incomplete price flagging (visual indicator)
- [ ] Q7 New/Save/Delete via PricingPlanService
- [ ] Q8 Subscribe to PricingDataChanged, CurrentPlayerChanged

---

## Phase R: Colony Activity & Daily Build

### R1: Colony Activity
- [ ] R1.1 Activity DataGrid (colony, structure, type, status, countdown)
- [ ] R1.2 Live countdown refresh (configurable rate from preferences)
- [ ] R1.3 Filter checkboxes (mining, refining, research, manufacturing)
- [ ] R1.4 Subscribe to ColonyDataChanged, CurrentPlayerChanged

### R2: Colony Daily Build
- [ ] R2.1 Route/plan selection combos
- [ ] R2.2 Dynamic build display (items needed for today's deliveries)
- [ ] R2.3 Subscribe to ColonyDataChanged, CurrentPlayerChanged

---

## Phase S: Help System

- [ ] S1 HelpTopicRegistry (map form types to docs/ files)
- [ ] S2 HelpRenderer (markdown → HTML via Markdig or similar)
- [ ] S3 Help view with topic tree + content panel
- [ ] S4 F1 context-sensitive help (active tab → topic lookup)
- [ ] S5 Internal link navigation between help pages

---

## Phase T: Packaging & Platform Testing

- [x] T1 Windows self-contained publish (verified)
- [x] T2 Linux self-contained publish (verified)
- [x] T3 .deb packaging script
- [x] T4 .rpm packaging script
- [ ] T5 Linux testing (Debian — X11 and Wayland)
- [ ] T6 RHEL/Fedora testing
- [ ] T7 Performance profiling with real data (50+ colonies, 200+ blueprints)
- [ ] T8 macOS testing (nice-to-have)

---

## Task Count Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| A: Infrastructure | 47 | Persistence, events, background processing, UI state, services, parsers, validation |
| B: Main Window | 7 | Menu, player selector, status bar, auto-open |
| C: Preferences | 5 | Threshold model, form, parser, integration |
| D: Colony | 22 | List, structures, commodities, items, overflow, import, admin |
| E: Blueprint | 16 | List, detail, stats, resources, evolution, import |
| F: Survey | 14 | List, detail, resources, import, distribution |
| G: Player Profile | 7 | List, detail, skills, import |
| H: Delivery | 12 | Routes, plans, execution |
| I: Market | 7 | Listings, transactions, summary, sale/purchase |
| J: Ships | 15 | Templates, instances, stats, cargo |
| K: Build Planner | 12 | Plans, items, auto-assign, delivery gen, manufacturing |
| L: Stations | 9 | List, hold, components, munitions |
| M: Stock Targets | 9 | Plans, targets, profiles, check & generate |
| N: Supply Chains | 7 | Chains, stages, flow summary |
| O: Contacts | 5 | Factions, characters |
| P: Systems | 7 | Repository, list, detail, import |
| Q: Pricing Plans | 8 | Plans, prices, calculator |
| R: Activity & Daily Build | 6 | Activity, daily build |
| S: Help System | 5 | Registry, renderer, view, F1 |
| T: Packaging | 8 | Publish, .deb, .rpm, testing |
| **TOTAL** | **~228** | |

---

## Implementation Order & Dependencies

1. **Phase A** (infrastructure) — everything depends on this
2. **Phase B** (main window) — needed for navigation and file operations
3. **Phase C** (preferences) — needed by background processor and timers
4. **Phases D-R** (features) — can be done in any order after A-C, but recommended:
   - D (Colony) first — most complex, establishes patterns
   - E (Blueprint) second — heavily referenced by other features
   - F (Survey) — depends on blueprint scanner
   - G (Player Profile) — relatively independent
   - H (Delivery) — depends on colonies and stations
   - I (Market) — depends on stations and blueprints
   - J (Ships) — depends on blueprints
   - K (Build Planner) — depends on colonies, blueprints, delivery
   - L (Stations) — relatively independent
   - M (Stock Targets) — depends on build planner, ship templates
   - N (Supply Chains) — depends on delivery routes
   - O (Contacts) — independent
   - P (Systems) — independent
   - Q (Pricing Plans) — depends on blueprints and commodities
   - R (Activity/Daily Build) — depends on colonies
5. **Phase S** (help) — can be done anytime
6. **Phase T** (packaging/testing) — last

## Notes

- The existing "shell" views (Phase 5-8 from the old task list) provide the AXAML layout
  but need to be completely reworked to support real CRUD operations, event subscriptions,
  and service layer integration. They are starting points, not finished work.
- Each feature phase assumes Phase A infrastructure is complete.
- The service layer (A5) can be built incrementally — implement each service as its
  corresponding feature phase begins.
