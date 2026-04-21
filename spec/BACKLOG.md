# Feature Backlog

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

### BL-069: Migrate Existing BindingList Fields to List on PlayerContext
**Dependencies:** None
**Status: Done** — New empire systems entities use `List<T>` from the start. Existing entities (Blueprint, Colony, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, PricingPlan) still use `BindingList<T>` on PlayerContext. Forms already build their own display lists from filtered queries, so the BindingList change notifications aren't providing value. Migrate the existing fields to `List<T>` for consistency. Touches many files — do as a standalone cleanup pass.

---

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

### BL-072: BuildOrderOptimizer — Incremental Simulation
**Dependencies:** None
**Status: New**

BuildOrderOptimizer.SimulateAll() is O(n) per call — it iterates all placed structures and calls FindBlueprint for each. It's called O(n) times during optimization (multiple times per primary structure: Step A deficit check, Step B look-ahead, Step C final check, plus FixDeficits loops up to 50 iterations). For a colony with 60 structures this produces ~18,000 FindBlueprint calls and O(n²) overall simulation cost.

The FindBlueprint dictionary cache (colony-form-rewrite Req 24.1) fixes the inner loop cost, turning 18,000 linear scans into dictionary lookups. But SimulateAll itself remains O(n) per call × O(n) calls = O(n²).

**Proposed fix:** Replace SimulateAll with incremental delta tracking. Maintain a running ColonyStructureStatus total. When a structure is added to the result list, compute its delta and add it to the total — O(1) per placement instead of re-simulating the entire list. SimulateOneMore already does single-structure simulation; the optimizer just needs to accumulate rather than recompute from scratch.

**Additional issues found:**
- `GetAllBlueprints()` allocates a new merged list (700+ entries) on every call. Called by FindBlueprintByType, CreateStructure, CreateStructureByType. Now cached via Req 24.6.
- `TakeFromPool` and `PlaceFromPool` call FindBlueprint per pool candidate — linear scan of pool × FindBlueprint per element. Pool is typically small (< 20) so not critical, but benefits from the dictionary cache.


---
### BL-045: GitHub MCP Integration
**Dependencies:** None (blocked by Docker installation issues)

Set up the GitHub MCP server so Kiro can read/write GitHub issues, PRs, and wiki pages directly. Enables intake of external issues, PR review, and maintaining user documentation in the repo wiki. Requires Docker Desktop (or the standalone Go binary from github/github-mcp-server releases) plus a fine-grained GitHub Personal Access Token scoped to the repo with Issues, Pull Requests, Contents, and Metadata permissions. Configuration goes in `.kiro/settings/mcp.json`. Currently blocked — Docker won't install on the dev machine. Revisit when Docker is available or try the standalone binary approach.

### BL-047: Game API Integration Planning
**Dependencies:** None

There will be a game API eventually. Plan the integration architecture now so we're ready when it arrives. Consider: authentication, polling vs push, data model mapping, how it replaces clipboard import, and what new capabilities it enables (real-time sync, automated queue management, etc.).

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

## Empire-Systems Audit Gaps

The following items were designed in the empire-systems spec and marked complete in tasks.md, but the UI controls were either not implemented or implemented differently than the mockup specified. Discovered during the April 2026 mockup-controls audit.

## Dead Code Findings (April 2026 Audit)

Found by `dead-code.js`. Private methods with no references outside their declaration.

### BL-075: Dead Code — FormColonyV2.AcquireStructureControl
**File:** OE2EmpireTracker/Forms/ColonyV2/FormColonyV2.cs:711
**Status: New**
Private method never called. Likely leftover from a refactor. Review and remove if confirmed dead.

### BL-076: Dead Code — FormColonyV2.ReturnAllToPool
**File:** OE2EmpireTracker/Forms/ColonyV2/FormColonyV2.cs:727
**Status: New**
Private method never called. May have been superseded by a different pool management approach. Review and remove if confirmed dead.

### BL-077: Dead Code — FormDeliveryExecution.GetLoadItemVolume
**File:** OE2EmpireTracker/Forms/DeliveryExecution/FormDeliveryExecution.cs:1115
**Status: New**
Private method never called. Possibly planned for volume calculations that were implemented differently. Review and remove if confirmed dead.

### BL-078: Dead Code — FormBuildPlanner.ShowInputDialog
**File:** OE2EmpireTracker/Forms/BuildPlanner/FormBuildPlanner.cs:1349
**Status: New**
Private method never called. Generic input dialog helper that may have been replaced by a more specific approach. Review and remove if confirmed dead.

### BL-079: Dead Code — FormPlayerProfile.chkColonyOperations_Click
**File:** OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs:251
**Status: New**
Event handler never wired. The checkbox may have been removed from the Designer but the handler left behind. Review and remove if confirmed dead.

## Duplicate Code Findings (April 2026 Audit)

Found by `dupe-code.js`. Methods with identical bodies across different classes.

### BL-080: Duplicate — PopulateRouteDropdown (31 lines)
**Files:** FormColonyDailyBuild.cs:83 == FormDeliveryExecution.cs:138
**Status: New**
Largest duplicate. Both forms populate a delivery route dropdown identically. Extract to a shared helper method or utility class.

### BL-081: Duplicate — PopulateHullCombo / SelectHullInCombo
**Files:** FormShipInstance.cs == FormShipTemplate.cs (14 + 6 lines)
**Status: Accepted — by design**
These were intentionally copied per the ship-form-overhaul spec. Both forms need identical hull combo logic but operate on different selected objects (_selectedShip vs _selectedTemplate). Extracting would require a shared base class or interface, adding complexity for minimal gain. Accept as baseline.

### BL-082: Duplicate — OnCurrentPlayerChanged (10 lines)
**Files:** FormBuildPlanner.cs:1541 == FormPricingPlan.cs:404
**Status: New**
Both forms have identical player-changed handlers. Could extract the common pattern to a base form class or helper.

### BL-083: Duplicate — flpSearchList_Layout (5 lines)
**Files:** FormBuildPlanner.cs:123 == FormPricingPlan.cs:68
**Status: New**
Identical layout handlers. These forms share the same left-list/right-detail pattern. Could extract to a shared layout helper, though at 5 lines the benefit is marginal.

### BL-084: Duplicate — GetRefiningOutputRate (7 lines)
**Files:** ColonyActivityCollector.cs:248 == ColonyAdminReportBuilder.cs:424
**Status: New**
Both services compute refining output rate identically. Extract to a shared static method on one of the services or a utility class.

