# OE2EmpireTracker.Server — Requirements

> This is the **server project** working spec. The authoritative umbrella spec is at
> `.kiro/specs/remote-faction-service/requirements.md`.
> Client-side requirements (Req 11, 16) are in the umbrella spec only.

## Req 1: Cross-Platform CLI Execution

**User Stories:** Faction leaders run the service on Linux VPS; developers run locally on Windows.

### Acceptance Criteria

- [ ] .NET 8+ console application with no GUI dependencies.
- [ ] Publishable as self-contained single-file executable for Windows (x64), Linux (x64), macOS (arm64).
- [ ] Starts by listening on configured HTTPS port; logs startup message.
- [ ] Exits cleanly on SIGINT/SIGTERM.

## Req 2: HTTPS / TLS Transport

**User Stories:** All communication encrypted; self-signed cert on first run; custom cert support.

### Acceptance Criteria

- [ ] HTTPS only (no plain HTTP).
- [ ] Generates self-signed TLS cert on first startup if none configured; persists to configurable path.
- [ ] Supports custom PFX/PEM certificate via configuration.
- [ ] Logs certificate thumbprint on startup.
- [ ] Supports reverse proxy (nginx, Caddy) via X-Forwarded-For/Proto headers when configured.

## Req 3: Faction Management API

**User Stories:** Create, rename, delete factions visible to all members.

### Acceptance Criteria

- [ ] POST /api/factions creates faction with deterministic UUID from name.
- [ ] Multiple independent factions per server; flat (no hierarchy).
- [ ] GET /api/factions returns all; GET /api/factions/{uuid} returns one or 404.
- [ ] PUT /api/factions/{uuid} updates Name and Description.
- [ ] DELETE /api/factions/{uuid} removes faction and clears FactionUUID on linked characters.
- [ ] Duplicate name returns 409 Conflict.

## Req 4: External Character Management API

**User Stories:** Register external characters; assign characters to factions.

### Acceptance Criteria

- [ ] POST /api/characters creates character with deterministic UUID from name.
- [ ] GET /api/characters returns all; GET /api/characters/{uuid} returns one or 404.
- [ ] PUT /api/characters/{uuid} updates Name and FactionUUID.
- [ ] DELETE /api/characters/{uuid} removes the character.
- [ ] Duplicate name returns 409 Conflict.
- [ ] Non-existent FactionUUID returns 400 Bad Request.

## Req 5: Data Persistence

**User Stories:** Data survives restarts; pluggable backends (simple VPS → managed AWS).

### Acceptance Criteria

- [ ] Pluggable persistence via storage abstraction layer.
- [ ] Backends: JSON files (default), SQLite, PostgreSQL, AWS DynamoDB.
- [ ] Active backend configurable via appsettings.json.
- [ ] JSON files: one file per character + global file; atomic writes (temp+rename); default dir `./data/`.
- [ ] SQLite: single .db file, no external deps.
- [ ] PostgreSQL: connection string in config; suitable for RDS.
- [ ] DynamoDB: table name + region in config; single-table design.
- [ ] All backends implement same interface (backend-agnostic service layer).
- [ ] Validates storage on startup; exits with clear error if unreachable.
- [ ] Storage format compatible with JSON export (Req 16).

## Req 6: Deterministic UUID Generation

**User Stories:** Same faction/character name → same UUID regardless of which client creates it.

### Acceptance Criteria

- [ ] Faction UUIDs use DeterministicUUID with faction namespace.
- [ ] Character UUIDs use DeterministicUUID with character namespace.
- [ ] Case-insensitive (normalizes to lowercase before hashing).

## Req 7: Conflict Resolution

**User Stories:** Edits not silently lost if someone else edited the same record.

### Acceptance Criteria

- [ ] Each entity has LastModified (UTC ISO 8601) updated on every mutation.
- [ ] Requests processed sequentially (no concurrent writes to same entity).
- [ ] Last-write-wins semantics.
- [ ] Responses include LastModified for client staleness detection.

## Req 8: REST API Design

**User Stories:** Predictable, standard API for integrators.

### Acceptance Criteria

- [ ] JSON request/response bodies; Content-Type: application/json.
- [ ] Success: 200 (GET/PUT), 201 (POST), 204 (DELETE).
- [ ] Errors: 400 (validation), 404 (not found), 409 (conflict), 401 (unauthorized).
- [ ] GET /health returns 200 with `{ status, version }`.
- [ ] All endpoints under /api prefix.

## Req 9: Service Configuration

**User Stories:** Configure port and data location without recompiling.

### Acceptance Criteria

- [ ] Config from appsettings.json, environment variables, CLI args (that precedence order).
- [ ] Configurable: HTTPS port (default 5443), data path, cert path, cert password, API keys.
- [ ] Validates config on startup; exits with clear error if invalid.

## Req 10: Logging and Diagnostics

**User Stories:** See what's happening without a debugger.

### Acceptance Criteria

- [ ] Logs to stdout; structured logging (timestamp, level, message).
- [ ] Levels: Information (startup/shutdown), Warning (validation failures), Error (unhandled exceptions).
- [ ] Each mutation logged with entity type, UUID, and client IP.

## Req 12: Bulk Data Retrieval (Sync)

**User Stories:** On app open, quickly get all current faction/character data.

### Acceptance Criteria

- [ ] GET /api/sync returns complete dataset (all factions + characters) in single response.
- [ ] Response includes server timestamp for cache validation.

## Req 13: Token-Based Permissions Model

**User Stories:** Owner has full control; characters get scoped access via tokens.

### Acceptance Criteria

- [ ] All requests authenticated via Bearer token in Authorization header.
- [ ] Unauthenticated requests return 401.
- [ ] Three roles: Owner, Faction Leader, Character.
- [ ] On first startup, generates Owner token and displays in console (one-time).
- [ ] Owner token persisted as SHA-256 hash.

#### Owner Role

- [ ] Full CRUD on factions and characters.
- [ ] POST /api/tokens creates character account + token in one step.
- [ ] GET /api/tokens lists all (name, role, created, last used — not token value).
- [ ] DELETE /api/tokens/{id} revokes; POST /api/tokens/{id}/regenerate issues new token.
- [ ] PUT /api/factions/{uuid}/leader designates faction leader.
- [ ] `--regenerate-owner-token` CLI command prints new token and exits.

#### Faction Leader Role

- [ ] Update own faction details (Name, Description).
- [ ] Invite characters; accept join requests; remove members.
- [ ] Promote/demote co-leaders (not self).
- [ ] Cannot create/delete factions or manage tokens.
- [ ] Retains all Character permissions.

#### Character Role

- [ ] Read all factions and characters.
- [ ] Update own character record only.
- [ ] Request to join faction; accept invitations; leave faction.
- [ ] Cannot modify other characters, factions, or tokens.
- [ ] Forbidden operations return 403.

#### Token Format

- [ ] 32 random bytes, base64url-encoded (43 chars).
- [ ] Stored as SHA-256 hash (never plaintext after generation).
- [ ] Lookup: hash incoming token, compare to stored hashes.

## Req 14: Faction Membership — Mutual Consent

**User Stories:** Characters request to join; leaders invite; membership requires both sides to agree.

### Acceptance Criteria

- [ ] POST /api/factions/{uuid}/requests — character requests to join.
- [ ] POST /api/factions/{uuid}/invitations — leader invites character.
- [ ] Membership granted only when both sides agree (request+acceptance OR invitation+acceptance).
- [ ] Leader accepting request sets FactionUUID and clears request.
- [ ] Character accepting invitation sets FactionUUID and clears invitation.
- [ ] GET endpoints for pending requests and invitations.
- [ ] Character already in a faction cannot request another (must leave first).
- [ ] DELETE /api/characters/{uuid}/faction — leave current faction.
- [ ] Requests/invitations expire after configurable period (default 7 days).
- [ ] Owner can bypass mutual consent (direct assignment).

## Req 15: Central Data Storage (Server-Side)

**User Stories:** All data on central server; single source of truth; access-controlled sharing.

### Acceptance Criteria

#### Stored Data Types

- [ ] Stores ALL PlayerData.json data per character: Blueprints, Colonies, Surveys, Delivery Routes/Plans, Build Plans, Ships, Ship Templates, Stations, Asteroids, Market Listings/Transactions, Pricing Plans, Supply Chains, Stock Plans/Profiles.
- [ ] Stores ALL BaselineData.json as shared/global data.
- [ ] Character data isolated; accessible by owner, granted faction members, or Owner.
- [ ] Global data readable by all authenticated users; writable by Owner (or granted characters).

#### Data Access API

- [ ] CRUD per data type: /api/characters/{uuid}/data/{dataType}.
- [ ] Individual entities: GET/PUT/DELETE /api/characters/{uuid}/data/{dataType}/{entityUuid}.
- [ ] Bulk upload/download: PUT/GET /api/characters/{uuid}/data.
- [ ] Global data: /api/global/{dataType}.
- [ ] PUT/POST/DELETE on character data restricted to owning character (or Owner).

#### Sharing Controls

- [ ] Category-level sharing: "share all colonies with Faction X".
- [ ] Item-level sharing: "share Colony A with Character Y".
- [ ] Per-item rules override category-level (most specific wins).
- [ ] No rule = private (owner + server Owner only).
- [ ] PUT/GET /api/characters/{uuid}/sharing for configuration.

#### Accessing Shared Data

- [ ] GET /api/factions/{uuid}/shared/{dataType} — faction members view shared data.
- [ ] GET /api/characters/{uuid}/shared-with-me/{dataType} — direct shares.
- [ ] Response identifies owning character.
- [ ] No grant = 403; Owner bypasses all checks.
- [ ] Leaving faction immediately revokes faction-based access.
- [ ] Cross-reference filtering: strip references to inaccessible entities server-side.

## Req 17: Server-Side Background Processing

**User Stories:** Server ticks colony timers so data stays current even when clients are offline.

### Acceptance Criteria

- [ ] Optional server-side background processor for colony timers (mining, refining, manufacturing, research, commodity production).
- [ ] Disabled by default; enabled via config.
- [ ] Only processes characters who have opted in via PUT /api/characters/{uuid}/preferences.
- [ ] Same tick interval as client-side BackgroundProcessor (default 60s).
- [ ] Uses same processing logic as client (shared code from Common).
- [ ] When client connected + server processing enabled, client disables its own processor.
- [ ] GET /api/status exposes processing state.
- [ ] PUT /api/admin/processing toggles at runtime (Owner only).

#### Game API Credential Storage

- [ ] PUT /api/v1/characters/{uuid}/secrets stores encrypted credentials.
- [ ] Encrypted at rest (server-side key).
- [ ] GET returns metadata only (never plaintext values).
- [ ] DELETE removes all credentials for character.
- [ ] Only owning character or Owner can access.
- [ ] Secrets excluded from exports and sync responses.

## Req 18: Real-Time Push via WebSocket

**User Stories:** See faction changes immediately; colony data updates in real-time from server ticks.

### Acceptance Criteria

#### Connection

- [ ] WebSocket endpoint at /ws; same HTTPS/TLS transport.
- [ ] Authenticated via Bearer token (query param or handshake).
- [ ] Persistent connection per client; auto-reconnect with exponential backoff.

#### Server-to-Client Push

- [ ] Push notifications when accessible data changes.
- [ ] Events include: type, entity type, entity UUID, timestamp.
- [ ] Event types: Created, Updated, Deleted, TimerTick, MembershipChanged, InvitationReceived, RequestReceived.
- [ ] Client only receives events for authorized data.
- [ ] Client fetches updated data via REST on event (notification only, not full payload).

#### Client-to-Server

- [ ] Heartbeat/ping to keep connection alive.
- [ ] Server disconnects after configurable timeout without heartbeat (default 60s).

#### Fallback

- [ ] Falls back to periodic REST polling if WebSocket unavailable.

## Req 19: Rate Limiting / Throttling

**User Stories:** Server owner sets per-token request limits to prevent abuse.

### Acceptance Criteria

- [ ] PUT /api/tokens/{id}/limits configures per-token rate limits.
- [ ] Expressed as requests per minute (default 60).
- [ ] 429 Too Many Requests when exceeded; includes Retry-After header.
- [ ] Owner token exempt from rate limiting.
- [ ] Rate limit config persisted in data file.
- [ ] GET /api/tokens/{id}/limits returns current limits.
- [ ] Limits communicated to client on WebSocket connect.
- [ ] Push notification when limits changed by Owner.

## Req 20: Solution Architecture (Server Portion)

**User Stories:** Server is a separate deployable without WinForms dependencies; references Common library.

### Acceptance Criteria

- [ ] Solution split: Tool (WinForms), Common (.NET Standard 2.0), Server (.NET 8+ ASP.NET Core Minimal API).
- [ ] Server references Common only (no Tool dependency).
- [ ] ASP.NET Core endpoints, middleware, authentication live in Server.
- [ ] Server-side persistence implementations live in Server.
- [ ] Server-side background processor lives in Server (uses Common's processing logic).
- [ ] WebSocket hub and push notification logic live in Server.
- [ ] Token management and rate limiting live in Server.
