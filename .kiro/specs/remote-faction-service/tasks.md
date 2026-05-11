# Tasks — Remote Faction Service

## Phase 1: Project Scaffolding and Health Endpoint

- [ ] 1.1 Create OE2EmpireTracker.Server .NET 8 project
  - [x] 1.1.1 Create the project directory and .csproj (ASP.NET Core Minimal API, net8.0)
  - [ ] 1.1.2 Add project to OE2EmpireTracker.sln
  - [ ] 1.1.3 Add NuGet packages: Microsoft.AspNetCore.App (implicit), Serilog, Newtonsoft.Json
  - [ ] 1.1.4 Create Program.cs with minimal API host builder, HTTPS/Kestrel config, graceful shutdown
  - [ ] 1.1.5 Create appsettings.json with default configuration (port 5443, JsonFile backend, data path)
  - [ ] 1.1.6 Implement GET /health endpoint returning { status, serverVersion }
  - [ ] 1.1.7 Verify the server starts, responds to /health, and shuts down on Ctrl+C

- [ ] 1.2 Create OE2EmpireTracker.Server.Tests project
  - [ ] 1.2.1 Create test project (net8.0, NUnit, Microsoft.AspNetCore.Mvc.Testing)
  - [ ] 1.2.2 Add WebApplicationFactory-based integration test for /health endpoint
  - [ ] 1.2.3 Verify tests pass

- [ ] 1.3 TLS / Certificate support
  - [x] 1.3.1 Implement CertificateManager — generate self-signed cert on first run, persist to configured path
  - [ ] 1.3.2 Support custom PFX/PEM certificate via configuration
  - [ ] 1.3.3 Log certificate thumbprint on startup
  - [ ] 1.3.4 Support X-Forwarded-For/Proto headers for reverse proxy mode

## Phase 2: Storage Abstraction and JSON File Backend

- [ ] 2.1 Define storage interfaces
  - [x] 2.1.1 Create IStorageBackend interface with all methods from design Section 3.1
  - [ ] 2.1.2 Create server-side model classes: ApiToken, TokenRole, RateLimitConfig, MembershipAction, SharingRule, CharacterPreferences, EntityMetadata, ServerFaction

- [ ] 2.2 Implement JsonFileStorageBackend
  - [ ] 2.2.1 Faction CRUD (factions.json — atomic write via temp+rename)
  - [ ] 2.2.2 Character CRUD (characters.json)
  - [ ] 2.2.3 Token storage (tokens.json)
  - [ ] 2.2.4 Character data storage (characters/{uuid}/*.json per data type)
  - [ ] 2.2.5 Global data storage (global/*.json)
  - [ ] 2.2.6 Membership actions storage
  - [ ] 2.2.7 Sharing rules storage
  - [ ] 2.2.8 Character preferences storage
  - [ ] 2.2.9 Full snapshot for sync endpoint

- [ ] 2.3 Storage backend registration and validation
  - [ ] 2.3.1 Register backend via DI based on appsettings.json config
  - [ ] 2.3.2 Validate storage on startup (create data directory if missing, test write)
  - [ ] 2.3.3 Exit with clear error if backend is unreachable

## Phase 3: Token Authentication

- [ ] 3.1 Token generation and storage
  - [-] 3.1.1 Generate Owner token on first startup, display in console, persist hash
  - [ ] 3.1.2 Implement token hashing (SHA-256) and lookup
  - [ ] 3.1.3 Implement --regenerate-owner-token CLI command

- [ ] 3.2 Authentication middleware
  - [ ] 3.2.1 Create TokenAuthHandler — extract Bearer token, hash, lookup, attach identity to HttpContext
  - [ ] 3.2.2 Return 401 for missing/invalid tokens
  - [ ] 3.2.3 Attach TokenRole and CharacterUUID to claims

- [ ] 3.3 Token management endpoints (Owner only)
  - [ ] 3.3.1 POST /api/v1/tokens — create character + token in one step
  - [ ] 3.3.2 GET /api/v1/tokens — list all tokens (no secret values)
  - [ ] 3.3.3 DELETE /api/v1/tokens/{id} — revoke token
  - [ ] 3.3.4 POST /api/v1/tokens/{id}/regenerate — invalidate old, return new

- [ ] 3.4 Role-based authorization
  - [ ] 3.4.1 Implement authorization policies: Owner, FactionLeader, Character
  - [ ] 3.4.2 Owner can do everything
  - [ ] 3.4.3 Character can read all, write own data only
  - [ ] 3.4.4 FactionLeader can manage their faction's membership

## Phase 4: Faction and Character CRUD Endpoints

- [ ] 4.1 Faction endpoints
  - [ ] 4.1.1 POST /api/v1/factions — create with deterministic UUID
  - [ ] 4.1.2 GET /api/v1/factions — list all
  - [ ] 4.1.3 GET /api/v1/factions/{uuid} — get one or 404
  - [ ] 4.1.4 PUT /api/v1/factions/{uuid} — update name/description
  - [ ] 4.1.5 DELETE /api/v1/factions/{uuid} — remove, clear linked characters
  - [ ] 4.1.6 409 Conflict on duplicate name

- [ ] 4.2 Character endpoints
  - [ ] 4.2.1 POST /api/v1/characters — create with deterministic UUID
  - [ ] 4.2.2 GET /api/v1/characters — list all
  - [ ] 4.2.3 GET /api/v1/characters/{uuid} — get one or 404
  - [ ] 4.2.4 PUT /api/v1/characters/{uuid} — update (Character: own only, Owner: any)
  - [ ] 4.2.5 DELETE /api/v1/characters/{uuid} — remove (Owner only)
  - [ ] 4.2.6 409 Conflict on duplicate name, 400 on invalid FactionUUID

- [ ] 4.3 Faction leadership endpoints
  - [ ] 4.3.1 PUT /api/v1/factions/{uuid}/leaders — add co-leader
  - [ ] 4.3.2 DELETE /api/v1/factions/{uuid}/leaders/{charUUID} — remove co-leader
  - [ ] 4.3.3 GET /api/v1/factions/{uuid}/leaders — list leaders

## Phase 5: Faction Membership (Mutual Consent)

- [ ] 5.1 Join requests
  - [ ] 5.1.1 POST /api/v1/factions/{uuid}/requests — character requests to join
  - [ ] 5.1.2 GET /api/v1/factions/{uuid}/requests — list pending requests (Leader/Owner)
  - [ ] 5.1.3 POST /api/v1/factions/{uuid}/requests/{id}/accept — leader accepts, sets FactionUUID

- [ ] 5.2 Invitations
  - [ ] 5.2.1 POST /api/v1/factions/{uuid}/invitations — leader invites character
  - [ ] 5.2.2 GET /api/v1/factions/{uuid}/invitations — list pending invitations
  - [ ] 5.2.3 POST /api/v1/factions/{uuid}/invitations/{id}/accept — character accepts

- [ ] 5.3 Leave faction
  - [ ] 5.3.1 DELETE /api/v1/characters/{uuid}/faction — character leaves
  - [ ] 5.3.2 Expiry cleanup — background task removes expired actions

## Phase 6: Central Data Storage Endpoints

- [ ] 6.1 Character data CRUD
  - [ ] 6.1.1 GET /api/v1/characters/{uuid}/data/{dataType} — get collection
  - [ ] 6.1.2 POST /api/v1/characters/{uuid}/data/{dataType} — create entity
  - [ ] 6.1.3 GET /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — get entity
  - [ ] 6.1.4 PUT /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — update entity
  - [ ] 6.1.5 DELETE /api/v1/characters/{uuid}/data/{dataType}/{entityUuid} — delete entity
  - [ ] 6.1.6 GET /api/v1/characters/{uuid}/data — bulk get all types
  - [ ] 6.1.7 PUT /api/v1/characters/{uuid}/data — bulk upload all types

- [ ] 6.2 Global/baseline data
  - [ ] 6.2.1 GET /api/v1/global/{dataType} — read (any authenticated user)
  - [ ] 6.2.2 PUT /api/v1/global/{dataType} — write (Owner only)

- [ ] 6.3 Sharing controls
  - [ ] 6.3.1 GET /api/v1/characters/{uuid}/sharing — get sharing config
  - [ ] 6.3.2 PUT /api/v1/characters/{uuid}/sharing — update sharing config
  - [ ] 6.3.3 GET /api/v1/factions/{uuid}/shared/{dataType} — view shared data (faction members)
  - [ ] 6.3.4 GET /api/v1/characters/{uuid}/shared-with-me/{dataType} — view data shared directly
  - [ ] 6.3.5 Server-side filtering of cross-references the viewer cannot access

- [ ] 6.4 Sync and export
  - [ ] 6.4.1 GET /api/v1/sync — full snapshot with server timestamp
  - [ ] 6.4.2 GET /api/v1/characters/{uuid}/export — export in PlayerData.json format

## Phase 7: WebSocket Real-Time Push

- [ ] 7.1 WebSocket infrastructure
  - [ ] 7.1.1 WebSocket endpoint at /ws with Bearer token auth
  - [ ] 7.1.2 Connection management (track connected clients, heartbeat, timeout)
  - [ ] 7.1.3 Reconnection support with exponential backoff (client-side)

- [ ] 7.2 Event dispatch
  - [ ] 7.2.1 Push events on data mutations (Created, Updated, Deleted)
  - [ ] 7.2.2 Push events on membership changes (MembershipChanged, InvitationReceived, RequestReceived)
  - [ ] 7.2.3 Filter events to only data the client has access to
  - [ ] 7.2.4 TimerTick events when server-side processing runs

## Phase 8: Rate Limiting

- [ ] 8.1 Rate limit enforcement
  - [ ] 8.1.1 Per-token request counting (sliding window, default 60/min)
  - [ ] 8.1.2 Return 429 with Retry-After header when exceeded
  - [ ] 8.1.3 Owner token exempt from limits

- [ ] 8.2 Rate limit management
  - [ ] 8.2.1 PUT /api/v1/tokens/{id}/limits — Owner sets limits
  - [ ] 8.2.2 GET /api/v1/tokens/{id}/limits — read current limits
  - [ ] 8.2.3 Push rate limit changes to client via WebSocket

## Phase 9: Server-Side Background Processing

- [ ] 9.1 Processing engine
  - [ ] 9.1.1 ServerBackgroundProcessor — wraps Common's processing logic
  - [ ] 9.1.2 Only processes opted-in characters
  - [ ] 9.1.3 Configurable tick interval (default 60s)
  - [ ] 9.1.4 Enable/disable via config and runtime API

- [ ] 9.2 Admin endpoints
  - [ ] 9.2.1 GET /api/v1/status — processing state, last tick, colonies processed
  - [ ] 9.2.2 PUT /api/v1/admin/processing — enable/disable at runtime (Owner only)

- [ ] 9.3 Character opt-in
  - [ ] 9.3.1 PUT /api/v1/characters/{uuid}/preferences — set serverProcessing flag
  - [ ] 9.3.2 GET /api/v1/characters/{uuid}/preferences — read preferences

## Phase 10: Build Verification

- [ ] 10.1 Full test suite passes
- [ ] 10.2 Server starts and responds to /health
- [ ] 10.3 Integration tests cover auth, CRUD, membership, data storage
- [ ] 10.4 Commit and push branch
