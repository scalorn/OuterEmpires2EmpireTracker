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
