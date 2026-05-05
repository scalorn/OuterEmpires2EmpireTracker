# Feature Backlog

**Next available ID: BL-133** (check COMPLETED.md before assigning â€” IDs are shared across both files)

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Systems & Planets Model â”€â”€â”€â”€â”€â”€ (standalone)
Systems & Planets Model â”€â”€â”€â”€â”€ Route Auto-Sequencing (depends on coordinates)
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

Explore whether AI can read the OE2 wiki at https://atlasgamingcorp.com/outer-empires-2/ and pull useful game information from it. Could be a good source of data to enrich the spec and tool â€” game mechanics, structure types, resource details, etc.

### BL-025: Read Game Status Updates
**Dependencies:** None

Explore whether AI can read the game status/dev updates at https://game.dev.outerempires.net/ to stay current on game changes, new features, and balance adjustments that might affect the tracker.

### BL-026: Read outerempires.net for Game Information
**Dependencies:** None

Explore whether AI can read https://outerempires.net/ to pull in additional game information â€” lore, mechanics, community resources, etc.

### BL-027: Read Discord Channels for Game Info
**Dependencies:** None

Explore whether AI can read the Discord channels for the game. Discord is often where the latest game info, patch notes, and community knowledge lives. Would need to investigate API access or bot integration.

### BL-028: Organize Completed Specs into Subdirectories
**Dependencies:** None
**Status: Deprioritized** â€” Kiro's spec tooling references specs by path under `.kiro/specs/{feature_name}/`. Moving into subdirectories (`completed/`, `in-progress/`) would change paths and likely break `.config.kiro` references, task status tracking, and subagent delegation. Currently 16 spec folders â€” manageable. Revisit if count exceeds 30+ or Kiro adds explicit subdirectory support. The backlog already tracks completion status; no need to duplicate that via folder structure.

### BL-029: BaselineData.json â€” Separate User File with Merge Strategy
**Dependencies:** None
**Status: Rejected** â€” The split-file approach adds merge-on-load complexity, "which file wins" ambiguity, and user confusion. With deterministic UUIDs + versioned migrations + idempotent renames (see `spec/discussions/baseline-data-stability.md`), a single BaselineData.json handles upgrades cleanly without a separate user file.

### BL-033: RtfBuilder Font Style Support
**Dependencies:** None
**Status: Deprioritized** â€” Current consumers (ColonyStatusCalculator, ColonyStructure control, ColonyAdminReportBuilder) use color effectively to distinguish headers, values, and status. No current feature is limited by the lack of font styles.

Add support for font styles to RtfBuilder â€” bold, italics, strikethrough, etc.

**Implementation notes:** Add an overload `Append(string text, Color color, FontStyle style = FontStyle.Regular)` using `System.Drawing.FontStyle` flags. Wrap text in RTF control words (`\b...\b0`, `\i...\i0`, `\strike...\strike0`). Backward compatible via default parameter. ~30 minutes of work when a specific need arises.

### BL-036: Colony Mining/Refining Production Graph
**Dependencies:** None

Add a chart to the Colony form showing mining and refining production over time. Use `System.Windows.Forms.DataVisualization.Charting`. Visualize resource output rates from mining rigs and refineries, helping players see production throughput at a glance and identify bottlenecks or underperforming structures.

### BL-070: Blueprint Evolution â€” Capped Property Ranges
**Dependencies:** None

Some blueprint properties evolve with +/- 50% of the base value, but others are capped to a range of 0â€“100 (e.g. Max Repair â€” you wouldn't want more than 100% max repair). The evolution graph and any future evolution prediction need to account for both modes. Per the game developer: "sometimes it's +/- 50%, sometimes it's the +/- 50% of game to 100." Need to identify which properties use which mode and adjust the evolution graph normalization accordingly.

### BL-071: TreeDataGridView â€” Tree-in-Grid Custom Control for Inventory
**Dependencies:** None
**Status: Under consideration** â€” may or may not implement depending on how the master-detail crate UI pattern works out in practice.

Build a custom `TreeDataGridView` control extending DataGridView that supports expandable/collapsible parent-child rows. Primary use case: displaying crate contents inline in inventory grids instead of using a separate master-detail panel (which eats vertical space).

**Discussion:** The master-detail pattern (select a crate, see contents in a grid below) works but doubles the vertical space needed. A tree-in-grid keeps everything in one grid with expand/collapse glyphs on crate rows. WinForms DataGridView is fundamentally flat, so this requires manual bookkeeping: child rows are hidden/shown on expand/collapse, indented via cell padding, and grouped under their parent during sort/filter.

**Complexity:** ~200-300 lines for one-level deep (current crate model â€” no nesting). Crate rows get â–¶/â–¼ glyph via CellPainting, child rows track their parent via tag/index, expand/collapse toggles Visible on children. Sorting must keep children grouped under parents. Filtering a parent hides its children.

**Future-proofing for nested crates:** If crates ever support nesting, use a `Depth` integer per row instead of a boolean IsChild flag. Indentation = depth Ã— indent pixels. Expand/collapse hides all descendants recursively. Adds ~50 lines over the flat version. The game doesn't support nested crates today but the dev hasn't ruled it out.

**Decision point:** Try master-detail first during Iteration 4 (Stations). If vertical space is a problem in practice, build this control as a replacement. Design the control with depth support from the start so nesting is incremental if it comes.

###  : Blueprint Cost Evolution Graph
**Dependencies:** None

Add a cost evolution graph to the Blueprint form, similar to the existing evolution graph but plotting the estimated cost of each blueprint at each evolution level. Cost is computed using a selected pricing plan â€” sum of (resource quantity Ã— resource price) for each resource in the blueprint, plus any time-based costs from the plan. The graph shows how total manufacturing cost changes as the blueprint evolves, helping players decide which evolution to manufacture based on cost efficiency. Requires a pricing plan selector dropdown on the graph panel. Reuse the charting infrastructure from the evolution graph (System.Windows.Forms.DataVisualization.Charting).

---
### BL-045: GitHub MCP Integration
**Dependencies:** None (blocked by Docker installation issues)

Set up the GitHub MCP server so Kiro can read/write GitHub issues, PRs, and wiki pages directly. Enables intake of external issues, PR review, and maintaining user documentation in the repo wiki. Requires Docker Desktop (or the standalone Go binary from github/github-mcp-server releases) plus a fine-grained GitHub Personal Access Token scoped to the repo with Issues, Pull Requests, Contents, and Metadata permissions. Configuration goes in `.kiro/settings/mcp.json`. Currently blocked â€” Docker won't install on the dev machine. Revisit when Docker is available or try the standalone binary approach.

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

## ~~MarketSample Coverage Gaps~~ â€” RESOLVED

All BlueprintTypes now have HTML coverage. No gaps detected (confirmed by IconPositionExtractor coverage gap report in test output).

---

### BL-073: Code Coverage Tooling
**Dependencies:** None
**Status: Blocked**  AltCover (both global tool and NuGet package) fails with .NET Framework 4.8.1 + NUnit + vstest.console. The global tool crashes with a CLR assertion (net8.0 runtime vs net4.8.1 assemblies). The NuGet package instruments successfully but the NUnit test adapter can't discover tests in the instrumented assemblies. OpenCover is unmaintained (last release 2021). VS Community doesn't include the Enterprise code coverage collector.

### BL-074: Immutable Data Model â€” Mutation Through Interface Only
**Dependencies:** BL-069 (done), readonly-list-encapsulation spec (in progress)
**Status: New**

Before we can move to a database or SOA we need to protect the data model from in-memory editing and make all mutation go through an interface. Currently entity POCOs (Blueprint, Colony, Survey, etc.) have public setters on all properties â€” any code can mutate any field at any time without going through a controlled path. This makes it impossible to track dirty state, emit change events, or swap the persistence layer.

Phase 1 (readonly-list-encapsulation) protects the *collections* â€” you can't add/remove entities without going through PlayerContext. Phase 2 (this item) protects the *entities themselves* â€” you can't mutate a Blueprint's Name or a Colony's OwnerUUID without going through a controlled update path. This likely means read-only public properties with internal/private setters, plus Update methods or a unit-of-work pattern that tracks changes and persists them atomically.

Revisit when the project migrates to .NET 8+ where `dotnet test --collect:"XPlat Code Coverage"` works natively. In the meantime, use the file-level coverage analysis tool (`node .kiro/tools/spec-coverage.js`) and the reference counter completeness tests as proxies for coverage.

## Unused Model Fields

Fields on model classes that exist in the data model (persisted in JSON) but are never read by production code. Discovered during the April 2026 model field audit. Each item is a candidate for either wiring into production logic or removing.

### BL-086: ColonyStructure.CurrentAttitude â€” Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** â€” Known incomplete feature (AMB-004/REQ-COL-001b). Deserialized from PlayerData.json but no production code reads the value. Will be needed when worker morale is implemented. Leave as-is until morale feature is built.

### BL-087: ColonyStructure.ContentmentIndex â€” Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** â€” Same incomplete feature group as CurrentAttitude (AMB-004). Deserialized from JSON, never consumed by production code. Leave as-is until morale feature is built.

### BL-088: ColonyStructure.WageLevel â€” Not Wired to Production Code
**Dependencies:** None
**Status: Deferred** â€” Same incomplete feature group as CurrentAttitude (AMB-004). Deserialized from JSON, never consumed by production code. Leave as-is until morale feature is built.

### BL-089: BlueprintType.ResearchableProperties â€” Not Wired to Production Code
**Dependencies:** None
**Status: New**
Deserialized from BaselineData.json but no production code ever reads the value after loading. Potential future feature for research lab UI (showing which properties can be researched on a blueprint type). Either wire it into the Research Lab UI or remove it from the model.

### BL-090: BuildItem.ParentBuildItemUUID â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on BuildItem but never set by any code and never read. Appears to be a planned feature for hierarchical build item dependencies (e.g. "build this after that") that was never implemented. Remove or implement.

### BL-092: ShipStats.CrewSupported â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set by `ShipBuildService.AddBlueprintStats()` and never displayed in any form. Always zero. Remove or wire into ship stat computation.

### BL-093: ShipStats.EngCapacityAvailable â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Remove or wire into ship stat computation.

### BL-094: ShipStats.EngCapacityUsed â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Same field exists on StationStats (BL-099) â€” also unused.

### BL-095: ShipStats.MiningYieldIncrease â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set by `AddBlueprintStats()` and never displayed. Note: `MiningYield` IS used â€” `MiningYieldIncrease` is a separate unused field. Remove or wire into ship stat computation.

### BL-096: ShipStats.SensorAbundanceFactor â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Remove or wire into ship stat computation.

### BL-097: ShipStats.PurityModifier â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always zero. Note: `SurveyViewModel.PurityModifier` IS used â€” that's a different property on a different class. Remove or wire into ship stat computation.

### BL-098: ShipStats.SlotSummary â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on ShipStats but never set or read. Always empty string. Same field exists on StationStats (BL-100) â€” also unused. Remove or wire into ship stat display.

### BL-099: StationStats.EngCapacityUsed â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on StationStats but never set or read. Always zero. Same pattern as ShipStats.EngCapacityUsed (BL-094). Remove or wire into station stat computation.

### BL-100: StationStats.SlotSummary â€” Never Set or Read
**Dependencies:** None
**Status: New**
Declared on StationStats but never set or read. Always empty string. Same pattern as ShipStats.SlotSummary (BL-098). Remove or wire into station stat display.

---

## Empire-Systems Audit Gaps

The following items were designed in the empire-systems spec and marked complete in tasks.md, but the UI controls were either not implemented or implemented differently than the mockup specified. Discovered during the April 2026 mockup-controls audit.

## FilteredTextComboSet â€” Future Adoption Candidates

Controls identified during the custom-control-adoption spec audit that don't have a paired filter TextBox today but would benefit from inline filtering. Each requires either adding a FilteredTextComboSet where no filter exists, or enhancing FilteredTextComboSet to support object binding (BindingSource with DisplayMember/ValueMember).

### BL-102: FormStation â€” cmbStationBlueprint FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbStationBlueprint` in FormStation is a standard ComboBox populated with a large blueprint list (50+ items) via BindingSource with DisplayMember/ValueMember. No paired filter TextBox exists today. Users must scroll through the full list to find a station blueprint. Adopting FilteredTextComboSet here requires either (a) converting from BindingSource to plain string list with a parallel UUID list, or (b) enhancing FilteredTextComboSet to support object binding natively. Option (b) would also benefit BL-103 and BL-104.

### BL-103: FormStation â€” cmbHoldItem and cmbMunItem FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbHoldItem` and `cmbMunItem` in FormStation are standard ComboBoxes populated with item lists that can be large (50+ resources, commodities, blueprints depending on the selected type). No paired filter TextBox exists today. Adding FilteredTextComboSet would improve item selection UX, especially for the Resource item type which has 50+ entries. These use a type-cascade pattern (cmbHoldType/cmbMunItem controls which items appear) â€” the FilteredTextComboSet replaces only the item combo, not the type selector.

### BL-104: FormShipInstance â€” cmbAddItem FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
`cmbAddItem` in FormShipInstance is a standard ComboBox populated with item lists that can be large (50+ items depending on the selected type). No paired filter TextBox exists today. Same type-cascade pattern as FormStation hold items (cmbAddType controls which items appear). Adding FilteredTextComboSet would improve item selection UX. Same BindingSource consideration as BL-102.

### BL-105: FormSupplyChain â€” cmbLocation, cmbResource, cmbRoute FilteredTextComboSet Adoption
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
Three ComboBoxes in FormSupplyChain's stage editor are populated dynamically but have no paired filter TextBox: `cmbLocation` (colonies/stations/asteroids/ships â€” can be 10-50 items), `cmbResource` (50+ resources), and `cmbRoute` (5-20 routes). These use a type-cascade pattern (cmbLocationType/cmbStageType controls which items appear). Adding FilteredTextComboSet to cmbResource would be most valuable (large list). cmbLocation and cmbRoute are smaller but would benefit from consistent UX.

### BL-106: FormBuildPlanner â€” FilteredTextComboSet Adoption for Item Pickers
**Dependencies:** custom-control-adoption spec (in progress)
**Status: New**
FormBuildPlanner has multiple ComboBoxes for selecting blueprints, resources, commodities, routes, plans, and surveys. None have paired filter TextBoxes â€” the form uses cascading type selectors instead. The blueprint and resource pickers have large item lists (50-100+ items) that would benefit from inline filtering. This is a larger conversion since the form has no existing filter pattern to replace â€” FilteredTextComboSet controls would need to be added fresh rather than replacing existing TextBox+ComboBox pairs.

### BL-107: Right-Click Context Menus for DataGridView Grids
**Dependencies:** None
**Status: New**
Add right-click context menus to DataGridView grids across all forms. Currently no form uses right-click context menus on grids â€” all actions are via buttons in command bars. Context menus provide faster access to common actions without moving the mouse to the button bar. The build-plan-execution spec introduces the first grid context menu (`cmsBuildItems` on FormBuildPlanner). This backlog item extends the pattern to other forms:
- **FormColonyV2**: dgvItems (Warehousing tab) â€” Add/Remove items, dgvCommodityRequests (Workers tab) â€” Add/Remove requests, dgvOverflowRules (Overflow tab) â€” Add/Remove rules
- **FormDeliveryRoute**: dgvStops â€” Add/Up/Down/Remove stops, dgvDropOff/dgvPickUp â€” Add/Remove items
- **FormDeliveryExecution**: load list and stop items â€” Mark delivered/undelivered
- **FormSurvey**: dgvResources â€” Add/Remove resources
- **FormBlueprintV2**: dgvResources â€” Add/Remove resources, dgvStatistics â€” Edit properties
- **FormStockTargets**: dgvTargets â€” Add/Remove targets
- **FormMarket**: dgvListings â€” Record sale, dgvTransactions â€” view details
- **FormShipTemplate/FormShipInstance**: dgvSlots/dgvComponents â€” Add/Remove
- **FormStation**: dgvHold/dgvComponents â€” Add/Remove
Each context menu should mirror the existing button actions for that grid, providing the same functionality via right-click.

## Immutable Data Model Migration — Per-Form Backlog Items

These items complete the immutable data model migration across all forms. Each form stops directly mutating entities and routes all changes through a service layer. ViewModels become disconnected edit buffers. List views use ReadOnly wrappers. Unsaved changes prompts are added where applicable. The pattern is established by BL-108 (Blueprint), BL-109 (Colony), BL-110 (Survey), BL-111 (PlayerProfile), BL-112 (DeliveryRoute), and BL-123 (PricingPlan).

### BL-113: DeliveryPlan — Immutable Data Model with Service Layer
**Dependencies:** BL-112 (done)
**Status: Complete** — see spec .kiro/specs/bl-113-deliveryplan-readonly/

### BL-114: Market — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: Complete**  see spec .kiro/specs/bl-114-market-readonly/

### BL-115: ShipTemplate — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: Complete** — see spec .kiro/specs/bl-115-shiptemplate-readonly/
Apply the immutable data model pattern to ShipTemplate. Create ShipTemplateService as sole mutator with CRUD methods for templates and component slots. Create ShipTemplateViewModel as disconnected edit buffer. Migrate FormShipTemplate to route all mutations through the service. Currently mutates ShipTemplate and ShipComponentSlot directly (3 WriteContext calls). Includes unsaved changes prompts.

### BL-116: Ship — Immutable Data Model with Service Layer
**Dependencies:** BL-115
**Status: Complete** — see spec .kiro/specs/bl-116-ship-readonly/

### BL-117: Station — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: Complete** — see spec .kiro/specs/bl-117-station-readonly/
Apply the immutable data model pattern to Station. Create StationService as sole mutator with CRUD methods for stations, components, holds, and munitions. Create StationViewModel as disconnected edit buffer. Migrate FormStation to route all mutations through the service. Currently mutates Station, ShipComponentSlot, and Item directly (3 WriteContext calls). Station is one of the more complex entities with components, holds, and munitions hold. Includes unsaved changes prompts.

### BL-118: BuildPlan — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: New**
Apply the immutable data model pattern to BuildPlan and BuildItem. Create BuildPlanMutationService as sole mutator with CRUD methods for plans and build items (status, location, structure assignment, dependencies). Create BuildPlanViewModel as disconnected edit buffer. Migrate FormBuildPlanner and FormStructureAllocation to route all mutations through the service. FormBuildPlanner currently mutates BuildPlan and BuildItem directly (2+ WriteContext calls). FormStructureAllocation mutates BuildItem allocation fields. Subsumes the old BL-121 (FormBuildPlanner) and BL-122 (FormStructureAllocation) read-only wrapper items. Includes unsaved changes prompts.

### BL-119: StockTargets — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: New**
Apply the immutable data model pattern to StockPlan and StockProfile. Create StockTargetMutationService as sole mutator with CRUD methods for plans, profiles, and entries. Create StockTargetViewModel as disconnected edit buffer. Migrate FormStockTargets to route all mutations through the service. Currently mutates StockPlan, StockProfile, and BuildItem directly (5 WriteContext calls). Includes unsaved changes prompts.

### BL-120: SupplyChain — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: New**
Apply the immutable data model pattern to SupplyChain and SupplyChainStage. Create SupplyChainMutationService as sole mutator with CRUD methods for chains and stages. Create SupplyChainViewModel as disconnected edit buffer. Migrate FormSupplyChain to route all mutations through the service. Currently mutates SupplyChain and SupplyChainStage directly (3 WriteContext calls). Includes unsaved changes prompts.

### BL-121: Contacts — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: New**
Apply the immutable data model pattern to Faction and ExternalCharacter. Create ContactsService as sole mutator with CRUD methods for factions and characters. Create ContactsViewModel as disconnected edit buffer (dual-entity: faction list + character list). Migrate FormContacts to route all mutations through the service. Currently mutates Faction and ExternalCharacter directly (3 WriteContext calls). Includes unsaved changes prompts and delete reference protection via FactionReferenceCounter.

### BL-122: Asteroid — Immutable Data Model with Service Layer
**Dependencies:** none
**Status: New**
Apply the immutable data model pattern to Asteroid and AsteroidReserve. Create AsteroidService as sole mutator with CRUD methods for asteroids and reserves. Create AsteroidViewModel as disconnected edit buffer. Migrate FormAsteroid to route all mutations through the service. Currently mutates Asteroid and AsteroidReserve directly (1+ WriteContext calls). Includes unsaved changes prompts.

### BL-124: Read-Only Display Forms — Switch to ReadOnly Wrappers
**Dependencies:** none
**Status: New**
Migrate the remaining read-only display forms to use ReadOnly wrappers consistently. These forms do not mutate entities and do not need services or ViewModels, but should consume ReadOnly types for consistency. Covers: FormColonyActivity (BL-128), FormColonyDailyBuild (BL-129), and FormAutoFill (BL-114). FormColonyActivity and FormColonyDailyBuild already partially use ReadOnly wrappers. FormAutoFill is a pure dialog that returns options to its parent. Subsumes the old BL-114, BL-128, and BL-129 items.

### BL-125: Final Mutation Audit — Verify No Direct Entity Mutation Outside Services
**Dependencies:** BL-113 through BL-124
**Status: New**
After all forms are migrated, run a comprehensive mutation audit across the entire codebase. Extend the mutation guard tests to cover every entity type. Verify that no form or ViewModel directly sets properties on any entity. Verify that WriteContext() is only called from service classes, PlayerContext deserialization/migration, and test code. This is the final validation that the immutable data model is fully enforced.
