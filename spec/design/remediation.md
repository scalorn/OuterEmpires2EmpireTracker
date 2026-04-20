<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 1984-2112 — Pre-Iteration Remediation -->
# Pre-Iteration Remediation

These gaps in the existing codebase must be addressed before or alongside Iteration 1 to ensure the new components are built on a consistent foundation. Each item brings existing code up to the cross-cutting standards defined in this design.

## R1: Add NLog Logger to Forms Missing It

| Form | Priority | Notes |
|---|---|---|
| FormPlayerProfile | High | Data-editing form, needs mutation and selection logging |
| FormAutoFill | Medium | Dialog with computation logic, needs decision logging |
| FormPreferences | Low | Simple settings form |
| FormHelp | Low | Display-only |
| FormAbout | Low | Display-only |

Pattern: `private static readonly Logger Log = LogManager.GetCurrentClassLogger();`

## R2: Add NLog Logger to Services Missing It

| Service | Priority | Notes |
|---|---|---|
| ColonyAdminReportBuilder | High | Complex report generation, needs PERF timing |
| ColonyActivityCollector | High | Iterates all colonies/structures |
| ColonyInactivityCollector | High | Iterates all colonies/structures |
| ColonyBuildEligibility | High | Build decision logic |
| ColonyImportHelper | High | Data mutation during import |
| SurveyImportHelper | High | Data mutation during import |
| BlueprintReferenceCounter | Medium | Reference counting logic |
| ColonyReferenceCounter | Medium | Reference counting logic |
| SurveyReferenceCounter | Medium | Reference counting logic |
| BuildTimeCalculator | Medium | Computation service |
| EvolutionChainService | Medium | Graph data computation |
| TabWarningService | Low | Simple threshold check |
| SurveyDateTimeParser | Low | Pure parsing |
| HelpTopicRegistry | Low | Static lookup |

## R3: Add PERF Timing to Existing Forms

All forms with list population or grid rebuild methods need Stopwatch timing. Currently only ColonyV2 has PERF logging.

| Form | Methods to Instrument |
|---|---|
| FormBlueprintV2 | `PopulateListView`, `PopulateForm`, `PopulateGrid` |
| FormDeliveryRoute | `PopulateRouteList`, `PopulateStops`, `PopulatePlanStops` |
| FormDeliveryExecution | `BuildExecution`, `PopulateLoadList` |
| FormSurvey | `PopulateSurveyList`, `PopulateForm`, `PopulateResourceGrid` |
| FormPlayerProfile | `PopulatePlayerList`, `PopulateForm`, `PopulateSkillBlocks` |
| FormPricingPlan | `PopulatePlanList`, `PopulateForm`, `PopulateResourceGrid` |
| FormColonyActivity | `PopulateActivityGrid` |
| FormColonyDailyBuild | `PopulateForm`, `PopulateBuildGrid` |

Pattern from ColonyV2:
```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
// ... work ...
sw.Stop();
Log.Info("MethodName PERF: total={0}ms", sw.ElapsedMilliseconds);
```

## R4: Fix Cross-Thread Marshaling in FormPricingPlan

`FormPricingPlan` subscribes to `CurrentPlayerChanged` but the event handler has no `InvokeRequired` / `BeginInvoke` check. If the background processor triggers a player-related event from the timer thread, this will cause a cross-thread UI access exception.

Fix: Add the standard pattern:
```csharp
private void OnCurrentPlayerChanged(object sender, EventArgs e)
{
    if (InvokeRequired)
    {
        try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
        catch (ObjectDisposedException) { }
        return;
    }
    // ... existing handler logic ...
}
```

## R5: Fix Event Unsubscribe Gap in FormColonyActivity

`FormColonyActivity` subscribes to `CurrentPlayerChanged` but does not unsubscribe in `OnFormClosed`. This can cause `ObjectDisposedException` when the event fires after the form is closed.

Fix: Add `playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;` to `OnFormClosed`.

## R6: Add DeliveryRouteReferenceCounter

`FormDeliveryRoute` has no reference counting. DeliveryRoutes are referenced by DeliveryPlans (via RouteUUID), but routes can be deleted freely, orphaning any plans that reference them.

Fix:
1. Create `Services/DeliveryRouteReferenceCounter.cs` — counts DeliveryPlans referencing the route.
2. Add a "Refs" column to the route ListView in FormDeliveryRoute.
3. Disable Delete button with "In Use (N)" when TotalCount > 0.
4. Block delete handler with MessageBox listing referencing plans.

## R7: Expand Existing Reference Counters

The existing reference counters don't account for all reference sources. These expansions are needed before Iteration 1 because the new entities (BuildItem, ShipTemplate, etc.) will add references to existing entities.

**BlueprintReferenceCounter** — add counts for:
- `BuildItem.BlueprintUUID` (from BuildPlanList)
- `ShipTemplate.HullBlueprintUUID` and `ShipTemplate.Components[].BlueprintUUID`
- `Ship.HullBlueprintUUID` and `Ship.Components[].BlueprintUUID`
- `Station.StationBlueprintUUID` and `Station.Components[].BlueprintUUID`
- `MarketListing.ItemReferenceID` (when ItemType = Blueprint)
- `StockPlan.Targets[].ItemReferenceID` (when ItemType = Blueprint)

Note: These new reference sources only exist after the new entity types are implemented. The counter expansion should be done as each iteration adds the referencing entity. Iteration 1 adds BuildItem → expand for BuildItem.BlueprintUUID. Iteration 2 adds ShipTemplate/Ship → expand for those. And so on.

**ColonyReferenceCounter** — add counts for:
- `BuildItem.BuildLocationUUID` when BuildLocationType=Colony (Iteration 1)
- `SupplyChainStage.LocationUUID` when Colony (Iteration 6)
- `WarehouseOverflowRule.ColonyUUID` and `.DestinationUUID` when Colony (Iteration 6)
- `StockPlan.Targets[].LocationUUID` when Scope=Colony (Iteration 7)

**SurveyReferenceCounter** — add count for:
- `BuildItem.MiningSurveyUUID` (Iteration 6)

## Remediation Sequencing

| Item | When | Blocking? |
|---|---|---|
| R1 (Logger — High priority forms) | Before Iteration 1 | No, but improves debuggability |
| R2 (Logger — High priority services) | Before Iteration 1 | No, but improves debuggability |
| R3 (PERF timing) | Before Iteration 1 | No, but establishes baseline metrics |
| R4 (FormPricingPlan cross-thread) | Before Iteration 1 | Yes — potential crash |
| R5 (FormColonyActivity unsubscribe) | Before Iteration 1 | Yes — potential crash |
| R6 (DeliveryRouteReferenceCounter) | Before Iteration 1 | Yes — data integrity risk |
| R7 (Expand reference counters) | Per-iteration as new entities are added | Yes — data integrity risk |

R4, R5, and R6 are blocking — they represent crash or data integrity risks that should be fixed before adding new complexity.
