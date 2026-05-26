# Design Document

## Overview

This document describes the technical design for extending the existing game API sync infrastructure to retrieve and merge colony data. The colony sync adds three new API client methods (colony list, buildings, warehouse), a set of response DTOs, new sub-model classes, a data model migration, and a `ColonyMergeService` that implements dedup matching and field-level merge with "API wins" strategy.

The local data model stores ALL fields from the game API. Where the current model and the game API disagree on field location, type, or existence, the game API model wins. This requires expanding Colony, ColonyStructure, and Item with new properties, adding sub-model classes for nested API objects, and migrating WorkerCurrentAttitude/ContentmentIndex from ColonyStructure to Colony.

The design follows the established patterns from the profile sync implementation:
- GameApiClient methods return `(bool Success, string Json)` tuples with status-code-based error signaling
- The scheduler orchestrates fetch → deserialize → merge → persist → notify
- Merge logic is a static/internal method on the service class for testability
- Polly policies (retry, circuit breaker, rate limiter) are reused from the existing client

Colony sync runs after profile sync in the same polling cycle. It fetches the colony list first, then conditionally fetches per-colony details (buildings, warehouse) only for colonies with `RemoteAccess > 0`. Scope availability is detected lazily from the first HTTP 403 response and cached for the remainder of the cycle.

## Implementation Phasing

This feature is implemented in two phases with a discovery gate between them.

### Phase 1: Infrastructure + Discovery (implement first)
- Data model expansion (new properties on Colony, ColonyStructure, Item + sub-models)
- Data model migration (CurrentAttitude/ContentmentIndex relocation)
- Colony response DTOs
- GameApiClient colony endpoint methods
- API Data Discovery test fixture
- **GATE: Run discovery tests, produce mapping report, confirm typeC/typeId mappings**

### Phase 2: Merge Logic + Integration (replan after discovery)
- ColonyMergeService (dedup, field merge, building merge, warehouse merge)
- GameApiSyncScheduler colony sync integration
- Persistence and UI notification
- Error handling and scope awareness
- All property-based and unit tests for merge logic

Phase 2 tasks are provisional. The mapping report from Phase 1 may reveal:
- Different typeC codes than assumed
- Additional fields not in the swagger schema
- Data format surprises (e.g. unexpected null patterns, enum values)
- Building name mismatches that affect dedup strategy

The tasks.md will mark Phase 2 tasks as dependent on the discovery gate.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                   GameApiSyncScheduler                            │
│  SyncCharacterAsync() → profile sync → colony sync              │
└───────┬─────────────────────────────────┬───────────────────────┘
        │                                 │
        │ GetColonyListAsync()            │ ColonyMergeService.MergeColonyList()
        │ GetColonyBuildingsAsync()       │ ColonyMergeService.MergeBuildings()
        │ GetColonyWarehouseAsync()       │ ColonyMergeService.MergeWarehouse()
        │                                 │
┌───────▼───────┐               ┌─────────▼──────────────────────┐
│ GameApiClient │               │ ColonyMergeService              │
│ (HTTP + Polly)│               │ (dedup + field merge + logging) │
└───────────────┘               └─────────┬──────────────────────┘
                                          │
                                ┌─────────▼──────────┐
                                │ PlayerContext       │
                                │ (colonies, persist) │
                                └────────────────────┘
```

**Colony Sync Flow (per character):**
1. Scheduler calls `GetColonyListAsync()` after successful profile sync
2. On HTTP 401 → invalidate token, transition to DisconnectedInvalidKey (same as profile)
3. On HTTP 403 → log scope missing, skip colony sync entirely
4. On success → deserialize into `GameApiColonyListResponse`
5. Invoke `ColonyMergeService.MergeColonyList()` for dedup + field merge
6. For each colony with `RemoteAccess > 0`:
   - Call `GetColonyBuildingsAsync(colonyId)` → merge buildings
   - Call `GetColonyWarehouseAsync(colonyId)` → merge warehouse
7. If any colony was created/updated → `WriteContext()` + raise `ColonyDataChanged`


## Components and Interfaces

### 1. GameApiClient — Colony Endpoint Methods

**File:** `OE2EmpireTracker.Common/Client/GameApiClient.cs`
**Satisfies:** Req 1, Req 2, Req 3

Three new async methods following the same pattern as `GetCharacterAsync`:

```csharp
public async Task<(bool Success, string Json)> GetColonyListAsync(string appId, string accessToken);
public async Task<(bool Success, string Json)> GetColonyBuildingsAsync(string appId, string accessToken, int colonyId);
public async Task<(bool Success, string Json)> GetColonyWarehouseAsync(string appId, string accessToken, int colonyId);
```

**Behavior (identical for all three):**
- Validates `accessToken` is not null/empty → returns `(false, null)` if invalid
- Calls `AcquireRateLimitTokenAsync()` before making the request
- Calls `ExecuteWithPoliciesAsync()` with the appropriate URL
- On HTTP 200: returns `(true, json)`
- On HTTP 401: returns `(false, "401")`
- On HTTP 403: returns `(false, "403")`
- On HTTP 404 (buildings/warehouse only): returns `(false, "404")`
- On `BrokenCircuitException` / `HttpRequestException` / `TaskCanceledException`: returns `(false, null)`
- Transient errors handled by existing Polly retry policy

### 2. Colony Response DTOs

**File:** `OE2EmpireTracker.Common/Models/GameApiColonyResponse.cs`
**Satisfies:** Req 4

All DTOs in a single file using `[JsonProperty]` attributes for explicit JSON field mapping.

#### GameApiColonyListItem (23 properties)

| Property | Type | JSON Field |
|----------|------|-----------|
| ColonyId | int | colonyId |
| ColonyName | string | colonyName |
| SystemObjectName | string | systemObjectName |
| SystemName | string | systemName |
| SystemId | int | systemId |
| ColonySize | int | colonySize |
| RemoteAccess | int | remoteAccess |
| Distance | double | distance |
| SurfaceVariation | int | surfaceVariation |
| AtmosVariation | int | atmosVariation |
| HexValue | string | hexValue |
| SystemObjectTypeName | string | systemObjectTypeName |
| ImagePreFix | string | imagePreFix |
| HasManufacturing | int | hasManufacturing |
| ManufacturingInProgress | int | manufacturingInProgress |
| HasMining | int | hasMining |
| MiningInProgress | int | miningInProgress |
| HasRefining | int | hasRefining |
| RefiningInProgress | int | refiningInProgress |
| HasResearch | int | hasResearch |
| ResearchInProgress | int | researchInProgress |
| ManufacturingBlocked | int | manufacturingBlocked |
| WorkerCurrentAttitude | int | workerCurrentAttitude |
| ContentmentIndex | int | contentmentIndex |

#### GameApiColonyBuilding (21 properties + nested collections)

| Property | Type | JSON Field |
|----------|------|-----------|
| BuildingId | int | buildingId |
| ColonyBuildingTypeId | int | colonyBuildingTypeId |
| BlueprintDesignName | string | blueprintDesignName |
| BuildingOnline | bool | buildingOnline |
| StatusId | int | statusId |
| ConstructingBuildingFinish | DateTime? | constructingBuildingFinish |
| ResourceId | int | resourceId |
| ResourceIcon | string | resourceIcon |
| ResourceName | string | resourceName |
| MaxRate | double | maxRate |
| NextFinish | DateTime? | nextFinish |
| ManufactureNumber | int | manufactureNumber |
| ManufactureAmountPerRun | int | manufactureAmountPerRun |
| DurabilityCurrent | double | durabilityCurrent |
| DurabilityMax | double | durabilityMax |
| OpsStatusEffects | List&lt;GameApiBuildingStatusEffect&gt; | opsStatusEffects |
| Industries | List&lt;GameApiBuildingIndustry&gt; | industries |
| DetailsRequired | List&lt;GameApiBuildingDetailRequirement&gt; | detailsRequired |
| SupportDetailsRequired | List&lt;GameApiBuildingDetailRequirement&gt; | supportDetailsRequired |
| BuildingAttributes | List&lt;GameApiBuildingAttribute&gt; | buildingAttributes |
| ExtraProperties | List&lt;GameApiBuildingExtraProperty&gt; | extraProperties |

#### GameApiWarehouseItem (assetCargoItem schema, 17 properties)

| Property | Type | JSON Field |
|----------|------|-----------|
| Id | int? | Id |
| TypeId | int | typeId |
| Amount | int | amount |
| ResourceName | string | resourceName |
| TypeC | string | typeC |
| Icon | string | icon |
| JobRef | int? | jobRef |
| JobDeliveryLoc | int? | jobDeliveryLoc |
| HealthPercentage | double? | healthPercentage |
| LastRepairHealthPercentage | double? | lastRepairHealthPercentage |
| Evolution | int? | evolution |
| Mass | double? | mass |
| Volume | double? | volume |
| Properties | List&lt;GameApiItemProperty&gt; | properties |
| JobName | string | jobName |
| JobTrack | string | jobTrack |
| ShipPartType | string | shipPartType |

#### Sub-DTOs

```csharp
public class GameApiBuildingStatusEffect { int StatusId; int ModTypeId; double Change; }
public class GameApiBuildingIndustry { int Id; string Name; }
public class GameApiBuildingDetailRequirement { /* fields from swagger */ }
public class GameApiBuildingAttribute { int ModTypeId; string PropertyName; string FriendlyPropertyName; string PropertyValue; string Unit; }
public class GameApiBuildingExtraProperty { string Info1; string Info2; string Info3; string Info4; }
public class GameApiItemProperty { int ModTypeId; string PropertyName; string FriendlyPropertyName; string PropertyValue; string Unit; }
```


### 3. Data Model Expansion

**Satisfies:** Req 17, Req 18

#### Colony Model — New Properties

**File:** `OE2EmpireTracker.Common/Models/Colony.cs`

```csharp
// New API-sourced fields (Req 17.1)
public int SystemId { get; set; }
public int ColonySize { get; set; }
public decimal Distance { get; set; }
public int SurfaceVariation { get; set; }
public int AtmosVariation { get; set; }
public string HexValue { get; set; } = string.Empty;
public string SystemObjectTypeName { get; set; } = string.Empty;
public string ImagePreFix { get; set; } = string.Empty;
public int ManufacturingBlocked { get; set; }

// Migrated from ColonyStructure (Req 18.1, 18.2)
public int WorkerCurrentAttitude { get; set; }
public int ContentmentIndex { get; set; }
```

#### ColonyStructure Model — New Properties

**File:** `OE2EmpireTracker.Common/Models/ColonyStructure.cs`

```csharp
// New API-sourced fields (Req 17.2)
public int ColonyBuildingTypeId { get; set; }
public int ResourceId { get; set; }
public string ResourceIcon { get; set; } = string.Empty;
public int ManufactureAmountPerRun { get; set; }
public decimal DurabilityCurrent { get; set; }
public decimal DurabilityMax { get; set; }
public List<BuildingStatusEffect> OpsStatusEffects { get; set; } = new List<BuildingStatusEffect>();
public List<BuildingIndustry> Industries { get; set; } = new List<BuildingIndustry>();
public List<BuildingDetailRequirement> DetailsRequired { get; set; } = new List<BuildingDetailRequirement>();
public List<BuildingDetailRequirement> SupportDetailsRequired { get; set; } = new List<BuildingDetailRequirement>();
public List<BuildingAttribute> BuildingAttributes { get; set; } = new List<BuildingAttribute>();
public List<BuildingExtraProperty> ExtraProperties { get; set; } = new List<BuildingExtraProperty>();

// Deprecated — retained for backward compat deserialization (Req 18.3)
// CurrentAttitude (string) — already exists, will be deprecated
// ContentmentIndex (int) — already exists, will be deprecated
```

#### Item Model — New/Changed Properties

**File:** `OE2EmpireTracker.Common/Models/Item.cs`

```csharp
// Volume remains decimal (no type change needed — project convention)
public decimal Volume { get; set; }

// New API-sourced fields (Req 17.3)
public decimal? Mass { get; set; }
public int? GameItemId { get; set; }
public int? JobRef { get; set; }
public int? JobDeliveryLoc { get; set; }
public decimal? HealthPercentage { get; set; }
public decimal? LastRepairHealthPercentage { get; set; }
public int? Evolution { get; set; }
public string ShipPartType { get; set; } = string.Empty;
public string JobName { get; set; } = string.Empty;
public string JobTrack { get; set; } = string.Empty;
public List<ItemProperty> ItemProperties { get; set; } = new List<ItemProperty>();
```

#### New Sub-Model Classes

**File:** `OE2EmpireTracker.Common/Models/BuildingSubModels.cs`

```csharp
public class BuildingStatusEffect
{
    [JsonProperty("statusId")]
    public int StatusId { get; set; }
    [JsonProperty("modTypeId")]
    public int ModTypeId { get; set; }
    [JsonProperty("change")]
    public decimal Change { get; set; }
}

public class BuildingIndustry
{
    [JsonProperty("Id")]
    public int Id { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public class BuildingDetailRequirement
{
    // Fields from colonyBuildingDetailRequirement swagger schema
}

public class BuildingAttribute
{
    [JsonProperty("modTypeId")]
    public int ModTypeId { get; set; }
    [JsonProperty("propertyName")]
    public string PropertyName { get; set; } = string.Empty;
    [JsonProperty("friendlyPropertyName")]
    public string FriendlyPropertyName { get; set; } = string.Empty;
    [JsonProperty("propertyValue")]
    public string PropertyValue { get; set; } = string.Empty;
    [JsonProperty("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class BuildingExtraProperty
{
    [JsonProperty("info1")]
    public string Info1 { get; set; } = string.Empty;
    [JsonProperty("info2")]
    public string Info2 { get; set; } = string.Empty;
    [JsonProperty("info3")]
    public string Info3 { get; set; } = string.Empty;
    [JsonProperty("info4")]
    public string Info4 { get; set; } = string.Empty;
}
```

**File:** `OE2EmpireTracker.Common/Models/ItemProperty.cs`

```csharp
public class ItemProperty
{
    [JsonProperty("modTypeId")]
    public int ModTypeId { get; set; }
    [JsonProperty("propertyName")]
    public string PropertyName { get; set; } = string.Empty;
    [JsonProperty("friendlyPropertyName")]
    public string FriendlyPropertyName { get; set; } = string.Empty;
    [JsonProperty("propertyValue")]
    public string PropertyValue { get; set; } = string.Empty;
    [JsonProperty("unit")]
    public string Unit { get; set; } = string.Empty;
}
```

### 4. Data Model Migration

**Satisfies:** Req 18

#### Migration Strategy: WorkerCurrentAttitude and ContentmentIndex

The migration from ColonyStructure to Colony uses Newtonsoft.Json's `[OnDeserialized]` callback:

```csharp
// In Colony.cs
[OnDeserialized]
internal void OnDeserializedMethod(StreamingContext context)
{
    MigrateWorkerFields();
}

private void MigrateWorkerFields()
{
    // Only migrate if Colony-level fields are at default (not yet set by API sync)
    if (WorkerCurrentAttitude == 0 && Structures != null)
    {
        var source = Structures.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.CurrentAttitude));
        if (source != null)
        {
            int.TryParse(source.CurrentAttitude, out int parsed);
            WorkerCurrentAttitude = parsed;
        }
    }

    if (ContentmentIndex == 0 && Structures != null)
    {
        var source = Structures.FirstOrDefault(s => s.ContentmentIndex != 0);
        if (source != null)
        {
            ContentmentIndex = source.ContentmentIndex;
        }
    }

    // Clear deprecated fields on structures (Req 18.9)
    if (Structures != null)
    {
        foreach (var s in Structures)
        {
            s.CurrentAttitude = string.Empty;
            s.ContentmentIndex = 0;
        }
    }
}
```

#### ColonyStructure Deprecated Fields

```csharp
// Retained for deserialization of legacy JSON, but excluded from new output
[JsonProperty("currentAttitude")]
[DefaultValue("")]
[Obsolete("Migrated to Colony.WorkerCurrentAttitude")]
public string CurrentAttitude { get; set; } = string.Empty;

[JsonProperty("contentmentIndex")]
[DefaultValue(0)]
[Obsolete("Migrated to Colony.ContentmentIndex")]
public int ContentmentIndex { get; set; }
```

To exclude from serialization, use `ShouldSerialize` pattern:
```csharp
public bool ShouldSerializeCurrentAttitude() => false;
public bool ShouldSerializeContentmentIndex() => false;
```


### 5. ColonyMergeService

**File:** `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`
**Satisfies:** Req 6, Req 7, Req 8, Req 9, Req 10, Req 13, Req 15

```csharp
public static class ColonyMergeService
{
    public static ColonyMergeResult MergeColonyList(
        List<GameApiColonyListItem> apiColonies,
        List<Colony> localColonies,
        string ownerUUID);

    public static bool MergeBuildings(
        List<GameApiColonyBuilding> apiBuildings,
        Colony colony);

    public static bool MergeWarehouse(
        List<GameApiWarehouseItem> apiItems,
        Colony colony);
}

public class ColonyMergeResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public bool HasChanges => Created > 0 || Updated > 0;
    public Dictionary<int, string> ColonyIdToUUIDMap { get; set; } = new Dictionary<int, string>();
}
```

#### MergeColonyList Algorithm

```
FOR each apiColony in apiColonies:
  IF apiColony.SystemObjectName is null or empty:
    Log warning, increment Skipped, CONTINUE

  TRY:
    Find local match by PlanetName + SystemName (case-insensitive, same owner)

    IF match found:
      MergeAllColonyFields(match, apiColony)  // ALL 14 game-authoritative fields
      Set match.LastImportDateTime = SystemClock.UtcNow.ToString("o")
      Add to ColonyIdToUUIDMap
      IF any field changed: increment Updated
    ELSE:
      Create new Colony with new GUID UUID
      Set ALL fields from API (OwnerUUID, PlanetName, SystemName, ColonyName,
        SystemId, ColonySize, Distance, SurfaceVariation, AtmosVariation,
        HexValue, SystemObjectTypeName, ImagePreFix, ManufacturingBlocked,
        WorkerCurrentAttitude, ContentmentIndex)
      Set LastImportDateTime = SystemClock.UtcNow.ToString("o")
      Add to localColonies + ColonyIdToUUIDMap
      Increment Created
  CATCH Exception:
    Log error, increment Skipped, CONTINUE
```

#### MergeAllColonyFields (game-authoritative — Req 7)

For each of the 14 fields (ColonyName, PlanetName, SystemName, SystemId, ColonySize, Distance, SurfaceVariation, AtmosVariation, HexValue, SystemObjectTypeName, ImagePreFix, ManufacturingBlocked, WorkerCurrentAttitude, ContentmentIndex):
- For string fields: IF API value is null or empty → skip (preserve local)
- For numeric fields: always overwrite with API value
- IF value differs from local → log conflict with "API wins" label
- Track whether any field actually changed

#### MergeBuildings Algorithm (Req 8 — 18 criteria)

```
FOR each apiBuilding in apiBuildings:
  Find local match by BlueprintDesignName (case-insensitive)

  IF match found:
    Update Properties["Built"] and Properties["Online"] from API
    Update ColonyBuildingTypeId, ResourceId, ResourceIcon, ManufactureAmountPerRun (API wins)
    Update DurabilityCurrent, DurabilityMax (API wins)
    Replace OpsStatusEffects, Industries, DetailsRequired, SupportDetailsRequired,
      BuildingAttributes, ExtraProperties with API data
    IF apiBuilding.ResourceName not empty AND local MiningSurveyResource is empty:
      Set MiningSurveyResource
    IF apiBuilding.ConstructingBuildingFinish != null AND local BuildCompletionTime is null:
      Set BuildCompletionTime
    Preserve Local_Only_Fields (timers, queue, manufacturing, overflow)
  ELSE:
    Create new ColonyStructure with ALL fields from API
    Add to colony.Structures
```

#### MergeWarehouse Algorithm (Req 9 — 14 criteria)

```
FOR each apiItem in apiItems:
  Find local match by ResourceName + TypeC (case-insensitive)

  IF match found:
    Update Quantity (API wins)
    Update Mass, Volume, HealthPercentage, LastRepairHealthPercentage (API wins)
    Update Evolution, GameItemId (API wins)
    Update JobRef, JobDeliveryLoc, JobName, JobTrack (API wins)
    Update ShipPartType (API wins)
    Replace ItemProperties with API data
  ELSE:
    Create new Item with ALL fields from API
    Add to colony.Items
```

**TypeC Mapping (PROVISIONAL — pending discovery):**

The following mapping is a best guess based on swagger documentation. The actual `typeC` values must be confirmed by running the API Data Discovery test fixture (Req 16) against real data. The swagger hints at short codes (`"R"`, `"C"`, `"S"`, `"Bp"`, `"Sh"`) rather than full words.

Provisional mapping:
- `"R"` or `"resource"` → Resource
- `"C"` or `"commodity"` → Commodity
- `"Bp"` or `"blueprint"` → Blueprint
- `"flatpack"` → Flatpack
- `"Sh"` or `"hull"` → ShipHull
- `"part"` → ShipPart
- `"S"` or `"survey"` → Survey
- Unknown → None (logged as warning)

**This mapping will be revised after the discovery phase.** The implementation plan is phased:
1. Phase 1: Implement DTOs, data model expansion, API client methods, and discovery test fixture
2. Phase 1 gate: Run discovery tests against real API, produce mapping report
3. Phase 2: Implement merge logic using confirmed mappings from the report

Tasks beyond the discovery gate are provisional and will be replanned based on findings.

### 6. GameApiSyncScheduler — Colony Sync Integration

**File:** `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
**Satisfies:** Req 5, Req 11, Req 12, Req 14, Req 15

The existing `SyncCharacterAsync` method is extended with a new `SyncColoniesAsync` internal method. Logic is identical to the original design — fetch list, merge, fetch details for RemoteAccess > 0 colonies, persist if changes, raise event.

Scope caching: `buildingsScopeAvailable` and `warehouseScopeAvailable` booleans start true, flip to false on first 403, preventing redundant requests for the remainder of the cycle.

### 7. API Data Discovery Test Fixture

**File:** `OE2EmpireTracker.Tests/Client/GameApiColonyDiscoveryTests.cs`
**Satisfies:** Req 16

An NUnit test fixture (marked `[Explicit]`) for manual data discovery. Produces mapping report at `.kiro/specs/colony-api-sync/api-mapping-report.md`.


## Data Models

### Colony (expanded)

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| UUID | string | local | Generated on creation |
| OwnerUUID | string | local | Links colony to player |
| PlanetName | string | API `systemObjectName` | Dedup key |
| SystemName | string | API `systemName` | Dedup key |
| ColonyName | string | API `colonyName` | API wins |
| SystemId | int | API `systemId` | **NEW** |
| ColonySize | int | API `colonySize` | **NEW** |
| Distance | decimal | API `distance` | **NEW** |
| SurfaceVariation | int | API `surfaceVariation` | **NEW** |
| AtmosVariation | int | API `atmosVariation` | **NEW** |
| HexValue | string | API `hexValue` | **NEW** |
| SystemObjectTypeName | string | API `systemObjectTypeName` | **NEW** |
| ImagePreFix | string | API `imagePreFix` | **NEW** |
| ManufacturingBlocked | int | API `manufacturingBlocked` | **NEW** |
| WorkerCurrentAttitude | int | API `workerCurrentAttitude` | **MIGRATED from ColonyStructure** |
| ContentmentIndex | int | API `contentmentIndex` | **MIGRATED from ColonyStructure** |
| LastImportDateTime | string | local | ISO 8601 timestamp |
| Items | ItemBag | local + API | Warehouse contents |
| Structures | List&lt;ColonyStructure&gt; | local + API | Building list |
| Commodities | List&lt;CommodityRequested&gt; | local | Local-only |
| Locks | LockTracking | local | Local-only |

### ColonyStructure (expanded)

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| UUID | string | local | Generated on creation |
| FlatpackBlueprintUUID | string | local | Blueprint link |
| BuildingID | int | API `buildingId` | Stored for correlation |
| ColonyBuildingTypeId | int | API `colonyBuildingTypeId` | **NEW** |
| DisplaySequence | int | local | Local-only |
| BuildQueueSequence | int | local | Local-only |
| Properties | PropertyBag | local + API | Built/Online flags |
| AssignedWorkers | PropertyBag | local | Local-only |
| BuildCompletionTime | CountDownTime | local (+ API conditional) | Local-only unless null |
| ProcessCompletionTime | CountDownTime | local | Local-only |
| MiningSurvey | string | local | Local-only |
| MiningSurveyResource | string | local + API | API sets if local empty |
| MiningLeftOvers | decimal | local | Local-only |
| RefiningResource | string | local | Local-only |
| RefiningResourcePurity | string | local | Local-only |
| ResearchingBlueprintUUID | string | local | Local-only |
| ManufacturingBlueprintUUID | string | local | Local-only |
| ManufacturingCommodityName | string | local | Local-only |
| ManufacturingQuantity | int | local | Local-only |
| ManufacturingCompleted | int | local | Local-only |
| StagingResources | bool | local | Local-only |
| ResourceId | int | API `resourceId` | **NEW** |
| ResourceIcon | string | API `resourceIcon` | **NEW** |
| ManufactureAmountPerRun | int | API `manufactureAmountPerRun` | **NEW** |
| DurabilityCurrent | decimal | API `durabilityCurrent` | **NEW** |
| DurabilityMax | decimal | API `durabilityMax` | **NEW** |
| OpsStatusEffects | List&lt;BuildingStatusEffect&gt; | API | **NEW** |
| Industries | List&lt;BuildingIndustry&gt; | API | **NEW** |
| DetailsRequired | List&lt;BuildingDetailRequirement&gt; | API | **NEW** |
| SupportDetailsRequired | List&lt;BuildingDetailRequirement&gt; | API | **NEW** |
| BuildingAttributes | List&lt;BuildingAttribute&gt; | API | **NEW** |
| ExtraProperties | List&lt;BuildingExtraProperty&gt; | API | **NEW** |
| CurrentAttitude | string | **DEPRECATED** | Migrated to Colony |
| ContentmentIndex | int | **DEPRECATED** | Migrated to Colony |
| WageLevel | int | local | Local-only |

### Item (expanded)

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| UUID | string | local | Generated on creation |
| ItemType | ItemTypeEnum | API `typeC` mapped | Dedup key |
| Name | string | API `resourceName` | Dedup key |
| BaseItemTypeID | string | local + API | Resource lookup |
| NickName | string | local | Local-only |
| Description | string | local | Local-only |
| Quantity | int | API `amount` | API wins |
| ResourcePurity | string | local | Local-only |
| Volume | decimal | API `volume` | Unchanged type |
| Mass | decimal? | API `mass` | **NEW** |
| GameItemId | int? | API `Id` | **NEW** |
| JobRef | int? | API `jobRef` | **NEW** |
| JobDeliveryLoc | int? | API `jobDeliveryLoc` | **NEW** |
| HealthPercentage | decimal? | API `healthPercentage` | **NEW** |
| LastRepairHealthPercentage | decimal? | API `lastRepairHealthPercentage` | **NEW** |
| Evolution | int? | API `evolution` | **NEW** |
| ShipPartType | string | API `shipPartType` | **NEW** |
| JobName | string | API `jobName` | **NEW** |
| JobTrack | string | API `jobTrack` | **NEW** |
| ItemProperties | List&lt;ItemProperty&gt; | API `properties` | **NEW** |
| Contents | ItemBag | local | Local-only |
| CurrentHP | int | local | Local-only |
| MaxHP | int | local | Local-only |
| MaxRepairPercent | decimal | local | Local-only |


## Correctness Properties

The following properties must hold for the colony sync implementation:

### Property 1: Dedup Idempotency

**Validates: Requirements 6.1, 6.2, 6.3**

Running the same API response through `MergeColonyList` multiple times produces the same result as running it once. No duplicate colonies are created regardless of how many times the sync runs with identical data.

**Formal:** `∀ apiData, localState: merge(merge(localState, apiData), apiData) == merge(localState, apiData)`

### Property 2: Local-Only Field Preservation

**Validates: Requirements 8.5**

The merge logic never overwrites local-only fields on existing structures. For any structure that exists before and after a merge, the following fields are unchanged: BuildCompletionTime (if already set), ProcessCompletionTime, BuildQueueSequence, ManufacturingBlueprintUUID, ManufacturingQuantity, ManufacturingCompleted, ManufacturingCommodityName, ResearchingBlueprintUUID, MiningLeftOvers, StagingResources, and OverflowRules.

**Formal:** `∀ structure ∈ pre-merge ∩ post-merge: structure.LocalOnlyFields_before == structure.LocalOnlyFields_after`

### Property 3: No Data Loss on Empty Response

**Validates: Requirements 8.4, 9.4, 13.5**

An empty API colony list (zero colonies) never deletes existing local colonies. An empty buildings list never removes existing local structures. An empty warehouse list never removes existing local items.

**Formal:** `∀ localState: |merge(localState, emptyResponse).colonies| >= |localState.colonies|`

### Property 4: Owner Isolation

**Validates: Requirements 6.4**

Dedup matching only considers colonies owned by the syncing player. A colony owned by Player A is never matched or modified when syncing Player B's data.

**Formal:** `∀ colonyA where colonyA.OwnerUUID != syncingPlayerUUID: colonyA_before == colonyA_after`

### Property 5: Null/Empty API Values Don't Overwrite Strings

**Validates: Requirements 7.16**

If an API string field value is null or empty, the corresponding local field retains its previous value. Only non-empty API string values can overwrite local data. Numeric fields always overwrite (0 is a valid game state).

**Formal:** `∀ stringField: (apiValue == null || apiValue == "") → localField_after == localField_before`

### Property 6: Persistence Consistency

**Validates: Requirements 11.1, 11.2, 12.1, 12.3**

WriteContext is called if and only if at least one colony was created or updated. ColonyDataChanged is raised if and only if WriteContext is called.

**Formal:** `WriteContext() called ⟺ mergeResult.HasChanges == true`

### Property 7: Fail-Fast Before Mutation

**Validates: Requirements 13.4**

No local colony data is modified until the API response has been successfully deserialized. A deserialization failure results in zero mutations to local state.

**Formal:** `∀ malformedJson: localState_after_attempt == localState_before_attempt`

### Property 8: Scope Caching Correctness

**Validates: Requirements 14.4**

Once a scope is detected as unavailable (HTTP 403), no further requests are made for that scope's endpoints during the same sync cycle.

**Formal:** `∀ cycle: after first 403 for scope S, requestCount(S, remainder_of_cycle) == 0`

### Property 9: Migration Idempotency

**Validates: Requirements 18.6, 18.7, 18.8, 18.9**

Running the migration logic multiple times on the same data produces the same result as running it once. After migration, deprecated fields on ColonyStructure are cleared and Colony-level fields hold the migrated values.

**Formal:** `∀ data: migrate(migrate(data)) == migrate(data) ∧ post-migrate.structures.∀s: s.CurrentAttitude == "" ∧ s.ContentmentIndex == 0`

### Property 10: All API Fields Stored

**Validates: Requirements 17.1, 17.2, 17.3**

For any successful API response, every non-null field in the response is stored in the corresponding local model property. No API data is discarded during merge.

**Formal:** `∀ apiField ∈ response where apiField != null: localModel.correspondingProperty == apiField`


## Error Handling

| Error Condition | Handling | Recovery |
|----------------|----------|----------|
| HTTP 401 on colony list | Invalidate token, transition to DisconnectedInvalidKey | Same as profile 401 — user must re-authenticate |
| HTTP 403 on colony list | Log "scope not granted", skip entire colony sync | No retry this cycle; user must grant colony.list.read scope |
| HTTP 403 on buildings/warehouse | Cache scope unavailability, skip all subsequent detail requests for that scope | No retry this cycle; user must grant the specific scope |
| HTTP 404 on buildings/warehouse | Log warning, skip detail merge for that colony | Continue with next colony |
| HTTP 500/502/503/504 | Polly retry policy handles (3 retries with exponential backoff) | If all retries fail, treated as transient failure |
| Circuit breaker open | Return failure without making request | Circuit breaker resets after configured timeout |
| Malformed JSON response | Log error with truncated response (500 chars), skip colony merge entirely | No mutation to local state; retry next cycle |
| Null/empty SystemObjectName in API colony | Log warning, skip that colony entry | Continue processing remaining colonies |
| Exception during single colony merge | Log error, skip that colony | Continue processing remaining colonies |
| Empty colony list (valid response, 0 colonies) | Treat as no-op | No deletions, no changes |
| Rate limit exhausted | AcquireRateLimitTokenAsync blocks until token available | Polly rate limiter manages throughput |
| Migration parse failure (CurrentAttitude string → int) | Default to 0 | No error, graceful degradation |


## Testing Strategy

### Unit Tests (NUnit — OE2EmpireTracker.Tests)

**ColonyMergeServiceTests:**
- `MergeColonyList_NewColony_CreatesWithAllFields` — verifies all 14+ fields mapped on creation
- `MergeColonyList_ExistingColony_UpdatesAllGameAuthoritativeFields` — verifies "API wins" for all colony fields
- `MergeColonyList_ExistingColony_PreservesLocalOnlyFields` — verifies local-only fields untouched
- `MergeColonyList_DuplicateApiData_NoDoubleCreation` — idempotency property
- `MergeColonyList_EmptyList_NoChanges` — empty response safety
- `MergeColonyList_NullSystemObjectName_SkipsEntry` — graceful skip
- `MergeColonyList_OwnerIsolation_OnlyMatchesOwnedColonies` — owner boundary
- `MergeColonyList_NullStringApiValue_PreservesLocal` — null/empty string protection
- `MergeColonyList_ZeroNumericApiValue_OverwritesLocal` — numeric 0 is valid
- `MergeBuildings_NewBuilding_CreatesWithAllFields` — all building fields mapped
- `MergeBuildings_ExistingBuilding_UpdatesAllApiFields` — all API-sourced fields updated
- `MergeBuildings_ExistingBuilding_ReplacesCollections` — OpsStatusEffects, Industries, etc. replaced
- `MergeBuildings_PreservesLocalOnlyFields` — timer/queue preservation
- `MergeBuildings_MissingFromApi_NotRemoved` — no deletion on absence
- `MergeBuildings_ResourceName_SetsIfLocalEmpty` — conditional resource update
- `MergeBuildings_ConstructionTimer_SetsIfLocalNull` — conditional timer set
- `MergeWarehouse_NewItem_CreatesWithAllFields` — all assetCargoItem fields mapped
- `MergeWarehouse_ExistingItem_UpdatesAllApiFields` — quantity + all new fields
- `MergeWarehouse_ZeroAmount_SetsToZero` — zero quantity handling
- `MergeWarehouse_MissingFromApi_NotRemoved` — no deletion on absence
- `MergeWarehouse_UnknownTypeC_MapsToNone` — unknown type handling
- `MergeWarehouse_ReplacesItemProperties` — properties collection replaced

**DataModelMigrationTests:**
- `Colony_Deserialization_MigratesCurrentAttitudeFromStructure` — string→int migration
- `Colony_Deserialization_MigratesContentmentIndexFromStructure` — value migration
- `Colony_Deserialization_ClearsDeprecatedFieldsAfterMigration` — cleanup verification
- `Colony_Deserialization_MigrationIsIdempotent` — running twice produces same result
- `Colony_Deserialization_InvalidAttitudeString_DefaultsToZero` — parse failure handling
- `Colony_Deserialization_AlreadyMigrated_NoReprocessing` — skip if Colony fields already set

### Property-Based Tests (FsCheck 2.16.6 + NUnit)

**ColonyMergePropertyTests:**
- `MergeIsIdempotent` — P1: running merge twice produces same result
- `LocalOnlyFieldsNeverChange` — P2: local-only fields unchanged after merge
- `EmptyResponseNeverDeletes` — P3: empty lists don't remove data
- `OwnerIsolationHolds` — P4: other owners' colonies unchanged
- `NullStringValuesPreserveLocal` — P5: null/empty strings don't overwrite
- `PersistenceCalledIffChanges` — P6: WriteContext correlation
- `MigrationIsIdempotent` — P9: migration produces stable state
- `AllApiFieldsStored` — P10: no API data discarded

### Integration Tests (Explicit — manual execution)

**GameApiColonyDiscoveryTests:**
- Real API calls with live credentials
- Writes raw JSON to files for inspection
- Produces mapping report
- Not run in CI — marked `[Explicit]`

### Test Data Strategy

- Use `SystemClock.Freeze()` for deterministic timestamps
- Generate test colonies with known UUIDs for predictable matching
- Use FsCheck generators for random colony names, system names, structure lists
- Mock `GameApiClient` responses in scheduler tests (no real HTTP in unit tests)
- Include legacy JSON fixtures with CurrentAttitude on structures for migration tests
