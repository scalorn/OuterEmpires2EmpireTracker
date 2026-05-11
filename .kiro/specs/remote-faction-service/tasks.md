# Tasks — Remote Faction Service

## Phase 1: Project Scaffolding and Health Endpoint

- [x] 1.1 Create OE2EmpireTracker.Server .NET 8 project
  - [x] 1.1.1 Create the project directory and .csproj (ASP.NET Core Minimal API, net8.0)
  - [x] 1.1.2 Add project to OE2EmpireTracker.sln
  - [x] 1.1.3 Add NuGet packages: Microsoft.AspNetCore.App (implicit), Serilog, Newtonsoft.Json
  - [x] 1.1.4 Create Program.cs with minimal API host builder, HTTPS/Kestrel config, graceful shutdown
  - [x] 1.1.5 Create appsettings.json with default configuration (port 5443, JsonFile backend, data path)
  - [x] 1.1.6 Implement GET /health endpoint returning { status, serverVersion }
  - [x] 1.1.7 Verify the server starts, responds to /health, and shuts down on Ctrl+C

- [x] 1.2 Create OE2EmpireTracker.Server.Tests project
  - [x] 1.2.1 Create test project (net8.0, NUnit, Microsoft.AspNetCore.Mvc.Testing)
  - [x] 1.2.2 Add WebApplicationFactory-based integration test for /health endpoint
  - [x] 1.2.3 Verify tests pass

- [x] 1.3 TLS / Certificate support
  - [x] 1.3.1 Implement CertificateManager — generate self-signed cert on first run, persist to configured path
  - [x] 1.3.2 Support custom PFX/PEM certificate via configuration
  - [x] 1.3.3 Log certificate thumbprint on startup
  - [x] 1.3.4 Support X-Forwarded-For/Proto headers for reverse proxy mode

## Phase 2: Storage Abstraction and JSON File Backend

- [x] 2.1 Define storage interfaces
  - [x] 2.1.1 Create IStorageBackend interface with all methods from design Section 3.1
  - [x] 2.1.2 Create server-side model classes: ApiToken, TokenRole, RateLimitConfig, MembershipAction, SharingRule, CharacterPreferences, EntityMetadata, ServerFaction

- [x] 2.2 Implement JsonFileStorageBackend
  - [x] 2.2.1 Faction CRUD (factions.json — atomic write via temp+rename)
  - [x] 2.2.2 Character CRUD (characters.json)
  - [x] 2.2.3 Token storage (tokens.json)
  - [x] 2.2.4 Character data storage (characters/{uuid}/*.json per data type)
  - [x] 2.2.5 Global data storage (global/*.json)
  - [x] 2.2.6 Membership actions storage
  - [x] 2.2.7 Sharing rules storage
  - [x] 2.2.8 Character preferences storage
  - [x] 2.2.9 Full snapshot for sync endpoint

- [x] 2.3 Storage backend registration and validation
  - [x] 2.3.1 Register backend via DI based on appsettings.json config
  - [x] 2.3.2 Validate storage on startup (create data directory if missing, test write)
  - [x] 2.3.3 Exit with clear error if backend is unreachable

## Phase 3: Token Authentication

- [x] 3.1 Token generation and storage
  - [x] 3.1.1 Generate Owner token on first startup, display in console, persist hash
  - [x] 3.1.2 Implement token hashing (SHA-256) and lookup
  - [x] 3.1.3 Implement --regenerate-owner-token CLI command

- [x] 3.2 Authentication middleware
  - [x] 3.2.1 Create TokenAuthHandler — extract Bearer token, hash, lookup, attach identity to HttpContext
  - [x] 3.2.2 Return 401 for missing/invalid tokens
  - [x] 3.2.3 Attach TokenRole and CharacterUUID to claims

- [x] 3.3 Token management endpoints (Owner only)
  - [x] 3.3.1 POST /api/v1/tokens — create character + token in one step
  - [x] 3.3.2 GET /api/v1/tokens — list all tokens (no secret values)
  - [x] 3.3.3 DELETE /api/v1/tokens/{id} — revoke token
  - [x] 3.3.4 POST /api/v1/tokens/{id}/regenerate — invalidate old, return new

- [x] 3.4 Role-based authorization
  - [x] 3.4.1 Implement authorization policies: Owner, FactionLeader, Character
  - [x] 3.4.2 Owner can do everything
  - [x] 3.4.3 Character can read all, write own data only
  - [x] 3.4.4 FactionLeader can manage their faction's membership

## Phase 4: Faction and Character CRUD Endpoints

- [x] 4.1 Faction endpoints
  - [x] 4.1.1 POST /api/v1/factions — create with deterministic UUID
  - [x] 4.1.2 GET /api/v1/factions — list all
  - [x] 4.1.3 GET /api/v1/factions/{uuid} — get one or 404
  - [x] 4.1.4 PUT /api/v1/factions/{uuid} — update name/description
  - [x] 4.1.5 DELETE /api/v1/factions/{uuid} — remove, clear linked characters
  - [x] 4.1.6 409 Conflict on duplicate name

- [x] 4.2 Character endpoints
  - [x] 4.2.1 POST /api/v1/characters — create with deterministic UUID
  - [x] 4.2.2 GET /api/v1/characters — list all
  - [x] 4.2.3 GET /api/v1/characters/{uuid} — get one or 404
  - [x] 4.2.4 PUT /api/v1/characters/{uuid} — update (Character: own only, Owner: any)
  - [x] 4.2.5 DELETE /api/v1/characters/{uuid} — remove (Owner only)
  - [x] 4.2.6 409 Conflict on duplicate name, 400 on invalid FactionUUID

- [x] 4.3 Faction leadership endpoints
  - [x] 4.3.1 PUT /api/v1/factions/{uuid}/leaders — add co-leader
  - [x] 4.3.2 DELETE /api/v1/factions/{uuid}/leaders/{charUUID} — remove co-leader
  - [x] 4.3.3 GET /api/v1/factions/{uuid}/leaders — list leaders

## Phase 5: Faction Membership (Mutual Consent)

- [x] 5.1 Join requests
  - [x] 5.1.1 POST /api/v1/factions/{uuid}/requests — character requests to join
  - [x] 5.1.2 GET /api/v1/factions/{uuid}/requests — list pending requests (Leader/Owner)
  - [x] 5.1.3 POST /api/v1/factions/{uuid}/requests/{id}/accept — leader accepts, sets FactionUUID

- [x] 5.2 Invitations
  - [x] 5.2.1 POST /api/v1/factions/{uuid}/invitations — leader invites character
  - [x] 5.2.2 GET /api/v1/factions/{uuid}/invitations — list pending invitations
  - [x] 5.2.3 POST /api/v1/factions/{uuid}/invitations/{id}/accept — character accepts

- [x] 5.3 Leave faction
  - [x] 5.3.1 DELETE /api/v1/characters/{uuid}/faction — character leaves
  - [x] 5.3.2 Expiry cleanup — background task removes expired actions

## Phase 6: Central Data Storage Endpoints

- [x] 6.1 Character data CRUD
  - [x] 6.1.1 GET /api/v1/characters/{uuid}/data/{dataType} — get collection
  - [x] 6.1.2 POST /api/v1/characters/{uuid}/data/{dataType} — create entity
  - [x] 6.1.3 GET /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — get entity
  - [x] 6.1.4 PUT /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — update entity
  - [x] 6.1.5 DELETE /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — delete entity
  - [x] 6.1.6 GET /api/v1/characters/{uuid}/data — bulk get all types
  - [x] 6.1.7 PUT /api/v1/characters/{uuid}/data — bulk upload all types

- [x] 6.2 Global/baseline data
  - [x] 6.2.1 GET /api/v1/global/{dataType} — read (any authenticated user)
  - [x] 6.2.2 PUT /api/v1/global/{dataType} — write (Owner only)

- [x] 6.3 Sharing controls
  - [x] 6.3.1 GET /api/v1/characters/{uuid}/sharing — get sharing config
  - [x] 6.3.2 PUT /api/v1/characters/{uuid}/sharing — update sharing config
  - [x] 6.3.3 GET /api/v1/factions/{uuid}/shared/{dataType} — view shared data (faction members)
  - [x] 6.3.4 GET /api/v1/characters/{uuid}/shared-with-me/{dataType} — view data shared directly
  - [x] 6.3.5 Server-side filtering of cross-references the viewer cannot access

- [x] 6.4 Sync and export
  - [x] 6.4.1 GET /api/v1/sync — full snapshot with server timestamp
  - [x] 6.4.2 GET /api/v1/characters/{uuid}/export — export in PlayerData.json format

## Phase 7: WebSocket Real-Time Push

- [x] 7.1 WebSocket infrastructure
  - [x] 7.1.1 WebSocket endpoint at /ws with Bearer token auth
  - [x] 7.1.2 Connection management (track connected clients, heartbeat, timeout)
  - [x] 7.1.3 Reconnection support with exponential backoff (client-side)

- [x] 7.2 Event dispatch
  - [x] 7.2.1 Push events on data mutations (Created, Updated, Deleted)
  - [x] 7.2.2 Push events on membership changes (MembershipChanged, InvitationReceived, RequestReceived)
  - [x] 7.2.3 Filter events to only data the client has access to
  - [x] 7.2.4 TimerTick events when server-side processing runs

## Phase 8: Rate Limiting

- [x] 8.1 Rate limit enforcement
  - [x] 8.1.1 Per-token request counting (sliding window, default 60/min)
  - [x] 8.1.2 Return 429 with Retry-After header when exceeded
  - [x] 8.1.3 Owner token exempt from limits

- [x] 8.2 Rate limit management
  - [x] 8.2.1 PUT /api/v1/tokens/{id}/limits — Owner sets limits
  - [x] 8.2.2 GET /api/v1/tokens/{id}/limits — read current limits
  - [x] 8.2.3 Push rate limit changes to client via WebSocket

## Phase 9: Server-Side Background Processing

- [x] 9.1 Processing engine
  - [x] 9.1.1 ServerBackgroundProcessor — wraps Common's processing logic
  - [x] 9.1.2 Only processes opted-in characters
  - [x] 9.1.3 Configurable tick interval (default 60s)
  - [x] 9.1.4 Enable/disable via config and runtime API

- [x] 9.2 Admin endpoints
  - [x] 9.2.1 GET /api/v1/status — processing state, last tick, colonies processed
  - [x] 9.2.2 PUT /api/v1/admin/processing — enable/disable at runtime (Owner only)

- [x] 9.3 Character opt-in
  - [x] 9.3.1 PUT /api/v1/characters/{uuid}/preferences — set serverProcessing flag
  - [x] 9.3.2 GET /api/v1/characters/{uuid}/preferences — read preferences

## Phase 10: Build Verification

- [x] 10.1 Full test suite passes
- [x] 10.2 Server starts and responds to /health
- [x] 10.3 Integration tests cover auth, CRUD, membership, data storage
- [x] 10.4 Commit and push branch
