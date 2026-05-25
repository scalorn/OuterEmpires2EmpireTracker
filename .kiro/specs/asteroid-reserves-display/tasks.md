# Implementation Plan: Asteroid Reserves Display

## Overview

Display asteroid max reserves on the survey detail page. Implementation spans three layers: a new public C# endpoint, updated TypeScript types and API client, and UI changes to SurveyDetail.tsx. Pure logic functions are separated from wiring and tests for clean task boundaries.

## Tasks

- [ ] 1. Add public asteroid detail endpoint
  - [x] 1.1 Implement GET /api/v1/public/asteroids/{uuid} endpoint in PublicDataEndpoints.cs
    - Add endpoint that searches all characters' asteroid data by UUID
    - Project response to include only uuid, name, and reserves (resourceName, purity, maxReserve)
    - Return 404 with error object when asteroid not found
    - Endpoint must be accessible without authentication
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
    - _Inputs: OE2EmpireTracker.Server/Endpoints/PublicDataEndpoints.cs, OE2EmpireTracker.Common/Models/Asteroid.cs_
    - _Output: OE2EmpireTracker.Server/Endpoints/PublicDataEndpoints.cs_
    - _Verification: Build succeeds, endpoint compiles cleanly_

  - [-] 1.2 Write integration tests for public asteroid endpoint
    - Test 200 response returns correct shape (uuid, name, reserves array)
    - Test 404 response for non-existent UUID
    - Test that CurrentReserve and ResetTimestamp are NOT in response
    - Test endpoint accessible without auth token
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
    - _Inputs: OE2EmpireTracker.Server/Endpoints/PublicDataEndpoints.cs_
    - _Output: OE2EmpireTracker.Tests/Server/PublicAsteroidEndpointTests.cs_
    - _Verification: Tests pass via vstest.console_

- [ ] 2. Update TypeScript types and API client
  - [x] 2.1 Add AsteroidReserve interface and update Asteroid interface in domain.ts
    - Add `AsteroidReserve` interface with resourceName (string), purity (string), maxReserve (number), optional currentReserve and resetTimestamp
    - Add `reserves: AsteroidReserve[]` field to Asteroid interface
    - Add optional `asteroidUUID?: string` field to Survey interface
    - _Requirements: 5.1, 5.2, 5.3, 2.1_
    - _Inputs: OE2EmpireTracker.Web/src/api/types/domain.ts_
    - _Output: OE2EmpireTracker.Web/src/api/types/domain.ts_
    - _Verification: TypeScript compiles cleanly_

  - [-] 2.2 Add getAsteroidDetail method to public API client
    - Add `getAsteroidDetail(uuid: string)` method to public endpoints
    - Method calls GET `api/v1/public/asteroids/${uuid}` and returns typed Asteroid
    - _Requirements: 3.1_
    - _Inputs: OE2EmpireTracker.Web/src/api/endpoints/public.ts, OE2EmpireTracker.Web/src/api/types/domain.ts_
    - _Output: OE2EmpireTracker.Web/src/api/endpoints/public.ts_
    - _Verification: TypeScript compiles cleanly_

  - [~] 2.3 Add usePublicAsteroidDetail hook in useAsteroids.ts
    - Create hook with `enabled: !!asteroidUUID` conditional fetching
    - Configure retry: false, staleTime: 5 minutes
    - Query key: ['public', 'asteroids', asteroidUUID]
    - _Requirements: 3.1, 3.5_
    - _Inputs: OE2EmpireTracker.Web/src/api/hooks/useAsteroids.ts, OE2EmpireTracker.Web/src/api/endpoints/public.ts_
    - _Output: OE2EmpireTracker.Web/src/api/hooks/useAsteroids.ts_
    - _Verification: TypeScript compiles cleanly_

- [ ] 3. Implement pure logic functions
  - [-] 3.1 Create matchReserveToResource function in reserveMatching.ts
    - Create new file `src/utils/reserveMatching.ts`
    - Implement case-insensitive matching by resourceName AND purity
    - Return matched AsteroidReserve or undefined
    - _Requirements: 3.3, 3.6, 4.3_
    - _Inputs: OE2EmpireTracker.Web/src/api/types/domain.ts_
    - _Output: OE2EmpireTracker.Web/src/utils/reserveMatching.ts_
    - _Verification: TypeScript compiles cleanly_

  - [-] 3.2 Add formatMaxReserve function in formatters.ts
    - Return "-" for undefined/null values
    - Return integer with thousands separators for numeric values (no decimals)
    - Display zero as "0" not as a dash
    - _Requirements: 4.1, 4.2, 4.4_
    - _Inputs: OE2EmpireTracker.Web/src/utils/formatters.ts_
    - _Output: OE2EmpireTracker.Web/src/utils/formatters.ts_
    - _Verification: TypeScript compiles cleanly_

- [~] 4. Checkpoint - Ensure all code compiles
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Wire reserves display into SurveyDetail page
  - [~] 5.1 Update SurveyDetail.tsx to fetch and display asteroid reserves
    - Call usePublicAsteroidDetail when surveyType === 'Asteroid' and asteroidUUID is non-empty
    - Show loading indicator while asteroid data is loading
    - Add "Max Reserve" column to resource table for asteroid surveys
    - Match reserves to resources using matchReserveToResource
    - Format values using formatMaxReserve
    - On fetch failure or 404, display "-" for all reserve cells
    - Hide max reserve column entirely for Planet surveys
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.5, 4.6_
    - _Inputs: OE2EmpireTracker.Web/src/pages/public/SurveyDetail.tsx, OE2EmpireTracker.Web/src/utils/reserveMatching.ts, OE2EmpireTracker.Web/src/utils/formatters.ts, OE2EmpireTracker.Web/src/api/hooks/useAsteroids.ts_
    - _Output: OE2EmpireTracker.Web/src/pages/public/SurveyDetail.tsx_
    - _Verification: TypeScript compiles cleanly, manual visual check_

- [ ] 6. Property-based tests for pure logic
  - [~] 6.1 Write property test for endpoint projection (Property 1)
    - **Property 1: Public endpoint projection strips private fields**
    - For any AsteroidReserve with currentReserve/resetTimestamp, the projected response must not contain those fields
    - **Validates: Requirements 1.3**
    - _Output: OE2EmpireTracker.Web/src/api/__tests__/asteroidProjection.test.ts_

  - [~] 6.2 Write property test for asteroidUUID inclusion (Property 2)
    - **Property 2: AsteroidUUID inclusion in survey response**
    - asteroidUUID included iff surveyType is 'Asteroid' AND UUID is non-empty string
    - **Validates: Requirements 2.2, 2.3**
    - _Output: OE2EmpireTracker.Web/src/api/__tests__/surveyProjection.test.ts_

  - [~] 6.3 Write property test for fetch-enabled logic (Property 3)
    - **Property 3: Fetch-enabled logic**
    - Fetch enabled iff surveyType === 'Asteroid' AND asteroidUUID is non-null, non-empty
    - **Validates: Requirements 3.1, 3.5**
    - _Output: OE2EmpireTracker.Web/src/hooks/__tests__/usePublicAsteroidDetail.test.ts_

  - [~] 6.4 Write property test for resource-to-reserve matching (Property 4)
    - **Property 4: Resource-to-reserve matching with case-insensitive comparison**
    - Match iff resourceName equal (case-insensitive) AND purity equal (case-insensitive)
    - **Validates: Requirements 3.3, 3.6, 4.3**
    - _Output: OE2EmpireTracker.Web/src/utils/__tests__/reserveMatching.test.ts_

  - [~] 6.5 Write property test for max reserve formatting (Property 5)
    - **Property 5: Max reserve formatting**
    - Non-negative integers produce digits + thousands separators only, zero displays as "0", undefined/null produce "-"
    - **Validates: Requirements 4.1, 4.4**
    - _Output: OE2EmpireTracker.Web/src/utils/__tests__/formatMaxReserve.test.ts_

  - [~] 6.6 Write property test for reserves field default (Property 6)
    - **Property 6: Reserves field defaults to empty array**
    - When server response has null/undefined/omitted reserves, parsed Asteroid has reserves = []
    - **Validates: Requirements 5.1**
    - _Output: OE2EmpireTracker.Web/src/api/__tests__/asteroidParsing.test.ts_

- [ ] 7. Unit tests for UI states
  - [~] 7.1 Write unit tests for SurveyDetail reserve display states
    - Test loading indicator shown during fetch
    - Test fetch failure shows resources without reserves (no error message)
    - Test Planet survey hides max reserve column
    - Test 404 asteroid shows dashes for all rows
    - Test zero maxReserve displays as "0"
    - _Requirements: 3.2, 3.4, 4.1, 4.2, 4.5, 4.6_
    - _Output: OE2EmpireTracker.Web/src/pages/public/__tests__/SurveyDetail.test.tsx_

- [~] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific UI states and edge cases
- The server endpoint (task 1.1) and TypeScript types (tasks 2.1-2.3) can be developed in parallel
- Pure logic functions (task 3) are independent of wiring (task 5)
- All property tests use fast-check v4.8.0 with vitest (already configured in the project)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1"] },
    { "id": 1, "tasks": ["1.2", "2.2", "3.1", "3.2"] },
    { "id": 2, "tasks": ["2.3", "6.4", "6.5"] },
    { "id": 3, "tasks": ["5.1", "6.1", "6.2", "6.3", "6.6"] },
    { "id": 4, "tasks": ["7.1"] }
  ]
}
```
