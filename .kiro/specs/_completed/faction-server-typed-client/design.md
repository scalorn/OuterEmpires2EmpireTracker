# Design Document: Faction Server Typed Client

## Overview

This design replaces the raw JSON communication in `RemoteFactionClient` with a fully-typed client interface (`IFactionServerTypedClient`) and implementation (`FactionServerTypedClient`). Every method parameter and return type uses the actual domain model classes from `OE2EmpireTracker.Common.Models` and `OE2EmpireTracker.Services` — no raw JSON strings anywhere in the public API surface.

The client follows Option A: **one method per entity collection**, providing fully granular typed methods for all 19 per-character entity types, plus typed methods for baseline upload, bulk import, sync, export, sharing, and server administration (factions/characters).

### Design Decisions

1. **Reuse existing domain models** — `PlayerRoot`, `BaselineRoot`, `Colony`, `Blueprint`, etc. already exist in Common. The typed client returns/accepts these directly.
2. **No wrapper DTOs for entity collections** — Methods like `GetColoniesAsync` return `Colony[]` directly, not a wrapper DTO. This matches the server's wire format (JSON arrays).
3. **BulkImportResult as a new DTO** — The server returns `{ imported: {}, total: N }` which needs a typed representation.
4. **SyncResponse as a new DTO** — The `/api/v1/sync/snapshot` endpoint returns factions, characters, and a timestamp.
5. **ServerFaction/ServerCharacter already in Common** — These models already exist in `OE2EmpireTracker.Common/Models/ServerModels.cs` with `EntityMetadata` properties. The typed client uses them directly for `GetFactionsAsync`/`GetCharactersAsync` return types. The `EntityMetadata` property is server-internal (tracks `LastModifiedUtc` for storage) and will be excluded from the typed client's serialization settings (`NullValueHandling.Ignore` ensures it is omitted on the wire when null).


### Requirements Note

The design uses per-entity-type endpoints (19 entity types) which reflect the actual server API. The requirements have been updated to align with this approach. The existing `RemoteFactionClient` still has the legacy `GetCharacterDataAsync(uuid, dataType)` and `GetAllCharacterDataAsync(uuid)` generic endpoints — these will be deprecated once the typed client is adopted.

---

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│  WinForms App / SyncManager                              │
│                                                          │
│  Uses: IFactionServerTypedClient                         │
└──────────────────────┬───────────────────────────────────┘
                       │ (interface dependency)
┌──────────────────────▼───────────────────────────────────┐
│  OE2EmpireTracker.Common/Client/FactionServer/           │
│                                                          │
│  ├── IFactionServerTypedClient.cs   (interface)          │
│  ├── FactionServerTypedClient.cs    (implementation)     │
│  ├── DTOs/                                               │
│  │   ├── SyncResponse.cs                                 │
│  │   ├── BulkImportResult.cs                             │
│  │   └── SharingRuleDto.cs                               │
│  └── Exceptions/                                         │
│      ├── FactionServerException.cs                       │
│      ├── FactionValidationException.cs                   │
│      ├── FactionAuthorizationException.cs                │
│      └── FactionConnectionException.cs                   │
└──────────────────────┬───────────────────────────────────┘
                       │ (HTTP + Newtonsoft.Json)
┌──────────────────────▼───────────────────────────────────┐
│  Faction Server (ASP.NET Core Minimal API)               │
│  /api/v1/...                                             │
└──────────────────────────────────────────────────────────┘
```


### Key Architectural Constraints

- **No circular dependencies** — Common references no other project. Server and WinForms both reference Common.
- **No new NuGet packages** — Uses existing Newtonsoft.Json and System.Net.Http.
- **Serialization consistency** — The domain models use Newtonsoft.Json attributes (`[JsonProperty]`, `[JsonIgnore]`, `[DefaultValue]`). The typed client serializes with Newtonsoft to match.
- **Server uses System.Text.Json with camelCase** — The server's HTTP JSON serialization uses `System.Text.Json` with `CamelCase` naming policy. The client must serialize requests in PascalCase (Newtonsoft default) because the server's deserialization is case-insensitive. For responses, Newtonsoft with `[JsonProperty("camelName")]` handles the mapping.

---

## Components and Interfaces

### IFactionServerTypedClient

```csharp
using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Strongly-typed client interface for the Faction Server API.
    /// One async method per endpoint, returning typed domain objects.
    /// </summary>
    public interface IFactionServerTypedClient : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the client is connected.
        /// </summary>
        bool IsConnected { get; }

        // --- Server Administration ---

        /// <summary>Checks server health.</summary>
        Task<bool> CheckHealthAsync(CancellationToken ct = default);

        /// <summary>Gets all server-level factions.</summary>
        Task<ServerFaction[]> GetFactionsAsync(CancellationToken ct = default);

        /// <summary>Gets all server-level characters.</summary>
        Task<ServerCharacter[]> GetCharactersAsync(CancellationToken ct = default);

        /// <summary>Creates a character on the server.</summary>
        Task CreateCharacterAsync(string name, string uuid = null, CancellationToken ct = default);

        // --- Sync and Export ---

        /// <summary>Gets the sync snapshot (factions, characters, timestamp).</summary>
        Task<SyncResponse> GetSyncSnapshotAsync(CancellationToken ct = default);

        /// <summary>Exports all character data as a typed PlayerRoot.</summary>
        Task<PlayerRoot> ExportCharacterDataAsync(string characterUUID, CancellationToken ct = default);

        // --- Global/Baseline Data ---

        /// <summary>Uploads baseline data (typed BaselineRoot).</summary>
        Task UploadBaselineAsync(BaselineRoot baseline, CancellationToken ct = default);

        // --- Bulk Import ---

        /// <summary>Bulk imports all character data (typed PlayerRoot).</summary>
        Task<BulkImportResult> BulkImportAsync(string characterUUID, PlayerRoot data, CancellationToken ct = default);

        // --- Sharing Rules ---

        /// <summary>Gets sharing rules for a character.</summary>
        Task<SharingRuleDto[]> GetSharingRulesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Replaces all sharing rules for a character.</summary>
        Task PutSharingRulesAsync(string characterUUID, SharingRuleDto[] rules, CancellationToken ct = default);

        // --- Per-Entity Collection Methods (19 entity types) ---

        /// <summary>Gets all colonies for a character.</summary>
        Task<Colony[]> GetColoniesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all blueprints for a character.</summary>
        Task<Blueprint[]> GetBlueprintsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all surveys for a character.</summary>
        Task<Survey[]> GetSurveysAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all player profiles for a character.</summary>
        Task<PlayerProfile[]> GetPlayerProfilesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all delivery routes for a character.</summary>
        Task<DeliveryRoute[]> GetDeliveryRoutesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all delivery plans for a character.</summary>
        Task<DeliveryPlan[]> GetDeliveryPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all ships for a character.</summary>
        Task<Ship[]> GetShipsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all ship templates for a character.</summary>
        Task<ShipTemplate[]> GetShipTemplatesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all market listings for a character.</summary>
        Task<MarketListing[]> GetMarketListingsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all market transactions for a character.</summary>
        Task<MarketTransaction[]> GetMarketTransactionsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all pricing plans for a character.</summary>
        Task<PricingPlan[]> GetPricingPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stock plans for a character.</summary>
        Task<StockPlan[]> GetStockPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stock profiles for a character.</summary>
        Task<StockProfile[]> GetStockProfilesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all build plans for a character.</summary>
        Task<BuildPlan[]> GetBuildPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all supply chains for a character.</summary>
        Task<SupplyChain[]> GetSupplyChainsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all asteroids for a character.</summary>
        Task<Asteroid[]> GetAsteroidsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stations for a character.</summary>
        Task<Station[]> GetStationsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all faction contacts for a character.</summary>
        Task<Faction[]> GetFactionContactsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all external characters for a character.</summary>
        Task<ExternalCharacter[]> GetExternalCharactersAsync(string characterUUID, CancellationToken ct = default);
    }
}
```


### Endpoint-to-Method Mapping

| Server Endpoint | HTTP | Client Method | Return Type |
|---|---|---|---|
| `/health` | GET | `CheckHealthAsync` | `bool` |
| `/api/v1/factions` | GET | `GetFactionsAsync` | `ServerFaction[]` |
| `/api/v1/characters` | GET | `GetCharactersAsync` | `ServerCharacter[]` |
| `/api/v1/characters` | POST | `CreateCharacterAsync` | `void` |
| `/api/v1/sync/snapshot` | GET | `GetSyncSnapshotAsync` | `SyncResponse` |
| `/api/v1/characters/{uuid}/export` | GET | `ExportCharacterDataAsync` | `PlayerRoot` |
| `/api/v1/global/baseline` | PUT | `UploadBaselineAsync` | `void` |
| `/api/v1/characters/{uuid}/import` | PUT | `BulkImportAsync` | `BulkImportResult` |
| `/api/v1/characters/{uuid}/sharing` | GET | `GetSharingRulesAsync` | `SharingRuleDto[]` |
| `/api/v1/characters/{uuid}/sharing` | PUT | `PutSharingRulesAsync` | `void` |
| `/api/v1/characters/{uuid}/colonies` | GET | `GetColoniesAsync` | `Colony[]` |
| `/api/v1/characters/{uuid}/blueprints` | GET | `GetBlueprintsAsync` | `Blueprint[]` |
| `/api/v1/characters/{uuid}/surveys` | GET | `GetSurveysAsync` | `Survey[]` |
| `/api/v1/characters/{uuid}/playerProfiles` | GET | `GetPlayerProfilesAsync` | `PlayerProfile[]` |
| `/api/v1/characters/{uuid}/deliveryRoutes` | GET | `GetDeliveryRoutesAsync` | `DeliveryRoute[]` |
| `/api/v1/characters/{uuid}/deliveryPlans` | GET | `GetDeliveryPlansAsync` | `DeliveryPlan[]` |
| `/api/v1/characters/{uuid}/ships` | GET | `GetShipsAsync` | `Ship[]` |
| `/api/v1/characters/{uuid}/shipTemplates` | GET | `GetShipTemplatesAsync` | `ShipTemplate[]` |
| `/api/v1/characters/{uuid}/marketListings` | GET | `GetMarketListingsAsync` | `MarketListing[]` |
| `/api/v1/characters/{uuid}/marketTransactions` | GET | `GetMarketTransactionsAsync` | `MarketTransaction[]` |
| `/api/v1/characters/{uuid}/pricingPlans` | GET | `GetPricingPlansAsync` | `PricingPlan[]` |
| `/api/v1/characters/{uuid}/stockPlans` | GET | `GetStockPlansAsync` | `StockPlan[]` |
| `/api/v1/characters/{uuid}/stockProfiles` | GET | `GetStockProfilesAsync` | `StockProfile[]` |
| `/api/v1/characters/{uuid}/buildPlans` | GET | `GetBuildPlansAsync` | `BuildPlan[]` |
| `/api/v1/characters/{uuid}/supplyChains` | GET | `GetSupplyChainsAsync` | `SupplyChain[]` |
| `/api/v1/characters/{uuid}/asteroids` | GET | `GetAsteroidsAsync` | `Asteroid[]` |
| `/api/v1/characters/{uuid}/stations` | GET | `GetStationsAsync` | `Station[]` |
| `/api/v1/characters/{uuid}/factions` | GET | `GetFactionContactsAsync` | `Faction[]` |
| `/api/v1/characters/{uuid}/externalCharacters` | GET | `GetExternalCharactersAsync` | `ExternalCharacter[]` |


### FactionServerTypedClient Implementation

```csharp
namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// HTTP implementation of IFactionServerTypedClient.
    /// Uses Newtonsoft.Json for serialization, SecureString for bearer token,
    /// optional certificate pinning, and a semaphore-based rate limiter.
    /// </summary>
    public class FactionServerTypedClient : IFactionServerTypedClient
    {
        private readonly string _serverUrl;
        private readonly SecureString _bearerToken;
        private readonly string _trustedThumbprint;
        private readonly TimeSpan _timeout;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _rateLimiter;
        private int _rateLimitRequestsPerMinute;
        private bool _disposed;

        public FactionServerTypedClient(
            string serverUrl,
            SecureString bearerToken,
            string trustedThumbprint = null,
            TimeSpan? timeout = null)
        {
            _serverUrl = (serverUrl ?? "").TrimEnd('/');
            _bearerToken = bearerToken;
            _trustedThumbprint = trustedThumbprint;
            _timeout = timeout ?? TimeSpan.FromMinutes(5); // No min/max bounds enforced
            _rateLimitRequestsPerMinute = 60;
            _rateLimiter = new SemaphoreSlim(60, 60);
            _httpClient = CreateHttpClient();
        }

        public bool IsConnected { get; private set; }

        // All methods follow the same pattern:
        // 1. AcquireRateLimitToken()
        // 2. Send HTTP request
        // 3. Check status code → throw typed exception on error
        // 4. Deserialize response with Newtonsoft.Json
        // 5. Return typed result
    }
}
```

#### Constructor Parameters

| Parameter | Type | Description |
|---|---|---|
| `serverUrl` | `string` | Base URL (e.g. `https://faction.example.com`) |
| `bearerToken` | `SecureString` | API bearer token, disposed with client |
| `trustedThumbprint` | `string` | Optional SHA-256 cert thumbprint for pinning |
| `timeout` | `TimeSpan?` | Optional request timeout (default: 5 min, no min/max bounds) |


#### Internal Method Pattern

Every public method follows this pattern:

```csharp
public async Task<Colony[]> GetColoniesAsync(string characterUUID, CancellationToken ct = default)
{
    await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
    string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/colonies";
    var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
    await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return JsonConvert.DeserializeObject<Colony[]>(json);
}
```

For write operations (PUT/POST):

```csharp
public async Task UploadBaselineAsync(BaselineRoot baseline, CancellationToken ct = default)
{
    await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
    string json = JsonConvert.SerializeObject(baseline);
    string url = $"{_serverUrl}/api/v1/global/baseline";
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
    await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
}
```

#### Certificate Pinning

Thumbprint verification is treated as separate from standard certificate validation. When a thumbprint is configured and the certificate does not match, the request is rejected immediately without performing further certificate validation (expired, untrusted CA, etc.).

**Intentional behavioral change from RemoteFactionClient:** The existing `RemoteFactionClient.ValidateCertificate` first checks `if (sslPolicyErrors == SslPolicyErrors.None) return true` — accepting any certificate that passes standard CA validation regardless of thumbprint match. The typed client intentionally removes this early-out: when pinning is configured, ONLY the thumbprint matters. This is stricter and more secure.

```csharp
private HttpClient CreateHttpClient()
{
    var handler = new HttpClientHandler();
    if (!string.IsNullOrEmpty(_trustedThumbprint))
    {
        handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
        {
            // Thumbprint check is separate from standard validation.
            // Mismatch = reject immediately, no further validation performed.
            if (cert == null) return false;
            return string.Equals(
                cert.GetCertHashString(),
                _trustedThumbprint,
                StringComparison.OrdinalIgnoreCase);
        };
    }

    var client = new HttpClient(handler) { Timeout = _timeout };
    string token = SecureStringToString(_bearerToken);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    return client;
}
```


#### Rate Limiting

```csharp
private async Task AcquireRateLimitTokenAsync(CancellationToken ct)
{
    await _rateLimiter.WaitAsync(ct).ConfigureAwait(false);
    // Release token after 60s (sliding window approximation)
    _ = Task.Run(async () =>
    {
        await Task.Delay(60000).ConfigureAwait(false);
        try { _rateLimiter.Release(); } catch (ObjectDisposedException) { }
    });
}

/// <summary>
/// Applies a new rate limit from any source (WebSocket, configuration, etc.).
/// No bounds checking or validation is performed — any positive value is accepted
/// regardless of how restrictive or permissive it is.
/// </summary>
public void ApplyRateLimit(int requestsPerMinute)
{
    if (requestsPerMinute == _rateLimitRequestsPerMinute) return;
    _rateLimitRequestsPerMinute = requestsPerMinute;
    var old = _rateLimiter;
    _rateLimiter = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
    old?.Dispose();
}
```

#### Error Handling (EnsureSuccessOrThrowAsync)

```csharp
private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;

    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

    switch ((int)response.StatusCode)
    {
        case 400:
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<BulkImportErrorResponse>(body);
                var errors = errorResponse?.Errors?.Select(e => new FactionValidationError
                {
                    EntityType = e.EntityType,
                    EntityUUID = e.EntityUUID,
                    Field = e.Field,
                    Error = e.Error,
                }).ToList() ?? new List<FactionValidationError>();
                throw new FactionValidationException(errors);
            }
            catch (JsonException)
            {
                throw new FactionServerException($"HTTP 400: {body}");
            }

        case 403:
            throw new FactionAuthorizationException(body);

        default:
            throw new FactionServerException(
                $"HTTP {(int)response.StatusCode}: {body}");
    }
}
```


---

## Data Models

### New DTOs (in `Common/Client/FactionServer/DTOs/`)

#### BulkImportResult

```csharp
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Response from the bulk import endpoint on success.
    /// </summary>
    public class BulkImportResult
    {
        /// <summary>
        /// Gets or sets the dictionary of collection names to imported entity counts.
        /// </summary>
        [JsonProperty("imported")]
        public Dictionary<string, int> Imported { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the total number of entities imported.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
```

#### SyncResponse

```csharp
using Newtonsoft.Json;
using System;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Response from the /api/v1/sync/snapshot endpoint.
    /// Contains server-level factions, characters, and a timestamp.
    /// </summary>
    public class SyncResponse
    {
        [JsonProperty("factions")]
        public ServerFaction[] Factions { get; set; }

        [JsonProperty("characters")]
        public ServerCharacter[] Characters { get; set; }

        [JsonProperty("serverTimestamp")]
        public DateTime ServerTimestamp { get; set; }
    }
}
```


#### ServerFaction (already in Common at `OE2EmpireTracker.Common/Models/ServerModels.cs`)

`ServerFaction` already exists in `OE2EmpireTracker.Common/Models/ServerModels.cs`. The typed client reuses it directly. The `EntityMetadata Metadata` property is present on the model but omitted from the wire format by `NullValueHandling.Ignore` (it will be null on client-side instances).

```csharp
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OE2EmpireTracker.Common.Models
{
    /// <summary>
    /// Server-level faction with leadership tracking.
    /// Used by both the Faction Server and the typed client.
    /// </summary>
    public class ServerFaction
    {
        [JsonProperty("uuid")]
        public string UUID { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("leaderCharacterUUIDs")]
        public List<string> LeaderCharacterUUIDs { get; set; } = new List<string>();

        public EntityMetadata Metadata { get; set; } = new EntityMetadata();
    }
}
```

#### ServerCharacter (already in Common at `OE2EmpireTracker.Common/Models/ServerModels.cs`)

`ServerCharacter` already exists in `OE2EmpireTracker.Common/Models/ServerModels.cs`. The typed client reuses it directly.

```csharp
using Newtonsoft.Json;

namespace OE2EmpireTracker.Common.Models
{
    /// <summary>
    /// Server-level character record.
    /// Used by both the Faction Server and the typed client.
    /// </summary>
    public class ServerCharacter
    {
        [JsonProperty("uuid")]
        public string UUID { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("factionUUID")]
        public string FactionUUID { get; set; }

        public EntityMetadata Metadata { get; set; } = new EntityMetadata();
    }
}
```

Note: The `EntityMetadata` property is server-internal (tracks `LastModifiedUtc` for storage). On the wire, it will be null for client-created instances and omitted by `NullValueHandling.Ignore`.

#### SharingRuleDto (already exists at `OE2EmpireTracker/Client/SharingRuleDto.cs`)

The existing `SharingRuleDto` in the WinForms project will be moved to `Common/Client/FactionServer/DTOs/SharingRuleDto.cs` so it is shared. It already has the correct shape with `[JsonProperty]` attributes.


### Existing Models Reused (no changes needed)

| Model | Location | Used By |
|---|---|---|
| `PlayerRoot` | `Common/Services/PlayerRoot.cs` | `BulkImportAsync`, `ExportCharacterDataAsync` |
| `BaselineRoot` | `Common/Services/BaselineRoot.cs` | `UploadBaselineAsync` |
| `Colony` | `Common/Models/Colony.cs` | `GetColoniesAsync` |
| `Blueprint` | `Common/Models/Blueprint.cs` | `GetBlueprintsAsync` |
| `Survey` | `Common/Models/Survey.cs` | `GetSurveysAsync` |
| `PlayerProfile` | `Common/Models/PlayerProfile.cs` | `GetPlayerProfilesAsync` |
| `DeliveryRoute` | `Common/Models/DeliveryRoute.cs` | `GetDeliveryRoutesAsync` |
| `DeliveryPlan` | `Common/Models/DeliveryPlan.cs` | `GetDeliveryPlansAsync` |
| `Ship` | `Common/Models/Ship.cs` | `GetShipsAsync` |
| `ShipTemplate` | `Common/Models/ShipTemplate.cs` | `GetShipTemplatesAsync` |
| `MarketListing` | `Common/Models/MarketListing.cs` | `GetMarketListingsAsync` |
| `MarketTransaction` | `Common/Models/MarketTransaction.cs` | `GetMarketTransactionsAsync` |
| `PricingPlan` | `Common/Models/PricingPlan.cs` | `GetPricingPlansAsync` |
| `StockPlan` | `Common/Models/StockPlan.cs` | `GetStockPlansAsync` |
| `StockProfile` | `Common/Models/StockProfile.cs` | `GetStockProfilesAsync` |
| `BuildPlan` | `Common/Models/BuildPlan.cs` | `GetBuildPlansAsync` |
| `SupplyChain` | `Common/Models/SupplyChain.cs` | `GetSupplyChainsAsync` |
| `Asteroid` | `Common/Models/Asteroid.cs` | `GetAsteroidsAsync` |
| `Station` | `Common/Models/Station.cs` | `GetStationsAsync` |
| `Faction` | `Common/Models/Faction.cs` | `GetFactionContactsAsync` |
| `ExternalCharacter` | `Common/Models/ExternalCharacter.cs` | `GetExternalCharactersAsync` |


### Exception Hierarchy (in `Common/Client/FactionServer/Exceptions/`)

```csharp
namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>Base exception for all Faction Server errors.</summary>
    public class FactionServerException : Exception
    {
        public FactionServerException(string message) : base(message) { }
        public FactionServerException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Thrown on HTTP 400 with parsed validation errors.</summary>
    public class FactionValidationException : FactionServerException
    {
        public List<FactionValidationError> Errors { get; }
        public FactionValidationException(List<FactionValidationError> errors)
            : base($"Validation failed with {errors.Count} error(s)")
        {
            Errors = errors;
        }
    }

    /// <summary>A single validation error from the server.</summary>
    public class FactionValidationError
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityUUID { get; set; }
        public string Field { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>Thrown on HTTP 403 (authorization denied).</summary>
    public class FactionAuthorizationException : FactionServerException
    {
        public FactionAuthorizationException(string message)
            : base($"Authorization denied: {message}") { }
    }

    /// <summary>Thrown on network failures or timeouts.</summary>
    public class FactionConnectionException : FactionServerException
    {
        public FactionConnectionException(string message, Exception inner)
            : base(message, inner) { }
    }
}
```


### Wire Format Examples

The server uses System.Text.Json with `CamelCase` naming policy. The domain models use Newtonsoft with PascalCase property names. Here's how the mapping works:

**GET /api/v1/characters/{uuid}/colonies response:**
```json
[
  {
    "uuid": "col-001",
    "name": "Mining Outpost Alpha",
    "systemId": 42,
    "planetName": "Kepler-22b",
    "structures": []
  }
]
```

**PUT /api/v1/characters/{uuid}/import request (serialized with Newtonsoft PascalCase):**
```json
{
  "DataVersion": 0,
  "CurrentPlayerUUID": "player-001",
  "Colony": [...],
  "Blueprint": [...],
  "Survey": [...]
}
```

The server deserializes this with `PropertyNameCaseInsensitive = true`, so PascalCase from the client works.

**PUT /api/v1/characters/{uuid}/import response (success):**
```json
{
  "imported": {
    "colonies": 5,
    "blueprints": 12,
    "surveys": 3
  },
  "total": 20
}
```

**GET /api/v1/sync response:**
```json
{
  "factions": [
    { "uuid": "f-001", "name": "Alliance", "description": "...", "leaderCharacterUUIDs": ["c-001"] }
  ],
  "characters": [
    { "uuid": "c-001", "name": "Captain Rex", "factionUUID": "f-001" }
  ],
  "serverTimestamp": "2024-01-15T10:30:00Z"
}
```

---

## File Organization

```
OE2EmpireTracker.Common/
├── Models/
│   └── ServerModels.cs           (already exists — ServerFaction, ServerCharacter)
└── Client/
    └── FactionServer/
        ├── IFactionServerTypedClient.cs
        ├── FactionServerTypedClient.cs
        ├── DTOs/
        │   ├── BulkImportResult.cs
        │   ├── SyncResponse.cs
        │   └── SharingRuleDto.cs
        └── Exceptions/
            ├── FactionServerException.cs
            ├── FactionValidationException.cs
            ├── FactionAuthorizationException.cs
            └── FactionConnectionException.cs
```


---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: DTO Serialization Round-Trip

*For any* valid DTO instance (BulkImportResult, SyncResponse, ServerFaction, ServerCharacter, SharingRuleDto), serializing to JSON with Newtonsoft.Json then deserializing back SHALL produce an object with equivalent property values.

**Validates: Requirements 4.3, 7.1**

### Property 2: Error Response Classification

*For any* HTTP error response with a status code and body, the `EnsureSuccessOrThrowAsync` method SHALL:
- Throw `FactionValidationException` with parsed errors when status is 400 and body is valid BulkImportErrorResponse JSON
- Throw `FactionServerException` when status is 400 and body is not parseable as BulkImportErrorResponse
- Throw `FactionAuthorizationException` when status is 403
- Throw `FactionServerException` for all other non-success status codes

**Validates: Requirements 4.4, 4.6**

### Property 3: CamelCase Wire Format

*For any* DTO instance serialized with the client's serialization settings, all top-level JSON property keys SHALL be in camelCase format (first character lowercase).

**Validates: Requirements 7.2**


### Property 4: Null Property Omission

*For any* DTO instance with one or more nullable properties set to null, serializing with the client's serialization settings SHALL produce JSON that does not contain keys for the null-valued properties.

**Validates: Requirements 7.3**

### Property 5: Rate Limiter Blocking

*For any* rate limit N (1 ≤ N ≤ 1000), after N requests have been issued without any token release, the (N+1)th call to `AcquireRateLimitTokenAsync` SHALL block (not complete immediately).

**Validates: Requirements 6.2**

### Property 6: Rate Limiter Application

*For any* positive integer rate limit value received from any source, calling `ApplyRateLimit` SHALL update the internal limiter to the new capacity without throwing an exception, regardless of how restrictive or permissive the value is.

**Validates: Requirements 6.3**

### Property 7: HTTP Request Equivalence

*For any* valid method input (characterUUID, entity type), the typed client SHALL send an HTTP request with the correct method (GET/PUT/POST), correct URL path matching the endpoint mapping table, correct Content-Type header for write operations, and a body that is valid JSON representing the input object.

**Validates: Requirements 8.2**

---


## Error Handling

### HTTP Error → Exception Mapping

| HTTP Status | Exception Type | Details |
|---|---|---|
| 400 (parseable body) | `FactionValidationException` | Body parsed as `BulkImportErrorResponse`, errors extracted |
| 400 (unparseable body) | `FactionServerException` | Raw body included in message |
| 403 | `FactionAuthorizationException` | Body included in message |
| 408, 504, timeout | `FactionConnectionException` | Original exception as inner |
| Network failure | `FactionConnectionException` | `HttpRequestException` or `TaskCanceledException` as inner |
| All other 4xx/5xx | `FactionServerException` | Status code and body in message |

### Retry Considerations

The typed client itself does NOT implement retry logic. That responsibility belongs to the `SyncManager` layer which already handles:
- Exponential backoff on `FactionConnectionException`
- Immediate fail on `FactionAuthorizationException` (no retry)
- Validation error surfacing on `FactionValidationException` (no retry, user must fix data)

### Disposal

```csharp
public void Dispose()
{
    if (!_disposed)
    {
        _disposed = true;
        _bearerToken?.Dispose();   // SecureString zeroed
        _httpClient?.Dispose();     // Closes connections
        _rateLimiter?.Dispose();    // Releases semaphore
    }
}
```

The `Dispose` pattern is safe to call multiple times and handles partial initialization (null checks).

---


## Testing Strategy

### Dual Testing Approach

**Unit Tests (example-based):**
- Constructor parameter validation (null URL, null token)
- Certificate pinning acceptance/rejection with mock certificates
- Certificate pinning rejects on thumbprint mismatch without performing standard validation
- Bearer token header attachment verification
- IsConnected property behavior
- Dispose behavior (SecureString zeroed, no double-dispose crash)
- Interface coverage verification (all endpoints have methods)
- SharingRuleDto field mapping verification
- Timeout default (5 minutes) and configurable timeout (any value accepted)

**Property Tests (FsCheck, minimum 100 iterations each):**
- Property 1: DTO round-trip — generate random DTOs, serialize → deserialize → assert equality
- Property 2: Error classification — generate random status codes and bodies → assert correct exception type
- Property 3: CamelCase format — generate random DTOs → serialize → parse JSON keys → assert camelCase
- Property 4: Null omission — generate DTOs with random nulls → serialize → assert null keys absent
- Property 5: Rate limiter blocking — generate random N → issue N requests → verify N+1 blocks
- Property 6: Rate limiter application — generate random positive int → call ApplyRateLimit → verify no throw
- Property 7: HTTP equivalence — generate random characterUUIDs → call method with mock HTTP → verify URL/method/body

### Property Test Configuration

- Library: FsCheck 2.16.6 (already in project)
- Attribute: `[FsCheck.NUnit.Property(MaxTest = 100)]`
- Custom generators for domain models using LINQ query syntax
- Tag format: `Feature: faction-server-typed-client, Property {N}: {description}`

### Test File Location

```
OE2EmpireTracker.Tests/
└── Client/
    └── FactionServer/
        ├── FactionServerTypedClientTests.cs       (unit tests)
        ├── DtoRoundTripPropertyTests.cs           (Property 1)
        ├── ErrorClassificationPropertyTests.cs    (Property 2)
        ├── SerializationFormatPropertyTests.cs    (Properties 3, 4)
        ├── RateLimiterPropertyTests.cs            (Properties 5, 6)
        └── HttpRequestPropertyTests.cs            (Property 7)
```

### Integration Tests

Integration tests verify end-to-end HTTP communication against a real (or TestServer) instance of the Faction Server:
- Health check returns true for running server
- Bulk import with valid PlayerRoot succeeds and returns correct counts
- Bulk import with invalid data throws FactionValidationException with details
- Export returns a deserializable PlayerRoot
- Per-entity GET methods return arrays of the correct type
- Authorization denied (bad token) throws FactionAuthorizationException
- Connection failure (bad URL) throws FactionConnectionException

These live in `OE2EmpireTracker.Server.Tests/` since they need the server project.

### Migration Testing

The migration from `RemoteFactionClient` to `IFactionServerTypedClient` will be verified by:
1. SyncManager compilation with new interface (build check)
2. All existing SyncManager tests passing with typed client mock
3. Manual E2E test: connect to local server → bulk import → export → verify round-trip

---

## Requirements Traceability

| Requirement | Covered By |
|---|---|
| Req 1 (OpenAPI Spec) | Swagger YAML file creation task |
| Req 2 (DTOs) | DTO classes, Property 1, Property 3, Property 4 |
| Req 3 (Interface) | IFactionServerTypedClient definition |
| Req 4 (Implementation) | FactionServerTypedClient, Property 2, Property 7 |
| Req 5 (Cert Pinning + Auth) | Constructor, certificate callback, unit tests |
| Req 6 (Rate Limiting) | Rate limiter implementation, Property 5, Property 6 |
| Req 7 (Round-Trip) | Property 1, Property 3, Property 4 |
| Req 8 (Migration) | Interface coverage, HTTP equivalence, SyncManager wiring |
| Req 9 (Exceptions) | Exception hierarchy, Property 2 |
| Req 10 (Shared Location) | File organization, no new dependencies |

---

## External Spec Updates (to include in implementation tasks)

The following external spec/ and docs/ files must be updated as part of the implementation to maintain spec traceability:

| File | Change |
|---|---|
| `spec/design/services/client-services.md` | Add `IFactionServerTypedClient` and `FactionServerTypedClient` classes. Update SyncManager class diagram to show `IFactionServerTypedClient` dependency instead of `RemoteFactionClient`. |
| `spec/requirements/Sharing.md` | Update sequence diagram participant from `RemoteFactionClient` to `IFactionServerTypedClient` after migration. |
