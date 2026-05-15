# .NET 8 + Avalonia Port — Tasks

## Phase 1: Project Setup & Evaluation

- [ ] 1.1 Create `OE2EmpireTracker.Desktop` project with Avalonia .NET 8 template
  - Satisfies: REQ-AVA-003 (project structure), REQ-AVA-004 (PackageReference)
- [ ] 1.2 Configure target frameworks (net8.0 — runs on Windows, Linux, macOS)
  - Satisfies: REQ-AVA-001 (platform targets)
- [ ] 1.3 Add reference to `OE2EmpireTracker.Common`
  - Satisfies: REQ-AVA-003 (Common referenced by Desktop project)
- [ ] 1.4 Set up Directory.Packages.props for central package management
  - Satisfies: REQ-AVA-004 (central package management)
- [ ] 1.5 Prototype DataGrid with inline editing (validate built-in DataGrid meets needs)
  - Satisfies: REQ-AVA-008 (DataGrid strategy)
  - Deliverable: Spike showing sortable columns, cell editing, combo column
- [ ] 1.6 Verify all Common dependencies are .NET 8 compatible
  - Satisfies: REQ-AVA-004 (NuGet packages compatible with .NET 8)
- [ ] 1.7 Configure build to produce zero warnings
  - Satisfies: REQ-AVA-016 (zero warnings policy)
- [ ] 1.8 Add Avalonia.Themes.Fluent with light/dark theme support
  - Satisfies: REQ-AVA-017 (theming)
- [ ] 1.9 Add Dock.Avalonia packages and verify basic docking layout works
  - Satisfies: REQ-AVA-005 (Dock library integration)
  - Deliverable: Spike showing tabbed documents, floating, and layout save/restore

## Phase 2: Core Infrastructure

- [ ] 2.1 Implement MainWindow with Dock layout (RootDock, DocumentDock, ToolDock)
  - Satisfies: REQ-AVA-005 (docking layout, document tabs, tool panels)
- [ ] 2.2 Implement DockFactory for creating document and tool instances
  - Satisfies: REQ-AVA-005 (tabbed interface, closeable, splittable, floatable)
- [ ] 2.3 Implement navigation sidebar as a dockable tool panel
  - Satisfies: REQ-AVA-005 (navigation sidebar)
- [ ] 2.4 Implement layout serialization (save/restore dock state via Dock.Serializer.Newtonsoft)
  - Satisfies: REQ-AVA-005 (persist and restore layout across sessions)
- [ ] 2.3 Set up dependency injection with Microsoft.Extensions.DependencyInjection
  - Satisfies: REQ-AVA-013 (DI registration)
- [ ] 2.4 Implement IFileSystemService with platform-appropriate paths
  - Satisfies: REQ-AVA-009 (platform-appropriate file locations)
- [ ] 2.5 Implement IClipboardService for HTML paste (using Avalonia clipboard API)
  - Satisfies: REQ-AVA-011 (clipboard HTML access)
- [ ] 2.6 Implement IWindowStateService (window position/size persistence)
  - Satisfies: REQ-AVA-009 (window state persistence)
- [ ] 2.7 Configure Microsoft.Extensions.Logging + Serilog file sink
  - Satisfies: REQ-AVA-010 (logging replacement)
- [ ] 2.8 Implement timer service for background processing
  - Satisfies: REQ-AVA-012 (timer migration)
- [ ] 2.9 Prototype Dock integration with sample document (validate floating, splitting, tab reorder)
  - Satisfies: REQ-AVA-005 (floatable, splittable documents)
- [ ] 2.10 Create ViewModelBase with CommunityToolkit.Mvvm patterns
  - Satisfies: REQ-AVA-006 (INotifyPropertyChanged, CommunityToolkit.Mvvm)
- [ ] 2.11 Implement ViewLocator for automatic View-ViewModel resolution
  - Satisfies: REQ-AVA-006 (data binding infrastructure)

## Phase 3: Simple Feature Migration

- [ ] 3.1 Port FormAbout → AboutView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 3.2 Port FormHelp → HelpView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 3.3 Port FormPreferences → PreferencesView (including theme selection)
  - Satisfies: REQ-AVA-002 (feature parity), REQ-AVA-017 (theme override)
- [ ] 3.4 Port FormPlayerProfile → PlayerProfileView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 3.5 Port FormSurvey → SurveyView
  - Satisfies: REQ-AVA-002 (feature parity), REQ-AVA-011 (HTML import)
- [ ] 3.6 Port FormContacts → ContactsView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 3.7 Implement custom controls: ValidatedTextBox, FilteredComboBox
  - Satisfies: REQ-AVA-007 (custom control equivalents)
- [ ] 3.8 Adapt existing ViewModels to use ObservableObject + ObservableCollection
  - Satisfies: REQ-AVA-006 (data binding), REQ-AVA-015 (shared code)

## Phase 4: Complex Feature Migration

- [ ] 4.1 Implement DataGrid custom cell templates (filtered combo, validated text)
  - Satisfies: REQ-AVA-008 (DataGrid strategy), REQ-AVA-007 (custom controls)
- [ ] 4.2 Port FormColonyV2 → ColonyView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.3 Port FormBlueprintV2 → BlueprintView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.4 Port FormDeliveryRoute → DeliveryRouteView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.5 Port FormDeliveryExecution → DeliveryExecutionView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.6 Port FormShipTemplate → ShipTemplateView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.7 Port FormShipInstance → ShipInstanceView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.8 Port FormMarket → MarketView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.9 Port FormStation → StationView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.10 Port FormBuildPlanner → BuildPlannerView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.11 Port FormColonyDailyBuild → ColonyDailyBuildView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.12 Port FormColonyActivity → ColonyActivityView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.13 Port FormSupplyChain → SupplyChainView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.14 Port FormStockTargets → StockTargetsView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.15 Port FormAsteroid → AsteroidView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.16 Port FormSystem → SystemView
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 4.17 Port FormPricingPlan → PricingPlanView
  - Satisfies: REQ-AVA-002 (feature parity)

## Phase 5: Polish & Platform Refinement

- [ ] 5.1 Linux testing and fixes (Ubuntu, Fedora — X11 and Wayland)
  - Satisfies: REQ-AVA-001 (Linux support)
- [ ] 5.2 macOS testing and fixes
  - Satisfies: REQ-AVA-001 (macOS support)
- [ ] 5.3 Performance profiling and optimization (large grids, startup time)
  - Satisfies: REQ-AVA-002 (feature parity — performance)
- [ ] 5.4 Configure self-contained publish for Windows
  - Satisfies: REQ-AVA-016 (Windows packaging)
- [ ] 5.5 Configure AppImage or .deb packaging for Linux
  - Satisfies: REQ-AVA-016 (Linux packaging)
- [ ] 5.6 Configure .app bundle for macOS
  - Satisfies: REQ-AVA-016 (macOS packaging)
- [ ] 5.7 Accessibility pass (screen reader, keyboard navigation)
  - Satisfies: REQ-AVA-002 (feature parity)
- [ ] 5.8 Remove WinForms project from solution (end of transition)
  - Satisfies: REQ-AVA-003 (transition period complete)
