# OE2EmpireTracker.Server — Design

> Server-specific design extracted from the umbrella spec at
> `.kiro/specs/remote-faction-service/design.md`.
> Satisfies: Req 1–10, 12–15, 17–20.

## 1. Solution Architecture (Server Portion)

```
OE2EmpireTracker.Server/       → .NET 8.0 (ASP.NET Core Minimal API)
  References: OE2EmpireTracker.Common (.NET Standard 2.0)

  Endpoints/    Minimal API route groups (Factions, Characters, Data, Tokens, Admin, Sync, WebSocket)
  Auth/         TokenAuthHandler, RoleAuthorization, TokenStore
  Storage/      IStorageBackend implementations (JsonFile, Sqlite, Postgres, Dynamo)
  Processing/   ServerBackgroundProcessor (wraps Common's processing logic)
  Push/         WebSocketHub, EventDispatcher
  Config/       ServiceConfiguration, CertificateManager
```

## 2. Data Models (Server-Side Extensions)

### 2.1 Server Models

| Model | Purpose |
|-------|---------|
| `ApiToken` | Token record: Id, TokenHash (SHA-256), CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits |
| `TokenRole` | Enum: Owner, FactionLeader, Character |
| `RateLimitConfig` | RequestsPerMinute (default 60) |
| `MembershipAction` | Join request or invitation: Id, FactionUUID, CharacterUUID, Type, CreatedUtc, ExpiresUtc |
| `MembershipActionType` | Enum: JoinRequest, Invitation |
| `SharingRule` | Per-character sharing config: OwnerCharacterUUID, TargetUUID, TargetType, DataType, EntityUUID |
| `SharingTargetType` | Enum: Faction, Character |
| `CharacterPreferences` | CharacterUUID, ServerProcessing (bool) |
| `EntityMetadata` | LastModifiedUtc, ModifiedByTokenId |
| `ServerFaction` | Wraps Faction + LeaderCharacterUUIDs + Metadata |

## 3. Storage Abstraction

### 3.1 Interface: `IStorageBackend`

Core operations: Initialize, ValidateConnection, CRUD for Factions, Characters, CharacterData (per type), GlobalData, Tokens, MembershipActions, SharingRules, CharacterPreferences, FullSnapshot (sync).

### 3.2 Backend Implementations

| Backend | Class | Config Key | Notes |
|---------|-------|-----------|-------|
| JSON Files | `JsonFileStorageBackend` | `Storage:Backend = "JsonFile"` | Default. Atomic writes via temp+rename. |
| SQLite | `SqliteStorageBackend` | `Storage:Backend = "Sqlite"` | Single .db file. Microsoft.Data.Sqlite. |
| PostgreSQL | `PostgresStorageBackend` | `Storage:Backend = "Postgres"` | Npgsql. Suitable for RDS. |
| DynamoDB | `DynamoStorageBackend` | `Storage:Backend = "DynamoDB"` | Single-table design (PK=EntityType#UUID, SK=DataType). |

### 3.3 JSON File Layout

```
data/
├── factions.json
├── characters.json
├── tokens.json
├── membership-actions.json
├── global/
│   ├── BlueprintTypes.json
│   ├── ShipClasses.json
│   ├── TechLevels.json
│   ├── Commodities.json
│   ├── RefiningRecipes.json
│   ├── ResearchTimes.json
│   └── GlobalBlueprints.json
└── characters/
    └── {uuid}/
        ├── Blueprints.json
        ├── Colonies.json
        ├── Surveys.json
        ├── DeliveryRoutes.json
        ├── DeliveryPlans.json
        ├── BuildPlans.json
        ├── Ships.json
        ├── ShipTemplates.json
        ├── Stations.json
        ├── Asteroids.json
        ├── MarketListings.json
        ├── MarketTransactions.json
        ├── PricingPlans.json
        ├── StockPlans.json
        ├── StockProfiles.json
        ├── SupplyChains.json
        ├── sharing.json
        └── preferences.json
```

## 4. REST API Design

See `api-reference.md` for the full endpoint map.

### 4.1 Data Type Identifiers

Used in `/api/v1/characters/{uuid}/data/{dataType}` and `/api/v1/global/{dataType}`:

| dataType | Entity | Scope |
|----------|--------|-------|
| blueprints | Blueprint | Character |
| colonies | Colony | Character |
| surveys | Survey | Character |
| delivery-routes | DeliveryRoute | Character |
| delivery-plans | DeliveryPlan | Character |
| build-plans | BuildPlan | Character |
| ships | Ship | Character |
| ship-templates | ShipTemplate | Character |
| stations | Station | Character |
| asteroids | Asteroid | Character |
| market-listings | MarketListing | Character |
| market-transactions | MarketTransaction | Character |
| pricing-plans | PricingPlan | Character |
| stock-plans | StockPlan | Character |
| stock-profiles | StockProfile | Character |
| supply-chains | SupplyChain | Character |
| blueprint-types | BlueprintType | Global |
| ship-classes | ShipClass | Global |
| tech-levels | TechLevel | Global |
| commodities | Commodity | Global |
| refining-recipes | RefiningRecipe | Global |
| research-times | ResearchTimeEntry | Global |
| global-blueprints | Blueprint | Global |

## 5. Authentication & Authorization

### 5.1 Token Lifecycle

1. First startup → generate Owner token → print to console → store SHA-256 hash.
2. Owner creates character: POST /api/tokens → creates ExternalCharacter + ApiToken → returns plaintext (one-time).
3. Request auth: `Authorization: Bearer <token>` → SHA-256(token) → lookup → resolve role + characterUUID.

### 5.2 Role Permission Matrix

| Operation | Owner | Leader | Character |
|-----------|:-----:|:------:|:---------:|
| CRUD all factions | ✓ | — | — |
| Update own faction | ✓ | ✓ | — |
| CRUD all characters | ✓ | — | — |
| Update own character | ✓ | ✓ | ✓ |
| Read all factions/characters | ✓ | ✓ | ✓ |
| Manage tokens | ✓ | — | — |
| Manage rate limits | ✓ | — | — |
| Read/write own data | ✓ | ✓ | ✓ |
| Read shared faction data | ✓ | ✓ | ✓ |
| Invite to faction | ✓ | ✓ (own) | — |
| Accept join request | ✓ | ✓ (own) | — |
| Remove from faction | ✓ | ✓ (own) | — |
| Toggle server processing | ✓ | — | — |
| Bypass mutual consent | ✓ | — | — |

### 5.3 Rate Limiting

- ASP.NET Core rate limiting middleware.
- Per-token sliding window: configurable requests/minute (default 60).
- Owner exempt.
- 429 response includes `Retry-After` header.
- State in-memory (resets on restart — acceptable for single-instance).

## 6. WebSocket Push

### 6.1 Connection Lifecycle

```
Connect:  wss://server:5443/ws?token=<bearer_token>
          Server validates → accepts or 401
          Server sends: { type: "connected", rateLimits: {...} }

Ongoing:  Client pings every 30s; server pongs
          Server pushes: { type: "event", event: <PushEvent> }

Timeout:  Server closes after 60s without ping
          Client reconnects with exponential backoff (1s → 60s max)
```

### 6.2 Push Event Types

Created, Updated, Deleted, TimerTick, MembershipChanged, InvitationReceived, RequestReceived, RateLimitChanged.

### 6.3 Event Filtering

Per-connection visibility based on token role:
- **Owner**: all events.
- **Character/Leader**: own data, faction membership changes, shared data changes, global data changes.

### 6.4 WebSocketHub

Maintains `ConcurrentDictionary<tokenId, WebSocketConnection>`. Methods: BroadcastToAuthorized, SendToCharacter, SendToFaction, DisconnectToken.

## 7. Background Processing

### 7.1 ServerBackgroundProcessor (BackgroundService)

- Wraps Common's processing algorithm.
- Configurable tick interval (default 60s).
- Only processes opted-in characters.
- Pushes TimerTick events via WebSocket after each cycle.

### 7.2 Client Coordination

On WebSocket connect, server sends `{ type: "processingActive", enabled: true }` if processing is active for that character. Client disables local processor; re-enables on disconnect.

### 7.3 Processing State API

- GET /api/status → processingEnabled, lastTickUtc, coloniesProcessed, optedInCharacters.
- PUT /api/admin/processing → enable/disable at runtime (Owner only).

## 8. Configuration

### 8.1 appsettings.json

Key sections: Server (Port, BehindReverseProxy), Certificate (Path, Password), Storage (Backend, DataPath, ConnectionString, Dynamo*), Processing (Enabled, TickIntervalSeconds), Membership (ExpiryDays), RateLimiting (DefaultRequestsPerMinute).

### 8.2 Precedence

1. CLI arguments (highest)
2. Environment variables (OE2_ prefix)
3. appsettings.json (lowest)

### 8.3 CLI Arguments

```
--port, --data-path, --cert-path, --cert-password,
--storage-backend, --regenerate-owner-token, --version
```
