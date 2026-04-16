# Design: Colony Form Rewrite

## Overview

Replace the existing 2041-line FormColony and 1859-line ColonyStructure control with a clean implementation built around three principles: (1) the data model is always the source of truth with write-through from UI controls, (2) structure controls are pooled and reused instead of created/disposed on every change, and (3) status recalculation uses incremental per-structure deltas instead of full O(n) passes. All existing services remain unchanged — the new form delegates to them.

## Architecture

```
FormColonyV2 (UI)
  |
  +-- ColonyViewModel (existing, unchanged)
  |     Wraps Colony model, exposes typed properties
  |     Save/AddStructure/RecalculateStatus
  |
  +-- ColonyStructureViewModel (existing, unchanged)
  |     Wraps ColonyStructure, hides PropertyBag access
  |     IsBuilt/IsStaged/IsOnline/worker assignment
  |
  +-- ColonyStatusCalculator (existing service, enhanced with delta caching)
  |     CalculateBuilt(), CalculateIdeal()
  |     NEW: per-structure delta separation for O(1) updates
  |
  +-- ColonyImportHelper (existing, unchanged)
  |     FindByPlanet(), MergeIdentity(), CreateFromTemp()
  |
  +-- ColonyParser (existing, unchanged)
  |     ParseClipboardToTemp(), ProcessHtml()
  |
  +-- BuildOrderOptimizer (existing, unchanged)
  |     Optimize()
  |
  +-- ColonyBootstrap (existing, unchanged)
  |     Bootstrap()
  |
  +-- ColonyAdminReportBuilder (existing, unchanged)
  |     BuildReport()
  |
  +-- TabWarningService (existing, unchanged)
  |     EvaluateStructureWarning(), EvaluateWorkerWarning(),
  |     EvaluateColonyImportStalenessWarning()
  |
  +-- ColonyReferenceCounter (existing, unchanged)
  |     CountReferences()
  |
  +-- Structure_Pool (NEW)
        Reusable pool of ColonyStructureV2 controls
```

## Key Design Decisions

### 1. Structure_Pool — Control Reuse

Current: Structure controls are created fresh on every colony selection and disposed on switch. With 60+ structures per colony, this causes visible lag.

New: A pool of ColonyStructureV2 controls is maintained. On colony switch, controls are returned to the pool (hidden, reset) and re-acquired as needed. The pool grows to the high-water mark and never shrinks during the form's lifetime.

```csharp
private readonly List<ColonyStructureV2> _pool = new List<ColonyStructureV2>();
private int _poolInUse = 0;

private ColonyStructureV2 AcquireStructureControl()
{
    if (_poolInUse < _pool.Count)
    {
        var ctrl = _pool[_poolInUse];
        _poolInUse++;
        ctrl.Visible = true;
        return ctrl;
    }
    var newCtrl = new ColonyStructureV2();
    newCtrl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
    _pool.Add(newCtrl);
    _poolInUse++;
    return newCtrl;
}

private void ReturnAllToPool()
{
    for (int i = 0; i < _poolInUse; i++)
    {
        _pool[i].Visible = false;
        _pool[i].Reset();
    }
    _poolInUse = 0;
}
```

### 2. Deferred Tab Population with Dirty Flags

Current: All tabs are populated on colony selection, even though only one tab is visible.

New: Only the active tab is populated on colony selection. Other tabs are marked dirty and populated when the user switches to them.

```csharp
private bool _structuresDirty = true;
private bool _warehouseDirty = true;
private bool _workersDirty = true;
private bool _adminDirty = true;

private void tabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
{
    var tab = tabDetailedData.SelectedTab;
    if (tab == tabStructures && _structuresDirty)
    {
        PopulateStructures();
        _structuresDirty = false;
    }
    else if (tab == tabWarehousing && _warehouseDirty)
    {
        PopulateItemGrid();
        _warehouseDirty = false;
    }
    else if (tab == tabWorkers && _workersDirty)
    {
        PopulateCommodityRequestGrid();
        _workersDirty = false;
    }
    else if (tab == tabAdmin && _adminDirty)
    {
        RefreshAdminReport();
        _adminDirty = false;
    }
}

private void MarkAllTabsDirty()
{
    _structuresDirty = true;
    _warehouseDirty = true;
    _workersDirty = true;
    _adminDirty = true;
}
```

### 3. Incremental Status Calculation with Per-Structure Deltas

Current: `ColonyStatusCalculator.CalculateBuilt()` uses a running accumulator — each structure's status is the cumulative sum of all prior structures. Changing one structure requires recalculating all subsequent structures O(n).

New: Each structure computes its own delta (what it contributes to colony totals). The colony total is the sum of all deltas. When a single structure changes, only its delta is recomputed and the total updated by subtract-old/add-new — O(1).

```csharp
// Per-structure delta stored on ColonyStructure.StatusDelta
public class StructureStatusDelta
{
    public decimal PowerProvided;
    public decimal PowerRequired;
    public decimal HabitationProvision;
    public decimal FoodProvision;
    public decimal EntertainmentProvided;
    public decimal WarehouseCapacity;
    public int WorkerCount;        // assigned + unallocated workers
    public int UnallocatedCount;   // unallocated workers only
}

// Full recalculation: sum all deltas
public void CalculateBuiltFull()
{
    ClearAllWorkerLocks();
    var blueprintCache = new Dictionary<string, Blueprint>();
    
    foreach (var structure in colony.Structures)
    {
        Blueprint bp = GetCachedBlueprint(structure.FlatpackBlueprintUUID, blueprintCache);
        var delta = ComputeStructureDelta(structure, bp);
        structure.StatusDelta = delta;
        LockResourcesForStructure(structure, bp);
    }
    
    SumAllDeltas();
}

// Incremental update: recompute one structure's delta
public void RecalculateStructure(ColonyStructure structure)
{
    var oldDelta = structure.StatusDelta;
    Blueprint bp = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
    var newDelta = ComputeStructureDelta(structure, bp);
    structure.StatusDelta = newDelta;
    
    SubtractDelta(oldDelta);
    AddDelta(newDelta);
    
    // Re-lock resources for this structure only
    colony.Locks.ClearLocksForProcess(structure.UUID);
    LockResourcesForStructure(structure, bp);
}
```

### 4. Dictionary Caches for FindBlueprint/FindSurvey/FindColony

Current: `PlayerContext.FindBlueprint()` does O(n) `FirstOrDefault` scan of BlueprintList, then falls back to `EmpireContext.FindGlobalBlueprint()` which does another O(n) scan. Same pattern for FindSurvey and FindColony.

New: Each context maintains a `Dictionary<string, T>` cache indexed by UUID. Cache is lazily built on first access and invalidated on add/remove/UUID change.

```csharp
// In PlayerContext
private Dictionary<string, Blueprint> _blueprintCache;

public Blueprint FindBlueprint(string id)
{
    if (string.IsNullOrEmpty(id)) return null;
    
    if (_blueprintCache == null)
        _blueprintCache = BlueprintList.Where(b => b.UUID != null)
            .ToDictionary(b => b.UUID, b => b);
    
    if (_blueprintCache.TryGetValue(id, out var match))
        return match;
    
    return EmpireContext.GetInstance()?.FindGlobalBlueprint(id);
}

public void InvalidateBlueprintCache() => _blueprintCache = null;

// Same pattern for FindSurvey, FindColony
// EmpireContext.FindGlobalBlueprint uses its own cache
```

### 5. ItemBag Secondary Index

Current: `FindByType()`, `CountByType()`, `FindResource()` all do O(n) LINQ scans over all items.

New: Secondary index keyed by `(ItemType, BaseItemTypeID)` for O(1) lookups. Updated on AddItem/Remove.

```csharp
// In ItemBag
private Dictionary<(ItemType.ItemTypeEnum, string), List<Item>> _typeIndex;

private void EnsureTypeIndex()
{
    if (_typeIndex != null) return;
    _typeIndex = new Dictionary<(ItemType.ItemTypeEnum, string), List<Item>>();
    foreach (var kvp in Items)
    {
        var key = (kvp.Value.ItemType, kvp.Value.BaseItemTypeID ?? "");
        if (!_typeIndex.TryGetValue(key, out var list))
        {
            list = new List<Item>();
            _typeIndex[key] = list;
        }
        list.Add(kvp.Value);
    }
}

public List<Item> FindByType(ItemType.ItemTypeEnum itemType, string baseItemTypeID)
{
    EnsureTypeIndex();
    var key = (itemType, baseItemTypeID ?? "");
    return _typeIndex.TryGetValue(key, out var list) ? list : new List<Item>();
}

public void AddItem(Item item)
{
    Items.Add(item.UUID, item);
    _typeIndex = null; // invalidate
}

public bool Remove(string uuid)
{
    if (Items.Remove(uuid)) { _typeIndex = null; return true; }
    return false;
}
```

### 6. Cached StructureViewModels

Current: `ColonyViewModel.StructureViewModels` creates new ViewModel objects on every property access.

New: Cached list, invalidated on structural changes.

```csharp
private List<ColonyStructureViewModel> _cachedStructureVMs;

public IReadOnlyList<ColonyStructureViewModel> StructureViewModels
{
    get
    {
        if (_cachedStructureVMs == null)
        {
            _cachedStructureVMs = _colony.Structures
                .Select(s => new ColonyStructureViewModel(s, _playerContext))
                .ToList();
        }
        return _cachedStructureVMs.AsReadOnly();
    }
}

public void InvalidateStructureViewModels() => _cachedStructureVMs = null;
```

### 7. Lightweight Update Paths

Current: Every change (worker toggle, state change) triggers full `UpdateData()` on the control — rebuilds all UI elements, re-looks up blueprint, recalculates layout.

New: Two update paths:
- `UpdateData(Blueprint bp)` — full repaint, accepts pre-resolved blueprint
- `UpdateBackgroundColor()` — lightweight, only changes background color based on current state

```csharp
// ColonyStructureV2
public void UpdateBackgroundColor()
{
    if (ViewModel.IsStaged)
        flpColonyStructure.BackColor = Color.Yellow;
    else if (!ViewModel.IsOnline)
        flpColonyStructure.BackColor = Color.PaleVioletRed;
    else
        flpColonyStructure.BackColor = _hasAllWorkers ? Color.Green : Color.LightGreen;
}

// Called from worker checkbox handler instead of full UpdateData
private void chkWorker_CheckedChanged(object sender, EventArgs e)
{
    if (_isProgrammaticUpdate > 0) return;
    var chk = (CheckBox)sender;
    ViewModel.SetWorkerAssigned((string)chk.Tag, chk.Checked);
    UpdateBackgroundColor();
    OnColonyStructureDataChanged(structural: false);
}
```

### 8. Form Layout — Designer-First

All panels defined in Designer.cs. The form layout:

```
+--------------------------------------------------+
| flpColonyList (left)  | flpColonyData (right)     |
|  txtColonyFilter      |  flpIdentity              |
|  lvwColonies          |    Planet, Colony, System  |
|                       |  tabDetailedData          |
|                       |    Administration          |
|                       |      rtbAdminReport        |
|                       |    Structures              |
|                       |      flpStatus (top)       |
|                       |      lvwStructureTypes     |
|                       |      flpStructures (scroll)|
|                       |    Workers                 |
|                       |      dgvCommodityRequests  |
|                       |    Warehousing             |
|                       |      dgvItems              |
|                       |  flpCommands               |
|                       |    New|Save|Delete|Import  |
+--------------------------------------------------+
```

### 9. ColonyStructureV2 Control Design

Each structure control is a compact UserControl with:

```
+----------------------------------------------------------+
| flpHeader: [Name #Seq] [Staged] [Built] [Online]        |
| rtbStatus: Actual: P:0/100 H:0/50 ... (colored RTF)     |
|            Ideal:  P:0/100 H:0/50 ...                    |
| flpWorkers: [BC1] [WC1] [Spec1] [Support-BC]            |
| flpSelection: [Filter] [Primary Combo] [Start] [Done]   |
| flpSubSelection: [Filter] [Secondary Combo]              |
| flpManufacturing: [Qty] [StageResources]                 |
| flpTimer: [Countdown] [Progress Status]                  |
| flpStructureCommands: [Up] [Down] [Delete] [Build]       |
+----------------------------------------------------------+
```

Panel visibility is controlled by blueprint type and structure state:

| Blueprint Type     | flpSelection (primary)   | flpSubSelection (secondary) | flpManufacturing     |
|--------------------|--------------------------|-----------------------------|-----------------------|
| MiningRig          | Survey combo + filter    | Resource combo + filter     | hidden                |
| Refinery           | Resource combo + filter  | hidden                      | hidden                |
| ResearchLab        | Blueprint combo + filter | hidden                      | hidden                |
| Manufactory        | Blueprint combo + filter | hidden                      | Qty + StageResources  |
| CommodityFactory   | Commodity combo + filter | hidden                      | Qty + StageResources  |
| Other (no process) | hidden                   | hidden                      | hidden                |

Mining flow: user selects a survey in flpSelection → flpSubSelection becomes visible with resources from that survey → user selects a resource → clicks Start. When MiningSurvey or MiningSurveyResource changes, MiningLeftOvers resets to zero.

### 10. Event Lifecycle

```csharp
public FormColonyV2()
{
    InitializeComponent();
    // ... setup combos, grids, pool, initial population ...
    
    playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
    playerContext.ColonyDataChanged += OnColonyDataChanged;
    
    tabDetailedData.SelectedIndexChanged += tabDetailedData_SelectedIndexChanged;
}

protected override void OnFormClosed(FormClosedEventArgs e)
{
    WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
    playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
    playerContext.ColonyDataChanged -= OnColonyDataChanged;
    base.OnFormClosed(e);
}
```

### 11. GetAllBlueprints Cache

Current: `PlayerContext.GetAllBlueprints()` allocates a new merged list (700+ entries) on every call.

New: Cached merged list, invalidated when either blueprint list changes.

```csharp
private List<Blueprint> _allBlueprintsCache;

public List<Blueprint> GetAllBlueprints()
{
    if (_allBlueprintsCache == null)
    {
        _allBlueprintsCache = new List<Blueprint>(BlueprintList);
        var ec = EmpireContext.GetInstance();
        if (ec?.GlobalBlueprintList != null)
            _allBlueprintsCache.AddRange(ec.GlobalBlueprintList);
    }
    return _allBlueprintsCache;
}

public void InvalidateAllBlueprintsCache() => _allBlueprintsCache = null;
```

## Components Changed

| Component | Change |
|-----------|--------|
| Forms/ColonyV2/FormColonyV2.cs | NEW — clean rewrite |
| Forms/ColonyV2/FormColonyV2.Designer.cs | NEW — full layout in Designer |
| Forms/ColonyV2/ColonyStructureV2.cs | NEW — poolable structure control |
| Forms/ColonyV2/ColonyStructureV2.Designer.cs | NEW — structure control layout |
| Services/PlayerContext.cs | ENHANCED — FindBlueprint/FindSurvey/FindColony dictionary caches, GetAllBlueprints cache |
| Services/EmpireContext.cs | ENHANCED — FindGlobalBlueprint dictionary cache |
| Services/ColonyStatusCalculator.cs | ENHANCED — per-structure delta separation, blueprint cache per pass |
| ViewModels/ColonyViewModel.cs | ENHANCED — cached StructureViewModels |
| Models/ItemBag.cs | ENHANCED — secondary type index |
| Forms/Colony/FormColony.cs | UNCHANGED — old form stays working |
| Forms/Colony/ColonyStructure.cs | UNCHANGED |
| MainWindow.cs | Add "Manage Colonies V2" menu item alongside existing |

### Parallel Development Strategy

The new form lives in `Forms/ColonyV2/` and is opened via a separate menu item ("Manage Colonies V2") during development. The old form remains fully functional. Service-level enhancements (dictionary caches, ItemBag index, delta calculation) benefit both forms. Once the V2 form passes acceptance testing:
1. Remove the old "Manage Colonies" menu item
2. Rename "Manage Colonies V2" to "Manage Colonies"
3. Delete `Forms/Colony/FormColony.cs`, `FormColony.Designer.cs`, `ColonyStructure.cs`, `ColonyStructure.Designer.cs`

## Testing Strategy

### Existing Tests (must continue to pass)
- ColonyStatusCalculatorTests
- ColonyParserTests
- ColonyImportHelperTests
- BuildOrderOptimizerTests
- ColonyBootstrapTests
- ColonyAdminReportBuilderTests
- TabWarningServiceTests
- ColonyProcessingTests
- ItemBagTests
- LockTrackingTests

### New Tests
- FindBlueprint/FindSurvey/FindColony cache: verify O(1) lookup, cache invalidation on add/remove
- ItemBag secondary index: verify FindByType/CountByType/FindResource return correct results after add/remove
- GetAllBlueprints cache: verify invalidation when blueprint lists change
- StructureViewModels cache: verify invalidation on structural changes
- Incremental status delta: verify single-structure recalculation produces same totals as full recalculation
- Structure_Pool: verify acquire/return/reuse lifecycle
