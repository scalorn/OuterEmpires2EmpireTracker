# Requirements Document

## Introduction

Replace the current sequential background sync with queue-based parallel dispatch using the proven GameApiRequestQueue infrastructure. Integrate crate, survey, and blueprint imports into the sync cycle, mapping new API fields to existing data model properties, and adding SystemObjectId tracking to Survey and Asteroid entities.

## Glossary

- **Queue_Sync_Service**: The new service that orchestrates background API synchronization using GameApiRequestQueue for parallel dispatch with rate governing.
- **GameApiRequestQueue**: The existing general-purpose parallel task queue with token-bucket rate governing, inflight concurrency limiting, retry support, and metrics CSV output.
- **WorkItem**: A unit of work dispatched by GameApiRequestQueue, consisting of a label and an async delegate that may return cascaded follow-up items.
- **Cascading**: The pattern where a list-level API call produces detail-level follow-up WorkItems (e.g., asset locations list → individual crate/survey/blueprint detail calls).
- **TPS**: Transactions per second — the rate at which the queue dispatches work items.
- **Inflight_Cap**: Maximum number of concurrent in-flight requests (TPS × 3 = 30 at TPS=10).
- **Token_Refresh**: The process of exchanging a refresh token for a new access token when the current token expires (HTTP 401).
- **CrateImporter**: The existing service that imports blueprints from crate JSON data into the player/empire context.
- **SurveyImportHelper**: The existing service that creates or merges survey data into the player context.
- **SystemObjectId**: An integer identifier from the game API representing the system object (planet/asteroid) that was surveyed.
- **IconClass_Mapping**: The transformation from API `partTypeIcon` (e.g., "A22") to the stored `_IconClass` property format with "ui_icon_" prefix (e.g., "ui_icon_A22").

## Requirements

### Requirement 1: Queue Sync Service Creation

**User Story:** As a developer, I want a new Queue_Sync_Service that replaces sequential API calls with parallel queue-based dispatch, so that background sync achieves maximum throughput.

#### Acceptance Criteria

1. THE Queue_Sync_Service SHALL use GameApiRequestQueue with TPS=10 as its dispatch engine.
2. THE Queue_Sync_Service SHALL configure an inflight cap of 30 (TPS × 3) to limit concurrent requests.
3. THE Queue_Sync_Service SHALL configure a maximum of 3 retry attempts per failed work item.
4. WHEN the Queue_Sync_Service starts a sync cycle, THE Queue_Sync_Service SHALL enqueue seed work items for all supported API endpoint categories.
5. THE Queue_Sync_Service SHALL call Start on the queue and await DrainAsync to complete the sync cycle.


### Requirement 2: Seed Work Items and Cascading

**User Story:** As a developer, I want the sync cycle to enqueue seed work items that cascade into detail calls, so that all data is fetched using the same list-then-detail pattern proven in the discovery test.

#### Acceptance Criteria

1. WHEN a sync cycle begins, THE Queue_Sync_Service SHALL enqueue seed work items for: character profile, character skills, colony list, banking balance, banking transactions, asset locations, accepted jobs, kill mail list, mail list, ship configuration, ship cargo, market listings, market items, market buy orders, and market sell orders.
2. WHEN the colony list response is received and contains one or more colonies, THE Queue_Sync_Service SHALL cascade one work item per colony for each of: colony summary, colony buildings, colony warehouse, and colony workers.
3. WHEN the asset locations response is received, THE Queue_Sync_Service SHALL cascade one detail work item per asset location.
4. WHEN an asset location detail response contains one or more crate items, THE Queue_Sync_Service SHALL cascade one work item per crate for crate detail retrieval.
5. WHEN an asset location detail response contains one or more survey items, THE Queue_Sync_Service SHALL cascade one work item per survey for survey detail retrieval.
6. WHEN an asset location detail response contains one or more blueprint items, THE Queue_Sync_Service SHALL cascade one work item per blueprint for blueprint detail retrieval.
7. WHEN the kill mail list response is received, THE Queue_Sync_Service SHALL cascade one detail work item per kill mail.
8. WHEN the mail list response is received, THE Queue_Sync_Service SHALL cascade one detail work item per mail and one work item for the next page if the current page is non-empty.


### Requirement 3: Crate Import Integration

**User Story:** As a player, I want crate contents (blueprints) to be automatically imported during background sync, so that my blueprint collection stays up to date without manual file import.

#### Acceptance Criteria

1. WHEN a crate detail response is received, THE Queue_Sync_Service SHALL parse the crate JSON and pass it to CrateImporter.ImportFromJson for blueprint extraction.
2. THE Queue_Sync_Service SHALL provide the current PlayerContext and EmpireContext to CrateImporter during import.
3. IF a crate detail request fails after 3 retries, THEN THE Queue_Sync_Service SHALL log each individual failure attempt during the retry process and continue processing other work items.

### Requirement 4: Survey Import Integration

**User Story:** As a player, I want survey data to be automatically imported during background sync, so that my survey collection reflects the latest scan results without manual import.

#### Acceptance Criteria

1. WHEN a survey detail response is received, THE Queue_Sync_Service SHALL parse the GameApiSurveyResponse and construct a temporary Survey object from the response data.
2. THE Queue_Sync_Service SHALL use SurveyImportHelper.FindByKey to check for an existing survey match.
3. WHEN a matching survey exists, THE Queue_Sync_Service SHALL use SurveyImportHelper.MergeData to update it.
4. WHEN no matching survey exists, THE Queue_Sync_Service SHALL use SurveyImportHelper.CreateFromTemp to create a new survey record.
5. THE Queue_Sync_Service SHALL call SurveyImportHelper.LinkOrCreateAsteroid for asteroid-type surveys.
6. WHEN the survey response contains a SystemObjectId, THE Queue_Sync_Service SHALL store the SystemObjectId on the Survey entity.


### Requirement 5: Blueprint Import Integration

**User Story:** As a player, I want blueprint details to be automatically imported during background sync, so that my blueprint collection includes full resource and property data.

#### Acceptance Criteria

1. WHEN a blueprint detail response is received, THE Queue_Sync_Service SHALL parse the GameApiBlueprintDetailResponse and construct a JSON object compatible with CrateImporter format.
2. THE Queue_Sync_Service SHALL map the API field `partTypeIcon` to the `_IconClass` property using the "ui_icon_" prefix (e.g., API value "A22" maps to stored value "ui_icon_A22").
3. THE Queue_Sync_Service SHALL pass the constructed JSON to CrateImporter.ImportFromJson for deduplication and storage.
4. IF a blueprint detail request fails after 3 retries, THEN THE Queue_Sync_Service SHALL log immediately when the retry limit is reached and continue processing other work items.

### Requirement 6: SystemObjectId Model Extension

**User Story:** As a developer, I want Survey and Asteroid entities to store a SystemObjectId, so that the application can track the game API's integer identifier for surveyed celestial objects.

#### Acceptance Criteria

1. THE Survey model SHALL include a SystemObjectId property of type int with a default value of 0.
2. THE Asteroid model SHALL include a SystemObjectId property of type int with a default value of 0.
3. THE SystemObjectId property SHALL be serialized to JSON using Newtonsoft.Json conventions (PascalCase key "SystemObjectId").
4. WHEN a survey is imported with a non-zero SystemObjectId, THE Queue_Sync_Service SHALL propagate the SystemObjectId to the linked Asteroid entity.


### Requirement 7: Token Refresh on 401

**User Story:** As a player, I want the sync to automatically refresh my access token when it expires, so that long sync cycles complete without manual re-authentication.

#### Acceptance Criteria

1. WHEN a work item receives an HTTP 401 response, THE Queue_Sync_Service SHALL attempt to exchange the refresh token for a new access token using GameApiClient.ExchangeTokenAsync.
2. WHEN the token refresh succeeds, THE Queue_Sync_Service SHALL update the stored access token regardless of re-enqueue outcome and re-enqueue the failed work item for retry.
3. IF the token refresh fails, THEN THE Queue_Sync_Service SHALL log the failure and mark the work item as failed without further retry.
4. THE Queue_Sync_Service SHALL serialize token refresh attempts so that only one refresh occurs at a time, and new 401 responses SHALL wait for any in-progress refresh attempt to fully complete before triggering another.

### Requirement 8: Metrics CSV Output

**User Story:** As a developer, I want the sync cycle to write a metrics CSV file, so that I can monitor throughput, success rates, and latency.

#### Acceptance Criteria

1. THE Queue_Sync_Service SHALL pass a metrics file path to GameApiRequestQueue at construction.
2. THE GameApiRequestQueue SHALL write one CSV row per work item execution attempt with columns: Timestamp, Label, Status, DurationMs, Attempt, Inflight, EffectiveTps.
3. WHEN the sync cycle completes, THE Queue_Sync_Service SHALL log a summary with total succeeded, total failed, and elapsed time.


### Requirement 9: Integration with Existing Sync Lifecycle

**User Story:** As a developer, I want the queue-based sync to integrate with the existing BackgroundProcessor timer cycle, so that sync runs automatically at the configured polling interval.

#### Acceptance Criteria

1. WHEN the BackgroundProcessor timer fires and Game API sync is enabled, THE BackgroundProcessor SHALL invoke Queue_Sync_Service to run a sync cycle.
2. THE Queue_Sync_Service SHALL read TPS, AppId, and access token from GameApiConnectionSettings.
3. WHILE a sync cycle is in progress, THE Queue_Sync_Service SHALL prevent a second concurrent sync cycle from starting.
4. WHEN the sync cycle completes, THE Queue_Sync_Service SHALL persist any modified player data via PlayerContext.WriteContext.

### Requirement 10: 429 Rate Limit Handling

**User Story:** As a developer, I want the queue to respect server rate limit responses, so that the sync does not overload the game API.

#### Acceptance Criteria

1. WHEN a work item receives an HTTP 429 response, THE Queue_Sync_Service SHALL call GameApiRequestQueue.NotifyRateLimited with the Retry-After header value.
2. WHILE the queue is in a rate-limited pause state, THE GameApiRequestQueue SHALL not dispatch new work items.
3. WHEN the pause period expires, THE GameApiRequestQueue SHALL resume dispatch at 50 percent of the prior effective TPS.
4. THE GameApiRequestQueue SHALL gradually recover TPS at a rate of 10 percent per minute after actual dispatches resume successfully.


### Requirement 11: IconClass Mapping for Blueprints

**User Story:** As a player, I want imported blueprints to have the correct icon class stored, so that the UI displays the correct part type icon.

#### Acceptance Criteria

1. WHEN a blueprint detail response contains a partTypeIcon value, THE Queue_Sync_Service SHALL map it to the _IconClass property by prepending "ui_icon_" (e.g., "A22" becomes "ui_icon_A22").
2. WHEN a blueprint detail response has an empty or null partTypeIcon, THE Queue_Sync_Service SHALL not set the _IconClass property.
3. THE mapped _IconClass value SHALL be stored in the blueprint's PropertyBag using the key "_IconClass".

### Requirement 12: Error Isolation and Resilience

**User Story:** As a developer, I want individual work item failures to not affect other items in the sync cycle, so that a single API error does not abort the entire sync.

#### Acceptance Criteria

1. IF a work item throws an exception, THEN THE GameApiRequestQueue SHALL record the error, mark the item as failed, and continue dispatching remaining items.
2. THE Queue_Sync_Service SHALL log all failed work items with their labels and exception messages at the end of a sync cycle.
3. WHEN the sync cycle completes with failures, THE Queue_Sync_Service SHALL report the count of succeeded and failed items but not treat partial failure as a sync cycle failure.
4. THE Queue_Sync_Service SHALL not persist data from failed individual imports (failed crate/survey/blueprint items are skipped, not half-written).



### Requirement 13: Game API Detail ID Tracking

**User Story:** As a developer, I want each imported survey and blueprint to record its game API detail ID, so that the system can identify which items have already been imported and avoid redundant detail fetches.

#### Acceptance Criteria

1. THE Blueprint model SHALL include a GameApiBlueprintId property of type int? (nullable) to store the blueprint detail ID from the API (GameApiBlueprintInfo.Id field).
2. THE Survey model SHALL include a GameApiSurveyId property of type int? (nullable) to store the survey detail ID from the API (GameApiSurveyDetail.Id field).
3. WHEN a blueprint is imported from the API, THE Queue_Sync_Service SHALL store the API blueprint Id in GameApiBlueprintId.
4. WHEN a survey is imported from the API, THE Queue_Sync_Service SHALL store the API survey Id in GameApiSurveyId.
5. THE GameApiBlueprintId and GameApiSurveyId SHALL be distinct from the existing GameItemId property (which represents the cargo item ID in an asset location, not the detail entity ID).

### Requirement 14: Import Freshness and Skip Logic

**User Story:** As a player, I want the sync to skip re-importing surveys and blueprints that were recently imported, so that slow API endpoints are only called when data is stale (older than 1 day).

#### Acceptance Criteria

1. THE Blueprint model SHALL include a LastDetailImportUtc property of type DateTime? (nullable) recording when the blueprint detail was last successfully imported from the API.
2. THE Survey model SHALL include a LastDetailImportUtc property of type DateTime? (nullable) recording when the survey detail was last successfully imported from the API.
3. WHEN cascading to a survey detail work item, THE Queue_Sync_Service SHALL check if a survey with the matching GameApiSurveyId already exists AND has a LastDetailImportUtc within the last 24 hours; IF SO, THE Queue_Sync_Service SHALL skip the detail fetch and not enqueue the work item.
4. WHEN cascading to a blueprint detail work item, THE Queue_Sync_Service SHALL check if a blueprint with the matching GameApiBlueprintId already exists AND has a LastDetailImportUtc within the last 24 hours; IF SO, THE Queue_Sync_Service SHALL skip the detail fetch and not enqueue the work item.
5. WHEN a detail import succeeds, THE Queue_Sync_Service SHALL update LastDetailImportUtc to SystemClock.UtcNow on the imported entity.
6. THE 24-hour threshold SHALL be configurable but default to 24 hours.

### Requirement 15: In-Memory Indexes for Game API IDs

**User Story:** As a developer, I want fast O(1) lookups from game API IDs to domain entities, so that the sync service can quickly determine if an item already exists without iterating all items.

#### Acceptance Criteria

1. THE PlayerContext SHALL maintain a Dictionary<int, Blueprint> index mapping GameApiBlueprintId to Blueprint for all blueprints with a non-null GameApiBlueprintId.
2. THE PlayerContext SHALL maintain a Dictionary<int, Survey> index mapping GameApiSurveyId to Survey for all surveys with a non-null GameApiSurveyId.
3. WHEN a blueprint is added or updated with a GameApiBlueprintId, THE PlayerContext SHALL update the blueprint index.
4. WHEN a survey is added or updated with a GameApiSurveyId, THE PlayerContext SHALL update the survey index.
5. WHEN a blueprint or survey is removed, THE PlayerContext SHALL remove its entry from the corresponding index.
6. THE indexes SHALL be rebuilt from persisted data on PlayerContext initialization (load from disk).
7. THE Queue_Sync_Service SHALL use these indexes for the freshness check in Requirement 14 instead of iterating all items.


### Requirement 16: Detail Refresh Interval Preference

**User Story:** As a player, I want to configure how often surveys and blueprints are re-imported from the API, so that I can balance freshness against API call volume.

#### Acceptance Criteria

1. THE GameApiConnectionSettings model SHALL include a DetailRefreshHours property of type int with a default value of 24.
2. THE Preferences Form SHALL display a numeric input labeled "Detail Refresh (hours)" on the Game API connection settings tab, accepting integer values between 1 and 168 (1 hour to 7 days).
3. WHEN the user changes the Detail Refresh value and clicks OK, THE Preferences Form SHALL persist the value to GameApiConnectionSettings.
4. THE Queue_Sync_Service SHALL read DetailRefreshHours from GameApiConnectionSettings when evaluating import freshness (Requirement 14).
5. THE DetailRefreshHours setting SHALL apply equally to both survey and blueprint detail imports.
