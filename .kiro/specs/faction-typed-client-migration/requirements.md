# Requirements Document

## Introduction

This feature migrates all consumers of the legacy `RemoteFactionClient` to the new `IFactionServerTypedClient` interface (created by the `faction-server-typed-client` spec). The legacy client communicates with the Faction Server using raw JSON strings and combines HTTP communication with WebSocket push event handling in a single class. The migration separates these responsibilities: HTTP calls move to `IFactionServerTypedClient`, while WebSocket/push functionality is extracted into a dedicated `FactionPushClient` class. After migration, `RemoteFactionClient` is deleted.

## Glossary

- **RemoteFactionClient**: The legacy HTTP+WebSocket client that communicates with the Faction Server using raw JSON strings and handles WebSocket push events, reconnection, heartbeat, and polling fallback
- **Typed_Client**: The `FactionServerTypedClient` class implementing `IFactionServerTypedClient`, returning strongly-typed domain objects instead of raw JSON
- **FactionPushClient**: A new class extracted from RemoteFactionClient that handles WebSocket connection, push event receiving, heartbeat, reconnection with exponential backoff, and polling fallback
- **SyncManager**: The orchestrator for write-through, offline queuing, and divergence resolution between local and remote faction data
- **ServerContext**: The singleton that creates and holds the remote server infrastructure instances (client, sync manager, offline queue)
- **FormPreferences**: The preferences form that uses RemoteFactionClient for test-connection and push-local-to-server operations
- **OfflineQueue**: The queue that stores changes for later sync when the client is offline
- **PushEventArgs**: Event arguments for push events received via WebSocket (EventType, EntityType, EntityUUID, CharacterUUID, Timestamp)
- **Common_Project**: The `OE2EmpireTracker.Common` class library shared between the WinForms app and the server project


## Requirements

### Requirement 1: Extract WebSocket/Push into FactionPushClient

**User Story:** As a developer, I want WebSocket push handling extracted from RemoteFactionClient into a dedicated class, so that HTTP and real-time concerns are separated and the HTTP client can be replaced independently.

#### Acceptance Criteria

1. THE WinForms project SHALL contain a `FactionPushClient` class in `OE2EmpireTracker/Client/` that encapsulates WebSocket connection, receive loop, heartbeat timer, reconnection with exponential backoff, and polling fallback
2. THE FactionPushClient SHALL expose an `EventReceived` event with `PushEventArgs` matching the existing RemoteFactionClient event signature
3. THE FactionPushClient SHALL expose a `ConnectionStatusChanged` event with `ConnectionStatusChangedEventArgs` matching the existing RemoteFactionClient event signature
4. THE FactionPushClient SHALL expose a `RealtimeModeChanged` event with `RealtimeModeChangedEventArgs` matching the existing RemoteFactionClient event signature
5. THE FactionPushClient SHALL expose `ConnectWebSocketAsync`, `DisconnectWebSocketAsync`, and `Disconnect` methods matching the existing RemoteFactionClient WebSocket API
6. THE FactionPushClient SHALL accept a server URL, SecureString bearer token, and optional trusted certificate thumbprint in its constructor
7. THE FactionPushClient SHALL implement IDisposable, disposing the WebSocket, timers, and SecureString token
8. WHEN a `rateLimitChanged` WebSocket message is received, THE FactionPushClient SHALL both apply the rate limit internally (for its own polling HTTP calls) AND raise a `RateLimitChanged` event so that external listeners (ServerContext) can synchronize other clients


### Requirement 2: SyncManager Migration to IFactionServerTypedClient

**User Story:** As a developer, I want SyncManager to use IFactionServerTypedClient for all API calls, so that sync operations use typed domain objects instead of raw JSON strings.

#### Acceptance Criteria

1. THE SyncManager SHALL accept `IFactionServerTypedClient` as a constructor dependency instead of `RemoteFactionClient`
2. WHEN performing a bulk import via WriteToServerAsync, THE SyncManager SHALL call `IFactionServerTypedClient.BulkImportAsync(characterUUID, playerRoot)` with a typed `PlayerRoot` object instead of a raw JSON string
3. WHEN pulling a sync snapshot, THE SyncManager SHALL call `IFactionServerTypedClient.GetSyncSnapshotAsync()` and receive a typed `SyncResponse` object instead of parsing raw JSON with JObject
4. WHEN the SyncManager receives a `FactionValidationException` from BulkImportAsync, THE SyncManager SHALL extract the validation errors from the exception's `Errors` list and raise the `SyncValidationFailed` event
5. WHEN the SyncManager receives a `FactionAuthorizationException` from BulkImportAsync, THE SyncManager SHALL set the connection status to unauthorized
6. WHEN the SyncManager receives a `FactionConnectionException` from BulkImportAsync, THE SyncManager SHALL queue the change offline
7. THE SyncManager SHALL remove all `JObject.Parse`, `ExtractTimestamp`, and `ExtractProcessingActive` manual JSON parsing code
8. THE SyncManager SHALL accept an `IFactionPushClient` (or the FactionPushClient directly) for connection status awareness, since `IsConnected` and `TryConnectAsync` now live on the push client


### Requirement 3: ServerContext Migration

**User Story:** As a developer, I want ServerContext to create and hold both the typed client and the push client, so that all downstream consumers use the typed interface and push events still work.

#### Acceptance Criteria

1. WHEN ServerContext initializes, THE ServerContext SHALL create a `FactionServerTypedClient` instance configured with the server URL, bearer token, and trusted thumbprint from preferences
2. WHEN ServerContext initializes, THE ServerContext SHALL create a `FactionPushClient` instance configured with the same connection parameters
3. THE ServerContext SHALL expose the typed client via a property typed as `IFactionServerTypedClient`
4. THE ServerContext SHALL expose the push client via a property typed as `FactionPushClient`
5. THE ServerContext SHALL remove the `RemoteFactionClient Client` property
6. WHEN the FactionPushClient raises `RateLimitChanged`, THE ServerContext SHALL call `ApplyRateLimit` on the FactionServerTypedClient to synchronize the rate limit; IF either client is disposed or ServerContext is in the process of disposing, THE ServerContext SHALL skip the ApplyRateLimit call
7. WHEN ServerContext is disposed, THE ServerContext SHALL dispose both the typed client and the push client


### Requirement 4: FormPreferences Migration

**User Story:** As a developer, I want FormPreferences to use IFactionServerTypedClient for test-connection and push-to-server operations, so that it no longer creates RemoteFactionClient instances directly.

#### Acceptance Criteria

1. WHEN the user clicks "Test Connection", THE FormPreferences SHALL create a temporary `FactionServerTypedClient` (configured with the form's URL, token, and thumbprint fields) and call `CheckHealthAsync` instead of creating a RemoteFactionClient; THE typed client SHALL be created before any API calls are made
2. WHEN the user clicks "Push Local to Server", THE FormPreferences SHALL create a temporary `FactionServerTypedClient` and use `BulkImportAsync(characterUUID, playerRoot)` with typed PlayerRoot instead of raw JSON
3. WHEN pushing baseline data, THE FormPreferences SHALL call `UploadBaselineAsync(baselineRoot)` with a typed BaselineRoot instead of raw JSON via `UploadGlobalDataAsync`
4. WHEN registering characters during push, THE FormPreferences SHALL call `CreateCharacterAsync(name, uuid)` on the typed client
5. WHEN push operations encounter a `FactionValidationException`, THE FormPreferences SHALL display validation errors to the user


### Requirement 5: OfflineQueue Migration to Typed Payloads

**User Story:** As a developer, I want the offline queue to store typed payloads instead of raw JSON strings, so that queued changes can be sent directly via the typed client on reconnection.

#### Acceptance Criteria

1. THE QueuedChange class SHALL store a `PlayerRoot` typed payload property instead of (or in addition to) the raw `Json` string property
2. WHEN a change is queued for offline sync, THE SyncManager SHALL serialize the PlayerRoot to JSON for disk persistence via the OfflineQueue
3. WHEN the offline queue is loaded from disk, THE OfflineQueue SHALL deserialize stored JSON back into PlayerRoot objects
4. WHEN flushing queued changes, THE SyncManager SHALL pass the typed `PlayerRoot` to `IFactionServerTypedClient.BulkImportAsync` instead of a raw JSON string
5. THE OfflineQueue disk format SHALL remain backward-compatible: existing queued changes with raw JSON SHALL be loadable and converted to typed objects on first load; IF deserialization fails for individual entries due to schema changes or corrupted data, THE OfflineQueue SHALL skip those entries, log warnings, and load the remaining valid changes


### Requirement 6: Connection Status Coordination

**User Story:** As a developer, I want connection status to be coordinated between the push client and the typed HTTP client, so that SyncManager knows when the server is reachable.

#### Acceptance Criteria

1. THE FactionPushClient SHALL expose a `bool IsConnected` property that reflects WebSocket or polling connectivity status
2. THE FactionPushClient SHALL expose a `TryConnectAsync` method that calls the typed client's `CheckHealthAsync` to verify HTTP reachability
3. WHEN the FactionPushClient detects a WebSocket disconnection, THE FactionPushClient SHALL set `IsConnected` to false and raise `ConnectionStatusChanged`
4. WHEN the FactionPushClient successfully reconnects (WebSocket or polling), THE FactionPushClient SHALL set `IsConnected` to true and raise `ConnectionStatusChanged`
5. THE SyncManager SHALL use the FactionPushClient's `IsConnected` property for its `IsOnline` check instead of RemoteFactionClient's property


### Requirement 7: RemoteFactionClient Removal

**User Story:** As a developer, I want RemoteFactionClient deleted from the codebase after all consumers are migrated, so that there is no dead code or ambiguity about which client to use.

#### Acceptance Criteria

1. WHEN all consumers have been migrated, THE codebase SHALL delete `RemoteFactionClient.cs`
2. THE codebase SHALL retain no compile-time references to `RemoteFactionClient` after deletion
3. THE codebase SHALL compile with zero errors and zero warnings immediately after removal
4. THE legacy methods `GetCharacterDataAsync(characterUUID, dataType)` and `GetAllCharacterDataAsync(characterUUID)` SHALL NOT be ported to the typed client (they are superseded by per-entity-type methods); no convenience methods that aggregate multiple per-entity-type calls SHALL be created


### Requirement 8: Test Migration

**User Story:** As a developer, I want existing tests updated to use IFactionServerTypedClient and FactionPushClient, so that tests validate the new code paths.

#### Acceptance Criteria

1. THE DesktopSyncMigrationTests SHALL be updated to construct a `FactionServerTypedClient` (or mock `IFactionServerTypedClient`) and `FactionPushClient` instead of RemoteFactionClient
2. THE SyncManager test scenarios (write-through, offline queuing, reconnection, divergence detection, validation failure handling, forbidden response) SHALL maintain equivalent coverage after migration
3. WHEN testing bulk import with typed payloads, THE tests SHALL verify that `FactionValidationException` is caught and surfaces validation errors through `SyncValidationFailed`
4. WHEN testing bulk import with typed payloads, THE tests SHALL verify that `FactionAuthorizationException` triggers the unauthorized status
5. THE full test suite SHALL pass with zero failures after migration


### Requirement 9: Behavioral Equivalence

**User Story:** As a developer, I want the migrated system to behave identically to the current system for all user-visible functionality, so that no functionality is lost during migration.

#### Acceptance Criteria

1. THE SyncManager SHALL produce the same sync results (write-through success, offline queuing on disconnect, divergence detection on reconnection, queue flushing) when calling Typed_Client methods as it did when calling RemoteFactionClient methods; THE new system MAY fix incorrect behaviors in the legacy system (such as claiming write-through success when the server was unreachable)
2. THE FormPreferences test-connection button SHALL report the same pass/fail status for the same server state as it did with RemoteFactionClient
3. THE FormPreferences push-to-server SHALL upload the same data structure to the same endpoints as it did with RemoteFactionClient
4. THE FactionPushClient SHALL deliver the same `PushEventArgs` events (same EventType, EntityType, EntityUUID, CharacterUUID, Timestamp values) as RemoteFactionClient delivered for the same WebSocket messages
5. THE FactionPushClient SHALL maintain the same reconnection behavior: exponential backoff (1s, 2s, 4s...60s capped), 10 max attempts before fallback to polling, polling interval of 60 seconds
6. THE FactionPushClient SHALL maintain the same heartbeat behavior: ping every 30 seconds while WebSocket is connected; WHEN the connection drops, THE heartbeat timer SHALL continue running so that reconnection immediately resumes the 30-second ping cycle


### Requirement 10: Rate Limit Coordination

**User Story:** As a developer, I want the WebSocket-received rate limit updates to be applied to the typed HTTP client, so that rate limiting remains coordinated between push and HTTP layers.

#### Acceptance Criteria

1. WHEN the FactionPushClient receives a `rateLimitChanged` WebSocket message, THE FactionPushClient SHALL raise a `RateLimitChanged` event containing the new requests-per-minute value
2. WHEN ServerContext receives the `RateLimitChanged` event, THE ServerContext SHALL call `FactionServerTypedClient.ApplyRateLimit(requestsPerMinute)` to update the HTTP client's rate limiter
3. WHEN the FactionPushClient receives a `connected` WebSocket message with embedded rate limits, THE FactionPushClient SHALL raise `RateLimitChanged` with the initial rate value
4. THE FactionServerTypedClient's `ApplyRateLimit` method SHALL be accessible from outside the class (it is already public per the design doc)

