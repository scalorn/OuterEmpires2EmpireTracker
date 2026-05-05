# Completed Backlog Items

Items moved here from BACKLOG.md after implementation, plus completed specs that were implemented outside the backlog.

---

### BL-069: Migrate Existing BindingList Fields to List on PlayerContext
Replaced all `BindingList<T>` fields with `List<T>` on PlayerContext and EmpireContext for consistency with newer entity lists. Removed `ListChanged` auto-invalidation handlers; added fallback linear scans to `FindBlueprint`, `FindSurvey`, and `FindColony` so cache misses from post-cache additions self-heal. Added explicit cache invalidation to `CleanupOrphanedData`. Updated `FindByDedupKey` signature to `IList<T>`, fixed `FilteredComboBox` cast, updated test helpers.
**Status: Complete**

### BL-004: Commodity Fulfillment Unit Tests
Optional task from commodity-delivery-loop spec (task 5.2). Extract fulfillment logic from the Form handler into a testable static helper and add unit tests.
**Status: Complete** — Extracted `DeliveryFulfillment` static helper with `FulfillCommodity`, `StageFlatpack`, `DeliverWorkers`. 17 unit tests covering all three operations.

### BL-007: Skill Multipliers — Timer Processing
From Recommendations.md #13 Phase 4: ProductionFocus/Builder/ResearchFocus time reductions deferred until timer processing is fully implemented.
**Status: Complete** — ProductionFocus applied to manufacturing and commodity factory cycle times. ResearchFocus applied to research times. Builder was already applied to build times.

### BL-010: Activity Inactivity Mode
Add an inactivity/idle highlighting mode to the Colony Activity window. Surfaces idle and underutilized production structures.
**Status: Complete** — ColonyInactivityCollector detects idle structures across all 5 production types plus underutilized refiners with warehouse stockpile exemption. "Show Inactive" checkbox toggles the Colony Activity form between activity and inactivity modes. Spec: `.kiro/specs/activity-inactivity-mode/`.

### BL-018: Mass Blueprint Importer
Import blueprints in bulk from the in-game market HTML. Parses market listings, extracts seller name and TechLevel, deduplicates by Name+Evolution+Type+Class+TechLevel, routes Government→global vs player storage.
**Status: Complete** — Import Market button on Blueprint form. Idempotency tests confirm no duplicates on re-import. Spec: `.kiro/specs/mass-blueprint-importer/`.

### BL-022: JSON Serialization — Skip Default Values
Configure Newtonsoft.Json serialization to skip fields with default values (null/empty strings, false booleans, zero integers/decimals) to reduce JSON file size.
**Status: Complete** — JsonSettings static class with DefaultValueHandling.Ignore and NullValueHandling.Ignore. All serialization call sites updated. ItemBagJSONConverter applies defaults to nested items. FsCheck property tests + unit tests validate round-trip and size reduction. Spec: `.kiro/specs/json-default-skip/`.

### BL-030: Colony Tab — Structure Count Warning Colors
The game limits colonies to 65 structures. Change the Structures tab selector background to yellow at 60+ structures, red at 66+.
**Status: Complete** — TabWarningService evaluates structure count thresholds (Yellow ≥60, Red ≥66). FormColony applies tab background colors on all data-change events. FsCheck property tests + unit tests validate logic. Spec: `.kiro/specs/worker-tab-due-warning/`.

### BL-031: Colony Tab — Worker Request Due Date Warning Colors
Change the Worker tab selector background to yellow when any worker request is due within 2 days, red when due within 1 day.
**Status: Complete** — TabWarningService evaluates unfulfilled commodity request due windows (Yellow ≤2 days, Red ≤1 day/overdue). Fulfilled requests and DateTime.MinValue sentinels excluded. Implemented alongside BL-030. Spec: `.kiro/specs/worker-tab-due-warning/`.

### BL-035: Blueprint Evolution Graph
Add a chart to the Blueprint form showing the evolution chain for the selected blueprint, plotting numeric properties as percentages of Ev0 base values.
**Status: Complete** — EvolutionChainService resolves ancestor chains. Chart tab with per-property line series and checkbox panel. FsCheck property tests validate chain resolution, graph data extraction, unchanged-property exclusion, and percentage normalization. Spec: `.kiro/specs/evolution-graph/`.

### BL-040: Delivery Execution — Load Item Count and Volume
The delivery execution form displays a count of the number of items, total quantity, and calculated total volume to the right of the "Load Before Departure" header. Format: "Load Before Departure — N items, Q qty, V vol".
**Status: Complete** — Implemented in FormDeliveryExecution.

### BL-044: EmpireContext/PlayerContext PascalCase Naming Cleanup
All 28 camelCase public fields renamed to PascalCase across 41 files.
**Status: Complete** — See AMB-045.

### BL-062: Blueprint Form — Evolved Blueprint Resource Import Fix
Bug: Importing the resources tab of an evolved blueprint created a new broken blueprint instead of updating the selected one. The resources tab HTML lacks key dedup fields (class, tech level, blueprint type), so FindByDedupKey couldn't match.
**Status: Complete** — Added IsResourcesOnlyImport detection and MergeResourcesOnly merge logic to MarketBlueprintImporter. cmdImport_Click routes resources-only imports to the selected blueprint. 2 FsCheck property tests + 4 unit tests. Spec: `.kiro/specs/blueprint-form-fixes/`.

### BL-066: Blueprint Form — Filter Combo Boxes Not Sticky
Bug: Filter combo boxes on the Blueprint form didn't persist their state between form close and reopen because they were created dynamically without Name properties.
**Status: Complete** — Assigned Name properties to all 5 filter controls (cmbFilterType, cmbFilterClass, cmbFilterTechLevel, cmbFilterEvolution, chkEvolutionAndAbove) in InitFilterPanel. WindowStateHelper already handles save/restore by name. 2 FsCheck property tests. Spec: `.kiro/specs/blueprint-form-fixes/`.

---

## Completed Specs (no backlog entry)

### Background Processing
Background thread that automatically processes colony timers without requiring the Colony Form to be open. Periodically scans all colonies, identifies expired timers, runs ProcessColony(), notifies open forms, and persists changes.
**Status: Complete** — BackgroundProcessor runs on a dedicated non-UI thread with 60-second tick interval. Spec: `.kiro/specs/background-processing/`.

### Blueprint Form Filters
Title bar with live global/player blueprint counts, plus structured filtering controls (Blueprint Type, Class, Tech Level, Evolution) for the blueprint list view.
**Status: Complete** — Dynamic title bar, four combo box filters with AND semantics, combined with existing text search. FsCheck property tests. Spec: `.kiro/specs/blueprint-form-filters/`.

### Blueprint Reference Guard
Reference counting and deletion protection for blueprints. Prevents deleting blueprints still referenced by colony structures, other blueprints, or surveys.
**Status: Complete** — BlueprintReferenceCounter scans all data sources. Delete button disabled when references exist. FsCheck property tests. Spec: `.kiro/specs/blueprint-reference-guard/`.

### Colony Daily Build
Dedicated form for managing structure building across colonies on a delivery route. Players select a route, see all colonies with staged structures ready to build, and initiate builds with countdown timers.
**Status: Complete** — FormColonyDailyBuild with route selector, scrollable colony panels, build initiation with Builder skill time reduction. Spec: `.kiro/specs/colony-daily-build/`.

### Colony Form UI Tweaks
Dynamic form title, sortable/filterable colony list, informative tab headers, and deferred structure loading for the colony form.
**Status: Complete** — Dynamic title with player name and colony count, column sort, text filter, tab headers with counts. Spec: `.kiro/specs/colony-form-ui-tweaks/`.

### Colony Import Dedup
Colony clipboard import dedup — parse into a temporary colony first, search by name (case-insensitive), then merge into existing or create new. Prevents duplicate colonies on import. Also adds duplicate-name validation on the colony name text field.
**Status: Complete** — ColonyImportHelper static class with FindByName, MergeIdentity, CreateFromTemp, IsDuplicateName. ColonyParser.ParseClipboardToTemp for non-mutating clipboard parse. FormColony.cmdImportColony_Click rewritten with parse→search→merge-or-create flow. 5 FsCheck property tests. Spec: `.kiro/specs/colony-import-dedupe/`.

### Colony Structure Dedupe Fix (Bugfix)
Fixed duplicate structures appearing when manually-added structures (gameSequence=0) were re-imported from clipboard. Renamed gameSequence to displaySequence, added buildingID field, changed merge detection to use FlatpackBlueprintUUID + displaySequence.
**Status: Complete** — Compound key merge logic, positional matching for commodity manufactories. Spec: `.kiro/specs/colony-structure-dedupe-fix/`.

### Delivery Auto-Fill Phase 7
Extended auto-fill system to support Flatpacks, Manufacturing Resources, and Workers in addition to Commodities. Added flatpack staging on delivery execution and StagingResources flag for manufacturing structures.
**Status: Complete** — FormAutoFill with checkboxes for all four request types. DeliveryPlanViewModel auto-fill methods for each type. Spec: `.kiro/specs/delivery-autofill-phase7/`.

### Directory Restructure
Reorganized project from catch-all Baseline/Data directories into purpose-driven structure: Models/, Services/, Parsers/, Persistence/. Covers both main project and test project.
**Status: Complete** — All files relocated, csproj entries updated, build and tests preserved. Spec: `.kiro/specs/directory-restructure/`.

### Form Management Alignment
Aligned Colony, Blueprint, Survey, and PlayerProfile forms to follow the same New/Save/Delete button pattern used by Delivery Routes. Removed Cancel button, Save no longer clears the form.
**Status: Complete** — Consistent button layout across all management forms. Spec: `.kiro/specs/form-management-alignment/`.

### Icon Position Refresh
Repeatable process to extract icon positions from refreshed MarketSample HTML files, compare against BaselineData.json, update changed positions, and add new BlueprintType entries. Split commodity factories into per-industry BlueprintType entries.
**Status: Complete** — IconPositionExtractor with coverage gap detection. Per-industry commodity factory types. FsCheck property tests. Spec: `.kiro/specs/icon-position-refresh/`.

### Individual Blueprint Clipboard Fix (Bugfix)
Fixed individual blueprint import silently doing nothing because processClipboard passed raw clipboard data (with Version/StartHTML header) to ProcessHtml instead of extracting the HTML fragment first.
**Status: Complete** — Added ExtractHtmlFragmentFromClipboardData call before ProcessHtml. Spec: `.kiro/specs/individual-bp-clipboard-fix/`.

### Main Menu Overhaul
Overhauled MainWindow menu: File menu with New/Open/Save/Save As/Exit and dirty-state tracking, Edit renamed to Manage with alphabetical items, Help → About dialog.
**Status: Complete** — Full menu restructure with file lifecycle operations and last-opened-file memory. Spec: `.kiro/specs/main-menu-overhaul/`.

### Market Import Clipboard Fix (Bugfix)
Fixed market import returning zero results from large clipboard HTML (~384KB). The SGML parser failed on full game UI HTML passed through ExtractHtmlFragmentFromClipboardData.
**Status: Complete** — Fixed HTML fragment extraction for large clipboard payloads. Spec: `.kiro/specs/market-import-clipboard-fix/`.

### MDI Window Menu
Added standard MDI Window menu to MainWindow with Cascade, Tile Horizontal, Tile Vertical commands and auto-populated list of open child forms.
**Status: Complete** — Window menu between Manage and Help with layout commands and child window list. Spec: `.kiro/specs/mdi-window-menu/`.

### Parser Idempotency Tests
Added repeated-import verification tests for Survey and Colony parsers. Parsing the same HTML multiple times must not create duplicate child records.
**Status: Complete** — Idempotency tests for both parsers confirming stable structure/commodity/resource counts on re-import. Spec: `.kiro/specs/parser-idempotency-tests/`.

### Structure Name Normalization
Centralized name normalization so flatpack blueprints expose structure-oriented display names (stripping " Flatpack" suffix). OutputItemName property on Blueprint, ExtendedName updated to use it.
**Status: Complete** — OutputItemName computed property, ExtendedName uses it automatically. All UI consumers display correct structure names. Spec: `.kiro/specs/structure-name-normalization/`.

### Window State Persistence
Remembers window positions, sizes, and form control state across application restarts. Covers MDI container, all child forms, per-type window numbering, filters, combo selections, grid columns, and sorting.
**Status: Complete** — PreferencesStore with UIPreferences.json in %LOCALAPPDATA%. WindowStateHelper for save/restore. BoundsValidator for multi-monitor safety. Spec: `.kiro/specs/window-state-persistence/`.

### Colony Activity Form
Read-only form aggregating all active countdown timers and unfulfilled commodity requests across all colonies. Sortable, filterable DataGridView with live countdown refresh.
**Status: Complete** — ColonyActivityCollector static helper, FormColonyActivity with activity type checkboxes, text filter, 1-second timer refresh. 6 property tests + unit tests. Spec: `.kiro/specs/colony-activity-form/`.

### Commodity Delivery Loop
Auto-fill commodity drop-offs from colony requests on the route builder plan tab, plus commodity fulfillment/unfulfillment on delivery execution.
**Status: Complete** — AutoFillCommodities on DeliveryPlanViewModel, FormAutoFill modal dialog, commodity fulfillment in FormDeliveryExecution, DeliveryFulfillment static helper with unit tests. Spec: `.kiro/specs/commodity-delivery-loop/`.

### Individual Import Dedup
Individual blueprint clipboard import dedup — parse complete blueprint identity from clipboard HTML, use dedup-key matching to route to correct blueprint (update existing or create new), reusing MarketBlueprintImporter routing logic.
**Status: Complete** — FindByDedupKey/UpdateExisting made internal on MarketBlueprintImporter. IsGlobalRoute pure routing function. BlueprintScanner.ParseClipboardToTemp for non-mutating parse. FormBlueprint.cmdImport_Click rewritten with parse→check selected→route→persist flow. 4 FsCheck property tests. Spec: `.kiro/specs/individual-import-dedup/`.

### BL-038: Survey Import Dedup
Survey clipboard import dedup — parse into a temporary survey first, search by PlanetName+SurveyID (case-insensitive), then merge into existing or create new. Falls back to current behavior when SurveyID is not parsed. Adds clipboard HTML guard and error handling.
**Status: Complete** — SurveyImportHelper static class with FindByKey, CreateFromTemp, MergeData. SurveyParser.ParseClipboardToTemp for non-mutating parse. FormSurvey.cmdImport_Click rewritten with parse→search→merge-or-create flow. 3 FsCheck property tests.


### Baseline Data Stability
Deterministic UUIDs for global blueprints, versioned migration framework with DataVersion gating, idempotent rename table, LegacyUUID preservation, externalization of GameConstants/Commodities/RefiningRecipes/ResearchTimeLookup from code to BaselineData.json, and double→decimal conversion for all game data numeric types.
**Status: Complete** — DeterministicUUID (UUID v5), RemapUUID reference walker, MigrationRunner with Migration001, RenameTable, BaselineGameConstants model, ResearchTimeEntry model. All blueprint creation paths updated. 6 FsCheck property tests (UUID determinism, RemapUUID completeness, rename idempotency, migration version gating, LegacyUUID preservation, decimal round-trip). Spec: `.kiro/specs/baseline-data-stability/`.


### BL-024: User Help Documentation
In-app help system with markdown docs in `docs/` folder (GitHub-browsable) rendered at runtime via Markdig + WebBrowser control. FormHelp dialog with TreeView navigation and HTML content panel. Help → Contents (Ctrl+F1) and F1 context-sensitive help mapping each form to its doc page. 9 documentation pages covering all application features.
**Status: Complete** — HelpTopicRegistry (form-to-doc mapping), HelpRenderer (Markdig markdown→HTML with embedded CSS), FormHelp (SplitContainer with TreeView + WebBrowser, internal link interception), MainWindow integration (Contents menu item, F1 ProcessCmdKey override). Spec: `.kiro/specs/user-help-docs/`.

### BL-067: MDI Window Numbering — Reuse Lowest Available Number
Bug: Window numbers incremented monotonically instead of reusing gaps left by closed windows.
**Status: Complete** — Replaced `_windowNumberCounters` counter with gap-scanning algorithm in `MainWindow.OpenMdiChild<T>()` that finds the lowest unused positive integer from `this.MdiChildren`. 2 FsCheck property tests + 6 edge-case unit tests. Spec: `.kiro/specs/mdi-window-reuse/`.

### BL-063: Colony Activity Form — Filter Controls Not Sticky
Bug: Filter checkboxes and text box on Colony Activity form didn't persist between close/reopen.
**Status: Complete** — Controls already had Name properties in Designer and WindowStateHelper already saved/restored them. Confirmed working via diagnostic logging — no code changes needed.

### BL-064: Delivery Execution Form — Filter Controls Not Sticky
Bug: Filter controls on Delivery Execution form didn't persist between close/reopen.
**Status: Complete** — Root cause: the "Execute" button on FormDeliveryRoute created FormDeliveryExecution directly (`new FormDeliveryExecution(routeUUID, planUUID)`) bypassing `OpenMdiChild<T>()`, so no Tag/RestoreState/SaveState occurred. Fix: route through `MainWindow.OpenMdiChild<T>()` (changed from private to internal), replaced parameterized constructor with public properties + `ApplyPreSelection()` method.

### BL-065: Delivery Routes Form — Filter Text Not Sticky
Bug: Filter text box on Delivery Routes form didn't persist between close/reopen.
**Status: Complete** — Controls already had Name properties in Designer and WindowStateHelper already saved/restored them. Confirmed working via diagnostic logging — no code changes needed.

### BL-060: No-Player Guard — Restrict to Player Profile Form
When no player profile exists, all Manage menu items except Player Profiles are disabled, along with the player dropdown. Prevents confusing errors when importing data with no active player.
**Status: Complete** — Added `UpdateNoPlayerGuard()` to MainWindow. Called on startup, after profile changes, after File→New, and after file load. Menu items re-enable automatically when a profile is created.

### BL-058: ~~Delivery Execution — Load Before Departure Totals~~
**Status: Duplicate** — Same as BL-040.

### BL-056: Delivery Routes — Focus After Delete
Bug: Removing a colony stop from a delivery route caused the grid to jump to the top.
**Status: Complete** — After deletion, selection moves to the row above the deleted item (or first row if top was deleted), and the grid scrolls to show it via `FirstDisplayedScrollingRowIndex`.

### BL-057: Delivery Routes — Focus After Move Up/Down
Bug: Moving a stop up or down in a delivery route caused the grid to jump to the top.
**Status: Complete** — After move, the grid scrolls to the moved row via `FirstDisplayedScrollingRowIndex`. Selection was already being re-applied but the viewport wasn't following it.

### BL-053: Colony Deletion Protection — In-Use by Route/Plan
Colonies referenced by delivery routes or plans are now protected against deletion.
**Status: Complete** — ColonyReferenceCounter scans DeliveryRouteList and DeliveryPlanList for colony UUID references. Delete button shows "In Use (N)" and is disabled when references exist. Delete handler also blocks with a message listing route/plan counts. Same pattern as BlueprintReferenceCounter.

### BL-051: Survey Form — Survey Count in Title Bar
Show survey count in the Survey form's title bar matching the Colony form pattern.
**Status: Complete** — Added `UpdateTitle()` to FormSurvey. Format: "#N - Manage Surveys - PlayerName : Count". Called on startup, player change, save, delete, and import.

### BL-052: Survey Deletion Protection — In-Use by Miner
Surveys assigned to mining rigs are now protected against deletion.
**Status: Complete** — SurveyReferenceCounter scans all ColonyStructure.MiningSurvey fields for survey UUID references. Refs column in survey list view, delete button shows "In Use (N)" when referenced, delete handler blocks with miner count message. Same pattern as BlueprintReferenceCounter and ColonyReferenceCounter.

### BL-054: Survey Form — Normalize Scan DateTime
Normalize survey scan date/time from the game's non-standard format (`"27JUL24-11:44p"`) into ISO 8601 (`"2024-07-27T23:44:00"`) for internal storage. Display game format in the UI. DateTimePicker for editing. Chronological sorting via ISO strings in ListView Tags. Data migration for existing surveys.
**Status: Complete** — SurveyDateTimeParser utility (parse/format/round-trip), SurveyParser ISO conversion on import, SurveyViewModel.DisplayDateTime, ListViewItemComparer Tag-based sorting, FormSurvey DateTimePicker + read-only text box, Migration003_SurveyDateTimeNormalization. 9 FsCheck property tests (200 iterations each) + 52 unit tests. Spec: `.kiro/specs/survey-datetime-normalization/`.

### BL-031: Colony Import Timestamp Tracking
Record the import timestamp on the Colony data model when importing via CreateFromTemp or MergeIdentity. Backfill existing colonies via Migration004. Timestamp stored as ISO 8601 UTC string using SurveyDateTimeParser.
**Status: Complete** — LastImportDateTime property on Colony, stamped during import, backfilled by migration. 2 FsCheck property tests (JSON round-trip, import produces valid ISO). Spec: `.kiro/specs/colony-import-timestamp/`.

### BL-049: Colony Import Timestamp — Surface on Activity Window
Surface colony import staleness on the Colony Activity form's inactivity mode and color the Administration tab based on staleness (yellow at 5+ days, red at 6+ days). Extends BL-031.
**Status: Complete** — ColonyInactivityCollector emits ColonyImportStaleness rows for colonies >1 day old. FormColonyActivity "Import Staleness" checkbox in inactivity mode. TabWarningService.EvaluateColonyImportStalenessWarning wired to Administration tab. 3 FsCheck property tests (staleness rows, warning levels, null/empty handling). Spec: `.kiro/specs/colony-import-timestamp/`.

### BL-068: CountDownTime and Remaining DateTime.Now → UTC Migration
Migrate all DateTime.Now usage to DateTime.UtcNow across the codebase. Convert existing CountDownTime StartTime/EndTime from local to UTC via Migration004. Retrofit SurveyDateTimeParser to be UTC-aware with Z suffix.
**Status: Complete** — Folded into colony-import-timestamp spec. DateTime.Now→UtcNow sweep across 11 source files. Migration004 converts CountDownTime values. SurveyDateTimeParser IsoFormat updated to include Z suffix. 3 FsCheck property tests (UTC storage/display, CountDownTime UTC consistency, migration local→UTC conversion). Spec: `.kiro/specs/colony-import-timestamp/`.

### BL-043: Colony Duplicate Validation — Planet+System Instead of Colony Name
With the dedup key changed from ColonyName to PlanetName+SystemName, the duplicate-name validation on the colony name text field was no longer relevant. The SetError/ClearError validation was removed from txtColonyName_TextChanged — it now does a simple write-through. ClearError remains in the import handler to clear stale state after successful import.
**Status: Complete** — Duplicate colony name validation removed as part of the colony-import-dedupe work. Spec: `.kiro/specs/colony-import-dedupe/`.

### BL-030: Colony Administration Summary Report
Per-colony status report on the Administration tab of the Colony form. Shows building progress, commodity requests, idle structures, active manufacturing with batch completion times, and aggregated mining/refining rates — all scoped to the selected colony.
**Status: Complete** — ColonyAdminReportBuilder static service builds colored RTF via RtfBuilder. Report sections: Building → Commodity Requests → Inactivity (7 groups in fixed order) → Activity (manufacturing/commodity mfg/research with batch times, aggregated mining/refining). RichTextBox on Administration tab with 60-second timer refresh, colony selection refresh, and ColonyDataChanged refresh. 11 FsCheck property tests covering all correctness properties. Spec: `.kiro/specs/colony-admin-summary/`.

### BL-039: Colony Administration Tab — Activity/Inactivity Status Area
Consolidated into BL-030 (Colony Administration Summary Report). The Administration tab now shows all activity and inactivity data scoped to the selected colony.
**Status: Complete** — See BL-030. Spec: `.kiro/specs/colony-admin-summary/`.

### BL-021: Player Profile Importer
Import player profile data from game HTML. Parse the in-game profile page to extract player name, faction, credits, all three rank tracks, skill points, skill group states, individual skill levels, and training status. Update existing profiles by case-insensitive name match or create new ones.
**Status: Complete** — PlayerProfileParser with ProcessClipboard/ProcessHtml. Parses identity, credits, headline fields (CitizenId, RegistrationDate, ActiveTime), rank tracks (Public/Private/Military with level, title, XP), skill points, skill groups, individual skills with training detection. Import button on FormPlayerProfile. 6 FsCheck property tests + integration tests against real game HTML. Spec: `.kiro/specs/player-profile-import/`.

### BL-046: Import Clipboard Validation — Show Error on Wrong Content
On all import buttons, if the clipboard HTML doesn't contain the expected content type, show a message box explaining what was found vs what was expected.
**Status: Complete** — Consolidated with BL-048. See BL-048.

### BL-048: Import Content Type Guard — Prevent Cross-Type Imports
Prevent importing wrong clipboard content type (e.g. blueprint HTML on the survey form). Each import handler validates clipboard HTML via `ClipboardContentDetector.Detect()` before parsing.
**Status: Complete** — New `ClipboardContentDetector` static class sniffs HTML for distinctive CSS class markers (ColonyInformation_PlanetOverview, ScanDetailOutputResourceName, ShipComponentProperty, ui_character_detail, Market_ShipComponentProperty). All 5 import handlers (colony, survey, blueprint individual, blueprint market, player profile) validate content type before parsing. Mismatched content shows a clear message: "The clipboard contains {found}, not {expected}." 17 unit tests. Blueprint import allows Survey content type through for resources-only imports.

### BL-034: Codebase Duplication Scan & Cleanup
Scanned the codebase for duplicated code blocks, refactoring candidates, and cleanliness opportunities.
**Status: Complete** — Moved clipboard HTML extraction logic from BlueprintScanner into ClipboardHelper (canonical location). Added `IsBuiltAndOnline` property to ColonyStructure model; ColonyInactivityCollector uses it. RefinerySetupHelper now reuses `structure.IsBuiltAndOnline` and `MinerSetupHelper.SetupTimer()` instead of reimplementing. Extracted `RefreshStatusDisplay()` helper in FormColony replacing 4 identical RtfBuilder patterns. Remaining patterns (ListView population, form titles) are domain-specific and not worth abstracting.

### BL-041: Delivery Execution — Stale Plan After Delete
Bug: After deleting a delivery plan, the delivery execution form kept displaying the deleted plan.
**Status: Complete** — `OnDeliveryDataChanged` now checks if `selectedPlan` still exists in `DeliveryPlanList`; if not, clears the execution view and hides buttons. `cmdDeletePlan_Click` now fires `playerContext.OnDeliveryDataChanged()` so other open instances get notified.

### BL-042: Delivery Execution — New Plan Not Visible Until Reopen
Bug: After creating a new delivery plan, the execution form's plan dropdown was stale until form reopen.
**Status: Complete** — `OnDeliveryDataChanged` now calls `PopulatePlanDropdown()` to refresh the dropdown before rebuilding the execution view. New plans appear immediately.

### BL-061: Preferences Form — Configurable Thresholds
Create a Preferences form (accessible from File → Preferences) that exposes nine configurable values previously hardcoded: structure count yellow/red thresholds, worker request due window yellow/red, colony import staleness yellow/red, background processing interval, admin report refresh interval, and countdown display refresh rate. Time-based values stored as seconds, displayed/entered in countdown format (e.g. "2d 0h 0m 0s"). Persisted in UIPreferences.json via PreferencesStore. All consumers (TabWarningService, BackgroundProcessor, FormColony, ColonyStructure, FormColonyActivity, PlayerSkillBlock, MainWindow) read from PreferencesStore instead of constants. Changes take effect immediately without restart.
**Status: Complete** — ThresholdPreferences model with Validate(), CountdownFormatParser (inverse of FormatSeconds), FormPreferences modal dialog with 6 GroupBox sections, "Preferences..." menu item in File menu, PreferencesStore backward-compatibility null-guard. 6 FsCheck property tests (validation correctness, countdown round-trip, countdown rejects invalid, structure/worker/staleness warning thresholds). 28 unit tests. Spec: `.kiro/specs/preferences-form/`.

### BL-016: Pricing Plans
Define pricing models for resources and items. Calculate the price of a manufactured item based on raw resources and time invested. Supports multiple pricing plans per player for different valuation scenarios (cost basis, market value, replacement cost, etc.).
**Status: Complete** — PricingPlan model with ResourcePrices dictionary (composite key: ResourceName|Purity), ComputedPrice result type, PriceCalculator static service (commodity and blueprint price computation with completeness flagging). PlayerContext integration (persistence, cascade delete, orphan cleanup). FormPricingPlan MDI child form with CRUD, resource price grid, and zero-vs-absent distinction. Blueprint form integration: pricing plan combo box and read-only calculated price textbox with live updates via PricingDataChanged event. 9 FsCheck property tests (purity determination, non-negative validation, commodity price formula, completeness flag, blueprint price formula, zero-vs-absent, serialization round-trip, whitespace name rejection). Spec: `.kiro/specs/pricing-plans/`.


### Data Model Thread Safety
Layered synchronization strategy for the data model so BackgroundProcessor, ColonyStatusCalculator, and multiple FormColonyV2 MDI child forms can safely share mutable colony data. Three-tier locking: PlayerContext._listLock (coarse), Colony.ColonyLock ReaderWriterLockSlim (per-colony), collection-level _syncRoot (ItemBag, PropertyBag, LockTracking). Background calculation with CancellationTokenSource for FormColonyV2 colony selection. Lock ordering convention prevents deadlocks. Graceful degradation on timeout.
**Status: Complete** — Replaced Colony.ProcessingLock with ReaderWriterLockSlim ColonyLock (NoRecursion, 1s read / 5s write timeouts). Added _syncRoot to ItemBag, PropertyBag, LockTracking. Added PlayerContext._listLock with SnapshotColonyList(). FormColonyV2 background calculation via ThreadPool.QueueUserWorkItem with CancellationTokenSource + generation counter. Read locks for display, write locks for mutations, events fired outside all locks. 10 thread-safety tests (concurrent processing, concurrent collections, defensive copies, cancellation, lock timeouts, BackgroundProcessor skip-and-continue). Performance optimizations: plain text status (eliminated RTF parsing), SuspendLayout on nested FlowLayoutPanels, single-item combo datasource for active processes. Colony switch time reduced from 1.5-4.3s to 0.3-0.7s. Spec: `.kiro/specs/data-model-thread-safety/`.

### Empire Systems Spec (8 Iterations)
Comprehensive empire management feature set implemented across 8 iterations in `.kiro/specs/empire-systems/`. Delivered: Build Planner (Iteration 1) with BuildPlan/BuildItem models, BuildPlanService, ResourceCheckService, DeliveryGenerationService, QueueCalculator, AutoAssignService, FormBuildPlanner; Ships (Iteration 2) with ShipTemplate/Ship models, ShipBuildService, FormShipTemplate, FormShipInstance; Ship-Aware Delivery (Iteration 3) with cargo volume computation, trip splitting, ship assignment; Stations (Iteration 4) with Station model, holds, components, munitions, FormStation, station route stops, StationReferenceCounter; Market (Iteration 5) with MarketListing/MarketTransaction models, MarketService, FormMarket; Full Production Queue + Supply Chain + Asteroids (Iteration 6) with Mining/Refining/Research in Build Planner, FormAsteroid, FormSupplyChain, SupplyChainService, Warehouse Overflow tab; Stock Targets (Iteration 7) with StockPlan/StockTarget/StockProfile models, StockTargetService, FormStockTargets, cascade integration; Delivery Auto-Fill Time Horizon (Iteration 8). Cross-cutting: Contacts form (Factions/ExternalCharacters), reference counter expansions, IsActive toggle pattern, 13 new entity types, PlayerContext updates, data migration.
**Status: Complete** — All 8 iterations implemented. Backlog items BL-001, BL-002, BL-003, BL-011, BL-012, BL-013, BL-014, BL-015, BL-017, BL-032, BL-055, BL-059 resolved.


### BL-001: Delivery Routes — Phase 8: Ship Integration
**Status: Complete** — Implemented as part of empire-systems Iteration 2-3 (Ships + Ship-Aware Delivery). Ship cargo capacity, volume computation, trip splitting, and ship assignment on delivery plans.

### BL-002: Delivery Routes — Phase 9: Space Station Hubs
**Status: Complete** — Implemented as part of empire-systems Iteration 4 (Stations). Station model with holds, components, munitions. Stations as route destinations.

### BL-003: Delivery Auto-Fill — Time Horizon Parameter
**Status: Complete** — Implemented as part of empire-systems Iteration 8. Time horizon parameter on flatpack auto-fill, persisted as user preference.

### BL-011: Manufacturing Queue
**Status: Complete** — Implemented as Build Planner in empire-systems Iteration 1. BuildPlan/BuildItem models, BuildPlanService, ResourceCheckService, DeliveryGenerationService, QueueCalculator, AutoAssignService, FormBuildPlanner MDI child.

### BL-012: Ships
**Status: Complete** — Implemented in empire-systems Iteration 2. ShipTemplate/Ship models, ShipBuildService, FormShipTemplate, FormShipInstance with Overview+Cargo tabs.

### BL-013: Ship-Aware Delivery Execution
**Status: Complete** — Implemented in empire-systems Iteration 3. Cargo volume computation, trip splitting, ship assignment UI on delivery plans.

### BL-014: Stations
**Status: Complete** — Implemented in empire-systems Iteration 4. Station model with Holds/Components/Munitions, FormStation MDI child, StationReferenceCounter.

### BL-015: Station Destinations in Routes
**Status: Complete** — Implemented in empire-systems Iteration 4. Station and asteroid stops in delivery routes and execution.

### BL-017: Market
**Status: Complete** — Implemented in empire-systems Iteration 5. MarketListing/MarketTransaction models, MarketService, FormMarket with Listings/Transactions/Summary tabs.

### BL-032: Manufacturing Build Queue Calculator
**Status: Complete** — Implemented as QueueCalculator in empire-systems Iteration 1 (Build Planner).

### BL-055: Mining/Refining/Manufacturing/Research Queue System
**Status: Complete** — Implemented as part of empire-systems Iteration 6. Mining/Refining/Research build item types in Build Planner, FormAsteroid, FormSupplyChain, SupplyChainService.

### BL-059: Manufacturing Build Queue — Auto-Create Orders from Fill Levels
**Status: Complete** — Implemented as Stock Targets in empire-systems Iteration 7. StockPlan/StockTarget/StockProfile models, StockTargetService, FormStockTargets, cascade integration in BackgroundProcessor.

### BL-074: Stock Profiles Tab on FormStockTargets
**Status: Complete** — Implemented Profiles tab on FormStockTargets with left-list/right-detail pattern. Profile list with filter, name/active fields, entries grid (GroupID + Plan), add/remove entry panel with filtered plan combo, logic summary label showing AND/OR grouping. TabControl wraps existing Targets & Plans content alongside new Profiles tab.


### BL-075: Crate Master-Detail UI (Ship Cargo, Station Hold)
**Status: Complete** — Added dgvCrateContents detail grid to FormShipInstance Cargo tab and dgvHoldCrateContents to FormStation Hold tab. When a crate row is selected in the main grid, the detail grid below shows the crate's contents. Crate rows display "[Crate]" prefix with item count. Detail grid hidden when non-crate row selected.


### BL-076: Ship Instance Stats and Swap Component
**Status: Complete** — Added rtbStats (RichTextBox) to FormShipInstance Overview tab showing computed ship stats via ShipBuildService.ComputeStats (same format as FormShipTemplate). Added cmdSwapComponent button that opens a blueprint picker dialog filtered by the selected slot's type, allowing component replacement. Stats refresh after swap.


### BL-077: Station Blueprint Selector and Stats Display
**Status: Complete** — Added cmbStationBlueprint (DropDownList) to FormStation Components tab for selecting/changing the station hull blueprint. Added rtbStationStats (RichTextBox) showing computed station stats via ShipBuildService.ComputeStationStats: mass, power, health, shields, defence ratings, and weapon counts. Blueprint change triggers component grid and stats refresh.


### BL-078: Market Pricing Plan Integration on Summary Tab
**Status: Complete** — Added cmbPricingPlan (DropDownList) to FormMarket Summary tab filter row. When a pricing plan is selected and Compute is clicked, the per-item breakdown grid shows "Plan Value" and "Margin" columns computed via PriceCalculator. Total plan valuation and margin shown in the summary labels. Supports commodity pricing via ConstructionResources and resource pricing via direct lookup.


### BL-079: Build Planner Target Duration Field
**Status: Complete** — Added txtTargetDuration (ValidatedTextBox, default "2d 0h 0m 0s") to FormBuildPlanner add-item panel row 2. Queue Calc button now reads from this field instead of showing a modal input dialog. Parses countdown format via CountdownFormatParser, computes runs via QueueCalculator, populates quantity field.


### BL-080: Overflow and Asteroid Filter TextBoxes
**Status: Complete** — Added 4 filter textboxes that pair with combo boxes for type-ahead filtering: txtOverflowResourceFilter, txtOverflowDestFilter, txtOverflowRouteFilter on FormColonyV2 Overflow tab, and txtReserveResourceFilter on FormAsteroid. Each TextChanged event repopulates its paired combo with items matching the filter substring (case-insensitive).


### BL-081: Stock Targets Quick Add and Expanded Components
**Status: Complete** — Added cmdQuickAdd button to FormStockTargets that adds all resources as targets (1000 qty, EmpireWide scope, skipping duplicates). Added dgvExpandedComponents grid that shows the hull and component breakdown when a ShipTemplate target is selected in the targets grid. Selection change on dgvTargets triggers the expanded view for template targets, hidden for other types.


### BL-080 (Audit): Duplicate — PopulateRouteDropdown (31 lines)
Extracted to `RouteDropdownHelper.Populate()` in `OE2EmpireTracker/Controls/RouteDropdownHelper.cs`. Both FormColonyDailyBuild and FormDeliveryExecution now call the shared helper. Dead `DropdownItem` class removed from FormColonyDailyBuild.
**Status: Complete**

### BL-081 (Audit): Duplicate — PopulateHullCombo / SelectHullInCombo
Intentionally copied per the ship-form-overhaul spec. Both forms need identical hull combo logic but operate on different selected objects. Added to KNOWN_DUPES in dupe-code.js.
**Status: Accepted — by design**

### BL-084 (Audit): Duplicate — GetRefiningOutputRate (7 lines)
Extracted to `GameConstants.GetRefiningOutputRate()`. Removed three private copies from ColonyActivityCollector, ColonyAdminReportBuilder, and ColonyStructureV2. The ColonyStructureV2 copy also had magic numbers instead of PurityMultiplier constants.
**Status: Complete**


### BL-075 (Audit): Dead Code — FormColonyV2.AcquireStructureControl
Removed unused private method. Leftover from structure pool refactor.
**Status: Complete**

### BL-076 (Audit): Dead Code — FormColonyV2.ReturnAllToPool
Removed unused private method. Superseded by different pool management approach.
**Status: Complete**

### BL-077 (Audit): Dead Code — FormDeliveryExecution.GetLoadItemVolume
Removed unused private method. Volume calculation delegated to CargoVolumeService elsewhere.
**Status: Complete**

### BL-078 (Audit): Dead Code — FormBuildPlanner.ShowInputDialog
Removed unused private method. Replaced by txtTargetDuration inline field (BL-079).
**Status: Complete**

### BL-079 (Audit): Dead Code — FormPlayerProfile.chkColonyOperations_Click
Removed unwired event handler. Checkbox wiring was removed but handler left behind.
**Status: Complete**


### BL-082 (Audit): Duplicate — OnCurrentPlayerChanged (10 lines)
Identical player-changed handlers across FormBuildPlanner and FormPricingPlan. Accepted as baseline — small boilerplate methods that follow the standard form pattern.
**Status: Accepted — by design**

### BL-083 (Audit): Duplicate — flpSearchList_Layout (5 lines)
Identical layout handlers across FormBuildPlanner and FormPricingPlan. Accepted as baseline — trivial 5-line methods, not worth abstracting.
**Status: Accepted — by design**


### BL-072: BuildOrderOptimizer — Incremental Simulation
Refactored BuildOrderOptimizer.Optimize() to replace O(n²) SimulateAll calls with O(n) incremental delta tracking using a running ColonyStructureStatus accumulator. SimulateAll re-simulated the entire result list from scratch on every status query (O(n) per call × O(n) calls = O(n²)). The refactored version maintains a single accumulator updated via SimulateOneMore each time a structure is appended, reducing overall complexity to O(n). FixDeficits and PlaceSupportSafe updated to accept `ref ColonyStructureStatus accumulator` and update it directly. Look-ahead projections remain stateless — they use SimulateOneMore against the accumulator without modifying it. SimulateAll method deleted entirely. Pure internal refactor — public API unchanged, all 17 existing pinned tests produce identical output. 4 FsCheck property tests (incremental accumulation equivalence, SimulateOneMore purity, optimizer output equivalence, linear call count). Spec: `.kiro/specs/optimizer-incremental-simulation/`.
**Status: Complete**

### BL-111: FormPlayerProfile — Immutable Data Model with Service Layer
Migrated FormPlayerProfile to the immutable data model pattern established in BL-108. The form no longer directly mutates PlayerProfile entities. The ViewModel is a disconnected edit buffer with dirty tracking. A new PlayerProfileService is the sole mutator (Update, Create, Delete, Import). Added SuppressUI guards to prevent MessageBox dialogs during tests. Added unsaved changes prompts on selection change, form close, and New button.
**Status: Complete** — Spec: .kiro/specs/bl-111-playerprofile-readonly/

### BL-123: FormPricingPlan — Immutable Data Model with Service Layer
Applied the immutable data model pattern (from BL-108/BL-111) to FormPricingPlan. Created PricingPlanViewModel as disconnected edit buffer, PricingPlanService as sole mutator, DTO request objects (PricingPlanUpdateRequest, PricingPlanCreateRequest). Migrated FormPricingPlan to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete. Added unsaved changes prompts (selection change, New, form close, app exit). Added 24 new tests: 3 ViewModel property tests, 8 ViewModel unit tests, 3 service property tests, 8 service unit tests, 2 mutation guard tests. See .kiro/specs/bl-123-pricingplan-readonly/.
**Status: Complete**

### BL-110: FormSurvey — Immutable Data Model with Service Layer
Applied the immutable data model pattern (from BL-108/BL-111/BL-123) to FormSurvey. Created SurveyViewModel as disconnected edit buffer, SurveyService as sole mutator with CRUD and Import methods, DTO request objects (SurveyUpdateRequest, SurveyCreateRequest). Migrated FormSurvey to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete/Import. Added unsaved changes prompts (selection change, New, Import, form close). Import routes through service with dedup and asteroid auto-linking. Delete checks SurveyReferenceCounter for reference protection. 37 new tests including property-based tests, unit tests, and mutation guard tests. Fixed missing AsteroidUUID comparison in IsDirty (found by property test). See .kiro/specs/bl-110-survey-readonly/ for full spec.
**Status: Complete**

### BL-108: FormBlueprintV2 — Immutable Data Model, Mutation Through Service Only
Full immutable data model for the blueprint form. ViewModel becomes a disconnected edit buffer (no write-through). All mutation goes through BlueprintService (Update, Create, Delete, Import, MoveToGlobal/Player). ReadOnly wrappers for all read-only paths. Unsaved changes prompts on selection change, form close, and new. See .kiro/specs/bl-108-blueprint-readonly/ for full spec.
**Status: Complete**

### BL-131: FormBlueprintV2 — Statistics Grid Add/Delete and Context Menus
Added Add/Delete buttons for the statistics grid (dgvStatistics) matching the resources grid pattern. Delete disabled for BlueprintType-defined properties — only user-added extras can be deleted. Added right-click context menus (cmsStatistics, cmsResources) to both statistics and resources grids with Add Row and Delete Row items.
**Status: Complete**

### BL-132: FormBlueprintV2 — Switch Type and Pricing Plan Combos to FilteredTextComboSet
Switched cmbFilterType, cmbBlueprintType, and cmbPricingPlan from standard ComboBox to FilteredTextComboSet. Updated initialization to use SetItems(), event handlers to SelectedItemChanged, and value reads to SelectedFullIndex/SelectedItem. Added _pricingPlanList parallel UUID list for pricing plan lookup.
**Status: Complete**

### BL-109: FormColonyV2 — Immutable Data Model with Service Layer
Applied the immutable data model pattern (from BL-108/BL-111/BL-123/BL-110) to FormColonyV2. ColonyViewModel rewritten as disconnected edit buffer for scalar fields (PlanetName, ColonyName, SystemName). ColonyService created as sole mutator with CRUD, import, structure, item, and commodity operations — all with ReaderWriterLockSlim concurrency. DTO request objects (ColonyUpdateRequest, ColonyCreateRequest). FormColonyV2 migrated to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete/Import. Structure/item/commodity operations are immediate service calls bypassing the edit buffer. Added unsaved changes prompts (selection change, New, Import, form close, app exit). Property-based tests, unit tests, and mutation guard tests added. See .kiro/specs/bl-109-colony-readonly/.
**Status: Complete**

### BL-112: FormDeliveryRoute — Immutable Data Model with Service Layer
Applied the immutable data model pattern (from BL-108/BL-109/BL-110/BL-111/BL-123) to FormDeliveryRoute. DeliveryRouteViewModel rewritten as disconnected edit buffer for Name and Stops list. DeliveryRouteService created as sole mutator with Update/Create/Delete methods. DTO request objects (DeliveryRouteUpdateRequest, DeliveryRouteCreateRequest). FormDeliveryRoute migrated to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete. All stop operations (add, remove, reorder) accumulate in the ViewModel edit buffer until Save. Added unsaved changes prompts (selection change, New, form close, app exit). Added FindMutableDeliveryRoute to PlayerContext. 28 new tests including 7 property-based tests, 16 unit tests, and 5 mutation guard tests. See .kiro/specs/bl-112-deliveryroute-readonly/.
**Status: Complete**

### BL-113: DeliveryPlan — Immutable Data Model with Service Layer
Applied the immutable data model pattern to DeliveryPlan. DeliveryPlanService created as sole mutator with CRUD, item operations (AddDropOffItem, AddPickUpItem, RemoveDropOffItems, RemovePickUpItems), execution operations (MarkItemDelivered, MarkStopComplete, MarkPlanComplete, SetShipUUID), and trip splitting. DeliveryPlanViewModel rewritten as disconnected edit buffer. FormDeliveryRoute Plan tab and FormDeliveryExecution migrated to route all mutations through the service. DTO request objects (DeliveryPlanUpdateRequest, StopDestinationInfo, DeliveryItemInfo). Removed unused DeliveryPlanCreateRequest. Added FindMutableDeliveryPlan to PlayerContext. 34 new tests including 8 property-based tests, 20 unit tests, and 6 mutation guard tests. See .kiro/specs/bl-113-deliveryplan-readonly/.
**Status: Complete**
### BL-114: Market — Immutable Data Model with Service Layer
Applied the immutable data model pattern to MarketListing and MarketTransaction. Created MarketListingService as sole mutator with CreateListing, UpdateListing, DeleteListing, and RecordSale methods. Migrated FormMarket to use ReadOnly wrappers in grid Tags and route all mutations through the service. Migrated FormListingEdit to accept ReadOnlyMarketListing and expose edited values as properties. Migrated FormRecordSale to accept ReadOnlyMarketListing. Added FindMutableMarketListing to PlayerContext. 21 new tests including 4 property-based tests, 13 unit tests, and 4 mutation guard tests. See .kiro/specs/bl-114-market-readonly/.
**Status: Complete**
### BL-115: ShipTemplate — Immutable Data Model with Service Layer
Applied the immutable data model pattern to ShipTemplate. Created ShipTemplateViewModel as disconnected edit buffer with IsDirty tracking for Name, HullBlueprintUUID, and Components. Created ShipTemplateService with Create/Update/Delete methods. Migrated FormShipTemplate to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete. Added unsaved changes prompts. 33 new tests. See .kiro/specs/bl-115-shiptemplate-readonly/.
**Status: Complete**
### BL-116: Ship — Immutable Data Model with Service Layer
Applied the immutable data model pattern to Ship (FormShipInstance). Created ShipViewModel as disconnected edit buffer with IsDirty tracking for all ship fields including Components, Cargo, and Hopper. Created ShipService with Create/Update/Delete/CreateFromTemplate methods. Migrated FormShipInstance to use ReadOnly wrappers in list view, ViewModel for all edits, and service for Save/Delete. Added unsaved changes prompts. 32 new tests. See .kiro/specs/bl-116-ship-readonly/.
**Status: Complete**### BL-124: Read-Only Display Forms ? Verification Audit
Audited FormColonyActivity, FormColonyDailyBuild, and FormAutoFill for mutable entity references. All three forms are clean: FormColonyActivity passes colonies to collectors without mutation, FormColonyDailyBuild has legitimate mutable access for its Build button, and FormAutoFill only accesses PreferencesStore. Added 3 verification tests confirming no persistent mutable entity fields in display forms. See .kiro/specs/bl-124-readonly-display-forms/.
**Status: Complete**

### BL-125: Final Mutation Audit — Verify No Direct Entity Mutation Outside Services
Capstone validation of the immutable data model. Created ComprehensiveMutationAuditTests with 4 tests: AllEntityTypes_HaveMutationGuardCoverage (verifies all 16 entity types have guard tests), WriteContext_OnlyCalledFromServices (verifies WriteContext() only in allowed files), NoFormDirectlyMutatesEntities, and NoViewModelDirectlyMutatesEntities. All 2466 tests pass. See .kiro/specs/bl-125-final-mutation-audit/.
**Status: Complete**

### BL-117: Station — Immutable Data Model with Service Layer
Created StationViewModel, StationService, migrated FormStation. See .kiro/specs/bl-117-station-readonly/.
**Status: Complete**

### BL-118: BuildPlan — Immutable Data Model with Service Layer
Created BuildPlanViewModel, BuildPlanMutationService, migrated FormBuildPlanner. See .kiro/specs/bl-118-buildplan-readonly/.
**Status: Complete**

### BL-119: StockTargets — Immutable Data Model with Service Layer
Created StockTargetViewModel, StockTargetMutationService, migrated FormStockTargets. See .kiro/specs/bl-119-stocktargets-readonly/.
**Status: Complete**

### BL-120: SupplyChain — Immutable Data Model with Service Layer
Created SupplyChainViewModel, SupplyChainMutationService, migrated FormSupplyChain. See .kiro/specs/bl-120-supplychain-readonly/.
**Status: Complete**

### BL-121: Contacts — Immutable Data Model with Service Layer
Created ContactsViewModel, ContactsService, migrated FormContacts. See .kiro/specs/bl-121-contacts-readonly/.
**Status: Complete**

### BL-122: Asteroid — Immutable Data Model with Service Layer
Created AsteroidViewModel, AsteroidService, migrated FormAsteroid. See .kiro/specs/bl-122-asteroid-readonly/.
**Status: Complete**

### BL-124: Read-Only Display Forms
Audited FormColonyActivity, FormColonyDailyBuild, FormAutoFill for mutable entity references. Added verification tests. See .kiro/specs/bl-124-readonly-display-forms/.
**Status: Complete**

### BL-125: Final Mutation Audit
Created ComprehensiveMutationAuditTests verifying all entity types have mutation guard coverage, WriteContext only in services, no form/ViewModel direct mutation. See .kiro/specs/bl-125-final-mutation-audit/.
**Status: Complete**
