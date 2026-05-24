# Implementation Plan: Canonical Data Storage

## Overview

Decomposes the monolithic baseline upload into individual canonical records and exposes public game-world data through unauthenticated endpoints. Implementation follows vertical slices: data models first, then service logic, then storage backends, then endpoints, then web UI, with tests alongside each slice.

## Tasks

- [x] 1. Data models and IStorageBackend interface extensions
  - [x] 1.1 Add ColonySummary and AsteroidSummary models to Storage/Models.cs
    - Add `ColonySummary` class (ColonyName, Size, PlanetName)
    - Add `AsteroidSummary` class (UUID, Name, SystemId)
    - _Requirements: 7.2, 7.3_
  - [x] 1.2 Add star system and colony summary methods to IStorageBackend
    - Add `GetAllStarSystemsAsync()` returning `IReadOnlyList<StarSystem>`
    - Add `UpsertStarSystemsAsync(IReadOnlyList<StarSystem>)` for bulk upsert
    - Add `GetColonySummariesForSystemAsync(int systemId)` returning `IReadOnlyList<ColonySummary>`
    - _Requirements: 8.1, 8.3, 7.1_

- [ ] 2. BaselineDecompositionService — core logic
  - [-] 2.1 Create BaselineDecompositionService with reference data decomposition
    - New file: `Server/Services/BaselineDecompositionService.cs`
    - Parse JSON payload, extract known section keys (GameConstants, ShipClass, BlueprintType, TechLevel, Commodity, RefiningRecipe, ResearchTime)
    - Call `UpsertGlobalDataAsync` for each present section
    - Throw on invalid JSON, throw if no sections processed
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.10, 1.11_
  - [~] 2.2 Add blueprint decomposition to BaselineDecompositionService
    - Deserialize Blueprint array from payload using Newtonsoft
    - Call `UpsertBlueprintAsync("", blueprint)` for each blueprint
    - Throw on any single blueprint upsert failure
    - Skip silently if Blueprint section is absent
    - _Requirements: 1.9, 2.1, 2.2, 2.3_

- [~] 3. Checkpoint — Verify decomposition service compiles
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Storage backend implementations — JsonFile
  - [~] 4.1 Implement star system methods in JsonFileStorageBackend
    - Implement `GetAllStarSystemsAsync` (read from JSON file)
    - Implement `UpsertStarSystemsAsync` (write/overwrite star systems)
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [~] 4.2 Implement GetColonySummariesForSystemAsync in JsonFileStorageBackend
    - Scan all characters' colonies, filter by system, project to ColonySummary
    - Return only ColonyName, Size (structure count), PlanetName
    - _Requirements: 7.1, 7.2, 7.3_

- [ ] 5. Storage backend implementations — Sqlite
  - [~] 5.1 Implement star system methods in SqliteStorageBackend
    - Implement `GetAllStarSystemsAsync` and `UpsertStarSystemsAsync`
    - Use existing SQLite patterns from the file
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [~] 5.2 Implement GetColonySummariesForSystemAsync in SqliteStorageBackend
    - Query colonies by system, project to ColonySummary
    - _Requirements: 7.1, 7.2, 7.3_

- [ ] 6. Storage backend implementations — Postgres and Dynamo
  - [~] 6.1 Implement star system and colony summary methods in PostgresStorageBackend
    - Implement `GetAllStarSystemsAsync`, `UpsertStarSystemsAsync`, `GetColonySummariesForSystemAsync`
    - _Requirements: 8.1, 8.3, 7.1_
  - [~] 6.2 Implement star system and colony summary methods in DynamoStorageBackend
    - Implement `GetAllStarSystemsAsync`, `UpsertStarSystemsAsync`, `GetColonySummariesForSystemAsync`
    - _Requirements: 8.1, 8.3, 7.1_

- [ ] 7. DataEndpoints modification — route baseline to decomposition
  - [~] 7.1 Modify PutGlobalData to invoke BaselineDecompositionService for baseline uploads
    - When `dataType == "baseline"`, call `DecomposeAsync` instead of storing blob
    - Return 200 with `{ decomposed: true }` on success
    - Return 400 on decomposition failure
    - Non-baseline dataTypes continue unchanged
    - _Requirements: 9.1, 9.2, 9.3_
  - [~] 7.2 Register BaselineDecompositionService in Program.cs DI container
    - Add `builder.Services.AddSingleton<BaselineDecompositionService>()`
    - _Requirements: 9.3_

- [ ] 8. PublicDataEndpoints — systems, planets, asteroids
  - [~] 8.1 Add systems endpoint to PublicDataEndpoints
    - `GET /api/v1/public/systems` → returns all star systems as JSON array
    - Return empty array when no systems stored
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [~] 8.2 Add planets and asteroids endpoints to PublicDataEndpoints
    - `GET /api/v1/public/systems/{systemId}/planets` → planets for system
    - `GET /api/v1/public/systems/{systemId}/asteroids` → asteroid summaries for system
    - Return empty arrays when none exist
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ] 9. PublicDataEndpoints — colonies and blueprint enhancement
  - [~] 9.1 Add colony summary endpoint to PublicDataEndpoints
    - `GET /api/v1/public/systems/{systemId}/colonies` → colony summaries (name, size, planet only)
    - Return empty array when no colonies exist
    - Never expose structures, resources, or inventories
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 12.1_
  - [~] 9.2 Enhance public blueprints endpoint to include global blueprints
    - Include blueprints with characterUUID="" in results
    - Combine with existing shared-blueprint logic
    - Maintain pagination with combined total count
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 12.2_

- [~] 10. Checkpoint — Verify all server endpoints compile and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Web UI API layer — new public endpoint functions
  - [~] 11.1 Add systems/planets/asteroids/colonies functions to public.ts
    - Add `getSystems()`, `getSystemPlanets(systemId)`, `getSystemAsteroids(systemId)`, `getSystemColonies(systemId)`
    - Add TypeScript interfaces: `StarSystem`, `AsteroidSummary`, `ColonySummary`
    - _Requirements: 13.1, 13.2_
  - [~] 11.2 Add evolution filter to BlueprintFilters interface in public.ts
    - Add `evolution?: string` to `BlueprintFilters`
    - _Requirements: 10.5, 13.5_

- [ ] 12. Blueprint Browser enhancement — data-driven filters
  - [~] 12.1 Fetch reference data for filter dropdowns in BlueprintBrowser
    - On mount, fetch BlueprintType, TechLevel, ShipClass from `getGlobalData`
    - Populate Type, Tech Level, Ship Class dropdowns from server data
    - Show empty dropdowns without error when data unavailable
    - _Requirements: 10.2, 10.3, 10.4, 10.7, 13.2, 13.6_
  - [~] 12.2 Add Evolution filter and column to BlueprintBrowser
    - Derive evolution values from blueprint data or ResearchTime reference data
    - Add Evolution filter dropdown
    - Display columns: Type, Name, Tech Level, Evolution, Nickname
    - _Requirements: 10.5, 10.6, 13.3, 13.4_

- [~] 13. Checkpoint — Verify web UI compiles (TypeScript check)
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 14. Server unit tests — BaselineDecompositionService
  - [~] 14.1 Write unit tests for BaselineDecompositionService reference data decomposition
    - Valid payload with all sections → all stored
    - Payload missing some sections → only present sections stored
    - Invalid JSON → throws JsonException
    - Empty object (no known sections) → throws InvalidOperationException
    - _Requirements: 1.1–1.8, 1.10, 1.11_
  - [~] 14.2 Write unit tests for BaselineDecompositionService blueprint decomposition
    - Blueprint array present → each blueprint upserted with characterUUID=""
    - Empty Blueprint array → no upserts, no error
    - Duplicate UUID → upsert (overwrite)
    - _Requirements: 1.9, 2.1, 2.3, 2.4_
  - [~] 14.3 Write property test for decomposition round-trip (Property 1)
    - **Property 1: Decomposition Round-Trip**
    - For any valid payload with section S, decompose then read(S) produces equivalent data
    - **Validates: Requirements 1.2–1.8, 11.4**
  - [~] 14.4 Write property test for decomposition idempotency (Property 2)
    - **Property 2: Decomposition Idempotency**
    - Decomposing the same payload twice produces identical stored state
    - **Validates: Requirements 11.1**

- [ ] 15. Server unit tests — Public endpoints and visibility
  - [~] 15.1 Write unit tests for public systems/planets/asteroids/colonies endpoints
    - Systems returns all systems; empty array when none
    - Colony summary returns only name/size/planet; never internals
    - Planets/asteroids return empty array when none exist
    - _Requirements: 5.1, 5.4, 6.3, 6.4, 7.1, 7.2_
  - [~] 15.2 Write unit tests for enhanced public blueprints endpoint
    - Includes global blueprints (characterUUID="")
    - Combines global + shared in single result set
    - Non-public data type returns 404
    - _Requirements: 4.1, 4.3, 12.2, 12.5_
  - [~] 15.3 Write property test for visibility isolation (Property 4)
    - **Property 4: Visibility Isolation**
    - No public endpoint response contains private entity fields
    - **Validates: Requirements 12.1–12.5**

- [ ] 16. Server unit tests — Storage and additivity
  - [~] 16.1 Write unit tests for storage backend star system methods
    - UpsertStarSystemsAsync stores and retrieves correctly
    - Upsert with same Id overwrites (upsert semantics)
    - GetAllStarSystemsAsync returns empty list initially
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [~] 16.2 Write property test for blueprint additivity (Property 3)
    - **Property 3: Blueprint Additivity**
    - Sequential uploads with overlapping blueprints produce the union
    - **Validates: Requirements 11.2**
  - [~] 16.3 Write property test for colony summary projection (Property 5)
    - **Property 5: Colony Summary Projection**
    - Colony summary fields == {ColonyName, Size, PlanetName} only
    - **Validates: Requirements 7.2, 7.3**

- [ ] 17. Web UI tests — BlueprintBrowser
  - [~] 17.1 Write Vitest tests for BlueprintBrowser data-driven filters
    - Renders with empty reference data (no errors)
    - Populates dropdowns from API responses
    - Displays correct columns (Type, Name, Tech Level, Evolution, Nickname)
    - Filters apply correctly to combined global+shared results
    - _Requirements: 10.2, 10.5, 10.6, 10.7, 13.1, 13.6_

- [~] 18. Final checkpoint — Full build and test verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Server is .NET 8 (SDK-style), Web is TypeScript/React with Vitest
- Storage backends (JsonFile, Sqlite, Postgres, Dynamo) all implement the same IStorageBackend interface
- The existing `GetAllBlueprintsAsync("")` already supports empty-string characterUUID for global blueprints

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["2.1"] },
    { "id": 2, "tasks": ["2.2", "4.1", "5.1", "6.1", "6.2"] },
    { "id": 3, "tasks": ["4.2", "5.2", "7.1", "7.2"] },
    { "id": 4, "tasks": ["8.1", "8.2", "11.1", "11.2"] },
    { "id": 5, "tasks": ["9.1", "9.2", "12.1"] },
    { "id": 6, "tasks": ["12.2"] },
    { "id": 7, "tasks": ["14.1", "14.2", "15.1", "15.2", "16.1"] },
    { "id": 8, "tasks": ["14.3", "14.4", "15.3", "16.2", "16.3", "17.1"] }
  ]
}
```
