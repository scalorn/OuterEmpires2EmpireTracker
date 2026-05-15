# .NET 8 + MAUI Port — Tasks

## Phase 1: Project Setup & Evaluation

- [ ] 1.1 Create `OE2EmpireTracker.Maui` project with .NET 8 MAUI template
  - Satisfies: REQ-MAUI-003 (project structure), REQ-MAUI-004 (PackageReference)
- [ ] 1.2 Configure target frameworks (Windows 10+, macOS 12+)
  - Satisfies: REQ-MAUI-001 (platform targets)
- [ ] 1.3 Add reference to `OE2EmpireTracker.Common`
  - Satisfies: REQ-MAUI-003 (Common referenced by MAUI project)
- [ ] 1.4 Set up Directory.Packages.props for central package management
  - Satisfies: REQ-MAUI-004 (central package management)
- [ ] 1.5 Evaluate DataGrid options (Syncfusion, DevExpress, custom)
  - Satisfies: REQ-MAUI-008 (DataGrid strategy)
  - Deliverable: Decision record in spec/decisions/
- [ ] 1.6 Verify all Common dependencies are .NET 8 compatible
  - Satisfies: REQ-MAUI-004 (NuGet packages compatible with .NET 8)
- [ ] 1.7 Configure build to produce zero warnings
  - Satisfies: REQ-MAUI-016 (zero warnings policy)

## Phase 2: Core Infrastructure

- [ ] 2.1 Implement AppShell with sidebar navigation
  - Satisfies: REQ-MAUI-005 (navigation sidebar)
- [ ] 2.2 Implement tabbed document container (MDI replacement)
  - Satisfies: REQ-MAUI-005 (tabbed interface, closeable, reorderable)
- [ ] 2.3 Set up dependency injection in MauiProgram.cs
  - Satisfies: REQ-MAUI-013 (DI registration)
- [ ] 2.4 Implement IFileSystemService with platform paths
  - Satisfies: REQ-MAUI-009 (platform-appropriate file locations)
- [ ] 2.5 Implement IClipboardService for HTML paste
  - Satisfies: REQ-MAUI-011 (clipboard HTML access)
- [ ] 2.6 Implement IWindowStateService
  - Satisfies: REQ-MAUI-009 (window state persistence)
- [ ] 2.7 Configure Microsoft.Extensions.Logging + Serilog file sink
  - Satisfies: REQ-MAUI-010 (logging replacement)
- [ ] 2.8 Implement timer service for background processing
  - Satisfies: REQ-MAUI-012 (timer migration)
- [ ] 2.9 Implement tab state persistence (remember open tabs)
  - Satisfies: REQ-MAUI-005 (restore tabs on launch)
- [ ] 2.10 Create BaseViewModel with common patterns
  - Satisfies: REQ-MAUI-006 (INotifyPropertyChanged, CommunityToolkit.Mvvm)

## Phase 3: Simple Feature Migration

- [ ] 3.1 Port FormAbout → AboutPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 3.2 Port FormHelp → HelpPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 3.3 Port FormPreferences → PreferencesPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 3.4 Port FormPlayerProfile → PlayerProfilePage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 3.5 Port FormSurvey → SurveyPage
  - Satisfies: REQ-MAUI-002 (feature parity), REQ-MAUI-011 (HTML import)
- [ ] 3.6 Port FormContacts → ContactsPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 3.7 Implement custom controls: ValidatedEntry, FilteredPicker
  - Satisfies: REQ-MAUI-007 (custom control equivalents)
- [ ] 3.8 Adapt existing ViewModels to use ObservableObject + ObservableCollection
  - Satisfies: REQ-MAUI-006 (data binding), REQ-MAUI-015 (shared code)

## Phase 4: Complex Feature Migration

- [ ] 4.1 Implement DataGrid control wrapper (based on Phase 1 evaluation)
  - Satisfies: REQ-MAUI-008 (DataGrid strategy)
- [ ] 4.2 Port FormColonyV2 → ColonyPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.3 Port FormBlueprintV2 → BlueprintPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.4 Port FormDeliveryRoute → DeliveryRoutePage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.5 Port FormDeliveryExecution → DeliveryExecutionPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.6 Port FormShipTemplate → ShipTemplatePage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.7 Port FormShipInstance → ShipInstancePage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.8 Port FormMarket → MarketPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.9 Port FormStation → StationPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.10 Port FormBuildPlanner → BuildPlannerPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.11 Port FormColonyDailyBuild → ColonyDailyBuildPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.12 Port FormColonyActivity → ColonyActivityPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.13 Port FormSupplyChain → SupplyChainPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.14 Port FormStockTargets → StockTargetsPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.15 Port FormAsteroid → AsteroidPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.16 Port FormSystem → SystemPage
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 4.17 Port PricingPlan → PricingPlanPage
  - Satisfies: REQ-MAUI-002 (feature parity)

## Phase 5: Polish & Platform Refinement

- [ ] 5.1 macOS-specific testing and fixes
  - Satisfies: REQ-MAUI-001 (macOS 12+ support)
- [ ] 5.2 Performance profiling and optimization (large grids, startup time)
  - Satisfies: REQ-MAUI-002 (feature parity — performance)
- [ ] 5.3 Configure MSIX packaging for Windows
  - Satisfies: REQ-MAUI-016 (Windows MSIX installer)
- [ ] 5.4 Configure .app bundle for macOS
  - Satisfies: REQ-MAUI-016 (macOS .app bundle)
- [ ] 5.5 Dark mode / theming support
  - Satisfies: Open Question #3
- [ ] 5.6 Accessibility pass (screen reader, keyboard navigation)
  - Satisfies: REQ-MAUI-002 (feature parity)
- [ ] 5.7 Remove WinForms project from solution (end of transition)
  - Satisfies: REQ-MAUI-003 (transition period complete)
