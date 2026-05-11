# OE2EmpireTracker.Server — API Reference

> Quick-reference for all REST endpoints. Full design details in `design.md`.
> All versioned endpoints use `/api/v1/` prefix. Auth = Bearer token required.

## Health (No Auth)

| Method | Path | Response | Notes |
|--------|------|----------|-------|
| GET | `/health` | 200 `{ status, serverVersion, apiVersion, minClientVersion }` | Public |

## Factions

| Method | Path | Auth | Response |
|--------|------|------|----------|
| POST | `/api/v1/factions` | Owner | 201 `{ faction }` |
| GET | `/api/v1/factions` | Any | 200 `[ factions ]` |
| GET | `/api/v1/factions/{uuid}` | Any | 200 `{ faction }` / 404 |
| PUT | `/api/v1/factions/{uuid}` | Owner, Leader (own) | 200 `{ faction }` |
| DELETE | `/api/v1/factions/{uuid}` | Owner | 204 |
| PUT | `/api/v1/factions/{uuid}/leaders` | Owner, Leader (own) | 200 — add co-leader |
| DELETE | `/api/v1/factions/{uuid}/leaders/{charUUID}` | Owner, Leader (own) | 204 — remove co-leader |
| GET | `/api/v1/factions/{uuid}/leaders` | Any | 200 `[ charUUIDs ]` |

## Faction Membership

| Method | Path | Auth | Response |
|--------|------|------|----------|
| POST | `/api/v1/factions/{uuid}/requests` | Character | 201 — request to join |
| GET | `/api/v1/factions/{uuid}/requests` | Leader, Owner | 200 `[ requests ]` |
| POST | `/api/v1/factions/{uuid}/requests/{id}/accept` | Leader, Owner | 200 |
| DELETE | `/api/v1/factions/{uuid}/requests/{id}` | Leader, Owner, Requester | 204 |
| POST | `/api/v1/factions/{uuid}/invitations` | Leader, Owner | 201 — invite character |
| GET | `/api/v1/factions/{uuid}/invitations` | Leader, Owner | 200 `[ invitations ]` |
| POST | `/api/v1/factions/{uuid}/invitations/{id}/accept` | Invited Character | 200 |
| DELETE | `/api/v1/factions/{uuid}/invitations/{id}` | Leader, Owner, Invitee | 204 |

## Characters

| Method | Path | Auth | Response |
|--------|------|------|----------|
| POST | `/api/v1/characters` | Owner | 201 `{ character }` |
| GET | `/api/v1/characters` | Any | 200 `[ characters ]` |
| GET | `/api/v1/characters/{uuid}` | Any | 200 `{ character }` / 404 |
| PUT | `/api/v1/characters/{uuid}` | Owner, Self | 200 `{ character }` |
| DELETE | `/api/v1/characters/{uuid}` | Owner | 204 |
| DELETE | `/api/v1/characters/{uuid}/faction` | Self, Owner | 204 — leave faction |

## Character Data

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/characters/{uuid}/data` | Self, Owner | 200 `{ all data }` |
| PUT | `/api/v1/characters/{uuid}/data` | Self, Owner | 200 — bulk upload |
| GET | `/api/v1/characters/{uuid}/data/{dataType}` | Self, Owner | 200 `[ entities ]` |
| POST | `/api/v1/characters/{uuid}/data/{dataType}` | Self, Owner | 201 `{ entity }` |
| GET | `/api/v1/characters/{uuid}/data/{dataType}/{id}` | Self, Owner | 200 `{ entity }` |
| PUT | `/api/v1/characters/{uuid}/data/{dataType}/{id}` | Self, Owner | 200 `{ entity }` |
| DELETE | `/api/v1/characters/{uuid}/data/{dataType}/{id}` | Self, Owner | 204 |

## Character Sharing & Preferences

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/characters/{uuid}/sharing` | Self, Owner | 200 `{ rules }` |
| PUT | `/api/v1/characters/{uuid}/sharing` | Self, Owner | 200 `{ rules }` |
| GET | `/api/v1/characters/{uuid}/preferences` | Self, Owner | 200 `{ prefs }` |
| PUT | `/api/v1/characters/{uuid}/preferences` | Self, Owner | 200 `{ prefs }` |

## Shared Data Access

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/factions/{uuid}/shared/{dataType}` | Faction Member, Owner | 200 `[ shared items ]` |
| GET | `/api/v1/characters/{uuid}/shared-with-me/{dataType}` | Self, Owner | 200 `[ shared items ]` |

## Character Export

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/characters/{uuid}/export` | Self, Owner | 200 — full PlayerData.json format |

## Global/Baseline Data

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/global/{dataType}` | Any | 200 `[ entities ]` |
| PUT | `/api/v1/global/{dataType}` | Owner (or granted) | 200 |

## Tokens (Owner Only)

| Method | Path | Auth | Response |
|--------|------|------|----------|
| POST | `/api/v1/tokens` | Owner | 201 `{ token, character }` — creates account + token |
| GET | `/api/v1/tokens` | Owner | 200 `[ summaries ]` — no token values |
| DELETE | `/api/v1/tokens/{id}` | Owner | 204 — revoke |
| POST | `/api/v1/tokens/{id}/regenerate` | Owner | 200 `{ newToken }` |
| GET | `/api/v1/tokens/{id}/limits` | Owner | 200 `{ rateLimits }` |
| PUT | `/api/v1/tokens/{id}/limits` | Owner | 200 `{ rateLimits }` |

## Sync

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/sync` | Any | 200 `{ factions, characters, timestamp }` |

## Admin (Owner Only)

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/v1/status` | Owner | 200 `{ processingEnabled, lastTickUtc, coloniesProcessed }` |
| PUT | `/api/v1/admin/processing` | Owner | 200 `{ enabled }` |

## Secrets (Character Data)

| Method | Path | Auth | Response |
|--------|------|------|----------|
| PUT | `/api/v1/characters/{uuid}/secrets` | Self, Owner | 200 — store encrypted credentials |
| GET | `/api/v1/characters/{uuid}/secrets` | Self, Owner | 200 — metadata only |
| DELETE | `/api/v1/characters/{uuid}/secrets` | Self, Owner | 204 |

## WebSocket

| Protocol | Path | Auth | Notes |
|----------|------|------|-------|
| WSS | `/ws?token={bearer_token}` | Token in query param | Persistent bidirectional connection |

### WebSocket Messages (Server → Client)

| Type | Payload | When |
|------|---------|------|
| `connected` | `{ rateLimits }` | On successful connection |
| `pong` | — | Response to client ping |
| `event` | `{ eventType, entityType, entityUUID, characterUUID, timestamp }` | Data change |
| `processingActive` | `{ enabled }` | On connect if server processing active for character |
| `rateLimitChanged` | `{ rateLimits }` | Owner changed limits |

### WebSocket Messages (Client → Server)

| Type | Payload | Purpose |
|------|---------|---------|
| `ping` | — | Keep-alive (every 30s) |

## HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success (GET, PUT) |
| 201 | Created (POST) |
| 204 | Deleted (DELETE) |
| 400 | Validation error |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient role) |
| 404 | Not found |
| 409 | Conflict (duplicate name) |
| 429 | Rate limited (includes Retry-After header) |
