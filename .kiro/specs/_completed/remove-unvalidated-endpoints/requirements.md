# Requirements Document

## Introduction

This spec removes all unvalidated data paths from the OE2EmpireTracker server. The current DataEndpoints.cs exposes raw JSON CRUD endpoints (`/api/v1/characters/{uuid}/data/{dataType}`) that accept arbitrary JSON without schema validation, type checking, or business rule enforcement. These endpoints must be removed entirely. The web UI (which currently calls these raw endpoints via `data.ts`) must be migrated to call the typed CRUD endpoints. The desktop app's sync path (which uploads the entire PlayerRoot as a raw blob) must be replaced with a validated bulk import endpoint. After this work, the typed endpoints become the ONLY way to read/write entity data — if the data format is wrong, the server rejects it.

## Glossary

- **Server**: The OE2EmpireTracker.Server ASP.NET Core application (net8.0)
- **Raw_Endpoints**: The DataEndpoints.cs character data CRUD routes (`/api/v1/characters/{uuid}/data/{dataType}`) that accept arbitrary JSON
- **Typed_Endpoints**: The existing typed CRUD endpoints (ColonyEndpoints, BlueprintEndpoints, etc.) that validate DTOs and enforce business rules
- **Web_UI**: The OE2EmpireTracker.Web browser frontend (TypeScript/Vite)
- **Desktop_App**: The OE2EmpireTracker WinForms application (.NET Framework 4.8.1)
- **SyncManager**: The desktop app component that writes data to the server via RemoteFactionClient
- **RemoteFactionClient**: The desktop app HTTP client that communicates with the server
- **Bulk_Import_Endpoint**: A new validated endpoint that accepts the full PlayerRoot structure and stores each entity via typed storage methods
- **PlayerRoot**: The combined data structure containing all 19 entity collections for a character (colonies, blueprints, surveys, etc.)
- **IStorageBackend**: The server-side storage abstraction interface
- **Raw_Storage_Methods**: The IStorageBackend methods that operate on raw JSON strings (GetCharacterDataAsync, UpsertCharacterDataAsync, etc.)
- **Typed_Storage_Methods**: The IStorageBackend methods that operate on domain model objects (GetAllColoniesAsync, UpsertColonyAsync, etc.)
- **Data_API_Layer**: The web UI TypeScript module (`data.ts`) that currently calls Raw_Endpoints

## Requirements

### Requirement 1: Remove Raw Data Endpoints

**User Story:** As a server operator, I want all unvalidated data endpoints removed, so that no client can bypass validation by calling the raw JSON routes.

#### Acceptance Criteria

1. THE Server SHALL NOT expose the route `GET /api/v1/characters/{uuid}/data/{dataType}` for character entity data retrieval.
2. THE Server SHALL NOT expose the route `PUT /api/v1/characters/{uuid}/data/{dataType}` for character entity data upsert.
3. THE Server SHALL NOT expose the route `POST /api/v1/characters/{uuid}/data/{dataType}` for character entity creation.
4. THE Server SHALL NOT expose the route `GET /api/v1/characters/{uuid}/data/{dataType}/{entityUuid}` for single entity retrieval.
5. THE Server SHALL NOT expose the route `PUT /api/v1/characters/{uuid}/data/{dataType}/{entityUuid}` for single entity update.
6. THE Server SHALL NOT expose the route `DELETE /api/v1/characters/{uuid}/data/{dataType}/{entityUuid}` for single entity deletion.
7. THE Server SHALL NOT expose the route `PUT /api/v1/characters/{uuid}/data` for bulk unvalidated data upload.
8. WHEN any client sends a request to a removed Raw_Endpoint route, THE Server SHALL return HTTP 404.
9. THE removed routes SHALL NOT have any registered handler — the HTTP 404 response SHALL come from the framework's default "no matching route" behavior, not from an explicit 404 handler.
10. THE Server SHALL NOT expose `GET /api/v1/characters/{uuid}/data` — all character data reads SHALL go through the individual typed endpoints.
11. THE Server SHALL continue to expose the global data endpoints (`GET/PUT /api/v1/global/{dataType}`) unchanged, as these serve baseline game data not player entities.


### Requirement 2: Validated Bulk Import Endpoint

**User Story:** As a desktop app developer, I want a validated bulk import endpoint that accepts the full PlayerRoot structure, so that the desktop sync can upload all character data in one request while still enforcing validation on every entity.

#### Acceptance Criteria

1. THE Server SHALL expose `PUT /api/v1/characters/{uuid}/import` that accepts a JSON body matching the PlayerRoot structure (all 19 entity collections).
2. WHEN the Bulk_Import_Endpoint receives a valid request, THE Server SHALL validate each entity in each collection against the same rules used by the individual typed endpoints.
3. WHEN any entity in the bulk import fails validation, THE Server SHALL reject the entire request with HTTP 400 and a response body listing all validation errors with their entity type, entity UUID (if present), and error message.
4. WHEN all entities pass validation, THE Server SHALL persist each entity via the Typed_Storage_Methods (UpsertColonyAsync, UpsertBlueprintAsync, etc.).
5. THE Bulk_Import_Endpoint SHALL require authentication and enforce the same authorization rules as typed endpoints (token Character_UUID must match URL Character_UUID, or token must be Owner).
6. WHEN the request body contains a collection with an unrecognized key name, THE Server SHALL ignore the unrecognized collection (lenient on unknown top-level keys).
7. WHEN the request body contains an entity with property names in the wrong case (e.g., "planetname" instead of "PlanetName"), THE Server SHALL return HTTP 400 with an error identifying the case mismatch.
8. THE Bulk_Import_Endpoint SHALL use case-sensitive deserialization for entity property names to enforce correct data format from clients.
9. THE Server SHALL return HTTP 200 with a summary response on successful import: `{ "imported": { "colonies": N, "blueprints": N, ... }, "total": N }`.
10. THE Bulk_Import_Endpoint SHALL be rate-limited at the same 5 TPS rate as other typed endpoints.


### Requirement 3: Web UI Migration to Typed Endpoints

**User Story:** As a web frontend developer, I want the web UI API layer to call typed endpoints instead of raw data endpoints, so that the frontend benefits from server-side validation and type safety.

#### Acceptance Criteria

1. THE Web_UI SHALL replace all calls to `getData(charUUID, dataType)` with calls to the corresponding typed endpoint (e.g., `GET /api/v1/characters/{uuid}/colonies`).
2. THE Web_UI SHALL replace all calls to `putData(charUUID, dataType, data)` with calls to the corresponding typed endpoint (e.g., `PUT /api/v1/characters/{uuid}/colonies/{entityUuid}`).
3. THE Web_UI SHALL replace all calls to `getEntity(charUUID, dataType, entityUUID)` with calls to the corresponding typed GET endpoint.
4. THE Web_UI SHALL replace all calls to `putEntity(charUUID, dataType, entityUUID, data)` with calls to the corresponding typed PUT endpoint.
5. THE Web_UI SHALL replace all calls to `deleteEntity(charUUID, dataType, entityUUID)` with calls to the corresponding typed DELETE endpoint.
6. THE Web_UI SHALL provide typed API functions for each of the 19 entity types (colonies, blueprints, surveys, profiles, delivery-routes, delivery-plans, ships, ship-templates, market-listings, market-transactions, pricing-plans, stock-plans, stock-profiles, build-plans, supply-chains, asteroids, stations, faction-contacts, external-characters).
7. THE Web_UI SHALL remove the `data.ts` module (or remove all raw data functions from it) after migration is complete.
8. WHEN the server returns HTTP 400 from a typed endpoint, THE Web_UI SHALL display the validation error message to the user.
9. WHEN a typed endpoint call succeeds (HTTP 2xx), THE Web_UI SHALL clear any previously displayed validation error messages for that operation.
10. WHEN the server returns HTTP 400 but the response body is malformed or cannot be parsed, THE Web_UI SHALL display a generic error message indicating the operation failed with a validation error.


### Requirement 4: Desktop Sync Migration

**User Story:** As a desktop app developer, I want the SyncManager to use the validated bulk import endpoint instead of the raw PUT /data endpoint, so that the desktop app cannot upload invalid data to the server.

#### Acceptance Criteria

1. THE Desktop_App SyncManager SHALL use `PUT /api/v1/characters/{uuid}/import` instead of `PUT /api/v1/characters/{uuid}/data/{dataType}` for write-through sync.
2. THE Desktop_App RemoteFactionClient SHALL replace `UploadCharacterDataAsync(characterUUID, dataType, json)` with a method that calls the Bulk_Import_Endpoint.
3. THE Desktop_App RemoteFactionClient SHALL replace `UploadAllCharacterDataAsync(characterUUID, json)` with a method that calls the Bulk_Import_Endpoint.
4. WHEN the Bulk_Import_Endpoint returns HTTP 400 (validation failure), THE SyncManager SHALL log the validation errors and queue the change for user review rather than silently discarding it.
5. THE Desktop_App SHALL serialize the PlayerRoot using the correct property casing (PascalCase matching the Common_Library model annotations) to pass server-side case validation.
6. THE Desktop_App offline queue flush logic SHALL use the Bulk_Import_Endpoint for replaying queued changes.


### Requirement 5: Remove Raw Storage Methods from IStorageBackend

**User Story:** As a server developer, I want the raw JSON storage methods removed from IStorageBackend, so that no future code can accidentally bypass typed storage and reintroduce unvalidated data paths.

#### Acceptance Criteria

1. THE IStorageBackend interface SHALL NOT contain the method `GetCharacterDataAsync(string characterUUID, string dataType)`.
2. THE IStorageBackend interface SHALL NOT contain the method `GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)`.
3. THE IStorageBackend interface SHALL NOT contain the method `UpsertCharacterDataAsync(string characterUUID, string dataType, string json)`.
4. THE IStorageBackend interface SHALL NOT contain the method `UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json)`.
5. THE IStorageBackend interface SHALL NOT contain the method `DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)`.
6. THE IStorageBackend interface SHALL NOT contain the method `PutAllCharacterDataAsync(string characterUUID, string json)`.
7. THE IStorageBackend interface SHALL NOT contain the method `GetAllCharacterDataAsync(string characterUUID)` — the combined read-all view is no longer served by any endpoint.
8. ALL implementations of IStorageBackend (JsonFileBackend, and any others) SHALL remove their implementations of the deleted methods.


### Requirement 6: Ownership Validation on Bulk Import

**User Story:** As a server operator, I want the bulk import endpoint to reject attempts to overwrite another character's data, so that a compromised or malicious client cannot corrupt other players' data.

#### Acceptance Criteria

1. WHEN the authenticated token's Character_UUID does not match the URL Character_UUID and the token is not Owner, THE Bulk_Import_Endpoint SHALL return HTTP 403 with `{ "error": "Access denied" }`.
2. THE Bulk_Import_Endpoint SHALL validate authorization BEFORE reading or parsing the request body, consistent with the server-wide rule that all endpoints perform authorization checks before reading request bodies (see Requirement 6 in context of the general pattern already followed by TypedEndpointBase).
3. THE Server SHALL NOT include any entity data, UUIDs, or counts in the 403 error response.
4. WHEN an entity within the bulk import contains a character UUID field that differs from the URL Character_UUID, THE Server SHALL reject that entity with a validation error identifying the ownership mismatch.


### Requirement 7: Case-Sensitive Data Format Enforcement

**User Story:** As a server operator, I want the server to reject data with incorrect property casing, so that data integrity is maintained and clients are forced to use the correct format.

#### Acceptance Criteria

1. THE Bulk_Import_Endpoint SHALL deserialize entity JSON using case-sensitive property matching.
2. WHEN an entity property name does not match the expected casing (e.g., "planetName" instead of "PlanetName"), THE Server SHALL treat the property as missing and apply required-field validation accordingly. THE Server SHALL reject the entire entity — not silently ignore the mismatched property.
3. THE Typed_Endpoints SHALL continue to use case-insensitive deserialization for backward compatibility with existing web UI clients during the migration period.
4. WHEN the Web_UI migration is complete (Requirement 3 fully satisfied), THE Typed_Endpoints MAY switch to case-sensitive deserialization in a future change.


### Requirement 8: Remove JsonFileStorageBackend Player-Data Splitting Hack

**User Story:** As a server developer, I want the player-data splitting hack removed from JsonFileStorageBackend, so that the storage layer uses only typed methods and does not maintain parallel raw JSON file structures.

#### Acceptance Criteria

1. THE JsonFileStorageBackend SHALL NOT contain logic that splits a raw JSON blob into per-entity-type files (the "player-data splitting" pattern).
2. THE JsonFileStorageBackend SHALL NOT contain logic that reassembles per-entity-type files into a combined raw JSON blob for the old PutAllCharacterDataAsync method.
3. THE JsonFileStorageBackend SHALL persist all entity data exclusively through the typed storage methods (UpsertColonyAsync, UpsertBlueprintAsync, etc.).
4. THE JsonFileStorageBackend `GetAllCharacterDataAsync` method SHALL be removed along with the other raw methods — the export endpoint will assemble its response by calling each typed GetAll method directly.
5. WHEN a typed storage method fails or is unavailable during persistence, THE Server SHALL fail the operation entirely and return an appropriate error — it SHALL NOT fall back to raw JSON persistence.


### Requirement 9: Validation Error Reporting

**User Story:** As a client developer, I want clear validation error messages from the server, so that I can identify and fix data format issues in my client.

#### Acceptance Criteria

1. WHEN the Bulk_Import_Endpoint rejects a request due to validation errors, THE Server SHALL return a response body with the structure: `{ "errors": [ { "entityType": "Colony", "entityUUID": "...", "field": "PlanetName", "error": "PlanetName is required" } ] }`.
2. THE Server SHALL collect ALL validation errors across all entities before returning the response (not fail-fast on the first error).
3. WHEN an entity fails deserialization entirely (malformed JSON within the collection), THE Server SHALL include an error entry with `"error": "Invalid JSON for entity at index N"`.
4. THE Server SHALL limit the error list to a maximum of 100 entries to prevent response size explosion on massively corrupt data. THE Server SHALL set `"truncated": true` ONLY when the total number of validation errors exceeds 100. WHEN exactly 100 errors exist, `"truncated"` SHALL be `false`.
5. WHEN the error list is truncated, THE Server SHALL include a field `"truncated": true` in the response.


### Requirement 10: Export Endpoint Preservation

**User Story:** As a server operator, I want the export endpoint to continue working after raw endpoints are removed, so that character data can still be exported for backup purposes.

#### Acceptance Criteria

1. THE Server SHALL continue to expose `GET /api/v1/characters/{uuid}/export` that returns the full character data as a combined JSON object.
2. THE export endpoint SHALL assemble its response by calling each of the 19 typed storage methods (GetAllColoniesAsync, GetAllBlueprintsAsync, etc.) and combining the results into a single JSON object.
3. THE export endpoint SHALL enforce the same authorization rules as other character endpoints (token must own the character or be Owner).


### Requirement 11: Sync and Global Endpoints Preservation

**User Story:** As a server operator, I want the sync and global data endpoints to continue working, so that client bootstrapping and baseline data distribution are unaffected.

#### Acceptance Criteria

1. THE Server SHALL continue to expose `GET /api/v1/sync` that returns factions, characters, and server timestamp.
2. THE Server SHALL continue to expose `GET /api/v1/global/{dataType}` for reading baseline game data.
3. THE Server SHALL continue to expose `PUT /api/v1/global/{dataType}` for Owner-only baseline data updates.
4. THE sync and global endpoints SHALL remain unchanged in behavior and response format.


### Requirement 12: DataEndpoints.cs Removal

**User Story:** As a server developer, I want DataEndpoints.cs removed or reduced to only the preserved endpoints, so that the codebase has no dead code for removed routes.

#### Acceptance Criteria

1. THE Server SHALL remove all character data route registrations from DataEndpoints.cs (the entire `charData` MapGroup including `GET /`, `PUT /`, `/{dataType}`, and `/{dataType}/{entityUuid}` routes).
2. THE Server SHALL NOT retain any character data read-all route — all character data reads go through typed endpoints.
3. THE Server SHALL retain the global data routes (`GET/PUT /api/v1/global/{dataType}`).
4. THE Server SHALL retain the sync route (`GET /api/v1/sync`).
5. THE Server SHALL retain the export route (`GET /api/v1/characters/{uuid}/export`), reimplemented to assemble from typed storage.
6. THE Server SHALL remove all private helper methods in DataEndpoints.cs that are no longer referenced after route removal (PutCharacterDataCollection, GetCharacterDataCollection, CreateCharacterEntity, GetCharacterEntity, UpdateCharacterEntity, DeleteCharacterEntity, PutAllCharacterData, GetAllCharacterData).






### Requirement 14: Character Endpoint Authorization Hardening

**User Story:** As a server operator, I want character endpoints to enforce the existing permission system on all operations including reads, so that Player B cannot enumerate or inspect Player A's data unless Player A has explicitly granted access via sharing rules or permission groups.

#### Acceptance Criteria

1. `GET /api/v1/characters` SHALL return only characters the authenticated token has access to: the token's own character, plus characters that have granted the caller access via SharingRules or CharacterGranteePermissions. Owner tokens see all.
2. `GET /api/v1/characters/{uuid}` SHALL enforce access control — the caller must be the character owner, be granted access via the target character's CharacterGranteePermissions, be a member of a faction the target has shared with via SharingRules (TargetType=Faction), or be Owner.
3. `GET /api/v1/characters/{uuid}/capabilities` SHALL enforce that the caller is the character owner or Owner — permission structure is never shared outward.
4. `GET /api/v1/characters/{uuid}/clearance-levels` SHALL enforce that the caller is the character owner or Owner — permission structure is never shared outward.
5. `GET /api/v1/characters/{uuid}/groups` SHALL enforce that the caller is the character owner or Owner — permission structure is never shared outward.
6. WHEN Player B's token attempts to read Player A's character or sub-resources and no sharing rule or grantee permission grants access, THE Server SHALL return HTTP 403 with `{ "error": "Access denied" }` and no entity data.
7. THE Server SHALL evaluate access by checking: (a) Is caller the character owner? (b) Is caller listed in CharacterGranteePermissions for this character? (c) Is caller's faction listed in CharacterGranteePermissions (GranteeType=Faction) for this character? (d) Has the character created a SharingRule targeting the caller's character or faction? (e) Is caller Owner?


### Requirement 15: Faction Endpoint Authorization Hardening

**User Story:** As a server operator, I want faction endpoints to enforce the existing permission system on sensitive reads, so that non-members cannot inspect a faction's internal permission structure unless explicitly granted access.

#### Acceptance Criteria

1. `GET /api/v1/factions` SHALL remain accessible to all authenticated users (faction names are semi-public for discovery/join-request purposes).
2. `GET /api/v1/factions/{uuid}` SHALL remain accessible to all authenticated users (basic faction info is semi-public).
3. `GET /api/v1/factions/{uuid}/capabilities` SHALL require the caller to be a member of the faction (has FactionMemberPermissions record), a faction leader, or Owner. Non-members receive HTTP 403.
4. `GET /api/v1/factions/{uuid}/clearance-levels` SHALL require the caller to be a member of the faction (has FactionMemberPermissions record), a faction leader, or Owner. Non-members receive HTTP 403.
5. `GET /api/v1/factions/{uuid}/groups` SHALL require the caller to be a member of the faction (has FactionMemberPermissions record), a faction leader, or Owner. Non-members receive HTTP 403.
6. `GET /api/v1/factions/{uuid}/members` SHALL require the caller to be a member of the faction (has FactionMemberPermissions record), a faction leader, or Owner. Non-members receive HTTP 403.
7. `GET /api/v1/factions/{uuid}/leaders` SHALL remain accessible to all authenticated users (leader identity is semi-public for contact purposes).
8. THE Server SHALL determine faction membership by checking if a `FactionMemberPermissions` record exists for the caller's Character_UUID in the requested faction.
9. THE Server SHALL also grant access to faction sub-resources if the caller has been granted a capability (via FactionMemberCapability) that includes read access to that resource type.


### Requirement 16: Sync Endpoint Authorization Hardening

**User Story:** As a server operator, I want the sync endpoint to only return data the caller is authorized to see based on the permission system, so that it cannot be used as a full roster dump by any authenticated user.

#### Acceptance Criteria

1. `GET /api/v1/sync` SHALL return only factions the caller is a member of (has FactionMemberPermissions record).
2. `GET /api/v1/sync` SHALL return only characters that the caller has access to: the caller's own character, plus characters that have granted the caller access via SharingRules or CharacterGranteePermissions (same logic as Requirement 14, criterion 7).
3. WHEN the caller is Owner, `GET /api/v1/sync` SHALL return all factions and all characters (unchanged behavior for admin).
4. THE sync response format SHALL remain unchanged (same JSON structure with `factions`, `characters`, `serverTimestamp` keys).
5. THE Server SHALL NOT include characters or factions the caller has no permission-system relationship with in the sync response.


### Requirement 17: Intel Endpoint Authorization Hardening

**User Story:** As a server operator, I want intel endpoints to prevent players from reading or modifying intel comments they don't own or haven't been shared with, so that the intel system cannot be used for harassment or data theft.

#### Acceptance Criteria

1. `POST /api/v1/characters/{uuid}/intel` (CreateComment) SHALL enforce that the caller's Character_UUID matches the URL `{uuid}` — Player B cannot create intel comments attributed to Player A.
2. `GET /api/v1/characters/{uuid}/intel` (GetComments) SHALL enforce `CanAccessCharacterData` — Player B cannot read Player A's intel comments.
3. `DELETE /api/v1/characters/{uuid}/intel/{commentId}` SHALL enforce that the caller owns the comment (is the submitter) or is Owner.
4. `POST /api/v1/characters/{uuid}/intel/{commentId}/share` SHALL enforce that the caller owns the comment or is Owner.
5. `DELETE /api/v1/characters/{uuid}/intel/{commentId}/share/{factionUUID}` SHALL enforce that the caller owns the comment or is Owner.
6. `PUT /api/v1/factions/{uuid}/intel/{shareId}/classify` SHALL enforce that the caller is a faction leader of the specified faction or is Owner.

