# Implementation Plan: Faction Typed Client Migration

## Overview

Migrate all consumers of `RemoteFactionClient` to `IFactionServerTypedClient` + `FactionPushClient`, then delete the legacy client. Tasks are ordered to maintain a compilable codebase at each step.

## Tasks

- [ ] 1. Create RateLimitChangedEventArgs and add ProcessingActive to SyncResponse
  - Create `OE2EmpireTracker/Client/RateLimitChangedEventArgs.cs` with `int RequestsPerMinute` property
  - Add `[JsonProperty("processingActive")] public bool ProcessingActive { get; set; }` to `SyncResponse.cs` in Common
  - _Requirements: 1.8, 10.1_
  - _Inputs: SyncResponse.cs, RemoteFactionClient.cs (for event signature reference)_
  - _Output: RateLimitChangedEventArgs.cs (new), SyncResponse.cs (modified)_
  - _Verification: Solution builds with zero errors/warnings_

- [ ] 2. Create FactionPushClient — WebSocket connection and receive loop
  - [ ] 2.1 Create FactionPushClient class skeleton with constructor, fields, events, IDisposable
    - Extract constants (HeartbeatIntervalMs=30000, MaxReconnectDelayMs=60000, MaxReconnectAttempts=10, DefaultPollingIntervalSeconds=60)
    - Constructor accepts (string serverUrl, SecureString bearerToken, string trustedThumbprint, IFactionServerTypedClient typedClient)
    - Events: EventReceived, ConnectionStatusChanged, RealtimeModeChanged, RateLimitChanged
    - Properties: IsConnected, IsRealtimeMode, PollingIntervalSeconds
    - Methods: stubs for ConnectWebSocketAsync, DisconnectWebSocketAsync, Disconnect, TryConnectAsync, SetConnectionStatus, Dispose
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_
    - _Inputs: RemoteFactionClient.cs (lines 1-100 for structure)_
    - _Output: FactionPushClient.cs (new, ~150 lines skeleton)_
    - _Verification: Solution builds_

  - [ ] 2.2 Implement FactionPushClient WebSocket connection, receive loop, and message processing
    - Copy `ConnectWebSocketAsync`, `BuildWebSocketUrl`, `ReceiveLoopAsync`, `ProcessIncomingMessage`, `HandleConnectedMessage`, `HandleEventMessage`, `HandleRateLimitMessage` from RemoteFactionClient
    - HandleRateLimitMessage raises `RateLimitChanged` event in addition to internal handling
    - HandleConnectedMessage raises `RateLimitChanged` when initial rate limits are present
    - _Requirements: 1.8, 9.4, 10.1, 10.3_
    - _Inputs: RemoteFactionClient.cs (WebSocket section), FactionPushClient.cs skeleton_
    - _Output: FactionPushClient.cs (modified, +200 lines)_
    - _Verification: Solution builds_


  - [ ] 2.3 Implement FactionPushClient heartbeat, reconnection, and polling fallback
    - Copy `StartHeartbeat`, `StopHeartbeat`, `SendHeartbeat`, `HandleWebSocketDisconnect`, `ReconnectLoopAsync`, `CalculateBackoffDelay` from RemoteFactionClient
    - Copy `StartPolling`, `StopPolling`, `PollServer` from RemoteFactionClient
    - Adapt `PollServer` to use typed client's methods if needed (currently it calls GetSyncSnapshotAsync which stays on push client as a simple HTTP call for now, or delegates to typedClient)
    - _Requirements: 9.5, 9.6, 6.1, 6.3, 6.4_
    - _Inputs: RemoteFactionClient.cs (heartbeat/reconnect/polling sections)_
    - _Output: FactionPushClient.cs (modified, +150 lines)_
    - _Verification: Solution builds_

  - [ ] 2.4 Implement FactionPushClient TryConnectAsync, Disconnect, certificate validation, and Dispose
    - `TryConnectAsync` calls `_typedClient.CheckHealthAsync()` and sets IsConnected
    - `Disconnect` stops WebSocket and polling, sets IsConnected=false
    - Copy `ValidateWebSocketCertificate` from RemoteFactionClient
    - `Dispose` disposes WebSocket, timers, SecureString token
    - Copy `StopWebSocket` lifecycle helper
    - _Requirements: 6.2, 1.5, 1.7_
    - _Inputs: RemoteFactionClient.cs (TryConnect, Disconnect, Dispose, cert validation)_
    - _Output: FactionPushClient.cs (modified, +100 lines)_
    - _Verification: Solution builds_

- [x] 3. Modify QueuedChange to add TypedPayload property
  - Add `[JsonIgnore] public PlayerRoot TypedPayload { get; set; }` to QueuedChange
  - Add using for `OE2EmpireTracker.Models` and `Newtonsoft.Json`
  - _Requirements: 5.1_
  - _Inputs: QueuedChange.cs_
  - _Output: QueuedChange.cs (modified, +5 lines)_
  - _Verification: Solution builds_

- [ ] 4. Modify OfflineQueue.Load() for typed payload deserialization
  - After loading `List<QueuedChange>` from disk, iterate and try `JsonConvert.DeserializeObject<PlayerRoot>(change.Json)` for each entry
  - On success: set `change.TypedPayload = deserialized`
  - On failure (JsonException): log warning, skip that entry (remove from list)
  - _Requirements: 5.3, 5.5_
  - _Inputs: OfflineQueue.cs, QueuedChange.cs_
  - _Output: OfflineQueue.cs (modified, +30 lines)_
  - _Verification: Solution builds_

- [ ] 4.1 Write property test for OfflineQueue PlayerRoot round-trip
  - **Property 1: OfflineQueue PlayerRoot Round-Trip**
  - **Validates: Requirements 5.2, 5.3**
  - Generate random PlayerRoot objects, serialize to QueuedChange.Json, load via OfflineQueue, verify TypedPayload equivalence


- [ ] 5. Migrate SyncManager constructor and IsOnline property
  - Change constructor to accept `IFactionServerTypedClient typedClient, FactionPushClient pushClient, OfflineQueue offlineQueue`
  - Change `_client` field to `_typedClient` (IFactionServerTypedClient) and add `_pushClient` field
  - Change `IsOnline` to use `_pushClient?.IsConnected ?? false`
  - Add using for `OE2EmpireTracker.Common.Client.FactionServer`
  - _Requirements: 2.1, 2.8, 6.5_
  - _Inputs: SyncManager.cs, IFactionServerTypedClient.cs_
  - _Output: SyncManager.cs (modified)_
  - _Verification: getDiagnostics (may have errors until methods are updated — that's OK, this is intermediate)_

- [ ] 6. Migrate SyncManager.WriteToServerAsync to typed client
  - Change signature to accept `PlayerRoot playerRoot` instead of `string json`
  - Call `_typedClient.BulkImportAsync(characterUUID, playerRoot)` instead of `_client.BulkImportAsync(characterUUID, json)`
  - Replace `response.StatusCode == HttpStatusCode.OK` check with: success = no exception thrown
  - Replace `response.StatusCode == HttpStatusCode.BadRequest` with `catch (FactionValidationException ex)`
  - Replace `response.StatusCode == HttpStatusCode.Forbidden` with `catch (FactionAuthorizationException)`
  - Replace `HttpRequestException` catch with `catch (FactionConnectionException)` for offline queuing
  - Update `QueueOfflineChange` to also store `TypedPayload`
  - Remove `HandleValidationFailureAsync` private method (replaced by exception handling)
  - Remove `HandleForbiddenResponse` private method
  - _Requirements: 2.2, 2.4, 2.5, 2.6_
  - _Inputs: SyncManager.cs, IFactionServerTypedClient.cs, FactionValidationException.cs_
  - _Output: SyncManager.cs (modified, net -50 lines)_
  - _Verification: Solution builds_

- [ ] 7. Migrate SyncManager.PullFullSyncAsync and HandleReconnectionAsync
  - PullFullSyncAsync: call `_typedClient.GetSyncSnapshotAsync()` → typed `SyncResponse`
  - Read `response.ServerTimestamp.ToString("o")` for LastSyncTimestamp
  - Read `response.ProcessingActive` for ServerProcessingActive
  - Remove `ExtractTimestamp` and `ExtractProcessingActive` private methods
  - Remove `ParseValidationErrors` private method (no longer needed)
  - HandleReconnectionAsync: call `_pushClient.TryConnectAsync()` instead of `_client.TryConnectAsync()`
  - Remove all `using Newtonsoft.Json.Linq` and `using System.Net` imports (no longer needed)
  - _Requirements: 2.3, 2.7_
  - _Inputs: SyncManager.cs_
  - _Output: SyncManager.cs (modified, net -60 lines)_
  - _Verification: Solution builds_


- [ ] 8. Migrate SyncManager.FlushOfflineQueueAsync to typed client
  - Change flush loop to use `change.TypedPayload` (PlayerRoot) with `_typedClient.BulkImportAsync`
  - Replace HTTP status code checks with typed exception catching (same pattern as WriteToServerAsync)
  - If `TypedPayload` is null (legacy entry that failed deserialization), skip with warning
  - _Requirements: 5.4_
  - _Inputs: SyncManager.cs_
  - _Output: SyncManager.cs (modified)_
  - _Verification: Solution builds_

- [ ] 9. Checkpoint — verify SyncManager migration compiles cleanly
  - Build full solution
  - Ensure zero errors, zero warnings
  - Ensure all SyncManager references to RemoteFactionClient are removed
  - _Verification: MSBuild OE2EmpireTracker.sln with zero errors/warnings_

- [ ] 10. Migrate ServerContext to use typed client and push client
  - Remove `RemoteFactionClient Client` property
  - Add `IFactionServerTypedClient TypedClient` property
  - Add `FactionPushClient PushClient` property
  - Update constructor to accept (IFactionServerTypedClient, FactionPushClient, SyncManager, OfflineQueue, OperatingMode)
  - Update `Initialize()`:
    - Create `FactionServerTypedClient(serverUrl, token, thumbprint)`
    - Clone token via `.Copy()` for push client
    - Create `FactionPushClient(serverUrl, tokenCopy, thumbprint, typedClient)`
    - Subscribe `PushClient.RateLimitChanged += OnRateLimitChanged`
    - In handler: if not disposing, call `((FactionServerTypedClient)TypedClient).ApplyRateLimit(e.RequestsPerMinute)`
    - Create SyncManager(typedClient, pushClient, offlineQueue)
  - Update `Dispose()`:
    - Unsubscribe from RateLimitChanged
    - Dispose TypedClient and PushClient
    - Set `_disposed = true` before disposing to guard the rate limit handler
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_
  - _Inputs: ServerContext.cs, FactionPushClient.cs, FactionServerTypedClient.cs_
  - _Output: ServerContext.cs (modified)_
  - _Verification: Solution builds_

- [ ] 11. Migrate FormPreferences to use typed client
  - [ ] 11.1 Migrate FormPreferences Test Connection to use FactionServerTypedClient
    - Replace `new RemoteFactionClient(...)` with `new FactionServerTypedClient(url, token, thumbprint)`
    - Replace health check call with `await typedClient.CheckHealthAsync()`
    - Wrap in using block, catch FactionConnectionException for failure message
    - _Requirements: 4.1_
    - _Inputs: FormPreferences.cs_
    - _Output: FormPreferences.cs (modified)_
    - _Verification: Solution builds_

  - [ ] 11.2 Migrate FormPreferences Push-to-Server to use typed client
    - Replace `new RemoteFactionClient(...)` with `new FactionServerTypedClient(url, token, thumbprint)`
    - Replace `CreateCharacterAsync(name, uuid)` call (already same signature)
    - Replace `BulkImportAsync(characterUUID, json)` with `BulkImportAsync(characterUUID, playerRoot)` (pass typed PlayerRoot)
    - Replace `UploadGlobalDataAsync("baseline", json)` with `UploadBaselineAsync(baselineRoot)` (pass typed BaselineRoot)
    - Catch `FactionValidationException` and display errors to user
    - _Requirements: 4.2, 4.3, 4.4, 4.5_
    - _Inputs: FormPreferences.cs_
    - _Output: FormPreferences.cs (modified)_
    - _Verification: Solution builds_


- [ ] 12. Checkpoint — verify all consumer migrations compile
  - Build full solution
  - Ensure zero errors, zero warnings
  - At this point, RemoteFactionClient.cs is still present but unused
  - _Verification: MSBuild OE2EmpireTracker.sln with zero errors/warnings_

- [ ] 13. Update DesktopSyncMigrationTests to use typed client and push client
  - [ ] 13.1 Update test setup to construct mock IFactionServerTypedClient and FactionPushClient
    - Replace `RemoteFactionClient` construction with mock `IFactionServerTypedClient` (or real FactionServerTypedClient pointing at test server)
    - Create `FactionPushClient` (or mock for connection status)
    - Update `SyncManager` construction to use new signature
    - _Requirements: 8.1_
    - _Inputs: DesktopSyncMigrationTests.cs, SyncManager.cs_
    - _Output: DesktopSyncMigrationTests.cs (modified)_
    - _Verification: Tests compile_

  - [ ] 13.2 Update test scenarios for typed exception handling
    - Update write-through test to verify `BulkImportAsync(characterUUID, PlayerRoot)` is called
    - Update validation failure test: mock throws `FactionValidationException`, verify `SyncValidationFailed` raised
    - Update authorization test: mock throws `FactionAuthorizationException`, verify unauthorized status
    - Update connection failure test: mock throws `FactionConnectionException`, verify offline queuing
    - _Requirements: 8.2, 8.3, 8.4_
    - _Inputs: DesktopSyncMigrationTests.cs_
    - _Output: DesktopSyncMigrationTests.cs (modified)_
    - _Verification: Tests compile and pass_

- [ ] 14. Delete RemoteFactionClient.cs
  - Delete `OE2EmpireTracker/Client/RemoteFactionClient.cs`
  - Verify no remaining compile-time references (grep for "RemoteFactionClient" in all .cs files)
  - _Requirements: 7.1, 7.2, 7.4_
  - _Inputs: All .cs files (grep verification)_
  - _Output: RemoteFactionClient.cs (deleted)_
  - _Verification: Solution builds with zero errors, zero warnings (Req 7.3)_

- [ ] 15. Final checkpoint — full build and test verification
  - Build full solution: zero errors, zero warnings
  - Run all test suites (vstest.console for WinForms, dotnet test for Server)
  - Run `node .kiro/tools/audit.js` — zero findings
  - Ensure all tests pass with zero failures
  - _Requirements: 7.3, 8.5, 9.1, 9.2, 9.3_
  - _Verification: All builds pass, all tests pass, audit clean_

## Notes

- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints (tasks 9, 12, 15) ensure incremental validation
- The migration maintains a compilable codebase at each step — RemoteFactionClient is only deleted after all consumers are migrated
- SyncManager is the highest-risk migration (most logic changes) — tasks 5-8 decompose it into focused steps
- FactionPushClient is a direct code extraction — minimal logic changes, just moving code to a new file
