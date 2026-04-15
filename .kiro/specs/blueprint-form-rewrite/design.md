# Design: Blueprint Form Rewrite

## Overview

Replace the existing 2143-line FormBlueprint with a clean implementation built around one core principle: the data model (PropertyBag, Resources dictionary) is always the source of truth. The grid is a view, not a store. All edits write through immediately. Save only persists to disk.

## Architecture

```
FormBlueprint (UI)
  |
  +-- BlueprintViewModel (existing, unchanged)
  |     Wraps Blueprint model, exposes typed properties
  |     Save/Delete/Reset/SelectBlueprint
  |
  +-- BlueprintImportHandler (NEW service)
  |     ClassifyImport() -> ResourcesOnly | Full | NoName
  |     FindTarget() -> selected match | dedup match | new
  |     MergeAndPersist() -> merge + save + notify
  |
  +-- EvolutionChainService (existing, unchanged)
  |     ResolveChain(), BuildGraphData(), ClassifySegments()
  |
  +-- BlueprintReferenceCounter (existing, unchanged)
  |     CountReferences() -> ReferenceReport
  |
  +-- PriceCalculator (existing, unchanged)
  |     ComputeBlueprintPrice()
  |
  +-- BlueprintScanner (existing, unchanged)
        ParseClipboardToTemp(), ProcessMarketHtml()
```

## Key Design Decisions

### 1. Write-Through Statistics Grid

Current: Grid cells are the source of truth. Save does `ClearProperties()` then writes all cells back. Empty cells overwrite imported values.

New: PropertyBag is the source of truth. `CellValueChanged` writes non-empty values to PropertyBag immediately, removes the key if cleared. Save just calls `WriteContext()`.

```csharp
private void dgvStatistics_CellValueChanged(object sender, DataGridViewCellEventArgs e)
{
    if (_isProgrammaticUpdate > 0) return;
    string propName = dgvStatistics.Rows[e.RowIndex].Cells["Property"].Tag as string;
    string value = dgvStatistics.Rows[e.RowIndex].Cells["CurrentValue"].Value?.ToString();
    if (string.IsNullOrEmpty(value))
        viewModel.Data.Properties.Remove(propName);
    else
        viewModel.SetProperty(propName, value);
}
```

### 2. Grid Structure — Defined + Extra Properties

Current: Grid only shows properties from BlueprintType.Properties array. Unknown properties logged as warnings and dropped from display.

New: Grid shows defined properties first, then any extra properties from the PropertyBag that aren't in the type's array (excluding underscore-prefixed internal keys like `_IconPosition`). Extra properties use Unknown validation (free-form text).

```csharp
private string _cachedGridKey; // "{typeId}|{extraKeysHash}"

private void RefreshStatisticsGrid()
{
    var bt = GetSelectedBlueprintType();
    string[] definedProps = bt?.Properties ?? Array.Empty<string>();
    
    // Find extra properties in PropertyBag not in the type definition
    var definedSet = new HashSet<string>(definedProps, StringComparer.Ordinal);
    var extraProps = viewModel.Data.Properties.Properties.Keys
        .Where(k => !definedSet.Contains(k) && !k.StartsWith("_"))
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    
    string gridKey = (bt?.Id ?? "") + "|" + string.Join(",", extraProps);
    
    if (gridKey != _cachedGridKey)
    {
        RebuildStatisticsGrid(definedProps, extraProps);
        _cachedGridKey = gridKey;
    }
    
    PopulateStatisticsValues();
}
```

Extra properties are logged at WARN level so they're noticed and incorporated into BlueprintPropertyValidation:
```csharp
foreach (var prop in extraProps)
    Log.Warn("Extra property '{0}' on '{1}' (not in {2} type definition)",
        prop, viewModel.Data.Name, bt?.Id ?? "unknown");
```

### 3. BlueprintImportHandler Service

Current: 150+ lines of nested if/else in `cmdImport_Click`.

New: Extracted into a stateless service with three clear methods.

```csharp
public static class BlueprintImportHandler
{
    public enum ImportType { ResourcesOnly, Full, NoName }

    public static ImportType ClassifyImport(Blueprint tempBP)
    {
        if (MarketBlueprintImporter.IsResourcesOnlyImport(tempBP))
            return ImportType.ResourcesOnly;
        if (string.IsNullOrEmpty(tempBP.Name))
            return ImportType.NoName;
        return ImportType.Full;
    }

    public static Blueprint FindTarget(
        Blueprint tempBP, Blueprint selected,
        PlayerContext pc, EmpireContext ec)
    {
        // 1. Check selected match (relaxed: Name + Evo, type matches or empty)
        // 2. Route to global or player list
        // 3. FindByDedupKey in target list
        // 4. Return existing match or null (caller creates new)
    }

    public static Blueprint MergeAndPersist(
        Blueprint target, Blueprint incoming, bool isNew,
        bool isGlobal, PlayerContext pc, EmpireContext ec)
    {
        // If existing: UpdateExisting (additive merge)
        // If new: assign UUID, add to list
        // Persist + notify
    }
}
```

### 4. Form Layout — Designer-First

Current: Filter panel built in code-behind (`InitFilterPanel`), evolution graph tab built in code-behind (`InitEvolutionGraphTab`).

New: All panels defined in Designer.cs. The form layout:

```
+--------------------------------------------------+
| flpSearchList (left)  | flpBlueprintData (right)  |
|  txtFilter            |  flpIdentity              |
|  flpFilterPanel       |    Name, NickName, Desc   |
|    Type | Class       |    Type, Class, Tech, Evo |
|    Tech | Evo | Above |    BaseBlueprint, Global  |
|    [Clear Filters]    |  flpCommands              |
|  lvwBlueprints        |    New | Save | Delete    |
|                       |    Import | Import Market |
|                       |  tabDetailedData          |
|                       |    Statistics | Resources  |
|                       |    Evolution Graph         |
|                       |  flpPricing               |
|                       |    PricingPlan combo       |
|                       |    Computed price display  |
+--------------------------------------------------+
```

### 5. Event Lifecycle

```csharp
public FormBlueprint()
{
    InitializeComponent();
    // ... setup combos, grids, initial population ...
    
    // Subscribe to data events
    playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
    playerContext.BlueprintDataChanged += OnBlueprintDataChanged;
    playerContext.PricingDataChanged += OnPricingDataChanged;
}

protected override void OnFormClosed(FormClosedEventArgs e)
{
    playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
    playerContext.BlueprintDataChanged -= OnBlueprintDataChanged;
    playerContext.PricingDataChanged -= OnPricingDataChanged;
    base.OnFormClosed(e);
}
```

### 6. Save Handler — Minimal

```csharp
private void btnSave_Click(object sender, EventArgs e)
{
    // End any active grid edits
    EndGridEdits();
    
    // Validate
    if (string.IsNullOrWhiteSpace(viewModel.Name))
    {
        MessageBox.Show("Blueprint name is required.");
        return;
    }
    
    // Persist — all data is already in the model via write-through
    viewModel.Save(chkGlobalBlueprint.Checked);
    
    RefreshBlueprintList();
    SelectBlueprintInList(viewModel.Data.UUID);
}
```

### 7. Import Handler — Form Integration

```csharp
private void cmdImport_Click(object sender, EventArgs e)
{
    // Validate clipboard
    if (!ValidateClipboardContent()) return;
    
    var scanner = new BlueprintScanner();
    var tempBP = scanner.ParseClipboardToTemp();
    if (tempBP == null) return;
    
    Log.Info("=== Individual Blueprint Import ===");
    LogParsedBlueprint(tempBP);
    
    var importType = BlueprintImportHandler.ClassifyImport(tempBP);
    
    if (importType == BlueprintImportHandler.ImportType.ResourcesOnly)
    {
        HandleResourcesOnlyImport(tempBP);
        return;
    }
    
    if (importType == BlueprintImportHandler.ImportType.NoName)
    {
        HandleNoNameFallback(scanner);
        return;
    }
    
    var target = BlueprintImportHandler.FindTarget(
        tempBP, viewModel.Data, playerContext, empireContext);
    
    var result = BlueprintImportHandler.MergeAndPersist(
        target, tempBP, target == null,
        MarketBlueprintImporter.IsGlobalRoute(tempBP.Evolution, 
            !string.IsNullOrEmpty(playerContext.CurrentPlayerUUID)),
        playerContext, empireContext);
    
    viewModel.SelectBlueprint(result);
    RefreshBlueprintList();
    SelectBlueprintInList(result.UUID);
    PopulateForm();
}
```

## Components Changed

| Component | Change |
|-----------|--------|
| Forms/BlueprintV2/FormBlueprintV2.cs | NEW — clean rewrite |
| Forms/BlueprintV2/FormBlueprintV2.Designer.cs | NEW — filter panel + evo graph in Designer |
| Services/BlueprintImportHandler.cs | NEW — extracted import logic (shared by both forms) |
| Forms/Blueprint/FormBlueprint.cs | UNCHANGED — old form stays working |
| Forms/Blueprint/FormBlueprint.Designer.cs | UNCHANGED |
| BlueprintViewModel.cs | Unchanged |
| BlueprintScanner.cs | Unchanged (refactored methods stay compatible) |
| MarketBlueprintImporter.cs | Unchanged |
| EvolutionChainService.cs | Unchanged |
| BlueprintReferenceCounter.cs | Unchanged |
| PriceCalculator.cs | Unchanged |
| MainWindow.cs | Add "Manage Blueprints V2" menu item alongside existing |

### Parallel Development Strategy

The new form lives in `Forms/BlueprintV2/` and is opened via a separate menu item ("Manage Blueprints V2") during development. The old form remains fully functional. Once the V2 form passes acceptance testing:
1. Remove the old "Manage Blueprints" menu item
2. Rename "Manage Blueprints V2" to "Manage Blueprints"
3. Delete `Forms/Blueprint/FormBlueprint.cs` and `.Designer.cs`
4. Optionally rename FormBlueprintV2 to FormBlueprint

Shared service extractions (BlueprintImportHandler) are used by both forms during the transition period. The old form's `cmdImport_Click` is updated to delegate to the shared service.

## Testing Strategy

### Existing Tests (must continue to pass)
- BlueprintScannerTests (individual + market import parsing)
- BlueprintReferenceCounterTests
- EvolutionChainServiceTests
- PriceCalculatorTests
- MarketBlueprintImporterTests

### New Tests
- BlueprintImportHandler unit tests: ClassifyImport, FindTarget, MergeAndPersist
- Property write-through: verify PropertyBag updated on cell edit, removed on clear
- Empty property handling: verify empty cells don't create PropertyBag entries
