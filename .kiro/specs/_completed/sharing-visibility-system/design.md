# Design Document: Sharing Visibility System

## Overview

This design addresses three gaps in the existing sharing/visibility infrastructure:

1. **Public data endpoints** — The server's `/api/v1/public/{dataType}` endpoints currently return empty arrays. They need to query sharing rules for `TargetType = Public` and return matching entity data with pagination.

2. **Web UI shared data view** — `SharedDataView.tsx` calls non-existent endpoints (`/shared-data`, `/shared-data/{sharerUUID}/{dataType}`). It must be rewired to use the existing `/shared-with-me/{dataType}` endpoint, restructuring the UI to a data-type-first navigation model.

3. **Desktop sharing configuration** — No `FormSharing` exists. A new WinForms form provides CRUD for sharing rules via the existing GET/PUT sharing API.

Additionally, the `SharingTargetType` enum needs a `Public` value added across server, web UI, and desktop app.

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| SharedDataView navigation model | Data-type tabs first, entities listed per tab | The server API is per-dataType (`/shared-with-me/{dataType}`), not per-sharer. A tab-per-type model maps directly to the API without client-side aggregation. |
| Sharer grouping in shared data | Include `ownerCharacterUUID` and `ownerCharacterName` in response | The server already iterates per-character; annotating each entity with its owner is trivial and enables client-side grouping. |
| Desktop FormSharing editing | DataGridView with inline editing | Consistent with other forms (Contacts, StockTargets). Rules are simple (3 fields) — a separate dialog adds unnecessary complexity. |
| Public endpoint performance | Scan all characters' rules on each request | The dataset is small (dozens of characters, not millions). Caching/indexing is premature optimization. If needed later, add a materialized view of public rules. |
| Rule display source flexibility | Apps MAY display rules from any source (cache, local state) for read-only display | Only management operations (create/update/delete) are required to use the server endpoints. This allows optimistic UI updates and offline display. |
| Immediate UI update after save | Both apps update their local rule list immediately after a successful save, without requiring navigation away and back | Ensures Req 6.6 — the user sees their new rule in the same view instantly. Web UI uses optimistic cache update; Desktop app updates the grid from the saved data. |


## Architecture

```mermaid
graph TD
    subgraph Server ["OE2EmpireTracker.Server"]
        PE[PublicDataEndpoints.cs]
        SE[SharingEndpoints.cs]
        SB[IStorageBackend]
        M[Models.cs - SharingTargetType enum]
    end

    subgraph WebUI ["OE2EmpireTracker.Web"]
        SC[SharingConfig.tsx]
        SDV[SharedDataView.tsx]
        SDA[shared-data.ts API module]
        SHA[sharing.ts API module]
        GT[generated.ts types]
    end

    subgraph Desktop ["OE2EmpireTracker (WinForms)"]
        FS[FormSharing]
        RFC[RemoteFactionClient]
        SCX[ServerContext]
    end

    SC -->|GET/PUT /sharing| SE
    SDV -->|GET /shared-with-me/{dataType}| SE
    FS -->|GET/PUT /sharing| SE
    PE -->|query rules + entities| SB
    SE -->|CRUD rules| SB

    SC --> SHA
    SDV --> SDA
    FS --> RFC
    RFC --> SCX
```

The architecture is a thin integration layer connecting existing components:

- **Server**: Add `Public` to `SharingTargetType` enum, implement public endpoint logic in `PublicDataEndpoints.cs`, add validation in `SharingEndpoints.cs`.
- **Web UI**: Rewire `SharedDataView.tsx` to use `/shared-with-me/{dataType}`, add `Public` to `SharingConfig.tsx` target types, update `generated.ts`.
- **Desktop**: New `FormSharing` in `Forms/Sharing/` folder, new sharing methods on `RemoteFactionClient`.

**Cross-app rule display (Req 6.1):** Both apps MUST use the server API endpoints (GET/PUT `/characters/{uuid}/sharing`) for all management operations (create, update, delete). However, apps MAY display rules from any source (including local cache or optimistic state) for read-only display purposes. This enables immediate UI updates after save without waiting for a server round-trip.


## Components and Interfaces

### Server Components

#### 1. SharingTargetType Enum (Models.cs)

Add `Public` value to the existing enum:

```csharp
public enum SharingTargetType
{
    Faction,
    Character,
    Public,
}
```

#### 2. PublicDataEndpoints.cs — Implementation

Replace placeholder logic with actual data retrieval:

```csharp
private static async Task<IResult> GetPublicBlueprints(
    HttpContext httpContext,
    IStorageBackend storage,
    int page = 1,
    int pageSize = 20)
{
    page = Math.Max(1, page);
    // STRICT maximum: pageSize is always clamped to 100 regardless of available data volume.
    // This is an absolute ceiling, not a soft default.
    pageSize = Math.Clamp(pageSize, 1, 100);

    var allCharacters = await storage.GetAllCharactersAsync();
    var publicEntities = new List<object>();

    foreach (var character in allCharacters)
    {
        var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
        var hasPublicBlueprints = rules.Any(r =>
            r.TargetType == SharingTargetType.Public &&
            (r.DataType == null || r.DataType == "Blueprints"));

        if (!hasPublicBlueprints) continue;

        var blueprints = await storage.GetAllBlueprintsAsync(character.UUID);
        publicEntities.AddRange(blueprints);
    }

    var totalCount = publicEntities.Count;
    var items = publicEntities.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

    return Results.Ok(new PaginatedResult(items, page, pageSize, totalCount));
}
```

The same pattern applies to surveys and colonies. Extract a shared helper to reduce duplication.

#### 3. SharingEndpoints.cs — Validation Enhancement

Add validation for empty `TargetUUID` and invalid `TargetType` in `UpdateSharingConfig`.

**Server-side validation independence (Req 7.7):** The server MUST enforce all validation rules independently of any client-side validation. Even if a client bypasses its own UI validation (e.g., via direct API call, modified client, or disabled JavaScript), the server SHALL reject invalid input with appropriate 400 errors. Server validation is the authoritative gate — client validation is a UX convenience only.

```csharp
foreach (var rule in rules)
{
    if (string.IsNullOrWhiteSpace(rule.TargetUUID))
    {
        return Results.BadRequest(new { error = "TargetUUID must not be empty" });
    }
    // TargetType validation is handled by JSON deserialization (enum)
    rule.OwnerCharacterUUID = uuid;
    if (string.IsNullOrWhiteSpace(rule.Id))
    {
        rule.Id = Guid.NewGuid().ToString();
    }
}
```


#### 4. SharedWithMe Response Enhancement

The existing `GetSharedWithMe` endpoint returns a flat list of entities without owner attribution. To support client-side grouping by sharer, wrap each entity with owner metadata:

```csharp
// In GetSharedWithMe, when aggregating entities per character:
var wrapper = new
{
    ownerCharacterUUID = character.UUID,
    ownerCharacterName = character.Name,
    entities = filteredEntities
};
aggregated.Add(wrapper);
```

Alternatively, return a flat list with an `ownerCharacterUUID` field injected into each JSON entity. The flat approach is simpler for the existing CrossReferenceFilter pipeline. Decision: use a wrapper object per sharer to avoid mutating entity JSON.

Response shape:
```json
[
  {
    "ownerCharacterUUID": "abc-123",
    "ownerCharacterName": "PlayerOne",
    "entities": [ { ...blueprint... }, { ...blueprint... } ]
  },
  {
    "ownerCharacterUUID": "def-456",
    "ownerCharacterName": "PlayerTwo",
    "entities": [ { ...blueprint... } ]
  }
]
```

### Web UI Components

#### 5. shared-data.ts — Rewired API Module

Replace non-existent endpoints with the real `/shared-with-me/{dataType}` endpoint:

```typescript
export interface SharedWithMeGroup {
  ownerCharacterUUID: string;
  ownerCharacterName: string;
  entities: unknown[];
}

export const sharedDataApi = {
  getSharedWithMe: (charUUID: string, dataType: string) =>
    apiClient
      .get(`api/v1/characters/${charUUID}/shared-with-me/${dataType}`)
      .json<SharedWithMeGroup[]>(),
};
```

#### 6. SharedDataView.tsx — Restructured UI

Replace the master-detail (sharer list → data type tabs) with a simpler data-type-first model:

- **Top-level tabs**: Blueprints | Surveys | Colonies
- **Per tab**: Fetch `/shared-with-me/{dataType}`, display entities grouped by `ownerCharacterName`
- **Empty state**: "No data has been shared with you yet." — displays ONLY this message with no placeholder content, no cached data, and no skeleton UI. The empty state is a clean, unambiguous indicator.
- **Error state**: RetryableError component with retry button

#### 7. SharingConfig.tsx — Add Public Target Type

Add `'Public'` to the `TARGET_TYPES` array:

```typescript
const TARGET_TYPES: SharingTargetType[] = ['Character', 'Faction', 'Public'];
```

When `Public` is selected, the Target UUID field should be hidden or auto-filled (public rules don't target a specific entity). Use a sentinel value like `"public"` for TargetUUID, or conditionally hide the field.

**Immediate UI update (Req 6.6):** After a successful PUT, the Web UI SHALL update the displayed rules list immediately using the saved data (optimistic update or cache invalidation + refetch). The user MUST see the new/modified rule without navigating away and back.

#### 8. generated.ts — Type Update

```typescript
export type SharingTargetType = 'Faction' | 'Character' | 'Public';
```


### Desktop App Components

#### 9. RemoteFactionClient — Sharing Methods

Add two methods to the existing client:

```csharp
/// <summary>
/// Gets sharing rules for the specified character.
/// </summary>
public async Task<string> GetSharingRulesAsync(string characterUUID)
{
    string path = string.Format("/characters/{0}/sharing", characterUUID);
    return await GetStringAsync(path).ConfigureAwait(false);
}

/// <summary>
/// Replaces all sharing rules for the specified character.
/// </summary>
public async Task<HttpResponseMessage> PutSharingRulesAsync(
    string characterUUID, string rulesJson)
{
    string path = string.Format("/characters/{0}/sharing", characterUUID);
    await AcquireRateLimitTokenAsync().ConfigureAwait(false);
    var content = new StringContent(rulesJson, Encoding.UTF8, "application/json");
    var response = await _httpClient.PutAsync(
        _serverUrl + ApiPrefix + path, content).ConfigureAwait(false);
    return response;
}
```

#### 10. FormSharing — New WinForms Form

Located at `Forms/Sharing/FormSharing.cs` (with `.Designer.cs` and `.resx`).

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│ FormSharing                                    [_][□][X] │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐ │
│ │ DataGridView (dgvRules)                             │ │
│ │ ┌──────────┬──────────────────┬──────────────────┐  │ │
│ │ │ Target   │ Target UUID      │ Data Type        │  │ │
│ │ │ Type     │                  │                  │  │ │
│ │ ├──────────┼──────────────────┼──────────────────┤  │ │
│ │ │ Faction ▼│ abc-123-def      │ Blueprints     ▼│  │ │
│ │ │ Public  ▼│ (auto)           │ All            ▼│  │ │
│ │ └──────────┴──────────────────┴──────────────────┘  │ │
│ └─────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ [Add Rule]  [Delete Selected]  [Save]  [Reload]    │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Behavior:**
- Implements `IProgrammaticUpdateSource`
- Subscribes to `CurrentPlayerChanged` → reloads rules
- DataGridView columns:
  - `colTargetType`: `DataGridViewComboBoxColumn` with items `["Faction", "Character", "Public"]`
  - `colTargetUUID`: `DataGridViewTextBoxColumn` (editable)
  - `colDataType`: `DataGridViewComboBoxColumn` with items `["All", "Colonies", "Blueprints", "Surveys"]`
- Add Rule: inserts a new row with defaults (Faction, empty UUID, All)
- Delete Selected: removes selected row(s)
- Save: validates no empty Target UUIDs (except Public rules), serializes to JSON, calls `PutSharingRulesAsync`
- Save disables the Save button (control is disabled, not just programmatically blocked) when any non-Public rule has an empty Target UUID field
- **Immediate UI update (Req 6.6):** After a successful save, the grid SHALL reflect the saved rules immediately — either by updating from the server response or by treating the local grid state as authoritative. The user MUST NOT need to navigate away and back to see the saved rule.
- Reload: re-fetches from server
- DataError handler on dgvRules (per steering rules)
- NLog Logger
- Unsubscribes from events in `OnFormClosed`

#### 11. MainWindow — Menu Item

Add a "Sharing" menu item under the existing menu structure that opens `FormSharing`.


## Data Models

### SharingRule (Server — existing, unchanged)

```csharp
public class SharingRule
{
    public string Id { get; set; } = string.Empty;
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public string TargetUUID { get; set; } = string.Empty;
    public SharingTargetType TargetType { get; set; }
    public string? DataType { get; set; }       // null = all types
    public string? EntityUUID { get; set; }     // null = all entities of type
}
```

### SharingTargetType (Server — modified)

```csharp
public enum SharingTargetType
{
    Faction,
    Character,
    Public,
}
```

### PaginatedResult (Server — existing, unchanged)

```csharp
private record PaginatedResult(
    object[] items,
    int page,
    int pageSize,
    int totalCount);
```

### SharedWithMeGroup (Server — new response DTO)

```csharp
public record SharedWithMeGroup(
    string OwnerCharacterUUID,
    string OwnerCharacterName,
    JsonElement[] Entities);
```

### TypeScript Types (Web UI)

```typescript
// generated.ts
export type SharingTargetType = 'Faction' | 'Character' | 'Public';

// shared-data.ts
export interface SharedWithMeGroup {
  ownerCharacterUUID: string;
  ownerCharacterName: string;
  entities: unknown[];
}
```

### Desktop App — No New Models

The desktop app will deserialize sharing rules using `Newtonsoft.Json` into a local DTO class or directly into `JArray`/`JObject` for grid population. A lightweight DTO is preferred:

```csharp
// In a new file: Client/SharingRuleDto.cs
public class SharingRuleDto
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("ownerCharacterUUID")]
    public string OwnerCharacterUUID { get; set; }

    [JsonProperty("targetUUID")]
    public string TargetUUID { get; set; }

    [JsonProperty("targetType")]
    public string TargetType { get; set; }

    [JsonProperty("dataType")]
    public string DataType { get; set; }

    [JsonProperty("entityUUID")]
    public string EntityUUID { get; set; }
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Public Data Filtering

*For any* set of characters with sharing rules and entity data, the public data endpoint for a given data type SHALL return exactly those entities owned by characters who have at least one sharing rule with `TargetType = Public` and `DataType` of either `null` or matching the requested type. No other entities shall be included, and no matching entities shall be excluded. Public endpoints require no authentication under any circumstances — there are no auth bypass exceptions or conditional access checks.

**Validates: Requirements 1.1, 1.2, 1.3, 5.3**

### Property 2: Pagination Slice Correctness

*For any* total result set of N items and any valid `page` (≥1) and `pageSize` (1–100) parameters, the paginated response SHALL contain exactly `min(pageSize, N - (page-1)*pageSize)` items starting at offset `(page-1)*pageSize`, and `totalCount` SHALL equal N. If `pageSize` exceeds 100, it SHALL be clamped to 100. If `page` is less than 1, it SHALL be treated as 1.

**Validates: Requirements 1.4, 1.5**

### Property 3: Server Rejects Invalid Sharing Rules

*For any* sharing rule submission where `TargetUUID` is empty/whitespace OR `TargetType` is not a valid enum value, the server SHALL return HTTP 400 and SHALL NOT persist the rules.

**Validates: Requirements 7.1, 7.2**

### Property 4: Server Normalizes Submitted Rules

*For any* set of sharing rules submitted via PUT, regardless of the client-provided `OwnerCharacterUUID` and `Id` values, the server SHALL overwrite `OwnerCharacterUUID` with the authenticated character's UUID on every rule, and SHALL assign a non-empty GUID to any rule with an empty or missing `Id`.

**Validates: Requirements 7.3, 7.4**

### Property 5: Target UUID Validation Blocks Empty Input

*For any* string that is empty or composed entirely of whitespace characters, the sharing rule save action (both Web UI and Desktop) SHALL be blocked — the Web UI disables the save button, and the Desktop app disables the Save control (Enabled = false, greyed out). The server independently enforces this validation regardless of client state (Req 7.7).

**Validates: Requirements 4.2, 7.5, 7.6, 7.7**


## Error Handling

### Server

| Scenario | Response | Details |
|----------|----------|---------|
| Empty request body on PUT /sharing | 400 Bad Request | `{ "error": "Request body is required" }` |
| Invalid JSON body | 400 Bad Request | `{ "error": "Invalid JSON body. Expected array of SharingRule." }` |
| Rule with empty TargetUUID | 400 Bad Request | `{ "error": "TargetUUID must not be empty" }` |
| Rule with invalid TargetType | 400 Bad Request | JSON deserialization fails, caught and returned as error |
| Unauthorized access | 403 Forbidden | Caller's character UUID doesn't match route UUID |
| Storage failure | 500 Internal Server Error | Unhandled exception from IStorageBackend |

**Note (Req 7.7):** All server validation responses above are enforced independently of client-side validation. The server never trusts that the client has validated input — it always re-validates. This applies regardless of whether the request comes from the Web UI, Desktop app, or a direct API call.

### Web UI

| Scenario | Behavior |
|----------|----------|
| GET /shared-with-me fails | Display `RetryableError` component with retry button |
| GET /sharing fails | Display `RetryableError` component with retry button |
| PUT /sharing fails | Display error notification (toast), retain form state for retry |
| PUT /sharing succeeds | Save is considered successful even if the post-save refresh fails. Invalidate query cache and attempt to refresh rules list; if refresh fails, show a non-blocking warning but do NOT revert or report the save as failed. |
| Empty shared data | Display `EmptyState` component |

### Desktop App

| Scenario | Behavior |
|----------|----------|
| GET /sharing fails (network) | Show MessageBox with error, leave grid empty |
| GET /sharing fails (403) | Show "Not authorized" message |
| PUT /sharing fails | Show MessageBox with error, retain unsaved grid state |
| PUT /sharing succeeds | Reload rules from server response |
| No server connection | Show "Server not connected" status, disable Save button |
| Empty Target UUID on save | Save button is DISABLED (greyed out, not clickable) — not merely programmatically blocked. The control's Enabled property is set to false when any non-Public rule has an empty Target UUID. |

## Testing Strategy

### Property-Based Tests (FsCheck 2.16.6 + NUnit)

Property-based testing is appropriate for this feature because the server-side filtering, pagination, and validation logic are pure functions with clear input/output behavior and large input spaces.

**Library**: FsCheck 2.16.6 (already in the test project)
**Configuration**: Minimum 100 iterations per property test
**Tag format**: `Feature: sharing-visibility-system, Property {N}: {description}`

| Property | Test Location | What It Tests |
|----------|---------------|---------------|
| Property 1: Public Data Filtering | `OE2EmpireTracker.Tests/Server/PublicDataFilteringTests.cs` | Given random characters/rules/entities, verify public endpoint returns correct subset |
| Property 2: Pagination Slice | `OE2EmpireTracker.Tests/Server/PaginationTests.cs` | Given random item counts and page params, verify correct slice |
| Property 3: Invalid Rule Rejection | `OE2EmpireTracker.Tests/Server/SharingValidationTests.cs` | Given rules with empty UUID or bad TargetType, verify 400 response |
| Property 4: Rule Normalization | `OE2EmpireTracker.Tests/Server/SharingNormalizationTests.cs` | Given rules with arbitrary owner/id values, verify server overwrites correctly |
| Property 5: UUID Validation | `OE2EmpireTracker.Tests/Forms/SharingValidationTests.cs` | Given whitespace strings, verify save is blocked |


### Unit Tests (Example-Based)

| Test | What It Verifies |
|------|------------------|
| SharedDataView renders grouped entities | Given mock response with 2 sharers, verify UI groups correctly |
| SharedDataView shows empty state | Given empty response, verify empty state message |
| SharedDataView shows error state | Given error response, verify RetryableError renders |
| SharingConfig adds Public target type | Verify TARGET_TYPES includes 'Public' |
| FormSharing loads rules on open | Given mock API response, verify grid is populated |
| FormSharing blocks save with empty UUID | Given row with empty UUID, verify save is prevented |
| FormSharing reloads on player change | Fire CurrentPlayerChanged, verify reload |

### Integration Tests

| Test | What It Verifies |
|------|------------------|
| Cross-app rule visibility | Create rule via PUT, verify GET returns it |
| Public endpoint returns data | Create Public rule + entity, verify public endpoint includes it |
| SharedWithMe endpoint returns grouped data | Create Character sharing rule, verify /shared-with-me returns data with owner info |

### Test Balance

- **Property tests** cover the core logic (filtering, pagination, validation, normalization) — these are the high-value tests where input variation reveals edge cases.
- **Unit tests** cover specific UI states and integration wiring — these verify concrete scenarios.
- **Integration tests** verify end-to-end behavior across the API boundary.

Avoid writing excessive unit tests for logic already covered by property tests. For example, don't write separate unit tests for "public endpoint returns blueprints when rule has DataType=null" and "public endpoint returns blueprints when rule has DataType=Blueprints" — the property test covers both via random generation.
