# Design Document: API Blueprint & Survey Linkage

## Overview

This feature extends the existing asset sync pipeline to extract Blueprint entities and Survey linkages from colony warehouse items. When the game API returns cargo items with blueprint properties (typeC="Bp" or "S") or survey items (typeC="Sc"), the system will create or update corresponding domain entities and link warehouse items back to them via BaseItemTypeID. Hull items arrive as typeC="S" with shipPartType="Hu" — there is no separate "SH" typeC code from the game API.

The design introduces two new service classes — `BlueprintLinkageService` and `SurveyLinkageService` — invoked from `ColonyMergeService.MergeWarehouse` after each item is processed. A new `PropertyTypeDefinition` model and registry on `EmpireContext` captures property metadata learned from the API.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     ColonyMergeService                           │
│  MergeWarehouse(apiItems, colony)                               │
│    ├── AssetMergeService.ProcessSingleAssetItemAndReturnUUID()  │
│    ├── BlueprintLinkageService.ProcessItem()  ← NEW             │
│    └── SurveyLinkageService.ProcessItem()     ← NEW             │
└─────────────────────────────────────────────────────────────────┘
         │                          │
         ▼                          ▼
┌──────────────────────┐   ┌──────────────────────┐
│ BlueprintLinkageService│   │ SurveyLinkageService │
│  - PlayerContext      │   │  - PlayerContext      │
│  - EmpireContext      │   │  - ownerUUID          │
│  - BlueprintService   │   └──────────────────────┘
│  - ownerUUID          │            │
└──────────────────────┘            ▼
         │                   ┌──────────────┐
         ▼                   │ Survey entity │
┌──────────────────────┐     └──────────────┘
│ Blueprint entity      │
│ PropertyBag (enriched)│
└──────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ EmpireContext                 │
│  PropertyTypeRegistry        │
│  (List<PropertyTypeDefinition>)│
└──────────────────────────────┘
```

### Data Flow

```
Game API Response
    │
    ▼
ColonyMergeService.MergeWarehouse
    │
    ├── For each apiItem:
    │     AssetMergeService.ProcessSingleAssetItemAndReturnUUID()
    │       → creates/updates Item in colony.Items
    │       → returns UUID of touched item
    │
    ├── After item processing, for items with typeC in {"Bp","S"} and non-empty properties:
    │     BlueprintLinkageService.ProcessItem(apiItem, localItem, ownerUUID)
    │       → extracts property type metadata → PropertyTypeRegistry
    │       → builds candidate Blueprint from API data
    │       → FindUnambiguousMatch against player+global lists
    │       → creates or updates Blueprint entity
    │       → sets localItem.BaseItemTypeID = blueprint.UUID
    │
    └── For items with typeC="Sc":
          SurveyLinkageService.ProcessItem(apiItem, localItem, ownerUUID)
            → parses planet name from resourceName
            → finds or creates stub Survey entity
            → sets localItem.BaseItemTypeID = survey.UUID
```

## Components and Interfaces

### BlueprintLinkageService

**File:** `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs`

**Responsibilities:** Creates or updates Blueprint entities from API cargo items with properties. Registers property type metadata in the global registry. Links warehouse items to their source blueprint via BaseItemTypeID.

**Interface:**
```csharp
public class BlueprintLinkageService
{
    public BlueprintLinkageService(PlayerContext playerContext, EmpireContext empireContext);
    public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID);
}
```

**Dependencies:** PlayerContext, EmpireContext, BlueprintService (static methods only)

### SurveyLinkageService

**File:** `OE2EmpireTracker.Common/Services/SurveyLinkageService.cs`

**Responsibilities:** Parses planet names from survey cargo items, finds or creates Survey entities, and links warehouse items to them via BaseItemTypeID.

**Interface:**
```csharp
public class SurveyLinkageService
{
    public SurveyLinkageService(PlayerContext playerContext);
    public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID);
}
```

**Dependencies:** PlayerContext

### ColonyMergeService (Modified)

**File:** `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`

**Change:** Add optional linkage service parameters to `MergeWarehouse`. Invoke them after each item is processed.

**Updated signature:**
```csharp
public static bool MergeWarehouse(
    List<GameApiAssetCargoItem> apiItems,
    Colony colony,
    BlueprintLinkageService blueprintLinkage = null,
    SurveyLinkageService surveyLinkage = null);
```

### EmpireContext (Modified)

**File:** `OE2EmpireTracker.Common/Services/EmpireContext.cs`

**New members:**
```csharp
public IReadOnlyList<PropertyTypeDefinition> PropertyTypeRegistry { get; }
public PropertyTypeDefinition FindPropertyType(int modTypeId);
public void UpsertPropertyType(PropertyTypeDefinition definition);
```


## Data Models

### PropertyTypeDefinition (New)

**File:** `OE2EmpireTracker.Common/Models/PropertyTypeDefinition.cs`

```csharp
public class PropertyTypeDefinition
{
    [JsonProperty("modTypeId")]
    public int ModTypeId { get; set; }

    [JsonProperty("propertyName")]
    public string PropertyName { get; set; } = string.Empty;

    [JsonProperty("friendlyPropertyName")]
    public string FriendlyPropertyName { get; set; } = string.Empty;

    [JsonProperty("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonProperty("researchPositive")]
    public bool ResearchPositive { get; set; }

    [JsonProperty("canResearch")]
    public bool CanResearch { get; set; }
}
```

Stored in BaselineData.json as `PropertyType[]` array. Keyed by ModTypeId in the runtime cache.

### GameApiAssetItemProperty (Modified)

**File:** `OE2EmpireTracker.Common/Models/GameApiAssetResponse.cs`

Add three fields to the existing DTO:

```csharp
[JsonProperty("originalPropertyValue")]
public decimal OriginalPropertyValue { get; set; }

[JsonProperty("researchPositive")]
public bool ResearchPositive { get; set; }

[JsonProperty("canResearch")]
public bool CanResearch { get; set; }
```

Defaults: OriginalPropertyValue=0, ResearchPositive=false, CanResearch=false (safe for items that omit these fields).

### ItemProperty (Modified)

**File:** `OE2EmpireTracker.Common/Models/ItemProperty.cs`

Add fields to persist enriched data on local items:

```csharp
[JsonProperty("originalPropertyValue")]
public string OriginalPropertyValue { get; set; } = string.Empty;

[JsonProperty("researchPositive")]
public bool ResearchPositive { get; set; }

[JsonProperty("canResearch")]
public bool CanResearch { get; set; }
```

### PropertyBag Enrichment Convention

The existing `PropertyBag` stores `Dictionary<string, string>`. To store both current and original values without breaking existing consumers, we use a paired-key convention:

- Current value: key = `propertyName` (e.g. `"Defence"`) → value = `"45.5"`
- Original value: key = `_orig_{propertyName}` (e.g. `"_orig_Defence"`) → value = `"30.0"`

This leverages the existing `_` prefix convention for internal properties (already preserved by `BlueprintService.UpdateExisting`). Existing code that reads `Properties.GetDecimal("Defence", ...)` continues to work unchanged.

### BaselineRoot (Modified)

**File:** `OE2EmpireTracker.Common/Services/BaselineRoot.cs`

```csharp
public PropertyTypeDefinition[] PropertyType { get; set; }
```

## Detailed Design

### BlueprintLinkageService Implementation

```csharp
public class BlueprintLinkageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly PlayerContext _playerContext;
    private readonly EmpireContext _empireContext;

    public BlueprintLinkageService(PlayerContext playerContext, EmpireContext empireContext)
    {
        _playerContext = playerContext;
        _empireContext = empireContext;
    }

    public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID)
    {
        // 1. Skip if no properties
        if (apiItem.Properties == null || apiItem.Properties.Count == 0)
            return false;

        // 2. Register property type metadata
        RegisterPropertyTypes(apiItem.Properties);

        // 3. Build candidate blueprint
        var candidate = BuildCandidateBlueprint(apiItem, ownerUUID);

        // 4. Determine routing (global vs player)
        bool isGlobal = BlueprintService.IsGlobalRoute(
            candidate.Evolution, !string.IsNullOrEmpty(ownerUUID));
        var searchList = isGlobal
            ? _empireContext.GlobalBlueprintList
            : _playerContext.GetCurrentPlayerBlueprints();

        // 5. Find existing match
        var existing = BlueprintService.FindUnambiguousMatch(searchList, candidate);

        // 6. Create or update
        Blueprint target;
        if (existing != null)
        {
            target = existing;
            MergeProperties(target, apiItem.Properties);
            Log.Info("BlueprintLinkage: updated '{0}' UUID={1}, {2} properties",
                target.Name, target.UUID, apiItem.Properties.Count);
        }
        else
        {
            candidate.UUID = Guid.NewGuid().ToString();
            candidate.OwnerUUID = isGlobal ? string.Empty : ownerUUID;
            BuildPropertyBag(candidate, apiItem.Properties);

            if (isGlobal)
                _empireContext.AddGlobalBlueprint(candidate);
            else
                _playerContext.AddBlueprint(candidate);

            target = candidate;
            Log.Info("BlueprintLinkage: created '{0}' UUID={1} type={2} evo={3}",
                target.Name, target.UUID, target.BluePrintType, target.Evolution);
        }

        // 7. Link item to blueprint
        localItem.BaseItemTypeID = target.UUID;

        return true;
    }
}
```

#### Key Methods

**BuildCandidateBlueprint(apiItem, ownerUUID):**
- For typeC="Bp": Name = resourceName, Evolution = apiItem.Evolution ?? 0, BluePrintType = ClassifyBlueprintType(apiItem)
- For typeC="S"/"SH": Name = resourceName, Evolution = apiItem.Evolution ?? 0, BluePrintType = MapShipPartType(apiItem.ShipPartType)
- Class and TechLevel default to 0 and empty (API doesn't provide these directly; dedup matching uses defaults)

**ClassifyBlueprintType(apiItem):**
- If shipPartType == "Hu" → "Hull" (hull-category)
- If resourceName contains "Flatpack" → "Flatpacks/{structureType}" (parsed from name)
- Otherwise → MapShipPartType(apiItem.ShipPartType) or empty string

**MapShipPartType(shipPartType):**

| API ShipPartType | BlueprintTypes Constant | Notes |
|-----------------|------------------------|-------|
| "Sh" | `BlueprintTypes.Shield` | Shield |
| "Re" | `BlueprintTypes.Reactor` | Reactor |
| "Nc" | `BlueprintTypes.NavComp` | Nav Computer |
| "Jd" | `BlueprintTypes.JumpDrive` | Jump Drive |
| "Oh" | `BlueprintTypes.OreHopper` | Ore Hopper |
| "Ml" | `BlueprintTypes.MiningLaser` | Mining Laser |
| "Ag" | `BlueprintTypes.AsteroidGrapple` | Asteroid Grapple |
| "Be" | `BlueprintTypes.Beamer` | Beamer weapon |
| "Cg" | `BlueprintTypes.Coilgun` | Coilgun weapon |
| "Rg" | `BlueprintTypes.Railgun` | Railgun weapon |
| "Ms" | `BlueprintTypes.MissileLauncher` | Missile Launcher |
| "Tp" | `BlueprintTypes.TorpedoLauncher` | Torpedo Launcher |
| "Hu" | "Hull" | Hull (size appended if known) |
| unknown | raw value (logged as warning) | Fallback |


**MergeProperties(existing, apiProperties):**
- For each API property: set `propertyName` → `propertyValue.ToString()` in PropertyBag
- For each API property: set `_orig_{propertyName}` → `originalPropertyValue.ToString()` in PropertyBag
- Does NOT remove existing properties not in the API response (additive merge per Req 6)
- Preserves Resources, CopyCost, NickName, BaseBlueprintUUID (never touches them)

**BuildPropertyBag(blueprint, apiProperties):**
- Same as MergeProperties but starts from a fresh PropertyBag

**RegisterPropertyTypes(apiProperties):**
- For each property in the list, upserts into `_empireContext.PropertyTypeRegistry` keyed by ModTypeId
- Creates `PropertyTypeDefinition` with all metadata fields

### SurveyLinkageService Implementation

```csharp
public class SurveyLinkageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex SurveyNameRegex = new Regex(
        @"^Survey Report:\s*(.+?)\s*\(([A-Fa-f0-9]+)\)$",
        RegexOptions.Compiled);

    private readonly PlayerContext _playerContext;

    public SurveyLinkageService(PlayerContext playerContext)
    {
        _playerContext = playerContext;
    }

    public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID)
    {
        var match = SurveyNameRegex.Match(apiItem.ResourceName ?? string.Empty);
        if (!match.Success)
        {
            Log.Warn("SurveyLinkage: resourceName '{0}' does not match expected format, skipping",
                apiItem.ResourceName);
            return false;
        }

        string planetName = match.Groups[1].Value.Trim();
        string hexCode = match.Groups[2].Value;

        // Find existing survey by planet name (case-insensitive)
        var surveys = _playerContext.GetCurrentPlayerSurveys();
        var existing = surveys.FirstOrDefault(s =>
            string.Equals(s.PlanetName, planetName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            localItem.BaseItemTypeID = existing.UUID;
            Log.Info("SurveyLinkage: linked to existing survey '{0}' UUID={1}",
                planetName, existing.UUID);
            return true;
        }

        // Create stub survey
        var stub = new Survey
        {
            UUID = Guid.NewGuid().ToString(),
            PlanetName = planetName,
            SurveyID = hexCode,
            OwnerUUID = ownerUUID,
            Name = apiItem.ResourceName,
        };

        _playerContext.AddSurvey(stub);
        localItem.BaseItemTypeID = stub.UUID;
        Log.Info("SurveyLinkage: created stub survey '{0}' hex={1} UUID={2}",
            planetName, hexCode, stub.UUID);
        return true;
    }
}
```

### ColonyMergeService Integration

Modify `MergeWarehouse` to accept service instances and invoke linkage after item processing:

```csharp
public static bool MergeWarehouse(
    List<GameApiAssetCargoItem> apiItems,
    Colony colony,
    BlueprintLinkageService blueprintLinkage = null,
    SurveyLinkageService surveyLinkage = null)
{
    // ... existing null checks and initialization ...

    foreach (var apiItem in apiItems)
    {
        try
        {
            string touchedUUID = AssetMergeService.ProcessSingleAssetItemAndReturnUUID(apiItem, colony.Items);
            if (touchedUUID != null)
            {
                touchedUUIDs.Add(touchedUUID);

                // Blueprint linkage for Bp/S items with properties
                if (blueprintLinkage != null && HasBlueprintProperties(apiItem))
                {
                    var localItem = colony.Items.FindByUUID(touchedUUID);
                    if (localItem != null)
                    {
                        blueprintLinkage.ProcessItem(apiItem, localItem, ownerUUID);
                    }
                }

                // Survey linkage for Sc items
                if (surveyLinkage != null && IsSurveyItem(apiItem))
                {
                    var localItem = colony.Items.FindByUUID(touchedUUID);
                    if (localItem != null)
                    {
                        surveyLinkage.ProcessItem(apiItem, localItem, ownerUUID);
                    }
                }
            }

            hasChanges = true;
        }
        catch (Exception ex) { /* existing error handling */ }
    }

    // ... existing untouched-item cleanup ...
}

private static bool HasBlueprintProperties(GameApiAssetCargoItem apiItem)
{
    var typeC = apiItem.TypeC?.Trim();
    if (string.IsNullOrEmpty(typeC)) return false;

    bool isBlueprintType = string.Equals(typeC, "Bp", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeC, "S", StringComparison.OrdinalIgnoreCase);

    return isBlueprintType && apiItem.Properties != null && apiItem.Properties.Count > 0;
}

private static bool IsSurveyItem(GameApiAssetCargoItem apiItem)
{
    return string.Equals(apiItem.TypeC?.Trim(), "Sc", StringComparison.OrdinalIgnoreCase);
}
```

The optional parameters maintain backward compatibility — existing callers that don't pass linkage services get the current behavior unchanged.

### EmpireContext PropertyTypeRegistry

```csharp
private List<PropertyTypeDefinition> _propertyTypeRegistry = new List<PropertyTypeDefinition>();
private Dictionary<int, PropertyTypeDefinition> _propertyTypeCache;

public IReadOnlyList<PropertyTypeDefinition> PropertyTypeRegistry => _propertyTypeRegistry;

public PropertyTypeDefinition FindPropertyType(int modTypeId)
{
    if (_propertyTypeCache == null)
    {
        _propertyTypeCache = _propertyTypeRegistry.ToDictionary(p => p.ModTypeId);
    }
    _propertyTypeCache.TryGetValue(modTypeId, out var result);
    return result;
}

public void UpsertPropertyType(PropertyTypeDefinition definition)
{
    if (_propertyTypeCache == null)
    {
        _propertyTypeCache = _propertyTypeRegistry.ToDictionary(p => p.ModTypeId);
    }

    if (_propertyTypeCache.TryGetValue(definition.ModTypeId, out var existing))
    {
        existing.PropertyName = definition.PropertyName;
        existing.FriendlyPropertyName = definition.FriendlyPropertyName;
        existing.Unit = definition.Unit;
        existing.ResearchPositive = definition.ResearchPositive;
        existing.CanResearch = definition.CanResearch;
    }
    else
    {
        _propertyTypeRegistry.Add(definition);
        _propertyTypeCache[definition.ModTypeId] = definition;
    }
}

private void InitPropertyTypes(BaselineRoot root)
{
    _propertyTypeRegistry = root.PropertyType != null
        ? new List<PropertyTypeDefinition>(root.PropertyType)
        : new List<PropertyTypeDefinition>();
    _propertyTypeCache = null;
}
```

### AssetMergeService.MapProperties Update

Update to include the new fields from the enriched DTO:

```csharp
private static List<ItemProperty> MapProperties(List<GameApiAssetItemProperty> apiProperties)
{
    if (apiProperties == null || apiProperties.Count == 0)
        return new List<ItemProperty>();

    var result = new List<ItemProperty>(apiProperties.Count);
    foreach (var prop in apiProperties)
    {
        result.Add(new ItemProperty
        {
            ModTypeId = prop.ModTypeId,
            PropertyName = prop.PropertyName ?? string.Empty,
            FriendlyPropertyName = prop.FriendlyPropertyName ?? string.Empty,
            PropertyValue = prop.PropertyValue.ToString(),
            OriginalPropertyValue = prop.OriginalPropertyValue.ToString(),
            Unit = prop.Unit ?? string.Empty,
            ResearchPositive = prop.ResearchPositive,
            CanResearch = prop.CanResearch,
        });
    }

    return result;
}
```

Also update `ArePropertiesEqual` to compare the new fields (OriginalPropertyValue, ResearchPositive, CanResearch).


## Correctness Properties

### Property 1: Idempotency

**Validates: Requirements 11.1**

Running `BlueprintLinkageService.ProcessItem` with the same API data multiple times produces exactly one Blueprint entity per unique dedup key. The second and subsequent calls update the existing entity rather than creating duplicates.

**Formal:** For all apiItem, for all n >= 1: |{bp in Blueprints | dedupKey(bp) = dedupKey(apiItem)}| = 1 after n invocations.

### Property 2: Survey Linkage Uniqueness

**Validates: Requirements 11.2**

Running `SurveyLinkageService.ProcessItem` with the same survey item multiple times links to the same Survey entity. No duplicate stub surveys are created.

**Formal:** For all surveyItem, for all n >= 1: |{s in Surveys | s.PlanetName =i parsedPlanetName(surveyItem)}| = 1 after n invocations.

### Property 3: BaseItemTypeID Consistency

**Validates: Requirements 5.1, 5.2**

After linkage, every Blueprint/ShipPart/ShipHull item with non-empty properties has BaseItemTypeID set to a valid Blueprint UUID. Every Survey item that matches the name format has BaseItemTypeID set to a valid Survey UUID.

**Formal:** For all item where typeC in {"Bp","S"} and properties.Count > 0: exists bp in Blueprints where bp.UUID = item.BaseItemTypeID.

### Property 4: Property Preservation

**Validates: Requirements 6.1, 6.2**

When updating an existing Blueprint, the linkage service never removes properties not present in the API response. It only adds or updates properties that the API provides.

**Formal:** For all key in existing.Properties.Keys where key not in apiPropertyNames: key in updated.Properties.Keys (preserved).

### Property 5: PropertyTypeRegistry Completeness

**Validates: Requirements 7.1, 7.5**

After processing any item with properties, every modTypeId in the item's properties array exists in the PropertyTypeRegistry.

**Formal:** For all prop in processedItem.Properties: exists def in PropertyTypeRegistry where def.ModTypeId = prop.ModTypeId.

### Property 6: No-Op on Empty Properties

**Validates: Requirements 1.6, 2.5**

Items with empty properties arrays never trigger blueprint creation or modification.

**Formal:** For all apiItem where apiItem.Properties.Count = 0: Blueprints' = Blueprints (unchanged).

### Property 7: Ownership Correctness

**Validates: Requirements 1.4, 10.3**

Blueprints created with Evolution > 0 are assigned to the current player. Blueprints with Evolution = 0 are stored globally.

**Formal:** For all bp created by linkage: (bp.Evolution > 0 implies bp.OwnerUUID = currentPlayerUUID) and (bp.Evolution = 0 implies bp.OwnerUUID = "").

## Error Handling

1. **Single-item failure isolation:** If `BlueprintLinkageService.ProcessItem` throws for one item, the exception is caught in `ColonyMergeService.MergeWarehouse`'s existing try/catch. Processing continues for remaining items. The error is logged at Error level.

2. **Malformed survey names:** If the survey resourceName doesn't match the regex, a warning is logged and the item is skipped (BaseItemTypeID retains its default name-based value).

3. **Unknown shipPartType:** If the shipPartType code can't be mapped, a warning is logged and the raw value is used as BluePrintType. The blueprint is still created.

4. **Duplicate UUID on AddBlueprint:** The existing `PlayerContext.AddBlueprint` throws `InvalidOperationException` on duplicate UUIDs. Since we generate fresh GUIDs, this should never happen. If it does, the exception propagates to the per-item catch block and is logged.

## Testing Strategy

### Unit Tests (NUnit + FsCheck 2.16.6)

**BlueprintLinkageServiceTests:**
- `ProcessItem_WithBlueprintProperties_CreatesNewBlueprint` — verifies creation with correct fields
- `ProcessItem_WithExistingMatch_UpdatesProperties` — verifies additive merge
- `ProcessItem_WithEmptyProperties_ReturnsFalseNoCreation` — verifies no-op
- `ProcessItem_SetsBaseItemTypeID_ToCreatedBlueprintUUID` — verifies linkage
- `ProcessItem_ShipPart_MapsShipPartTypeCorrectly` — verifies type classification
- `ProcessItem_UnknownShipPartType_UsesRawValueAndLogsWarning` — verifies fallback
- `ProcessItem_RegistersPropertyTypes_InRegistry` — verifies registry population

**SurveyLinkageServiceTests:**
- `ProcessItem_MatchesExistingSurvey_LinksByUUID` — verifies existing match
- `ProcessItem_NoExistingSurvey_CreatesStub` — verifies stub creation
- `ProcessItem_MalformedName_SkipsAndLogsWarning` — verifies error handling
- `ProcessItem_CaseInsensitiveMatch_FindsExisting` — verifies case handling

**Property-Based Tests (FsCheck):**
- `Idempotency_Property` — process same item N times, assert exactly one blueprint exists
- `SurveyIdempotency_Property` — process same survey N times, assert exactly one survey exists
- `PropertyPreservation_Property` — existing properties not in API are never removed
- `OwnershipRouting_Property` — evolution routing matches IsGlobalRoute logic

### Integration Points
- Existing `ColonyMergeService` tests extended to verify linkage services are invoked
- Existing `AssetMergeService` tests verify new ItemProperty fields are mapped

## File Change Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `OE2EmpireTracker.Common/Models/PropertyTypeDefinition.cs` | NEW | Property type metadata model |
| `OE2EmpireTracker.Common/Models/GameApiAssetResponse.cs` | MODIFY | Add 3 fields to GameApiAssetItemProperty |
| `OE2EmpireTracker.Common/Models/ItemProperty.cs` | MODIFY | Add 3 fields for enriched data |
| `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs` | NEW | Blueprint creation/update from API items |
| `OE2EmpireTracker.Common/Services/SurveyLinkageService.cs` | NEW | Survey linkage from API items |
| `OE2EmpireTracker.Common/Services/ColonyMergeService.cs` | MODIFY | Invoke linkage services in MergeWarehouse |
| `OE2EmpireTracker.Common/Services/AssetMergeService.cs` | MODIFY | Update MapProperties and ArePropertiesEqual |
| `OE2EmpireTracker.Common/Services/EmpireContext.cs` | MODIFY | Add PropertyTypeRegistry collection + methods |
| `OE2EmpireTracker.Common/Services/BaselineRoot.cs` | MODIFY | Add PropertyType array |
| `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs` | NEW | Unit + property tests |
| `OE2EmpireTracker.Tests/Services/SurveyLinkageServiceTests.cs` | NEW | Unit + property tests |

## Design Decisions

### D1: Paired-key convention for original values
**Decision:** Store original property values as `_orig_{propertyName}` in the existing PropertyBag rather than introducing a new data structure.
**Rationale:** Minimizes changes to the Blueprint model and serialization. The `_` prefix convention is already understood by `BlueprintService.UpdateExisting` (preserved during imports). Existing consumers that read properties by name are unaffected.

### D2: Optional parameters on MergeWarehouse
**Decision:** Add linkage services as optional parameters (defaulting to null) rather than requiring them.
**Rationale:** Maintains backward compatibility with all existing callers. Station and ship cargo merges (which don't need linkage) continue to work unchanged. Only the colony warehouse sync path passes the services.

### D3: Additive property merge (API wins, no removal)
**Decision:** When updating an existing blueprint, add/overwrite API-provided properties but never remove properties the API doesn't mention.
**Rationale:** The HTML scanner may have captured properties the API doesn't include (e.g. internal tracking properties). Removing them would lose data. The API is authoritative for values it provides, but absence doesn't mean deletion.

### D4: Stub surveys with minimal data
**Decision:** When no matching survey exists, create a stub with only PlanetName, SurveyID, OwnerUUID, and UUID. Don't attempt to populate resources or scan metadata.
**Rationale:** The API cargo item contains no survey resource data — only the name. A stub allows linkage now; the full survey data will be populated when the player imports the actual survey HTML or when a future API endpoint provides survey details.

### D5: Fresh GUID for player blueprints, not DeterministicUUID
**Decision:** Use `Guid.NewGuid()` for all linkage-created blueprints rather than `DeterministicUUID.Generate`.
**Rationale:** `DeterministicUUID.Generate` is designed for global (Evo 0) blueprints where the same base blueprint should have the same UUID across all players. For player-owned evolved blueprints, each instance is unique. For global blueprints created by linkage, we also use fresh GUIDs because the dedup-key matching already prevents duplicates — deterministic UUIDs would only matter if we needed cross-player dedup, which the existing `FindUnambiguousMatch` already handles.
