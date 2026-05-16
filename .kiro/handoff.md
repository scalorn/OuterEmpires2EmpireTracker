# Session Handoff — 2026-05-16

## Branch: net8maui (57 commits, all backed up to 3 remotes)

## What Was Done This Session

### Avalonia Desktop Port — 226/228 tasks complete (99%)
- Built the full cross-platform Avalonia UI client from scratch
- All 22 feature views with CRUD, editable grids, event-driven refresh
- Infrastructure: persistence, events, background processing, 17 services, parsers, validation, reference counting, price calculator, preferences, help system, UI state persistence, DataGrid column persistence
- Clipboard import for colonies, surveys, blueprints, player profiles
- ScottPlot charts (evolution + yield distribution)
- Delivery auto-fill dialog
- Systems detail/editing with SystemRepository

### Context Migration to Common (partially done)
- Moved PlayerContext, EmpireContext, SafeFileWriter to Common
- Removed all BindingSource properties (replaced with IReadOnlyList.ToList())
- Removed all MessageBox.Show (replaced with logging)
- WinForms dependencies abstracted via delegates (MigrationRunner, ServerContext, BlueprintScanner)

## Next Task: Move ~40 remaining services to Common

### Batch 1: Pure logic services (no dependencies beyond what's already in Common)
- QueueCalculator, DistanceCalculator, ColonyStatusCalculator, ColonyBuildEligibility
- BuildOrderOptimizer, YieldDistributionService, CountdownFormatParser
- ChartColors, MinerSetupHelper, RefinerySetupHelper, SystemImporter

### Batch 2: Services that reference PlayerContext/EmpireContext (already in Common)
- ColonyService, BlueprintService, SurveyService, PlayerProfileService
- DeliveryRouteService, DeliveryPlanService, PricingPlanService
- BuildPlanMutationService, BuildPlanService, BuildPlanExecutionService
- StationService, ShipTemplateService, ShipService
- StockTargetMutationService, StockTargetService
- SupplyChainMutationService, SupplyChainService
- ContactsService, AsteroidService
- ResourceCheckService, DeliveryGenerationService, DeliveryFulfillment
- ColonyBootstrap, ColonyActivityCollector, ColonyInactivityCollector
- ColonyAdminReportBuilder, AutoAssignService, EvolutionChainService
- BlueprintReferenceCounter, ColonyReferenceCounter, SurveyReferenceCounter

### Batch 3: Parsers (need Sgml NuGet package added to Common.csproj)
- ColonyParser, SurveyParser, BlueprintScanner, PlayerProfileParser
- (ClipboardHelper stays in WinForms — uses System.Windows.Forms.Clipboard)

### What stays in WinForms project (UI-dependent):
- BackgroundProcessor (System.Windows.Forms.Timer)
- HelpRenderer, HelpTopicRegistry (form type references)
- PreferencesStore (WindowStateHelper)
- TabWarningService (form tab controls)
- ClipboardHelper, ClipboardContentDetector (WinForms clipboard)
- MarketBlueprintImporter (clipboard)
- Migration/ directory (8 migration classes + MigrationRunner)

### How to do the migration:
1. Move files to Common/Services/ (or Common/Parsers/ for parsers)
2. Keep namespaces the same (no reference updates needed)
3. Remove from WinForms .csproj (old-style, so remove the file reference or just delete the file)
4. For parsers: add `<PackageReference Include="Microsoft.Xml.SgmlReader" Version="1.8.30" />` to Common.csproj
5. Build full solution after each batch
6. Run tests to verify

### Bug found during testing:
- ColonyView wasn't rendering because DataTemplate for ColonyViewModel was missing from App.axaml (fixed)
- OverflowException in admin summary — item quantity sum exceeded int.MaxValue (fixed with long cast)

## Current branch state:
- All work committed and backed up to all 3 remotes
- Full solution builds with zero warnings
- Desktop project builds with zero warnings
- 2563 tests pass

## Remaining Avalonia tasks (4 — all require user testing):
- T5: Linux testing (Debian — X11 and Wayland)
- T6: RHEL/Fedora testing
- T7: Performance profiling with real data
- T8: macOS testing (nice-to-have)
