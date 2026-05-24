# Technical Design: Canonical Data Storage

## Overview

This design decomposes the monolithic `PUT /api/v1/global/baseline` upload into individual canonical records and exposes inherently-public game-world data through unauthenticated endpoints. The architecture adds a `BaselineDecompositionService` that intercepts baseline uploads, a set of new public endpoints for systems/planets/asteroids/colonies, enhancements to the existing public blueprints endpoint, and Web UI changes to consume reference data from the server.

Satisfies: Requirements 1–13

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  PUT /api/v1/global/baseline                                │
│  (DataEndpoints.PutGlobalData, dataType == "baseline")      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  BaselineDecompositionService                               │
│  - ParsePayload(json) → BaselinePayload                    │
│  - DecomposeAsync(payload, storage) → void                 │
│    ├─ UpsertGlobalDataAsync("GameConstants", ...)          │
│    ├─ UpsertGlobalDataAsync("ShipClass", ...)              │
│    ├─ UpsertGlobalDataAsync("BlueprintType", ...)          │
│    ├─ UpsertGlobalDataAsync("TechLevel", ...)              │
│    ├─ UpsertGlobalDataAsync("Commodity", ...)              │
│    ├─ UpsertGlobalDataAsync("RefiningRecipe", ...)         │
│    ├─ UpsertGlobalDataAsync("ResearchTime", ...)           │
│    └─ UpsertBlueprintAsync("", blueprint) × N             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Public Endpoints (AllowAnonymous)                          │
│  GET /api/v1/public/global/{dataType}     (existing)        │
│  GET /api/v1/public/blueprints            (enhanced)        │
│  GET /api/v1/public/systems               (new)             │
│  GET /api/v1/public/systems/{id}/planets  (new)             │
│  GET /api/v1/public/systems/{id}/asteroids(new)             │
│  GET /api/v1/public/systems/{id}/colonies (new)             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  IStorageBackend Extensions                                 │
│  + GetAllStarSystemsAsync() → IReadOnlyList<StarSystem>    │
│  + UpsertStarSystemsAsync(List<StarSystem>) → void         │
│  + GetPlanetsForSystemAsync(int systemId) → List<Planet>   │
│  + GetAsteroidsForSystemAsync(int systemId) → List<obj>    │
│  + GetColonySummariesForSystemAsync(int) → List<Summary>   │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### BaselineDecompositionService

**Location:** `OE2EmpireTracker.Server/Services/BaselineDecompositionService.cs`

**Responsibility:** Parses a BaselineData.json payload and stores each section as individual canonical records.

```csharp
public class BaselineDecompositionService
{
    private readonly ILogger<BaselineDecompositionService> _logger;

    public BaselineDecompositionService(ILogger<BaselineDecompositionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Decomposes a baseline JSON payload into individual canonical records.
    /// Throws on invalid JSON or if no sections are present.
    /// </summary>
    public async Task DecomposeAsync(string json, IStorageBackend storage);
}
```

**Algorithm:**
1. Parse JSON using `System.Text.Json.JsonDocument`
2. If parse fails → throw `JsonException` (caller returns 400)
3. Track whether at least one section was processed
4. For each known section key (GameConstants, ShipClass, BlueprintType, TechLevel, Commodity, RefiningRecipe, ResearchTime):
   - If property exists in root object → serialize that section → `UpsertGlobalDataAsync(key, sectionJson)`
   - Increment processed count
5. For "Blueprint" array:
   - Deserialize as `List<Blueprint>` using Newtonsoft (matches Common model)
   - For each blueprint → `UpsertBlueprintAsync("", blueprint)`
   - If any upsert fails → throw (caller rejects entire request)
6. If no sections were processed → throw `InvalidOperationException`

**Idempotency:** Reference data sections use replace semantics (overwrite entire key). Blueprints use upsert-by-UUID (additive — never deletes).

### DataEndpoints Modification

**Location:** `OE2EmpireTracker.Server/Endpoints/DataEndpoints.cs`

**Change:** In `PutGlobalData`, when `dataType == "baseline"`:
- Instead of storing the blob as-is, invoke `BaselineDecompositionService.DecomposeAsync`
- On success → return 200 with `{ decomposed: true, sections: [...] }`
- On failure → return 400 with error message
- Non-baseline dataTypes continue to use direct `UpsertGlobalDataAsync` (unchanged)

### IStorageBackend Extensions

**Location:** `OE2EmpireTracker.Server/Storage/IStorageBackend.cs`

New methods added to the interface:

```csharp
// Star Systems (global, not per-character)
Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync();
Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems);

// Colony summaries for a system (derived from per-character data)
Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId);
```

### PublicDataEndpoints Enhancement

**Location:** `OE2EmpireTracker.Server/Endpoints/PublicDataEndpoints.cs`

New route registrations:
```csharp
publicGroup.MapGet("/systems", GetPublicSystems);
publicGroup.MapGet("/systems/{systemId}/planets", GetPublicPlanets);
publicGroup.MapGet("/systems/{systemId}/asteroids", GetPublicAsteroids);
publicGroup.MapGet("/systems/{systemId}/colonies", GetPublicColonies);
```

Enhanced blueprint endpoint includes global blueprints (characterUUID="") in addition to shared blueprints.

### Web UI API Layer

**Location:** `OE2EmpireTracker.Web/src/api/endpoints/public.ts`

New functions:
```typescript
getSystems: () => apiClient.get('api/v1/public/systems').json<StarSystem[]>()
getSystemPlanets: (systemId: number) => apiClient.get(`api/v1/public/systems/${systemId}/planets`).json<Planet[]>()
getSystemAsteroids: (systemId: number) => apiClient.get(`api/v1/public/systems/${systemId}/asteroids`).json<AsteroidSummary[]>()
getSystemColonies: (systemId: number) => apiClient.get(`api/v1/public/systems/${systemId}/colonies`).json<ColonySummary[]>()
```

### Blueprint Browser Enhancement

**Location:** `OE2EmpireTracker.Web/src/pages/public/BlueprintBrowser.tsx`

- Add `evolution` to `BlueprintFilters` interface
- Fetch filter options from public global endpoints on mount
- Display columns: Type, Name, Tech Level, Evolution, Nickname
- Handle missing reference data gracefully (empty dropdowns, no errors)
- No hardcoded filter values

## Data Models

### ColonySummary

```csharp
/// <summary>Public colony summary (name + size only).</summary>
public class ColonySummary
{
    public string ColonyName { get; set; } = string.Empty;
    public int Size { get; set; }
    public string PlanetName { get; set; } = string.Empty;
}
```

### AsteroidSummary

```csharp
/// <summary>Public asteroid summary for system view.</summary>
public class AsteroidSummary
{
    public string UUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SystemId { get; set; }
}
```

### StarSystem (existing, in Common)

Already defined in `OE2EmpireTracker.Common/Models/StarSystem.cs` with fields: Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass, FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase.

### Data Visibility Matrix

| Data Type | Public Access | Filtering |
|-----------|:---:|---|
| Reference data (GameConstants, ShipClass, etc.) | ✓ | None — full data |
| Global blueprints (OwnerUUID="") | ✓ | None — always visible |
| Shared player blueprints | ✓ | Only if SharingRule with TargetType=Public |
| Private player blueprints | ✗ | Never exposed |
| Star systems | ✓ | None — inherently public |
| Planets | ✓ | None — inherently public |
| Asteroids (summary) | ✓ | None — inherently public |
| Colony summaries | ✓ | Name + size only |
| Colony internals | ✗ | Never exposed |
| Surveys, profiles, routes, etc. | ✗ | Never exposed |

Non-public data types requested via public endpoints return 404 (no information leakage).

## Error Handling

### Baseline Decomposition Errors

| Condition | Response | Side Effects |
|-----------|----------|--------------|
| Invalid JSON payload | HTTP 400 `{ error: "Invalid JSON body" }` | No data stored |
| No sections present in valid JSON | HTTP 400 `{ error: "No valid sections found" }` | No data stored |
| Blueprint upsert failure | HTTP 500 `{ error: "Decomposition failed" }` | Partial state possible (reference data may be stored) |
| Missing optional section | Success (skip silently) | Only present sections stored |

### Public Endpoint Errors

| Condition | Response |
|-----------|----------|
| Requested dataType not stored | HTTP 404 `{ error: "Global data type not found" }` |
| Non-public data type via public endpoint | HTTP 404 (no information leakage) |
| Storage retrieval error | HTTP 500 |
| No systems/planets/asteroids/colonies | HTTP 200 with empty array `[]` |

### Atomicity Considerations

- Reference data sections are individually atomic (each `UpsertGlobalDataAsync` is atomic)
- Blueprint upserts are individually atomic but the batch is not transactional
- If blueprint upsert N fails after N-1 succeeded, the first N-1 remain stored (acceptable given additive/idempotent semantics — re-upload will retry)
- The baseline endpoint returns an error if any blueprint fails, signaling the client to retry

## Testing Strategy

### Unit Tests (Server)

1. **BaselineDecompositionService tests:**
   - Valid payload with all sections → all stored correctly
   - Payload missing some sections → only present sections stored
   - Invalid JSON → throws JsonException
   - Empty Blueprint array → no blueprint upserts
   - Blueprint with duplicate UUID → upsert (not duplicate)

2. **Public endpoint tests:**
   - Systems endpoint returns all systems
   - Systems endpoint returns empty array when none stored
   - Colony summary endpoint returns only name/size/planet
   - Colony summary never includes structures or resources
   - Blueprint endpoint includes global blueprints
   - Non-public data type returns 404

3. **Storage backend tests:**
   - `GetAllBlueprintsAsync("")` returns global blueprints
   - `UpsertStarSystemsAsync` stores and retrieves correctly
   - `GetColonySummariesForSystemAsync` projects correctly

### Property-Based Tests

1. **Round-trip property:** For any valid BaselineData payload, decompose then read each section produces equivalent data
2. **Idempotency property:** Decomposing the same payload twice produces identical state
3. **Additivity property:** Sequential uploads with overlapping blueprints produce the union
4. **Visibility isolation:** No public endpoint response contains private entity fields

### Web UI Tests (Vitest)

1. **BlueprintBrowser:** Renders with empty reference data (no errors)
2. **BlueprintBrowser:** Populates dropdowns from API responses
3. **BlueprintBrowser:** Displays correct columns
4. **Filter behavior:** Filters apply correctly to combined global+shared results

## Correctness Properties

### Property 1: Decomposition Round-Trip

**Validates: Requirements 1.2-1.8, 11.4**

For any valid BaselineData.json payload P containing section S with value V:
```
decompose(P) ; read(S) == V
```
Each reference data section stored via decomposition is byte-equivalent to the corresponding section in the original payload when re-serialized.

### Property 2: Decomposition Idempotency

**Validates: Requirements 11.1**

For any valid payload P:
```
decompose(P) ; state_1 = snapshot()
decompose(P) ; state_2 = snapshot()
state_1 == state_2
```

### Property 3: Blueprint Additivity

**Validates: Requirements 11.2**

For payloads P1 (with blueprints {A, B}) and P2 (with blueprints {B, C}):
```
decompose(P1) ; decompose(P2) ; readBlueprints("") ⊇ {A, B, C}
```
Blueprints are never deleted by decomposition — only upserted.

### Property 4: Visibility Isolation

**Validates: Requirements 12.1-12.5**

For any response R from any public endpoint and any private entity field F (structures, resources, inventories, surveys, profiles, routes, plans, ships, templates, listings, transactions):
```
F ∉ R
```

### Property 5: Colony Summary Projection

**Validates: Requirements 7.2, 7.3**

For any colony C returned by the colony summary endpoint:
```
fields(C) == {ColonyName, Size, PlanetName}
Size == count(C.Structures)
```

### Property 6: Partial Upload Tolerance

**Validates: Requirements 1.10**

For a payload P missing section S but containing at least one other valid section:
```
stored_value(S) before decompose(P) == stored_value(S) after decompose(P)
decompose(P) returns success
```

### Property 7: Invalid JSON Rejection

**Validates: Requirements 1.11**

For any non-JSON string X:
```
decompose(X) → HTTP 400
∀ key K: stored_value(K) unchanged
```

## File Changes Summary

| File | Change Type | Purpose |
|------|-------------|---------|
| `Server/Services/BaselineDecompositionService.cs` | New | Decomposition logic |
| `Server/Storage/IStorageBackend.cs` | Modified | Add system/colony-summary methods |
| `Server/Storage/JsonFileStorageBackend.cs` | Modified | Implement new methods |
| `Server/Storage/SqliteStorageBackend.cs` | Modified | Implement new methods |
| `Server/Storage/PostgresStorageBackend.cs` | Modified | Implement new methods |
| `Server/Storage/DynamoStorageBackend.cs` | Modified | Implement new methods |
| `Server/Storage/Models.cs` | Modified | Add ColonySummary, AsteroidSummary |
| `Server/Endpoints/DataEndpoints.cs` | Modified | Route baseline to decomposition |
| `Server/Endpoints/PublicDataEndpoints.cs` | Modified | Add systems/planets/asteroids/colonies, enhance blueprints |
| `Server/Program.cs` | Modified | Register BaselineDecompositionService |
| `Web/src/api/endpoints/public.ts` | Modified | Add new endpoint functions |
| `Web/src/pages/public/BlueprintBrowser.tsx` | Modified | Data-driven filters, evolution column |
