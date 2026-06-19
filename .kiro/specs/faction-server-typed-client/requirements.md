# Requirements Document

## Introduction

This feature replaces the raw JSON communication in `RemoteFactionClient` with a fully-typed client built on an OpenAPI (Swagger) specification. The new `IFactionServerTypedClient` and its implementation will live in `OE2EmpireTracker.Common/Client/` following the same pattern as `IGameApiTypedClient`/`GameApiTypedClient`. All faction server endpoints will have corresponding strongly-typed DTOs, eliminating raw `string` JSON payloads from the sync and data layers.

## Glossary

- **Faction_Server**: The remote backend service that stores faction/character data, handles sync, sharing rules, and push events
- **Typed_Client**: A C# class implementing an interface that exposes one async method per API endpoint, returning/accepting typed DTOs instead of raw JSON strings
- **DTO**: Data Transfer Object — a plain class with typed properties representing a request or response payload
- **OpenAPI_Spec**: A machine-readable YAML/JSON document describing the Faction Server API endpoints, request/response schemas, and authentication
- **Swagger_Definition**: Synonym for OpenAPI_Spec in this context
- **Bulk_Import**: The PUT endpoint that uploads an entire character's data to the server for validation and persistence
- **Sync_Snapshot**: A server response containing a timestamp and processing-active flag, used for divergence detection
- **SyncManager**: The component that orchestrates write-through, offline queuing, and divergence resolution between local and remote data
- **RemoteFactionClient**: The existing HTTP/WebSocket client that communicates with the Faction Server using raw JSON strings
- **Common_Project**: The `OE2EmpireTracker.Common` class library shared between the WinForms app and the server project


## Requirements

### Requirement 1: OpenAPI Specification for the Faction Server

**User Story:** As a developer, I want a formal OpenAPI specification for the Faction Server API, so that clients can be generated or hand-built from a single source of truth.

#### Acceptance Criteria

1. THE OpenAPI_Spec SHALL define all Faction Server endpoints currently implemented in RemoteFactionClient: health check, list factions, list characters, create character, get character data by type, get all character data, bulk import, upload global data, get sync snapshot, export character data, get sharing rules, and put sharing rules
2. THE OpenAPI_Spec SHALL define request and response schemas for every endpoint using JSON Schema types (no untyped `object` or `string` placeholders for structured data)
3. THE OpenAPI_Spec SHALL define a Bearer token authentication security scheme matching the Faction Server's current auth mechanism
4. THE OpenAPI_Spec SHALL use semantic versioning in the `info.version` field starting at `1.0.0`
5. THE OpenAPI_Spec SHALL be stored in `OE2EmpireTracker.Common/Client/faction-server-swagger.yaml`


### Requirement 2: Strongly-Typed DTOs for All Endpoints

**User Story:** As a developer, I want strongly-typed DTO classes for every Faction Server request and response, so that I never pass raw JSON strings between components.

#### Acceptance Criteria

1. THE Common_Project SHALL contain DTO classes for each Faction Server response: HealthResponse, FactionListResponse, CharacterListResponse, CharacterDataResponse, BulkImportResponse, SyncSnapshotResponse, ExportResponse, SharingRulesResponse, and ValidationErrorResponse
2. THE Common_Project SHALL contain DTO classes for each Faction Server request body: CreateCharacterRequest, BulkImportRequest, UploadGlobalDataRequest, and PutSharingRulesRequest
3. WHEN a DTO property represents a UUID, THE DTO SHALL type it as `string` with a descriptive property name ending in `UUID` or `Id`
4. WHEN a DTO property represents a timestamp, THE DTO SHALL type it as `DateTime` or `DateTimeOffset`
5. THE DTO classes SHALL use `Newtonsoft.Json` attributes (`[JsonProperty]`) on all properties regardless of property complexity, consistent with the existing project conventions
6. THE DTO classes SHALL reside in the namespace `OE2EmpireTracker.Common.Client.FactionServer` within a `FactionServer/` subfolder of `OE2EmpireTracker.Common/Client/`


### Requirement 3: Typed Client Interface

**User Story:** As a developer, I want a typed client interface for the Faction Server, so that consuming code depends on an abstraction with typed method signatures.

#### Acceptance Criteria

1. THE Common_Project SHALL define an `IFactionServerTypedClient` interface with one async method per Faction Server endpoint, each returning a typed DTO
2. THE IFactionServerTypedClient interface SHALL follow the same patterns as `IGameApiTypedClient`: async Task return types, CancellationToken parameters, and IDisposable implementation
3. THE IFactionServerTypedClient interface SHALL expose a `bool IsConnected` property for connection status checking
4. THE IFactionServerTypedClient interface SHALL expose methods for: CheckHealthAsync, GetFactionsAsync, GetCharactersAsync, CreateCharacterAsync, GetCharacterDataAsync, GetAllCharacterDataAsync, BulkImportAsync, UploadGlobalDataAsync, GetSyncSnapshotAsync, ExportCharacterDataAsync, GetSharingRulesAsync, and PutSharingRulesAsync


### Requirement 4: Typed Client Implementation

**User Story:** As a developer, I want a concrete typed client implementation in Common, so that both the WinForms app and the server project can use the same typed HTTP client.

#### Acceptance Criteria

1. THE Common_Project SHALL contain a `FactionServerTypedClient` class implementing `IFactionServerTypedClient`
2. THE FactionServerTypedClient SHALL accept a base URL, bearer token, and optional certificate thumbprint in its constructor (matching the existing RemoteFactionClient parameter set)
3. THE FactionServerTypedClient SHALL serialize request DTOs to JSON and deserialize response JSON into typed DTOs using `Newtonsoft.Json`
4. THE FactionServerTypedClient SHALL throw typed exceptions (not return null) on HTTP error responses: a distinct exception type for HTTP 400 (validation), HTTP 403 (forbidden), and general HTTP failures
5. THE FactionServerTypedClient SHALL include a configurable request timeout defaulting to 5 minutes for large payloads (matching the existing RemoteFactionClient behavior)
6. IF the server returns HTTP 400, THEN THE FactionServerTypedClient SHALL attempt to parse the validation errors from the response body and throw a `FactionValidationException`; IF parsing the validation errors fails, THEN THE FactionServerTypedClient SHALL fall back to throwing the general HTTP failure exception


### Requirement 5: Certificate Pinning and Authentication

**User Story:** As a developer, I want the typed client to support the same certificate pinning and bearer token authentication as the existing RemoteFactionClient, so that the transition is seamless.

#### Acceptance Criteria

1. THE FactionServerTypedClient SHALL support optional self-signed certificate pinning via a trusted thumbprint parameter
2. WHEN a trusted thumbprint is configured, THE FactionServerTypedClient SHALL reject server certificates that do not match the thumbprint
3. THE FactionServerTypedClient SHALL attach the bearer token as an `Authorization: Bearer <token>` header on every HTTP request WHERE certificate validation has not already failed
4. THE FactionServerTypedClient SHALL accept the bearer token as a `SecureString` and dispose it when the client is disposed regardless of whether client disposal succeeds or is interrupted


### Requirement 6: Rate Limiting

**User Story:** As a developer, I want the typed client to enforce rate limiting, so that the Faction Server is not overwhelmed by requests.

#### Acceptance Criteria

1. THE FactionServerTypedClient SHALL implement a configurable rate limiter defaulting to 60 requests per minute
2. WHEN the rate limit is reached, THE FactionServerTypedClient SHALL queue the request and wait until a rate limit token is available before sending
3. WHEN the server communicates a new rate limit (via WebSocket message), THE FactionServerTypedClient SHALL apply the updated limit without bounds checking regardless of how restrictive the new limit is


### Requirement 7: DTO Serialization Round-Trip

**User Story:** As a developer, I want to guarantee that DTOs serialize and deserialize correctly, so that no data is lost in transit between client and server.

#### Acceptance Criteria

1. FOR ALL valid DTO instances, serializing to JSON then deserializing back SHALL produce an equivalent object (round-trip property)
2. THE DTO serialization SHALL use `camelCase` property naming on the wire (matching the `[JsonProperty]` attributes)
3. WHEN a DTO property is null, THE serializer SHALL omit it from the JSON output (matching the existing Faction Server behavior of ignoring null fields)


### Requirement 8: Migration Path from RemoteFactionClient

**User Story:** As a developer, I want a clear migration path from the raw-JSON RemoteFactionClient to the new typed client, so that SyncManager and other consumers can be updated incrementally.

#### Acceptance Criteria

1. THE IFactionServerTypedClient interface SHALL cover all endpoints currently exposed by RemoteFactionClient (no loss of functionality)
2. THE FactionServerTypedClient SHALL produce equivalent HTTP requests (same URL paths, HTTP methods, headers, and body structure) as the existing RemoteFactionClient for every endpoint
3. WHEN the typed client is integrated, THE SyncManager SHALL accept `IFactionServerTypedClient` instead of `RemoteFactionClient` as its client dependency
4. THE typed client SHALL maintain the same error handling semantics: HTTP 400 returns validation errors, HTTP 403 signals unauthorized, network failures signal connection loss


### Requirement 9: Typed Exception Hierarchy

**User Story:** As a developer, I want typed exceptions for Faction Server errors, so that consuming code can catch specific failure modes without parsing raw HTTP responses.

#### Acceptance Criteria

1. THE Common_Project SHALL define a `FactionServerException` base exception class
2. THE Common_Project SHALL define a `FactionValidationException` (extends FactionServerException) containing a list of `FactionValidationError` objects with EntityType, EntityUUID, Field, and Error properties
3. THE Common_Project SHALL define a `FactionAuthorizationException` (extends FactionServerException) thrown on HTTP 403
4. THE Common_Project SHALL define a `FactionConnectionException` (extends FactionServerException) thrown on network failures or timeouts

### Requirement 10: Shared Location of Client and DTOs

**User Story:** As a developer, I want the typed client and DTOs in the Common project, so that both the WinForms app and the server project can reference them without circular dependencies.

#### Acceptance Criteria

1. THE IFactionServerTypedClient interface, FactionServerTypedClient implementation, all DTOs, and all exception types SHALL reside in the `OE2EmpireTracker.Common` project
2. THE Common_Project SHALL NOT introduce new NuGet package dependencies beyond what is already referenced (Newtonsoft.Json, System.Net.Http)
3. THE typed client source files SHALL be organized under `OE2EmpireTracker.Common/Client/FactionServer/` with subfolders for DTOs and Exceptions
