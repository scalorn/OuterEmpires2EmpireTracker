# Tasks — Remote Faction Service

> **Traceability:** Every task references the requirement(s) and acceptance criteria it satisfies.
> Server phases (1–10) are COMPLETE. Client phases (11–15) are NOT yet implemented.

---

## Phase 1: Project Scaffolding and Health Endpoint (COMPLETE)

- [x] 1.1 Create OE2EmpireTracker.Server .NET 8 project with Minimal API, add to solution
  - _Satisfies: Req 1 (CLI execution), Req 20 (solution split — Server project)_
- [x] 1.2 Implement GET /health returning { status, serverVersion }
  - _Satisfies: Req 8 ("GET /health SHALL return 200 with status and version")_
- [x] 1.3 HTTPS/Kestrel config, graceful shutdown on SIGINT/SIGTERM
  - _Satisfies: Req 1 ("exit cleanly on SIGINT/SIGTERM"), Req 2 ("listen on HTTPS only")_
- [x] 1.4 TLS certificate management — self-signed generation, custom PFX/PEM, thumbprint logging
  - _Satisfies: Req 2 (all TLS criteria)_
- [x] 1.5 Reverse proxy support (X-Forwarded-For/Proto)
  - _Satisfies: Req 2 ("support running behind a reverse proxy")_
- [x] 1.6 Create OE2EmpireTracker.Server.Tests with WebApplicationFactory integration test
  - _Satisfies: Req 8 (verifiable health endpoint)_

## Phase 2: Storage Abstraction and JSON File Backend (COMPLETE)

- [x] 2.1 Define IStorageBackend interface with all methods from design Section 3.1
  - _Satisfies: Req 5 ("pluggable persistence backends via storage abstraction layer")_
- [x] 2.2 Implement JsonFileStorageBackend (atomic writes, per-character directories)
  - _Satisfies: Req 5 ("JSON files: single file per character, atomic writes via temp+rename")_
- [x] 2.3 Storage backend DI registration, startup validation, clear error on failure
  - _Satisfies: Req 5 ("validate storage on startup and exit with clear error"), Req 9 (configuration)_

## Phase 3: Token Authentication (COMPLETE)

- [x] 3.1 Owner token generation on first startup, SHA-256 hashing, --regenerate-owner-token CLI
  - _Satisfies: Req 13 ("generate Owner token on first startup", "regenerable via CLI command")_
- [x] 3.2 TokenAuthHandler middleware — Bearer extraction, hash lookup, 401 on invalid
  - _Satisfies: Req 13 ("authenticate all requests via Bearer token", "401 Unauthorized")_
- [x] 3.3 Token management endpoints (POST/GET/DELETE /api/v1/tokens, regenerate)
  - _Satisfies: Req 13 (Owner role: create, list, revoke, regenerate tokens)_
- [x] 3.4 Role-based authorization (Owner, FactionLeader, Character)
  - _Satisfies: Req 13 (all role permission criteria)_

## Phase 4: Faction and Character CRUD Endpoints (COMPLETE)

- [x] 4.1 Faction CRUD (POST/GET/PUT/DELETE /api/v1/factions) with deterministic UUID
  - _Satisfies: Req 3 (all faction management criteria), Req 6 (deterministic UUID)_
- [x] 4.2 Character CRUD (POST/GET/PUT/DELETE /api/v1/characters) with deterministic UUID
  - _Satisfies: Req 4 (all character management criteria), Req 6 (deterministic UUID)_
- [x] 4.3 Faction leadership endpoints (add/remove/list co-leaders)
  - _Satisfies: Req 13 (Faction Leader: promote/demote co-leaders)_
- [x] 4.4 Conflict detection (409 on duplicate name, 400 on invalid FactionUUID)
  - _Satisfies: Req 3 ("409 Conflict on duplicate name"), Req 4 ("409 Conflict", "400 Bad Request")_

## Phase 5: Faction Membership — Mutual Consent (COMPLETE)

- [x] 5.1 Join requests (POST/GET/accept)
  - _Satisfies: Req 14 ("character SHALL request joining", "leader accepting sets FactionUUID")_
- [x] 5.2 Invitations (POST/GET/accept)
  - _Satisfies: Req 14 ("leader SHALL invite", "character accepting sets FactionUUID")_
- [x] 5.3 Leave faction, expiry cleanup
  - _Satisfies: Req 14 ("leave via DELETE", "expire after configurable period")_

## Phase 6: Central Data Storage Endpoints (COMPLETE)

- [x] 6.1 Character data CRUD (/api/v1/characters/{uuid}/data/{dataType})
  - _Satisfies: Req 15 (Data Access API: all CRUD criteria)_
- [x] 6.2 Bulk upload/download (GET/PUT /api/v1/characters/{uuid}/data)
  - _Satisfies: Req 15 ("bulk upload/download of all character data")_
- [x] 6.3 Global/baseline data (GET/PUT /api/v1/global/{dataType})
  - _Satisfies: Req 15 ("global data readable by all", "writable only by Owner")_
- [x] 6.4 Sharing controls (GET/PUT sharing rules, faction shared data, shared-with-me)
  - _Satisfies: Req 15 (all Sharing Controls criteria)_
- [x] 6.5 Cross-reference filtering (server-side, no dangling references)
  - _Satisfies: Req 15 ("filter out referenced entities viewer cannot access")_
- [x] 6.6 Sync endpoint (GET /api/v1/sync) and export (GET /api/v1/characters/{uuid}/export)
  - _Satisfies: Req 12 ("complete dataset in single response"), Req 16 ("export in PlayerData.json format")_

## Phase 7: WebSocket Real-Time Push — Server Side (COMPLETE)

- [x] 7.1 WebSocket endpoint at /ws with Bearer token auth
  - _Satisfies: Req 18 ("expose WebSocket endpoint at /ws", "same HTTPS/TLS transport")_
- [x] 7.2 Connection management (heartbeat, timeout, registry)
  - _Satisfies: Req 18 ("persistent connection per client", "disconnect after missed heartbeats")_
- [x] 7.3 Event dispatch (Created, Updated, Deleted, TimerTick, MembershipChanged, etc.)
  - _Satisfies: Req 18 (all Server-to-Client Push Events criteria)_
- [x] 7.4 Event filtering (only data client is authorized to see)
  - _Satisfies: Req 18 ("client SHALL only receive events for data they are authorized to see")_

## Phase 8: Rate Limiting (COMPLETE)

- [x] 8.1 Per-token sliding window rate limiting, 429 with Retry-After
  - _Satisfies: Req 19 ("return 429 Too Many Requests", "Retry-After header")_
- [x] 8.2 Owner exempt, rate limit management endpoints
  - _Satisfies: Req 19 ("Owner token exempt", "configure rate limits per token")_
- [x] 8.3 Push rate limit changes to client via WebSocket
  - _Satisfies: Req 19 ("push notification when rate limits changed")_

## Phase 9: Server-Side Background Processing (COMPLETE)

- [x] 9.1 ServerBackgroundProcessor (wraps Common processing logic, configurable tick)
  - _Satisfies: Req 17 ("optional server-side background processor", "configurable tick interval")_
- [x] 9.2 Character opt-in/out via preferences endpoint
  - _Satisfies: Req 17 ("opt in/out via PUT preferences", "default opted OUT")_
- [x] 9.3 Admin endpoints (GET /api/v1/status, PUT /api/v1/admin/processing)
  - _Satisfies: Req 17 ("expose processing state via GET /api/status", "enable/disable at runtime")_

## Phase 10: Build Verification (COMPLETE)

- [x] 10.1 Full test suite passes, server starts and responds to /health
  - _Satisfies: Req 1, Req 8 (verifiable operation)_
- [x] 10.2 Integration tests cover auth, CRUD, membership, data storage
  - _Satisfies: All server-side requirements verified via tests_

---

## Phase 11: Client Infrastructure — Preferences and Connection

- [x] 11.1 Add server connection settings to PreferencesStore (ServerUrl, TrustedThumbprint, BearerToken reference, OperatingMode)
  - _Satisfies: Req 11 ("allow configuring the service URL and certificate thumbprint in preferences")_
  - _Satisfies: Req 16 ("operating mode SHALL be configurable in preferences")_
- [x] 11.2 Instantiate RemoteFactionClient on application startup using stored preferences
  - _Satisfies: Req 11 ("tracker client SHALL fall back to local data if service unreachable")_
  - _Satisfies: Design §9.2 (RemoteFactionClient lifecycle)_
- [x] 11.3 Instantiate SyncManager on application startup, wire to RemoteFactionClient and OfflineQueue
  - _Satisfies: Req 15 Sync/Offline ("tracker client SHALL sync all data on startup when connected")_
  - _Satisfies: Design §9.3 (SyncManager coordinates data flow)_
- [x] 11.4 Instantiate OfflineQueue on application startup, load persisted queue
  - _Satisfies: Req 15 Sync/Offline ("queue changes locally and sync on reconnection")_
  - _Satisfies: Design §9.4 (OfflineQueue persisted to local file)_
- [x] 11.5 Store bearer token securely via CredentialStore (DPAPI)
  - _Satisfies: Req 2 ("client SHALL allow user to trust a specific server certificate thumbprint")_
  - _Satisfies: Design §9.2 (SecureString bearer token)_

## Phase 12: Client UI — Server Preferences and Connection Status

- [~] 12.1 Add "Server" tab/section to Preferences UI with fields: Server URL, Certificate Thumbprint, Bearer Token, Operating Mode dropdown (Local Only / Server Only / Server + Local)
  - _Satisfies: Req 11 ("allow configuring the service URL and certificate thumbprint in preferences")_
  - _Satisfies: Req 16 ("operating mode SHALL be configurable in preferences and changeable at any time")_
- [~] 12.2 Add "Test Connection" button in preferences that calls /health and reports success/failure
  - _Satisfies: Req 11 (verifiable connectivity)_
  - _Satisfies: Req 2 ("client SHALL allow user to trust a specific server certificate thumbprint")_
- [~] 12.3 On first connect with untrusted cert, prompt user to trust the thumbprint
  - _Satisfies: Req 2 ("client SHALL allow user to trust a specific server certificate thumbprint")_
  - _Satisfies: Design §6.3 ("Server certificate thumbprint: ... Trust this server?")_
- [~] 12.4 Add connection status indicator to MainWindow (Connected/Disconnected/Connecting)
  - _Satisfies: Req 11 ("indicate connection status (connected/disconnected) in the UI")_
  - _Satisfies: Design §9.2 (ConnectionStatus enum, StatusChanged event)_
- [~] 12.5 Add real-time vs polling indicator in status bar
  - _Satisfies: Req 18 Fallback ("client SHALL indicate whether receiving real-time updates or polling")_
- [~] 12.6 When switching from Server Only to Local Only, prompt user to export data first
  - _Satisfies: Req 16 ("switching from Server Only to Local Only SHALL prompt user to export first")_

## Phase 13: Sync and Offline Behavior

- [~] 13.1 On startup (if connected): call SyncManager.SyncOnStartupAsync() to pull full sync from server
  - _Satisfies: Req 12 ("client SHALL use this endpoint on startup")_
  - _Satisfies: Req 15 Sync/Offline ("tracker client SHALL sync all data with service on startup")_
- [~] 13.2 Write-through: when data changes locally and mode is Server Only or DualWrite, push to server immediately via SyncManager
  - _Satisfies: Req 15 Sync/Offline ("write changes to service immediately when connected")_
  - _Satisfies: Design §9.3 ("On data change if connected: push to server immediately")_
- [~] 13.3 Offline queuing: when disconnected, queue mutations via OfflineQueue, persist to disk
  - _Satisfies: Req 15 Sync/Offline ("queue changes locally and sync on reconnection")_
  - _Satisfies: Design §9.4 (OfflineQueue persisted to offline-queue.json)_
- [~] 13.4 On reconnection: flush offline queue, detect divergence if server also changed
  - _Satisfies: Req 15 Sync/Offline ("on reconnection after offline changes, IF server data also changed, prompt user")_
  - _Satisfies: Design §9.3 (HandleReconnectionAsync)_
- [~] 13.5 Divergence resolution UI: show what diverged, let user choose upload-local or download-server
  - _Satisfies: Req 15 Sync/Offline ("client SHALL clearly show what has diverged before user makes choice")_
- [~] 13.6 Fallback to local data when server unreachable
  - _Satisfies: Req 11 ("fall back to local faction/character data if service is unreachable")_
- [~] 13.7 Disable local BackgroundProcessor when server-side processing is active for the character
  - _Satisfies: Req 17 ("client SHALL NOT run its own background processor to avoid double-processing")_
  - _Satisfies: Design §8.2 ("Client disables its local BackgroundProcessor")_

## Phase 14: WebSocket Client

- [~] 14.1 Implement WebSocket client in RemoteFactionClient (connect to wss://server/ws?token=...)
  - _Satisfies: Req 18 ("authenticate WebSocket using same Bearer token")_
  - _Satisfies: Design §7.1 (connection lifecycle)_
- [~] 14.2 Automatic reconnection with exponential backoff (1s, 2s, 4s, 8s, max 60s)
  - _Satisfies: Req 18 ("automatically reconnect on disconnection with exponential backoff")_
  - _Satisfies: Design §7.1 ("Client reconnects with exponential backoff")_
- [~] 14.3 Send heartbeat/ping every 30s to keep connection alive
  - _Satisfies: Req 18 ("client SHALL send heartbeat/ping to keep connection alive")_
  - _Satisfies: Design §7.1 ("Client sends ping every 30s")_
- [~] 14.4 Handle incoming push events (Created, Updated, Deleted, TimerTick, MembershipChanged, etc.)
  - _Satisfies: Req 18 ("on receiving a push event, client SHALL fetch updated data via REST API")_
  - _Satisfies: Design §7.2 (push event schema)_
- [~] 14.5 Fallback to periodic REST polling when WebSocket unavailable (configurable interval, default 60s)
  - _Satisfies: Req 18 Fallback ("IF WebSocket fails, client SHALL fall back to periodic polling")_
- [~] 14.6 Self-throttle based on server-communicated rate limits (from WebSocket handshake)
  - _Satisfies: Req 19 ("client SHALL respect communicated limits to self-throttle")_
  - _Satisfies: Req 19 ("service SHALL inform client of rate limits on WebSocket connection")_

## Phase 15: Data Portability and Operating Modes

- [~] 15.1 Implement dual-write mode: every server write also writes to local PlayerData.json
  - _Satisfies: Req 16 ("in dual-write mode, every change written to server SHALL also be written to local file")_
  - _Satisfies: Req 16 ("local file format SHALL remain identical to current PlayerData.json format")_
- [~] 15.2 Implement Server Only mode: all reads/writes go to server, no local file maintained
  - _Satisfies: Req 16 ("Server Only: all reads/writes go to server, no local file maintained")_
- [~] 15.3 Implement Local Only mode: no server connection (current behavior preserved)
  - _Satisfies: Req 16 ("Local Only: no server connection, reads/writes local JSON files")_
  - Note: This is the existing behavior — task is to ensure mode switching works correctly.
- [~] 15.4 "Export from Server" UI function — download all character data, save as local JSON
  - _Satisfies: Req 16 ("provide an Export from Server function")_
  - _Satisfies: Req 16 ("export SHALL produce file identical in format to current PlayerData.json")_
- [~] 15.5 Ensure disconnecting from server allows continued local operation with zero data loss
  - _Satisfies: Req 16 ("disconnect from server and continue using tracker with local file with zero data loss")_
- [~] 15.6 Use server as primary storage when connected (services read from server, not local file)
  - _Satisfies: Req 15 ("tracker client SHALL use service as primary storage when connected")_
- [~] 15.7 Prompt sharing preferences configuration when character first joins a faction
  - _Satisfies: Req 15 Sharing ("when character first joins faction, client SHALL prompt to configure sharing")_

---

## Traceability Matrix — Unmapped Client Requirements

The following acceptance criteria from requirements.md are covered by the tasks above:

| Requirement | Criteria Summary | Task(s) |
|-------------|-----------------|---------|
| Req 2 (client portion) | Trust specific thumbprint | 11.5, 12.2, 12.3 |
| Req 11 | Offline fallback, sync on reconnect, connection status, configure URL/thumbprint | 11.1–11.5, 12.1–12.4, 13.1, 13.6 |
| Req 12 (client portion) | Use sync endpoint on startup, periodically | 13.1 |
| Req 15 Sync/Offline | Sync on startup, write-through, queue offline, divergence prompt | 13.1–13.6 |
| Req 15 Sharing (client) | Prompt sharing config on faction join | 15.7 |
| Req 16 | Dual-write, export, operating modes, mode switching | 15.1–15.6, 12.6 |
| Req 17 (client portion) | Disable local processor when server processing active | 13.7 |
| Req 18 (client portion) | WebSocket client, reconnection, heartbeat, event handling, fallback polling, UI indicator | 14.1–14.6, 12.5 |
| Req 19 (client portion) | Self-throttle based on server-communicated limits | 14.6 |
