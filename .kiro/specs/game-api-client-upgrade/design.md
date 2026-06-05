# Technical Design: Game API Client Upgrade

## Overview

This design extends the existing `GameApiClient` in `OE2EmpireTracker.Common/Client/` to cover all endpoints in the OE2 Public API swagger specification. The work adds 14 new client methods (3 asset detail + 11 market) and ~25 new DTO classes for deserialization, plus discovery test coverage.

All new code follows the established patterns exactly: same error handling structure, same rate limiting, same Polly retry/circuit breaker via `ExecuteWithPoliciesAsync`, same `(bool Success, string Json)` return tuple.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  GameApiClient (OE2EmpireTracker.Common/Client/)        │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Existing methods (character, colonies, banking,   │  │
│  │ assets/locations, killmails, mail, ship, jobs)    │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ NEW: Asset detail methods (crate, survey, bp)     │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ NEW: Market methods (listings, prices, items,     │  │
│  │ ship components, buy/sell orders, competitors)    │  │
│  └───────────────────────────────────────────────────┘  │
│  Infrastructure: ExecuteWithPoliciesAsync, rate limit,  │
│  circuit breaker, token cache (all unchanged)           │
└─────────────────────────────────────────────────────────┘
         │ returns (bool, string json)
         ▼
┌─────────────────────────────────────────────────────────┐
│  DTO Models (OE2EmpireTracker.Common/Models/)           │
│  ┌─────────────────┐  ┌──────────────────────────────┐ │
│  │ Existing:       │  │ NEW:                         │ │
│  │ GameApiAsset*   │  │ GameApiColonySummary*        │ │
│  │ GameApiToken*   │  │ GameApiCrateContents*        │ │
│  │ GameApiProfile* │  │ GameApiSurvey*               │ │
│  │ GameApiColony*  │  │ GameApiBlueprintDetail*      │ │
│  │ GameApiSkill*   │  │ GameApiMarketListing*        │ │
│  │                 │  │ GameApiMarketPriceStats*     │ │
│  │                 │  │ GameApiMarketItem*           │ │
│  │                 │  │ GameApiMarketShipComponent*  │ │
│  │                 │  │ GameApiMarketBuyOrder*       │ │
│  │                 │  │ GameApiMarketSellOrder*      │ │
│  │                 │  │ GameApiMarketCompetitor*     │ │
│  │                 │  │ GameApiShipConfiguration*    │ │
│  │                 │  │ GameApiShipCargo*            │ │
│  │                 │  │ GameApiAcceptedJobs*         │ │
│  └─────────────────┘  └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### GameApiClient (Modified)

The existing `GameApiClient` class gains 14 new public methods. No new interfaces or classes are introduced at the client layer — all methods follow the same `Task<(bool Success, string Json)>` contract.

#### Asset Detail Methods

| Method | Path | Parameters | Scope | Has 404? |
|--------|------|-----------|-------|----------|
| `GetAssetCrateAsync` | `/v1/assets/crates/{crateId}` | crateId (int) | assets.locations.read | Yes |
| `GetAssetSurveyAsync` | `/v1/assets/surveys/{surveyId}` | surveyId (int) | assets.surveys.read | Yes |
| `GetAssetBlueprintAsync` | `/v1/assets/blueprints/{blueprintId}` | blueprintId (int) | assets.blueprints.read | Yes |

#### Market Methods

| Method | Path | Parameters | Scope | Has 404? |
|--------|------|-----------|-------|----------|
| `GetMarketListingsAsync` | `/v1/market/listings` | view (required), range?, search?, type?, subType?, evolution?, orderBy?, orderByDirection? | market.listings.read | No |
| `GetMarketPricesAsync` | `/v1/market/prices` | type (required), typeId (required, long), daysBack?, buyOrders? | market.prices.read | No |
| `GetMarketItemsAsync` | `/v1/market/items` | type (required), search (required) | market.items.read | No |
| `GetMarketShipComponentsAsync` | `/v1/market/ships/{marketId}/components` | marketId (long) | market.listings.read | Yes |
| `GetMarketBuyOrdersAsync` | `/v1/market/orders/buy` | (none) | market.orders.read | No |
| `GetMarketSellOrdersAsync` | `/v1/market/orders/sell` | (none) | market.orders.read | No |
| `GetMarketBuyCompetitorsAsync` | `/v1/market/orders/buy/competitors` | marketIds (string, comma-separated) | market.competitors.read | No |
| `GetMarketSellCompetitorsAsync` | `/v1/market/orders/sell/competitors` | marketIds (string, comma-separated) | market.competitors.read | No |


### Client Method Template

Every new method follows this exact template (matching existing methods like `GetColonyBuildingsAsync`):

```csharp
public async Task<(bool Success, string Json)> Get{Endpoint}Async(string appId, string accessToken, ...)
{
    if (string.IsNullOrEmpty(accessToken))
    {
        return (false, null);
    }

    try
    {
        await AcquireRateLimitTokenAsync().ConfigureAwait(false);

        var response = await ExecuteWithPoliciesAsync(
            HttpMethod.Get,
            _serverUrl + "/v1/{path}",
            appId,
            accessToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (true, json);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Log.Warn("Game API Get{Endpoint} received HTTP 401 — token is invalid or expired");
            return (false, "401");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            Log.Warn("Game API Get{Endpoint} received HTTP 403 — {scope} scope not granted");
            return (false, "403");
        }

        // 404 only for path-parameter endpoints
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Log.Warn("Game API Get{Endpoint} received HTTP 404 — resource not found or not owned");
            return (false, "404");
        }

        Log.Warn("Game API Get{Endpoint} failed: HTTP {0}", (int)response.StatusCode);
        return (false, null);
    }
    catch (BrokenCircuitException)
    {
        Log.Warn("Game API Get{Endpoint} blocked by open circuit breaker");
        return (false, null);
    }
    catch (HttpRequestException ex)
    {
        Log.Warn(ex, "Game API Get{Endpoint} request failed");
        return (false, null);
    }
    catch (TaskCanceledException)
    {
        Log.Warn("Game API Get{Endpoint} request timed out");
        return (false, null);
    }
}
```

### Query String Construction

Market endpoints with query parameters build URLs dynamically using `Uri.EscapeDataString`:

```csharp
// GetMarketListingsAsync — view is always required, others optional
string url = _serverUrl + "/v1/market/listings?view=" + Uri.EscapeDataString(view);
if (range.HasValue) url += "&range=" + range.Value;
if (!string.IsNullOrEmpty(search)) url += "&search=" + Uri.EscapeDataString(search);
// ... other optional params omitted when null/empty

// GetMarketPricesAsync — type and typeId required, daysBack and buyOrders optional
string url = _serverUrl + "/v1/market/prices?type=" + Uri.EscapeDataString(type) + "&typeId=" + typeId;
if (daysBack.HasValue) url += "&daysBack=" + daysBack.Value;
if (buyOrders.HasValue) url += "&buyOrders=" + buyOrders.Value.ToString().ToLowerInvariant();

// GetMarketItemsAsync — type and search both required
string url = _serverUrl + "/v1/market/items?type=" + Uri.EscapeDataString(type)
    + "&search=" + Uri.EscapeDataString(search);

// Competitor endpoints — marketIds is a comma-separated string
string url = _serverUrl + "/v1/market/orders/buy/competitors?marketIds=" + Uri.EscapeDataString(marketIds);
```

## Data Models

### File Organization

New DTOs are organized into separate files by domain area in `OE2EmpireTracker.Common/Models/`:

| File | Classes |
|------|---------|
| `GameApiColonySummaryResponse.cs` | GameApiColonySummaryResponse, GameApiColonyNotice, GameApiColonyIndustry, GameApiColonyOperation, GameApiColonyDurability, GameApiColonyOperationalEfficiency |
| `GameApiCrateContentsResponse.cs` | GameApiCrateContentsResponse (reuses GameApiAssetCargoItem) |
| `GameApiSurveyResponse.cs` | GameApiSurveyResponse, GameApiSurveyDetail, GameApiSurveyResource |
| `GameApiBlueprintDetailResponse.cs` | GameApiBlueprintDetailResponse, GameApiBlueprintInfo, GameApiBlueprintResource |
| `GameApiMarketListingsResponse.cs` | GameApiMarketListingsResponse, GameApiMarketListing, GameApiMarketListingProperty, GameApiMarketListingResource |
| `GameApiMarketPriceStatsResponse.cs` | GameApiMarketPriceStatsResponse |
| `GameApiMarketItemsResponse.cs` | GameApiMarketItemsResponse, GameApiMarketItem |
| `GameApiMarketShipComponentsResponse.cs` | GameApiMarketShipComponentsResponse, GameApiMarketShipComponent |
| `GameApiMarketOrdersResponse.cs` | GameApiMarketBuyOrdersResponse, GameApiMarketBuyOrder, GameApiMarketSellOrdersResponse, GameApiMarketSellOrder |
| `GameApiMarketCompetitorsResponse.cs` | GameApiMarketCompetitorOrdersResponse, GameApiMarketOrderCompetitors, GameApiMarketCompetitor |
| `GameApiShipConfigurationResponse.cs` | GameApiShipConfigurationResponse, GameApiShipSummary, GameApiShipComponent, GameApiShipComponentProperty, GameApiShipComponentMunition |
| `GameApiShipCargoResponse.cs` | GameApiShipCargoResponse (reuses GameApiAssetCargoItem) |
| `GameApiAcceptedJobsResponse.cs` | GameApiAcceptedJobsResponse, GameApiAcceptedJob |

### DTO Conventions

1. Namespace: `OE2EmpireTracker.Client` (same as existing DTOs)
2. All properties use `[JsonProperty("camelCaseName")]` from Newtonsoft.Json
3. Nullable types for `nullable: true` fields in swagger spec
4. `string` properties default to `string.Empty`
5. `List<T>` properties default to `new List<T>()`
6. XML doc comments on every class and property
7. Copyright header matching existing files

### Type Mapping from Swagger

| Swagger Type | C# Type | Nullable C# |
|---|---|---|
| integer/int32 | `int` | `int?` |
| integer/int64 | `long` | `long?` |
| number/double | `double` | `double?` |
| boolean | `bool` | — |
| string | `string` | `string` |
| string/date-time | `DateTime` | `DateTime?` |
| array | `List<T>` | — |

### Shared Types Reuse

Existing types reused directly (no new classes needed):
- `GameApiAssetCargoItem` — used by crate contents and ship cargo DTOs
- `GameApiAssetItemProperty` — used by blueprint detail DTO for blueprintProperties array

New market-specific types (swagger defines them with fewer fields than `GameApiAssetItemProperty`):
- `GameApiMarketListingProperty` — modTypeId (int?), propertyName, friendlyPropertyName, propertyValue (double), unit
- `GameApiMarketListingResource` — resourceId, resourceName, resourceIcon, resourceAmount, rarityClassification
- `GameApiShipComponentProperty` — propertyName, friendlyPropertyName, propertyValue (double), unit


## Error Handling

All new methods follow the same error handling pattern as existing methods:

1. **Pre-request validation**: If `accessToken` is null or empty, return `(false, null)` immediately without network call
2. **HTTP 200**: Return `(true, json)` with the raw response body
3. **HTTP 401**: Log token invalid/expired, return `(false, "401")`
4. **HTTP 403**: Log scope not granted (naming the specific scope), return `(false, "403")`
5. **HTTP 404** (path-parameter endpoints only): Log resource not found, return `(false, "404")`
6. **Other HTTP errors**: Log the status code, return `(false, null)`
7. **BrokenCircuitException**: Log circuit breaker open, return `(false, null)`
8. **HttpRequestException**: Log exception details, return `(false, null)`
9. **TaskCanceledException**: Log timeout, return `(false, null)`

The failure tuple is always returned regardless of whether logging succeeds. Rate limiting is enforced before every request via `AcquireRateLimitTokenAsync()`.

## Testing Strategy

### Discovery Tests (Integration)

The `GameApiFullDiscoveryTests` fixture is extended with 11 new test methods that call real API endpoints. These tests:
- Use `[Test, Order(N)]` for sequencing (asset detail at Order 7-9, market at Order 16-23)
- Skip gracefully on HTTP 403 (scope not granted) via `RecordSkipped`
- Save raw JSON responses to files for offline DTO validation
- Store shared state (e.g., listings JSON) for dependent tests (prices needs typeId from listings)

### Test Ordering

| Order | Category | Test |
|-------|----------|------|
| 7 | assets | `GetAssetCrateDetail` — find first crate in location detail, fetch contents |
| 8 | assets | `GetAssetSurveyDetail` — find first survey, fetch detail |
| 9 | assets | `GetAssetBlueprintDetail` — find first blueprint, fetch detail |
| 16 | market | `GetMarketListings` — fetch with view "All" |
| 17 | market | `GetMarketPrices` — use type/typeId from first listing |
| 18 | market | `GetMarketItems` — search for a common type |
| 19 | market | `GetMarketShipComponents` — find ship listing, fetch components |
| 20 | market | `GetMarketBuyOrders` — fetch buy orders |
| 21 | market | `GetMarketSellOrders` — fetch sell orders |
| 22 | market | `GetMarketBuyCompetitors` — use first 5 buy order marketIds |
| 23 | market | `GetMarketSellCompetitors` — use first 5 sell order marketIds |

### Output Structure

```
TestOutput/
├── assets/
│   ├── crate-{crateId}.json   (NEW)
│   ├── survey-{surveyId}.json (NEW)
│   └── blueprint-{bpId}.json  (NEW)
└── market/                     (NEW directory)
    ├── listings.json
    ├── prices.json
    ├── items.json
    ├── ship-components.json
    ├── buy-orders.json
    ├── sell-orders.json
    ├── buy-competitors.json
    └── sell-competitors.json
```

## Correctness Properties

### Property 1: Token Validation Guard

**Validates: Requirements 16.1**

For every new client method M with parameter accessToken:
- `M(appId, null)` returns `(false, null)` without making a network request
- `M(appId, "")` returns `(false, null)` without making a network request

### Property 2: HTTP Status Code Mapping

**Validates: Requirements 16.2, 16.3, 16.4, 16.5, 16.7, 16.8**

For every new client method M:
- HTTP 200 produces `(true, non-null json string)`
- HTTP 401 produces `(false, "401")`
- HTTP 403 produces `(false, "403")`
- HTTP 404 (path-parameter endpoints only) produces `(false, "404")`
- Other HTTP errors produce `(false, null)`

### Property 3: Exception Mapping

**Validates: Requirements 16.3, 16.4, 16.5**

For every new client method M:
- `BrokenCircuitException` produces `(false, null)`
- `HttpRequestException` produces `(false, null)`
- `TaskCanceledException` produces `(false, null)`

### Property 4: Rate Limiting Enforcement

**Validates: Requirements 16.6**

Every new client method calls `AcquireRateLimitTokenAsync()` before making the HTTP request. No request is sent without first acquiring a rate limit token.

### Property 5: Query Parameter Completeness

**Validates: Requirements 18.1, 18.2, 18.3, 18.4**

For `GetMarketListingsAsync`:
- The `view` parameter is always present in the query string (it is required)
- Null/empty optional parameters are omitted from the query string entirely
- The `search` parameter is URL-encoded via `Uri.EscapeDataString`

### Property 6: DTO Schema Completeness

**Validates: Requirements 17.1, 17.2, 17.3, 17.4, 17.5, 17.6**

For each DTO class, every field defined in the corresponding swagger schema has a matching C# property with a `[JsonProperty]` attribute. No swagger fields are omitted.

### Property 7: DTO Type Correctness

**Validates: Requirements 17.3, 17.4**

Every DTO property uses the C# type that correctly maps from the swagger type:
- `integer/int32` maps to `int` (or `int?` if nullable)
- `integer/int64` maps to `long` (or `long?` if nullable)
- `number/double` maps to `double` (or `double?` if nullable)
- `boolean` maps to `bool`
- `string` maps to `string`
- `string/date-time` maps to `DateTime` (or `DateTime?` if nullable)
- Arrays map to `List<T>`

## Design Decisions

1. **Raw JSON return (no deserialization in client)**: The client returns raw JSON strings. Deserialization to DTOs is the caller's responsibility. This matches the existing pattern and keeps the client lightweight.

2. **Separate DTO files per domain**: Market DTOs get their own files rather than one giant file. Keeps file sizes manageable and related types easy to find.

3. **Market-specific property types vs reusing GameApiAssetItemProperty**: The market listing property schema has fewer fields (no evolution, originalPropertyValue, researchPositive, canResearch). A separate `GameApiMarketListingProperty` accurately represents the API contract.

4. **No pagination for market endpoints**: The swagger spec defines no pagination parameters for market listings, orders, or competitors. The API returns all matching results in one call.

5. **Competitors use same response DTO for buy and sell**: The swagger spec uses the same `marketCompetitorOrders` schema for both. One DTO class serves both.

6. **Uri.EscapeDataString for query parameters**: Properly handles the `||` multi-term separator in market search terms.

## Dependencies

- No new NuGet packages required
- Uses existing: Newtonsoft.Json, NLog, Polly, System.Net.Http
- All code targets .NET Framework 4.8.1

## Files Modified/Created

### Modified
- `OE2EmpireTracker.Common/Client/GameApiClient.cs` — add 14 new public methods
- `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs` — add 11 new test methods

### Created (in OE2EmpireTracker.Common/Models/)
- `GameApiColonySummaryResponse.cs`
- `GameApiCrateContentsResponse.cs`
- `GameApiSurveyResponse.cs`
- `GameApiBlueprintDetailResponse.cs`
- `GameApiMarketListingsResponse.cs`
- `GameApiMarketPriceStatsResponse.cs`
- `GameApiMarketItemsResponse.cs`
- `GameApiMarketShipComponentsResponse.cs`
- `GameApiMarketOrdersResponse.cs`
- `GameApiMarketCompetitorsResponse.cs`
- `GameApiShipConfigurationResponse.cs`
- `GameApiShipCargoResponse.cs`
- `GameApiAcceptedJobsResponse.cs`
