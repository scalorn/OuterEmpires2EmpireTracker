# Feature Backlog

**Next available ID: BL-130** (check COMPLETED.md before assigning — IDs are shared across both files)

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Systems & Planets Model ────── (standalone)
Systems & Planets Model ───── Route Auto-Sequencing (depends on coordinates)
```

Suggested build order:
1. Systems & Planets Model (independent)
2. Route Auto-Sequencing (depends on Systems & Planets Model)

---

## Open (Existing)

### BL-005: Global Blueprint Enhancements
From Recommendations.md #14:
- Visual indicator in blueprint list to distinguish global vs player-specific
- Permission/setting to gate global blueprint editing
- Separate file for global blueprints if BaselineData.json grows too large

### BL-006: Player Transfer UI
From Recommendations.md #13: UI for transferring colonies/blueprints/surveys between player profiles.

## New

### BL-019: Systems & Planets Model
**Dependencies:** None (but enables Route Auto-Sequencing)

Model star systems and planets more completely with coordinate data. Planet and jump point coordinates are available in the game UI. System-level coordinates may not be directly visible but could potentially be parsed from game JSON data. Once modeled, this enables travel time approximation and automatic delivery route sequencing.

### BL-020: Route Auto-Sequencing
**Dependencies:** Systems & Planets Model (BL-019)

With coordinate data for systems and planets, automatically sequence delivery route stops to minimize travel time. Approximate travel distances from coordinates and optimize stop order.

### BL-023: Explore OE2 Wiki as Data Source
**Dependencies:** None

Explore whether AI can read the OE2 wiki at https://atlasgamingcorp.com/outer-empires-2/ and pull useful game information from it. Could be a good source of data to enrich the spec and tool — game mechanics, structure types, resource details, etc.

### BL-025: Read Game Status Updates
**Dependencies:** None

Explore whether AI can read the game status/dev updates at https://game.dev.outerempires.net/ to stay current on game changes, new features, and balance adjustments that might affect the tracker.

### BL-026: Read outerempires.net for Game Information
**Dependencies:** None

Explore whether AI can read https://outerempires.net/ to pull in additional game information — lore, mechanics, community resources, etc.

### BL-027: Read Discord Channels for Game Info
**Dependencies:** None

Explore whether AI can read the Discord channels for the game. Discord is often where the latest game info, patch notes, and community knowledge lives. Would need to investigate API access or bot integration.

### BL-028: Organize Completed Specs into Subdirectories
**Dependencies:** None
**Status: Deprioritized** — Kiro's spec tooling references specs by path under `.kiro/specs/{feature_name}/`. Moving into subdirectories (`completed/`, `in-progress/`) would change paths and likely break `.config.kiro` references, task status tracking, and subagent delegation. Currently 16 spec folders — manageable. Revisit if count exceeds 30+ or Kiro adds explicit subdirectory support. The backlog already tracks completion status; no need to duplicate that via folder structure.

### BL-029: BaselineData.json — Separate User File with Merge Strategy
**Dependencies:** None
**Status: Rejected** — The split-file approach adds merge-on-load complexity, "which file wins" ambiguity, and user confusion. With deterministic UUIDs + versioned migrations + idempotent renames (see `spec/discussions/baseline-data-stability.md`), a single BaselineData.json handles upgrades cleanly without a separate user file.

### BL-033: RtfBuilder Font Style Support
**Dependencies:** None
**Status: Deprioritized** — Current consumers (ColonyStatusCalculator, ColonyStructure control, ColonyAdminReportBuilder) use color effectively to distinguish headers, values, and status. No current feature is limited by the lack of font styles.

Add support for font styles to RtfBuilder — bold, italics, strikethrough, etc.

**Implementation notes:** Add an overload `Append(string text, Color color, FontStyle style = FontStyle.Regular)` using `System.Drawing.FontStyle` flags. Wrap text in RTF control words (`\b...\b0`, `\i...\i0`, `\strike...\strike0`). Backward compatible via default parameter. ~30 minutes of work when a specific need arises.

### BL-036: Colony Mining/Refining Production Graph
**Dependencies:** None

Add a chart to the Colony form showing mining and refining production over time. Use `System.Windows.Forms.DataVisualization.Charting`. Visualize resource output rates from mining rigs and refineries, helping players see production throughput at a glance and identify bottlenecks or underperforming structures.

### BL-070: Blueprint Evolution — Capped Property Ranges
**Dependencies:** None

Some blueprint properties evolve with +/- 50% of the base value, but others are capped to a range of 0–100 (e.g. Max Repair — you wouldn't want more than 100% max repair). The evolution graph and any future evolution prediction need to account for both modes. Per the game developer: "sometimes it's +/- 50%, sometimes it's the +/- 50% of game to 100." Need to identify which properties use which mode and adjust the evolution graph normalization accordingly.

### BL-071: TreeDataGridView — Tree-in-Grid Custom Control for Inventory
**Dependencies:** None
**Status: Under consideration** — may or may not implement depending on how the master-detail crate UI pattern works out in practice.

Build a custom `TreeDataGridView` control extending DataGridView that supports expandable/collapsible parent-child rows. Primary use case: displaying crate contents inline in inventory grids instead of using a separate master-detail panel (which eats vertical space).

**Discussion:** The master-detail pattern (select a crate, see contents in a grid below) works but doubles the vertical space needed. A tree-in-grid keeps everything in one grid with expand/collapse glyphs on crate rows. WinForms DataGridView is fundamentally flat, so this requires manual bookkeeping: child rows are hidden/shown on expand/collapse, indented via cell padding, and grouped under their parent during sort/filter.

**Complexity:** ~200-300 lines for one-level deep (current crate model — no nesting). Crate rows get ▶/▼ glyph via CellPainting, child rows track their parent via tag/index, expand/collapse toggles Visible on children. Sorting must keep children grouped under parents. Filtering a parent hides its children.

**Future-proofing for nested crates:** If crates ever support nesting, use a `Depth` integer per row instead of a boolean IsChild flag. Indentation = depth × indent pixels. Expand/collapse hides all descendants recursively. Adds ~50 lines over the flat version. The game doesn't support nested crates today but the dev hasn't ruled it out.

**Decision point:** Try master-detail first during Iteration 4 (Stations). If vertical space is a problem in practice, build this control as a replacement. Design the control with depth support from the start so nesting is incremental if it comes.

###  : Blueprint Cost Evolution Graph
**Dependencies:** None

Add a cost evolution graph to the Blueprint form, similar to the existing evolution graph but plotting the estimated cost of each blueprint at each evolution level. Cost is computed using a selected pricing plan — sum of (resource quantity × resource price) for each resource in the blueprint, plus any time-based costs from the plan. The graph shows how total manufacturing cost changes as the blueprint evolves, helping players decide which evolution to manufacture based on cost efficiency. Requires a pricing plan selector dropdown on the graph panel. Reuse the charting infrastructure from the evolution graph (System.Windows.Forms.DataVisualization.Charting).


---
### BL-045: GitHub MCP Integration
**Dependencies:** None (blocked by Docker installation issues)

Set up the GitHub MCP server so Kiro can read/write GitHub issues, PRs, and wiki pages directly. Enables intake of external issues, PR review, and maintaining user documentation in the repo wiki. Requires Docker Desktop (or the standalone Go binary from github/github-mcp-server releases) plus a fine-grained GitHub Personal Access Token scoped to the repo with Issues, Pull Requests, Contents, and Metadata permissions. Configuration goes in `.kiro/settings/mcp.json`. Currently blocked — Docker won't install on the dev machine. Revisit when Docker is available or try the standalone binary approach.

### BL-047: Game API Integration Planning
**Dependencies:** None

There will be a game API eventually. Plan the integration architecture now so we're ready when it arrives. Consider: authentication, polling vs push, data model mapping, how it replaces clipboard import, and what new capabilities it enables (real-time sync, automated queue management, etc.).

**Secret storage:** Use Windows DPAPI (`System.Security.Cryptography.ProtectedData`) to encrypt API keys at rest. Store the encrypted blob in `%LOCALAPPDATA%\OE2EmpireTracker\secrets.dat`. The application API key is stored once (shared across all characters). Each character's API key is stored keyed by player UUID. Structure: `Dictionary<string, string>` serialized to JSON then encrypted with `DataProtectionScope.CurrentUser`. The encrypted file is useless on any other machine or user account. Add `secrets.dat` to `.gitignore` as a safety net.

**Rate limiting:** Polly 7.2.4 is already installed in the project. Use Polly's rate limiter, retry with exponential backoff (for 429 responses), and circuit breaker policies to handle API throttling.

### BL-050: Default Survey Duplicate on Out-of-Order Import
**Dependencies:** None

Bug: When colonies and surveys are imported in the wrong order, duplicate default surveys can still be created (2 surveys for the same planet). Reimporting the colony removes one but not both. The cleanup logic in CleanupDefaultSurvey should handle all duplicates in a single pass, and CreateOrUpdateDefaultSurvey should be more aggressive about finding and consolidating existing defaults.

**Diagnostic logging added:** SetupMiners now logs a snapshot of all DEFAULT surveys for the colony's planet before and after processing (count + UUIDs). CreateOrUpdateDefaultSurvey logs the deterministic UUID it's searching for and whether a match was found. CleanupDefaultSurvey logs all DEFAULT surveys for the planet (regardless of owner) before cleanup. A `BL-050 DIAGNOSTIC` warning fires if multiple DEFAULT surveys remain after SetupMiners completes. Check the NLog output after importing colonies to reproduce.

---

## ~~MarketSample Coverage Gaps~~ — RESOLVED

All BlueprintTypes now have HTML coverage. No gaps detected (confirmed by IconPositionExtractor coverage gap report in test output).

---


### BL-073: Code Coverage Tooling
**Dependencies:** None
**Status: Blocked**  AltCover (both global tool and NuGet package) fails with .NET Framework 4.8.1 + NUnit + vstest.console. The global tool crashes with a CLR assertion (net8.0 runtime vs net4.8.1 assemblies). The NuGet package instruments successfully but the NUnit test adapter can't discover tests in the instrumented assemblies. OpenCover is unmaintained (last release 2021). VS Community doesn't include the Enterprise code coverage collector.

### BL-074: Immutable Data Model — Mutation Through Interface Only
**Dependencies:** BL-069 (done), readonly-list-encapsulation spec (in progress)
**Status: New**

Before we can move to a database or SOA we need to protect the data model from in-memory editing and make all mutation go through an interface. Currently entity POCOs (Blueprint, Colony, Survey, etc.) have public setters on all properties — any code can mutate any field at any time without going through a controlled path. This makes it impossible to track dirty state, emit change events, or swap the persistence layer.

Phase 1 (readonly-list-encapsulation) protects the *collections* — you can't add/remove entities without going through PlayerContext. Phase 2 (this item) protects the *entities themselves* — you can't mutate a Blueprint's Name or a Colony's OwnerUUID without going through a controlled update path. This likely means read-only public properties with internal/private setters, plus Update methods or a unit-of-work pattern that tracks changes and persists them atomically.

Revisit when the project migrates to .NET 8+ where `dotnet test --collect:"XPlat Code Coverage"` works natively. In the meantime, use the file-level coverage analysis tool (`node .kiro/tools/spec-coverage.js`) and the reference counter completeness tests as proxies for coverage.

## Unused Model Fields

Fields on model classes that exist in the data model (persisted in JSON) but are never read by production code. Discovered during the April 2026 model field audit. Each item is a candidate for either wiring into production logic or removing.

### BL-086: ColonyStructure.CurrentAttitude — Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** — Known incomplete feature (AMB-004/REQ-COL-001b). Deserialized from PlayerData.json but no production code reads the value. Will be needed when worker morale is implemented. Leave as-is until morale feature is built.

### BL-087: ColonyStructure.ContentmentIndex — Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** — Same incomplete feature group as CurrentAttitude (AMB-004). Deserialized from JSON, never consumed by production code. Leave as-is until morale feature is built.

### BL-088: ColonyStructure.WageLevel — Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** — Same incomplete feature group as CurrentAttitude (AMB-004). Deserialized from JSON, never consumed by production code. Leave as-is until morale feature is built.

### BL-089: BlueprintType.ResearchableProperties — Not Wired to Production Code
**Dependencies:** None
**Status: New**
Deserialized from BaselineData.json but no production code ever reads the value after loading. Potential future feature for research lab UI (showing which properties can be researched on a blueprint type). Either wire it into the Research Lab UI or remove it from the model.

### BL-090: BuildItem.ParentBuildItemUUID — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on BuildItem but never set by any code and never read. Appears to be a planned feature for hierarchical build item dependencies (e.g. "build this after that") that was never implemented. Remove or implement.

### BL-092: ShipStats.CrewSupported — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set by `ShipBuildService.AddBlueprintStats()` and never displayed in any form. Always zero. Remove or wire into ship stat computation.

### BL-093: ShipStats.EngCapacityAvailable — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Remove or wire into ship stat computation.

### BL-094: ShipStats.EngCapacityUsed — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Same field exists on StationStats (BL-099) — also unused.

### BL-095: ShipStats.MiningYieldIncrease — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set by `AddBlueprintStats()` and never displayed. Note: `MiningYield` IS used — `MiningYieldIncrease` is a separate unused field. Remove or wire into ship stat computation.

### BL-096: ShipStats.SensorAbundanceFactor — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Remove or wire into ship stat computation.

### BL-097: ShipStats.PurityModifier — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Note: `SurveyViewModel.PurityModifier` IS used — that's a different property on a different class. Remove or wire into ship stat computation.

### BL-098: ShipStats.SlotSummary — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always empty string. Same field exists on StationStats (BL-100) — also unused. Remove or wire into ship stat display.

### BL-099: StationStats.EngCapacityUsed — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on StationStats but never set or read. Always zero. Same pattern as ShipStats.EngCapacityUsed (BL-094). Remove or wire into station stat computation.

### BL-100: StationStats.SlotSummary — Never Set or Read
**Dependencies:** None
**Status: New**
Declared on StationStats but never set or read. Always empty string. Same pattern as ShipStats.SlotSummary (BL-098). Remove or wire into station stat display.

---

## Empire-Systems Audit Gaps

The following items were designed in the empire-systems spec and marked complete in tasks.md, but the UI controls were either not implemented or implemented differently than the mockup specified. Discovered during the April 2026 mockup-controls audit.

## FilteredTextComboSet — Future Adoption Candidates

Controls identified during the custom-control-adoption spec audit that don't have a paired filter TextBox today but would benefit from inline filtering. Each requires either adding a FilteredTextComboSet where no filter exists, or enhancing FilteredTextComboSet to support object binding (BindingSource with DisplayMember/ValueMember).

### BL-102: FormStation — cmbStationBlueprint FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbStationBlueprint` in FormStation is a standard ComboBox populated with a large blueprint list (50+ items) via BindingSource with DisplayMember/ValueMember. No paired filter TextBox exists today. Users must scroll through the full list to find a station blueprint. Adopting FilteredTextComboSet here requires either (a) converting from BindingSource to plain string list with a parallel UUID list, or (b) enhancing FilteredTextComboSet to support object binding natively. Option (b) would also benefit BL-103 and BL-104.

### BL-103: FormStation — cmbHoldItem and cmbMunItem FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbHoldItem` and `cmbMunItem` in FormStation are standard ComboBoxes populated with item lists that can be large (50+ resources, commodities, blueprints depending on the selected type). No paired filter TextBox exists today. Adding FilteredTextComboSet would improve item selection UX, especially for the Resource item type which has 50+ entries. These use a type-cascade pattern (cmbHoldType/cmbMunItem controls which items appear) — the FilteredTextComboSet replaces only the item combo, not the type selector.

### BL-104: FormShipInstance — cmbAddItem FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbAddItem` in FormShipInstance is a standard ComboBox populated with item lists that can be large (50+ items depending on the selected type). No paired filter TextBox exists today. Same type-cascade pattern as FormStation hold items (cmbAddType controls which items appear). Adding FilteredTextComboSet would improve item selection UX. Same BindingSource consideration as BL-102.

### BL-105: FormSupplyChain — cmbLocation, cmbResource, cmbRoute FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
Three ComboBoxes in FormSupplyChain's stage editor are populated dynamically but have no paired filter TextBox: `cmbLocation` (colonies/stations/asteroids/ships — can be 10-50 items), `cmbResource` (50+ resources), and `cmbRoute` (5-20 routes). These use a type-cascade pattern (cmbLocationType/cmbStageType controls which items appear). Adding FilteredTextComboSet to cmbResource would be most valuable (large list). cmbLocation and cmbRoute are smaller but would benefit from consistent UX.

### BL-106: FormBuildPlanner — FilteredTextComboSet Adoption for Item Pickers
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
FormBuildPlanner has multiple ComboBoxes for selecting blueprints, resources, commodities, routes, plans, and surveys. None have paired filter TextBoxes — the form uses cascading type selectors instead. The blueprint and resource pickers have large item lists (50-100+ items) that would benefit from inline filtering. This is a larger conversion since the form has no existing filter pattern to replace — FilteredTextComboSet controls would need to be added fresh rather than replacing existing TextBox+ComboBox pairs.

### BL-107: Right-Click Context Menus for DataGridView Grids
**Dependencies:** None
**Status: New**
Add right-click context menus to DataGridView grids across all forms. Currently no form uses right-click context menus on grids — all actions are via buttons in command bars. Context menus provide faster access to common actions without moving the mouse to the button bar. The build-plan-execution spec introduces the first grid context menu (`cmsBuildItems` on FormBuildPlanner). This backlog item extends the pattern to other forms:
- **FormColonyV2**: dgvItems (Warehousing tab) — Add/Remove items, dgvCommodityRequests (Workers tab) — Add/Remove requests, dgvOverflowRules (Overflow tab) — Add/Remove rules
- **FormDeliveryRoute**: dgvStops — Add/Up/Down/Remove stops, dgvDropOff/dgvPickUp — Add/Remove items
- **FormDeliveryExecution**: load list and stop items — Mark delivered/undelivered
- **FormSurvey**: dgvResources — Add/Remove resources
- **FormBlueprintV2**: dgvResources — Add/Remove resources, dgvStatistics — Edit properties
- **FormStockTargets**: dgvTargets — Add/Remove targets
- **FormMarket**: dgvListings — Record sale, dgvTransactions — view details
- **FormShipTemplate/FormShipInstance**: dgvSlots/dgvComponents — Add/Remove
- **FormStation**: dgvHold/dgvComponents — Add/Remove
Each context menu should mirror the existing button actions for that grid, providing the same functionality via right-click.


## Read-Only Data Wrapper Migration — Per-Form Backlog Items

These items migrate individual forms from consuming mutable entity references to using ReadOnly wrapper types for their read-only data paths (combo box population, list view display, reference counting, status display). Each form retains mutable access for its ViewModel/edit paths. Depends on the readonly-data-wrappers spec (complete).

### BL-108: FormBlueprintV2 — Immutable Data Model, Mutation Through Service Only
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Full immutable data model for the blueprint form. ViewModel becomes a disconnected edit buffer (no write-through). All mutation goes through BlueprintService (Update, Create, Delete, Import, MoveToGlobal/Player). ReadOnly wrappers for all read-only paths. Unsaved changes prompts on selection change, form close, and new. See `.kiro/specs/bl-108-blueprint-readonly/` for full spec.

### BL-131: FormBlueprintV2 — Statistics Grid Add/Delete and Context Menus
**Dependencies:** None
**Status: New**
Add explicit Add/Delete buttons for the statistics grid (dgvStatistics), matching the existing Add/Delete on the resources grid. Delete SHALL be disabled for properties that are part of the BlueprintType's defined property list — only user-added extra properties can be deleted. Add right-click context menus to both the statistics grid and resources grid with Add Row and Delete Row items. Delete Row in the context menu follows the same rules: disabled for type-defined statistics properties, always enabled for resources.

### BL-132: FormBlueprintV2 — Switch Type and Pricing Plan Combos to FilteredTextComboSet
**Dependencies:** None
**Status: New**
The blueprint type combo (cmbBlueprintType) has grown long enough to need filtering. Switch it to FilteredTextComboSet on both the filter panel (cmbFilterType) and the edit panel (cmbBlueprintType). Also switch the pricing plan combo (cmbPricingPlan) to FilteredTextComboSet to future-proof it as more plans are added. Follow the same pattern used by cmbItem on the build planner and cmbHull on the ship forms — SetItems with a parallel ID list, SelectedFullIndex for lookup.

### BL-109: FormColonyV2 — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormColonyV2 to use ReadOnly wrappers. This form has heavy read-only consumption: colony list view with ColonyReferenceCounter, FilteredTextComboSet combos for flatpacks/items/resources/overflow destinations/routes, survey combos in ColonyStructureV2, and status display via ColonyStatusCalculator. The ColonyViewModel continues to use mutable Colony for editing. Switch list population, combo population, reference counter inputs, and status calculator inputs to read-only types.

### BL-110: FormSurvey — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormSurvey to use ReadOnly wrappers. This form uses SurveyReferenceCounter for delete-guard logic, FilteredTextComboSet for scanner blueprint selection, and colony list for reference counting. The SurveyViewModel continues to use mutable Survey for editing. Switch list population to GetReadOnlySurveyList, reference counter inputs to read-only lists, and combo population to ReadOnly types.

### BL-111: FormPlayerProfile — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormPlayerProfile to use ReadOnly wrappers. This form displays player profiles in a list view and populates faction combos. The PlayerProfileViewModel continues to use mutable PlayerProfile for editing. Switch list population to GetReadOnlyPlayerProfileList and faction combo to ReadOnly types.

### BL-112: FormDeliveryRoute — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormDeliveryRoute to use ReadOnly wrappers. This form has extensive read-only consumption: route list view with DeliveryRouteReferenceCounter, FilteredTextComboSet combos for colony/item selection (drop-off and pick-up), and RouteDropdownHelper for destination combos. The DeliveryRouteViewModel continues to use mutable DeliveryRoute for editing. Switch list population, reference counter inputs, combo population, and RouteDropdownHelper inputs to read-only types.

### BL-113: FormDeliveryExecution — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormDeliveryExecution to use ReadOnly wrappers. This form populates route combos via RouteDropdownHelper, displays delivery plan stops and items in read-only grids, and shows colony/station names for stop destinations. Switch route combo population, plan list, and destination name lookups to read-only types.

### BL-114: FormAutoFill — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormAutoFill to use ReadOnly wrappers. This dialog reads colony inventory and commodity data to auto-fill delivery plan items. Switch colony and commodity lookups to read-only types.

### BL-115: FormMarket — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormMarket to use ReadOnly wrappers. This form displays market listings in a grid with MarketListingReferenceCounter, shows transaction history, and populates station combos. Switch listing/transaction list population, reference counter inputs, and station combo to read-only types.

### BL-116: FormListingEdit — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormListingEdit to use ReadOnly wrappers. This dialog populates station and item type combos for creating/editing market listings. Switch combo population to read-only types.

### BL-117: FormRecordSale — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormRecordSale to use ReadOnly wrappers. This dialog displays listing details and records sale transactions. Switch listing display to read-only types.

### BL-118: FormShipTemplate — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormShipTemplate to use ReadOnly wrappers. This form displays ship templates in a list view with ShipTemplateReferenceCounter, and uses FilteredTextComboSet for hull blueprint selection. Switch list population, reference counter inputs, and hull combo to read-only types.

### BL-119: FormShipInstance — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormShipInstance to use ReadOnly wrappers. This form displays ships in a list view with ShipReferenceCounter, uses FilteredTextComboSet for hull blueprint selection, and populates location/template combos. Switch list population, reference counter inputs, and combo population to read-only types.

### BL-120: FormStation — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormStation to use ReadOnly wrappers. This form displays stations in a list view with StationReferenceCounter, populates blueprint/item combos, and displays hold inventory and component grids. Switch list population, reference counter inputs, combo population, and inventory display to read-only types.

### BL-121: FormBuildPlanner — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormBuildPlanner to use ReadOnly wrappers. This form has extensive read-only consumption: build plan list, build item grid, blueprint/colony/station/route/survey combos, and shortfall computation. Switch list population, combo population, and shortfall calculator inputs to read-only types.

### BL-122: FormStructureAllocation — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormStructureAllocation to use ReadOnly wrappers. This dialog displays colony structures and their allocation status. Switch structure list and blueprint lookups to read-only types.

### BL-123: FormPricingPlan — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormPricingPlan to use ReadOnly wrappers. This form displays pricing plans in a list view and populates resource combos from EmpireContext. Switch list population and resource combo to read-only types.

### BL-124: FormStockTargets — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormStockTargets to use ReadOnly wrappers. This form displays stock plans/profiles in list views, uses FilteredTextComboSet for entry selection, and populates item/colony/station combos. Switch list population and combo population to read-only types.

### BL-125: FormSupplyChain — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormSupplyChain to use ReadOnly wrappers. This form displays supply chains in a list view and populates location/resource/route combos for stage editing. Switch list population and combo population to read-only types.

### BL-126: FormContacts — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormContacts to use ReadOnly wrappers. This form displays factions and external characters in list views with FactionReferenceCounter. Switch list population, reference counter inputs, and faction combo to read-only types.

### BL-127: FormAsteroid — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormAsteroid to use ReadOnly wrappers. This form displays asteroids in a list view and shows reserve details. Switch list population and reserve display to read-only types.

### BL-128: FormColonyActivity — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormColonyActivity to use ReadOnly wrappers. This form displays colony activity data in read-only grids and populates colony combos. Switch colony combo and activity data display to read-only types.

### BL-129: FormColonyDailyBuild — Switch to ReadOnly Data Wrappers
**Dependencies:** readonly-data-wrappers spec (done)
**Status: New**
Migrate read-only data paths in FormColonyDailyBuild to use ReadOnly wrappers. This form displays daily build status, populates colony and route combos via RouteDropdownHelper, and reads blueprint data for build time calculations. Switch combo population, route helper inputs, and blueprint lookups to read-only types.
