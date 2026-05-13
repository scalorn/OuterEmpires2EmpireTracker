# Feature Backlog

**Next available ID: BL-142** (check COMPLETED.md before assigning — IDs are shared across both files)

Open features and enhancements to be worked on.

Items in the "New" section have dependency annotations. Work them in an order that respects the dependency chain.

## Dependency Graph

```
Systems & Planets Model ────── (standalone)
Systems & Planets Model ───── Route Auto-Sequencing (depends on coordinates)
Route Auto-Sequencing ──────── Fuel-Constrained Route Optimization (depends on sequencing + coordinates)
```

Suggested build order:
1. Systems & Planets Model (independent)
2. Route Auto-Sequencing (depends on Systems & Planets Model)
3. Fuel-Constrained Route Optimization (depends on Route Auto-Sequencing)

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

### BL-020: Route Auto-Sequencing
**Dependencies:** Systems & Planets Model (BL-019 — Complete)

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

### BL-134: FlaUI Integration Testing for WinForms
**Dependencies:** None
**Status: New**

Investigate FlaUI (https://github.com/FlaUI/FlaUI) for automated integration testing of the WinForms UI. FlaUI wraps the Windows UI Automation framework and lets you find controls by name/type, click buttons, read grid values, and assert UI state. Works with NUnit.

**Motivation:** Several recent bugs (survey form dirty-on-open, commodity checkbox not persisting, layout issues) are UI lifecycle bugs that unit tests can't catch because they depend on the full form lifecycle — WindowStateHelper.RestoreState, MDI child behavior, event wiring order, and control interaction sequences. A small smoke test suite that opens each form, verifies no dirty prompt on immediate close, and checks that key controls exist would catch these regressions automatically.

**Options to evaluate:**
1. **FlaUI** — Most actively maintained Windows UI Automation wrapper for .NET. Supports .NET Framework 4.8.1. Launch app as a process, attach to window, drive controls programmatically. Tests are slower (real app launch) but catch real lifecycle bugs.
2. **WinAppDriver** — Microsoft's Appium/WebDriver for desktop apps. Functional but deprecated (no active development). FlaUI is the better bet.
3. **In-process form testing** — Already used in the project (e.g. MainMenuOverhaulTests). Fast but doesn't test full lifecycle (no WindowStateHelper restore, no MDI behavior).

**Evaluation criteria:**
- Compatibility with .NET Framework 4.8.1 and NUnit 4.x
- Can it run in CI (headless or with a display)?
- Test execution speed (acceptable for a small smoke suite, not full regression)
- Stability — are tests brittle when layout changes?
- Can it verify: form opens without dirty prompt, checkbox persists on click, grid fills area, command bar position

**Suggested first test:** Open FormSurvey, immediately close it, assert no MessageBox appeared (the exact bug that was just fixed).

**Research deliverable:** A spike in `spec/decisions/` documenting findings, with a working prototype test if FlaUI is viable.

### BL-135: Remote Faction Service — Faction Logistics Coordination (OI-001)
**Dependencies:** Remote Faction Service core implementation
**Status: New — deferred until core service is operational**

Factions need to coordinate logistics among members — requesting resources, manufacturing jobs, and deliveries. The current design supports data sharing (read-only visibility) but has no mechanism for members to make requests of each other or track fulfillment. Scenarios include posting resource requests, offering surplus materials, requesting manufacturing runs, and planning delivery routes to fulfill pending requests.

Key design questions: backing data model (Request entity?), request targeting (faction-wide vs directed), relationship to DeliveryRoutes/Plans, state machine (Open → Claimed → In Transit → Fulfilled → Closed), who can create requests, fulfillment confirmation mechanism, UI integration, priority/urgency, expiry.

Requires the core sharing and membership model to be implemented first.

### BL-136: Remote Faction Service — Game API Integration Specifics (OI-002)
**Dependencies:** Remote Faction Service core implementation, game API documentation
**Status: Blocked — waiting on game API documentation from developers**

The server needs to call the game API on behalf of characters (for background processing, data import). The credential storage mechanism is designed (Requirement 17/Section 25 of the spec), but the actual API endpoints, authentication flow, rate limits, and data formats are unknown.

Key questions: authentication method (OAuth2, API key, session cookie?), available endpoints, rate limits (per-account, per-IP, global?), webhook/push vs polling, token refresh handling, credential expiry notification.

### BL-137: Remote Faction Service — Audit Log Detailed Design (OI-004)
**Dependencies:** Remote Faction Service core CRUD endpoints
**Status: New — requirements clarified, needs detailed API design**

For faction governance and future write-delegation scenarios, all mutations need a full audit trail with diffs. Resolved: every mutation logged with full diff, configurable retention (Owner max >= Faction Leader >= Character), viewable by anyone who can view the entity, includes character UUID and token ID.

Remaining questions: diff format (JSON Patch RFC 6902 vs full snapshots vs both?), API shape for querying audit records, whether audit records are included in data exports, storage impact of full diffs on every colony tick (60s) — should timer ticks be batched/summarized differently from user-initiated mutations?

### BL-138: Colony Primary Activity Classification
**Dependencies:** None
**Status: New**
**Origin:** DarkCrusader analysis — the original OE management tool let users classify colonies by primary activity (mining, manufacturing, research, refining, processing) for filtering and grouping.

Add a user-defined "Primary Activity" label to each colony. The label is a simple enum (Mining, Manufacturing, Research, Refining, Commodity Production, Mixed, Unclassified) stored on the Colony model. The Colony form gets a dropdown to set it, and the colony list/combo boxes can filter by activity type.

**Rationale:** As empires grow to 20+ colonies, finding "my refining colonies" or "my manufacturing colonies" in a flat list becomes tedious. DarkCrusader solved this with a classification field that users set manually. OE2 could infer a default from the dominant structure type but allow manual override.

**Implementation notes:**
- Add `PrimaryActivity` string property to Colony model (persisted in JSON)
- Add a combo box to FormColonyV2 for setting the classification
- Add a filter combo to the colony selector in MainWindow (optional — "Show: All / Mining / Manufacturing / ...")
- ColonyService.Update handles the mutation
- Auto-suggest based on structure composition (>50% of one type = suggest that classification)
- ~2-3 hours of work

### BL-139: Operating Cost Calculator — Worker Wages vs. Market Income
**Dependencies:** None
**Status: New**
**Origin:** DarkCrusader analysis — computed `workerCostsLastWeek` vs `marketSalesLastWeek` for profit/loss analysis. OE2 tracks market transactions but doesn't compute colony operating costs.

Add an "Empire Economics" summary that calculates total worker wages across all colonies and compares to market income over configurable periods (7 days, 30 days, all time). Display net profit/loss.

**Rationale:** Players need to know if their empire is profitable. DarkCrusader computed this by summing "Worker" type bank transactions. OE2 can compute it from colony data (population × wage rate per cycle) since we track colony populations and the game's wage formula is known (6 credits per worker per 25-hour cycle per DarkCrusader's `get_worker_costs_per_25_hours()`).

**Implementation notes:**
- Worker cost formula: `colony.Population * WageRatePerCycle * CyclesPerPeriod`
- Market income: sum of MarketTransaction amounts where TransactionType = Sale, filtered by date range
- Market expenses: sum of MarketTransaction amounts where TransactionType = Purchase, filtered by date range
- Net = Market income - Market expenses - Worker costs
- Display on a new "Economics" tab in MainWindow or as a summary panel on the Market form
- Need to confirm OE2's wage formula (may differ from OE1's 6cr/worker/25h)
- ~4-6 hours of work (model + service + UI)

### BL-140: Market Counterparty Analysis — Top Buyers and Sellers
**Dependencies:** None
**Status: New**
**Origin:** DarkCrusader analysis — `getTopCustomers()` aggregated sales by buyer name with period filtering (forever, 30 days, 7 days, 24 hours), showing total sales volume and transaction count per customer.

Add a "Counterparties" tab or summary to the Market form that aggregates transactions by counterparty name. Show top buyers (by total credits received) and top sellers (by total credits spent), with period filtering.

**Rationale:** Knowing who your best customers are helps players focus trading relationships. DarkCrusader proved this was one of the most-used premium features. OE2 already stores `CounterpartyName` on MarketTransaction — the data is there, just needs aggregation and display.

**Implementation notes:**
- Group MarketTransactions by CounterpartyName
- For each counterparty: sum TotalPrice (sales to them), count transactions, compute average transaction size
- Sort by total volume descending
- Period filter: All Time, Last 30 Days, Last 7 Days
- Display as a DataGridView with columns: Rank, Name, Total Volume, # Transactions, Avg Size
- Optionally show a "Faction" column if counterparty has a known faction (via Contacts/ExternalCharacter lookup)
- Static service method: `MarketService.GetTopCounterparties(transactions, period, direction, limit)`
- ~3-4 hours of work (service method + UI tab)

### BL-141: Fuel-Constrained Route Optimization (extends BL-019/BL-020)
**Dependencies:** BL-019 (Systems & Planets Model), BL-020 (Route Auto-Sequencing)
**Status: New — deferred until BL-019 is implemented**
**Origin:** DarkCrusader analysis — `calculateOptimalManufacturingRoute()` implemented a greedy nearest-first algorithm with fuel constraints, refueling stops, and cargo capacity limits. The most complex feature in the original tool.

Once system coordinates are available (BL-019), extend route auto-sequencing (BL-020) with fuel-aware pathfinding:
- Given a ship's fuel capacity and consumption rate, determine if each leg of a route is reachable
- Insert refueling stops at the nearest station when fuel is insufficient for the next leg
- Consider cargo capacity as a constraint on how many resources can be collected per trip
- Generate step-by-step travel instructions (jump to X, dock at Y, collect Z, refuel at W)

**Rationale:** DarkCrusader's route planner was described as "the most complex feature I have ever coded" in the changelog. It solved a real player pain point: manually planning multi-stop resource collection routes while managing fuel. OE2's delivery routes already model multi-stop logistics — adding fuel awareness makes them actionable travel plans rather than abstract resource lists.

**DarkCrusader's algorithm (for reference):**
1. Determine max items craftable = min(available resources, ship cargo, manufacturing colony storage)
2. Identify bottleneck (which constraint limits production)
3. Subtract resources already at destination
4. For each needed resource, find all colonies with sufficient quantity
5. Greedy loop: visit nearest colony with needed resources, check fuel for leg + return-to-station
6. If insufficient fuel: insert refuel stop at nearest reachable station
7. After all resources collected: route to manufacturing colony, drop off, return to station

**OE2 adaptation:**
- Ship model already has cargo capacity (ShipStats)
- Fuel capacity/consumption would need to be added to ShipTemplate or Ship (new properties)
- Station locations already modeled
- DeliveryRoute stops already have sequence numbers
- The algorithm becomes: given a DeliveryRoute, reorder stops by distance, insert refuel stops where needed, compute trip splits if cargo exceeds capacity
- Integrates with existing `CargoVolumeService.SplitIntoTrips()` for multi-trip planning
- ~8-12 hours of work (coordinate model + distance service + route optimizer + UI integration)

