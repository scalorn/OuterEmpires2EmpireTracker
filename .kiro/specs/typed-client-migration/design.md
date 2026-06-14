# Technical Design: Typed Client Migration

## Overview

Migrate all production consumers from the legacy `GameApiClient` (raw JSON tuples) to the existing `IGameApiTypedClient` interface (strongly-typed DTOs with built-in resilience). This is a consumer-side migration — the typed client infrastructure already exists from the nswag-typed-api-client spec. After all consumers are rewired, the old client class and all hand-written response models are deleted.

## Architecture

### Before Migration

```
┌─────────────────────────────────────────────────────────────────┐
│  Consumers                                                       │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │ QueueSyncService │  │ GameApiSyncSched │  │ BankingService│  │
│  │ MailService      │  │ ConnectionMonitor│  │ Merge Services│  │
│  └────────┬─────────┘  └────────┬─────────┘  └───────┬───────┘  │
│           │                      │                     │          │
│           ▼                      ▼                     ▼          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  GameApiClient (old)                                      │    │
│  │  Returns (bool Success, string Json) tuples               │    │
│  │  SemaphoreSlim rate limiter                               │    │
│  │  Manual Polly policies                                    │    │
│  └──────────────────────────────────────────────────────────┘    │
│           │                                                       │
│           ▼                                                       │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  Hand-Written Response Models                             │    │
│  │  GameApiProfileResponse, GameApiColonyResponse, etc.      │    │
│  │  GameApiServiceResponse<T> envelope class                 │    │
│  └──────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### After Migration

```
┌─────────────────────────────────────────────────────────────────┐
│  Consumers                                                       │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │ QueueSyncService │  │ GameApiSyncSched │  │ BankingService│  │
│  │ MailService      │  │ ConnectionMonitor│  │ Merge Services│  │
│  └────────┬─────────┘  └────────┬─────────┘  └───────┬───────┘  │
│           │                      │                     │          │
│           ▼                      ▼                     ▼          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  IGameApiTypedClient (existing)                           │    │
│  │  Returns typed DTOs directly                              │    │
│  │  TokenBucketRateLimiter (authoritative)                   │    │
│  │  Polly retry + circuit breaker                            │    │
│  │  Internal token cache + envelope unwrapping               │    │
│  └──────────────────────────────────────────────────────────┘    │
│           │                                                       │
│           ▼                                                       │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  NSwag Generated DTOs (existing)                          │    │
│  │  PublicCharacter, ColonyList, BankingBalance, etc.         │    │
│  │  System.Text.Json [JsonPropertyName] attributes           │    │
│  └──────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### GameApiContext (Modified)

**File:** `OE2EmpireTracker.Common/Client/GameApiContext.cs`

Changes:
- Replace `GameApiClient Client` property with `IGameApiTypedClient TypedClient` property
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient`
- `Initialize()` creates `GameApiTypedClient` configured with ServerUrl, AppId, ClientId from settings
- Configures `TokenBucketRateLimiter` TPS from `GameApiConnectionSettings.Tps`
- `Dispose()` disposes the typed client
- Remove `SetRateLimit` call on old client

```csharp
public class GameApiContext : IDisposable
{
    // REMOVED: public GameApiClient Client { get; }
    public IGameApiTypedClient TypedClient { get; }

    private GameApiContext(
        GameApiCredentialManager credentialManager,
        IGameApiTypedClient typedClient,
        GameApiConnectionMonitor connectionMonitor,
        GameApiSyncScheduler syncScheduler)
    {
        CredentialManager = credentialManager;
        TypedClient = typedClient;
        ConnectionMonitor = connectionMonitor;
        SyncScheduler = syncScheduler;
    }

    public static void Initialize()
    {
        // ... settings validation unchanged ...
        var typedClient = new GameApiTypedClient(
            settings.ServerUrl,
            settings.AppId,
            settings.Tps);

        var connectionMonitor = new GameApiConnectionMonitor(
            typedClient, credentialManager, firstPlayerUUID,
            settings.AppId, settings.ClientId);

        var syncScheduler = new ProductionSyncScheduler(
            typedClient, credentialManager, connectionMonitor,
            settings.AppId, settings.ClientId, EmpireContext.PlayerContext);

        _instance = new GameApiContext(credentialManager, typedClient, connectionMonitor, syncScheduler);
    }
}
```


### QueueSyncService (Modified)

**File:** `OE2EmpireTracker.Common/Services/QueueSyncService.cs`

Changes:
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient`
- `RunSyncAsync` calls `IGameApiTypedClient.ExchangeTokenAsync` (no longer receives tuple)
- Token is exchanged once at cycle start; the typed client caches it internally
- `ThrowIfUnauthorizedAsync` replaced with try/catch for `ApiHttpException` (StatusCode 401)
- `ThrowIfRateLimited` replaced with catch for `ApiHttpException` (StatusCode 429)
- All work item bodies call typed client methods directly (no `result.Json` handling)
- All `JsonConvert.DeserializeObject<GameApiServiceResponse<T>>` calls removed
- Merge services called with Generated DTOs instead of hand-written models

**Error handling pattern (per work item):**

```csharp
private WorkItem CreateCharacterProfileItem()
{
    return new WorkItem
    {
        Label = "CharacterProfile",
        ExecuteAsync = async ct =>
        {
            try
            {
                var profile = await _typedClient.GetCharacterAsync(ct).ConfigureAwait(false);

                var localProfile = _playerContext.FindMutablePlayerProfile(
                    _playerContext.CurrentPlayerUUID);
                if (localProfile == null)
                {
                    Log.Warn("CharacterProfile: no local profile found.");
                    return Array.Empty<WorkItem>();
                }

                bool changed = ProfileMergeService.MergeProfileData(localProfile, profile);
                if (changed)
                {
                    _playerContext.WriteContext();
                    _playerContext.OnPlayerProfileDataChanged(_playerContext.CurrentPlayerUUID);
                }
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401)
            {
                await HandleUnauthorizedAsync("CharacterProfile", ct).ConfigureAwait(false);
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 429)
            {
                HandleRateLimited("CharacterProfile");
            }
            catch (ApiBusinessException ex)
            {
                Log.Error("CharacterProfile: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
            }

            return Array.Empty<WorkItem>();
        },
    };
}
```

**New helper methods:**

```csharp
private async Task HandleUnauthorizedAsync(string label, CancellationToken ct)
{
    Log.Warn("{0}: received HTTP 401, attempting token refresh.", label);
    var refreshResult = await _tokenRefreshHandler.HandleUnauthorizedAsync(ct).ConfigureAwait(false);
    if (refreshResult.Success)
    {
        Log.Info("{0}: token refresh succeeded, retrying.", label);
        throw new InvalidOperationException("Token refreshed for '" + label + "'; retrying.");
    }

    Log.Error("{0}: token refresh failed.", label);
    throw new UnauthorizedAccessException("Token refresh failed for '" + label + "'.");
}

private void HandleRateLimited(string label)
{
    const int defaultRetryAfterSeconds = 60;
    Log.Warn("{0}: received HTTP 429, pausing queue for {1}s.", label, defaultRetryAfterSeconds);
    _currentQueue.NotifyRateLimited(defaultRetryAfterSeconds);
    throw new InvalidOperationException("Rate limited (429) for '" + label + "'; retrying.");
}
```

### TokenRefreshHandler (Modified)

**File:** `OE2EmpireTracker.Common/Services/TokenRefreshHandler.cs`

Changes:
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient`
- Remove `_apiClient.InvalidateToken()` call (typed client manages its own token cache)
- `HandleUnauthorizedAsync` calls `IGameApiTypedClient.ExchangeTokenAsync` directly
- The typed client caches the new token internally after exchange

```csharp
public class TokenRefreshHandler
{
    private readonly IGameApiTypedClient _typedClient;
    private readonly GameApiConnectionSettings _settings;
    private readonly GameApiCredentialManager _credentialManager;
    private readonly string _playerUUID;
    private string _currentAccessToken;

    public TokenRefreshHandler(
        IGameApiTypedClient typedClient,
        GameApiConnectionSettings settings,
        GameApiCredentialManager credentialManager,
        string playerUUID,
        string initialAccessToken)
    {
        _typedClient = typedClient;
        _settings = settings;
        _credentialManager = credentialManager;
        _playerUUID = playerUUID;
        _currentAccessToken = initialAccessToken ?? string.Empty;
    }

    public async Task<TokenRefreshResult> HandleUnauthorizedAsync(string failedToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!string.Equals(failedToken, _currentAccessToken, StringComparison.Ordinal))
            {
                return new TokenRefreshResult { Success = true, NewToken = _currentAccessToken };
            }

            string secret = GetDecryptedSecret();
            if (string.IsNullOrEmpty(secret)) return new TokenRefreshResult { Success = false };

            // No InvalidateToken call needed — typed client manages token cache
            var tokenDto = await _typedClient.ExchangeTokenAsync(
                _settings.AppId, _settings.ClientId, secret, ct).ConfigureAwait(false);

            _currentAccessToken = tokenDto.AccessToken;
            _tokenVersion++;
            return new TokenRefreshResult { Success = true, NewToken = _currentAccessToken };
        }
        catch (ApiHttpException ex)
        {
            Log.Error("Token refresh failed: HTTP {0}", ex.StatusCode);
            return new TokenRefreshResult { Success = false };
        }
        finally { _refreshLock.Release(); }
    }
}
```


### BankingService (Modified)

**File:** `OE2EmpireTracker.Common/Services/BankingService.cs`

Changes:
- `ImportTransactionsAsync` accepts `IGameApiTypedClient` instead of `GameApiClient`
- `ImportBalanceAsync` accepts `IGameApiTypedClient` instead of `GameApiClient`
- No explicit `accessToken` parameter needed (typed client manages tokens internally)
- Remove all `JArray`/`JObject` parsing; use `BankingTransactions` DTO directly
- Remove `GameApiServiceResponse` envelope unwrapping

```csharp
public static async Task<BankingImportResult> ImportTransactionsAsync(
    IGameApiTypedClient typedClient,
    PlayerContext playerContext,
    CancellationToken ct = default)
{
    var response = await typedClient.GetBankingTransactionsAsync(ct: ct).ConfigureAwait(false);
    // response is BankingTransactions DTO — iterate response.Transactions directly
    // Deduplication and pagination logic remains identical
}

public static async Task<decimal?> ImportBalanceAsync(
    IGameApiTypedClient typedClient,
    CancellationToken ct = default)
{
    var response = await typedClient.GetBankingBalanceAsync(ct).ConfigureAwait(false);
    return response.Balance;
}
```

### MailService (Modified)

**File:** `OE2EmpireTracker.Common/Services/MailService.cs`

Changes:
- `SyncMailAsync` accepts `IGameApiTypedClient` instead of `GameApiClient`
- No explicit `accessToken` parameter needed
- Replace `JObject.Parse` mail list parsing with `MailList` DTO iteration
- Replace `ParseMailFromDetail` JSON parsing with `MailBody` DTO field access
- Error handling via ApiHttpException catch blocks

```csharp
public static async Task<int> SyncMailAsync(
    IGameApiTypedClient typedClient,
    PlayerContext playerContext,
    CancellationToken ct = default)
{
    try
    {
        var mailList = await typedClient.GetMailListAsync(ct: ct).ConfigureAwait(false);
        // Iterate mailList.Items, fetch bodies via typedClient.GetMailBodyAsync
    }
    catch (ApiHttpException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
    {
        return -1; // Authentication failure
    }
    catch (ApiHttpException ex) when (ex.StatusCode == 429)
    {
        return -2; // Rate limited
    }
}
```

### GameApiConnectionMonitor (Modified)

**File:** `OE2EmpireTracker.Common/Services/GameApiConnectionMonitor.cs`

Changes:
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient`
- `PerformConnectivityCheckAsync` calls `IGameApiTypedClient.TestConnectionAsync`
- Checks `IGameApiTypedClient.IsCircuitOpen` for circuit breaker state
- Error handling via ApiHttpException catch for 401 → DisconnectedInvalidKey transition

```csharp
public class GameApiConnectionMonitor
{
    private readonly IGameApiTypedClient _typedClient;

    public GameApiConnectionMonitor(
        IGameApiTypedClient typedClient,
        GameApiCredentialManager credentialManager,
        string playerUUID, string appId, string clientId)
    {
        _typedClient = typedClient;
        // ... same field assignments ...
    }

    private async Task PerformConnectivityCheckAsync()
    {
        if (_typedClient.IsCircuitOpen)
        {
            TransitionTo(ConnectionState.DisconnectedCircuitOpen, "Circuit breaker open");
            return;
        }

        try
        {
            bool connected = await _typedClient.TestConnectionAsync(
                _appId, _clientId, secret).ConfigureAwait(false);
            if (connected)
            {
                TransitionTo(ConnectionState.Connected, "Token exchange succeeded");
            }
        }
        catch (ApiHttpException ex) when (ex.StatusCode == 401)
        {
            TransitionTo(ConnectionState.DisconnectedInvalidKey, "Invalid credentials (401)");
        }
        catch (Exception ex)
        {
            HandleConnectivityFailure(ex.Message);
        }
    }
}
```

### GameApiSyncScheduler (Modified)

**File:** `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`

Changes:
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient`
- `SyncCharacterAsync` calls `IGameApiTypedClient.ExchangeTokenAsync` and `GetCharacterAsync`
- Replace `JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiProfileResponse>>` with direct DTO
- All colony/asset sync methods use typed client methods
- Error handling via ApiHttpException catch blocks

### ProductionSyncScheduler (Modified)

**File:** `OE2EmpireTracker.Common/Services/ProductionSyncScheduler.cs`

Changes:
- Constructor accepts `IGameApiTypedClient` instead of `GameApiClient` and passes to base class

```csharp
public ProductionSyncScheduler(
    IGameApiTypedClient typedClient,
    GameApiCredentialManager credentialManager,
    GameApiConnectionMonitor connectionMonitor,
    string appId, string clientId,
    PlayerContext playerContext)
    : base(typedClient, credentialManager, connectionMonitor, appId, clientId)
{
    _playerContext = playerContext;
}
```


## Merge Service Adapter Layer

### Strategy: Direct DTO Acceptance

The merge services currently accept hand-written response models (e.g. `GameApiProfileResponse`). Rather than introducing an intermediate adapter/mapping layer, the merge services will be updated to accept the NSwag-generated DTOs directly. The generated DTO field names match the API JSON field names, which are the same fields the hand-written models mapped.

### ProfileMergeService (Modified)

**File:** `OE2EmpireTracker.Common/Services/ProfileMergeService.cs`

```csharp
// BEFORE:
public static bool MergeProfileData(PlayerProfile local, GameApiProfileResponse remote)

// AFTER:
public static bool MergeProfileData(PlayerProfile local, PublicCharacter remote)
```

Field mapping (GameApiProfileResponse → PublicCharacter):
| Hand-Written Field | Generated DTO Field | Notes |
|---|---|---|
| `UUID` | `Uuid` | Same JSON key "uuid" |
| `Name` | `Name` | Same |
| `Faction` | `Faction` | Same |
| `SkillPoints` | `SkillPoints` | Same |
| `CitizenId` | `CitizenId` | Same |
| `RegistrationDate` | `RegistrationDate` | Same |
| `CharacterId` | `CharacterId` | Same |
| `FirstName` | `FirstName` | Same |
| `LastName` | `LastName` | Same |
| `ActiveTimeMinutes` | `ActiveTimeMinutes` | Same |
| `Skills` | `Skills` | Dictionary structure same |
| `Ranks` | `Ranks` | Nested object same |
| `SkillInTraining` | `SkillInTraining` | Nested object same |

### ColonyMergeService (Modified)

**File:** `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`

```csharp
// BEFORE:
public static ColonyMergeResult MergeColonyList(GameApiColonyListResponse apiResponse, ...)
public static bool MergeBuildings(GameApiColonyBuildingsResponse apiResponse, Colony colony)
public static bool MergeWarehouse(GameApiColonyWarehouseResponse apiResponse, Colony colony)
public static bool MergeWorkers(GameApiColonyWorkersResponse apiWorkers, Colony colony)

// AFTER:
public static ColonyMergeResult MergeColonyList(ColonyList apiResponse, ...)
public static bool MergeBuildings(ColonyBuildings apiResponse, Colony colony)
public static bool MergeWarehouse(ColonyWarehouse apiResponse, Colony colony)
public static bool MergeWorkers(ColonyWorkers apiWorkers, Colony colony)
```

Internal field access patterns change from Newtonsoft `[JsonProperty]` names to System.Text.Json `[JsonPropertyName]` — but since both serialize to the same JSON keys, the generated DTO property names match semantically.

### AssetMergeService (Modified)

**File:** `OE2EmpireTracker.Common/Services/AssetMergeService.cs`

The `AssetMergeService` currently accepts `List<GameApiAssetCargoItem>`. The generated DTO uses `AssetCargoItem` (from the `AssetLocationDetail.Cargo` or `AssetCrateContents.Cargo` collections).

```csharp
// BEFORE:
public static bool MergeColonyAssets(List<GameApiAssetCargoItem> apiItems, Colony colony)
public static bool MergeStationAssets(List<GameApiAssetCargoItem> apiItems, Station station, ItemBag targetHold)
public static bool MergeShipAssets(List<GameApiAssetCargoItem> apiItems, Ship ship)

// AFTER:
public static bool MergeColonyAssets(ICollection<AssetCargoItem> apiItems, Colony colony)
public static bool MergeStationAssets(ICollection<AssetCargoItem> apiItems, Station station, ItemBag targetHold)
public static bool MergeShipAssets(ICollection<AssetCargoItem> apiItems, Ship ship)
```

Field mapping (GameApiAssetCargoItem → AssetCargoItem):
| Hand-Written | Generated DTO | Notes |
|---|---|---|
| `CargoItemId` | `Id` | JSON key "id" |
| `TypeId` | `TypeId` | Same |
| `Amount` | `Amount` | Same |
| `ResourceName` | `ResourceName` | Same |
| `TypeC` | `TypeC` | Same |
| `Evolution` | `Evolution` | Same |
| `Mass` | `Mass` | Same |
| `Volume` | `Volume` | Same |
| `HealthPercentage` | `HealthPercentage` | Same |
| `LastRepairHealthPercentage` | `LastRepairHealthPercentage` | Same |
| `Properties` | `Properties` | List<AssetCargoProperty> |
| `Icon` | `Icon` | Same |
| `ShipPartType` | `ShipPartType` | Same |
| `JobRef` | `JobRef` | Same |
| `JobDeliveryLoc` | `JobDeliveryLoc` | Same |
| `JobName` | `JobName` | Same |
| `JobTrack` | `JobTrack` | Same |

### BlueprintLinkageService (Modified)

**File:** `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs`

```csharp
// BEFORE:
public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID)
internal Blueprint BuildCandidateBlueprint(GameApiAssetCargoItem apiItem)
internal string ClassifyBlueprintType(GameApiAssetCargoItem apiItem)

// AFTER:
public bool ProcessItem(AssetCargoItem apiItem, Item localItem, string ownerUUID)
internal Blueprint BuildCandidateBlueprint(AssetCargoItem apiItem)
internal string ClassifyBlueprintType(AssetCargoItem apiItem)
```

### SurveyLinkageService (Modified)

**File:** `OE2EmpireTracker.Common/Services/SurveyLinkageService.cs`

```csharp
// BEFORE:
public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID)

// AFTER:
public bool ProcessItem(AssetCargoItem apiItem, Item localItem, string ownerUUID)
```

Field access remains identical since `ResourceName` is the same property name on both types.


## Data Models

### Generated DTOs (from NSwag, already existing)

The typed client returns these DTOs directly. No new data models are introduced by this migration — the generated DTOs from the nswag-typed-api-client spec are reused.

| Generated DTO | Replaces Hand-Written Model | Used By |
|---|---|---|
| `PublicCharacter` | `GameApiProfileResponse` | ProfileMergeService |
| `ColonyList` | `GameApiColonyListResponse` | ColonyMergeService.MergeColonyList |
| `ColonyBuildings` | `GameApiColonyBuildingsResponse` | ColonyMergeService.MergeBuildings |
| `ColonyWarehouse` | `GameApiColonyWarehouseResponse` | ColonyMergeService.MergeWarehouse |
| `ColonyWorkers` | `GameApiColonyWorkersResponse` | ColonyMergeService.MergeWorkers |
| `ColonySummary` | `GameApiColonySummaryResponse` | QueueSyncService colony summary items |
| `AssetLocations` | `GameApiAssetLocationsResponse` | QueueSyncService asset locations |
| `AssetLocationDetail` | `GameApiAssetDetailResponse` | QueueSyncService asset detail |
| `AssetCrateContents` | `GameApiAssetDetailResponse` (crate variant) | QueueSyncService crate detail |
| `AssetCargoItem` | `GameApiAssetCargoItem` | AssetMergeService, BlueprintLinkageService, SurveyLinkageService |
| `AssetCargoProperty` | `GameApiAssetItemProperty` | Property mapping in merge/linkage |
| `AssetBlueprint` | `GameApiBlueprintDetailResponse` | QueueSyncService blueprint detail |
| `AssetSurvey` | `GameApiSurveyResponse` | QueueSyncService survey detail |
| `BankingBalance` | Raw JSON parsing | BankingService.ImportBalanceAsync |
| `BankingTransactions` | Raw JSON (JArray) parsing | BankingService.ImportTransactionsAsync |
| `MailList` | Raw JSON (JObject) parsing | MailService.SyncMailAsync |
| `MailBody` | Raw JSON parsing | MailService mail detail fetch |
| `ShipConfiguration` | `GameApiShipConfigurationResponse` | QueueSyncService ship config |
| `ShipCargo` | `GameApiShipCargoResponse` | QueueSyncService ship cargo |
| `AcceptedJobs` | `GameApiAcceptedJobsResponse` | QueueSyncService accepted jobs |
| `MarketListings` | `GameApiMarketListingsResponse` | QueueSyncService market listings |
| `MarketItems` | `GameApiMarketItemsResponse` | QueueSyncService market items |
| `MarketPriceStats` | `GameApiMarketPriceStatsResponse` | QueueSyncService market prices |
| `MarketBuyOrders` | `GameApiMarketOrdersResponse` | QueueSyncService buy orders |
| `MarketSellOrders` | `GameApiMarketOrdersResponse` | QueueSyncService sell orders |
| `MarketCompetitorOrders` | `GameApiMarketCompetitorsResponse` | QueueSyncService competitors |
| `MarketShipComponents` | `GameApiMarketShipComponentsResponse` | QueueSyncService ship components |
| `KillMailList` | Raw JSON parsing | QueueSyncService kill mail list |
| `KillMail` | Raw JSON parsing | QueueSyncService kill mail detail |
| `TokenResponseDto` | `GameApiTokenResponse` | TokenRefreshHandler, QueueSyncService |
| `CharacterSkills` | Raw JSON parsing | QueueSyncService skills item |

### Local Domain Models (Unchanged)

These are NOT affected by the migration. They remain as Newtonsoft.Json-persisted local models:
- `PlayerProfile`, `PlayerSkill`, `PlayerRank`
- `Colony`, `ColonyStructure`
- `Ship`, `Station`
- `Blueprint`, `Survey`
- `Item`, `ItemBag`, `ItemProperty`
- `BankingTransaction`, `MailMessage`
- `MarketListing`, `MarketTransaction`

The merge services remain the bridge between API DTOs and local models.

## Error Handling

### Pattern: Exception-Based Instead of Tuple-Based

**Before (old client):**
```csharp
var result = await _apiClient.GetCharacterAsync(appId, accessToken);
if (!result.Success) { /* check result.Json for "401", "429" */ }
var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<T>>(result.Json);
var data = envelope?.Data;
```

**After (typed client):**
```csharp
try
{
    var data = await _typedClient.GetCharacterAsync(ct);
    // data is already the unwrapped DTO
}
catch (ApiHttpException ex) when (ex.StatusCode == 401) { /* auth failure */ }
catch (ApiHttpException ex) when (ex.StatusCode == 429) { /* rate limited */ }
catch (ApiHttpException ex) when (ex.StatusCode >= 500) { /* transient failure */ }
catch (ApiBusinessException ex) { /* business logic failure, no retry */ }
```

### Exception Hierarchy

```
Exception
├── ApiHttpException (HTTP 4xx/5xx)
│   ├── StatusCode 401 → Authentication failure, trigger token refresh
│   ├── StatusCode 429 → Rate limited, pause queue, retry after delay
│   └── StatusCode 5xx → Transient failure (handled by Polly internally)
├── ApiBusinessException (envelope success=false)
│   └── Log and fail without retry
├── ApiDeserializationException (malformed response)
│   └── Log and fail without retry
└── BrokenCircuitException (Polly circuit open)
    └── Log circuit-open state, fail immediately
```

### QueueSyncService Error Flow

```
Work Item ExecuteAsync
├─ SUCCESS → DTO received, pass to merge service, return child work items
├─ ApiHttpException (401) → HandleUnauthorizedAsync → token refresh
│   ├─ refresh succeeds → throw InvalidOperationException → queue retries
│   └─ refresh fails → throw UnauthorizedAccessException → work item fails
├─ ApiHttpException (429) → NotifyRateLimited → throw InvalidOperationException → queue retries
├─ ApiHttpException (5xx) → already retried by Polly internally; if still thrown, work item fails
├─ ApiBusinessException → log, work item fails (no retry)
└─ BrokenCircuitException → log circuit-open, work item fails
```

## Rate Limiter Consolidation

### Single Authority

After migration, rate limiting is controlled exclusively by the `TokenBucketRateLimiter` inside `GameApiTypedClient`:
- **Throughput:** configured from `GameApiConnectionSettings.Tps` (currently 0.9 TPS)
- **Concurrency:** 9 max inflight requests

### Queue Governor as Pass-Through

The `GameApiRequestQueue.TokenBucketGovernor` remains configured with a very high TPS (1000) so it acts as a pure dispatch pacer (controlling concurrency via `maxInflight`) without imposing throughput limits. The actual HTTP rate limiting is done by the typed client.

### 429 Handling

When a 429 is received:
1. `GameApiTypedClient` throws `ApiHttpException(429)` (Polly does NOT retry 429s)
2. QueueSyncService catches it and calls `GameApiRequestQueue.NotifyRateLimited(60)`
3. The queue pauses dispatch for 60 seconds
4. Work item is retried after the pause

## Token Management

### Simplified Flow

After migration, explicit `accessToken` passing is eliminated from all consumers:

1. `QueueSyncService.RunSyncAsync` calls `typedClient.ExchangeTokenAsync(appId, clientId, secret)` once at cycle start
2. The typed client caches the token internally
3. All subsequent API calls automatically attach the Bearer token
4. On 401 mid-sync, `TokenRefreshHandler` calls `ExchangeTokenAsync` again to refresh
5. The typed client updates its internal cache; subsequent calls use the new token

### TokenRefreshHandler Simplification

- No `InvalidateToken` call needed (typed client manages cache internally)
- Serialized refresh semantics (SemaphoreSlim) remain unchanged
- Stale-check optimization remains: if token was already refreshed by another waiter, skip

## Files to Delete After Migration

### Hand-Written Response Models (OE2EmpireTracker.Common/Models/)

| File | Replacement |
|---|---|
| `GameApiProfileResponse.cs` | Generated `PublicCharacter` + related DTOs |
| `GameApiColonyResponse.cs` | Generated `ColonyList`, `ColonyBuildings`, `ColonyWarehouse` |
| `GameApiColonySummaryResponse.cs` | Generated `ColonySummary` |
| `GameApiColonyWorkersResponse.cs` | Generated `ColonyWorkers` |
| `GameApiAssetResponse.cs` | Generated `AssetLocations`, `AssetLocationDetail`, `AssetCrateContents` |
| `GameApiBlueprintDetailResponse.cs` | Generated `AssetBlueprint` |
| `GameApiSurveyResponse.cs` | Generated `AssetSurvey` |
| `GameApiShipConfigurationResponse.cs` | Generated `ShipConfiguration` |
| `GameApiShipCargoResponse.cs` | Generated `ShipCargo` |
| `GameApiAcceptedJobsResponse.cs` | Generated `AcceptedJobs` |
| `GameApiMarketListingsResponse.cs` | Generated `MarketListings` |
| `GameApiMarketItemsResponse.cs` | Generated `MarketItems` |
| `GameApiMarketPriceStatsResponse.cs` | Generated `MarketPriceStats` |
| `GameApiMarketOrdersResponse.cs` | Generated `MarketBuyOrders`, `MarketSellOrders` |
| `GameApiMarketCompetitorsResponse.cs` | Generated `MarketCompetitorOrders` |
| `GameApiMarketShipComponentsResponse.cs` | Generated `MarketShipComponents` |
| `GameApiCrateContentsResponse.cs` | Generated `AssetCrateContents` |
| `GameApiSkillInTrainingResponse.cs` | Generated SkillInTraining DTO (inside PublicCharacter) |

### Envelope Class

| File | Notes |
|---|---|
| `GameApiTokenResponse.cs` (contains `GameApiServiceResponse<T>`) | Typed client handles envelope unwrapping internally |

Note: `GameApiTokenResponse.cs` also contains `GameApiTokenResponse` — this is replaced by generated `TokenResponseDto`. The `GameApiServiceResponse<T>` generic envelope in the same file is also removed.

### Old Client

| File | Notes |
|---|---|
| `OE2EmpireTracker.Common/Client/GameApiClient.cs` | Entire class deleted |

### Retained Files

- `GameApiConnectionSettings.cs` — Settings model, NOT a response DTO. Retained.
- `IGameApiTypedClient.cs` — The new interface. Retained.
- `GameApiTypedClient.cs` — The new implementation. Retained.
- All generated code in `Client/Generated/` — Retained.


## Testing Strategy

### Unit Tests

- **QueueSyncService property tests:** Updated to verify `ApiHttpException` catch patterns instead of `result.Json == "401"` string comparisons. FsCheck generators produce random `ApiHttpException` with various status codes.
- **TokenRefreshHandler tests:** Verify serialized refresh, stale-check optimization, and typed client ExchangeTokenAsync integration.
- **Merge service tests:** Input changed from hand-written models to generated DTOs. Verification logic unchanged.
- **BankingService tests:** Mock `IGameApiTypedClient` returning `BankingTransactions` DTO. Verify same deduplication results.
- **MailService tests:** Mock `IGameApiTypedClient` returning `MailList` and `MailBody` DTOs. Verify same incremental sync results.

### Integration Tests

- **GameApiFullDiscoveryTests:** Already migrated to `IGameApiTypedClient` in the nswag-typed-api-client spec. No changes needed.
- **QueueSyncService integration test:** End-to-end sync cycle with HttpListener returning real API JSON. Verify typed client correctly deserializes and merge services produce same results.

### Behavioral Equivalence Tests

Property-based tests that verify behavioral equivalence:
- Given the same API response JSON, the old path (GameApiClient → JsonConvert → hand-written model → merge) and new path (GameApiTypedClient → generated DTO → merge) produce the same local data state.
- These tests can run in parallel during migration to validate before removing old code.

## Correctness Properties

### Property 1: Merge Equivalence

**Validates: Requirements 13.1, 13.2, 13.3, 13.4**

For any valid API response JSON body, deserializing via the old path (Newtonsoft `GameApiServiceResponse<GameApiProfileResponse>`) and via the new path (System.Text.Json `PublicCharacter` DTO) then applying the respective merge service produces identical local model state.

Tested by: generating random valid profile/colony/asset JSON, running both deserialization paths, applying both merge paths, comparing resulting PlayerProfile/Colony/Ship/Station state field-by-field.

### Property 2: Error Classification Equivalence

**Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5, 9.6**

For any HTTP response (2xx with success=false, 401, 429, 5xx), the error classification (auth failure, rate limit, transient, business error) is the same whether detected by the old pattern (`result.Json == "401"`) or the new pattern (`ApiHttpException.StatusCode == 401`).

Tested by: generating random HTTP status codes and response bodies, verifying the old tuple-based classification matches the new exception-based classification.

### Property 3: Token Refresh Serialization

**Validates: Requirements 13.6, 3.1, 3.2**

For any sequence of concurrent 401 responses triggering refresh, at most one ExchangeTokenAsync call is in-flight at a time, and all concurrent waiters receive the same refreshed token. This property is unchanged from the existing TokenRefreshHandler tests but verified to hold with the new typed client integration.

### Property 4: Rate Limiter Single Authority

**Validates: Requirements 8.1, 8.3, 8.4**

For any sequence of N concurrent API requests dispatched through QueueSyncService, the actual HTTP request rate never exceeds the configured TPS (0.9), and at most 9 requests are in-flight simultaneously. Only the typed client's `TokenBucketRateLimiter` enforces this — no old-client semaphore is involved.

## Migration Order (Phased Approach)

Migration must be done atomically per consumer (no partial state where a consumer uses both clients), but consumers can be migrated independently in sequence.

### Phase 1: Foundation (GameApiContext + TokenRefreshHandler)

1. Modify GameApiContext to create and hold IGameApiTypedClient
2. Modify TokenRefreshHandler to use IGameApiTypedClient
3. Both changes compile together — GameApiContext still exposes old client temporarily via backward-compat property for unmigrated consumers

### Phase 2: Merge Services

4. Update ProfileMergeService to accept PublicCharacter
5. Update ColonyMergeService to accept ColonyList/ColonyBuildings/ColonyWarehouse/ColonyWorkers
6. Update AssetMergeService to accept AssetCargoItem
7. Update BlueprintLinkageService to accept AssetCargoItem
8. Update SurveyLinkageService to accept AssetCargoItem

### Phase 3: Core Consumers

9. Migrate QueueSyncService (the largest consumer — all work item bodies)
10. Migrate BankingService
11. Migrate MailService
12. Migrate GameApiConnectionMonitor

### Phase 4: Legacy Scheduler

13. Migrate GameApiSyncScheduler
14. Migrate ProductionSyncScheduler

### Phase 5: Cleanup

15. Delete GameApiClient.cs
16. Delete all hand-written response model files
17. Delete GameApiServiceResponse<T> envelope class
18. Verify zero compile-time references to deleted types

### Phase 6: Test Migration

19. Update merge service tests to use generated DTOs
20. Update QueueSyncService property tests for exception-based error handling
21. Update integration tests
22. Run full test suite — zero failures

## Design Decisions

| Decision | Rationale |
|---|---|
| Direct DTO acceptance (no adapter layer) | The generated DTOs have the same JSON field names as the hand-written models. An intermediate adapter would add complexity without semantic value. Merge services are updated to reference the new types directly. |
| Phased migration order | Merge services first (no runtime behavior change, just type signature change), then consumers. This allows each phase to compile independently. |
| TokenRefreshHandler simplified (no InvalidateToken) | The typed client manages its own token cache. External invalidation is unnecessary and could cause race conditions. |
| Retain GameApiRequestQueue TokenBucketGovernor as pass-through | The queue still needs concurrency control (maxInflight) and 429 pause behavior. Only throughput limiting moves to the typed client. |
| Exception-based error handling | Typed exceptions are more expressive than string comparisons. `catch (ApiHttpException ex) when (ex.StatusCode == 401)` is self-documenting and compiler-verified. |
| No transition period for rate limiters | Requirement 8.4 explicitly forbids dual rate limiting. The old semaphore is removed atomically with the old client in Phase 5. During Phases 1-4, the old client's semaphore may still exist in the codebase but is not actively limiting requests (since consumers call the typed client). |
| QueueSyncService.BuildCrateImporterJson refactored | Currently operates on raw JSON string. After migration, operates on `AssetCrateContents` DTO directly — simpler and type-safe. |
| QueueSyncService.ResponseContainsBlueprints refactored | Currently deserializes raw JSON. After migration, checks `AssetCrateContents.Cargo` collection for blueprint TypeC directly. |

## File Layout (Post-Migration)

```
OE2EmpireTracker.Common/
  Client/
    Generated/
      GameApiGeneratedClient.cs       ← NSwag auto-generated (retained)
    GameApiTypedClient.cs             ← Typed client wrapper (retained)
    IGameApiTypedClient.cs            ← Interface (retained)
    TokenBucketRateLimiter.cs         ← Rate limiter (retained)
    Exceptions/
      ApiHttpException.cs             ← HTTP errors (retained)
      ApiBusinessException.cs         ← Business errors (retained)
      ApiDeserializationException.cs  ← Deserialization errors (retained)
      ApiValidationError.cs           ← Validation error detail (retained)
    GameApiContext.cs                 ← Modified: holds IGameApiTypedClient
    GameApiClient.cs                  ← DELETED

  Models/
    GameApiConnectionSettings.cs      ← Retained (settings, not a response model)
    GameApiProfileResponse.cs         ← DELETED
    GameApiColonyResponse.cs          ← DELETED
    GameApiColonySummaryResponse.cs   ← DELETED
    GameApiColonyWorkersResponse.cs   ← DELETED
    GameApiAssetResponse.cs           ← DELETED
    GameApiBlueprintDetailResponse.cs ← DELETED
    GameApiSurveyResponse.cs          ← DELETED
    GameApiShipConfigurationResponse.cs ← DELETED
    GameApiShipCargoResponse.cs       ← DELETED
    GameApiAcceptedJobsResponse.cs    ← DELETED
    GameApiMarketListingsResponse.cs  ← DELETED
    GameApiMarketItemsResponse.cs     ← DELETED
    GameApiMarketPriceStatsResponse.cs ← DELETED
    GameApiMarketOrdersResponse.cs    ← DELETED
    GameApiMarketCompetitorsResponse.cs ← DELETED
    GameApiMarketShipComponentsResponse.cs ← DELETED
    GameApiCrateContentsResponse.cs   ← DELETED
    GameApiSkillInTrainingResponse.cs ← DELETED
    GameApiTokenResponse.cs           ← DELETED (GameApiServiceResponse<T> + token type)

  Services/
    QueueSyncService.cs               ← Modified: uses IGameApiTypedClient
    TokenRefreshHandler.cs            ← Modified: uses IGameApiTypedClient
    BankingService.cs                 ← Modified: uses IGameApiTypedClient
    MailService.cs                    ← Modified: uses IGameApiTypedClient
    GameApiConnectionMonitor.cs       ← Modified: uses IGameApiTypedClient
    GameApiSyncScheduler.cs           ← Modified: uses IGameApiTypedClient
    ProductionSyncScheduler.cs        ← Modified: uses IGameApiTypedClient
    ProfileMergeService.cs            ← Modified: accepts PublicCharacter
    ColonyMergeService.cs             ← Modified: accepts ColonyList/Buildings/etc.
    AssetMergeService.cs              ← Modified: accepts AssetCargoItem
    BlueprintLinkageService.cs        ← Modified: accepts AssetCargoItem
    SurveyLinkageService.cs           ← Modified: accepts AssetCargoItem
```
