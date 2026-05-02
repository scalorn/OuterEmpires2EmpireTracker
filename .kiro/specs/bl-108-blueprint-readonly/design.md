# BL-108 Design: Blueprint Immutable Data Model

## Overview

This is a two-phase migration:
1. **Phase 1**: Switch FormBlueprintV2's read-only paths to use ReadOnly wrappers
2. **Phase 2**: Make Blueprint/Item property setters `internal`, enforcing immutability at compile time

## Phase 1: Read-Only Consumer Migration

### Selection Boundary Pattern

The critical design decision: the list view selection handler is the ONLY mutable boundary.

```
List View (ReadOnlyBlueprint in Tags)
    │
    ▼ user selects
Selection Handler
    │ reads UUID from ReadOnlyBlueprint
    │ looks up mutable Blueprint by UUID
    ▼
BlueprintViewModel (wraps mutable Blueprint)
    │
    ▼ write-through
Mutable Blueprint (internal setters)
```

```csharp
// Selection handler — the controlled gate
var roBp = lvwBlueprints.SelectedItems[0].Tag as ReadOnlyBlueprint;
var bp = playerContext.FindBlueprint(roBp.UUID)
      ?? empireContext.FindGlobalBlueprint(roBp.UUID);
viewModel.SelectBlueprint(bp);
```

### What Uses ReadOnly (Phase 1)

| Component | Current | After |
|-----------|---------|-------|
| List view Tags | `Blueprint` | `ReadOnlyBlueprint` |
| Filter combos | Mutable type lists | Read-only type lists |
| Reference counter | Mutable lists | Read-only lists |
| Evolution graph | Mutable blueprint lists | Read-only blueprint lists |
| Base blueprint combo | Mutable list | Read-only list |
| Pricing plan combo | Mutable list | Read-only list |

### What Stays Mutable (Phase 1)

| Component | Reason |
|-----------|--------|
| BlueprintViewModel | Legitimate edit path |
| Edit-panel combos (cmbBlueprintType, cmbShipClass, cmbTechLevel) | Write-through to ViewModel |
| Statistics grid | Editable cells write through ViewModel |
| Resources grid | Editable cells write through ViewModel |
| Import/scanner path | Creates temp Blueprint objects |
| Save/delete path | Persists through ViewModel |

## Phase 2: Internal Setters

### Item Base Class Changes

```csharp
public class Item
{
    // UUID: set during construction or deserialization only
    public string UUID { get; internal set; }

    // ItemType: set in constructor, never changed
    public ItemType.ItemTypeEnum ItemType { get; internal set; }

    // Name/NickName/Description: set via ViewModel or import
    public virtual string Name { get; internal set; } = string.Empty;
    public virtual string NickName { get; internal set; } = string.Empty;
    public virtual string Description { get; internal set; } = string.Empty;

    // Other Item properties
    public string BaseItemTypeID { get; internal set; } = string.Empty;
    public int Quantity { get; internal set; } = 0;
    public string ResourcePurity { get; internal set; } = string.Empty;
    public decimal Volume { get; internal set; } = 0m;
    public ItemBag Contents { get; internal set; }
    public int CurrentHP { get; internal set; } = 0;
    public int MaxHP { get; internal set; } = 0;
    public decimal MaxRepairPercent { get; internal set; } = 0m;
}
```

### Blueprint Class Changes

```csharp
public class Blueprint : Item
{
    public string OwnerUUID { get; internal set; } = string.Empty;
    public string BaseBlueprintUUID { get; internal set; }
    public string LegacyUUID { get; internal set; }
    public string BluePrintType { get; internal set; }
    public int Evolution { get; internal set; }
    public string TechLevel { get; internal set; }
    public int Class { get; internal set; }
    public int CopyCost { get; internal set; }
    public PropertyBag Properties { get; internal set; }
    public Dictionary<string, string> Resources { get; internal set; }
}
```

### InternalsVisibleTo

In `Properties/AssemblyInfo.cs` or a new file:
```csharp
[assembly: InternalsVisibleTo("OE2EmpireTracker.Tests")]
```

### JSON Deserialization

Newtonsoft.Json uses reflection to set properties. With `internal` setters, the deserializer can still set them because:
- The deserializer runs within the same assembly (OE2EmpireTracker)
- Newtonsoft.Json uses `BindingFlags.NonPublic` when the property has a non-public setter

Verify with existing round-trip serialization tests.

### Impact on Other Code

Since `internal` is assembly-scoped, ALL code within OE2EmpireTracker can still set properties. This means:
- ViewModel write-through works (same assembly)
- Importer/scanner works (same assembly)
- Migration code works (same assembly)
- Colony form reading blueprint properties works (same assembly, read-only)

The protection is against:
- External assemblies (future plugins, API consumers)
- The test project (unless InternalsVisibleTo is declared)
- Accidental mutation from code that shouldn't be touching Blueprint state (caught by code review, not compiler — but ReadOnly wrappers in Phase 1 provide the compile-time enforcement for read-only paths)

### Risk: Item Setters Affect All Item Subclasses

Making `Item` setters `internal` affects ALL classes that inherit from `Item`, not just `Blueprint`. This includes warehouse items, delivery items, etc. Those code paths also need to be within the main assembly (which they are). But it's a broader change than just Blueprint.

**Mitigation**: Do Item setter changes in a separate task, after Blueprint-specific changes are verified. Test thoroughly.

## Task Ordering

Phase 1 tasks (read-only wrappers) can be done independently of Phase 2 (internal setters). Phase 1 provides immediate protection for the blueprint form. Phase 2 provides assembly-wide enforcement.

Recommended order:
1. Phase 1 tasks (list view, selection, combos, graph, pricing)
2. Phase 2: Blueprint-specific internal setters
3. Phase 2: Item base class internal setters (broader impact)
4. Verification
