# Implementation Plan: Remove Unvalidated Endpoints

## Overview

This plan removes all unvalidated data paths from the server, replaces them with a validated bulk import endpoint, migrates clients to typed endpoints, and hardens authorization. Work is decomposed into small vertical slices: each task modifies ≤5 files, writes ≤200 LOC, and has a concrete verification.

## Tasks

- [x] 1. Remove raw storage methods from IStorageBackend interface
  - [x] 1.1 Remove raw method signatures from IStorageBackend
    - Remove `GetCharacterDataAsync`, `GetCharacterEntityAsync`, `UpsertCharacterDataAsync`, `UpsertCharacterEntityAsync`, `DeleteCharacterEntityAsync`, `PutAllCharacterDataAsync`, `GetAllCharacterDataAsync` from the interface
    - _Satisfies: Req 5, Criteria 1–7_
    - _Inputs: IStorageBackend.cs_
    - _Output: IStorageBackend.cs_
    - _Verification: getDiagnostics shows compile errors only in implementations (expected)_

  - [x] 1.2 Remove raw method implementations from JsonFileStorageBackend
    - Remove implementations of the 7 deleted interface methods
    - Remove split/reassemble helper logic referenced only by those methods
    - _Satisfies: Req 5 Criterion 8; Req 8, Criteria 1–2_
    - _Inputs: JsonFileStorageBackend.cs, IStorageBackend.cs_
    - _Output: JsonFileStorageBackend.cs_
    - _Verification: getDiagnostics on JsonFileStorageBackend.cs shows zero errors_


  - [x] 1.3 Remove any other IStorageBackend implementations' raw methods
    - Check for additional implementations (e.g., test mocks, in-memory backends) and remove raw methods
    - _Satisfies: Req 5 Criterion 8_
    - _Inputs: grep for `: IStorageBackend`_
    - _Output: Any additional implementation files_
    - _Verification: Full solution build succeeds with zero errors_

- [x] 2. Remove raw CRUD routes from DataEndpoints.cs
  - [x] 2.1 Delete raw character data route registrations and handlers
    - Remove the `charData` MapGroup and all its route handlers (PutCharacterDataCollection, GetCharacterDataCollection, CreateCharacterEntity, GetCharacterEntity, UpdateCharacterEntity, DeleteCharacterEntity, PutAllCharacterData, GetAllCharacterData)
    - Retain: global routes, sync route, export route
    - _Satisfies: Req 1, Criteria 1–7, 10; Req 12, Criteria 1–2, 6_
    - _Inputs: DataEndpoints.cs_
    - _Output: DataEndpoints.cs_
    - _Verification: getDiagnostics on DataEndpoints.cs shows zero errors_

  - [x] 2.2 Reimplement export endpoint to assemble from typed storage
    - Modify the export handler to call each of the 19 typed GetAll methods and combine into PlayerRoot JSON
    - _Satisfies: Req 10, Criteria 1–3; Req 12 Criterion 5_
    - _Inputs: DataEndpoints.cs, IStorageBackend.cs (typed method signatures)_
    - _Output: DataEndpoints.cs_
    - _Verification: getDiagnostics clean; export handler references only typed methods_


- [~] 3. Checkpoint — Verify storage and endpoint removal compiles
  - Build full solution, ensure zero errors and zero warnings
  - Ensure all tests pass (existing tests that referenced raw methods may need updating)
  - _Verification: MSBuild zero errors/warnings; vstest all pass_

- [x] 4. Create AuthorizationHelper
  - [x] 4.1 Create AuthorizationHelper.cs with CanAccessCharacterData method
    - Implement access evaluation: owner check → grantee check → faction grantee check → sharing rule check → Owner role check
    - _Satisfies: Req 14 Criterion 7_
    - _Inputs: Existing TypedEndpointBase auth logic (reference pattern), IStorageBackend.cs_
    - _Output: OE2EmpireTracker.Server/Endpoints/AuthorizationHelper.cs_
    - _Verification: getDiagnostics clean on new file_

  - [x] 4.2 Add IsFactionMember and helper methods to AuthorizationHelper
    - Implement `IsFactionMember(characterUUID, factionUUID, storage)`
    - Implement `IsOwner(HttpContext)` and `GetCallerCharacterUUID(HttpContext)`
    - _Satisfies: Req 15 Criterion 8_
    - _Inputs: AuthorizationHelper.cs, IStorageBackend.cs_
    - _Output: AuthorizationHelper.cs_
    - _Verification: getDiagnostics clean_


  - [x] 4.3 Write unit tests for AuthorizationHelper
    - Test each access evaluation path: owner, grantee, faction grantee, sharing rule, Owner role
    - Test denial when no relationship exists
    - _Satisfies: Req 14 Criterion 7; Req 15 Criterion 8_
    - _Verification: AuthorizationHelperTests pass_

- [x] 5. Create Bulk Import Endpoint
  - [x] 5.1 Create BulkImportEndpoints.cs with route registration and auth check
    - Register `PUT /api/v1/characters/{uuid}/import` with RequireAuthorization
    - Implement authorization check BEFORE reading request body
    - Return 403 if token doesn't own character and isn't Owner
    - _Satisfies: Req 2 Criterion 5; Req 6, Criteria 1–3_
    - _Inputs: AuthorizationHelper.cs, existing endpoint patterns_
    - _Output: BulkImportEndpoints.cs_
    - _Verification: getDiagnostics clean_

  - [x] 5.2 Implement bulk import deserialization and validation logic
    - Deserialize PlayerRoot with case-sensitive JSON options
    - Iterate each collection, validate each entity using typed endpoint validation
    - Collect errors (up to 100), set truncated flag
    - _Satisfies: Req 2, Criteria 1–2, 6–8; Req 7, Criteria 1–2; Req 9, Criteria 1–5_
    - _Inputs: BulkImportEndpoints.cs, PlayerRoot.cs, typed validators_
    - _Output: BulkImportEndpoints.cs_
    - _Verification: getDiagnostics clean_


  - [x] 5.3 Implement bulk import persistence and response models
    - On zero errors: persist all entities via typed storage methods
    - Return BulkImportResult (imported counts + total) on success
    - Return BulkImportErrorResponse on failure
    - Create BulkImportResult and BulkImportErrorResponse model classes
    - _Satisfies: Req 2, Criteria 3–4, 9; Req 6 Criterion 4_
    - _Inputs: BulkImportEndpoints.cs, IStorageBackend.cs_
    - _Output: BulkImportEndpoints.cs, BulkImportResult.cs, BulkImportErrorResponse.cs_
    - _Verification: getDiagnostics clean_

  - [x] 5.4 Wire BulkImportEndpoints into Program.cs
    - Call `app.MapBulkImportEndpoints()` in the endpoint registration section
    - Apply rate limiting (5 TPS) consistent with other endpoints
    - _Satisfies: Req 2 Criterion 10_
    - _Inputs: Program.cs, BulkImportEndpoints.cs_
    - _Output: Program.cs_
    - _Verification: getDiagnostics clean; full solution builds_

  - [x] 5.5 Write unit tests for bulk import — valid request path
    - Test: valid PlayerRoot → HTTP 200 with correct imported counts
    - Test: empty collections → HTTP 200 with zero counts
    - Test: unrecognized collection key → ignored, no error
    - _Satisfies: Req 2, Criteria 4, 6, 9_
    - _Verification: BulkImportEndpointTests pass_


  - [x] 5.6 Write unit tests for bulk import — error paths
    - Test: missing required field → HTTP 400 with error list
    - Test: wrong property casing → HTTP 400
    - Test: wrong character UUID in token → HTTP 403
    - Test: auth checked before body read
    - Test: entity with mismatched character UUID → validation error
    - _Satisfies: Req 2, Criteria 2–3, 7; Req 6, Criteria 1–2, 4; Req 7 Criterion 2_
    - _Verification: BulkImportEndpointTests pass_

  - [x] 5.7 Write unit tests for bulk import — error collection and truncation
    - Test: multiple errors collected (not fail-fast)
    - Test: exactly 100 errors → truncated=false
    - Test: 101+ errors → truncated=true, only 100 returned
    - Test: malformed entity JSON → error at index
    - _Satisfies: Req 9, Criteria 2–5_
    - _Verification: BulkImportEndpointTests pass_

  - [x] 5.8 Write property test for all-or-nothing import semantics
    - **Property 3: All-or-Nothing Import Semantics**
    - Generate random PlayerRoot with mix of valid/invalid entities; verify either all persisted or none persisted
    - **Validates: Req 2, Criteria 3–4**
    - _Verification: Property3_AllOrNothingImport passes_

  - [x] 5.9 Write property test for error collection completeness
    - **Property 4: Error Response Completeness**
    - Generate N entities with errors (N ≤ 100); verify response has exactly N entries and truncated=false. For N > 100, verify 100 entries and truncated=true
    - **Validates: Req 9, Criteria 1, 2, 4**
    - _Verification: Property4_ErrorResponseCompleteness passes_


  - [x] 5.10 Write property test for case sensitivity enforcement
    - **Property 7: Case Sensitivity Enforcement**
    - Generate entity property names with random casing mutations; verify bulk import rejects when required field casing is wrong
    - **Validates: Req 7, Criteria 1–2**
    - _Verification: Property7_CaseSensitivityEnforcement passes_

- [~] 6. Checkpoint — Verify bulk import and auth helper compile and pass tests
  - Build full solution, ensure zero errors and zero warnings
  - Run all tests
  - _Verification: MSBuild zero errors/warnings; vstest all pass_

- [x] 7. Harden Character Endpoints authorization
  - [x] 7.1 Add access control to GET /characters list endpoint
    - Filter returned characters to only those the caller has access to (own + granted + shared)
    - Owner tokens see all
    - _Satisfies: Req 14 Criterion 1_
    - _Inputs: CharacterEndpoints.cs, AuthorizationHelper.cs_
    - _Output: CharacterEndpoints.cs_
    - _Verification: getDiagnostics clean_

  - [x] 7.2 Add access control to GET /characters/{uuid} endpoint
    - Call CanAccessCharacterData; return 403 if denied
    - _Satisfies: Req 14, Criteria 2, 6_
    - _Inputs: CharacterEndpoints.cs, AuthorizationHelper.cs_
    - _Output: CharacterEndpoints.cs_
    - _Verification: getDiagnostics clean_


  - [x] 7.3 Add access control to character sub-resource endpoints (capabilities, clearance-levels, groups)
    - GET /characters/{uuid}/capabilities → owner or Owner only
    - GET /characters/{uuid}/clearance-levels → owner or Owner only
    - GET /characters/{uuid}/groups → owner or Owner only
    - _Satisfies: Req 14, Criteria 3–5_
    - _Inputs: CharacterEndpoints.cs (or sub-resource endpoint files), AuthorizationHelper.cs_
    - _Output: Character sub-resource endpoint file(s)_
    - _Verification: getDiagnostics clean_

  - [x] 7.4 Write unit tests for character endpoint authorization
    - Test: GET /characters returns only accessible characters
    - Test: GET /characters/{uuid} enforces access control (403 on denied)
    - Test: capabilities/clearance-levels/groups require owner
    - _Satisfies: Req 14, Criteria 1–6_
    - _Verification: CharacterEndpointAuthTests pass_

- [x] 8. Harden Faction Endpoints authorization
  - [x] 8.1 Add membership check to faction sub-resource endpoints
    - GET /factions/{uuid}/capabilities → require membership or Owner
    - GET /factions/{uuid}/clearance-levels → require membership or Owner
    - GET /factions/{uuid}/groups → require membership or Owner
    - GET /factions/{uuid}/members → require membership or Owner
    - _Satisfies: Req 15, Criteria 3–6_
    - _Inputs: FactionEndpoints.cs, AuthorizationHelper.cs_
    - _Output: FactionEndpoints.cs_
    - _Verification: getDiagnostics clean_


  - [x] 8.2 Verify faction list and basic info remain public
    - Confirm GET /factions and GET /factions/{uuid} remain accessible to all authenticated users
    - Confirm GET /factions/{uuid}/leaders remains public
    - _Satisfies: Req 15, Criteria 1–2, 7_
    - _Inputs: FactionEndpoints.cs_
    - _Output: FactionEndpoints.cs (no-op if already correct, add test assertions)_
    - _Verification: getDiagnostics clean_

  - [x] 8.3 Write unit tests for faction endpoint authorization
    - Test: capabilities/clearance-levels/groups/members require membership (403 for non-members)
    - Test: leaders endpoint is public
    - Test: faction list and basic info are public
    - _Satisfies: Req 15, Criteria 1–9_
    - _Verification: FactionEndpointAuthTests pass_

- [ ] 9. Harden Sync Endpoint authorization
  - [x] 9.1 Filter sync response to authorized factions and characters
    - Modify sync handler to return only factions caller is a member of
    - Return only characters caller has access to (via CanAccessCharacterData)
    - Owner sees all (unchanged)
    - _Satisfies: Req 16, Criteria 1–3, 5_
    - _Inputs: DataEndpoints.cs (sync handler), AuthorizationHelper.cs_
    - _Output: DataEndpoints.cs_
    - _Verification: getDiagnostics clean_

  - [x] 9.2 Preserve sync response format
    - Ensure response structure unchanged (factions, characters, serverTimestamp keys)
    - _Satisfies: Req 16 Criterion 4_
    - _Inputs: DataEndpoints.cs_
    - _Output: DataEndpoints.cs_
    - _Verification: getDiagnostics clean_


  - [x] 9.3 Write unit tests for sync endpoint authorization
    - Test: non-Owner caller gets only their factions and accessible characters
    - Test: Owner caller gets all factions and characters
    - Test: response format unchanged
    - _Satisfies: Req 16, Criteria 1–5_
    - _Verification: SyncEndpointAuthTests pass_

  - [x] 9.4 Write property test for sync response filtering
    - **Property 6: Sync Response Filtering**
    - Generate random permission configurations; verify sync response never contains unauthorized factions/characters
    - **Validates: Req 16, Criteria 1–2, 5**
    - _Verification: Property6_SyncResponseFiltering passes_

- [x] 10. Harden Intel Endpoints authorization
  - [x] 10.1 Add authorization to POST /intel (CreateComment)
    - Enforce caller's Character_UUID matches URL {uuid}
    - _Satisfies: Req 17 Criterion 1_
    - _Inputs: IntelEndpoints.cs, AuthorizationHelper.cs_
    - _Output: IntelEndpoints.cs_
    - _Verification: getDiagnostics clean_

  - [x] 10.2 Add authorization to GET /intel (GetComments)
    - Enforce CanAccessCharacterData on the URL character
    - _Satisfies: Req 17 Criterion 2_
    - _Inputs: IntelEndpoints.cs, AuthorizationHelper.cs_
    - _Output: IntelEndpoints.cs_
    - _Verification: getDiagnostics clean_


  - [x] 10.3 Add authorization to DELETE /intel and share endpoints
    - DELETE /intel/{commentId} → enforce comment ownership or Owner
    - POST /intel/{commentId}/share → enforce comment ownership or Owner
    - DELETE /intel/{commentId}/share/{factionUUID} → enforce comment ownership or Owner
    - _Satisfies: Req 17, Criteria 3–5_
    - _Inputs: IntelEndpoints.cs, AuthorizationHelper.cs_
    - _Output: IntelEndpoints.cs_
    - _Verification: getDiagnostics clean_

  - [x] 10.4 Add authorization to PUT /factions/{uuid}/intel/{shareId}/classify
    - Enforce caller is faction leader of the specified faction or Owner
    - _Satisfies: Req 17 Criterion 6_
    - _Inputs: IntelEndpoints.cs (or FactionIntelEndpoints.cs), AuthorizationHelper.cs_
    - _Output: Intel/faction intel endpoint file_
    - _Verification: getDiagnostics clean_

  - [x] 10.5 Write unit tests for intel endpoint authorization
    - Test: POST /intel rejects mismatched character UUID
    - Test: GET /intel enforces CanAccessCharacterData
    - Test: DELETE /intel enforces ownership
    - Test: share endpoints enforce ownership
    - Test: classify requires faction leader
    - _Satisfies: Req 17, Criteria 1–6_
    - _Verification: IntelEndpointAuthTests pass_


  - [x] 10.6 Write property test for authorization isolation
    - **Property 5: Authorization Isolation (Hardened Endpoints)**
    - Generate random caller/target pairs with no permission relationship; verify all hardened endpoints return 403 with no entity data
    - **Validates: Req 14, 15, 16, 17**
    - _Verification: Property5_AuthorizationIsolation passes_

- [~] 11. Checkpoint — Verify all authorization hardening compiles and passes tests
  - Build full solution, ensure zero errors and zero warnings
  - Run all tests
  - _Verification: MSBuild zero errors/warnings; vstest all pass_

- [x] 12. Migrate Web UI to typed endpoint modules — entity types A–G
  - [x] 12.1 Create typed API modules for colonies, blueprints, surveys, profiles
    - Create src/api/endpoints/colonies.ts, blueprints.ts, surveys.ts, profiles.ts
    - Each module provides getAll, get, create, update, delete functions calling typed endpoints
    - _Satisfies: Req 3, Criteria 1–5 (partial); Req 3 Criterion 6 (partial)_
    - _Inputs: Existing data.ts, typed endpoint URL patterns_
    - _Output: 4 new TypeScript files_
    - _Verification: TypeScript compiles (tsc --noEmit)_

  - [x] 12.2 Create typed API modules for delivery-routes, delivery-plans, ships, ship-templates
    - Create src/api/endpoints/delivery-routes.ts, delivery-plans.ts, ships.ts, ship-templates.ts
    - _Satisfies: Req 3 Criterion 6 (partial)_
    - _Inputs: Existing data.ts, typed endpoint URL patterns_
    - _Output: 4 new TypeScript files_
    - _Verification: TypeScript compiles (tsc --noEmit)_


  - [x] 12.3 Create typed API modules for market-listings, market-transactions, pricing-plans, stock-plans, stock-profiles
    - Create src/api/endpoints/ files for each
    - _Satisfies: Req 3 Criterion 6 (partial)_
    - _Inputs: Existing data.ts, typed endpoint URL patterns_
    - _Output: 5 new TypeScript files_
    - _Verification: TypeScript compiles (tsc --noEmit)_

  - [x] 12.4 Create typed API modules for build-plans, supply-chains, asteroids, stations, faction-contacts, external-characters
    - Create src/api/endpoints/ files for each
    - _Satisfies: Req 3 Criterion 6 (complete)_
    - _Inputs: Existing data.ts, typed endpoint URL patterns_
    - _Output: 6 new TypeScript files_
    - _Verification: TypeScript compiles (tsc --noEmit)_

- [ ] 13. Migrate Web UI consumers from data.ts to typed modules
  - [x] 13.1 Update page components to import from typed API modules (batch 1)
    - Replace getData/putData/getEntity/putEntity/deleteEntity calls in first set of page components
    - _Satisfies: Req 3, Criteria 1–5 (partial)_
    - _Inputs: Page component files, new typed API modules_
    - _Output: ≤5 page component files_
    - _Verification: TypeScript compiles; no imports from data.ts in modified files_


  - [x] 13.2 Update page components to import from typed API modules (batch 2)
    - Replace remaining getData/putData/getEntity/putEntity/deleteEntity calls
    - _Satisfies: Req 3, Criteria 1–5 (complete)_
    - _Inputs: Remaining page component files, new typed API modules_
    - _Output: ≤5 page component files_
    - _Verification: TypeScript compiles; no imports from data.ts in modified files_

  - [x] 13.3 Add error display handling for typed endpoint responses
    - On HTTP 400: parse error and display to user
    - On HTTP 2xx: clear previous error messages
    - On unparseable 400: display generic error
    - _Satisfies: Req 3, Criteria 8–10_
    - _Inputs: API client/error handling module_
    - _Output: Error handling utility + integration into API client_
    - _Verification: TypeScript compiles_

  - [x] 13.4 Remove data.ts module
    - Delete data.ts (or remove all raw data functions)
    - Verify no remaining imports reference it
    - _Satisfies: Req 3 Criterion 7_
    - _Inputs: grep for data.ts imports_
    - _Output: data.ts deleted_
    - _Verification: TypeScript compiles; grep confirms zero imports from data.ts_


- [~] 14. Checkpoint — Verify Web UI migration compiles
  - TypeScript compiles with zero errors
  - No imports from data.ts remain
  - _Verification: tsc --noEmit succeeds; grep for data.ts returns zero results_

- [ ] 15. Migrate Desktop SyncManager to bulk import
  - [-] 15.1 Add BulkImportAsync method to RemoteFactionClient
    - Implement `BulkImportAsync(characterUUID, playerRootJson)` calling PUT /api/v1/characters/{uuid}/import
    - Use PascalCase serialization matching Common model annotations
    - _Satisfies: Req 4, Criteria 2–3, 5_
    - _Inputs: RemoteFactionClient.cs_
    - _Output: RemoteFactionClient.cs_
    - _Verification: getDiagnostics clean_

  - [~] 15.2 Remove old upload methods from RemoteFactionClient
    - Remove `UploadCharacterDataAsync(characterUUID, dataType, json)`
    - Remove `UploadAllCharacterDataAsync(characterUUID, json)`
    - _Satisfies: Req 4, Criteria 2–3_
    - _Inputs: RemoteFactionClient.cs_
    - _Output: RemoteFactionClient.cs_
    - _Verification: getDiagnostics shows errors only in SyncManager (expected, fixed next)_

  - [~] 15.3 Update SyncManager to use BulkImportAsync
    - Change WriteToServerAsync to call BulkImportAsync instead of per-dataType upload
    - On HTTP 400: log validation errors, queue for user review, raise SyncValidationFailed event
    - On HTTP 403: log denial, set connection status to Unauthorized
    - _Satisfies: Req 4, Criteria 1, 4_
    - _Inputs: SyncManager.cs, RemoteFactionClient.cs_
    - _Output: SyncManager.cs_
    - _Verification: getDiagnostics clean_


  - [~] 15.4 Update offline queue flush to use BulkImportAsync
    - Change FlushOfflineQueueAsync to replay via BulkImportAsync
    - _Satisfies: Req 4 Criterion 6_
    - _Inputs: SyncManager.cs (or OfflineQueue.cs)_
    - _Output: SyncManager.cs (or OfflineQueue.cs)_
    - _Verification: getDiagnostics clean; full solution builds_

  - [~] 15.5 Write unit tests for desktop sync migration
    - Test: SyncManager calls PUT /import (not PUT /data)
    - Test: RemoteFactionClient.BulkImportAsync exists and calls correct URL
    - Test: validation failure queues for review
    - Test: offline queue flush uses bulk import
    - _Satisfies: Req 4, Criteria 1–6_
    - _Verification: DesktopSyncMigrationTests pass_

- [~] 16. Checkpoint — Verify desktop sync migration compiles and passes tests
  - Build full solution, ensure zero errors and zero warnings
  - Run all tests
  - _Verification: MSBuild zero errors/warnings; vstest all pass_

- [ ] 17. Write endpoint removal verification tests
  - [~] 17.1 Write tests verifying removed routes return 404
    - Test: PUT /data/{dataType} → 404
    - Test: POST /data/{dataType} → 404
    - Test: GET /data/{dataType}/{entityUuid} → 404
    - Test: PUT /data/{dataType}/{entityUuid} → 404
    - Test: DELETE /data/{dataType}/{entityUuid} → 404
    - Test: PUT /data (bulk) → 404
    - Test: GET /data (read-all) → 404
    - _Satisfies: Req 1, Criteria 1–10_
    - _Verification: EndpointRemovalTests pass_


  - [~] 17.2 Write tests verifying preserved endpoints still work
    - Test: GET /export returns assembled character data
    - Test: GET /sync returns filtered data
    - Test: GET /global/{dataType} works
    - Test: PUT /global/{dataType} works (Owner only)
    - _Satisfies: Req 1 Criterion 11; Req 10 Criterion 1; Req 11, Criteria 1–3_
    - _Verification: PreservedEndpointTests pass_

  - [~] 17.3 Write property test for removed routes returning 404 via framework default
    - **Property 8: Removed Routes Return 404 via Framework Default**
    - Generate random removed route paths; verify 404 comes from framework (no explicit handler)
    - **Validates: Req 1, Criteria 8–9**
    - _Verification: Property8_RemovedRoutesReturn404 passes_

- [ ] 18. Write remaining property tests
  - [~] 18.1 Write property test for no unvalidated write path
    - **Property 1: No Unvalidated Write Path Exists**
    - Static analysis / reflection test: verify no route handler references raw storage methods
    - **Validates: Req 1, 5, 8, 12**
    - _Verification: Property1_NoUnvalidatedWritePath passes_

  - [~] 18.2 Write property test for authorization before body read
    - **Property 2: Authorization Before Body Read**
    - Verify bulk import endpoint checks auth before reading any request body bytes
    - **Validates: Req 6 Criterion 2**
    - _Verification: Property2_AuthBeforeBodyRead passes_


  - [~] 18.3 Write property test for rate limit independence
    - **Property 9: Rate Limit Independence**
    - Verify Token A exceeding rate limit does not affect Token B
    - **Validates: Req 2 Criterion 10**
    - _Verification: Property9_RateLimitIndependence passes_

- [~] 19. Final checkpoint — Full verification
  - Build full solution: zero errors, zero warnings
  - Run all tests: all pass
  - Run audit: `node .kiro/tools/audit.js` reports no new findings
  - Verify no raw storage method references remain in codebase (grep)
  - Verify no data.ts imports remain in web UI (grep)
  - _Verification: MSBuild clean; vstest all pass; audit clean; grep confirms removal_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each major area
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design uses C# (ASP.NET Core net8.0) for server, C# (.NET Framework 4.8.1) for desktop, and TypeScript for web UI — no language selection needed
- Task 1 (storage removal) will cause compile errors in DataEndpoints.cs that are resolved by Task 2
- Task 15.2 (remove old upload methods) will cause compile errors in SyncManager resolved by Task 15.3
- Global endpoints (Req 11) are preserved by not touching them during route removal (Task 2.1)
- Req 7 Criteria 3–4 (typed endpoints remain case-insensitive) requires no code change — verified by existing behavior
- Req 8, Criteria 3–5 are satisfied by the combination of Tasks 1.2 and 2.2 (raw methods removed, export uses typed methods)


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["2.1"] },
    { "id": 3, "tasks": ["2.2", "4.1"] },
    { "id": 4, "tasks": ["4.2"] },
    { "id": 5, "tasks": ["4.3", "5.1"] },
    { "id": 6, "tasks": ["5.2"] },
    { "id": 7, "tasks": ["5.3"] },
    { "id": 8, "tasks": ["5.4"] },
    { "id": 9, "tasks": ["5.5", "5.6", "5.7"] },
    { "id": 10, "tasks": ["5.8", "5.9", "5.10"] },
    { "id": 11, "tasks": ["7.1", "7.2"] },
    { "id": 12, "tasks": ["7.3"] },
    { "id": 13, "tasks": ["7.4", "8.1"] },
    { "id": 14, "tasks": ["8.2"] },
    { "id": 15, "tasks": ["8.3", "9.1"] },
    { "id": 16, "tasks": ["9.2"] },
    { "id": 17, "tasks": ["9.3", "9.4", "10.1", "10.2"] },
    { "id": 18, "tasks": ["10.3", "10.4"] },
    { "id": 19, "tasks": ["10.5", "10.6"] },
    { "id": 20, "tasks": ["12.1", "12.2"] },
    { "id": 21, "tasks": ["12.3", "12.4"] },
    { "id": 22, "tasks": ["13.1"] },
    { "id": 23, "tasks": ["13.2"] },
    { "id": 24, "tasks": ["13.3"] },
    { "id": 25, "tasks": ["13.4"] },
    { "id": 26, "tasks": ["15.1"] },
    { "id": 27, "tasks": ["15.2"] },
    { "id": 28, "tasks": ["15.3"] },
    { "id": 29, "tasks": ["15.4"] },
    { "id": 30, "tasks": ["15.5"] },
    { "id": 31, "tasks": ["17.1", "17.2", "17.3"] },
    { "id": 32, "tasks": ["18.1", "18.2", "18.3"] }
  ]
}
```
