# Design Document: Faction Typed Client Migration

## Overview

This migration replaces the monolithic `RemoteFactionClient` (1097 lines combining HTTP API, WebSocket push, reconnection, heartbeat, and polling) with two focused classes:

1. **FactionServerTypedClient** (already built in `OE2EmpireTracker.Common`) — handles all HTTP API calls, returns strongly-typed domain objects, throws typed exceptions
2. **FactionPushClient** (new, in `OE2EmpireTracker/Client/`) — handles WebSocket connection, push event receiving, heartbeat, reconnection with exponential backoff, and polling fallback

After migration, all consumers use the typed client for HTTP and the push client for real-time events. `RemoteFactionClient.cs` is deleted.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ ServerContext (singleton, owns both clients)                 │
│                                                             │
│  ┌──────────────────────┐   ┌──────────────────────────┐   │
│  │ FactionServerTypedClient │   │ FactionPushClient           │   │
│  │ (HTTP API, typed)     │   │ (WebSocket, heartbeat,   │   │
│  │                       │   │  reconnect, polling)     │   │
│  └───────────┬───────────┘   └──────────┬──────────────┘   │
│              │                           │                   │
│              │  RateLimitChanged ────────┤                   │
│              │◄─────────────────────────┘                   │
└──────────────┼───────────────────────────┼───────────────────┘
               │                           │
    ┌──────────▼──────────┐     ┌──────────▼──────────────┐
    │ SyncManager          │     │ EventReceived            │
    │ (write-through,      │     │ ConnectionStatusChanged  │
    │  offline queue,      │     │ RealtimeModeChanged      │
    │  divergence)         │     │                          │
    └─────────────────────┘     └──────────────────────────┘
```

### Dependency Flow

- `ServerContext` creates and holds both `FactionServerTypedClient` and `FactionPushClient`
- `ServerContext` listens to `FactionPushClient.RateLimitChanged` and calls `FactionServerTypedClient.ApplyRateLimit()`
- `SyncManager` depends on `IFactionServerTypedClient` (for HTTP calls) and `FactionPushClient` (for `IsConnected` / `TryConnectAsync`)
- `FormPreferences` creates temporary `FactionServerTypedClient` instances for test-connection and push operations (does not use the singleton)

## Components and Interfaces

### FactionPushClient (New Class)

**Location:** `OE2EmpireTracker/Client/FactionPushClient.cs`

Extracted directly from `RemoteFactionClient`'s WebSocket/push sections. All behavior constants and algorithms are preserved exactly.

```csharp
public class FactionPushClient : IDisposable
{
    // Constants (same values as RemoteFactionClient)
    private const int DefaultPollingIntervalSeconds = 60;
    private const int HeartbeatIntervalMs = 30000;
    private const int MaxReconnectDelayMs = 60000;
    private const int MaxReconnectAttempts = 10;

    // Constructor
    public FactionPushClient(
        string serverUrl,
        SecureString bearerToken,
        string trustedThumbprint,
        IFactionServerTypedClient typedClient);  // for TryConnectAsync health check

    // Events
    public event EventHandler<PushEventArgs> EventReceived;
    public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    public event EventHandler<RealtimeModeChangedEventArgs> RealtimeModeChanged;
    public event EventHandler<RateLimitChangedEventArgs> RateLimitChanged;

    // Properties
    public bool IsConnected { get; }
    public bool IsRealtimeMode { get; }
    public int PollingIntervalSeconds { get; set; }

    // Methods
    public Task ConnectWebSocketAsync();
    public Task DisconnectWebSocketAsync();
    public void Disconnect();
    public async Task<bool> TryConnectAsync();  // calls typedClient.CheckHealthAsync()
    public void SetConnectionStatus(bool connected, string message);
    public void Dispose();
}
```

### RateLimitChangedEventArgs (New Class)

**Location:** `OE2EmpireTracker/Client/RateLimitChangedEventArgs.cs`

```csharp
public class RateLimitChangedEventArgs : EventArgs
{
    public int RequestsPerMinute { get; set; }
}
```


### SyncManager (Modified)

**Location:** `OE2EmpireTracker/Client/SyncManager.cs`

Constructor changes from `RemoteFactionClient` to `IFactionServerTypedClient` + `FactionPushClient`:

```csharp
public class SyncManager
{
    private readonly IFactionServerTypedClient _typedClient;
    private readonly FactionPushClient _pushClient;
    private readonly OfflineQueue _offlineQueue;

    public SyncManager(
        IFactionServerTypedClient typedClient,
        FactionPushClient pushClient,
        OfflineQueue offlineQueue);

    public bool IsOnline => _pushClient?.IsConnected ?? false;

    // WriteToServerAsync changes:
    // - Accepts PlayerRoot instead of string json
    // - Calls _typedClient.BulkImportAsync(characterUUID, playerRoot)
    // - Catches FactionValidationException → raises SyncValidationFailed
    // - Catches FactionAuthorizationException → sets unauthorized status
    // - Catches FactionConnectionException → queues offline

    // PullFullSyncAsync changes:
    // - Calls _typedClient.GetSyncSnapshotAsync() → typed SyncResponse
    // - Reads .ServerTimestamp and .ProcessingActive directly (no JObject parsing)

    // HandleReconnectionAsync changes:
    // - Calls _pushClient.TryConnectAsync() instead of _client.TryConnectAsync()

    // FlushOfflineQueueAsync changes:
    // - Reads QueuedChange.TypedPayload (PlayerRoot)
    // - Calls _typedClient.BulkImportAsync(characterUUID, typedPayload)
    // - Catches typed exceptions instead of inspecting HttpResponseMessage.StatusCode
}
```

### ServerContext (Modified)

**Location:** `OE2EmpireTracker/Client/ServerContext.cs`

```csharp
public class ServerContext : IDisposable
{
    // Properties change:
    // REMOVE: public RemoteFactionClient Client { get; }
    // ADD:    public IFactionServerTypedClient TypedClient { get; }
    // ADD:    public FactionPushClient PushClient { get; }

    // Initialize() changes:
    // 1. Create FactionServerTypedClient (serverUrl, token, thumbprint)
    // 2. Create FactionPushClient (serverUrl, token copy, thumbprint, typedClient)
    // 3. Subscribe to PushClient.RateLimitChanged → TypedClient.ApplyRateLimit()
    // 4. Create SyncManager(typedClient, pushClient, offlineQueue)

    // Dispose changes:
    // - Dispose both TypedClient and PushClient
    // - Unsubscribe from RateLimitChanged before disposing
    // - Guard against ApplyRateLimit during disposal
}
```

### FormPreferences (Modified)

**Location:** `OE2EmpireTracker/Forms/FormPreferences/FormPreferences.cs`

Changes:
- Test Connection: creates a temporary `FactionServerTypedClient`, calls `CheckHealthAsync()`
- Push to Server: creates a temporary `FactionServerTypedClient`, calls `BulkImportAsync()` with typed `PlayerRoot`
- Baseline upload: calls `UploadBaselineAsync(baselineRoot)` with typed `BaselineRoot`
- Character registration: calls `CreateCharacterAsync(name, uuid)`
- Error handling: catches `FactionValidationException` and displays errors to user

### QueuedChange (Modified)

**Location:** `OE2EmpireTracker/Client/QueuedChange.cs`

```csharp
public class QueuedChange
{
    public string CharacterUUID { get; set; }
    public string DataType { get; set; }

    // KEEP for backward compatibility (disk persistence)
    public string Json { get; set; }

    // ADD: typed payload (populated on queue, or deserialized on load)
    [JsonIgnore]
    public PlayerRoot TypedPayload { get; set; }

    public DateTime QueuedUtc { get; set; }
}
```

### OfflineQueue (Modified)

**Location:** `OE2EmpireTracker/Client/OfflineQueue.cs`

Changes to `Load()`:
- After deserializing `List<QueuedChange>` from disk, iterate and attempt to deserialize each item's `Json` into a `PlayerRoot` object, storing it in `TypedPayload`
- If deserialization fails for an individual entry, skip it with a warning log and continue loading valid entries

Changes to `Enqueue()` (via SyncManager):
- SyncManager serializes `PlayerRoot` to JSON before calling `Enqueue()`, so both `Json` and `TypedPayload` are populated


## Data Models

### RateLimitChangedEventArgs (New)

```csharp
namespace OE2EmpireTracker.Client
{
    public class RateLimitChangedEventArgs : EventArgs
    {
        public int RequestsPerMinute { get; set; }
    }
}
```

### QueuedChange (Modified)

Adds a `[JsonIgnore]` typed payload that is populated at runtime but not persisted directly (the `Json` field is the persistence format).

### SyncResponse (Already exists in Common)

Already returns strongly-typed `ServerTimestamp` (DateTime) — replaces the manual `ExtractTimestamp(string)` parsing in SyncManager.

Note: `SyncResponse` does not currently have a `ProcessingActive` property. We need to add one:

```csharp
// Added to OE2EmpireTracker.Common/Client/FactionServer/DTOs/SyncResponse.cs
[JsonProperty("processingActive")]
public bool ProcessingActive { get; set; }
```

## Error Handling

### Exception-Based Error Flow (Replaces HTTP Status Code Inspection)

The typed client throws typed exceptions. SyncManager catches them:

| Typed Client Throws | SyncManager Action |
|---|---|
| `FactionValidationException` | Extract `Errors` list, raise `SyncValidationFailed` event |
| `FactionAuthorizationException` | Set connection status to unauthorized via push client |
| `FactionConnectionException` | Queue change offline |
| `FactionServerException` (other) | Log warning, queue change offline |

### FormPreferences Error Handling

| Typed Client Throws | FormPreferences Action |
|---|---|
| `FactionValidationException` | Display validation errors in a message box |
| `FactionConnectionException` | Display "Connection failed" message |
| `FactionAuthorizationException` | Display "Unauthorized" message |

### FactionPushClient Error Handling

- WebSocket connection failure: triggers reconnection loop (same as RemoteFactionClient)
- WebSocket disconnect: sets IsConnected=false, raises ConnectionStatusChanged, starts reconnect
- All WebSocket errors are caught and logged (never crash the app)

## Testing Strategy

### Unit Tests (Example-Based)

- SyncManager: test each exception path (validation, authorization, connection, generic) produces the correct behavior
- SyncManager: test offline queue flush with typed payloads
- ServerContext: test RateLimitChanged event wiring
- OfflineQueue: test backward-compatible load (legacy JSON-only entries)
- QueuedChange: test TypedPayload population from Json field

### Integration Tests

- End-to-end write-through with mock typed client
- End-to-end reconnection flow with mock push client
- FormPreferences push-to-server with mock typed client

### Behavioral Equivalence Verification

The test file `DesktopSyncMigrationTests.cs` already validates sync scenarios. These tests will be updated to:
1. Construct `FactionServerTypedClient` mock (or real instance) instead of `RemoteFactionClient`
2. Construct `FactionPushClient` (or mock) for connection status
3. Verify same outcomes: write-through success, offline queuing, divergence detection, validation failure handling, forbidden response


## Migration Order

The migration is ordered to minimize risk and maintain a compilable codebase at each step:

### Phase 1: Foundation (No consumer changes)
1. **Add `RateLimitChangedEventArgs`** — new file, no dependencies
2. **Add `ProcessingActive` to `SyncResponse`** — additive change to Common
3. **Create `FactionPushClient`** — extracted from RemoteFactionClient, compiles independently

### Phase 2: Core Consumer Migration
4. **Migrate SyncManager** — update constructor and all method bodies to use typed client + push client
5. **Migrate QueuedChange** — add `TypedPayload` property
6. **Migrate OfflineQueue.Load()** — add typed payload deserialization on load

### Phase 3: Wiring
7. **Migrate ServerContext** — wire both clients, rate limit coordination, disposal
8. **Migrate FormPreferences** — use temporary typed clients for test/push operations

### Phase 4: Cleanup
9. **Update DesktopSyncMigrationTests** — use new constructors and mocks
10. **Delete RemoteFactionClient.cs** — remove the file, verify zero references
11. **Final build verification** — zero errors, zero warnings

### Risk Mitigation

- Each phase produces a compilable codebase (with RemoteFactionClient still present until Phase 4)
- SyncManager migration is the highest-risk change (most logic). It should be done carefully with typed exception handling replacing all HTTP status code checks.
- The push client is a direct extraction — behavior is copied verbatim from RemoteFactionClient's private methods with no logic changes.
- FormPreferences is low-risk: isolated temporary client instances, no shared state.

## Design Decisions

### D1: FactionPushClient receives IFactionServerTypedClient in constructor

The push client needs to call `CheckHealthAsync()` for its `TryConnectAsync()` method and for polling fallback. Rather than duplicating HTTP logic, it accepts the typed client as a dependency.

### D2: QueuedChange keeps both Json and TypedPayload

Backward compatibility requires that existing disk-persisted queue files (which contain `Json` strings) still load correctly. The `TypedPayload` is `[JsonIgnore]` and populated at runtime from the `Json` field during `Load()`.

### D3: SyncManager.WriteToServerAsync signature changes

The new signature accepts `PlayerRoot` directly instead of a raw JSON string. The caller (PlayerContext.WriteContext) already has a `PlayerRoot` object — it currently serializes to JSON just for the legacy client. The serialization step moves into the offline queue (for disk persistence) rather than happening before the SyncManager call.

### D4: SecureString token is cloned for FactionPushClient

ServerContext creates the token once, but both clients need it. The typed client gets the original; the push client gets a `Copy()` so each can dispose independently.

### D5: Rate limit coordination via events (not shared state)

The push client raises `RateLimitChanged`. ServerContext listens and calls `ApplyRateLimit()` on the typed client. This keeps the two clients decoupled — neither knows about the other directly.


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: OfflineQueue PlayerRoot Round-Trip

*For any* valid `PlayerRoot` object, serializing it to JSON (for disk persistence in QueuedChange.Json), then deserializing it back into a `PlayerRoot` (during OfflineQueue.Load()), SHALL produce an object equivalent to the original.

**Validates: Requirements 5.2, 5.3**

### Property 2: Backward-Compatible Queue Loading

*For any* valid legacy `QueuedChange` JSON file (containing `CharacterUUID`, `DataType`, `Json`, and `QueuedUtc` fields where `Json` is a serialized PlayerRoot), loading via `OfflineQueue.Load()` SHALL produce a `QueuedChange` with a non-null `TypedPayload` equivalent to deserializing the `Json` field directly.

**Validates: Requirements 5.5**
