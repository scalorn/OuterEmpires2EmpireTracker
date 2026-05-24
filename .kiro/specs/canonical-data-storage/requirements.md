# Requirements Document

## Introduction

The server currently stores global/baseline data as a single opaque JSON blob via `PUT /api/v1/global/baseline`. This violates the core principle that "a record is a record" — data should be stored consistently in one canonical location regardless of whether it arrived via bulk upload or individual mutation. This feature decomposes the global baseline upload into individual records and exposes all inherently-public game-world data through unauthenticated endpoints.

## Glossary

- **Baseline_Upload**: The `PUT /api/v1/global/baseline` endpoint that receives the full BaselineData.json payload
- **Decomposition_Service**: The server-side service that parses the baseline blob and stores each section as individual records
- **Reference_Data**: Game constants and lookup tables shared by all players (BlueprintType, ShipClass, TechLevel, Commodity, RefiningRecipe, ResearchTime, GameConstants)
- **Global_Blueprint**: A blueprint with OwnerUUID="" representing shared game reference blueprints (not owned by any player)
- **Public_Endpoint**: An API endpoint accessible without authentication (AllowAnonymous)
- **Storage_Backend**: The `IStorageBackend` interface providing storage-agnostic persistence
- **Star_System**: A location in the galaxy map with coordinates, spectral class, and faction ownership
- **Planet**: A celestial body within a star system where colonies can be established
- **Asteroid**: A celestial body within a star system that can be mined
- **Colony_Summary**: The publicly visible portion of a colony (name and size), distinct from private colony internals
- **Blueprint_Browser**: The web UI page that displays blueprints with filtering capabilities


## Requirements

### Requirement 1: Baseline Upload Decomposition

**User Story:** As the server administrator, I want the global baseline upload to decompose the JSON blob into individual records, so that each piece of reference data is stored in its canonical location and accessible independently.

#### Acceptance Criteria

1. WHEN a valid BaselineData.json payload is received at `PUT /api/v1/global/baseline`, THE Decomposition_Service SHALL parse the JSON and extract each top-level section (GameConstants, ShipClass, BlueprintType, TechLevel, Commodity, RefiningRecipe, ResearchTime)
2. WHEN the payload contains a GameConstants object, THE Decomposition_Service SHALL store it as a global data entry with key "GameConstants"
3. WHEN the payload contains a ShipClass array, THE Decomposition_Service SHALL store it as a global data entry with key "ShipClass"
4. WHEN the payload contains a BlueprintType array, THE Decomposition_Service SHALL store it as a global data entry with key "BlueprintType"
5. WHEN the payload contains a TechLevel array, THE Decomposition_Service SHALL store it as a global data entry with key "TechLevel"
6. WHEN the payload contains a Commodity array, THE Decomposition_Service SHALL store it as a global data entry with key "Commodity"
7. WHEN the payload contains a RefiningRecipe array, THE Decomposition_Service SHALL store it as a global data entry with key "RefiningRecipe"
8. WHEN the payload contains a ResearchTime array, THE Decomposition_Service SHALL store it as a global data entry with key "ResearchTime"
9. WHEN the payload contains a Blueprint array, THE Decomposition_Service SHALL store each blueprint as an individual blueprint record with OwnerUUID="" (empty string indicating global ownership)
10. WHEN the payload is missing a section, THE Decomposition_Service SHALL silently skip that section and continue processing other present sections — no extraction, no storage, no side effects for the missing section (partial uploads are valid); the request SHALL return success if at least one section was processed
11. IF the payload contains invalid JSON, THEN THE Baseline_Upload SHALL immediately return HTTP 400 with a descriptive error message and perform no processing — even if some sections could be partially extracted, the entire request SHALL be rejected and no data SHALL be stored


### Requirement 2: Global Blueprint Storage

**User Story:** As a player using the web UI, I want global blueprints to be stored as individual records, so that they appear in the public blueprint browser alongside shared player blueprints.

#### Acceptance Criteria

1. WHEN the baseline upload contains a Blueprint array, THE Decomposition_Service SHALL upsert each blueprint individually using the existing `UpsertBlueprintAsync` method with characterUUID="" (empty string) — IF any single blueprint fails to upsert, THEN THE entire baseline upload SHALL be rejected with an error response
2. THE Storage_Backend SHALL support characterUUID="" as a valid key for global blueprints (not tied to any player)
3. WHEN a global blueprint has the same UUID as an existing global blueprint, THE Decomposition_Service SHALL overwrite the existing record (upsert semantics)
4. THE Storage_Backend SHALL return global blueprints (characterUUID="") when queried via `GetAllBlueprintsAsync("")`


### Requirement 3: Public Reference Data Endpoints

**User Story:** As a web UI consumer, I want to access reference data without authentication, so that filter dropdowns and game constants are available to all visitors.

#### Acceptance Criteria

1. THE Public_Endpoint at `GET /api/v1/public/global/{dataType}` SHALL return the stored reference data for the specified dataType without requiring authentication — the endpoint SHALL only respond to actual HTTP requests (no hypothetical evaluation)
2. WHEN an actual endpoint request is received with dataType "GameConstants", THE Public_Endpoint SHALL attempt to return the GameConstants JSON object — IF no GameConstants data is stored, THEN THE Public_Endpoint SHALL let the return operation handle missing data (no pre-check required)
3. WHEN an actual endpoint request is received with dataType "ShipClass", THE Public_Endpoint SHALL return the ShipClass JSON array
4. WHEN an actual endpoint request is received with dataType "BlueprintType", THE Public_Endpoint SHALL check if BlueprintType data is stored before attempting to return it — IF data exists, THEN return the BlueprintType JSON array; IF data does not exist, THEN return HTTP 404
5. WHEN an actual endpoint request is received with dataType "TechLevel", THE Public_Endpoint SHALL attempt to return the TechLevel JSON array — the endpoint logic SHALL trigger on request and handle missing data within the operation
6. WHEN an actual endpoint request is received with dataType "Commodity", THE Public_Endpoint SHALL only return the Commodity JSON array when Commodity data exists in storage — IF no Commodity data is stored, THEN THE Public_Endpoint SHALL return HTTP 404
7. WHEN an actual endpoint request is received with dataType "RefiningRecipe", THE Public_Endpoint SHALL always attempt to return the RefiningRecipe JSON array regardless of storage status — missing data is handled by the general 404 rule (criterion 9)
8. WHEN an actual endpoint request is received with dataType "ResearchTime", THE Public_Endpoint SHALL always attempt to return the ResearchTime JSON array regardless of storage status — missing data is handled by the general 404 rule (criterion 9)
9. IF the requested dataType has no stored data, THEN THE Public_Endpoint SHALL return HTTP 404 with error message "Global data type not found" — this rule applies only to actual HTTP requests received by the endpoint
10. WHEN some reference data types have stored data and others do not, THE Public_Endpoint SHALL return data for available types and HTTP 404 for missing types independently (partial availability is valid)


### Requirement 4: Public Blueprint Endpoint Enhancement

**User Story:** As a web UI visitor, I want the public blueprints endpoint to include global blueprints, so that the blueprint browser shows the same data as the WinForms app.

#### Acceptance Criteria

1. WHEN `GET /api/v1/public/blueprints` is called, THE Public_Endpoint SHALL return global blueprints (OwnerUUID="") in addition to character-shared blueprints
2. THE Public_Endpoint SHALL support pagination via `page` and `pageSize` query parameters for global blueprints
3. WHEN both global blueprints and character-shared blueprints exist, THE Public_Endpoint SHALL return them in a single combined result set
4. THE Public_Endpoint SHALL include the total count of all public blueprints (global plus shared) in the pagination metadata


### Requirement 5: Public Systems Endpoint

**User Story:** As a web UI visitor, I want to view the galaxy map data, so that I can see star systems without needing to log in.

#### Acceptance Criteria

1. THE Public_Endpoint at `GET /api/v1/public/systems` SHALL return all star systems without requiring authentication
2. WHEN star systems exist in storage, THE Public_Endpoint SHALL return them as a JSON array — IF systems exist in storage but cannot be retrieved due to an error, THEN THE Public_Endpoint SHALL return an appropriate error status code (never an empty array or cached/fallback data)
3. THE Public_Endpoint SHALL include all Star_System fields: Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass, FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase
4. IF no star systems are stored (zero results from a successful query — including fresh installations or post-migration states with no system data), THEN THE Public_Endpoint SHALL return an empty JSON array (treated as a successful zero-result query, not an error)


### Requirement 6: Public Planets and Asteroids Endpoint

**User Story:** As a web UI visitor, I want to view planets and asteroids in a system, so that I can see celestial bodies without needing to log in.

#### Acceptance Criteria

1. THE Public_Endpoint at `GET /api/v1/public/systems/{systemId}/planets` SHALL return all planets in the specified system without requiring authentication — IF planets exist but cannot be retrieved due to a database error, network issue, or other internal failure, THEN THE Public_Endpoint SHALL return an appropriate error status code (never an empty array or cached/fallback data when data exists but is unreachable)
2. THE Public_Endpoint at `GET /api/v1/public/systems/{systemId}/asteroids` SHALL return all asteroids in the specified system without requiring authentication
3. WHEN planets exist for the specified system, THE Public_Endpoint SHALL return them as a JSON array — IF no planets exist for the specified system, THEN THE Public_Endpoint SHALL return an empty JSON array regardless of serialization capability — IF planets exist but cannot be serialized to JSON, THEN THE Public_Endpoint SHALL reject the request with an error status code
4. WHEN asteroids exist for the specified system, THE Public_Endpoint SHALL return them as a JSON array — IF no asteroids exist for the specified system, THEN THE Public_Endpoint SHALL return an empty JSON array — IF asteroids exist but cannot be serialized to JSON, THEN THE Public_Endpoint SHALL reject the request with an error status code
5. [Removed — redundant with criteria 3 and 4 which already specify empty array behavior for missing planets/asteroids]


### Requirement 7: Public Colony Summary Endpoint

**User Story:** As a web UI visitor, I want to see colony names and sizes at a planet, so that I can view the same public information visible in-game when landing at a planet.

#### Acceptance Criteria

1. THE Public_Endpoint at `GET /api/v1/public/systems/{systemId}/colonies` SHALL return colony summaries (name and size only) for all colonies in the specified system without requiring authentication, including colonies with zero structures
2. THE Public_Endpoint SHALL return only the colony name and structure count (size) — no internal details (structures, resources, inventories)
3. WHEN colonies exist for the specified system, THE Public_Endpoint SHALL return them as a JSON array of objects with fields: ColonyName, Size, PlanetName — including colonies with zero structures (Size = 0); colony entries SHALL always be returned even when Size = 0
4. IF no colonies currently exist for the specified system, THEN THE Public_Endpoint SHALL return a strictly empty array — no cached, placeholder, or previously-queried colony data SHALL be returned


### Requirement 8: Systems Data Storage

**User Story:** As the server administrator, I want star systems stored as canonical global data, so that they are accessible as public game-world data independent of any player's character.

#### Acceptance Criteria

1. THE Storage_Backend SHALL provide methods to store and retrieve star systems as global data (not per-character)
2. WHEN star systems are uploaded, THE Storage_Backend SHALL store each system individually keyed by its Id — IF storage fails for any system, THEN THE entire upload SHALL be rejected and the client SHALL receive an error response
3. THE Storage_Backend SHALL support bulk upsert of star systems (for initial galaxy data load)
4. WHEN a system with the same Id already exists, THE Storage_Backend SHALL overwrite it (upsert semantics)


### Requirement 9: Existing Endpoint Continuity

**User Story:** As a developer maintaining the existing system, I want the new decomposition to not break existing authenticated endpoints, so that current clients continue to work.

#### Acceptance Criteria

1. THE authenticated endpoint `GET /api/v1/global/{dataType}` SHALL continue to return data for any dataType that has been stored via decomposition
2. THE authenticated endpoint `PUT /api/v1/global/{dataType}` SHALL continue to accept arbitrary dataType values for direct storage (non-baseline uploads bypass decomposition)
3. WHEN `PUT /api/v1/global/baseline` is called, THE Decomposition_Service SHALL perform decomposition — IF decomposition fails, THEN THE entire request SHALL be rejected (atomic semantics) — IF decomposition is never attempted (e.g., request bypasses the decomposition path), THEN THE request SHALL also be rejected — THE system SHALL always reject baseline requests when decomposition attempt cannot be verified, even if the data appears valid; any request that does not pass through the Decomposition_Service SHALL be rejected regardless of reason
4. THE existing `PUT /api/v1/characters/{uuid}/import` endpoint SHALL remain unchanged — player bulk import is already correct


### Requirement 10: Web UI Blueprint Browser Parity

**User Story:** As a web UI visitor, I want the blueprint browser to show global blueprints with the same filters and columns as the WinForms app, so that the web experience matches the desktop experience.

#### Acceptance Criteria

1. THE Blueprint_Browser SHALL display global blueprints (OwnerUUID="") among other blueprint types in the results list (inclusive display)
2. THE Blueprint_Browser SHALL populate the Type filter dropdown from the server's BlueprintType reference data — WHEN BlueprintType reference data is unavailable from the server, THE Type filter dropdown SHALL be empty
3. THE Blueprint_Browser SHALL populate the Tech Level filter dropdown from the server's TechLevel reference data
4. THE Blueprint_Browser SHALL populate the Ship Class filter dropdown from the server's ShipClass reference data — WHEN ShipClass reference data is unavailable from the server, THE Ship Class filter dropdown SHALL be empty
5. THE Blueprint_Browser SHALL include an Evolution filter dropdown with values populated from the distinct Evolution values present in the blueprint data — WHEN no evolution data is available in the blueprint data, THE Evolution filter dropdown SHALL be empty
6. THE Blueprint_Browser SHALL display columns: Type, Name, Tech Level, Evolution, Nickname (matching WinForms column set)
7. WHEN reference data is not yet available (server has no baseline uploaded), THE Blueprint_Browser SHALL display empty filter dropdowns without error — WHEN some reference data types are available while others are not, THE Blueprint_Browser SHALL populate dropdowns for available types and show empty dropdowns for unavailable types independently
8. WHEN new reference data is uploaded to the server (via baseline sync), THE Web_UI SHALL reflect the updated values on next page load without requiring a code deployment — IF a page loads while an upload is still in progress, THEN THE Web_UI SHALL show stale (pre-upload) data; updates are visible only when a page is loaded after the upload completes


### Requirement 11: Decomposition Idempotency

**User Story:** As the server administrator, I want repeated baseline uploads to produce the same result, so that re-syncing does not create duplicate data or corrupt existing records.

#### Acceptance Criteria

1. WHEN the same BaselineData.json is uploaded multiple times, THE Decomposition_Service SHALL produce identical stored state after each upload (idempotent)
2. WHEN a baseline upload contains fewer blueprints than a previous upload, THE Decomposition_Service SHALL upsert the provided blueprints without deleting blueprints from previous uploads (additive semantics)
3. WHEN a reference data section (e.g., ShipClass) is uploaded, THE Decomposition_Service SHALL overwrite the entire previous value for that key (replace semantics for reference arrays) — reference data replacement MAY break round-trip equivalence with previous uploads
4. FOR ALL valid BaselineData.json payloads, uploading then reading each decomposed section SHALL produce data equivalent to the corresponding section in the original payload (round-trip property) — this applies to a single upload cycle; subsequent uploads with different reference data may overwrite previous values


### Requirement 12: Data Visibility Enforcement

**User Story:** As a player, I want my private data to remain private even though public game-world data is now exposed, so that my colony internals, surveys, and personal blueprints are protected.

#### Acceptance Criteria

1. THE Public_Endpoint for colony summaries SHALL expose only colony name and size — no structures, resources, inventories, or other internal data — this filtering rule SHALL apply when the request is identified as a colony summary request (i.e., routed to the colony summary endpoint)
2. THE Public_Endpoint for blueprints SHALL expose only global blueprints (OwnerUUID="") and explicitly shared player blueprints — private player blueprints (those not explicitly marked as shared, including blueprints with no sharing flag set) SHALL be inaccessible through any public endpoint — global blueprints SHALL always be accessible regardless of any access controls applied to private blueprints; access denials for global blueprint requests SHALL be overridden
3. THE Public_Endpoint for systems, planets, and asteroids SHALL always allow access to all records (these are inherently public game-world data); access SHALL never be denied regardless of other access controls
4. THE Public_Endpoint SHALL NOT expose surveys, player profiles, delivery routes, delivery plans, ships, ship templates, market listings, market transactions, pricing plans, stock plans, stock profiles, build plans, or supply chains — the endpoint implementation SHALL simply not include these data types in responses (no active validation required)
5. IF a request attempts to access a non-public data type through a public endpoint, THEN THE Public_Endpoint SHALL return HTTP 404 regardless of whether the requested data exists — the response SHALL NOT distinguish between "data exists but is private" and "data does not exist" — EXCEPT colony summary requests, which SHALL return filtered colony data (name and size only) rather than 404


### Requirement 13: Web UI Data-Driven Architecture

**User Story:** As a web UI user, I want the web application to fetch all data from server APIs with no hardcoded values, so that the web UI always reflects the current game state and has the same functionality as the WinForms desktop app.

#### Acceptance Criteria

1. THE Web_UI SHALL NOT contain hardcoded lists of blueprint types, ship classes, tech levels, evolution values, or any other reference data — all such values SHALL be fetched from the server's public global data endpoints
2. THE Web_UI SHALL fetch filter dropdown options from `GET /api/v1/public/global/{dataType}` for each reference data type (BlueprintType, ShipClass, TechLevel)
3. THE Web_UI SHALL derive Evolution filter options from the ResearchTime reference data or from distinct values in the blueprint dataset — WHEN ResearchTime reference data is unavailable from the server, THE Evolution filter dropdown SHALL be empty (evolution filtering becomes unavailable)
4. THE Web_UI SHALL display the same blueprint fields as the WinForms app: Type (BluePrintType), Name, Tech Level, Evolution, and Nickname
5. THE Web_UI SHALL provide the same filtering capabilities as the WinForms app: filter by Type, Tech Level, Ship Class, Evolution, and free-text search across name and type
6. THE Web_UI SHALL gracefully handle the case where reference data is not yet available (server has no baseline uploaded) by showing empty filter dropdowns without errors — WHEN some reference data types are available while others are not, THE Web_UI SHALL populate dropdowns for available types and show empty dropdowns only for unavailable types
7. THE Web_UI SHALL use the same API endpoints for reading data regardless of whether the user is authenticated or not — public pages use public endpoints, authenticated pages use authenticated endpoints
8. WHEN new reference data is uploaded to the server (via baseline sync), THE Web_UI SHALL reflect the updated values on next page load without requiring a code deployment — updates are visible only when a page is loaded after the upload completes, not immediately upon upload

