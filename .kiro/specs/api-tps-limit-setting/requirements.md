# Requirements Document

## Introduction

Add a parallel request queue system (`GameApiRequestQueue`) to enable high-throughput API data fetching. The system manages concurrent in-flight requests governed by a configurable TPS (transactions per second) rate, supports cascading work (results from one request spawn follow-up requests), and self-limits based on what is discovered. The discovery test fixture is the first consumer, replacing its sequential iteration with queue-based parallel fetching.

A TPS setting is added to the Game API integration settings tab so users can control throughput. The queue is designed as a general-purpose parallel task processor that can later be used by the sync scheduler, colony background processing, and other subsystems.

## Glossary

- **TPS_Setting**: The user-facing numeric input representing transactions per second for the Game API rate limiter.
- **GameApiConnectionSettings**: The persisted model holding all Game API integration configuration (server URL, app ID, client ID, polling interval, and now TPS).
- **GameApiClient**: The HTTP client that communicates with the OE2 Public API.
- **GameApiRequestQueue**: A new general-purpose parallel task queue service that manages concurrent work items with rate-limited throughput.
- **Work_Item**: A unit of work submitted to the queue — typically an async delegate that returns a result and optionally spawns follow-up work items.
- **Rate_Governor**: The component within GameApiRequestQueue that ensures work items are dispatched at no more than the configured TPS.
- **Cascading_Work**: A pattern where completing one work item produces new work items to enqueue (e.g., fetching asset locations produces individual crate/survey/blueprint fetch requests).
- **Preferences_Form**: The FormPreferences dialog containing the Game API connection settings tab.

## Requirements

### Requirement 1: Persist TPS Setting

**User Story:** As a user, I want my TPS rate limit setting saved with my other Game API connection settings, so that it persists across application restarts.

#### Acceptance Criteria

1. THE GameApiConnectionSettings SHALL include a Tps property of type double with a default value of 0.5.
2. WHEN the GameApiConnectionSettings is serialized to JSON, THE Tps property SHALL be persisted using PascalCase key naming (consistent with existing properties such as ServerUrl and PollingIntervalMinutes).
3. WHEN the GameApiConnectionSettings is deserialized from JSON that does not contain a Tps key, THE GameApiConnectionSettings SHALL initialize Tps to the default value of 0.5.
4. WHEN the GameApiConnectionSettings is deserialized from JSON that contains a Tps key with a numeric value, THE GameApiConnectionSettings SHALL populate the Tps property with the stored value.

### Requirement 2: Display TPS Setting in Preferences

**User Story:** As a user, I want to see and modify the TPS rate limit on the Game API connection settings tab, so that I can control how fast the app calls the API.

#### Acceptance Criteria

1. THE Preferences_Form SHALL display a numeric input labeled "TPS Limit" on the Game API connection settings tab, accepting values with up to 1 decimal place and a step increment of 0.1, AND THE input SHALL both display correctly and validate decimal handling (display without validation is insufficient).
2. WHEN the Game API connection settings tab is opened, THE TPS_Setting input SHALL display the current persisted TPS value from GameApiConnectionSettings.
3. WHEN the user clicks OK on the Preferences_Form, THE Preferences_Form SHALL persist the current TPS_Setting input value to GameApiConnectionSettings.
4. THE TPS_Setting input SHALL accept decimal values with up to 1 decimal place (e.g. 0.5, 1.0, 2.0).
5. IF the user enters a non-numeric value or a negative value into the TPS_Setting input, THEN THE Preferences_Form SHALL reject the input and retain the previous valid value.

### Requirement 3: Validate TPS Input

**User Story:** As a user, I want the application to prevent invalid TPS values, so that the rate limiter always operates with a safe configuration.

#### Acceptance Criteria

1. THE TPS_Setting input SHALL enforce a minimum value of 0.1 TPS.
2. THE TPS_Setting input SHALL enforce a maximum value of 100.0 TPS.
3. WHEN the user leaves the TPS_Setting input field (loses focus), IF the current value is numeric but outside the valid range, THEN THE Preferences_Form SHALL clamp the value to the nearest boundary (0.1 for values below minimum, 100.0 for values above maximum).
4. THE TPS_Setting input SHALL use the ValidatedTextBox control with a regex pattern that validates the input format. THE ValidatedTextBox SHALL show a red indicator immediately when the current text does not match the regex (not deferred to focus loss). WHEN the user leaves the input field (loses focus), IF the value is non-numeric or empty, THEN THE Preferences_Form SHALL revert the input to the last valid TPS value.
5. THE TPS_Setting input SHALL accept values with up to 1 decimal place of precision (e.g. 0.1, 0.5, 2.0).

### Requirement 4: GameApiRequestQueue — Core Dispatch

**User Story:** As a developer, I want a general-purpose parallel task queue that dispatches work items at a configurable rate, so that I can run multiple API requests concurrently without exceeding the server's rate limit.

#### Acceptance Criteria

1. THE GameApiRequestQueue SHALL accept a TPS configuration value and dispatch work items at no more than the configured TPS rate (measured as average throughput over a sliding 1-second window).
2. THE GameApiRequestQueue SHALL support multiple work items in-flight concurrently — the number of concurrent items is self-limiting based on how fast responses return relative to the TPS dispatch rate.
3. THE GameApiRequestQueue SHALL process work items in FIFO order (first enqueued, first dispatched).
4. THE GameApiRequestQueue SHALL be thread-safe — multiple threads may enqueue work items concurrently.
5. THE GameApiRequestQueue SHALL expose a method to enqueue a work item (an async delegate) and return a Task that completes when the work item finishes.

### Requirement 5: GameApiRequestQueue — Cascading Work

**User Story:** As a developer, I want work items to spawn follow-up work items based on their results, so that I can implement discovery patterns where one response generates further requests.

#### Acceptance Criteria

1. THE GameApiRequestQueue SHALL support work items that return zero or more follow-up work items to enqueue upon completion.
2. WHEN a work item completes and returns follow-up items, THE GameApiRequestQueue SHALL enqueue those items at the back of the queue for dispatch.
3. THE GameApiRequestQueue SHALL provide a method to wait until all work items (including cascaded follow-ups) have completed and all in-flight items have finished generating their follow-ups — i.e., the queue is fully drained with no items pending, executing, or generating cascaded work.
4. THE cascaded work items SHALL be subject to the same TPS rate governor as directly-enqueued items.

### Requirement 6: GameApiRequestQueue — Completion and Error Handling

**User Story:** As a developer, I want the queue to handle failures gracefully without stopping other in-flight work, so that one failed request doesn't block the rest.

#### Acceptance Criteria

1. IF a work item throws an exception, THE GameApiRequestQueue SHALL catch the exception, record it, and continue processing remaining items.
2. THE GameApiRequestQueue SHALL expose a list of errors (exception + work item context) that occurred during processing.
3. THE GameApiRequestQueue SHALL support cancellation via CancellationToken — when cancelled, no new items are dispatched but in-flight items are allowed to complete. EACH in-flight work item SHALL be subject to a configurable per-item timeout (default 30 seconds) to prevent permanent blocking when in-flight items fail to complete after cancellation.
4. THE GameApiRequestQueue SHALL expose a completion status indicating total items processed, succeeded, and failed.

### Requirement 7: GameApiRequestQueue — TPS Rate Governor

**User Story:** As a developer, I want the rate governor to accurately maintain the target TPS regardless of response latency, so that slow responses don't cause the queue to exceed the rate limit.

#### Acceptance Criteria

1. THE Rate_Governor SHALL use a token bucket algorithm: tokens are added at the configured TPS rate, each dispatch consumes one token, and dispatch blocks when no tokens are available.
2. THE Rate_Governor SHALL NOT dispatch more than TPS items per second on average (measured over a rolling window of at least 5 seconds), even when many items are queued and responses are fast. Brief bursts above the TPS rate within short measurement windows are acceptable as long as the longer-term average remains at or below the configured TPS.
3. THE Rate_Governor SHALL allow burst up to 1 second of accumulated tokens (i.e., max burst equals TPS value) to handle bursty enqueue patterns.
4. WHEN TPS is set to 0.5, THE Rate_Governor SHALL dispatch at most 1 item every 2 seconds on average.
5. WHEN TPS is set to 10.0, THE Rate_Governor SHALL dispatch at most 10 items per second on average (one every 100ms average interval), allowing brief bursts above this rate provided the average over longer periods remains at or below 10 items per second.

### Requirement 8: Discovery Tests — Structural Rework

**User Story:** As a developer, I want the full discovery test fixture restructured from 23 sequential ordered tests into a single queue-driven run method, so that all independent API calls execute concurrently within the TPS limit and the total discovery time is reduced from O(n) sequential calls to O(n/TPS) parallel throughput.

#### Acceptance Criteria

1. THE GameApiFullDiscoveryTests SHALL be restructured from multiple `[Test] [Order(N)]` methods into a single `[Test]` method that enqueues all work items into a GameApiRequestQueue and awaits drain.
2. THE discovery tests SHALL enqueue the following independent seed work items simultaneously at startup: character profile, character skills, colony list, banking balance, banking transactions (page 0), asset locations, accepted jobs, kill mail list, mail list (page 0), ship configuration, ship cargo, market listings, market items, market buy orders, market sell orders.
3. THE discovery tests SHALL configure the queue with a TPS value of 10.0 for the duration of the test run.

### Requirement 9: Discovery Tests — Colony Cascading

**User Story:** As a developer, I want colony detail fetching to cascade from the colony list response, so that all per-colony requests (summary, buildings, warehouse, workers) execute in parallel rather than sequentially iterating one colony at a time.

#### Acceptance Criteria

1. WHEN the colony list work item completes successfully, THE discovery tests SHALL enqueue one cascading work item per colony for each of: summary, buildings, warehouse, and workers (4 work items per colony). WHEN the colony list returns zero colonies, THE work item SHALL report success and enqueue no follow-up items.
2. THE per-colony work items SHALL execute concurrently with all other in-flight work items (not sequentially per-colony).
3. EACH per-colony work item SHALL write its response to `colonies/{colonyId}/{endpoint}.json` and call RecordSuccess/RecordSkipped independently.

### Requirement 10: Discovery Tests — Asset Cascading

**User Story:** As a developer, I want asset location detail fetching to cascade from the locations list, and individual asset detail (crate, survey, blueprint) to cascade from each location's cargo manifest, so that all asset requests fan out in parallel.

#### Acceptance Criteria

1. WHEN the asset locations work item completes successfully, THE discovery tests SHALL enqueue one cascading work item per location to fetch location detail. IF the enqueue operation itself throws an exception, THE discovery tests SHALL fail the entire discovery run and report the error.
2. WHEN a location detail work item completes successfully, THE discovery tests SHALL inspect the cargo array and enqueue one cascading work item for each cargo item with typeC "Cr" (crate detail), "Sc" (survey detail), or "Bp" (blueprint detail).
3. THE asset detail cascading SHALL fetch ALL matching crate/survey/blueprint items from ALL locations (not just the first match as the current implementation does).
4. EACH asset detail work item SHALL write its response to `assets/{type}-{id}.json` and call RecordSuccess/RecordSkipped independently.

### Requirement 11: Discovery Tests — Kill Mail Cascading

**User Story:** As a developer, I want kill mail detail fetching to cascade from the kill mail list response, so that all individual kill mail requests execute in parallel.

#### Acceptance Criteria

1. WHEN the kill mail list work item completes successfully, THE discovery tests SHALL enqueue one cascading work item per kill mail to fetch its detail.
2. EACH kill mail detail work item SHALL write its response to `killmails/{killMailId}.json` and call RecordSuccess/RecordSkipped independently.

### Requirement 12: Discovery Tests — Mail Pagination and Cascading

**User Story:** As a developer, I want mail list pagination to cascade page-by-page and individual mail detail to cascade from collected IDs, so that mail fetching is as parallel as the TPS allows.

#### Acceptance Criteria

1. THE mail list seed work item SHALL fetch page 0. WHEN page 0 returns results, THE work item SHALL check if the current page is empty; IF NOT empty, THEN enqueue a cascading work item for the next page (sequential pagination via cascading chain, stopping when an empty page is received).
2. WHEN each mail list page completes, THE work item SHALL also enqueue one cascading work item per mail ID found on that page to fetch mail detail.
3. THE mail detail work items from earlier pages SHALL execute concurrently with mail list pagination of later pages and with all other in-flight work items.
4. EACH mail detail work item SHALL write its response to `mail/{mailId}.json` and call RecordSuccess/RecordSkipped independently.

### Requirement 13: Discovery Tests — Market Cascading

**User Story:** As a developer, I want market-dependent requests (prices, ship components, competitors) to cascade from their parent responses, so that market data is fetched in parallel as soon as the prerequisite data arrives.

#### Acceptance Criteria

1. WHEN the market listings work item completes successfully, THE discovery tests SHALL enqueue cascading work items for: market prices (using first listing's type/typeId) and market ship components (using first ship listing's marketId).
2. WHEN the market buy orders work item completes successfully, THE discovery tests SHALL enqueue a cascading work item to fetch buy competitors (using first 5 marketIds).
3. WHEN the market sell orders work item completes successfully, THE discovery tests SHALL enqueue a cascading work item to fetch sell competitors (using first 5 marketIds).
4. THE market cascading work items SHALL execute concurrently with all other in-flight work items.

### Requirement 14: Discovery Tests — Banking Pagination

**User Story:** As a developer, I want banking transaction pagination to cascade page-by-page so that each page is fetched as a separate work item under the rate governor.

#### Acceptance Criteria

1. THE banking balance seed work item SHALL NOT cascade to transactions. THE banking transactions seed work item SHALL fetch page 0 independently.
2. WHEN banking transactions page N completes, THE work item SHALL check if the current page is empty; IF NOT empty, THEN enqueue a cascading work item for page N+1 (sequential pagination via cascading chain, stopping when an empty page is received).
3. AFTER queue drain, THE discovery tests SHALL combine all banking transaction pages into `banking/transactions-all.json`.

### Requirement 15: Discovery Tests — Completion and Reporting

**User Story:** As a developer, I want the discovery test to produce the same output structure and metadata as the current implementation, so that results are comparable and the transition is seamless.

#### Acceptance Criteria

1. THE discovery tests SHALL await queue completion (drain) and THEN write the `_metadata.json` summary.
2. THE `_metadata.json` SHALL include: run timestamp, character ID, total succeeded/skipped/failed counts, and per-endpoint detail (category, endpoint, success, httpStatus, reason) — identical structure to the current implementation.
3. THE discovery tests SHALL collect errors from the queue's error list and report them as RecordSkipped entries with the exception message as the reason.
4. THE discovery tests SHALL use a thread-safe mechanism for RecordSuccess/RecordSkipped since multiple work items complete concurrently (replace `List<EndpointResult>` with `ConcurrentBag<EndpointResult>` or equivalent).
5. THE output directory structure SHALL remain unchanged: character/, colonies/, banking/, assets/, jobs/, killmails/, mail/, ship/, market/ subdirectories.
6. IF a seed work item fails (e.g. colony list returns error), THE discovery tests SHALL record the failure and continue processing other independent seed items — a single failed seed SHALL NOT abort the entire run. THE discovery run SHALL write metadata and complete even if all seed items fail. THE run SHALL only be considered aborted if the queue itself fails to process (infrastructure failure), not if individual work items fail.

### Requirement 16: GameApiClient — Preserve Internal Rate Limiter

**User Story:** As a developer, I want the existing internal rate limiter to remain functional in GameApiClient until all callers are migrated to the queue, so that the sync scheduler and other sequential callers remain properly throttled.

#### Acceptance Criteria

1. THE GameApiClient SHALL retain its internal SemaphoreSlim rate limiter (AcquireRateLimitTokenAsync) and existing rate limiting behavior unchanged throughout the transition period, providing dual-layer rate protection alongside the queue's rate governor.
2. THE GameApiClient SHALL continue to honor the X-RateLimit-Limit response header for dynamic adjustment. THE GameApiRequestQueue SHALL receive rate limit information (X-RateLimit-Limit header values and 429 responses) from work items immediately, even while the internal limiter is still active, so that both systems can act on server feedback concurrently.
3. THE GameApiClient SHALL continue to handle HTTP 429 responses with its existing pause mechanism.
4. THE GameApiRequestQueue SHALL use its own rate governor independently of GameApiClient's internal limiter — the queue controls dispatch timing, and the client's internal limiter adds a secondary safety net.
5. WHEN a future migration moves all callers to GameApiRequestQueue, THE GameApiClient internal rate limiter SHALL be removed in a separate spec. THE internal limiter SHALL remain active until that migration is complete.

### Requirement 17: Adaptive Rate Limiting on 429

**User Story:** As a developer, I want the queue to automatically reduce its TPS when the server returns 429 (Too Many Requests), so that the system self-corrects without manual intervention and avoids repeated rate limit violations.

#### Acceptance Criteria

1. WHEN a work item receives an HTTP 429 response, THE GameApiRequestQueue SHALL pause all dispatch for the Retry-After duration (or 60 seconds if no Retry-After header is present).
2. AFTER pausing for the Retry-After duration, THE GameApiRequestQueue SHALL reduce its effective TPS to 50% of the rate that was active when the 429 was received.
3. IF a second consecutive 429 is received after resuming at the reduced rate, THE GameApiRequestQueue SHALL double the pause duration (exponential backoff: 2x, 4x, 8x the base pause, capped at 5 minutes).
4. AFTER successfully processing 60 seconds of requests without a 429, THE GameApiRequestQueue SHALL gradually increase the effective TPS by 10% per minute until it reaches the originally configured TPS value. WHEN the effective TPS reaches the configured TPS value, THE GameApiRequestQueue SHALL stop increasing and hold at the configured rate.
5. THE GameApiRequestQueue SHALL log each rate adjustment (reduction and recovery) including the old TPS, new TPS, and reason.
6. THE GameApiRequestQueue SHALL expose the current effective TPS (which may differ from the configured TPS during backoff/recovery) via a property for monitoring.
7. THE 429 response SHALL be re-enqueued for retry after the pause completes — the work item is not lost.
