# .NET 8 + Avalonia Port — Tasks

## Phase 1: Project Setup [DONE]

- [x] 1.1 Create `OE2EmpireTracker.Desktop` project with Avalonia .NET 8 template
- [x] 1.2 Configure target framework (net8.0)
- [x] 1.3 Add reference to `OE2EmpireTracker.Common`
- [x] 1.4 Add Avalonia.Themes.Fluent with light/dark theme support
- [x] 1.5 Add Dock.Avalonia packages
- [x] 1.6 Add CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection
- [x] 1.7 Configure build to produce zero warnings

## Phase 2: Smoke Test Prototype [DONE]

- [x] 2.1 Create ViewModelBase with CommunityToolkit.Mvvm patterns
- [x] 2.2 Set up dependency injection in App.axaml.cs
- [x] 2.3 Implement MainWindow with Dock layout
- [x] 2.4 Implement DockFactory for creating document and tool instances
- [x] 2.5 Implement navigation sidebar as a dockable tool panel
- [x] 2.6 Port FormAbout → AboutView
- [x] 2.7 Colony DataGrid with sample data (sorting, column resize, editing)
- [x] 2.8 Validate docking behaviors (float, split, close/reopen)

## Phase 3: Decision Gate [DONE — PASSED]

- [x] 3.1 UI acceptable — proceed to Phase 4

## Phase 4: Core Infrastructure

- [ ] 4.0 Create DataService for Desktop project (reads same JSON files as WinForms)
  - Satisfies: REQ-AVA-013, REQ-AVA-015
  - Scope: Lightweight service that loads PlayerData.json and BaselineData.json using
    Common models. Does NOT move PlayerContext/EmpireContext to Common yet — that
    refactoring (removing BindingSource, MessageBox dependencies) is deferred to
    merge-time when the branch is proven. The Desktop DataService reads the same
    JSON format and exposes the same data via DI.
  - Note: Full context migration (4300+ line PlayerContext has WinForms BindingSource
    and MessageBox dependencies) will be done as a separate task when merging to mainline.
- [ ] 4.1 Implement IFileSystemService with platform-appropriate paths
  - Satisfies: REQ-AVA-009
  - Scope: Interface + implementation, Windows/Linux/macOS path resolution
- [ ] 4.2 Implement IClipboardService for HTML paste
  - Satisfies: REQ-AVA-011
  - Scope: Interface + Avalonia clipboard wrapper, HTML format handling
- [ ] 4.3 Configure Microsoft.Extensions.Logging + Serilog file sink
  - Satisfies: REQ-AVA-010
  - Scope: Logger registration, file sink config, platform log paths
- [ ] 4.4 Implement timer service for background processing
  - Satisfies: REQ-AVA-012
  - Scope: DispatcherTimer wrapper, periodic processing interface
- [ ] 4.5 Implement layout serialization (save/restore dock state)
  - Satisfies: REQ-AVA-005
  - Scope: Dock.Serializer.Newtonsoft integration, load on startup, save on close
- [ ] 4.6 Implement ValidatedTextBox custom control
  - Satisfies: REQ-AVA-007
  - Scope: TextBox with regex validation, error display, focus trapping
- [ ] 4.7 Implement FilteredComboBox custom control
  - Satisfies: REQ-AVA-007
  - Scope: AutoCompleteBox-based, filterable dropdown, text search
- [ ] 4.8 Implement DataGrid filtered combo cell template
  - Satisfies: REQ-AVA-008
  - Scope: DataGridTemplateColumn with AutoCompleteBox for editing
- [ ] 4.9 Implement DataGrid validated text cell template
  - Satisfies: REQ-AVA-008
  - Scope: DataGridTemplateColumn with validation behavior
- [ ] 4.10 Implement ProgrammaticUpdateGuard equivalent for ViewModels
  - Satisfies: REQ-AVA-007
  - Scope: Flag property to suppress change notifications during bulk updates
- [ ] 4.11 Implement PlayerContext/EmpireContext DI registration and data loading
  - Satisfies: REQ-AVA-013
  - Scope: Register singletons, load JSON data files on startup
- [ ] 4.12 Implement data change messaging (WeakReferenceMessenger)
  - Satisfies: REQ-AVA-006
  - Scope: Message types for colony/blueprint/player changes, publish from services
- [ ] 4.13 Add ScottPlot.Avalonia package for charting
  - Satisfies: REQ-AVA-002 (feature parity — charts in Survey and Blueprint views)
  - Scope: Add NuGet package, verify renders on Windows and Linux

## Phase 5: Feature Migration — Tier 4 (Small Forms, <500 lines each)

- [ ] 5.1 Port FormHelp → HelpView
  - Scope: Help topic tree, markdown/HTML content display (~103 lines)
- [ ] 5.2 Port FormPreferences → PreferencesView
  - Scope: Server settings, threshold inputs, theme selector (~230 lines)
- [ ] 5.3 Port FormSystem → SystemView
  - Scope: Star system DataGrid, detail editing, search (~269 lines)
- [ ] 5.4 Port FormColonyActivity → ColonyActivityView
  - Scope: Activity DataGrid, filter checkboxes, auto-refresh timer (~295 lines)
- [ ] 5.5 Port FormColonyDailyBuild → ColonyDailyBuildView
  - Scope: Route/plan selection combos, dynamic build display (~322 lines)

## Phase 6: Feature Migration — Tier 3 (Medium Forms, 500-1000 lines)

- [ ] 6.1 Port FormMarket → MarketView
  - Scope: Listings DataGrid, transactions DataGrid, plan selector (~541 lines)
- [x] 6.2 Port FormAsteroid → AsteroidView
  - Scope: Asteroid list, reserves DataGrid, linked surveys (~502 lines)
- [x] 6.3 Port FormPricingPlan → PricingPlanView
  - Scope: Plan list, resource price DataGrid, markup editing (~659 lines)
- [x] 6.4 Port FormSupplyChain → SupplyChainView
  - Scope: Chain list, stages DataGrid, flow summary display (~758 lines)
- [x] 6.5a Port FormContacts → ContactsView — Factions tab
  - Scope: Faction list, detail editing, member display (~320 lines)
- [x] 6.5b Port FormContacts → ContactsView — Characters tab
  - Scope: Character list, detail editing, faction assignment (~320 lines)
- [x] 6.6a Port FormShipTemplate → ShipTemplateView — Template list and details
  - Scope: Template list, hull/class selection, pricing display (~625 lines)
- [x] 6.6b Port FormShipTemplate → ShipTemplateView — Slot grid
  - Scope: Slot DataGrid with combo columns, stats recalculation (~625 lines)
- [x] 6.7a Port FormShipInstance → ShipInstanceView — Ship list and components
  - Scope: Ship list, component DataGrid, template selection (~610 lines)
- [x] 6.7b Port FormShipInstance → ShipInstanceView — Cargo management
  - Scope: Cargo DataGrid, volume/mass calculation (~610 lines)
- [x] 6.8a Port FormStation → StationView — Station list and hold
  - Scope: Station list, hold DataGrid, detail editing (~580 lines)
- [x] 6.8b Port FormStation → StationView — Components and munitions
  - Scope: Components DataGrid, munitions DataGrid (~580 lines)

## Phase 7: Feature Migration — Tier 2 (Large Forms, 1000-2000 lines)

- [x] 7.1a Port FormPlayerProfile → PlayerProfileView — Profile list
  - Scope: Profile list/selector, add/delete, player switch handling (~350 lines)
- [x] 7.1b Port FormPlayerProfile → PlayerProfileView — Skills and ranks
  - Scope: Skill group checkboxes, level inputs, rank display (~360 lines)
- [x] 7.2a Port FormSurvey → SurveyView — Survey list and details
  - Scope: Survey list, detail fields, HTML paste import (~490 lines)
- [x] 7.2b Port FormSurvey → SurveyView — Resource DataGrid
  - Scope: Resource grid with purity, yield, depletion columns (~490 lines)
- [x] 7.2c Port FormSurvey → SurveyView — Distribution chart
  - Scope: Resource distribution visualization (needs charting library) (~490 lines)
- [x] 7.3a Port FormDeliveryExecution → DeliveryExecutionView — Plan selection
  - Scope: Plan/route selector, execution state display (~340 lines)
- [x] 7.3b Port FormDeliveryExecution → DeliveryExecutionView — Stop execution
  - Scope: Stop list, item delivery tracking, cargo volume (~340 lines)
- [x] 7.3c Port FormDeliveryExecution → DeliveryExecutionView — Load list
  - Scope: Load DataGrid, item completion marking (~340 lines)
- [x] 7.4a Port FormDeliveryRoute → DeliveryRouteView — Route list
  - Scope: Route list, add/delete, detail fields (~500 lines)
- [x] 7.4b Port FormDeliveryRoute → DeliveryRouteView — Stops grid
  - Scope: Stops DataGrid, sequencing, destination combos (~500 lines)
- [x] 7.4c Port FormDeliveryRoute → DeliveryRouteView — Plan management
  - Scope: Plan list, pickup/dropoff grids, auto-fill dialog (~510 lines)
- [x] 7.5a Port FormStockTargets → StockTargetsView — Plans tab
  - Scope: Plan list, target DataGrid, colony/item selection (~410 lines)
- [x] 7.5b Port FormStockTargets → StockTargetsView — Profiles tab
  - Scope: Profile list, entry DataGrid, item assignment (~410 lines)
- [x] 7.5c Port FormStockTargets → StockTargetsView — Target grid editing
  - Scope: Inline editing, quantity validation, shortfall display (~400 lines)

## Phase 8: Feature Migration — Tier 1 (Very Large Forms, 2000+ lines)

- [ ] 8.1a Port FormBlueprintV2 → BlueprintView — Blueprint list and search
  - Scope: Blueprint list, filter combos, search, type/class filtering (~450 lines)
- [ ] 8.1b Port FormBlueprintV2 → BlueprintView — Detail editing
  - Scope: Blueprint detail fields, property editing, type selection (~450 lines)
- [ ] 8.1c Port FormBlueprintV2 → BlueprintView — Statistics DataGrid
  - Scope: Stats grid, stat editing, computed values (~450 lines)
- [ ] 8.1d Port FormBlueprintV2 → BlueprintView — Resources DataGrid
  - Scope: Resource requirements grid, quantity editing (~450 lines)
- [ ] 8.1e Port FormBlueprintV2 → BlueprintView — Evolution and pricing
  - Scope: Evolution chart, price history, pricing plan integration (~480 lines)
- [ ] 8.2a Port FormBuildPlanner → BuildPlannerView — Plan list
  - Scope: Plan list, add/delete, colony assignment (~400 lines)
- [ ] 8.2b Port FormBuildPlanner → BuildPlannerView — Build items grid
  - Scope: Build items DataGrid, item selection, quantity, priority (~540 lines)
- [ ] 8.2c Port FormBuildPlanner → BuildPlannerView — Shortfall analysis
  - Scope: Shortfall DataGrid, resource gap calculation (~540 lines)
- [ ] 8.2d Port FormBuildPlanner → BuildPlannerView — Allocation and delivery
  - Scope: Resource allocation, delivery route generation (~540 lines)
- [ ] 8.3a Port FormColonyV2 → ColonyView — Colony list and header
  - Scope: Colony list/selector, detail header (name, planet, system) (~400 lines)
- [ ] 8.3b Port FormColonyV2 → ColonyView — Structures DataGrid (read-only)
  - Scope: Structure list display, status indicators, type/state columns (~500 lines)
- [ ] 8.3c Port FormColonyV2 → ColonyView — Structures inline editing
  - Scope: Add/remove/modify structures, combo columns, validation (~500 lines)
- [ ] 8.3d Port FormColonyV2 → ColonyView — Commodity requests tab
  - Scope: Commodity request DataGrid, item selection, quantity editing (~450 lines)
- [ ] 8.3e Port FormColonyV2 → ColonyView — Items/inventory tab
  - Scope: Item inventory display, quantity tracking (~400 lines)
- [ ] 8.3f Port FormColonyV2 → ColonyView — Overflow rules tab
  - Scope: Overflow rules DataGrid, rule editing, priority (~400 lines)
- [ ] 8.3g Port FormColonyV2 → ColonyView — Timer processing
  - Scope: Mining/refining/research/manufacturing timers, status updates (~350 lines)
- [ ] 8.3h Port FormColonyV2 → ColonyView — Admin/reports tab
  - Scope: Admin report display, build queue, status summary (~350 lines)

## Phase 9: Polish & Packaging

- [ ] 9.1 Linux testing (Debian/Ubuntu — X11 and Wayland)
  - Scope: Run full app, verify all views render, test clipboard paste
- [ ] 9.2 RHEL/Fedora testing (for AWS server compatibility)
  - Scope: Verify builds and runs on rpm-based distro
- [ ] 9.3 Performance profiling (large grids, startup time)
  - Scope: Profile with 50+ colonies, 200+ blueprints, identify bottlenecks
- [ ] 9.4 Configure self-contained publish for Windows
  - Scope: dotnet publish -r win-x64, verify exe runs standalone
- [ ] 9.5 Configure .deb packaging for Debian/Ubuntu
  - Scope: dpkg-deb or dotnet-deb tool, desktop entry, icon
- [ ] 9.6 Configure .rpm packaging for RHEL/Fedora
  - Scope: rpmbuild or dotnet-rpm tool
- [ ] 9.7 macOS testing and .app bundle (nice-to-have)
  - Scope: Verify on macOS, create .app if feasible

---

## Task Count Summary

| Phase | Tasks | Estimated Effort |
|-------|-------|-----------------|
| Phase 4: Infrastructure | 12 | ~4-6 hours |
| Phase 5: Tier 4 (small) | 5 | ~3-4 hours |
| Phase 6: Tier 3 (medium) | 12 | ~8-10 hours |
| Phase 7: Tier 2 (large) | 14 | ~10-14 hours |
| Phase 8: Tier 1 (very large) | 17 | ~14-18 hours |
| Phase 9: Polish | 7 | ~4-6 hours |
| **TOTAL** | **67** | **~43-58 hours** |

## Implementation Notes

- Each subtask should produce a buildable, runnable increment
- DataGrid-heavy tasks depend on Phase 4 custom cell templates (4.8, 4.9)
- Chart tasks (7.2c, 8.1e) need a charting library decision — evaluate LiveChartsCore or ScottPlot.Avalonia
- Timer tasks (5.4, 8.3g) depend on Phase 4 timer service (4.4)
- All form ports depend on Phase 4 data loading (4.11) and messaging (4.12)
