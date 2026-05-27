# Game API Integration

## API Reference

- **Swagger JSON:** https://oe2-pub-api-dev.azure-api.net/swagger.json
- **Swagger UI:** https://oe2-pub-api-dev.azure-api.net/swagger/ui#/
- **Base URL:** https://oe2-pub-api-dev.azure-api.net

## Authentication

OAuth2 client_credentials flow. The client exchanges appId + clientId + secret for an access token via the token endpoint.

## Response Envelope

All responses are wrapped in a `ServiceResponse<T>` envelope:

```json
{
  "success": true,
  "returnCode": 0,
  "returnString": "",
  "data": { ... }
}
```

## Endpoints Used

| Endpoint | Scope | Description |
|----------|-------|-------------|
| `GET /v1/characters` | character.read | Player profile (name, UUID, skills) |
| `GET /v1/characters/skills` | character.read | Player skill list |
| `GET /v1/colonies` | colony.list.read | Colony list for the authenticated character |
| `GET /v1/colonies/{colonyId}/buildings` | colony.buildings.read | Buildings and structures for a colony |
| `GET /v1/colonies/{colonyId}/warehouse` | colony.warehouse.read | Warehouse contents for a colony |
| `GET /v1/colonies/{colonyId}/workers` | colony.workers.read | Workforce, commodity demands, wages for a colony |

## HTTP Status Codes

| Code | Meaning | Client Handling |
|------|---------|-----------------|
| 200 | Success | Deserialize response body |
| 401 | Unauthorized | Token expired or invalid — re-authenticate |
| 403 | Forbidden | Scope not granted or colony lacks Remote Operations Array |
| 404 | Not Found | Colony not found or not owned by this character |
| 429 | Rate Limited | Back off per Retry-After header |
| 500/502/503/504 | Server Error | Polly retry with exponential backoff |

## Rate Limiting

The API returns rate limit headers:
- `X-RateLimit-Remaining` — requests remaining in current window
- `Retry-After` — seconds to wait when rate limited (HTTP 429)

The client uses a SemaphoreSlim-based sliding window to stay within limits proactively.

## Implementation

- **Client:** `OE2EmpireTracker.Common/Client/GameApiClient.cs`
- **Scheduler:** `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
- **Production wiring:** `OE2EmpireTracker.Common/Services/ProductionSyncScheduler.cs`
- **Merge logic:** `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`
- **Context:** `OE2EmpireTracker.Common/Client/GameApiContext.cs`
- **Credentials:** `OE2EmpireTracker.Common/Client/GameApiCredentialManager.cs`
- **Connection monitor:** `OE2EmpireTracker.Common/Client/GameApiConnectionMonitor.cs`
