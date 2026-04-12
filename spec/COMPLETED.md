# Completed Backlog Items

Items moved here from BACKLOG.md after implementation, plus completed specs that were implemented outside the backlog.

---

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
