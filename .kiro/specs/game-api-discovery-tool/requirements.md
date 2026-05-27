# Requirements Document

## Introduction

Expand the existing colony-focused API discovery test fixture into a comprehensive data dump tool that calls every read endpoint in the game API for a given character. The output is saved as raw JSON files to a dedicated directory, serving as reference data for future integration development. This extends the pattern established by `GameApiColonyDiscoveryTests` to cover all API endpoints.

## Glossary

- **Discovery_Tool**: An NUnit `[Explicit]` test fixture that authenticates against the game API and calls read endpoints, saving raw JSON responses to disk.
- **GameApiClient**: The existing HTTP client class (`OE2EmpireTracker.Common/Client/GameApiClient.cs`) that handles authentication, rate limiting, and API calls.
- **Output_Directory**: The `spec/game-api-data/` directory at the workspace root where raw JSON responses are stored, organized by endpoint category.
- **Endpoint_Category**: A logical grouping of related API endpoints (e.g., character, colony, banking, assets, jobs, killmails, mail, ship).
- **Parameterized_Endpoint**: An endpoint that requires an ID from a parent list endpoint (e.g., `/v1/colonies/{colonyId}/buildings` requires a colony ID from `/v1/colonies`).
- **Service_Response_Envelope**: The standard API response wrapper containing `success`, `returnCode`, `returnString`, and `data` fields.

## Requirements

### Requirement 1: GameApiClient Endpoint Coverage

**User Story:** As a developer, I want the GameApiClient to have methods for all read endpoints in the game API, so that the discovery tool can call them without raw HTTP logic.

#### Acceptance Criteria

1. THE GameApiClient SHALL expose a method `GetColonySummaryAsync` that calls `GET /v1/colonies/{colonyId}` and returns the raw JSON response
2. THE GameApiClient SHALL expose a method `GetAssetLocationsAsync` that calls `GET /v1/assets/locations` and returns the raw JSON response
3. THE GameApiClient SHALL expose a method `GetAssetLocationDetailAsync` that calls `GET /v1/assets/locations/{locationId}?locationType={type}` and returns the raw JSON response
4. THE GameApiClient SHALL expose a method `GetBankingBalanceAsync` that calls `GET /v1/banking/balance` and returns the raw JSON response
5. THE GameApiClient SHALL expose a method `GetBankingTransactionsAsync` that calls `GET /v1/banking/transactions` and returns the raw JSON response
6. THE GameApiClient SHALL expose a method `GetAcceptedJobsAsync` that calls `GET /v1/jobs/accepted` and returns the raw JSON response
7. THE GameApiClient SHALL expose a method `GetKillMailListAsync` that calls `GET /v1/killmails` and returns the raw JSON response
8. THE GameApiClient SHALL expose a method `GetKillMailDetailAsync` that calls `GET /v1/killmails/{killMailId}` and returns the raw JSON response
9. THE GameApiClient SHALL expose a method `GetMailListAsync` that calls `GET /v1/mail` and returns the raw JSON response
10. THE GameApiClient SHALL expose a method `GetMailDetailAsync` that calls `GET /v1/mail/{mailId}` and returns the raw JSON response
11. THE GameApiClient SHALL expose a method `GetShipConfigurationAsync` that calls `GET /v1/ship/configuration` and returns the raw JSON response
12. THE GameApiClient SHALL expose a method `GetShipCargoAsync` that calls `GET /v1/ship/cargo` and returns the raw JSON response


### Requirement 2: Output Directory Structure

**User Story:** As a developer, I want API responses saved to a well-organized directory structure, so that I can easily find and reference specific endpoint data.

#### Acceptance Criteria

1. THE Discovery_Tool SHALL write output files to the `spec/game-api-data/` Output_Directory at the workspace root
2. THE Discovery_Tool SHALL organize output files by Endpoint_Category using subdirectories: `character/`, `colonies/`, `banking/`, `assets/`, `jobs/`, `killmails/`, `mail/`, `ship/`
3. THE Discovery_Tool SHALL create the Output_Directory and subdirectories if they do not exist
4. THE Discovery_Tool SHALL overwrite existing files on subsequent runs to keep data current
5. THE Discovery_Tool SHALL write a `_metadata.json` file in the Output_Directory root containing the run timestamp, character ID, and list of scopes granted by the token

### Requirement 3: Character and Skills Data Dump

**User Story:** As a developer, I want the discovery tool to pull character profile and skills data, so that I have reference data for character-related integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/character` and save the response to `character/profile.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/character/skills` and save the response to `character/skills.json`

### Requirement 4: Colony Data Dump

**User Story:** As a developer, I want the discovery tool to pull all colony data including per-colony details, so that I have reference data for colony integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/colonies` and save the response to `colonies/list.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/colonies/{colonyId}` for each colony and save responses to `colonies/{colonyId}/summary.json`
3. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/colonies/{colonyId}/buildings` for each colony and save responses to `colonies/{colonyId}/buildings.json`
4. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/colonies/{colonyId}/warehouse` for each colony and save responses to `colonies/{colonyId}/warehouse.json`
5. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/colonies/{colonyId}/workers` for each colony and save responses to `colonies/{colonyId}/workers.json`

### Requirement 5: Banking Data Dump

**User Story:** As a developer, I want the discovery tool to pull banking data, so that I have reference data for financial integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/banking/balance` and save the response to `banking/balance.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/banking/transactions` and save the response to `banking/transactions.json`


### Requirement 6: Assets Data Dump

**User Story:** As a developer, I want the discovery tool to pull asset location data including per-location details, so that I have reference data for asset tracking integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/assets/locations` and save the response to `assets/locations.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/assets/locations/{locationId}?locationType={type}` for each location returned by the list endpoint and save responses to `assets/{locationType}-{locationId}.json`

### Requirement 7: Jobs Data Dump

**User Story:** As a developer, I want the discovery tool to pull accepted jobs data, so that I have reference data for job-related integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/jobs/accepted` and save the response to `jobs/accepted.json`

### Requirement 8: Kill Mail Data Dump

**User Story:** As a developer, I want the discovery tool to pull kill mail data including individual mail details, so that I have reference data for combat log integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/killmails` and save the response to `killmails/list.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/killmails/{killMailId}` for up to 5 kill mails from the list and save responses to `killmails/{killMailId}.json`

### Requirement 9: Mail Data Dump

**User Story:** As a developer, I want the discovery tool to pull in-game mail data including message bodies, so that I have reference data for mail integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/mail` and save the response to `mail/list.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/mail/{mailId}` for up to 5 mails from the list and save responses to `mail/{mailId}.json`

### Requirement 10: Ship Data Dump

**User Story:** As a developer, I want the discovery tool to pull ship configuration and cargo data, so that I have reference data for ship-related integrations.

#### Acceptance Criteria

1. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/ship/configuration` and save the response to `ship/configuration.json`
2. WHEN the Discovery_Tool runs, THE Discovery_Tool SHALL call `GET /v1/ship/cargo` and save the response to `ship/cargo.json`

### Requirement 11: Error Handling and Scope Awareness

**User Story:** As a developer, I want the discovery tool to handle missing scopes and API errors gracefully, so that a partial data dump is still useful even when some scopes are not granted.

#### Acceptance Criteria

1. IF an endpoint returns HTTP 403 (scope not granted), THEN THE Discovery_Tool SHALL log the missing scope and continue with remaining endpoints
2. IF an endpoint returns an error other than 403, THEN THE Discovery_Tool SHALL log the error with the endpoint path and HTTP status code and continue with remaining endpoints
3. WHEN the Discovery_Tool completes, THE Discovery_Tool SHALL write a summary to the test output indicating how many endpoints succeeded and how many were skipped
4. THE Discovery_Tool SHALL save the full Service_Response_Envelope (not just the `data` field) so that error responses are preserved for debugging

### Requirement 12: Test Fixture Structure

**User Story:** As a developer, I want the discovery tool implemented as an NUnit Explicit test fixture, so that it follows the same pattern as the existing colony discovery tests and can be run on demand.

#### Acceptance Criteria

1. THE Discovery_Tool SHALL be implemented as an NUnit test fixture in `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs`
2. THE Discovery_Tool SHALL be marked with `[Explicit]` so it does not run during normal test execution
3. THE Discovery_Tool SHALL reuse the same credential loading pattern as `GameApiColonyDiscoveryTests` (preferences + credential manager)
4. THE Discovery_Tool SHALL format all JSON output with indentation for human readability
5. THE Discovery_Tool SHALL respect the GameApiClient rate limiting to avoid triggering HTTP 429 responses
