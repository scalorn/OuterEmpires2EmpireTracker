# Requirements Document

## Introduction

This feature integrates OE2 Empire Tracker with the Outer Empires 2 game API to enable automated data retrieval from the game server. This spec covers the core infrastructure: the Game API HTTP client with resilience policies, per-character secret credential management, Player Profile synchronization as the first data type, a Preferences UI for Game API settings, sync status visibility in the status bar, and offline/graceful degradation behavior.

Additional data types (colonies, blueprints, surveys, market transactions) will be integrated in separate follow-up specs once this core infrastructure is debugged and stable.

The Game API client operates alongside the existing Remote Faction Service (which handles player-to-player data sharing via bearer tokens and WebSocket) as a separate client targeting a different server with a different authentication mechanism (OAuth2 client_credentials flow: appId + clientId + per-character secret → JWT access token).

## Glossary

- **Game_API_Client**: The HTTP client component that communicates with the Outer Empires 2 game server API endpoints, using OAuth2 client_credentials flow for authentication and Polly resilience policies (retry, circuit breaker, rate limiting).
- **Credential_Manager**: The component responsible for securely storing and retrieving per-character game API secrets using Windows DPAPI encryption via the existing CredentialStore pattern.
- **Connection_Monitor**: The component that tracks game API reachability via periodic token exchange attempts and triggers appropriate offline/online transitions.
- **Rate_Limiter**: The Polly-based component that enforces API call rate limits using a sliding window to prevent throttling, updating limits immediately when the server communicates new values via response headers.
- **Sync_Scheduler**: The component that manages periodic polling intervals and coordinates Player Profile data retrieval timing.
- **Game_API**: The Outer Empires 2 server-side REST API that exposes game state data for authenticated players.
- **App_ID**: A registered third-party application GUID used in token exchange and as X-App-Id header on data requests. Not secret — stored in settings.
- **Client_ID**: A player's account identifier, shared across all characters on the same account. Not secret — stored in settings.
- **Secret**: A per-character authentication credential issued by the game that grants read access to that character's game data via token exchange. Stored encrypted via DPAPI.
- **Access_Token**: A JWT obtained from the token exchange endpoint, used as Bearer token for authenticated data requests. Cached with expiry tracking.
- **Polling_Interval**: The configurable time between successive data retrieval cycles from the game API (default: 5 minutes, range: 1–60 minutes).
- **Player_Profile**: The character data model containing UUID, Name, Faction, Skills dictionary, Ranks (Public/Private/Military), SkillPoints, CitizenId, and RegistrationDate.

## Requirements

### Requirement 1: API Credential Storage

**User Story:** As a player, I want to securely store my per-character game API secrets, so that the tracker can authenticate with the game server without exposing my credentials.

#### Acceptance Criteria

1. WHEN a player provides a game API secret for a character, THE Credential_Manager SHALL encrypt the secret using Windows DPAPI with DataProtectionScope.CurrentUser before persisting it to disk.
2. THE Credential_Manager SHALL store each character's secret separately, keyed by the character's player UUID.
3. WHEN the application starts, THE Credential_Manager SHALL load encrypted secret credentials from the secrets file without requiring the user to re-enter them.
4. IF the secrets file is corrupted or unreadable, THEN THE Credential_Manager SHALL log the error, discard the corrupted file, allow the application to continue running without credentials, and prompt the user asynchronously to re-enter credentials via the Preferences form.
5. THE Credential_Manager SHALL store the encrypted secret credentials in a file at %LOCALAPPDATA%\OE2EmpireTracker\game-api-secrets.dat, separate from the Remote Faction Service credentials.
6. WHEN a player removes a character's secret, THE Credential_Manager SHALL delete that key entry from the secrets file and overwrite the old file contents.
7. THE Credential_Manager SHALL use the existing CredentialStore.Protect and CredentialStore.Unprotect methods for DPAPI encryption and decryption.


### Requirement 2: Game API HTTP Client

**User Story:** As a player, I want the tracker to communicate reliably with the game API, so that data retrieval succeeds even under transient network conditions.

#### Acceptance Criteria

1. THE Game_API_Client SHALL be a separate HTTP client instance from the RemoteFactionClient, targeting the game API server URL configured in Preferences.
2. THE Game_API_Client SHALL authenticate requests using OAuth2 client_credentials flow: exchange appId + clientId + secret for a JWT access token via POST /v1/auth/token, then include `Authorization: Bearer <token>` and `X-App-Id: <appId>` headers on data requests.
3. THE Game_API_Client SHALL use Polly retry policy with exponential backoff for transient failures (HTTP 500, 502, 503, 504), with a maximum of 3 retry attempts and initial delay of 1 second.
4. THE Game_API_Client SHALL use Polly circuit breaker policy that opens after 3 consecutive failures, with a break duration of 30 seconds before attempting a half-open probe.
5. THE Game_API_Client SHALL implement IDisposable and dispose of the underlying HttpClient and rate limiter when disposed.
6. THE Game_API_Client SHALL expose an async method to test connectivity by performing a token exchange against the game API (no /health endpoint exists), returning success or failure with a descriptive message.
7. THE Game_API_Client SHALL cache access tokens per-character with a 60-second expiry buffer, automatically refreshing when expired.

### Requirement 3: Rate Limiting

**User Story:** As a player, I want the tracker to respect the game API's rate limits, so that my account is not throttled or banned for excessive requests.

#### Acceptance Criteria

1. THE Rate_Limiter SHALL enforce a configurable maximum number of API requests per minute (default: 30 requests per minute).
2. WHEN the game API returns an HTTP 429 (Too Many Requests) response with a Retry-After header, THE Rate_Limiter SHALL pause all outgoing requests for the duration specified in the Retry-After header.
3. IF the game API returns HTTP 429 without a Retry-After header, THEN THE Rate_Limiter SHALL pause all outgoing requests for 60 seconds.
4. WHEN the game API returns a rate limit value in a response header (X-RateLimit-Limit), THE Rate_Limiter SHALL update its internal limit immediately to match the server-communicated value.
5. THE Rate_Limiter SHALL use a sliding window token approach consistent with the existing RemoteFactionClient rate limiting pattern (SemaphoreSlim with timed token release).


### Requirement 4: Connection Monitoring

**User Story:** As a player, I want the tracker to monitor its connection to the game API automatically, so that I get live data when available and graceful degradation when the API is unreachable.

#### Acceptance Criteria

1. WHEN the application starts with configured credentials (appId, clientId, and at least one character secret), THE Connection_Monitor SHALL attempt to verify game API reachability by performing a token exchange within 5 seconds of startup.
2. WHILE the game API is reachable, THE Connection_Monitor SHALL periodically verify reachability by performing a token exchange at an interval no greater than the configured Polling_Interval.
3. WHEN the Connection_Monitor verifies reachability, THE Connection_Monitor SHALL report the connection status as "Connected" only after a successful real-time token exchange response (not cached state).
4. IF the game API becomes unreachable during a token exchange or data retrieval attempt, THEN THE Connection_Monitor SHALL transition to "Disconnected" status and begin exponential backoff reconnection attempts starting at 1 second, doubling up to a maximum of 60 seconds.
5. WHEN the Connection_Monitor transitions between Connected and Disconnected states, THE Connection_Monitor SHALL raise a GameApiConnectionStatusChanged event; state transitions SHALL proceed even if the event handler throws an exception.
6. WHEN the circuit breaker transitions to Open state, THE Connection_Monitor SHALL report status as "Disconnected (Circuit Open)" and wait the configured break duration before attempting a half-open probe.

### Requirement 5: Periodic Player Profile Synchronization

**User Story:** As a player, I want the tracker to periodically pull my character's skills and rank from the game API, so that skill-dependent calculations use current values.

#### Acceptance Criteria

1. WHILE the game API connection is active, THE Sync_Scheduler SHALL poll for Player Profile data at the configured Polling_Interval (default: 5 minutes).
2. WHEN the Sync_Scheduler retrieves Player Profile data, THE Sync_Scheduler SHALL update the existing PlayerProfile's game-authoritative fields: Skills dictionary (skill names and levels), Ranks (Public, Private, Military), SkillPoints, Faction, and CitizenId.
3. WHEN the Sync_Scheduler retrieves Player Profile data, THE Sync_Scheduler SHALL preserve user-preference fields that are not represented in the API response: SkillGroup collapse states (the private SkillGroups dictionary).
4. THE Sync_Scheduler SHALL retrieve Player Profile data for all characters that have configured API keys, processing them in round-robin sequence.
5. WHEN a profile sync cycle completes successfully for a character, THE Sync_Scheduler SHALL update a LastProfileSyncUtc timestamp on the character's sync metadata.
6. THE Sync_Scheduler SHALL only update the LastProfileSyncUtc timestamp upon successful completion of the sync operation, not on partial or failed attempts.
7. IF a profile sync cycle fails for one character, THEN THE Sync_Scheduler SHALL continue with the next character and retry the failed character on the next polling cycle.
8. THE Sync_Scheduler SHALL expose a manual "Sync Now" action that triggers an immediate Player Profile retrieval outside the normal polling schedule.


### Requirement 6: Player Profile Conflict Resolution

**User Story:** As a player, I want the tracker to handle conflicts between my local profile data and game API data intelligently, so that I do not lose my UI preferences while keeping game data current.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler detects that a game-authoritative field (Skills, Ranks, SkillPoints, Faction, CitizenId) differs between the local PlayerProfile and the API response, THE Sync_Scheduler SHALL overwrite the local value with the API value (API wins for game-authoritative fields).
2. WHEN the Sync_Scheduler processes a Player Profile update, THE Sync_Scheduler SHALL preserve all SkillGroup collapse states (expanded/collapsed UI preferences) regardless of what the API returns.
3. WHEN a conflict is resolved, THE Sync_Scheduler SHALL log the resolution decision including the field name, previous local value, new API value, and the resolution strategy applied ("API wins" or "Local preserved").

### Requirement 7: Preferences UI for Game API Settings

**User Story:** As a player, I want to configure game API settings in the Preferences form, so that I can manage credentials, polling intervals, and test connectivity.

#### Acceptance Criteria

1. THE Preferences form SHALL include a "Game API" tab for configuring game API integration settings.
2. THE "Game API" tab SHALL provide input fields for: App ID (registered application GUID), Client ID (player account identifier), and Secret (per-character, displayed as masked characters ●●●●).
3. THE "Game API" tab SHALL provide a text input for the game API server URL.
4. THE "Game API" tab SHALL provide a numeric input for the polling interval in minutes, with a minimum value of 1 and a maximum value of 60, defaulting to 5.
5. THE "Game API" tab SHALL provide a "Test Connection" button that performs a token exchange using the entered credentials (appId, clientId, secret) and displays the result (success with character ID and scopes, or failure reason) in the UI.
6. WHEN the user saves Preferences with a new secret value, THE Preferences form SHALL pass the secret to the Credential_Manager for DPAPI encryption and persistence.
7. WHEN the user saves Preferences with a changed polling interval, THE Sync_Scheduler SHALL apply the new interval immediately without requiring an application restart.
8. THE "Game API" tab SHALL persist its settings (server URL, App ID, Client ID, polling interval, enabled state) in the UIPreferences.json file under a new GameApiConnection section, following the same pattern as the existing ServerConnection section.


### Requirement 8: Sync Status Visibility

**User Story:** As a player, I want to see the current game API sync status and last sync time in the status bar, so that I know whether my data is current.

#### Acceptance Criteria

1. THE application status bar SHALL display the game API connection status using one of these states: "Game API: Connected", "Game API: Disconnected", "Game API: Syncing", "Game API: Rate Limited", or "Game API: Not Configured".
2. THE application status bar SHALL display the time elapsed since the last successful Player Profile sync cycle (e.g., "Last sync: 3m ago").
3. WHEN a sync cycle is in progress, THE application status bar SHALL display "Game API: Syncing..." as the status indicator.
4. IF a sync cycle fails, THEN THE application status bar SHALL wait until the current sync operation fully completes (success or final failure) before displaying the failure reason for 30 seconds, then revert to the normal connection status display.
5. WHEN no credentials are configured for any character (no secret stored), THE application status bar SHALL display "Game API: Not Configured" and suppress sync-related status updates.

### Requirement 9: Offline Operation and Graceful Degradation

**User Story:** As a player, I want the tracker to work fully offline when the game API is unavailable, so that I can still manage my empire data locally.

#### Acceptance Criteria

1. WHILE the game API is unreachable, THE application SHALL continue to operate using locally-persisted PlayerProfile data with full functionality (skill lookups, rank display, all calculations).
2. WHILE the game API is unreachable, THE Sync_Scheduler SHALL suspend polling and resume automatically when the Connection_Monitor reports connectivity is restored.
3. WHEN connectivity is restored after an offline period, THE Sync_Scheduler SHALL perform an immediate Player Profile sync cycle to update local data with the current game state.
4. THE application SHALL preserve all manual HTML import functionality as a fallback data entry method regardless of API availability or online/offline status. HTML import SHALL always be available.
5. IF the game API returns an HTTP 401 (Unauthorized) response during token exchange, THEN THE Game_API_Client SHALL transition to "Disconnected (Invalid Key)" status, stop polling for that character, and log the authentication failure without retrying until the user provides new credentials.

### Requirement 10: Game API Settings Persistence

**User Story:** As a player, I want my game API configuration to persist between application sessions, so that I do not need to reconfigure settings each time I launch the tracker.

#### Acceptance Criteria

1. THE application SHALL persist game API settings (server URL, App ID, Client ID, polling interval in minutes, enabled/disabled state) in the UIPreferences.json file under a GameApiConnection section.
2. WHEN the application starts, THE application SHALL load GameApiConnection settings from UIPreferences.json and initialize the Game_API_Client and Sync_Scheduler accordingly.
3. IF the GameApiConnection section is missing from UIPreferences.json, THEN THE application SHALL use default values (empty server URL, empty App ID, empty Client ID, 5-minute polling interval, disabled state) and continue startup without error.
4. THE GameApiConnection settings SHALL follow the same JSON serialization pattern as the existing ServerConnection section (Newtonsoft.Json, PascalCase property names in the file).
