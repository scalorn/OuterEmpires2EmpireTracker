# Implementation Plan

## Overview

This plan implements the Game API integration infrastructure: credential management, HTTP client with Polly resilience, rate limiting, connection monitoring, periodic Player Profile synchronization, Preferences UI, and status bar visibility. Tasks are ordered by dependency — models first, then services, then UI, then tests.

## Tasks

- [x] 1. Create GameApiConnectionSettings model and extend UIPreferences
  - [x] 1.1 Create GameApiConnectionSettings model
    - Create `OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs` with `ServerUrl` (string, default empty), `PollingIntervalMinutes` (int, default 5), and `Enabled` (bool, default false) properties using Newtonsoft.Json serialization attributes.
    - _Satisfies: Req 10, Criterion 1 ("persist game API settings: server URL, polling interval, enabled/disabled state")_
    - Inputs: design.md data models section
    - Output: `OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs`
    - Verification: getDiagnostics clean compile

  - [x] 1.2 Extend UIPreferences with GameApiConnection section
    - Add `GameApiConnection` property (type `GameApiConnectionSettings`) to the existing `UIPreferences` class with default initialization. Verify that missing section in JSON produces defaults (empty URL, 5 min, disabled) without error.
    - _Satisfies: Req 10, Criteria 1, 3 ("persist in UIPreferences.json under GameApiConnection section", "use default values and continue startup without error")_
    - Inputs: existing UIPreferences.cs, GameApiConnectionSettings model
    - Output: modified UIPreferences.cs
    - Verification: getDiagnostics clean compile; existing preferences tests still pass

- [x] 2. Implement GameApiCredentialManager
  - [x] 2.1 Create GameApiCredentialManager service
    - Create `OE2EmpireTracker.Common/Services/GameApiCredentialManager.cs` with `StoreKey`, `GetKey`, `RemoveKey`, `HasKey`, `GetConfiguredPlayerUUIDs` methods. Uses CredentialStore.Protect/Unprotect for DPAPI. Stores to `%LOCALAPPDATA%\OE2EmpireTracker\game-api-secrets.dat`. Handles corruption by logging, discarding file, and initializing empty.
    - _Satisfies: Req 1, Criteria 1-7 (DPAPI encryption, per-character keying, load on startup, corruption handling, file location, delete on remove, use existing CredentialStore)_
    - Inputs: design.md component 1, existing CredentialStore.cs
    - Output: `OE2EmpireTracker.Common/Services/GameApiCredentialManager.cs`
    - Verification: getDiagnostics clean compile


- [x] 3. Implement GameApiClient - HTTP infrastructure and Polly policies
  - [x] 3.1 Create GameApiClient with Polly resilience
    - Create `OE2EmpireTracker.Common/Client/GameApiClient.cs` with HttpClient, Polly retry policy (exponential backoff 1s/2s/4s, max 3 attempts on HTTP 5xx), circuit breaker (3 consecutive failures, 30s break duration), `CheckHealthAsync`, `GetPlayerProfileAsync` methods, X-API-Key header authentication, and IDisposable cleanup.
    - _Satisfies: Req 2, Criteria 1-6 (separate client, X-API-Key auth, retry policy, circuit breaker, IDisposable, health check method)_
    - Inputs: design.md component 2, existing RemoteFactionClient.cs for pattern reference
    - Output: `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - Verification: getDiagnostics clean compile

- [x] 4. Implement rate limiter and connection monitor
  - [x] 4.1 Add rate limiter logic to GameApiClient
    - Add SemaphoreSlim-based sliding window rate limiter to GameApiClient with configurable requests-per-minute (default 30), HTTP 429 handling (pause for Retry-After duration or 60s fallback), and dynamic limit update from X-RateLimit-Limit response header.
    - _Satisfies: Req 3, Criteria 1-5 (configurable limit, 429 with Retry-After, 429 without Retry-After, dynamic update from header, SemaphoreSlim pattern)_
    - Inputs: GameApiClient.cs, existing RemoteFactionClient rate limiting pattern
    - Output: modified `GameApiClient.cs` (rate limiter methods added)
    - Verification: getDiagnostics clean compile

  - [x] 4.2 Implement GameApiConnectionMonitor
    - Create `OE2EmpireTracker.Common/Services/GameApiConnectionMonitor.cs` with ConnectionState enum (NotConfigured, Connected, Disconnected, DisconnectedCircuitOpen, DisconnectedInvalidKey, Syncing, RateLimited), state machine transitions, exponential backoff reconnection (1s doubling to 60s max), StatusChanged event with exception safety, and `GameApiConnectionStatusChangedEventArgs.cs`.
    - _Satisfies: Req 4, Criteria 1-6 (startup health check within 10s, periodic checks, real-time status, backoff reconnection, event raising with exception safety, circuit breaker state reporting)_
    - Inputs: design.md component 3, GameApiClient.cs
    - Output: `OE2EmpireTracker.Common/Services/GameApiConnectionMonitor.cs`, `OE2EmpireTracker.Common/Client/GameApiConnectionStatusChangedEventArgs.cs`
    - Verification: getDiagnostics clean compile

- [x] 5. Implement GameApiSyncScheduler and profile merge
  - [x] 5.1 Implement GameApiSyncScheduler - timer and round-robin
    - Create `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs` with timer-based polling, round-robin character selection, Start/Stop/UpdatePollingInterval/SyncNowAsync methods, SyncStatusChanged event, and `GameApiSyncStatusChangedEventArgs.cs`. Suspends when disconnected, resumes on reconnect.
    - _Satisfies: Req 5, Criteria 1, 4, 7, 8; Req 9, Criteria 2, 3 (polling at interval, round-robin, skip failed character, manual sync now, suspend when unreachable, resume on reconnect)_
    - Inputs: design.md component 4, GameApiClient.cs, GameApiCredentialManager.cs, GameApiConnectionMonitor.cs
    - Output: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`, `OE2EmpireTracker.Common/Services/GameApiSyncStatusChangedEventArgs.cs`
    - Verification: getDiagnostics clean compile

  - [x] 5.2 Implement Player Profile merge logic and DTO
    - Create `OE2EmpireTracker.Common/Models/GameApiProfileResponse.cs` with DTO classes (GameApiProfileResponse, GameApiSkillResponse, GameApiRanksResponse, GameApiRankResponse) using JsonProperty attributes. Add profile merge method to GameApiSyncScheduler that overwrites game-authoritative fields (Skills, Ranks, SkillPoints, Faction, CitizenId) while preserving local-only fields (SkillGroups), with conflict logging and LastProfileSyncUtc update only on success.
    - _Satisfies: Req 5, Criteria 2, 3, 5, 6; Req 6, Criteria 1, 2, 3 (API wins for game fields, preserve SkillGroups, log conflicts, update LastProfileSyncUtc only on success)_
    - Inputs: GameApiSyncScheduler.cs, existing PlayerProfile model, design.md data models
    - Output: `OE2EmpireTracker.Common/Models/GameApiProfileResponse.cs`, modified `GameApiSyncScheduler.cs`
    - Verification: getDiagnostics clean compile


- [x] 6. Implement GameApiContext singleton and wire into Program.cs
  - [x] 6.1 Implement GameApiContext singleton
    - Create `OE2EmpireTracker.Common/Client/GameApiContext.cs` with Initialize/Reset/Dispose lifecycle, owning CredentialManager, Client, ConnectionMonitor, SyncScheduler, and LastSyncTimes dictionary. Follows startup sequence from design: read settings, check enabled, create components, start monitor and scheduler.
    - _Satisfies: Req 2, Req 4, Req 5, Req 9, Req 10 (top-level coordination, startup sequence, offline suspension, settings loading)_
    - Inputs: design.md component 5, all service/client classes from tasks 2-5
    - Output: `OE2EmpireTracker.Common/Client/GameApiContext.cs`
    - Verification: getDiagnostics clean compile

  - [x] 6.2 Wire GameApiContext initialization into Program.cs and handle HTTP 401
    - Add `GameApiContext.Initialize()` call in Program.cs after ServerContext initialization, and `GameApiContext.Reset()` in shutdown path. Add HTTP 401 handling in GameApiClient: transition to DisconnectedInvalidKey, stop polling for that character, log authentication failure.
    - _Satisfies: Req 9, Criteria 1-5 (offline operation, suspend polling, resume on reconnect, preserve HTML import, 401 handling); Req 10, Criterion 2 (load settings on startup)_
    - Inputs: existing Program.cs, GameApiClient.cs, GameApiConnectionMonitor.cs
    - Output: modified Program.cs, modified GameApiClient.cs
    - Verification: getDiagnostics clean compile

- [x] 7. Add Game API UI integration
  - [x] 7.1 Add Game API tab to Preferences form
    - Add "Game API" tab to FormPreferences with controls: txtGameApiUrl, txtGameApiKey (PasswordChar='*'), nudPollingInterval (min:1, max:60, default:5), chkGameApiEnabled, btnTestConnection, lblTestResult. Implement save logic: persist settings to UIPreferences.GameApiConnection, encrypt API key via CredentialManager, update polling interval on SyncScheduler immediately.
    - _Satisfies: Req 7, Criteria 1-8 (Game API section, masked key, URL input, polling interval, test connection, encrypt on save, apply interval immediately, persist to UIPreferences.json)_
    - Inputs: existing FormPreferences.cs, GameApiCredentialManager, GameApiContext, PreferencesStore
    - Output: modified FormPreferences.cs and FormPreferences.Designer.cs
    - Verification: getDiagnostics clean compile

  - [x] 7.2 Add Game API status to MainWindow status bar
    - Add ToolStripStatusLabel to MainWindow status bar, subscribe to GameApiContext events (ConnectionMonitor.StatusChanged, SyncScheduler.SyncStatusChanged), display connection state and last sync elapsed time. Show failure reason for 30s then revert. Suppress sync updates when not configured.
    - _Satisfies: Req 8, Criteria 1-5 (status states, elapsed time, syncing indicator, failure display for 30s, not-configured suppression)_
    - Inputs: existing MainWindow.cs, GameApiContext, design.md component 7
    - Output: modified MainWindow.cs and MainWindow.Designer.cs
    - Verification: getDiagnostics clean compile


- [x] 8. Tests - Credential Manager and Settings
  - [x] 8.1 Tests - GameApiCredentialManager (Property: Credential Isolation)
    - Write NUnit + FsCheck property tests: store/retrieve round-trip, remove isolation (removing key A doesn't affect key B), corruption recovery (corrupted file to empty state), file location validation. Use FsCheck 2.16.6 LINQ query syntax generators.
    - _Satisfies: Correctness Property 1 (Credential Confidentiality), Correctness Property 7 (Credential Isolation)_
    - Inputs: GameApiCredentialManager.cs, existing CredentialStore test patterns
    - Output: `OE2EmpireTracker.Tests/Services/GameApiCredentialManagerTests.cs`
    - Verification: vstest.console passes all tests

  - [x] 8.2 Tests - Settings persistence round-trip
    - Write NUnit tests: serialize/deserialize round-trip for GameApiConnectionSettings, missing section produces defaults, PascalCase property names in JSON output, integration with UIPreferences save/load.
    - _Satisfies: Req 10, Criteria 1-4 (persistence, load on startup, defaults, serialization pattern)_
    - Inputs: GameApiConnectionSettings.cs, UIPreferences.cs
    - Output: `OE2EmpireTracker.Tests/Services/GameApiSettingsTests.cs`
    - Verification: vstest.console passes all tests

- [x] 9. Tests - Rate limiter and HTTP 429
  - [x] 9.1 Tests - Rate limiter (Property: Rate Limit Compliance)
    - Write NUnit + FsCheck property tests: token exhaustion blocks requests, sliding window resets after time passes, dynamic limit update changes behavior. Use FsCheck 2.16.6 LINQ query syntax.
    - _Satisfies: Correctness Property 2 (Rate Limit Compliance)_
    - Inputs: GameApiClient.cs (rate limiter methods)
    - Output: `OE2EmpireTracker.Tests/Client/GameApiRateLimiterTests.cs`
    - Verification: vstest.console passes all tests

  - [x] 9.2 Tests - HTTP 429 handling
    - Write NUnit tests with mock HttpMessageHandler: 429 with Retry-After header pauses for specified duration, 429 without header pauses 60s, requests resume after pause expires, X-RateLimit-Limit header updates internal limit.
    - _Satisfies: Req 3, Criteria 2, 3, 4 (429 handling with and without Retry-After, dynamic limit update)_
    - Inputs: GameApiClient.cs
    - Output: `OE2EmpireTracker.Tests/Client/GameApiClient429Tests.cs`
    - Verification: vstest.console passes all tests

- [x] 10. Tests - Profile merge and sync scheduling
  - [x] 10.1 Tests - Profile merge (Property: Merge Idempotency)
    - Write NUnit + FsCheck property tests: API-authoritative fields overwritten, local-only fields preserved (SkillGroups), idempotent application (merge twice = same result), conflict logging emitted. Use FsCheck 2.16.6 LINQ query syntax.
    - _Satisfies: Correctness Property 3 (Merge Idempotency)_
    - Inputs: GameApiSyncScheduler.cs (merge method), PlayerProfile model
    - Output: `OE2EmpireTracker.Tests/Services/GameApiProfileMergeTests.cs`
    - Verification: vstest.console passes all tests

  - [x] 10.2 Tests - Round-robin scheduling (Property: Round-Robin Fairness)
    - Write NUnit + FsCheck property tests: each character synced at least floor(N/K) times over N cycles with K characters, failed character skipped and retried next cycle, manual SyncNow syncs all characters.
    - _Satisfies: Correctness Property 6 (Round-Robin Fairness)_
    - Inputs: GameApiSyncScheduler.cs
    - Output: `OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerTests.cs`
    - Verification: vstest.console passes all tests

- [x] 11. Tests - Connection state machine
  - [x] 11.1 Tests - Connection state machine (Property: State Machine Validity)
    - Write NUnit + FsCheck property tests: valid transitions only (no Connected to Connected without Disconnected), backoff doubling (1s to 2s to 4s...to 60s cap), event raised on every transition, exception in handler does not prevent transition.
    - _Satisfies: Correctness Property 5 (State Machine Validity)_
    - Inputs: GameApiConnectionMonitor.cs
    - Output: `OE2EmpireTracker.Tests/Services/GameApiConnectionMonitorTests.cs`
    - Verification: vstest.console passes all tests

## Notes

- All new files go in `OE2EmpireTracker.Common/` (shared library) except Forms (main project) and Tests (test project)
- No new NuGet packages required — Polly 7.2.4, Newtonsoft.Json 13.0.4, and NLog 5.3.4 are already available
- FsCheck property tests must use 2.16.6 APIs: LINQ query syntax generators, `[FsCheck.NUnit.Property]` attribute, no FsCheck.Fluent namespace
- The design references `OE2EmpireTracker.Common` for service/client files — verify this project exists and is referenced by the main project before implementation
- HTML import functionality (Req 9.4) requires no changes — it already works independently of API connectivity
