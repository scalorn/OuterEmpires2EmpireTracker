# BL-108 Design: Blueprint Immutable Data Model with Service Layer

## Current Architecture

```
Form ──write-through──► ViewModel ──direct set──► Mutable Blueprint ──► JSON
                              │
                              └── holds mutable reference
```

Every keystroke in a text box writes directly to the Blueprint entity in memory. Any code with a Blueprint reference can mutate it. There's no controlled gate, no dirty tracking, no atomic save.

## Target Architecture

```
Form ──local edit──► ViewModel (edit buffer) ──save──► BlueprintService ──► Mutable Blueprint ──► JSON
  ▲                       │                                    │
  │                       │ copies from                        │ fires event
  │                       ▼                                    ▼
  └──── refresh ◄── ReadOnlyBlueprint ◄──────────── PlayerContext/EmpireContext
```

The form never touches the entity. The ViewModel is a disconnected edit buffer. The service is the only code that mutates the entity.

## ViewModel as Edit Buffer

### Current ViewModel (write-through)

```csharp
public class BlueprintViewModel
{
    private Blueprint _blueprint;  // mutable reference

    public string Name
    {
        get => _blueprint.Name;
        set => _blueprint.Name = value;  // writes directly to entity
    }
}
```

### New ViewModel (edit buffer)

```csharp
public class BlueprintViewModel
{
    private ReadOnlyBlueprint _original;  // snapshot loaded from — kept for dirty comparison
    private string _uuid;

    // Local edit state — disconnected from entity
    private string _name;
    private string _nickName;
    private string _description;
    private string _bluePrintType;
    private int _evolution;
    private string _techLevel;
    private int _class;
    private int _copyCost;
    private string _baseBlueprintUUID;
    private string _ownerUUID;
    private bool _isGlobal;
    private Dictionary<string, string> _properties;
    private Dictionary<string, string> _resources;

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    // ... same pattern for all fields

    /// <summary>
    /// True if any local field differs from the original snapshot.
    /// </summary>
    public bool IsDirty
    {
        get
        {
            if (_original == null) return _uuid != null;  // new blueprint
            return _name != _original.Name
                || _nickName != _original.NickName
                || _description != _original.Description
                || _bluePrintType != _original.BluePrintType
                || _evolution != _original.Evolution
                || _techLevel != _original.TechLevel
                || _class != _original.Class
                || _copyCost != _original.CopyCost
                || _baseBlueprintUUID != _original.BaseBlueprintUUID
                || !PropertiesEqual(_properties, _original.Properties)
                || !ResourcesEqual(_resources, _original.Resources);
        }
    }

    /// <summary>
    /// Loads field values from a ReadOnlyBlueprint snapshot.
    /// Retains the original for dirty comparison.
    /// </summary>
    public void LoadFrom(ReadOnlyBlueprint ro)
    {
        _original = ro;
        _uuid = ro.UUID;
        _name = ro.Name;
        // ... copy all fields
    }

    /// <summary>
    /// Builds an update request carrying both the original snapshot
    /// and the current local state. The service can use the original
    /// for field-level diff if needed.
    /// </summary>
    public BlueprintUpdateRequest BuildUpdateRequest()
    {
        return new BlueprintUpdateRequest
        {
            Original = _original,
            Name = _name,
            NickName = _nickName,
            // ... all fields
        };
    }

    /// <summary>
    /// Builds a create request for a new blueprint.
    /// </summary>
    public BlueprintCreateRequest BuildCreateRequest()
    {
        return new BlueprintCreateRequest
        {
            Name = _name,
            NickName = _nickName,
            Description = _description,
            BluePrintType = _bluePrintType,
            Evolution = _evolution,
            TechLevel = _techLevel,
            Class = _class,
            CopyCost = _copyCost,
            BaseBlueprintUUID = _baseBlueprintUUID,
            Properties = new Dictionary<string, string>(_properties),
            Resources = new Dictionary<string, string>(_resources),
        };
    }

    /// <summary>
    /// Resets to empty state for a new blueprint.
    /// </summary>
    public void Reset()
    {
        _original = null;
        _uuid = null;
        _name = string.Empty;
        _nickName = string.Empty;
        _description = string.Empty;
        _bluePrintType = null;
        _evolution = 0;
        _techLevel = null;
        _class = 0;
        _copyCost = 0;
        _baseBlueprintUUID = null;
        _ownerUUID = string.Empty;
        _isGlobal = false;
        _properties = new Dictionary<string, string>();
        _resources = new Dictionary<string, string>();
    }

    /// <summary>True if this is a new blueprint not yet saved.</summary>
    public bool IsNew => _original == null;

    public string UUID => _uuid;
    public ReadOnlyBlueprint Original => _original;
}
```

### BlueprintUpdateRequest

A plain DTO carrying the original snapshot and the current local state. The service can diff against the original for field-level change detection if needed (e.g. only sending changed fields to a remote API):

```csharp
public class BlueprintUpdateRequest
{
    /// <summary>
    /// The original snapshot the edit was based on.
    /// Enables field-level dirty detection and optimistic concurrency.
    /// </summary>
    public ReadOnlyBlueprint Original { get; set; }

    public string Name { get; set; }
    public string NickName { get; set; }
    public string Description { get; set; }
    public string BluePrintType { get; set; }
    public int Evolution { get; set; }
    public string TechLevel { get; set; }
    public int Class { get; set; }
    public int CopyCost { get; set; }
    public string BaseBlueprintUUID { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public Dictionary<string, string> Resources { get; set; }
}
```

### BlueprintCreateRequest

A DTO for creating a new blueprint. No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).

```csharp
public class BlueprintCreateRequest
{
    public string Name { get; set; }
    public string NickName { get; set; }
    public string Description { get; set; }
    public string BluePrintType { get; set; }
    public int Evolution { get; set; }
    public string TechLevel { get; set; }
    public int Class { get; set; }
    public int CopyCost { get; set; }
    public string BaseBlueprintUUID { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public Dictionary<string, string> Resources { get; set; }
}
```

## BlueprintService

```csharp
public class BlueprintService
{
    private readonly PlayerContext _playerContext;
    private readonly EmpireContext _empireContext;

    public ReadOnlyBlueprint Update(string uuid, BlueprintUpdateRequest request)
    {
        var bp = _playerContext.FindMutableBlueprint(uuid)
              ?? _empireContext.FindMutableGlobalBlueprint(uuid);
        if (bp == null) throw new InvalidOperationException("Blueprint not found");

        // Apply changes
        bp.Name = request.Name;
        bp.NickName = request.NickName;
        bp.Description = request.Description;
        bp.BluePrintType = request.BluePrintType;
        bp.Evolution = request.Evolution;
        bp.TechLevel = request.TechLevel;
        bp.Class = request.Class;
        bp.CopyCost = request.CopyCost;
        bp.BaseBlueprintUUID = request.BaseBlueprintUUID;
        bp.Properties = /* rebuild from request */;
        bp.Resources = new Dictionary<string, string>(request.Resources);

        // Persist
        if (IsGlobal(bp))
            _empireContext.WriteContext();
        else
            _playerContext.WriteContext();

        _playerContext.OnBlueprintDataChanged(uuid);
        return new ReadOnlyBlueprint(bp);
    }

    public ReadOnlyBlueprint Create(BlueprintCreateRequest request) { ... }
    public void Delete(string uuid) { ... }
    public ReadOnlyBlueprint Import(Blueprint temp, ReadOnlyBlueprint target) { ... }
    public void MoveToGlobal(string uuid) { ... }
    public void MoveToPlayer(string uuid) { ... }
}
```

## Save Flow

The Save button doesn't need to know whether it's a create or update — the ViewModel decides based on whether `_original` is null.

```
User clicks Save
    │
    ▼
viewModel.IsNew?  (i.e. _original == null)
    │
    ├── YES (new blueprint)
    │     ▼
    │   Form calls viewModel.BuildCreateRequest()
    │   Form calls blueprintService.Create(request, isGlobal)
    │     ▼
    │   Service creates Blueprint, assigns UUID, adds to list
    │   Service persists, fires BlueprintDataChanged
    │     ▼
    │   Form refreshes list, selects new blueprint by UUID
    │   ViewModel.LoadFrom(new ReadOnlyBlueprint)
    │
    └── NO (existing blueprint)
          ▼
        Form calls viewModel.BuildUpdateRequest()
        Form calls blueprintService.Update(viewModel.UUID, request)
          ▼
        Service looks up mutable Blueprint, applies changes
        Service persists, fires BlueprintDataChanged
          ▼
        Form refreshes list, re-selects blueprint
        ViewModel.LoadFrom(updated ReadOnlyBlueprint)
    │
    ▼
ViewModel._original is now set, IsDirty = false
```

## New Blueprint Flow

```
User clicks New
    │
    ▼
Prompt if ViewModel.IsDirty (Save / Discard / Cancel)
    │
    ▼
ViewModel.Reset()
  _original = null
  _uuid = null
  all local fields = defaults (empty strings, zero ints)
    │
    ▼
Form clears all controls
Form enables edit panel
Save button disabled (IsDirty = false — nothing entered yet)
    │
    ▼
User fills in name, type, properties, resources
Each change updates ViewModel local state
IsDirty = true (because _original is null and fields are non-default)
Save button enables
    │
    ▼
User clicks Save → Create flow (see above)
```

## Import Flow

Two patterns depending on whether a blueprint is selected:

### Import with selection (individual blueprint — stats tab, then resources tab)
```
User pastes clipboard (stats)
    │
    ▼
BlueprintScanner parses HTML into temp Blueprint
    │
    ▼
Form merges parsed properties into viewModel's local _properties
ViewModel becomes dirty
    │
User pastes clipboard again (resources)
    │
    ▼
BlueprintScanner parses HTML into temp Blueprint
    │
    ▼
Form merges parsed resources into viewModel's local _resources
ViewModel stays dirty
    │
User clicks Save
    │
    ▼
blueprintService.Update(uuid, viewModel.BuildUpdateRequest())
```

### Import without selection or new blueprint from market
```
User pastes clipboard (full blueprint or market listing)
    │
    ▼
BlueprintScanner parses HTML into temp Blueprint
    │
    ▼
Form calls blueprintService.Import(temp, selectedTarget)
    │
    ▼
Service handles dedup, creates or updates entity, persists
    │
    ▼
Form refreshes, selects imported blueprint
ViewModel.LoadFrom(updated ReadOnlyBlueprint)
```

## What Changes for the User

Nothing. The form looks and behaves identically. The only behavioral difference:
- Changes don't persist until Save (currently they write-through immediately but still need Save to persist to disk — so the user experience is the same)
- The Save button enables when changes are made (dirty tracking)

## Service-Call Readiness

The `BlueprintService.Update(uuid, request)` signature is directly replaceable with an HTTP call:

```csharp
// Local (today)
var result = blueprintService.Update(uuid, request);

// Remote (future)
var result = await httpClient.PutAsync($"/api/blueprints/{uuid}", request);
```

The ViewModel, the form, and the ReadOnly wrappers don't change at all. Only the service implementation changes.

## Risk: Statistics Grid Editing

The statistics grid currently has editable cells that write through to the PropertyBag on every cell change. With the edit buffer pattern, cell edits write to the ViewModel's local `_properties` dictionary instead. This requires changing the grid's CellValueChanged handler to write to the ViewModel rather than directly to the entity.

Same for the resources grid — cell edits write to `_resources` in the ViewModel.

## Risk: Write-Through Removal

The current write-through pattern means the in-memory entity always reflects the UI state. Other forms that read the same blueprint (e.g. colony form showing blueprint properties) see changes immediately. With the edit buffer, other forms see the old values until Save is clicked.

This is actually correct behavior for a service-call model — you don't see uncommitted changes from other users. But it's a behavioral change from the current app.
