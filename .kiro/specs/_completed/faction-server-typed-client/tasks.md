# Implementation Plan: Faction Server Typed Client

## Overview

Replace raw JSON communication in `RemoteFactionClient` with a fully-typed client (`IFactionServerTypedClient` / `FactionServerTypedClient`) in `OE2EmpireTracker.Common/Client/FactionServer/`. Implementation proceeds bottom-up: exceptions → DTOs → interface → implementation → tests → external spec updates.

## Tasks

- [x] 1. Create exception hierarchy
  - [x] 1.1 Create FactionServerException and FactionValidationError
    - Create `OE2EmpireTracker.Common/Client/FactionServer/Exceptions/FactionServerException.cs` with base exception class (two constructors: message, message+inner)
    - Create `OE2EmpireTracker.Common/Client/FactionServer/Exceptions/FactionValidationError.cs` with EntityType, EntityUUID, Field, Error properties
    - _Requirements: 9.1_
    - _Inputs: design.md (Exception Hierarchy section)_
    - _Output: 2 new .cs files in Exceptions/_
    - _Verification: getDiagnostics on both files shows zero errors_

  - [x] 1.2 Create FactionValidationException, FactionAuthorizationException, FactionConnectionException
    - Create `FactionValidationException.cs` extending FactionServerException with `List<FactionValidationError> Errors` property
    - Create `FactionAuthorizationException.cs` extending FactionServerException with message constructor
    - Create `FactionConnectionException.cs` extending FactionServerException with message+inner constructor
    - _Requirements: 9.2, 9.3, 9.4_
    - _Inputs: design.md (Exception Hierarchy section), task 1.1 files_
    - _Output: 3 new .cs files in Exceptions/_
    - _Verification: getDiagnostics on all exception files shows zero errors_

- [x] 2. Create DTOs
  - [x] 2.1 Create BulkImportResult and SyncResponse DTOs
    - Create `OE2EmpireTracker.Common/Client/FactionServer/DTOs/BulkImportResult.cs` with [JsonProperty] attributes on Imported (Dictionary<string,int>) and Total (int)
    - Create `OE2EmpireTracker.Common/Client/FactionServer/DTOs/SyncResponse.cs` with [JsonProperty] on Factions (ServerFaction[]), Characters (ServerCharacter[]), ServerTimestamp (DateTime)
    - _Requirements: 2.1, 2.4, 2.5, 2.6_
    - _Inputs: design.md (Data Models section)_
    - _Output: 2 new .cs files in DTOs/_
    - _Verification: getDiagnostics on both files shows zero errors_

  - [x] 2.2 Move SharingRuleDto to Common
    - Copy `OE2EmpireTracker/Client/SharingRuleDto.cs` to `OE2EmpireTracker.Common/Client/FactionServer/DTOs/SharingRuleDto.cs`
    - Update namespace to `OE2EmpireTracker.Common.Client.FactionServer`
    - Update the original file in OE2EmpireTracker to use a `using` alias or delete and redirect references
    - _Requirements: 2.1, 2.6_
    - _Inputs: OE2EmpireTracker/Client/SharingRuleDto.cs_
    - _Output: New SharingRuleDto.cs in Common, updated references in WinForms project_
    - _Verification: Full solution build with zero errors_


- [x] 3. Create OpenAPI specification
  - [x] 3.1 Create faction-server-swagger.yaml (server admin + sync endpoints)
    - Create `OE2EmpireTracker.Common/Client/faction-server-swagger.yaml`
    - Define OpenAPI 3.0 info block with version 1.0.0, Bearer auth security scheme
    - Define paths: /health (GET), /api/v1/factions (GET), /api/v1/characters (GET, POST), /api/v1/sync/snapshot (GET)
    - Define schemas: ServerFaction, ServerCharacter, SyncResponse, CreateCharacterRequest
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_
    - _Inputs: design.md (Endpoint-to-Method Mapping, Wire Format Examples)_
    - _Output: faction-server-swagger.yaml (partial — admin/sync endpoints)_
    - _Verification: YAML is valid (no syntax errors on parse)_

  - [x] 3.2 Add per-entity-type endpoints to swagger (colonies through stations — 10 entity types)
    - Add paths for: /api/v1/characters/{uuid}/colonies, blueprints, surveys, playerProfiles, deliveryRoutes, deliveryPlans, ships, shipTemplates, marketListings, marketTransactions
    - Reference existing domain model schemas (Colony, Blueprint, Survey, etc.)
    - _Requirements: 1.1, 1.2_
    - _Inputs: design.md (Endpoint-to-Method Mapping), task 3.1 file_
    - _Output: Updated faction-server-swagger.yaml_
    - _Verification: YAML valid, all 10 paths present_

  - [x] 3.3 Add remaining entity endpoints and data mutation paths to swagger
    - Add paths for: pricingPlans, stockPlans, stockProfiles, buildPlans, supplyChains, asteroids, stations, factions, externalCharacters (9 entity types)
    - Add paths for: /api/v1/characters/{uuid}/import (PUT), /api/v1/global/baseline (PUT), /api/v1/characters/{uuid}/export (GET), /api/v1/characters/{uuid}/sharing (GET, PUT)
    - Define schemas: BulkImportResult, BulkImportRequest (PlayerRoot reference), SharingRuleDto, ValidationErrorResponse
    - _Requirements: 1.1, 1.2_
    - _Inputs: design.md (Endpoint-to-Method Mapping), task 3.2 file_
    - _Output: Completed faction-server-swagger.yaml with all 26 endpoints_
    - _Verification: YAML valid, all endpoint paths present, all schemas defined_


- [x] 4. Create typed client interface
  - [x] 4.1 Create IFactionServerTypedClient interface
    - Create `OE2EmpireTracker.Common/Client/FactionServer/IFactionServerTypedClient.cs`
    - Define interface extending IDisposable with IsConnected property
    - Define all 26 async methods: CheckHealthAsync, GetFactionsAsync, GetCharactersAsync, CreateCharacterAsync, GetSyncSnapshotAsync, ExportCharacterDataAsync, UploadBaselineAsync, BulkImportAsync, GetSharingRulesAsync, PutSharingRulesAsync, plus 19 per-entity-type Get methods
    - All methods take CancellationToken with default, return Task<T> or Task
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
    - _Inputs: design.md (IFactionServerTypedClient section)_
    - _Output: IFactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors_

- [x] 5. Implement typed client core infrastructure
  - [x] 5.1 Create FactionServerTypedClient — constructor, fields, Dispose, CreateHttpClient
    - Create `OE2EmpireTracker.Common/Client/FactionServer/FactionServerTypedClient.cs`
    - Implement constructor accepting serverUrl, SecureString bearerToken, optional trustedThumbprint, optional timeout (default 5 min, no min/max bounds)
    - Implement certificate pinning callback (thumbprint-only validation when configured, per design)
    - Implement Dispose (dispose SecureString, HttpClient, SemaphoreSlim)
    - Implement SecureStringToString helper
    - Implement IsConnected property
    - _Requirements: 4.1, 4.2, 4.5, 5.1, 5.2, 5.3, 5.4_
    - _Inputs: design.md (FactionServerTypedClient Implementation, Certificate Pinning, Disposal sections)_
    - _Output: FactionServerTypedClient.cs (partial — infrastructure only)_
    - _Verification: getDiagnostics shows zero errors_

  - [x] 5.2 Implement rate limiter and error handler in FactionServerTypedClient
    - Add AcquireRateLimitTokenAsync method (semaphore-based, 60 req/min default, token released after 60s)
    - Add ApplyRateLimit method (replaces semaphore, no bounds checking)
    - Add EnsureSuccessOrThrowAsync method (HTTP 400 → parse validation errors or fallback, 403 → auth exception, network → connection exception, others → base exception)
    - Add BulkImportErrorResponse private DTO for parsing 400 error bodies
    - _Requirements: 6.1, 6.2, 6.3, 4.4, 4.6_
    - _Inputs: design.md (Rate Limiting, Error Handling sections), task 5.1 file_
    - _Output: Updated FactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors_


  - [x] 5.3 Implement server admin and sync endpoint methods
    - Implement CheckHealthAsync (GET /health → bool)
    - Implement GetFactionsAsync (GET /api/v1/factions → ServerFaction[])
    - Implement GetCharactersAsync (GET /api/v1/characters → ServerCharacter[])
    - Implement CreateCharacterAsync (POST /api/v1/characters with name + optional uuid)
    - Implement GetSyncSnapshotAsync (GET /api/v1/sync/snapshot → SyncResponse)
    - Implement ExportCharacterDataAsync (GET /api/v1/characters/{uuid}/export → PlayerRoot)
    - _Requirements: 8.1, 8.2_
    - _Inputs: design.md (Internal Method Pattern, Endpoint-to-Method Mapping), task 5.2 file_
    - _Output: Updated FactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors_

  - [x] 5.4 Implement data mutation methods (bulk import, baseline, sharing)
    - Implement UploadBaselineAsync (PUT /api/v1/global/baseline with BaselineRoot body)
    - Implement BulkImportAsync (PUT /api/v1/characters/{uuid}/import with PlayerRoot body → BulkImportResult)
    - Implement GetSharingRulesAsync (GET /api/v1/characters/{uuid}/sharing → SharingRuleDto[])
    - Implement PutSharingRulesAsync (PUT /api/v1/characters/{uuid}/sharing with SharingRuleDto[] body)
    - _Requirements: 8.1, 8.2_
    - _Inputs: design.md (Internal Method Pattern for write operations), task 5.3 file_
    - _Output: Updated FactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors_

  - [x] 5.5 Implement per-entity-type GET methods (first 10 entity types)
    - Implement GetColoniesAsync, GetBlueprintsAsync, GetSurveysAsync, GetPlayerProfilesAsync, GetDeliveryRoutesAsync, GetDeliveryPlansAsync, GetShipsAsync, GetShipTemplatesAsync, GetMarketListingsAsync, GetMarketTransactionsAsync
    - All follow the pattern: AcquireRateLimitToken → GET /api/v1/characters/{uuid}/{entityType} → EnsureSuccess → Deserialize<T[]>
    - _Requirements: 8.1, 8.2_
    - _Inputs: design.md (Internal Method Pattern, Endpoint Mapping), task 5.4 file_
    - _Output: Updated FactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors_

  - [x] 5.6 Implement per-entity-type GET methods (remaining 9 entity types)
    - Implement GetPricingPlansAsync, GetStockPlansAsync, GetStockProfilesAsync, GetBuildPlansAsync, GetSupplyChainsAsync, GetAsteroidsAsync, GetStationsAsync, GetFactionContactsAsync, GetExternalCharactersAsync
    - All follow the same pattern as task 5.5
    - _Requirements: 8.1, 8.2_
    - _Inputs: design.md (Internal Method Pattern, Endpoint Mapping), task 5.5 file_
    - _Output: Updated FactionServerTypedClient.cs_
    - _Verification: getDiagnostics shows zero errors; full solution builds cleanly_

- [x] 6. Checkpoint - Ensure solution builds
  - Ensure the full solution (`OE2EmpireTracker.sln`) builds with zero errors and zero warnings. Ask the user if questions arise.


- [x] 7. Unit tests for typed client
  - [x] 7.1 Create FactionServerTypedClientTests — constructor and disposal tests
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/FactionServerTypedClientTests.cs`
    - Test: constructor stores parameters correctly (serverUrl trimmed, timeout defaults to 5 min)
    - Test: constructor accepts null thumbprint (no pinning)
    - Test: Dispose zeroes SecureString and does not throw on double-dispose
    - Test: timeout accepts any developer-specified value (1ms, 1 hour — no bounds)
    - _Requirements: 4.2, 4.5, 5.4_
    - _Inputs: FactionServerTypedClient.cs_
    - _Output: FactionServerTypedClientTests.cs_
    - _Verification: Tests pass via vstest.console_

  - [x] 7.2 Create FactionServerTypedClientTests — certificate pinning and auth tests
    - Test: certificate pinning rejects mismatched thumbprint
    - Test: certificate pinning accepts matching thumbprint (case-insensitive)
    - Test: thumbprint mismatch rejects without performing standard CA validation
    - Test: bearer token attached as Authorization header on requests
    - Test: no pinning callback when thumbprint is null/empty
    - _Requirements: 5.1, 5.2, 5.3_
    - _Inputs: FactionServerTypedClient.cs_
    - _Output: Updated FactionServerTypedClientTests.cs_
    - _Verification: Tests pass via vstest.console_

  - [x] 7.3 Create FactionServerTypedClientTests — error handling and interface coverage tests
    - Test: HTTP 400 with valid error body throws FactionValidationException with parsed errors
    - Test: HTTP 400 with unparseable body throws FactionServerException (fallback)
    - Test: HTTP 403 throws FactionAuthorizationException
    - Test: network failure throws FactionConnectionException
    - Test: IFactionServerTypedClient interface has exactly 26 methods + IsConnected property (reflection test)
    - _Requirements: 4.4, 4.6, 8.1, 8.4_
    - _Inputs: FactionServerTypedClient.cs, IFactionServerTypedClient.cs_
    - _Output: Updated FactionServerTypedClientTests.cs_
    - _Verification: Tests pass via vstest.console_

- [x] 8. Checkpoint - Ensure all unit tests pass
  - Ensure all tests pass via vstest.console, ask the user if questions arise.


- [x] 9. Property-based tests — DTO serialization
  - [x] 9.1 Write property test for DTO round-trip (Property 1)
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/DtoRoundTripPropertyTests.cs`
    - **Property 1: DTO Serialization Round-Trip**
    - **Validates: Requirements 4.3, 7.1**
    - Use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`
    - Generate random BulkImportResult, SyncResponse, SharingRuleDto instances
    - Assert: serialize → deserialize produces equivalent object
    - Custom Arbitrary generators using LINQ query syntax for each DTO type

  - [x] 9.2 Write property test for camelCase wire format (Property 3)
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/SerializationFormatPropertyTests.cs`
    - **Property 3: CamelCase Wire Format**
    - **Validates: Requirements 7.2**
    - Use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`
    - Generate random DTO instances, serialize with client settings, parse JSON keys
    - Assert: all top-level keys start with lowercase character

  - [x] 9.3 Write property test for null property omission (Property 4)
    - Add to `SerializationFormatPropertyTests.cs`
    - **Property 4: Null Property Omission**
    - **Validates: Requirements 7.3**
    - Generate DTOs with random nullable properties set to null
    - Assert: serialized JSON does not contain keys for null-valued properties

- [x] 10. Property-based tests — error classification and rate limiting
  - [x] 10.1 Write property test for error response classification (Property 2)
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/ErrorClassificationPropertyTests.cs`
    - **Property 2: Error Response Classification**
    - **Validates: Requirements 4.4, 4.6**
    - Use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`
    - Generate random HTTP status codes (400 with valid/invalid JSON, 403, 500, etc.)
    - Assert: correct exception type thrown for each status code category

  - [x] 10.2 Write property test for rate limiter blocking (Property 5)
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/RateLimiterPropertyTests.cs`
    - **Property 5: Rate Limiter Blocking**
    - **Validates: Requirements 6.2**
    - Generate random N (1–100), issue N AcquireRateLimitTokenAsync calls
    - Assert: N+1th call does not complete within 50ms (blocks)

  - [x] 10.3 Write property test for rate limiter application (Property 6)
    - Add to `RateLimiterPropertyTests.cs`
    - **Property 6: Rate Limiter Application**
    - **Validates: Requirements 6.3**
    - Generate random positive integers (1–10000)
    - Assert: ApplyRateLimit does not throw for any positive value


  - [x] 10.4 Write property test for HTTP request equivalence (Property 7)
    - Create `OE2EmpireTracker.Tests/Client/FactionServer/HttpRequestPropertyTests.cs`
    - **Property 7: HTTP Request Equivalence**
    - **Validates: Requirements 8.2**
    - Generate random characterUUIDs, call typed client methods with mock HttpMessageHandler
    - Assert: correct HTTP method, correct URL path from endpoint mapping, correct Content-Type for writes

- [x] 11. Checkpoint - Ensure all property tests pass
  - Ensure all tests pass (unit + property), ask the user if questions arise.

- [x] 12. Update external spec documents
  - [x] 12.1 Update spec/design/services/client-services.md
    - Add `IFactionServerTypedClient` interface documentation with method listing
    - Add `FactionServerTypedClient` class with constructor parameters and behavior notes
    - Update SyncManager section to show `IFactionServerTypedClient` dependency instead of `RemoteFactionClient`
    - _Requirements: 8.3 (migration path documentation)_
    - _Inputs: design.md (Architecture, Requirements Traceability, External Spec Updates sections)_
    - _Output: Updated spec/design/services/client-services.md_
    - _Verification: File exists with IFactionServerTypedClient documented_

  - [x] 12.2 Update spec/requirements/Sharing.md
    - Update sequence diagram participant from `RemoteFactionClient` to `IFactionServerTypedClient`
    - Ensure sharing rule endpoints reference the typed SharingRuleDto in Common
    - _Requirements: 8.3 (migration path documentation)_
    - _Inputs: spec/requirements/Sharing.md (current content), design.md (External Spec Updates)_
    - _Output: Updated spec/requirements/Sharing.md_
    - _Verification: Sequence diagram uses IFactionServerTypedClient participant_

- [x] 13. Final checkpoint - Ensure all tests pass and audit clean
  - Ensure full solution builds with zero errors and zero warnings.
  - Ensure all tests pass (vstest.console + dotnet test server tests).
  - Ensure `node .kiro/tools/audit.js` reports zero findings.
  - Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The implementation language is C# (.NET Framework 4.8.1) matching the existing project
- FsCheck 2.16.6 is used for property tests (no 3.x APIs)
- All files use Newtonsoft.Json (no System.Text.Json in Common)
- Task sizing: each task modifies ≤5 files, adds ≤200 new lines, covers ≤3 acceptance criteria


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "3.1"] },
    { "id": 1, "tasks": ["1.2", "2.1", "3.2"] },
    { "id": 2, "tasks": ["2.2", "3.3", "4.1"] },
    { "id": 3, "tasks": ["5.1"] },
    { "id": 4, "tasks": ["5.2"] },
    { "id": 5, "tasks": ["5.3", "5.4"] },
    { "id": 6, "tasks": ["5.5"] },
    { "id": 7, "tasks": ["5.6"] },
    { "id": 8, "tasks": ["7.1", "9.1", "9.2"] },
    { "id": 9, "tasks": ["7.2", "9.3", "10.1"] },
    { "id": 10, "tasks": ["7.3", "10.2", "10.3"] },
    { "id": 11, "tasks": ["10.4"] },
    { "id": 12, "tasks": ["12.1", "12.2"] }
  ]
}
```
