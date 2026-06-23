# Requirements Document

## Introduction

Display asteroid max reserves on the survey detail page in the web UI. When a user views an asteroid survey, the page fetches the linked asteroid's reserve data (which is public) and displays max reserves alongside the survey resources. This gives users immediate visibility into how much of each resource an asteroid can hold, complementing the survey's yield-rate data.

## Glossary

- **Survey_Detail_Page**: The web UI page (`SurveyDetail.tsx`) that displays full details of a single survey including location, metadata, and resources.
- **Asteroid_Survey**: A survey where `surveyType` equals `'Asteroid'`, linked to an Asteroid entity via `asteroidUUID`.
- **Asteroid_Reserve**: A record on an Asteroid entity containing ResourceName, Purity, MaxReserve, CurrentReserve, and ResetTimestamp.
- **Max_Reserve**: The maximum amount of a resource an asteroid can hold before it stops regenerating. Displayed as a numeric value alongside survey resource rows.
- **Public_Asteroid_Endpoint**: A server API endpoint that returns asteroid data (including reserves) without requiring authentication.
- **Survey_Resource_Row**: A single row in the survey detail resources section showing resource name, purity, and amount per hour.

## Requirements

### Requirement 1: Public Asteroid Detail Endpoint

**User Story:** As a web UI user, I want the server to expose asteroid reserve data publicly, so that the survey detail page can fetch it without authentication.

#### Acceptance Criteria

1. WHEN a GET request is made to the public asteroid detail endpoint with an asteroid UUID that matches an existing asteroid, THE Public_Asteroid_Endpoint SHALL return the asteroid's UUID, Name, and reserves list as an array where each entry includes ResourceName, Purity, and MaxReserve.
2. IF a GET request is made to the public asteroid detail endpoint with a UUID that does not match any asteroid, THEN THE Public_Asteroid_Endpoint SHALL return HTTP 404 with an error object indicating the asteroid was not found.
3. THE Public_Asteroid_Endpoint SHALL NOT expose CurrentReserve or ResetTimestamp fields in the response (these are private operational data).
4. THE Public_Asteroid_Endpoint SHALL be accessible without authentication (anonymous access allowed).

### Requirement 2: Survey Type Exposes AsteroidUUID

**User Story:** As a web UI developer, I want the Survey TypeScript interface and public survey response to include `asteroidUUID`, so that the survey detail page can identify which asteroid to fetch.

#### Acceptance Criteria

1. THE Survey TypeScript interface SHALL include an optional `asteroidUUID` field of type `string`.
2. WHEN the public surveys endpoint returns a survey whose `surveyType` is `Asteroid` and the survey has an associated asteroid UUID, THE response SHALL include the `asteroidUUID` field containing the UUID of the associated asteroid. IF the Asteroid survey has no associated asteroid UUID, THE response SHALL omit the `asteroidUUID` field.
3. WHEN the public surveys endpoint returns a survey whose `surveyType` is `Planet`, THE response SHALL omit the `asteroidUUID` field or return it as undefined.

### Requirement 3: Fetch Asteroid Reserves on Survey Detail

**User Story:** As a user viewing an asteroid survey, I want the page to automatically fetch the linked asteroid's reserves, so that I can see max reserve data without navigating away.

#### Acceptance Criteria

1. WHEN the Survey_Detail_Page renders an Asteroid_Survey that has a non-null, non-empty-string `asteroidUUID`, THE Survey_Detail_Page SHALL fetch the asteroid's reserve data from the Public_Asteroid_Endpoint (`/api/v1/public/asteroids/{asteroidUUID}`). IF the `asteroidUUID` field is missing or empty, THE Survey_Detail_Page SHALL NOT attempt to fetch asteroid reserve data and SHALL display resource rows without max reserve values.
2. WHILE the asteroid reserve data is loading, THE Survey_Detail_Page SHALL display a loading indicator in the reserves section.
3. WHEN the asteroid reserve data is fetched successfully, THE Survey_Detail_Page SHALL match each asteroid reserve to the corresponding survey resource by resource name and purity, and display the asteroid's `maxReserve` value alongside each matched survey resource row. Survey resources with no matching reserve (by name and purity) SHALL NOT display a max reserve value.
4. IF the asteroid reserve fetch fails or does not respond within 10 seconds, THEN THE Survey_Detail_Page SHALL display the survey resources without max reserve values and SHALL NOT display an error message to the user.
5. WHEN the Survey_Detail_Page renders a Planet survey (surveyType is not 'Asteroid'), THE Survey_Detail_Page SHALL NOT attempt to fetch asteroid reserve data.
6. IF a survey resource has no matching reserve in the asteroid data (no match on resource name and purity), THEN THE Survey_Detail_Page SHALL display that resource row without a max reserve value.

### Requirement 4: Display Max Reserves Alongside Survey Resources

**User Story:** As a user viewing an asteroid survey, I want to see the max reserve for each resource next to the survey yield data, so that I can assess both production rate and total capacity.

#### Acceptance Criteria

1. WHEN asteroid reserve data is available for the linked asteroid, THE Survey_Detail_Page SHALL display the MaxReserve value in a dedicated column next to each Survey_Resource_Row where the resource name matches an Asteroid_Reserve entry. A MaxReserve of zero SHALL be displayed as "0" (not as a dash).
2. IF a survey resource has no matching Asteroid_Reserve entry, THEN THE Survey_Detail_Page SHALL display a dash character ("-") in the max reserve column for that row.
3. THE Survey_Detail_Page SHALL match survey resources to asteroid reserves by ResourceName using case-insensitive comparison.
4. THE Survey_Detail_Page SHALL display max reserve values formatted as integers with thousands separators and no decimal places (e.g., "1,250,000").
5. IF the survey has SurveyType of Planet, THEN THE Survey_Detail_Page SHALL hide the max reserve column.
6. IF the survey is linked to an AsteroidUUID that does not resolve to an existing asteroid entity, THEN THE Survey_Detail_Page SHALL display a dash character ("-") in the max reserve column for all resource rows, ignoring any previously cached reserve data.

### Requirement 5: Asteroid TypeScript Type Includes Reserves

**User Story:** As a web UI developer, I want the Asteroid TypeScript interface to include a reserves array, so that fetched asteroid data can be properly typed.

#### Acceptance Criteria

1. THE Asteroid TypeScript interface SHALL include a `reserves` field of type `AsteroidReserve[]`, defaulting to an empty array when the server response omits the field or returns null.
2. THE AsteroidReserve TypeScript interface SHALL include exactly three required fields: `resourceName` (string), `purity` (string), and `maxReserve` (number, representing a non-negative integer). It MAY additionally include optional fields `currentReserve` (number | undefined) and `resetTimestamp` (string | undefined) to accommodate future private endpoint responses, but these SHALL be undefined in public endpoint responses.
3. WHEN the server returns an Asteroid entity with a `reserves` array, THE Asteroid TypeScript interface SHALL type each element as AsteroidReserve without requiring additional transformation or mapping.
