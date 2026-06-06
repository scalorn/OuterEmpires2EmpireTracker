# Technical Design Document

## Overview

Replace the current sequential background sync with queue-based parallel dispatch using the proven `GameApiRequestQueue` infrastructure. The new `QueueSyncService` orchestrates all API calls through a rate-governed parallel queue, cascading list-level responses into detail-level fetches. Crate, survey, and blueprint imports are integrated into the sync cycle, with freshness-based skip logic to avoid redundant API calls.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    BackgroundProcessor                        │
│  (timer fires → invokes QueueSyncService.RunSyncAsync)       │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                     QueueSyncService                         │
│  - Reads config from GameApiConnectionSettings               │
│  - Creates GameApiRequestQueue(TPS, inflight, retries)       │
│  - Enqueues seed WorkItems                                   │
│  - Awaits DrainAsync                                         │
│  - Persists via PlayerContext.WriteContext                    │
└────────────┬───────────────────────────────────┬────────────┘
             │                                   │
             ▼                                   ▼
┌────────────────────────┐         ┌────────────────────────────┐
│  GameApiRequestQueue   │         │     TokenRefreshHandler     │
│  - Token bucket TPS    │         │  - SemaphoreSlim(1,1)       │
│  - Inflight cap        │         │  - Serialized 401 handling  │
│  - Retry (max 3)       │         │  - ExchangeTokenAsync       │
│  - Rate limit (429)    │         └────────────────────────────┘
│  - Metrics CSV         │
│  - AdaptiveRate        │
└────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│                    WorkItem Delegates                         │
│                                                              │
│  Seed Items:            Cascaded Items:                       │
│  - CharacterProfile     - ColonySummary(id)                  │
│  - CharacterSkills      - ColonyBuildings(id)                │
│  - ColonyList           - ColonyWarehouse(id)                │
│  - BankingBalance       - ColonyWorkers(id)                  │
│  - BankingTransactions  - AssetLocationDetail(id,type)       │
│  - AssetLocations       - CrateDetail(id) → CrateImporter   │
│  - AcceptedJobs         - SurveyDetail(id) → SurveyImport   │
│  - KillMailList         - BlueprintDetail(id) → BpImport     │
│  - MailList             - KillMailDetail(id)                  │
│  - ShipConfiguration    - MailDetail(id)                      │
│  - ShipCargo            - MailListPage(offset)               │
│  - MarketListings                                            │
│  - MarketItems                                               │
│  - MarketBuyOrders                                           │
│  - MarketSellOrders                                          │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. QueueSyncService

**Location:** `OE2EmpireTracker.Common/Services/QueueSyncService.cs`

The central orchestrator for queue-based background sync. Replaces sequential API calls with parallel dispatch.

```csharp
public class QueueSyncService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly PlayerContext _playerContext;
    private readonly EmpireContext _empireContext;
    private readonly GameApiClient _apiClient;
    private readonly GameApiConnectionSettings _settings;
    private readonly TokenRefreshHandler _tokenRefreshHandler;

    private volatile bool _isSyncRunning;
    private readonly object _syncLock = new object();

    public QueueSyncService(
        PlayerContext playerContext,
        EmpireContext empireContext,
        GameApiClient apiClient,
        GameApiConnectionSettings settings)

    public async Task<QueueSyncResult> RunSyncAsync(CancellationToken ct = default)
    public bool IsSyncRunning { get; }
}
```

**Responsibilities:**
- Constructs `GameApiRequestQueue` with TPS from settings, inflight cap = TPS × 3, maxRetries = 3
- Enqueues all seed work items at cycle start
- Awaits `DrainAsync` for cycle completion
- Guards against concurrent sync cycles via `_isSyncRunning` + lock
- Persists data via `PlayerContext.WriteContext` on completion
- Logs summary (succeeded/failed/elapsed) on completion

### 2. TokenRefreshHandler

**Location:** `OE2EmpireTracker.Common/Services/TokenRefreshHandler.cs`

Serializes token refresh attempts so only one 401 → refresh exchange occurs at a time.

```csharp
public class TokenRefreshHandler
{
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
    private readonly GameApiClient _apiClient;
    private readonly GameApiConnectionSettings _settings;
    private readonly CredentialStore _credentialStore;

    private string _currentAccessToken;
    private int _tokenVersion;

    public string CurrentAccessToken { get; }

    public async Task<TokenRefreshResult> HandleUnauthorizedAsync(CancellationToken ct)
}
```

**Token Refresh Flow:**
1. Work item receives HTTP 401
2. Calls `HandleUnauthorizedAsync`
3. Acquires `_refreshLock` (other 401s wait here)
4. Checks if token was already refreshed (compare token version)
5. If stale: calls `GameApiClient.ExchangeTokenAsync(clientId, secret, refreshToken)`
6. On success: updates `_currentAccessToken`, increments version, returns success
7. On failure: logs error, returns failure (work item marked failed)
8. Releases `_refreshLock` (waiters wake and see updated token)

### 3. QueueSyncResult

**Location:** `OE2EmpireTracker.Common/Services/QueueSyncResult.cs`

```csharp
public class QueueSyncResult
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<string> FailedLabels { get; set; } = new List<string>();
}
```

### 4. Work Item Factory Methods

**Location:** Within `QueueSyncService` as private methods.

Each factory method creates a `WorkItem` with a descriptive label and an async delegate. The delegate:
1. Calls the appropriate `GameApiClient` method
2. Parses the JSON response
3. Updates the data model (PlayerContext)
4. Returns cascading `WorkItem`s (or empty list)

#### Seed Work Item Factories

| Method | Label | API Call | Cascades |
|--------|-------|----------|----------|
| `CreateCharacterProfileItem` | "CharacterProfile" | `GetCharacterAsync` | None |
| `CreateCharacterSkillsItem` | "CharacterSkills" | `GetCharacterSkillsAsync` | None |
| `CreateColonyListItem` | "ColonyList" | `GetColonyListAsync` | Per-colony items |
| `CreateBankingBalanceItem` | "BankingBalance" | `GetBankingBalanceAsync` | None |
| `CreateBankingTransactionsItem` | "BankingTransactions" | `GetBankingTransactionsAsync` | None |
| `CreateAssetLocationsItem` | "AssetLocations" | `GetAssetLocationsAsync` | Per-location detail |
| `CreateAcceptedJobsItem` | "AcceptedJobs" | `GetAcceptedJobsAsync` | None |
| `CreateKillMailListItem` | "KillMailList" | `GetKillMailListAsync` | Per-kill-mail detail |
| `CreateMailListItem` | "MailList" | `GetMailListAsync` | Per-mail + next page |
| `CreateShipConfigItem` | "ShipConfiguration" | `GetShipConfigurationAsync` | None |
| `CreateShipCargoItem` | "ShipCargo" | `GetShipCargoAsync` | None |
| `CreateMarketListingsItem` | "MarketListings" | `GetMarketListingsAsync` | None |
| `CreateMarketItemsItem` | "MarketItems" | `GetMarketItemsAsync` | None |
| `CreateMarketBuyOrdersItem` | "MarketBuyOrders" | `GetMarketBuyOrdersAsync` | None |
| `CreateMarketSellOrdersItem` | "MarketSellOrders" | `GetMarketSellOrdersAsync` | None |

#### Cascading Work Item Factories

| Method | Label Pattern | Triggered By | Cascades |
|--------|--------------|--------------|----------|
| `CreateColonySummaryItem(id)` | "ColonySummary:{id}" | ColonyList | None |
| `CreateColonyBuildingsItem(id)` | "ColonyBuildings:{id}" | ColonyList | None |
| `CreateColonyWarehouseItem(id)` | "ColonyWarehouse:{id}" | ColonyList | None |
| `CreateColonyWorkersItem(id)` | "ColonyWorkers:{id}" | ColonyList | None |
| `CreateAssetLocationDetailItem(id,type)` | "AssetDetail:{id}" | AssetLocations | Crate/Survey/Bp |
| `CreateCrateDetailItem(crateId)` | "CrateDetail:{id}" | AssetDetail | None |
| `CreateSurveyDetailItem(surveyId)` | "SurveyDetail:{id}" | AssetDetail | None |
| `CreateBlueprintDetailItem(bpId)` | "BlueprintDetail:{id}" | AssetDetail | None |
| `CreateKillMailDetailItem(id)` | "KillMailDetail:{id}" | KillMailList | None |
| `CreateMailDetailItem(id)` | "MailDetail:{id}" | MailList | None |
| `CreateMailListPageItem(offset)` | "MailList:page{n}" | MailList | Per-mail + next page |

### 5. BackgroundProcessor Integration

**Location:** `OE2EmpireTracker.Common/Services/BackgroundProcessor.cs`

Add a call to `QueueSyncService.RunSyncAsync` within the timer cycle when Game API is enabled:

```csharp
// In BackgroundProcessor.ExecuteCycle (or a new async variant)
if (settings.Enabled && !_queueSyncService.IsSyncRunning)
{
    _ = Task.Run(() => _queueSyncService.RunSyncAsync(ct));
}
```

The sync runs on a background thread and does not block the timer cycle. The `IsSyncRunning` guard prevents overlapping cycles. The existing `GameApiConnectionSettings.PollingIntervalMinutes` controls how often BackgroundProcessor checks if a sync should start.

### 6. Preferences Form Integration

Add a numeric up/down control to the Game API connection settings tab:

- **Label:** "Detail Refresh (hours)"
- **Control:** NumericUpDown, Min=1, Max=168, Default=24
- **Binding:** Reads/writes `GameApiConnectionSettings.DetailRefreshHours`
- **Position:** Below existing TPS setting on the Game API tab

## Data Models

### Survey Model Extensions

```csharp
// Added to OE2EmpireTracker.Common/Models/Survey.cs
[JsonProperty("SystemObjectId")]
[DefaultValue(0)]
public int SystemObjectId { get; set; } = 0;

[JsonProperty("GameApiSurveyId")]
public int? GameApiSurveyId { get; set; }

[JsonProperty("LastDetailImportUtc")]
public DateTime? LastDetailImportUtc { get; set; }
```

### Asteroid Model Extensions

```csharp
// Added to OE2EmpireTracker.Common/Models/Asteroid.cs
[JsonProperty("SystemObjectId")]
[DefaultValue(0)]
public int SystemObjectId { get; set; } = 0;
```

### Blueprint Model Extensions

```csharp
// Added to OE2EmpireTracker.Common/Models/Blueprint.cs (extends Item)
[JsonProperty("GameApiBlueprintId")]
public int? GameApiBlueprintId { get; set; }

[JsonProperty("LastDetailImportUtc")]
public DateTime? LastDetailImportUtc { get; set; }
```

### GameApiConnectionSettings Extensions

```csharp
// Added to OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs
/// <summary>
/// Gets or sets the detail refresh interval in hours.
/// Controls how often surveys and blueprints are re-imported.
/// Valid range: 1-168 (1 hour to 7 days). Default 24 hours.
/// </summary>
[DefaultValue(24)]
public int DetailRefreshHours { get; set; } = 24;
```

### In-Memory Indexes (PlayerContext)

```csharp
// New private fields in PlayerContext
private Dictionary<int, Blueprint> _blueprintByApiIdIndex;
private Dictionary<int, Survey> _surveyByApiIdIndex;

// New public methods
public Blueprint FindBlueprintByApiId(int apiId)
public Survey FindSurveyByApiId(int apiId)
public void IndexBlueprintByApiId(Blueprint bp)
public void IndexSurveyByApiId(Survey survey)
```

**Index Lifecycle:**
- **Build:** During `InitBlueprints` / `InitSurveys`, iterate items with non-null API IDs and populate.
- **Add:** On `AddBlueprint` / `AddSurvey` with non-null API ID, insert into index.
- **Remove:** On `RemoveBlueprint` / `RemoveSurvey`, remove from index if present.
- **Update:** On first import setting API ID, call `IndexBlueprintByApiId` / `IndexSurveyByApiId`.
- **Lookup:** O(1) dictionary access under `_listLock`.

## Data Flow

### Crate Import Flow

```
CrateDetail WorkItem
  → GameApiClient.GetAssetCrateAsync(appId, token, crateId)
  → Parse JSON (GameApiAssetDetailResponse format)
  → CrateImporter.ImportFromJson(json, playerContext, empireContext)
  → Returns empty cascades
```

The raw JSON is passed directly to `CrateImporter.ImportFromJson` which handles blueprint extraction, deduplication, and PropertyBag population.

### Survey Import Flow

```
SurveyDetail WorkItem
  → GameApiClient.GetAssetSurveyAsync(appId, token, surveyId)
  → Parse JSON → GameApiSurveyResponse
  → Construct temp Survey from GameApiSurveyDetail:
      - PlanetName = (from parent asset location context)
      - SystemName = (from parent asset location context)
      - SurveyID = encryptedId
      - ScannedBy = scanCharacter
      - DateTime = scanDate.ToString()
      - SurveyType = objectType → SurveyType enum mapping
      - Resources = mapped from GameApiSurveyResource list
      - ParsedMaxReserves = from resource.MaxReserve values
  → SurveyImportHelper.FindByKey(surveys, planetName, surveyId)
  → If match: SurveyImportHelper.MergeData(existing, temp)
  → If no match: SurveyImportHelper.CreateFromTemp(temp, ownerUUID)
      → PlayerContext.AddSurvey(newSurvey)
  → SurveyImportHelper.LinkOrCreateAsteroid(survey, playerContext)
  → Set survey.SystemObjectId = response.Survey.SystemObjectId
  → If SystemObjectId > 0: propagate to linked asteroid
  → Set survey.GameApiSurveyId = response.Survey.Id
  → Set survey.LastDetailImportUtc = SystemClock.UtcNow
  → PlayerContext.IndexSurveyByApiId(survey)
  → Returns empty cascades
```

### Blueprint Import Flow

```
BlueprintDetail WorkItem
  → GameApiClient.GetAssetBlueprintAsync(appId, token, blueprintId)
  → Parse JSON → GameApiBlueprintDetailResponse
  → Construct CrateImporter-compatible JSON object:
      {
        "cargoItemId": blueprintInfo.Id,
        "typeC": "Bp",
        "resourceName": blueprintInfo.Name,
        "evolution": blueprintInfo.Evolution,
        "shipPartType": blueprintInfo.Type,
        "properties": [ mapped from blueprintProperties ]
      }
  → If partTypeIcon non-empty: add "_IconClass": "ui_icon_" + partTypeIcon
  → CrateImporter.ImportFromJson(constructedJson, playerContext, empireContext)
  → On imported blueprint: set GameApiBlueprintId = blueprintInfo.Id
  → Set LastDetailImportUtc = SystemClock.UtcNow
  → PlayerContext.IndexBlueprintByApiId(blueprint)
  → Returns empty cascades
```

### Import Freshness Skip Logic

```
AssetDetail response contains item with TypeC == "Bp" and CargoItemId = X
  → playerContext.FindBlueprintByApiId(X) returns bp?
    → Yes AND bp.LastDetailImportUtc within DetailRefreshHours → SKIP
    → Otherwise → Enqueue BlueprintDetail work item

AssetDetail response contains item with TypeC == "S" and CargoItemId = Y
  → playerContext.FindSurveyByApiId(Y) returns survey?
    → Yes AND survey.LastDetailImportUtc within DetailRefreshHours → SKIP
    → Otherwise → Enqueue SurveyDetail work item
```

```csharp
private bool IsDetailFresh(DateTime? lastImportUtc)
{
    if (lastImportUtc == null) return false;
    var threshold = TimeSpan.FromHours(_settings.DetailRefreshHours);
    return (SystemClock.UtcNow - lastImportUtc.Value) < threshold;
}
```

## Error Handling

### Per-WorkItem Exception Handling

The existing `GameApiRequestQueue.ExecuteWorkItemAsync` wraps each work item in try/catch, records `QueueError`, and continues dispatch. No changes needed to the queue for error isolation.

### Import Transaction Safety

Each import operation (crate, survey, blueprint) is self-contained:
- Parse response → construct domain object → add to context
- If any step throws, the work item fails and the partially-constructed object is never added to PlayerContext
- Other work items continue unaffected

### 401 Handling

Work item delegates detect HTTP 401 and delegate to `TokenRefreshHandler`. On success, the work item is re-enqueued. On failure, the item is marked failed with no further retry.

### 429 Rate Limit Handling

Work item delegates detect HTTP 429, extract `Retry-After` header, and call `queue.NotifyRateLimited(retryAfterSeconds)`. The queue pauses dispatch, resumes at 50% TPS, and recovers at +10% per minute.

### End-of-Cycle Reporting

```csharp
var status = queue.GetCompletionStatus();
Log.Info("Sync complete: {0} succeeded, {1} failed, elapsed {2:F1}s",
    status.Succeeded, status.Failed, elapsed.TotalSeconds);

foreach (var error in queue.GetErrors())
{
    Log.Warn("Failed work item '{0}': {1}", error.WorkItemLabel, error.Exception.Message);
}
```

## Testing Strategy

### Unit Tests

| Test Class | Covers | Key Scenarios |
|-----------|--------|---------------|
| `QueueSyncServiceTests` | RunSyncAsync flow | Seed enqueue, concurrency guard, persistence call |
| `TokenRefreshHandlerTests` | 401 handling | Serialization, version check, success/failure paths |
| `FreshnessSkipTests` | Skip logic | Fresh/stale/null timestamps, configurable threshold |
| `PlayerContextApiIdIndexTests` | Index CRUD | Add/remove/rebuild, concurrent access |
| `SurveyImportIntegrationTests` | Survey flow | New survey, merge existing, SystemObjectId propagation |
| `BlueprintImportIntegrationTests` | Blueprint flow | IconClass mapping, API ID tracking |

### Property-Based Tests

| Test | Generators | Property |
|------|-----------|----------|
| `ConcurrencyGuardProperty` | Random concurrent RunSyncAsync calls | At most 1 cycle runs |
| `TokenRefreshSerializationProperty` | N concurrent 401 events | Exactly 1 exchange call |
| `FreshnessDecisionProperty` | Random DateTime?/hours combos | Skip iff fresh per spec |
| `IndexConsistencyProperty` | Random Add/Remove/Index sequences | Lookup always correct |
| `ErrorIsolationProperty` | Random failure injection | succeeded + failed = total |
| `CascadeCompletenessProperty` | Random colony/asset lists | Cascade count correct |
| `SystemObjectIdPropagationProperty` | Random surveys with SystemObjectId | Both entities match |

## File Changes Summary

| File | Change Type | Purpose |
|------|-------------|---------|
| `OE2EmpireTracker.Common/Services/QueueSyncService.cs` | New | Main sync orchestrator |
| `OE2EmpireTracker.Common/Services/TokenRefreshHandler.cs` | New | Serialized 401 token refresh |
| `OE2EmpireTracker.Common/Services/QueueSyncResult.cs` | New | Sync result DTO |
| `OE2EmpireTracker.Common/Models/Survey.cs` | Modified | Add SystemObjectId, GameApiSurveyId, LastDetailImportUtc |
| `OE2EmpireTracker.Common/Models/Asteroid.cs` | Modified | Add SystemObjectId |
| `OE2EmpireTracker.Common/Models/Blueprint.cs` | Modified | Add GameApiBlueprintId, LastDetailImportUtc |
| `OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs` | Modified | Add DetailRefreshHours |
| `OE2EmpireTracker.Common/Services/PlayerContext.cs` | Modified | Add API ID indexes + lookup methods |
| `OE2EmpireTracker.Common/Services/BackgroundProcessor.cs` | Modified | Invoke QueueSyncService |
| `OE2EmpireTracker.Desktop/Forms/Preferences/*` | Modified | Add DetailRefreshHours UI control |

## Correctness Properties

### Property 1: No Concurrent Sync Cycles

**Validates: Requirements 9.3**

At most one sync cycle runs at any time. If `IsSyncRunning` is true, `RunSyncAsync` returns immediately without starting a new cycle.

**Test approach:** Invoke `RunSyncAsync` concurrently from multiple threads; verify only one completes with actual work, others return early.

### Property 2: Token Refresh Serialization

**Validates: Requirements 7.4**

When multiple work items receive 401 simultaneously, only one `ExchangeTokenAsync` call is made. All others wait and reuse the refreshed token.

**Test approach:** Simulate N concurrent 401 responses; verify exactly 1 exchange call occurs and all N items receive the updated token.

### Property 3: Freshness Skip Correctness

**Validates: Requirements 14.3, 14.4**

A detail work item is skipped if and only if: (a) an entity with matching API ID exists, AND (b) its `LastDetailImportUtc` is within `DetailRefreshHours` of `SystemClock.UtcNow`.

**Test approach:** Generate random entities with various LastDetailImportUtc values and DetailRefreshHours settings; verify skip/enqueue decision matches the specification.

### Property 4: Error Isolation

**Validates: Requirements 12.1, 12.3**

A failed work item (exception) does not prevent other work items from completing. The total processed count equals succeeded + failed.

**Test approach:** Inject random failures into work item delegates; verify succeeded + failed = total enqueued, and non-failing items all complete.

### Property 5: Index Consistency

**Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5**

After any sequence of Add/Remove/Index operations on PlayerContext, `FindBlueprintByApiId(x)` returns the correct blueprint (or null) for all valid API IDs.

**Test approach:** Perform random sequences of add, remove, and index operations; verify lookup correctness after each operation.

### Property 6: Rate Limit Respect

**Validates: Requirements 10.2, 10.3**

After `NotifyRateLimited(N)` is called, no work items are dispatched for at least N seconds. After resume, effective TPS is at most 50% of prior TPS.

**Test approach:** Call `NotifyRateLimited`, attempt dispatch, verify no items start until pause expires, and post-resume TPS is halved.

### Property 7: Cascading Completeness

**Validates: Requirements 2.2, 2.3**

For every colony in the colony list response, exactly 4 cascade items are enqueued (summary, buildings, warehouse, workers). For every asset location, exactly 1 detail item is enqueued.

**Test approach:** Generate random colony/asset lists; verify cascade count matches expected.

### Property 8: SystemObjectId Propagation

**Validates: Requirements 6.4**

When a survey is imported with SystemObjectId > 0 and is linked to an asteroid, both the survey and asteroid entities have the same SystemObjectId value.

**Test approach:** Import surveys with various SystemObjectId values; verify propagation to linked asteroids.
