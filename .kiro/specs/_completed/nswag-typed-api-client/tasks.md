# Implementation Plan

## Overview

Replace the hand-written GameApiClient with an NSwag-generated strongly-typed client. Phase 1 creates the infrastructure (generated client, wrapper, rate limiter, exception types, unit tests). Phase 2 migrates the discovery tool as proof-of-concept.

## Task Dependency Graph

```json
{
  "waves": [
    ["task1", "task2", "task3"],
    ["task4"],
    ["task5"],
    ["task6", "task7"],
    ["task8"],
    ["task9", "task10", "task11", "task12"],
    ["task13"],
    ["task14"],
    ["task15"]
  ]
}
```

## Tasks

- [x] 1. Create exception types (ApiHttpException, ApiBusinessException, ApiDeserializationException, ApiValidationError)
  - Satisfies: Req 3, Criteria 3-5
  - Inputs: Design doc exception type signatures
  - Output: 4 files in `OE2EmpireTracker.Common/Client/Exceptions/`
  - Verification: getDiagnostics reports zero errors on all 4 files
  - Sub-tasks:
    - [x] 1.1 Create `ApiHttpException.cs` with StatusCode (int) and ResponseBody (string) properties
    - [x] 1.2 Create `ApiBusinessException.cs` with ReturnCode (int), ReturnString (string), Errors (IReadOnlyList<ApiValidationError>)
    - [x] 1.3 Create `ApiDeserializationException.cs` with StatusCode (int) and RawBody (string)
    - [x] 1.4 Create `ApiValidationError.cs` with Field (string) and Error (string)

- [x] 2. Run NSwag code generation to produce GameApiGeneratedClient.cs
  - Satisfies: Req 1, Criteria 3-5; Req 4, Criteria 1-5; Req 8, Criterion 5
  - Inputs: `tools/nswag-gen/Program.cs`, `docs/game-api-swagger.json`
  - Output: `OE2EmpireTracker.Common/Client/Generated/GameApiGeneratedClient.cs`
  - Verification: Solution builds with zero errors; generated file has auto-gen header; DTOs use [JsonPropertyName]
  - Sub-tasks:
    - [x] 2.1 Run `dotnet run --project tools/nswag-gen` to produce generated client
    - [x] 2.2 Verify generated code compiles under netstandard2.0 in both .NET Framework 4.8.1 and .NET 8
    - [x] 2.3 Verify generated file includes auto-generated header comment

- [x] 3. Create IGameApiTypedClient interface
  - Satisfies: Req 2, Criteria 1, 5-6; Req 6, Criterion 6
  - Inputs: Design doc interface definition
  - Output: `OE2EmpireTracker.Common/Client/IGameApiTypedClient.cs`
  - Verification: getDiagnostics reports zero errors
  - Sub-tasks:
    - [x] 3.1 Define all method signatures (one per endpoint, Task<T> return, CancellationToken param)
    - [x] 3.2 Include IsCircuitOpen property and IDisposable

- [x] 4. Create TokenBucketRateLimiter
  - Satisfies: Req 6, Criteria 3-5
  - Inputs: Design doc rate limiter specification
  - Output: `OE2EmpireTracker.Common/Client/TokenBucketRateLimiter.cs`
  - Verification: getDiagnostics reports zero errors; unit tests pass (Task 10)
  - Sub-tasks:
    - [x] 4.1 Implement token-bucket with 0.5 TPS refill rate and SemaphoreSlim(30) concurrency
    - [x] 4.2 Implement AcquireAsync (wait for concurrency slot + throughput token)
    - [x] 4.3 Implement PauseFor method (drains tokens, sets next-available forward, caps at 300s)

- [x] 5. Create GameApiTypedClient with resilience policies
  - Satisfies: Req 6, Criteria 1-2, 6-7
  - Inputs: Exception types (Task 1), Interface (Task 3), RateLimiter (Task 4), Generated client (Task 2)
  - Output: `OE2EmpireTracker.Common/Client/GameApiTypedClient.cs`
  - Verification: getDiagnostics reports zero errors
  - Sub-tasks:
    - [x] 5.1 Create class implementing IGameApiTypedClient, constructor accepts HttpClient + config
    - [x] 5.2 Wire Polly 7.2.4 retry (3x, 1s/2s/4s) and circuit breaker (3 failures, 30s break)
    - [x] 5.3 Wire rate limiter (AcquireAsync before dispatch, Release in finally, circuit check first)

- [x] 6. Implement token manager and cache in GameApiTypedClient
  - Satisfies: Req 7, Criteria 1-5
  - Inputs: GameApiTypedClient skeleton (Task 5), Generated client token exchange method
  - Output: `OE2EmpireTracker.Common/Client/GameApiTypedClient.cs` (token management additions)
  - Verification: getDiagnostics reports zero errors; unit tests pass (Task 9)
  - Sub-tasks:
    - [x] 6.1 Implement ConcurrentDictionary token cache keyed by clientId+secret hash
    - [x] 6.2 Implement token exchange bypassing rate limiter (auth calls skip AcquireAsync)
    - [x] 6.3 Implement TestConnectionAsync (trial exchange, returns bool)

- [x] 7. Implement envelope unwrapping success path in GameApiTypedClient
  - Satisfies: Req 2, Criterion 3; Req 3, Criteria 1-2, 6
  - Inputs: GameApiTypedClient (Task 5), Generated client envelope types (Task 2)
  - Output: `OE2EmpireTracker.Common/Client/GameApiTypedClient.cs` (unwrap helper + endpoint methods)
  - Verification: getDiagnostics reports zero errors; unit tests pass (Task 9 sub-task 9.1)
  - Sub-tasks:
    - [x] 7.1 Create generic UnwrapAsync<T> helper that extracts .Data from envelope on success=true
    - [x] 7.2 Handle null data field: return null typed as T? rather than throwing
    - [x] 7.3 Wire endpoint methods to call generated client then UnwrapAsync

- [x] 8. Implement envelope unwrapping error paths and 429 handling
  - Satisfies: Req 3, Criteria 3-5; Req 6, Criterion 5
  - Inputs: GameApiTypedClient (Task 7), Exception types (Task 1), Rate limiter (Task 4)
  - Output: `OE2EmpireTracker.Common/Client/GameApiTypedClient.cs` (error handling in UnwrapAsync)
  - Verification: getDiagnostics reports zero errors; unit tests pass (Task 9 sub-tasks 9.2-9.3)
  - Sub-tasks:
    - [x] 8.1 Implement success=false → throw ApiBusinessException with returnCode, returnString, errors
    - [x] 8.2 Implement 4xx/5xx → throw ApiHttpException; malformed JSON → throw ApiDeserializationException
    - [x] 8.3 Implement 429 handling: parse Retry-After header (default 60s, cap 300s), call PauseFor, throw

- [x] 9. Unit tests for envelope unwrapping
  - Satisfies: Req 3, Criteria 1-5 (validation)
  - Inputs: GameApiTypedClient (Task 8), Exception types (Task 1)
  - Output: `OE2EmpireTracker.Tests/Services/GameApiTypedClientEnvelopeTests.cs`
  - Verification: vstest.console — all tests pass
  - Sub-tasks:
    - [x] 9.1 Test success=true returns typed DTO; success=true with null data returns null
    - [x] 9.2 Test success=false throws ApiBusinessException with correct fields
    - [x] 9.3 Test 4xx/5xx throws ApiHttpException; malformed JSON throws ApiDeserializationException

- [x] 10. Unit tests for token cache
  - Satisfies: Req 7, Criteria 2-4 (validation)
  - Inputs: GameApiTypedClient (Task 6)
  - Output: `OE2EmpireTracker.Tests/Services/GameApiTypedClientTokenTests.cs`
  - Verification: vstest.console — all tests pass
  - Sub-tasks:
    - [x] 10.1 Test cache hit reuses token (no second HTTP call); expired token triggers re-exchange
    - [x] 10.2 Test token exchange bypasses rate limiter (AcquireAsync not called for auth)

- [x] 11. Unit tests for rate limiter
  - Satisfies: Req 6, Criteria 3-5 (validation)
  - Inputs: TokenBucketRateLimiter (Task 4)
  - Output: `OE2EmpireTracker.Tests/Services/TokenBucketRateLimiterTests.cs`
  - Verification: vstest.console — all tests pass
  - Sub-tasks:
    - [x] 11.1 Test throughput enforcement: dispatches spaced ≥ 2s apart (use SystemClock)
    - [x] 11.2 Test concurrency limit: only 30 of 35 concurrent tasks acquire simultaneously
    - [x] 11.3 Test PauseFor: next AcquireAsync blocks until pause expires

- [x] 12. Unit tests for circuit breaker and retry
  - Satisfies: Req 6, Criteria 1-2, 6-7 (validation)
  - Inputs: GameApiTypedClient (Task 5)
  - Output: `OE2EmpireTracker.Tests/Services/GameApiTypedClientResilienceTests.cs`
  - Verification: vstest.console — all tests pass
  - Sub-tasks:
    - [x] 12.1 Test circuit opens after 3 consecutive 500s; IsCircuitOpen=true; throws without dispatch
    - [x] 12.2 Test retry: mock 500→500→200 sequence succeeds on third attempt

- [x] 13. Migrate discovery tool setup and auth to typed client
  - Satisfies: Req 9, Criteria 1-2, 7
  - Inputs: IGameApiTypedClient (Task 3), GameApiTypedClient (Tasks 5-8), existing GameApiFullDiscoveryTests.cs
  - Output: `OE2EmpireTracker.Tests/Services/GameApiFullDiscoveryTests.cs` (refactored)
  - Verification: getDiagnostics reports zero errors; auth test passes against live API
  - Sub-tasks:
    - [x] 13.1 Replace GameApiClient instantiation with GameApiTypedClient
    - [x] 13.2 Migrate token exchange to ExchangeTokenAsync, use TokenResponseDto
    - [x] 13.3 Add 401 catch-and-retry logic (refresh token, retry once)

- [x] 14. Implement cascading work items in discovery tool
  - Satisfies: Req 9, Criteria 3-6
  - Inputs: Discovery tool setup (Task 13), TokenBucketRateLimiter (Task 4)
  - Output: `OE2EmpireTracker.Tests/Services/GameApiFullDiscoveryTests.cs` (work item methods)
  - Verification: getDiagnostics reports zero errors; fixture crawls multiple endpoint types
  - Sub-tasks:
    - [x] 14.1 Implement cascading pattern: locations → location detail → crates/blueprints/surveys
    - [x] 14.2 Verify 0.9 TPS and 9 concurrent limits applied (validated by timing checks)

- [x] 15. Implement discovery tool summary report and JSON output
  - Satisfies: Req 9, Criteria 8-9
  - Inputs: Discovery tool with cascading items (Task 14)
  - Output: `OE2EmpireTracker.Tests/Services/GameApiFullDiscoveryTests.cs` (report + output logic)
  - Verification: Full discovery run produces summary and JSON output files organized by category
  - Sub-tasks:
    - [x] 15.1 Track call counts per endpoint category, output summary at teardown
    - [x] 15.2 Write raw JSON responses to output directory (auth/, locations/, crates/, blueprints/, surveys/)

## Notes

- NSwag CLI crashes on this machine due to CET shadow stack assertion. Use `dotnet run --project tools/nswag-gen` instead of nswag CLI or MSBuild target.
- Generated code is committed to source control to avoid needing NSwag at build time. Regeneration is manual.
- The discovery tool runs against the live API — these are integration tests requiring valid credentials and network access.
- Rate limiter tests must use `SystemClock.FreezeAt` + `SystemClock.EnableInstantDelay` to avoid wall-clock waits.
- Polly 7.2.4 is used (not 8.x) — no ResiliencePipeline API, use Policy.WrapAsync pattern.
