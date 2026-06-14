# Requirements Document

## Introduction

Migrate all production consumers from the legacy `GameApiClient` (which returns raw JSON tuples `(bool Success, string Json)`) to the validated NSwag-generated `GameApiTypedClient` (which implements `IGameApiTypedClient` and returns strongly-typed DTOs). After migration, remove the old client class and all hand-written response models. All existing functionality must continue to work identically after migration.

## Glossary

- **Typed_Client**: The `GameApiTypedClient` class implementing `IGameApiTypedClient`, using NSwag-generated DTOs with built-in resilience and rate limiting.
- **Old_Client**: The legacy `GameApiClient` class that returns `(bool Success, string Json)` tuples requiring manual JSON deserialization.
- **QueueSyncService**: The central orchestrator for queue-based background sync that creates work items for all API endpoints.
- **TokenRefreshHandler**: The serialized token refresh component that handles 401 responses by re-exchanging credentials.
- **BankingService**: The static service that imports banking balance and transactions from the game API.
- **MailService**: The static service that performs incremental mail sync from the game API.
- **GameApiConnectionMonitor**: The connectivity monitor that polls via periodic token exchange attempts.
- **GameApiContext**: The top-level singleton that creates and holds all game API component instances.
- **GameApiSyncScheduler**: The legacy sequential sync scheduler (superseded by QueueSyncService but still referenced).
- **Merge_Service**: Any of the data merge services (ProfileMergeService, ColonyMergeService, AssetMergeService, BlueprintLinkageService, SurveyLinkageService) that reconcile API responses with local data.
- **Hand_Written_Model**: Any of the manually-created response POCOs (GameApiProfileResponse, GameApiColonyListResponse, etc.) used by old-client consumers for JSON deserialization.
- **Generated_DTO**: The NSwag-generated data transfer objects (PublicCharacter, ColonyList, BankingBalance, etc.) returned by the Typed_Client.
- **ApiHttpException**: The typed exception thrown by the Typed_Client for HTTP-level errors (401, 403, 429, 5xx).
- **ApiBusinessException**: The typed exception thrown by the Typed_Client when the API envelope indicates a business logic failure.
- **TokenBucketRateLimiter**: The rate limiter internal to the Typed_Client that controls request throughput.

## Requirements

### Requirement 1: GameApiContext Migration

**User Story:** As a developer, I want GameApiContext to create and hold the Typed_Client instead of the Old_Client, so that all downstream consumers use the strongly-typed interface.

#### Acceptance Criteria

1. WHEN GameApiContext initializes, THE GameApiContext SHALL create a GameApiTypedClient instance configured with the server URL and AppId from settings.
2. WHEN GameApiContext initializes, THE GameApiContext SHALL expose the Typed_Client via a property typed as IGameApiTypedClient.
3. WHEN GameApiContext initializes, THE GameApiContext SHALL configure the TokenBucketRateLimiter with the TPS value from GameApiConnectionSettings.
4. THE GameApiContext SHALL remove the GameApiClient property and cease creating Old_Client instances.
5. WHEN GameApiContext is disposed, THE GameApiContext SHALL dispose the Typed_Client.

### Requirement 2: QueueSyncService Migration

**User Story:** As a developer, I want QueueSyncService to use IGameApiTypedClient for all API calls, so that work item bodies use typed DTOs instead of raw JSON parsing.

#### Acceptance Criteria

1. THE QueueSyncService SHALL accept IGameApiTypedClient as a constructor dependency instead of GameApiClient.
2. WHEN a sync cycle starts, THE QueueSyncService SHALL call IGameApiTypedClient.ExchangeTokenAsync for token acquisition instead of GameApiClient.ExchangeTokenAsync.
3. WHEN a work item executes an API call, THE QueueSyncService SHALL call the corresponding IGameApiTypedClient method and receive a Generated_DTO directly.
4. IF an API call encounters StatusCode 401 (whether via ApiHttpException or HTTP response), THEN THE QueueSyncService SHALL trigger token refresh via the TokenRefreshHandler and block retry until token refresh succeeds before retrying the work item.
5. IF an API call encounters StatusCode 429 (whether via ApiHttpException or HTTP response), THEN THE QueueSyncService SHALL call GameApiRequestQueue.NotifyRateLimited and retry the work item following its general retry policy.
6. THE QueueSyncService SHALL remove all manual JsonConvert.DeserializeObject calls and GameApiServiceResponse envelope unwrapping from work item bodies.

### Requirement 3: TokenRefreshHandler Migration

**User Story:** As a developer, I want TokenRefreshHandler to use IGameApiTypedClient for token exchange, so that the refresh mechanism works with the typed client's token management.

#### Acceptance Criteria

1. THE TokenRefreshHandler SHALL accept IGameApiTypedClient as a constructor dependency instead of GameApiClient.
2. WHEN HandleUnauthorizedAsync is called, THE TokenRefreshHandler SHALL always call IGameApiTypedClient.ExchangeTokenAsync to obtain a new access token regardless of current token state (null token or missing refresh token).
3. THE TokenRefreshHandler SHALL remove the call to GameApiClient.InvalidateToken since the Typed_Client manages its own token cache.
4. WHEN ExchangeTokenAsync succeeds, THE TokenRefreshHandler SHALL extract the new AccessToken from the TokenResponseDto and update its internal state.

### Requirement 4: BankingService Migration

**User Story:** As a developer, I want BankingService to use IGameApiTypedClient for banking API calls, so that balance and transaction imports use typed DTOs without manual JSON parsing.

#### Acceptance Criteria

1. THE BankingService.ImportTransactionsAsync SHALL accept IGameApiTypedClient as a parameter instead of GameApiClient.
2. WHEN importing transactions, THE BankingService SHALL call IGameApiTypedClient.GetBankingTransactionsAsync and receive a BankingTransactions DTO directly.
3. THE BankingService SHALL completely remove all JArray/JObject manual parsing and GameApiServiceResponse envelope unwrapping from ImportTransactionsAsync; no method SHALL contain both typed DTO calls and manual parsing code.
4. IF GetBankingTransactionsAsync throws ApiHttpException with StatusCode 401, THEN THE BankingService SHALL re-read the token accessor and retry once.
5. THE BankingService.ImportBalanceAsync SHALL accept IGameApiTypedClient as a parameter instead of GameApiClient.
6. WHEN importing balance, THE BankingService SHALL call IGameApiTypedClient.GetBankingBalanceAsync and receive a BankingBalance DTO directly.

### Requirement 5: MailService Migration

**User Story:** As a developer, I want MailService to use IGameApiTypedClient for mail API calls, so that mail sync uses typed DTOs without manual JSON parsing.

#### Acceptance Criteria

1. THE MailService.SyncMailAsync SHALL accept IGameApiTypedClient as a parameter instead of GameApiClient.
2. WHEN fetching the mail list, THE MailService SHALL call IGameApiTypedClient.GetMailListAsync and receive a MailList DTO directly.
3. WHEN fetching mail detail, THE MailService SHALL call IGameApiTypedClient.GetMailBodyAsync and receive a MailBody DTO directly.
4. THE MailService SHALL completely remove all JObject.Parse and manual JSON extraction from SyncMailAsync; no code paths SHALL retain manual JSON parsing.
5. IF GetMailListAsync throws ApiHttpException with StatusCode 401 or 403, THEN THE MailService SHALL return -1 indicating authentication failure. IF both authentication failure (401/403) and rate limiting (429) occur, authentication failure takes precedence.
6. IF GetMailListAsync throws ApiHttpException with StatusCode 429, THEN THE MailService SHALL return -2 indicating rate limiting (distinct from authentication failure code -1).

### Requirement 6: GameApiConnectionMonitor Migration

**User Story:** As a developer, I want GameApiConnectionMonitor to use IGameApiTypedClient for connectivity checks, so that the monitor leverages the typed client's token exchange.

#### Acceptance Criteria

1. THE GameApiConnectionMonitor SHALL accept IGameApiTypedClient as a constructor dependency instead of GameApiClient.
2. WHEN performing a connectivity check, THE GameApiConnectionMonitor SHALL call IGameApiTypedClient.TestConnectionAsync.
3. WHEN TestConnectionAsync returns true, THE GameApiConnectionMonitor SHALL transition to Connected state (including transitioning out of DisconnectedInvalidKey if previously in that state).
4. IF TestConnectionAsync throws ApiHttpException with StatusCode 401, THEN THE GameApiConnectionMonitor SHALL transition to DisconnectedInvalidKey state regardless of whether the method also returns a value (exception takes precedence).
5. THE GameApiConnectionMonitor SHALL check IGameApiTypedClient.IsCircuitOpen to determine if the circuit breaker is blocking requests.

### Requirement 7: Merge Service Adapter Layer

**User Story:** As a developer, I want merge services to accept Generated_DTOs from the Typed_Client, so that the data reconciliation pipeline works without hand-written response models.

#### Acceptance Criteria

1. THE ProfileMergeService.MergeProfileData SHALL accept the Generated_DTO PublicCharacter instead of GameApiProfileResponse.
2. THE ColonyMergeService.MergeColonyList SHALL accept the Generated_DTO ColonyList instead of GameApiColonyListResponse.
3. THE ColonyMergeService.MergeBuildings SHALL accept the Generated_DTO ColonyBuildings instead of the hand-written buildings response.
4. THE ColonyMergeService.MergeWarehouse SHALL accept the Generated_DTO ColonyWarehouse instead of the hand-written warehouse response.
5. THE ColonyMergeService.MergeWorkers SHALL accept the Generated_DTO ColonyWorkers instead of the hand-written workers response.
6. THE AssetMergeService SHALL accept Generated_DTOs (AssetLocations, AssetLocationDetail, AssetCrateContents) instead of hand-written asset response models.
7. THE BlueprintLinkageService SHALL accept the Generated_DTO AssetBlueprint instead of GameApiBlueprintDetailResponse.
8. THE SurveyLinkageService SHALL accept the Generated_DTO AssetSurvey instead of the hand-written survey response model.
9. WHEN a merge service receives a Generated_DTO, THE Merge_Service SHALL map DTO fields to local model fields using the same semantic logic as the current hand-written model mapping. IF mapping fails or mapping rules are missing at runtime, THE Merge_Service SHALL reject the merge operation and return an error rather than applying partial data.

### Requirement 8: Rate Limiter Consolidation

**User Story:** As a developer, I want a single authoritative rate limiter, so that request throughput is controlled consistently without redundant semaphore contention.

#### Acceptance Criteria

1. THE Typed_Client's TokenBucketRateLimiter SHALL be the authoritative rate limiter for all API requests.
2. WHEN GameApiContext initializes, THE GameApiContext SHALL configure the TokenBucketRateLimiter rate from GameApiConnectionSettings.Tps.
3. THE GameApiRequestQueue's TokenBucketGovernor SHALL remain configured as a pass-through (high TPS) for dispatch pacing only.
4. THE Old_Client's SemaphoreSlim-based rate limiter SHALL be removed atomically with the Old_Client class; no transition period where both rate limiting systems coexist is permitted.
5. THE GameApiRequestQueue SHALL continue to use NotifyRateLimited for HTTP 429 pause behavior independent of the client rate limiter.

### Requirement 9: Error Handling Migration

**User Story:** As a developer, I want all consumers to handle typed exceptions from the Typed_Client, so that error handling is consistent and explicit rather than relying on string comparisons.

#### Acceptance Criteria

1. WHEN an API call throws ApiHttpException with StatusCode 401, THE consumer SHALL treat the error as an authentication failure (equivalent to old `result.Json == "401"` check).
2. WHEN an API call throws ApiHttpException with StatusCode 429, THE consumer SHALL treat the error as a rate limit response (equivalent to old `result.Json == "429"` check).
3. WHEN an API call throws ApiHttpException with StatusCode in the 5xx range, THE consumer SHALL treat the error as a transient server failure.
4. WHEN an API call throws ApiBusinessException, THE consumer SHALL log the business error and treat the call as failed without retry.
5. IF the Typed_Client's circuit breaker is open, THE consumer SHALL log the circuit-open state whenever it detects the open condition (regardless of whether a BrokenCircuitException is thrown). IF a call is attempted while the circuit breaker is open and BrokenCircuitException is thrown, THE consumer SHALL catch it and log the circuit-open state.
6. THE consumers SHALL remove all `if (!result.Success)` and `result.Json == "401"` string-comparison error handling patterns.

### Requirement 10: Legacy Scheduler Migration

**User Story:** As a developer, I want GameApiSyncScheduler and ProductionSyncScheduler to use IGameApiTypedClient, so that the legacy scheduler code (still referenced by GameApiContext) operates through the typed interface.

#### Acceptance Criteria

1. THE GameApiSyncScheduler SHALL accept IGameApiTypedClient as a constructor dependency instead of GameApiClient.
2. WHEN the legacy scheduler performs token exchange, THE GameApiSyncScheduler SHALL call IGameApiTypedClient.ExchangeTokenAsync.
3. WHEN the legacy scheduler fetches profile data, THE GameApiSyncScheduler SHALL call IGameApiTypedClient.GetCharacterAsync and receive a PublicCharacter DTO.
4. THE ProductionSyncScheduler SHALL pass IGameApiTypedClient through to the base class constructor.

### Requirement 11: Old Client Removal

**User Story:** As a developer, I want the old GameApiClient class removed from the codebase, so that there is a single API client implementation with no dead code.

#### Acceptance Criteria

1. WHEN all consumers have been migrated to IGameApiTypedClient, THE codebase SHALL delete the GameApiClient.cs file.
2. WHEN all consumers have been migrated, THE codebase SHALL delete all hand-written response model files (GameApiProfileResponse, GameApiColonyListResponse, GameApiColonyResponse, GameApiAssetLocationsResponse, GameApiAssetDetailResponse, GameApiBlueprintDetailResponse, GameApiSurveyDetail, and related types).
3. WHEN all consumers have been migrated, THE codebase SHALL remove the GameApiServiceResponse envelope class since the Typed_Client handles envelope unwrapping internally.
4. THE codebase SHALL retain no compile-time references to GameApiClient or Hand_Written_Models after deletion.
5. THE codebase SHALL continue to compile with zero errors and zero warnings immediately after removal with no allowance for temporary compilation failures during the deletion process.

### Requirement 12: Test Migration

**User Story:** As a developer, I want all tests that use the old GameApiClient (typically via HttpListener stubs) to be updated to use IGameApiTypedClient, so that tests validate the new code paths.

#### Acceptance Criteria

1. WHEN a test previously constructed a GameApiClient against an HttpListener, THE test SHALL instead construct a GameApiTypedClient (or mock IGameApiTypedClient) against the same test infrastructure.
2. THE test suite SHALL maintain equivalent coverage: every endpoint call and error scenario tested against the Old_Client SHALL have a corresponding test against the Typed_Client.
3. THE merge service tests (ProfileMergeServiceTests, ColonyMergeServiceTests, etc.) SHALL be updated to use Generated_DTOs as inputs instead of Hand_Written_Models.
4. THE QueueSyncService property tests SHALL be updated to verify typed exception handling (ApiHttpException catch) instead of `result.Json == "401"` patterns.
5. WHEN all test migrations are complete, THE full test suite SHALL pass with zero failures.

### Requirement 13: Behavioral Equivalence

**User Story:** As a developer, I want the migrated system to behave identically to the current system, so that no user-visible functionality is lost or changed during migration.

#### Acceptance Criteria

1. THE QueueSyncService SHALL produce the same sync results (succeeded/failed counts, merged data) when calling Typed_Client methods as it did when calling Old_Client methods with the same server responses.
2. THE BankingService SHALL import the same transactions (same deduplication logic, same pagination stop conditions) when using Typed_Client responses as it did with raw JSON.
3. THE MailService SHALL sync the same messages (same incremental logic, same high-water-mark detection) when using Typed_Client responses as it did with raw JSON.
4. THE merge services SHALL produce the same local data state when given semantically equivalent Generated_DTOs as they did with Hand_Written_Models.
5. THE GameApiConnectionMonitor SHALL produce the same state transitions (Connected, Disconnected, DisconnectedInvalidKey, DisconnectedCircuitOpen) as it did with the Old_Client.
6. THE TokenRefreshHandler SHALL maintain the same serialized-refresh semantics (single-flight, stale-check optimization) after migration.

### Requirement 14: Token Management Simplification

**User Story:** As a developer, I want to leverage the Typed_Client's internal token management, so that consumers no longer need to pass explicit accessToken parameters or maintain their own token state.

#### Acceptance Criteria

1. WHEN the Typed_Client is authenticated (token exchanged successfully), THE Typed_Client SHALL automatically attach the Bearer token to subsequent API requests.
2. THE QueueSyncService SHALL exchange the token once at sync-cycle start via IGameApiTypedClient.ExchangeTokenAsync and rely on the client's cached token for all work item calls.
3. THE BankingService SHALL not require an explicit accessToken parameter since the Typed_Client manages token state internally.
4. THE MailService SHALL not require an explicit accessToken parameter since the Typed_Client manages token state internally.
5. WHEN a 401 is received mid-sync, THE TokenRefreshHandler SHALL call ExchangeTokenAsync on the Typed_Client to refresh the token, and the Typed_Client SHALL cache the new token for subsequent calls.
