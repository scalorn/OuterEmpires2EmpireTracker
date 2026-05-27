# Implementation Plan: Game API Discovery Tool

## Overview

Expand the existing `GameApiColonyDiscoveryTests` pattern into a comprehensive `GameApiFullDiscoveryTests` fixture that calls every read endpoint in the game API. New methods are added to `GameApiClient` in small batches (grouped by endpoint category), then the test fixture is built incrementally — setup/teardown first, then one test method per endpoint category, wired together with result tracking and metadata output.

## Tasks

- [x] 1. Add banking and assets methods to GameApiClient
  - [x] 1.1 Add GetBankingBalanceAsync and GetBankingTransactionsAsync methods
    - Follow the existing pattern in GetColonyListAsync (appId, accessToken params, ExecuteWithPoliciesAsync, rate limiting)
    - GET /v1/banking/balance and GET /v1/banking/transactions
    - Return `(bool Success, string Json)` tuple
    - _Requirements: 1.4, 1.5_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics on GameApiClient.cs — zero errors_

  - [x] 1.2 Add GetAssetLocationsAsync and GetAssetLocationDetailAsync methods
    - GET /v1/assets/locations (no params beyond appId/accessToken)
    - GET /v1/assets/locations/{locationId}?locationType={type} (int locationId, string locationType)
    - _Requirements: 1.2, 1.3_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics on GameApiClient.cs — zero errors_

- [x] 2. Add jobs, killmail, and mail methods to GameApiClient
  - [x] 2.1 Add GetAcceptedJobsAsync method
    - GET /v1/jobs/accepted
    - _Requirements: 1.6_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 2.2 Add GetKillMailListAsync and GetKillMailDetailAsync methods
    - GET /v1/killmails (no extra params)
    - GET /v1/killmails/{killMailId} (int killMailId)
    - _Requirements: 1.7, 1.8_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 2.3 Add GetMailListAsync and GetMailDetailAsync methods
    - GET /v1/mail (no extra params)
    - GET /v1/mail/{mailId} (int mailId)
    - _Requirements: 1.9, 1.10_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics — zero errors_

- [x] 3. Add ship and colony summary methods to GameApiClient
  - [x] 3.1 Add GetShipConfigurationAsync and GetShipCargoAsync methods
    - GET /v1/ship/configuration and GET /v1/ship/cargo
    - _Requirements: 1.11, 1.12_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 3.2 Add GetColonySummaryAsync method
    - GET /v1/colonies/{colonyId} (int colonyId)
    - _Requirements: 1.1_
    - _Inputs: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Client/GameApiClient.cs_
    - _Verification: getDiagnostics — zero errors_

- [x] 4. Checkpoint — Verify full solution builds
  - Ensure all tests pass, ask the user if questions arise.
  - Build OE2EmpireTracker.sln — zero errors, zero warnings
  - All 12 new GameApiClient methods compile cleanly


- [x] 5. Create GameApiFullDiscoveryTests fixture with setup and teardown
  - [x] 5.1 Create the test fixture class with OneTimeSetUp and OneTimeTearDown
    - File: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs
    - [TestFixture], [Explicit] attributes
    - OneTimeSetUp: load credentials (same pattern as GameApiColonyDiscoveryTests), exchange token, create output directory tree at spec/game-api-data/ with all subdirectories
    - OneTimeTearDown: write _metadata.json with run timestamp, character ID, scopes, endpoint results summary; dispose client
    - Add EndpointResult tracking class and helper methods: RecordSuccess, RecordSkipped, FormatJson, EnsureDirectory
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 2.1, 2.3, 2.5_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiColonyDiscoveryTests.cs (pattern reference)_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

- [x] 6. Implement character and colony test methods
  - [x] 6.1 Add PullCharacterProfile and PullCharacterSkills test methods
    - [Test, Order(1)] PullCharacterProfile — calls GetCharacterAsync, saves to character/profile.json
    - [Test, Order(2)] PullCharacterSkills — calls GetCharacterSkillsAsync, saves to character/skills.json
    - Handle 403 gracefully (log and continue via RecordSkipped)
    - Save full envelope, format with indentation
    - _Requirements: 3.1, 3.2, 11.1, 11.4, 12.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 6.2 Add PullColonyList and PullColonyDetails test methods
    - [Test, Order(3)] PullColonyList — calls GetColonyListAsync, saves to colonies/list.json
    - [Test, Order(4)] PullColonyDetails — iterates colonies from list, calls GetColonySummaryAsync, GetColonyBuildingsAsync, GetColonyWarehouseAsync, GetColonyWorkersAsync per colony
    - Saves to colonies/{colonyId}/summary.json, buildings.json, warehouse.json, workers.json
    - Extract colony IDs from list response using JObject/JArray parsing
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 11.1, 11.2, 11.4, 12.5_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_


- [x] 7. Implement banking and assets test methods
  - [x] 7.1 Add PullBankingBalance and PullBankingTransactions test methods
    - [Test, Order(5)] PullBankingBalance — calls GetBankingBalanceAsync, saves to banking/balance.json
    - [Test, Order(6)] PullBankingTransactions — calls GetBankingTransactionsAsync, saves to banking/transactions.json
    - _Requirements: 5.1, 5.2, 11.1, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 7.2 Add PullAssetLocations and PullAssetLocationDetails test methods
    - [Test, Order(7)] PullAssetLocations — calls GetAssetLocationsAsync, saves to assets/locations.json
    - [Test, Order(8)] PullAssetLocationDetails — iterates locations from list, calls GetAssetLocationDetailAsync per location
    - Extract locationId and locationType from list response using JObject/JArray
    - Saves to assets/{locationType}-{locationId}.json
    - _Requirements: 6.1, 6.2, 11.1, 11.2, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

- [x] 8. Implement jobs, killmail, mail, and ship test methods
  - [x] 8.1 Add PullAcceptedJobs test method
    - [Test, Order(9)] PullAcceptedJobs — calls GetAcceptedJobsAsync, saves to jobs/accepted.json
    - _Requirements: 7.1, 11.1, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 8.2 Add PullKillMailList and PullKillMailDetails test methods
    - [Test, Order(10)] PullKillMailList — calls GetKillMailListAsync, saves to killmails/list.json
    - [Test, Order(11)] PullKillMailDetails — iterates up to 5 kill mails from list, calls GetKillMailDetailAsync
    - Saves to killmails/{killMailId}.json
    - _Requirements: 8.1, 8.2, 11.1, 11.2, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 8.3 Add PullMailList and PullMailDetails test methods
    - [Test, Order(12)] PullMailList — calls GetMailListAsync, saves to mail/list.json
    - [Test, Order(13)] PullMailDetails — iterates up to 5 mails from list, calls GetMailDetailAsync
    - Saves to mail/{mailId}.json
    - _Requirements: 9.1, 9.2, 11.1, 11.2, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

  - [x] 8.4 Add PullShipConfiguration and PullShipCargo test methods
    - [Test, Order(14)] PullShipConfiguration — calls GetShipConfigurationAsync, saves to ship/configuration.json
    - [Test, Order(15)] PullShipCargo — calls GetShipCargoAsync, saves to ship/cargo.json
    - _Requirements: 10.1, 10.2, 11.1, 11.4_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_


- [x] 9. Add error summary output and circuit breaker handling
  - [x] 9.1 Add test output summary and circuit breaker awareness
    - In OneTimeTearDown, write summary to TestContext.WriteLine: endpoints succeeded, skipped, failed
    - Add BrokenCircuitException catch in each test method — if circuit breaker opens, record remaining endpoints as skipped
    - Ensure non-403 errors are logged with endpoint path and HTTP status code
    - _Requirements: 11.1, 11.2, 11.3_
    - _Inputs: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Output: OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs_
    - _Verification: getDiagnostics — zero errors_

- [x] 10. Add .gitignore entry for output directory
  - [x] 10.1 Add spec/game-api-data/ to .gitignore
    - Add entry to the root .gitignore so raw API data is not committed
    - _Requirements: 2.1 (output directory is for reference, not source control)_
    - _Inputs: .gitignore_
    - _Output: .gitignore_
    - _Verification: getDiagnostics — no issues_

- [x] 11. Final checkpoint — Verify full solution builds cleanly
  - Ensure all tests pass, ask the user if questions arise.
  - Build OE2EmpireTracker.sln — zero errors, zero warnings
  - Run vstest.console against OE2EmpireTracker.Tests — all non-Explicit tests pass
  - The new GameApiFullDiscoveryTests fixture does NOT run during normal test execution (Explicit attribute)

## Notes

- No property-based tests are included — this feature is an integration tool with side effects (HTTP calls, file I/O) and no pure functions with meaningful input variation
- The test fixture is marked [Explicit] and will not run in CI — it requires real game API credentials
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- The existing GameApiClient rate limiting handles all throttling transparently — no additional rate limiting needed in the fixture
- Output files save the full Service_Response_Envelope (not just the `data` field) to preserve error responses for debugging
- Colony detail endpoints iterate all colonies; kill mail and mail details are capped at 5 items to prevent excessive API calls

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "2.1", "2.2", "2.3", "3.1", "3.2"] },
    { "id": 1, "tasks": ["5.1", "10.1"] },
    { "id": 2, "tasks": ["6.1", "6.2"] },
    { "id": 3, "tasks": ["7.1", "7.2", "8.1", "8.2", "8.3", "8.4"] },
    { "id": 4, "tasks": ["9.1"] }
  ]
}
```
