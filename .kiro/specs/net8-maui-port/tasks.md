# .NET 8 + Avalonia Port — Tasks

## Phase 1: Project Setup

- [ ] 1.1 Create `OE2EmpireTracker.Desktop` project with Avalonia .NET 8 template
  - Satisfies: REQ-AVA-003 (project structure), REQ-AVA-004 (PackageReference)
- [ ] 1.2 Configure target framework (net8.0 — runs on Windows, Linux, macOS)
  - Satisfies: REQ-AVA-001 (platform targets)
- [ ] 1.3 Add reference to `OE2EmpireTracker.Common`
  - Satisfies: REQ-AVA-003 (Common referenced by Desktop project)
- [ ] 1.4 Set up Directory.Packages.props for central package management
  - Satisfies: REQ-AVA-004 (central package management)
- [ ] 1.5 Verify all Common dependencies are .NET 8 compatible
  - Satisfies: REQ-AVA-004 (NuGet packages compatible with .NET 8)
- [ ] 1.6 Add Avalonia.Themes.Fluent with light/dark theme support
  - Satisfies: REQ-AVA-017 (theming)
- [ ] 1.7 Add Dock.Avalonia packages
  - Satisfies: REQ-AVA-005 (Dock library integration)
- [ ] 1.8 Add CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection
  - Satisfies: REQ-AVA-006, REQ-AVA-013
- [ ] 1.9 Configure build to produce zero warnings
  - Satisfies: REQ-AVA-016 (zero warnings policy)

## Phase 2: Smoke Test Prototype

Goal: Build the minimum viable UI to validate Avalonia + Dock + DataGrid on Windows and Linux before investing in full migration. This is a go/no-go gate.

- [ ] 2.1 Create ViewModelBase with CommunityToolkit.Mvvm patterns
  - Satisfies: REQ-AVA-006 (INotifyPropertyChanged, commands)
- [ ] 2.2 Set up dependency injection in App.axaml.cs
  - Satisfies: REQ-AVA-013 (DI registration)
- [ ] 2.3 Implement MainWindow with Dock layout (RootDock, DocumentDock, ToolDock)
  - Satisfies: REQ-AVA-005 (docking layout)
- [ ] 2.4 Implement DockFactory for creating document and tool instances
  - Satisfies: REQ-AVA-005 (tabbed, closeable, splittable, floatable)
- [ ] 2.5 Implement navigation sidebar as a dockable tool panel
  - Satisfies: REQ-AVA-005 (navigation sidebar)
- [ ] 2.6 Port FormAbout → AboutView (simplest view, validates View/ViewModel wiring)
  - Satisfies: REQ-AVA-002 (feature parity — smoke test)
- [ ] 2.7 Port one DataGrid-heavy view (Colony or Blueprint — partial, read-only is fine)
  - Satisfies: REQ-AVA-008 (validate DataGrid with real data)
  - Deliverable: DataGrid showing real colony/blueprint data with sorting and column resize
- [ ] 2.8 Validate docking behaviors: float a tab, split document area, close/reopen tabs
  - Satisfies: REQ-AVA-005 (floatable, splittable)
- [ ] 2.9 Test on Linux (Debian-based) — verify rendering, fonts, clipboard, DataGrid
  - Satisfies: REQ-AVA-001 (Linux support validation)
- [ ] 2.10 Implement layout serialization (save/restore dock state)
  - Satisfies: REQ-AVA-005 (persist layout across sessions)

## Phase 3: Decision Gate

- [ ] 3.1 Evaluate smoke test results — is the UI acceptable?
  - Criteria: DataGrid usable, docking intuitive, renders correctly on Linux, no showstoppers
  - If NO: document issues, reassess framework choice, stop further migration
  - If YES: proceed to Phase 4

## Phase 4: Core Infrastructure (post-gate)

- [ ] 4.1 Implement IFileSystemService with platform-appropriate paths
  - Satisfies: REQ-AVA-009 (platform-appropriate file locations)
- [ ] 4.2 Implement IClipboardService for HTML paste (using Avalonia clipboard API)
  - Satisfies: REQ-AVA-011 (clipboard HTML access)
- [ ] 4.3 Implement IWindowStateService (window position/size persistence)
  - Satisfies: REQ-AVA-009 (window state persistence)
- [ ] 4.4 Configure Microsoft.Extensions.Logging + Serilog file sink
  - Satisfies: REQ-AVA-010 (logging replacement)
- [ ] 4.5 Implement timer service for background processing
  - Satisfies: REQ-AVA-012 (timer migration)
- [ ] 4.6 Implement ViewLocator for automatic View-ViewModel resolution
  - Satisfies: REQ-AVA-006 (data binding infrastructure)
- [ ] 4.7 Implement custom controls: ValidatedTextBox, FilteredComboBox
  - Satisfies: REQ-AVA-007 (custom control equivalents)
- [ ] 4.8 Implement DataGrid custom cell templates (filtered combo, validated text)
  - Satisfies: REQ-AVA-008 (DataGrid strategy), REQ-AVA-007 (custom controls)

## Phase 5: Feature Migration

- [ ] 5.1 Port FormHelp → HelpView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.2 Port FormPreferences → PreferencesView (including theme selection)
  - Satisfies: REQ-AVA-002 (feature parity), REQ-AVA-017 (theme override)
- [ ] 5.3 Port FormPlayerProfile → PlayerProfileView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.4 Port FormSurvey → SurveyView
  - Satisfies: REQ-AVA-002 (feature parity), REQ-AVA-011 (HTML import)
- [ ] 5.5 Port FormContacts → ContactsView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.6 Port FormColonyV2 → ColonyView (full implementation)
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.7 Port FormBlueprintV2 → BlueprintView (full implementation)
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.8 Port FormDeliveryRoute → DeliveryRouteView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.9 Port FormDeliveryExecution → DeliveryExecutionView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.10 Port FormShipTemplate → ShipTemplateView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.11 Port FormShipInstance → ShipInstanceView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.12 Port FormMarket → MarketView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.13 Port FormStation → StationView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.14 Port FormBuildPlanner → BuildPlannerView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.15 Port FormColonyDailyBuild → ColonyDailyBuildView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.16 Port FormColonyActivity → ColonyActivityView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.17 Port FormSupplyChain → SupplyChainView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.18 Port FormStockTargets → StockTargetsView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.19 Port FormAsteroid → AsteroidView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.20 Port FormSystem → SystemView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.21 Port FormPricingPlan → PricingPlanView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.22 Adapt all ViewModels to use ObservableObject + ObservableCollection
  - Satisfies: REQ-AVA-006 (data binding), REQ-AVA-015 (shared code)

## Phase 6: Polish & Packaging

- [ ] 6.1 Linux testing and fixes (Debian/Ubuntu — X11 and Wayland)
  - Satisfies: REQ-AVA-001 (Linux support)
- [ ] 6.2 RHEL/Fedora testing (for AWS server compatibility)
  - Satisfies: REQ-AVA-001 (Linux support)
- [ ] 6.3 macOS testing and fixes (nice-to-have)
  - Satisfies: REQ-AVA-001 (macOS support)
- [ ] 6.4 Performance profiling and optimization (large grids, startup time)
  - Satisfies: REQ-AVA-002 (feature parity — performance)
- [ ] 6.5 Configure self-contained publish for Windows
  - Satisfies: REQ-AVA-016 (Windows packaging)
- [ ] 6.6 Configure .deb packaging for Debian/Ubuntu
  - Satisfies: REQ-AVA-016 (Linux packaging — primary)
- [ ] 6.7 Configure .rpm packaging for RHEL/Fedora
  - Satisfies: REQ-AVA-016 (Linux packaging — secondary)
- [ ] 6.8 Configure .app bundle for macOS (nice-to-have)
  - Satisfies: REQ-AVA-016 (macOS packaging)
- [ ] 6.9 Accessibility pass (screen reader, keyboard navigation)
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 6.10 Remove WinForms project from solution (end of transition)
  - Satisfies: REQ-AVA-003 (transition period complete)
