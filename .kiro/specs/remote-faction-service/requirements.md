# Remote Faction Service — Requirements

## User Goal

Faction members want a shared, always-on service that holds ALL player and game data so that multiple OE2 Empire Tracker instances can read and write from a central source. The service replaces local JSON files as the primary data store, with local files serving only as an offline cache.

## Out of Scope

- GUI — this is a headless command-line service
- Game integration or scraping
- Chat, messaging, or diplomacy mechanics

## Requirement 1: Cross-Platform CLI Execution

### User Stories

- As a faction leader, I want to run the service on my Linux VPS so it's always available.
- As a developer, I want to run it locally on Windows for testing.

### Acceptance Criteria

- [ ] The service SHALL be a .NET 8+ console application with no GUI dependencies.
- [ ] The service SHALL be publishable as a self-contained single-file executable for Windows (x64), Linux (x64), and macOS (arm64).
- [ ] The service SHALL start by listening on a configured HTTPS port and log a startup message to the console.
- [ ] The service SHALL exit cleanly on SIGINT/SIGTERM (Ctrl+C).

## Requirement 2: HTTPS / TLS Transport

### User Stories

- As a faction leader, I want all communication encrypted so credentials and data aren't exposed on the network.
- As a server operator, I want the service to generate a self-signed certificate on first run so it works out of the box.
- As a server operator with a domain, I want to provide my own certificate for trusted HTTPS.

### Acceptance Criteria

- [ ] The service SHALL listen on HTTPS only (no plain HTTP endpoint).
- [ ] On first startup, IF no certificate is configured, the service SHALL generate a self-signed TLS certificate and persist it to a configurable path.
- [ ] The service SHALL support configuring a custom PFX/PEM certificate file and password via configuration.
- [ ] The client (tracker app) SHALL allow the user to trust a specific server certificate thumbprint to handle self-signed certs without OS trust store changes.
- [ ] The service SHALL log the certificate thumbprint on startup so operators can share it with clients.
- [ ] The service SHALL support running behind a reverse proxy (nginx, Caddy) that terminates TLS, by respecting X-Forwarded-For and X-Forwarded-Proto headers when configured.

## Requirement 3: Faction Management API

### User Stories

- As a tracker user, I want to create a faction so all members can see it.
- As a tracker user, I want to rename or update a faction's description.
- As a tracker user, I want to delete a faction that's no longer relevant.

### Acceptance Criteria

- [ ] POST /api/factions SHALL create a faction with a deterministic UUID derived from the name.
- [ ] The service SHALL support multiple independent factions on the same server instance.
- [ ] Factions SHALL be flat (no sub-factions or hierarchy).
- [ ] GET /api/factions SHALL return all factions.
- [ ] GET /api/factions/{uuid} SHALL return a single faction or 404.
- [ ] PUT /api/factions/{uuid} SHALL update Name and Description.
- [ ] DELETE /api/factions/{uuid} SHALL remove the faction and clear FactionUUID on linked characters.
- [ ] Creating a faction with a name that already exists SHALL return 409 Conflict.

## Requirement 4: External Character Management API

### User Stories

- As a tracker user, I want to register an external character I've interacted with.
- As a tracker user, I want to assign a character to a faction.

### Acceptance Criteria

- [ ] POST /api/characters SHALL create a character with a deterministic UUID derived from the name.
- [ ] GET /api/characters SHALL return all characters.
- [ ] GET /api/characters/{uuid} SHALL return a single character or 404.
- [ ] PUT /api/characters/{uuid} SHALL update Name and FactionUUID.
- [ ] DELETE /api/characters/{uuid} SHALL remove the character.
- [ ] Creating a character with a name that already exists SHALL return 409 Conflict.
- [ ] Setting FactionUUID to a non-existent faction SHALL return 400 Bad Request.

## Requirement 5: Data Persistence

### User Stories

- As a server operator, I want data to survive service restarts.
- As a server operator, I want to choose a storage backend that fits my infrastructure.
- As a server operator running on a VPS, I want a simple option that needs no external database.
- As a server operator on AWS, I want to use managed services for reliability.

### Acceptance Criteria

- [ ] The service SHALL support pluggable persistence backends via a storage abstraction layer.
- [ ] The following backends SHALL be supported: JSON files, SQLite, PostgreSQL, AWS DynamoDB.
- [ ] The active backend SHALL be configurable via appsettings.json (default: JSON files).
- [ ] JSON files: single file per character + one global file, atomic writes (write-to-temp + rename). Default data directory: ./data/.
- [ ] SQLite: single database file, no external dependencies. Suitable for single-server deployments.
- [ ] PostgreSQL: connection string configured in settings. Suitable for multi-instance or managed hosting (including AWS RDS).
- [ ] AWS DynamoDB: table name and region configured in settings. Suitable for serverless/managed AWS deployments.
- [ ] All backends SHALL implement the same storage interface so the rest of the service is backend-agnostic.
- [ ] The service SHALL validate the storage configuration on startup and exit with a clear error if the backend is unreachable.
- [ ] Data format in storage SHALL be compatible with export to the standard JSON format (Requirement 16).

## Requirement 6: Deterministic UUID Generation

### User Stories

- As a tracker user, I want the same faction name to produce the same UUID regardless of which client creates it.

### Acceptance Criteria

- [ ] Faction UUIDs SHALL use the same deterministic algorithm and namespace as the existing tracker (DeterministicUUID with faction namespace).
- [ ] Character UUIDs SHALL use the same deterministic algorithm and namespace as the existing tracker (DeterministicUUID with character namespace).
- [ ] UUID generation SHALL be case-insensitive (normalizes to lowercase before hashing).

## Requirement 7: Conflict Resolution

### User Stories

- As a tracker user, I don't want my edits silently lost if someone else edited the same record.

### Acceptance Criteria

- [ ] Each entity SHALL have a LastModified (UTC ISO 8601) timestamp updated on every mutation.
- [ ] The service SHALL process requests sequentially (no concurrent writes to the same entity).
- [ ] The service SHALL use last-write-wins semantics — the most recent PUT overwrites previous state.
- [ ] Responses SHALL include the LastModified timestamp so clients can detect staleness.

## Requirement 8: REST API Design

### User Stories

- As a developer integrating the tracker, I want a predictable, standard API.

### Acceptance Criteria

- [ ] All endpoints SHALL use JSON request/response bodies with Content-Type: application/json.
- [ ] Success responses: 200 (GET/PUT), 201 (POST), 204 (DELETE).
- [ ] Error responses: 400 (validation), 404 (not found), 409 (conflict), 401 (unauthorized).
- [ ] GET /health SHALL return 200 with { "status": "ok", "version": "x.y.z" }.
- [ ] All endpoints SHALL be under the /api prefix.

## Requirement 9: Service Configuration

### User Stories

- As a server operator, I want to configure the port and data location without recompiling.

### Acceptance Criteria

- [ ] Configuration SHALL be read from appsettings.json, environment variables, and command-line arguments (in that precedence order).
- [ ] Configurable values: HTTPS port (default 5443), data file path, certificate path, certificate password, API keys.
- [ ] The service SHALL validate configuration on startup and exit with a clear error if invalid.

## Requirement 10: Logging and Diagnostics

### User Stories

- As a server operator, I want to see what's happening without attaching a debugger.

### Acceptance Criteria

- [ ] The service SHALL log to stdout using structured logging (timestamp, level, message).
- [ ] Log levels: Information for startup/shutdown, Warning for validation failures, Error for unhandled exceptions.
- [ ] Each mutation (create/update/delete) SHALL be logged with entity type, UUID, and client IP.

## Requirement 11: Client Connectivity and Offline Fallback

### User Stories

- As a tracker user, I want the app to still work if the service is temporarily down.

### Acceptance Criteria

- [ ] The tracker client SHALL fall back to local faction/character data if the service is unreachable.
- [ ] The tracker client SHALL sync local data with the service on reconnection.
- [ ] The tracker client SHALL indicate connection status (connected/disconnected) in the UI.
- [ ] The tracker client SHALL allow configuring the service URL and certificate thumbprint in preferences.

## Requirement 12: Bulk Data Retrieval (Sync)

### User Stories

- As a tracker user opening the app, I want to quickly get all current faction/character data.

### Acceptance Criteria

- [ ] GET /api/sync SHALL return the complete dataset (all factions and all characters) in a single response.
- [ ] The response SHALL include a server timestamp for cache validation.
- [ ] The client SHALL use this endpoint on startup and periodically to stay current.

## Requirement 13: Token-Based Permissions Model

### User Stories

- As the server owner, I want full control over the service — creating characters, managing tokens, and editing any data.
- As the server owner, I want to create a character account and give that person a token so they can access the service.
- As a character (regular user), I want to be able to read all data and edit my own character, but not manage other people's data or create new accounts.

### Acceptance Criteria

- [ ] The service SHALL authenticate all requests via a Bearer token in the Authorization header.
- [ ] Unauthenticated requests SHALL return 401 Unauthorized.
- [ ] The service SHALL support three roles: **Owner**, **Faction Leader**, and **Character**.
- [ ] On first startup, the service SHALL generate an Owner token and display it in the console log (one-time display).
- [ ] The Owner token SHALL be persisted in the data file (hashed) so it survives restarts.

#### Owner Role Permissions

- [ ] The Owner SHALL be able to perform all CRUD operations on factions and characters.
- [ ] The Owner SHALL be able to create new Character accounts via POST /api/tokens, providing a character name.
- [ ] POST /api/tokens SHALL automatically create the ExternalCharacter entity AND generate a token in a single step.
- [ ] POST /api/tokens SHALL return the generated token that the owner shares with the character out-of-band (email, Discord, etc.).
- [ ] The Owner SHALL be able to list all tokens via GET /api/tokens (showing character name, role, created date, last used — but NOT the token value).
- [ ] The Owner SHALL be able to revoke a character's token via DELETE /api/tokens/{id}.
- [ ] The Owner SHALL be able to regenerate a new token for an existing character via POST /api/tokens/{id}/regenerate, which invalidates the old token and returns a new one.
- [ ] The Owner SHALL be able to designate a character as a Faction Leader for a specific faction via PUT /api/factions/{uuid}/leader.
- [ ] The Owner SHALL be able to remove a Faction Leader designation.
- [ ] The Owner token SHALL be regenerable via a CLI command: `--regenerate-owner-token`. This prints the new token and exits.

#### Faction Leader Role Permissions

- [ ] A Faction Leader SHALL be a Character who has been designated as leader of a specific faction by the Owner or by another Faction Leader of the same faction.
- [ ] A faction SHALL support multiple Faction Leaders simultaneously (co-leaders).
- [ ] A Faction Leader SHALL be able to promote another faction member to Faction Leader for their faction via PUT /api/factions/{uuid}/leaders.
- [ ] A Faction Leader SHALL be able to demote another Faction Leader (but not themselves) via DELETE /api/factions/{uuid}/leaders/{characterUUID}.
- [ ] A Faction Leader SHALL be able to update their faction's details (Name, Description) via PUT /api/factions/{their-faction-uuid}.
- [ ] A Faction Leader SHALL be able to invite a character to their faction via POST /api/factions/{uuid}/invitations.
- [ ] A Faction Leader SHALL be able to accept a pending join request for their faction.
- [ ] A Faction Leader SHALL be able to remove characters from their faction (clear FactionUUID).
- [ ] A Faction Leader SHALL NOT be able to create or delete factions.
- [ ] A Faction Leader SHALL NOT be able to manage tokens or other factions.
- [ ] A Faction Leader SHALL retain all Character role permissions (read all, edit own record).

#### Character Role Permissions

- [ ] A Character token SHALL be linked to a specific ExternalCharacter UUID.
- [ ] A Character SHALL be able to read all factions and characters (GET endpoints).
- [ ] A Character SHALL be able to update their own character record (PUT /api/characters/{their-uuid}).
- [ ] A Character SHALL be able to request to join a faction via POST /api/factions/{uuid}/requests.
- [ ] A Character SHALL be able to accept a pending invitation to a faction.
- [ ] A Character SHALL be able to leave their current faction.
- [ ] A Character SHALL NOT be able to create, update, or delete other characters.
- [ ] A Character SHALL NOT be able to create, update, or delete factions (faction management is owner/leader-only).
- [ ] A Character SHALL NOT be able to manage tokens.
- [ ] Attempting a forbidden operation SHALL return 403 Forbidden.

#### Token Format

- [ ] Tokens SHALL be cryptographically random strings (at least 32 bytes, base64url-encoded).
- [ ] Tokens SHALL be stored as SHA-256 hashes in the data file (never stored in plaintext after generation).
- [ ] Token lookup SHALL hash the incoming token and compare against stored hashes.

## Requirement 14: Faction Membership — Mutual Consent

### User Stories

- As a character, I want to request joining a faction so the faction leader knows I'm interested.
- As a faction leader, I want to invite a character to my faction so they know we want them.
- As a character, I want to accept a faction invitation to complete my membership.
- As a faction leader, I want to accept a join request to bring a character into the faction.

### Acceptance Criteria

- [ ] A character SHALL be able to request joining a faction via POST /api/factions/{uuid}/requests.
- [ ] A faction leader SHALL be able to invite a character via POST /api/factions/{uuid}/invitations with the target character UUID.
- [ ] Membership SHALL only be granted when BOTH sides have agreed (request + acceptance OR invitation + acceptance).
- [ ] A faction leader accepting a pending join request SHALL set the character's FactionUUID and clear the request.
- [ ] A character accepting a pending invitation SHALL set their FactionUUID and clear the invitation.
- [ ] Pending requests and invitations SHALL be visible via GET /api/factions/{uuid}/requests and GET /api/factions/{uuid}/invitations.
- [ ] A character who is already in a faction SHALL NOT be able to request joining another (must leave first).
- [ ] A character SHALL be able to leave their current faction via DELETE /api/characters/{uuid}/faction.
- [ ] Requests and invitations SHALL expire after a configurable period (default: 7 days).
- [ ] The Owner SHALL be able to bypass mutual consent and directly assign a character to a faction.

## Requirement 15: Central Data Storage

### User Stories

- As a tracker user, I want all my data stored on the central server so I can access it from any machine.
- As a faction leader, I want to see what my members have shared to coordinate faction operations.
- As a server operator, I want a single source of truth for all game data.

### Acceptance Criteria

#### Stored Data Types

- [ ] The service SHALL store ALL data currently in PlayerData.json per character: Blueprints, Colonies, Surveys, Delivery Routes, Delivery Plans, Build Plans, Ships, Ship Templates, Stations, Asteroids, Market Listings, Market Transactions, Pricing Plans, Supply Chains, Stock Plans, Stock Profiles.
- [ ] The service SHALL store ALL data currently in BaselineData.json as shared/global data: Blueprint Types, Ship Classes, Tech Levels, Commodities, Refining Recipes, Research Times, Global Blueprints.
- [ ] Each character's data SHALL be isolated — only accessible by that character, faction members with grants, or the Owner.
- [ ] Global/baseline data SHALL be readable by all authenticated users.
- [ ] Global/baseline data SHALL be writable only by the Owner.

#### Data Access API

- [ ] Each data type SHALL have CRUD endpoints under /api/characters/{uuid}/data/{dataType}.
- [ ] Individual entities SHALL be addressable: GET/PUT/DELETE /api/characters/{uuid}/data/{dataType}/{entityUuid}.
- [ ] POST /api/characters/{uuid}/data/{dataType} SHALL create a new entity within that collection.
- [ ] GET /api/characters/{uuid}/data/{dataType} SHALL return the full collection for that data type.
- [ ] A bulk upload/download of all character data SHALL be available via PUT/GET /api/characters/{uuid}/data (all types at once).
- [ ] Global data SHALL be accessible under /api/global/{dataType}.
- [ ] Global data SHALL be writable by the Owner by default.
- [ ] The Owner SHALL be able to grant global data write permissions to specific characters (e.g., for market imports that update global blueprints).
- [ ] PUT/POST/DELETE on character data SHALL only be allowed by the owning character (or Owner).
- [ ] The tracker client SHALL use the service as primary storage when connected, falling back to local JSON files when disconnected.

#### Sharing Controls

- [ ] A character SHALL be able to set sharing at the category level: "share all blueprints with Faction X", "share all colonies with Character Y", etc.
- [ ] A character SHALL be able to set sharing at the individual item level: "share Colony A with Faction X", "share Colony B only with Character Y".
- [ ] Per-item sharing rules SHALL override category-level rules (most specific wins).
- [ ] Each sharing rule SHALL specify: a grant target (faction UUID or character UUID) and either a data type (category-level) or a specific entity UUID (item-level).
- [ ] A character SHALL be able to have multiple sharing rules with different targets for the same item (e.g., Colony A shared with both Faction X and Character Z).
- [ ] Items without any sharing rule SHALL be private (not visible to anyone except the owner and the server Owner).
- [ ] A character SHALL be able to grant access to multiple factions for cross-faction coordination (alliance scenario).
- [ ] Sharing preferences SHALL be stored per-character on the service.
- [ ] By default, a character's data SHALL be private (not shared). They must explicitly configure what to share.
- [ ] When a character first joins a faction, the client SHALL prompt them to configure their sharing preferences.
- [ ] A character SHALL be able to update their sharing preferences via PUT /api/characters/{uuid}/sharing.
- [ ] GET /api/characters/{uuid}/sharing SHALL return the current sharing configuration.

#### Accessing Shared Data

- [ ] Faction members SHALL be able to view shared data from other members who have granted access to their faction via GET /api/factions/{uuid}/shared/{dataType}.
- [ ] Individual characters SHALL be able to view data shared directly with them via GET /api/characters/{uuid}/shared-with-me/{dataType}.
- [ ] The response SHALL identify which character owns each shared item.
- [ ] Characters without a matching grant (not in a granted faction, not individually granted) SHALL receive 403 Forbidden.
- [ ] The Owner SHALL be able to access all shared data regardless of grants.
- [ ] When a character leaves a faction, their shared data SHALL become immediately inaccessible to that faction.
- [ ] When serving shared data that contains cross-references to other entities (e.g. a Station with Holds referencing Blueprints, or a DeliveryRoute referencing Colonies), the server SHALL filter out any referenced entities that the viewer does not have access to. No dangling references SHALL be returned.
- [ ] Station Holds SHALL only include entries for characters whose data the viewer has access to. Hold entries belonging to characters who have not shared with the viewer SHALL be omitted entirely.
- [ ] DeliveryRoutes and DeliveryPlans that reference Colonies the viewer cannot access SHALL omit those colony references (or omit the route/plan entirely if it becomes meaningless without them).
- [ ] The filtering SHALL be applied server-side before the response is sent — the client SHALL never receive UUIDs it cannot resolve.

#### Sync and Offline

- [ ] The tracker client SHALL sync all data with the service on startup when connected.
- [ ] The tracker client SHALL write changes to the service immediately when connected.
- [ ] When disconnected, the tracker client SHALL queue changes locally and sync on reconnection.
- [ ] On reconnection after offline changes, IF the server data has also changed (e.g., server-side processing), the client SHALL prompt the user to choose: upload local data to server OR download server data to local.
- [ ] The client SHALL clearly show what has diverged before the user makes their choice.

## Requirement 16: Data Portability and Player Sovereignty

### User Stories

- As a player, I want to keep my local JSON file up to date so I can disconnect from the server at any time without losing data.
- As a player, I want to export all my data from the server into a standard JSON file so I'm never locked in.
- As a player, I want to be able to use the tracker in local-only mode without any server connection.

### Acceptance Criteria

#### Local File Maintenance

- [ ] The tracker client SHALL offer a preference to maintain a local JSON file alongside the server connection (dual-write mode).
- [ ] In dual-write mode, every change written to the server SHALL also be written to the local file.
- [ ] The local file format SHALL remain identical to the current PlayerData.json / BaselineData.json format — fully compatible with local-only operation.
- [ ] A player SHALL be able to disconnect from the server and continue using the tracker with their local file with zero data loss.

#### Data Export

- [ ] The tracker client SHALL provide an "Export from Server" function that downloads all of the character's data from the service and saves it as a local JSON file.
- [ ] The export SHALL produce a file identical in format to the current PlayerData.json.
- [ ] The service SHALL provide GET /api/characters/{uuid}/export that returns the character's complete dataset in the existing JSON format.
- [ ] The Owner SHALL be able to export any character's data.

#### Operating Modes

- [ ] The tracker client SHALL support three operating modes: Local Only, Server Only, and Server + Local (dual-write).
- [ ] Local Only: no server connection, reads/writes local JSON files (current behavior).
- [ ] Server Only: all reads/writes go to the server, no local file maintained.
- [ ] Server + Local (dual-write): primary storage is the server, local file kept in sync as a backup.
- [ ] The operating mode SHALL be configurable in preferences and changeable at any time.
- [ ] Switching from Server Only to Local Only SHALL prompt the user to export their data first.

## Requirement 17: Server-Side Background Processing

### User Stories

- As a server owner, I want the server to run background processing (mining cycles, refining, manufacturing timers) so that player data stays current even when their tracker client is offline.
- As a player, I want my colonies to keep producing while I'm away, without needing my PC running.
- As a server owner, I want to be able to enable or disable this feature.
- As a player who has opted in to server-side processing, I want to provide my game API credentials so the server can interact with the game on my behalf.

### Acceptance Criteria

- [ ] The service SHALL support an optional server-side background processor that ticks colony timers (mining, refining, manufacturing, research, commodity production).
- [ ] Server-side processing SHALL be disabled by default and enabled via configuration (appsettings.json or environment variable).
- [ ] When enabled, the processor SHALL only process colonies for characters who have opted in to server-side processing.
- [ ] Characters SHALL opt in/out of server-side processing via PUT /api/characters/{uuid}/preferences with a `serverProcessing: true/false` flag.
- [ ] By default, characters SHALL be opted OUT of server-side processing.
- [ ] When enabled, the processor SHALL run on the same tick interval as the client-side BackgroundProcessor (configurable, default 60 seconds).
- [ ] The processor SHALL process all characters' colonies that have active timers, regardless of whether the character's client is connected.
- [ ] The processor SHALL use the same processing logic as the client-side BackgroundProcessor (shared code or identical algorithm).
- [ ] When a client is connected and server-side processing is enabled, the CLIENT SHALL NOT run its own background processor to avoid double-processing.
- [ ] The service SHALL expose the processing state via GET /api/status (enabled/disabled, last tick time, colonies processed).
- [ ] The server owner SHALL be able to enable/disable processing at runtime via PUT /api/admin/processing (Owner token required).

#### Game API Credential Storage

- [ ] A character SHALL be able to store game API credentials on the server via PUT /api/v1/characters/{uuid}/secrets.
- [ ] Game API credentials SHALL be encrypted at rest using a server-side encryption key (configurable in appsettings or derived from the Owner token hash).
- [ ] GET /api/v1/characters/{uuid}/secrets SHALL return metadata only (credential type, last updated) — never the plaintext secret values.
- [ ] DELETE /api/v1/characters/{uuid}/secrets SHALL remove all stored credentials for that character.
- [ ] Only the owning character (or Owner) SHALL be able to read, write, or delete their secrets.
- [ ] Secrets SHALL NOT be included in data exports or sync responses.
- [ ] The server-side background processor SHALL use stored game API credentials when interacting with the game API on behalf of a character (details of game API integration are out of scope for this spec).

## Requirement 18: Real-Time Push via WebSocket

### User Stories

- As a player, I want to see faction changes immediately when another member updates shared data, without manually refreshing.
- As a player, I want my colony data to update in real-time when the server processes a timer tick.
- As a faction leader, I want to see when a new member joins or a join request arrives.

### Acceptance Criteria

#### WebSocket Connection

- [ ] The service SHALL expose a WebSocket endpoint at /ws for bidirectional communication.
- [ ] The WebSocket connection SHALL use the same HTTPS/TLS transport as the REST API.
- [ ] The client SHALL authenticate the WebSocket connection using the same Bearer token (sent as a query parameter or in the initial handshake).
- [ ] The service SHALL maintain a persistent WebSocket connection per connected client.
- [ ] The client SHALL automatically reconnect on disconnection with exponential backoff.

#### Server-to-Client Push Events

- [ ] The service SHALL push notifications to connected clients when data they have access to changes.
- [ ] Push events SHALL include: event type, entity type, entity UUID, and timestamp.
- [ ] Event types SHALL include: Created, Updated, Deleted, TimerTick, MembershipChanged, InvitationReceived, RequestReceived.
- [ ] The client SHALL only receive events for data they are authorized to see (own data, shared data, faction data).
- [ ] On receiving a push event, the client SHALL fetch the updated data via the REST API (event is a notification, not the full payload).

#### Client-to-Server Messages

- [ ] The client SHALL be able to send a heartbeat/ping to keep the connection alive.
- [ ] The service SHALL disconnect clients that miss heartbeats for a configurable timeout (default: 60 seconds).

#### Fallback

- [ ] IF the WebSocket connection fails or is unavailable, the client SHALL fall back to periodic polling via REST (configurable interval, default: 60 seconds).
- [ ] The client SHALL indicate in the UI whether it is receiving real-time updates or polling.

## Requirement 19: Rate Limiting / Throttling

### User Stories

- As a server owner, I want to set request limits per token to prevent a misbehaving client from overwhelming the server.
- As a server owner, I want to set different limits for different characters based on their needs.

### Acceptance Criteria

- [ ] The Owner SHALL be able to configure rate limits per token via PUT /api/tokens/{id}/limits.
- [ ] Rate limits SHALL be expressed as requests per minute (default: 60 requests/minute if not set).
- [ ] The service SHALL return 429 Too Many Requests when a token exceeds its rate limit.
- [ ] The 429 response SHALL include a Retry-After header indicating when the client can retry.
- [ ] The Owner token SHALL be exempt from rate limiting.
- [ ] Rate limit configuration SHALL be persisted in the data file.
- [ ] GET /api/tokens/{id}/limits SHALL return the current limits for a token.
- [ ] The service SHALL inform the client of its rate limits on WebSocket connection (as part of the initial handshake response).
- [ ] The service SHALL push a notification to the client via WebSocket when their rate limits are changed by the Owner.
- [ ] The client SHALL respect the communicated limits to self-throttle and avoid 429 responses.

## Requirement 20: Solution Architecture — Tool / Common / Server Split

### User Stories

- As a developer, I want shared code (models, services, algorithms) in a common library so the tool and server stay in sync.
- As a developer, I want the server to be a separate deployable without WinForms dependencies.
- As a developer, I want the tool to remain a WinForms app that references the common library.

### Acceptance Criteria

#### Project Structure

- [ ] The solution SHALL be split into three projects: **Tool** (WinForms client), **Common** (shared library), and **Server** (ASP.NET Core service).
- [ ] **Common** SHALL target .NET Standard 2.0 (compatible with both .NET Framework 4.8.1 and .NET 8+).
- [ ] **Tool** SHALL remain a .NET Framework 4.8.1 WinForms application, referencing Common.
- [ ] **Server** SHALL be a .NET 8+ console application (ASP.NET Core Minimal API), referencing Common.

#### What Goes in Common

- [ ] All Models (POCOs, enums, data types) SHALL live in Common.
- [ ] All Services that contain pure business logic (processing, calculations, validators) SHALL live in Common.
- [ ] Persistence interfaces and serialization logic SHALL live in Common.
- [ ] Constants, deterministic UUID generation, and shared algorithms SHALL live in Common.

#### What Stays in Tool

- [ ] WinForms UI (Forms, Controls, ViewModels, Designer files) SHALL remain in Tool.
- [ ] Local file I/O implementation (SafeFileWriter, WindowStateHelper, PreferencesStore) SHALL remain in Tool.
- [ ] Client-side server communication (HTTP client, WebSocket client) SHALL live in Tool.

#### What Goes in Server

- [ ] ASP.NET Core API endpoints, middleware, and authentication SHALL live in Server.
- [ ] Server-side persistence (JSON file storage for all characters) SHALL live in Server.
- [ ] Server-side background processor SHALL live in Server (using shared processing logic from Common).
- [ ] WebSocket hub and push notification logic SHALL live in Server.
- [ ] Token management and rate limiting SHALL live in Server.
