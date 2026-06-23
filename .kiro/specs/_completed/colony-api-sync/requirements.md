# Requirements Document

## Introduction

The OE2 Empire Tracker currently retrieves player profile data from the Outer Empires 2 game API via the GameApiSyncScheduler. This feature extends that sync infrastructure to also retrieve colony data from the game API and merge it into the local colony list.

The game API exposes colony data across multiple endpoints (see API Reference below):
- `GET /v1/colonies` — returns the colony list with summary info (scope: `colony.list.read`)
- `GET /v1/colonies/{colonyId}/buildings` — returns buildings per colony (scope: `colony.buildings.read`, requires Remote Operations Array)
- `GET /v1/colonies/{colonyId}/warehouse` — returns warehouse contents per colony (scope: `colony.warehouse.read`, requires Remote Operations Array)

Colonies fetched from the API are matched to existing local colonies by planet name and system name (dedup), or created as new entries. The merge uses "API wins" strategy for game-authoritative fields while preserving local-only data (timers, build queue sequence, manufacturing assignments, overflow rules).

The local data model stores ALL fields from the game API. Where the local model and the game API disagree on field location, type, or existence, the game API model wins.

This replaces the manual HTML copy-paste import workflow with automated periodic retrieval for players who have configured game API credentials with the appropriate scopes.

## API Reference

- **Swagger JSON:** https://oe2-pub-api-dev.azure-api.net/swagger.json
- **Swagger UI:** https://oe2-pub-api-dev.azure-api.net/swagger/ui#/

All responses are wrapped in a `ServiceResponse<T>` envelope: `{ success, returnCode, returnString, data }`.

## Glossary

- **Game_API**: The Outer Empires 2 public REST API at oe2-pub-api-dev.azure-api.net, accessed via OAuth2 client_credentials flow.
- **Game_API_Client**: The existing GameApiClient HTTP client that communicates with the Game API using Polly resilience policies.
- **Sync_Scheduler**: The existing GameApiSyncScheduler that manages periodic polling and coordinates data retrieval from the Game API.
- **Colony_Sync**: The process of fetching colony data from the Game API and merging it into the local colony list.
- **Merge_Logic**: The code path that applies API response data to local Colony objects using "API wins" strategy for game-authoritative fields.
- **Dedup_Match**: The process of matching an API colony to an existing local colony by systemObjectName (planet) + systemName (case-insensitive).
- **Local_Only_Fields**: Colony and structure fields that are managed locally and not overwritten by API sync (timers, build queue sequence, manufacturing assignments, overflow rules, lock tracking).
- **PlayerContext**: The singleton that manages player data persistence and provides access to the colony list.
- **Remote_Operations_Array**: A colony building that must be installed and online for the per-colony detail endpoints (buildings, warehouse, workers) to be accessible. Without it, those endpoints return an error.
- **ServiceResponse**: The standard API response envelope containing `success` (bool), `returnCode` (int), `returnString` (string), and `data` (T).
- **Data_Model_Expansion**: The process of adding new properties to Colony, ColonyStructure, and Item models to store all fields returned by the game API.
- **Data_Model_Migration**: The process of moving fields from one model to another (e.g. CurrentAttitude from ColonyStructure to Colony) while maintaining backward compatibility with existing serialized JSON data.

## Requirements

### Requirement 1: Game API Colony List Endpoint

**User Story:** As a player, I want the tracker to fetch my colony list from the game API, so that my colonies are populated automatically without manual HTML import.

#### Acceptance Criteria

1. THE Game_API_Client SHALL expose an async method GetColonyListAsync(appId, accessToken) that calls GET /v1/colonies on the Game API.
2. WHEN the Game API returns HTTP 200 with a JSON response, THE Game_API_Client SHALL return a success result containing the raw JSON string.
3. IF the Game API returns HTTP 401, THEN THE Game_API_Client SHALL return a failure result with the string "401" to signal credential invalidation.
4. IF the Game API returns HTTP 403, THEN THE Game_API_Client SHALL return a failure result with the string "403" to signal missing scope (colony.list.read not granted).
5. IF the Game API returns a transient error (HTTP 500, 502, 503, 504), THEN THE Game_API_Client SHALL retry with exponential backoff per the existing Polly retry policy.
6. IF the circuit breaker is open, THEN THE Game_API_Client SHALL return a failure result without making the HTTP request.
7. THE Game_API_Client SHALL acquire a rate limit token before making the colony list request, consistent with the existing rate limiting pattern.


### Requirement 2: Game API Colony Buildings Endpoint

**User Story:** As a player, I want the tracker to fetch building details for each colony that has remote access, so that my structure list is kept current.

#### Acceptance Criteria

1. THE Game_API_Client SHALL expose an async method GetColonyBuildingsAsync(appId, accessToken, colonyId) that calls GET /v1/colonies/{colonyId}/buildings on the Game API.
2. WHEN the Game API returns HTTP 200 with a JSON response, THE Game_API_Client SHALL return a success result containing the raw JSON string.
3. IF the Game API returns HTTP 401, THEN THE Game_API_Client SHALL return a failure result with the string "401".
4. IF the Game API returns HTTP 403, THEN THE Game_API_Client SHALL return a failure result with the string "403" to signal missing scope (colony.buildings.read not granted) or that the colony lacks a Remote Operations Array.
5. IF the Game API returns HTTP 404, THEN THE Game_API_Client SHALL return a failure result (colony not found or not owned by this character).
6. THE Game_API_Client SHALL acquire a rate limit token before making the buildings request.


### Requirement 3: Game API Colony Warehouse Endpoint

**User Story:** As a player, I want the tracker to fetch warehouse contents for each colony that has remote access, so that my inventory data is kept current.

#### Acceptance Criteria

1. THE Game_API_Client SHALL expose an async method GetColonyWarehouseAsync(appId, accessToken, colonyId) that calls GET /v1/colonies/{colonyId}/warehouse on the Game API.
2. WHEN the Game API returns HTTP 200 with a JSON response, THE Game_API_Client SHALL return a success result containing the raw JSON string.
3. IF the Game API returns HTTP 401, THEN THE Game_API_Client SHALL return a failure result with the string "401".
4. IF the Game API returns HTTP 403, THEN THE Game_API_Client SHALL return a failure result with the string "403" (missing scope or no Remote Operations Array).
5. IF the Game API returns HTTP 404, THEN THE Game_API_Client SHALL return a failure result.
6. THE Game_API_Client SHALL acquire a rate limit token before making the warehouse request.


### Requirement 4: Colony Response DTOs

**User Story:** As a developer, I want typed DTOs that map ALL fields from the game API colony responses, so that the merge logic can access every API field in a type-safe manner.

#### Acceptance Criteria

1. THE GameApiColonyListResponse DTO SHALL include a Colonies property of type List&lt;GameApiColonyListItem&gt; mapped to "colonies".
2. THE GameApiColonyListItem DTO SHALL include ALL of the following properties mapped to their JSON fields: ColonyId (int, "colonyId"), ColonyName (string, "colonyName"), SystemObjectName (string, "systemObjectName"), SystemName (string, "systemName"), SystemId (int, "systemId"), ColonySize (int, "colonySize"), RemoteAccess (int, "remoteAccess"), HasManufacturing (int, "hasManufacturing"), ManufacturingInProgress (int, "manufacturingInProgress"), HasMining (int, "hasMining"), MiningInProgress (int, "miningInProgress"), HasRefining (int, "hasRefining"), RefiningInProgress (int, "refiningInProgress"), HasResearch (int, "hasResearch"), ResearchInProgress (int, "researchInProgress"), Distance (double, "distance"), SurfaceVariation (int, "surfaceVariation"), AtmosVariation (int, "atmosVariation"), HexValue (string, "hexValue"), SystemObjectTypeName (string, "systemObjectTypeName"), ImagePreFix (string, "imagePreFix"), ManufacturingBlocked (int, "manufacturingBlocked"), WorkerCurrentAttitude (int, "workerCurrentAttitude"), ContentmentIndex (int, "contentmentIndex").
3. THE GameApiColonyBuildingsResponse DTO SHALL include a Buildings property of type List&lt;GameApiColonyBuilding&gt; mapped to "buildings", and a Summary property mapped to "summary", and a ColonyCapacities property mapped to "colonyCapacities".
4. THE GameApiColonyBuilding DTO SHALL include ALL of the following properties: BuildingId (int, "buildingId"), ColonyBuildingTypeId (int, "colonyBuildingTypeId"), BlueprintDesignName (string, "blueprintDesignName"), BuildingOnline (bool, "buildingOnline"), StatusId (int, "statusId"), ConstructingBuildingFinish (DateTime?, "constructingBuildingFinish"), ResourceName (string, "resourceName"), MaxRate (double, "maxRate"), NextFinish (DateTime?, "nextFinish"), ManufactureNumber (int, "manufactureNumber"), ResourceId (int, "resourceId"), ResourceIcon (string, "resourceIcon"), ManufactureAmountPerRun (int, "manufactureAmountPerRun"), DurabilityCurrent (double, "durabilityCurrent"), DurabilityMax (double, "durabilityMax"), OpsStatusEffects (List&lt;GameApiBuildingStatusEffect&gt;, "opsStatusEffects"), Industries (List&lt;GameApiBuildingIndustry&gt;, "industries"), DetailsRequired (List&lt;GameApiBuildingDetailRequirement&gt;, "detailsRequired"), SupportDetailsRequired (List&lt;GameApiBuildingDetailRequirement&gt;, "supportDetailsRequired"), BuildingAttributes (List&lt;GameApiBuildingAttribute&gt;, "buildingAttributes"), ExtraProperties (List&lt;GameApiBuildingExtraProperty&gt;, "extraProperties").
5. THE GameApiBuildingStatusEffect DTO SHALL include: StatusId (int, "statusId"), ModTypeId (int, "modTypeId"), Change (double, "change").
6. THE GameApiBuildingIndustry DTO SHALL include: Id (int, "Id"), Name (string, "name").
7. THE GameApiBuildingDetailRequirement DTO SHALL include all fields from the colonyBuildingDetailRequirement schema in the swagger.
8. THE GameApiBuildingAttribute DTO SHALL include: ModTypeId (int, "modTypeId"), PropertyName (string, "propertyName"), FriendlyPropertyName (string, "friendlyPropertyName"), PropertyValue (string, "propertyValue"), Unit (string, "unit").
9. THE GameApiBuildingExtraProperty DTO SHALL include: Info1 (string, "info1"), Info2 (string, "info2"), Info3 (string, "info3"), Info4 (string, "info4").
10. THE GameApiColonyWarehouseResponse DTO SHALL include a Contents property of type List&lt;GameApiWarehouseItem&gt; mapped to "contents", and a WarehouseCapacity (int, "warehouseCapacity") property.
11. THE GameApiWarehouseItem DTO SHALL include ALL of the following properties from the assetCargoItem schema: TypeId (int, "typeId"), Amount (int, "amount"), ResourceName (string, "resourceName"), TypeC (string, "typeC"), Icon (string, "icon"), Id (int?, "Id"), JobRef (int?, "jobRef"), JobDeliveryLoc (int?, "jobDeliveryLoc"), HealthPercentage (double?, "healthPercentage"), LastRepairHealthPercentage (double?, "lastRepairHealthPercentage"), Evolution (int?, "evolution"), Mass (double?, "mass"), Volume (double?, "volume"), Properties (List&lt;GameApiItemProperty&gt;, "properties"), JobName (string, "jobName"), JobTrack (string, "jobTrack"), ShipPartType (string, "shipPartType").
12. THE GameApiItemProperty DTO SHALL include: ModTypeId (int, "modTypeId"), PropertyName (string, "propertyName"), FriendlyPropertyName (string, "friendlyPropertyName"), PropertyValue (string, "propertyValue"), Unit (string, "unit").
13. IF the Game API response omits optional fields, THEN THE DTO deserialization SHALL use safe defaults (null for nullable types, 0 for integers, empty string for strings) without error.


### Requirement 5: Colony Sync Integration in Scheduler

**User Story:** As a player, I want colony data to be fetched automatically on the same polling schedule as my profile, so that my colony list stays current without manual action.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler performs a sync cycle for a character, THE Sync_Scheduler SHALL call GetColonyListAsync after the profile sync completes successfully.
2. IF the colony list API call fails, THEN THE Sync_Scheduler SHALL log the failure and continue without aborting the overall sync cycle (profile sync result is preserved).
3. WHEN the colony list API call succeeds, THE Sync_Scheduler SHALL deserialize the response and invoke the colony merge logic for the list data.
4. WHEN a colony in the list has RemoteAccess greater than 0, THE Sync_Scheduler SHALL call GetColonyBuildingsAsync and GetColonyWarehouseAsync for that colony's ColonyId.
5. IF a per-colony detail call (buildings or warehouse) fails with HTTP 403 or 404, THEN THE Sync_Scheduler SHALL log the failure and continue with the next colony (partial sync is acceptable).
6. IF the colony list API returns HTTP 401, THEN THE Sync_Scheduler SHALL handle it identically to a profile 401 (invalidate token, transition connection monitor to DisconnectedInvalidKey).
7. THE Sync_Scheduler SHALL log the number of colonies received from the API and the number of colonies created or updated after merge.


### Requirement 6: Colony Dedup Matching

**User Story:** As a player, I want API colonies to be matched to my existing local colonies, so that I do not get duplicate entries when the same colony is fetched from the API.

#### Acceptance Criteria

1. WHEN the Merge_Logic receives a GameApiColonyListItem, THE Merge_Logic SHALL search the local colony list for an existing colony with matching SystemObjectName (mapped to PlanetName) AND SystemName (case-insensitive comparison).
2. IF a Dedup_Match is found, THEN THE Merge_Logic SHALL update the existing colony with API data rather than creating a new colony.
3. IF no Dedup_Match is found, THEN THE Merge_Logic SHALL create a new Colony with a new UUID, set its OwnerUUID to the current player UUID, and add it to the local colony list.
4. THE Merge_Logic SHALL perform dedup matching only within colonies owned by the same player (OwnerUUID matches the syncing character's UUID).


### Requirement 7: Colony Field Merge — Game-Authoritative Fields

**User Story:** As a player, I want the API to update all of my colony's game-authoritative fields, so that renames, status changes, and game state are reflected locally.

#### Acceptance Criteria

1. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.ColonyName with the API ColonyName value using "API wins" strategy.
2. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.PlanetName with the API SystemObjectName value using "API wins" strategy.
3. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.SystemName with the API SystemName value using "API wins" strategy.
4. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.SystemId with the API SystemId value using "API wins" strategy.
5. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.ColonySize with the API ColonySize value using "API wins" strategy.
6. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.Distance with the API Distance value using "API wins" strategy.
7. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.SurfaceVariation with the API SurfaceVariation value using "API wins" strategy.
8. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.AtmosVariation with the API AtmosVariation value using "API wins" strategy.
9. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.HexValue with the API HexValue value using "API wins" strategy.
10. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.SystemObjectTypeName with the API SystemObjectTypeName value using "API wins" strategy.
11. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.ImagePreFix with the API ImagePreFix value using "API wins" strategy.
12. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.ManufacturingBlocked with the API ManufacturingBlocked value using "API wins" strategy.
13. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.WorkerCurrentAttitude with the API WorkerCurrentAttitude value using "API wins" strategy.
14. WHEN a Dedup_Match is found, THE Merge_Logic SHALL overwrite Colony.ContentmentIndex with the API ContentmentIndex value using "API wins" strategy.
15. THE Merge_Logic SHALL log each field conflict with the field name, old value, new value, and "API wins" strategy label, consistent with the profile merge logging pattern.
16. IF the API value for a string field is null or empty, THEN THE Merge_Logic SHALL leave the corresponding local field unchanged.


### Requirement 8: Building Merge

**User Story:** As a player, I want the API to update my colony's structure list with all building data, so that new structures built in-game appear in the tracker and all building details are kept current.

#### Acceptance Criteria

1. WHEN the API returns buildings for a colony, THE Merge_Logic SHALL match each API building to a local ColonyStructure by BlueprintDesignName (case-insensitive match against the local structure's flatpack blueprint name).
2. IF a matching local structure is found, THEN THE Merge_Logic SHALL update its built/online status from the API (BuildingOnline maps to the "Online" property; StatusId indicates construction state).
3. IF no matching local structure is found for an API building, THEN THE Merge_Logic SHALL create a new ColonyStructure with a new UUID, set its name from BlueprintDesignName, mark it as built and online per the API status, and add it to the colony's structure list.
4. THE Merge_Logic SHALL NOT remove local structures that are absent from the API response (the API may return a partial list or the colony may lack remote access).
5. THE Merge_Logic SHALL preserve Local_Only_Fields on existing structures: BuildCompletionTime, ProcessCompletionTime, BuildQueueSequence, ManufacturingBlueprintUUID, ManufacturingQuantity, ManufacturingCompleted, ManufacturingCommodityName, ResearchingBlueprintUUID, MiningLeftOvers, StagingResources, and OverflowRules.
6. WHEN the API provides ResourceName for a mining rig building, THE Merge_Logic SHALL update the local structure's MiningSurveyResource if the local value is empty.
7. WHEN the API provides ConstructingBuildingFinish (non-null DateTime), THE Merge_Logic SHALL set the local structure's BuildCompletionTime if no local build timer already exists.
8. WHEN the API provides ColonyBuildingTypeId, THE Merge_Logic SHALL store it on the local ColonyStructure.ColonyBuildingTypeId using "API wins" strategy.
9. WHEN the API provides ResourceId, THE Merge_Logic SHALL store it on the local ColonyStructure.ResourceId using "API wins" strategy.
10. WHEN the API provides ResourceIcon, THE Merge_Logic SHALL store it on the local ColonyStructure.ResourceIcon using "API wins" strategy.
11. WHEN the API provides ManufactureAmountPerRun, THE Merge_Logic SHALL store it on the local ColonyStructure.ManufactureAmountPerRun using "API wins" strategy.
12. WHEN the API provides DurabilityCurrent and DurabilityMax, THE Merge_Logic SHALL store them on the local ColonyStructure using "API wins" strategy.
13. WHEN the API provides OpsStatusEffects, THE Merge_Logic SHALL replace the local ColonyStructure.OpsStatusEffects collection with the API data.
14. WHEN the API provides Industries, THE Merge_Logic SHALL replace the local ColonyStructure.Industries collection with the API data.
15. WHEN the API provides DetailsRequired, THE Merge_Logic SHALL replace the local ColonyStructure.DetailsRequired collection with the API data.
16. WHEN the API provides SupportDetailsRequired, THE Merge_Logic SHALL replace the local ColonyStructure.SupportDetailsRequired collection with the API data.
17. WHEN the API provides BuildingAttributes, THE Merge_Logic SHALL replace the local ColonyStructure.BuildingAttributes collection with the API data.
18. WHEN the API provides ExtraProperties, THE Merge_Logic SHALL replace the local ColonyStructure.ExtraProperties collection with the API data.


### Requirement 9: Warehouse Item Merge

**User Story:** As a player, I want the API to update my colony's warehouse inventory with all item data from the assetCargoItem schema, so that resource quantities and item details reflect the current game state.

#### Acceptance Criteria

1. WHEN the API returns warehouse contents for a colony, THE Merge_Logic SHALL match each API item to a local Item in the colony's ItemBag by ResourceName + TypeC (type code).
2. IF a matching local item is found, THEN THE Merge_Logic SHALL update its Quantity to the API Amount value using "API wins" strategy.
3. IF no matching local item is found, THEN THE Merge_Logic SHALL create a new Item with a new UUID, set its properties from the API DTO (ResourceName maps to Name, TypeC maps to ItemType, Amount maps to Quantity), and add it to the colony's ItemBag.
4. THE Merge_Logic SHALL NOT remove local items that are absent from the API response (the API may return a partial inventory).
5. IF the API returns an Amount of 0 for an existing item, THEN THE Merge_Logic SHALL set the local item's Quantity to 0 (not remove it).
6. WHEN the API provides Mass for an item, THE Merge_Logic SHALL store it on the local Item.Mass using "API wins" strategy.
7. WHEN the API provides Volume for an item, THE Merge_Logic SHALL store it on the local Item.Volume using "API wins" strategy.
8. WHEN the API provides HealthPercentage for an item, THE Merge_Logic SHALL store it on the local Item.HealthPercentage using "API wins" strategy.
9. WHEN the API provides LastRepairHealthPercentage for an item, THE Merge_Logic SHALL store it on the local Item.LastRepairHealthPercentage using "API wins" strategy.
10. WHEN the API provides Evolution for an item, THE Merge_Logic SHALL store it on the local Item.Evolution using "API wins" strategy.
11. WHEN the API provides Id for an item, THE Merge_Logic SHALL store it on the local Item.GameItemId using "API wins" strategy.
12. WHEN the API provides JobRef, JobDeliveryLoc, JobName, or JobTrack for an item, THE Merge_Logic SHALL store them on the corresponding local Item properties using "API wins" strategy.
13. WHEN the API provides ShipPartType for an item, THE Merge_Logic SHALL store it on the local Item.ShipPartType using "API wins" strategy.
14. WHEN the API provides Properties for an item, THE Merge_Logic SHALL replace the local Item.ItemProperties collection with the API data.


### Requirement 10: LastImportDateTime Update

**User Story:** As a player, I want the tracker to record when each colony was last synced from the API, so that staleness tracking works correctly for API-synced colonies.

#### Acceptance Criteria

1. WHEN the Merge_Logic successfully merges API data into a colony (whether existing or newly created), THE Merge_Logic SHALL set Colony.LastImportDateTime to the current UTC time formatted as an ISO 8601 string.
2. THE LastImportDateTime update SHALL use SystemClock.UtcNow for testability.


### Requirement 11: Persistence After Sync

**User Story:** As a player, I want my synced colony data to be saved to disk, so that it persists across application restarts.

#### Acceptance Criteria

1. WHEN the colony merge completes with at least one colony created or updated, THE Sync_Scheduler SHALL trigger a PlayerContext save (WriteContext) to persist the changes to PlayerData.json.
2. IF no colonies were changed during the merge, THEN THE Sync_Scheduler SHALL NOT trigger a save (avoiding unnecessary disk writes).
3. THE save SHALL occur after all colonies in the API response have been merged (batch save, not per-colony save).


### Requirement 12: UI Notification After Sync

**User Story:** As a player, I want the colony form to refresh when new colony data arrives from the API, so that I see updated information without manually reopening the form.

#### Acceptance Criteria

1. WHEN the colony merge creates or updates colonies, THE Sync_Scheduler SHALL raise the existing ColonyDataChanged event on PlayerContext so that open forms refresh.
2. WHILE the Colony form is open and a Colony_Sync completes, THE Colony form SHALL refresh its colony list and the currently selected colony's details within the same UI update cycle triggered by the ColonyDataChanged event.
3. IF no colonies were changed during the merge, THEN THE Sync_Scheduler SHALL NOT raise the ColonyDataChanged event.


### Requirement 13: Error Handling and Graceful Degradation

**User Story:** As a player, I want colony sync failures to be handled gracefully, so that a bad API response does not corrupt my local data or crash the application.

#### Acceptance Criteria

1. IF the colony API response JSON is malformed or cannot be deserialized, THEN THE Sync_Scheduler SHALL log the error with the raw response (truncated to 500 characters) and skip the colony merge for this cycle.
2. IF an individual colony in the list has a null or empty SystemObjectName, THEN THE Merge_Logic SHALL skip that colony entry, log a warning, and continue processing remaining colonies.
3. IF an exception occurs during the merge of a single colony, THEN THE Merge_Logic SHALL log the error, skip that colony, and continue processing remaining colonies without aborting the entire batch.
4. THE Colony_Sync SHALL NOT modify any local colony data until the colony list response has been successfully deserialized (fail-fast before mutation).
5. IF the Game API returns an empty colony list (valid response with zero colonies), THEN THE Merge_Logic SHALL treat this as a no-op and NOT delete any existing local colonies.
6. IF a per-colony detail endpoint (buildings, warehouse) returns an error for a specific colony, THEN THE Merge_Logic SHALL still merge the colony list data (name, system) for that colony and skip only the detail merge.


### Requirement 14: Scope Awareness

**User Story:** As a player, I want the tracker to handle missing API scopes gracefully, so that partial data is still synced when I have not granted all colony scopes.

#### Acceptance Criteria

1. IF the token lacks the colony.list.read scope (HTTP 403 on GET /v1/colonies), THEN THE Sync_Scheduler SHALL log "Colony sync skipped: colony.list.read scope not granted" and skip the entire colony sync without error.
2. IF the token has colony.list.read but lacks colony.buildings.read, THEN THE Sync_Scheduler SHALL sync colony list data (names, systems) but skip building detail retrieval for all colonies.
3. IF the token has colony.list.read but lacks colony.warehouse.read, THEN THE Sync_Scheduler SHALL sync colony list data and buildings (if scope available) but skip warehouse retrieval for all colonies.
4. THE Sync_Scheduler SHALL determine available scopes from the first failed request (HTTP 403) and cache the result for the remainder of the sync cycle to avoid redundant failed requests.


### Requirement 15: Sync Status Logging

**User Story:** As a developer, I want detailed logging of the colony sync process, so that I can diagnose sync issues and verify correct behavior.

#### Acceptance Criteria

1. WHEN a colony sync cycle begins, THE Sync_Scheduler SHALL log "Colony sync starting for character {UUID}".
2. WHEN the API returns colonies, THE Sync_Scheduler SHALL log "Received {N} colonies from game API for character {UUID}".
3. WHEN a new colony is created from API data, THE Merge_Logic SHALL log "Created new colony: {PlanetName} / {ColonyName} in {SystemName} (UUID={newUUID})".
4. WHEN an existing colony is updated, THE Merge_Logic SHALL log "Updated colony: {PlanetName} / {ColonyName} (UUID={existingUUID}), {N} fields changed".
5. WHEN the colony sync cycle completes, THE Sync_Scheduler SHALL log "Colony sync complete for character {UUID}: {created} created, {updated} updated, {skipped} skipped".
6. WHEN a per-colony detail request is skipped due to RemoteAccess being 0, THE Sync_Scheduler SHALL log "Skipping detail sync for colony {ColonyName} (colonyId={id}): no Remote Operations Array".


### Requirement 16: API Data Discovery and Mapping Validation

**User Story:** As a developer, I want to pull real game API data and compare it to the local data model, so that I can identify and resolve data mapping mismatches before implementing the full merge logic.

#### Acceptance Criteria

1. THE project SHALL include a standalone console test application (or test fixture) that authenticates with the game API using real credentials and pulls colony list, buildings, and warehouse data.
2. THE test application SHALL output the raw JSON responses to files for manual inspection and comparison against the local Colony, ColonyStructure, and Item models.
3. THE test application SHALL produce a mapping report that identifies: (a) API fields with no local equivalent, (b) local fields with no API equivalent, (c) fields where the data type or format differs (e.g. int ID vs string UUID, type code vs enum name), and (d) naming mismatches between API field names and local property names.
4. THE mapping report SHALL specifically document how the following game API concepts map to local model concepts:
   - colonyId (int) → Colony.UUID (string GUID)
   - systemObjectName → Colony.PlanetName
   - colonyBuildingTypeId / blueprintDesignName → ColonyStructure.FlatpackBlueprintUUID / structure name
   - statusId (int) → Built/Staged/Online property flags
   - typeC / typeId → ItemType enum / resource identification
   - resourceName → Item.Name / Item.BaseItemTypeID
5. THE mapping report SHALL be committed to the spec directory at `.kiro/specs/colony-api-sync/api-mapping-report.md` so that the merge logic implementation can reference it.
6. WHEN the mapping report reveals data mismatches that require design decisions (e.g. how to correlate integer building IDs with local UUID-based structures), THE report SHALL document each mismatch with proposed resolution options.


### Requirement 17: Data Model Expansion

**User Story:** As a developer, I want the local data models (Colony, ColonyStructure, Item) to store ALL fields returned by the game API, so that no API data is discarded during sync.

#### Acceptance Criteria

1. THE Colony model SHALL include the following new properties to store colony list API fields: SystemId (int), ColonySize (int), Distance (decimal), SurfaceVariation (int), AtmosVariation (int), HexValue (string), SystemObjectTypeName (string), ImagePreFix (string), ManufacturingBlocked (int), WorkerCurrentAttitude (int), ContentmentIndex (int).
2. THE ColonyStructure model SHALL include the following new properties to store building API fields: ColonyBuildingTypeId (int), ResourceId (int), ResourceIcon (string), ManufactureAmountPerRun (int), DurabilityCurrent (decimal), DurabilityMax (decimal), OpsStatusEffects (List&lt;BuildingStatusEffect&gt;), Industries (List&lt;BuildingIndustry&gt;), DetailsRequired (List&lt;BuildingDetailRequirement&gt;), SupportDetailsRequired (List&lt;BuildingDetailRequirement&gt;), BuildingAttributes (List&lt;BuildingAttribute&gt;), ExtraProperties (List&lt;BuildingExtraProperty&gt;).
3. THE Item model SHALL include the following new properties to store assetCargoItem API fields: GameItemId (int?), JobRef (int?), JobDeliveryLoc (int?), HealthPercentage (decimal?), LastRepairHealthPercentage (decimal?), Evolution (int?), Mass (decimal?), ShipPartType (string), JobName (string), JobTrack (string), ItemProperties (List&lt;ItemProperty&gt;).
4. THE Item model SHALL retain the Volume property as decimal (project convention: all floating-point fields use decimal for precision).
5. THE BuildingStatusEffect model SHALL include: StatusId (int), ModTypeId (int), Change (decimal).
6. THE BuildingIndustry model SHALL include: Id (int), Name (string).
7. THE BuildingDetailRequirement model SHALL include all fields from the colonyBuildingDetailRequirement swagger schema.
8. THE BuildingAttribute model SHALL include: ModTypeId (int), PropertyName (string), FriendlyPropertyName (string), PropertyValue (string), Unit (string).
9. THE BuildingExtraProperty model SHALL include: Info1 (string), Info2 (string), Info3 (string), Info4 (string).
10. THE ItemProperty model SHALL include: ModTypeId (int), PropertyName (string), FriendlyPropertyName (string), PropertyValue (string), Unit (string).
11. ALL new collection properties SHALL be initialized to empty lists in their constructors (not null).
12. ALL new nullable properties SHALL default to null; all new int properties SHALL default to 0; all new string properties SHALL default to empty string.
13. ALL new properties SHALL be serialized to JSON using Newtonsoft.Json with appropriate JsonProperty attributes for camelCase naming.


### Requirement 18: Data Model Migration — Field Relocation

**User Story:** As a developer, I want to move WorkerCurrentAttitude and ContentmentIndex from ColonyStructure to Colony, so that the local model matches the game API's data structure while maintaining backward compatibility with existing saved data.

#### Acceptance Criteria

1. THE Colony model SHALL have a WorkerCurrentAttitude property of type int (matching the API type), replacing the string CurrentAttitude that was previously on ColonyStructure.
2. THE Colony model SHALL have a ContentmentIndex property of type int (matching the API type), replacing the int ContentmentIndex that was previously on ColonyStructure.
3. THE ColonyStructure model SHALL retain the CurrentAttitude (string) and ContentmentIndex (int) properties as deprecated, marked with [JsonProperty] for deserialization of existing saved data but excluded from new serialization output.
4. WHEN loading existing PlayerData.json that has CurrentAttitude on ColonyStructure, THE deserialization SHALL read the legacy value without error.
5. WHEN saving PlayerData.json after a sync, THE serialization SHALL write WorkerCurrentAttitude and ContentmentIndex on the Colony object and SHALL NOT write CurrentAttitude or ContentmentIndex on ColonyStructure objects.
6. WHEN loading legacy data where ColonyStructure has CurrentAttitude (string) and the Colony does not yet have WorkerCurrentAttitude, THE Colony model SHALL migrate the value by parsing the string to int (defaulting to 0 if parsing fails) from the first structure that has a non-empty value.
7. WHEN loading legacy data where ColonyStructure has ContentmentIndex and the Colony does not yet have ContentmentIndex, THE Colony model SHALL migrate the value from the first structure that has a non-zero value.
8. THE migration logic SHALL run once during deserialization (or on first access after load) and SHALL NOT require a separate migration tool or manual intervention.
9. AFTER migration completes, THE deprecated ColonyStructure.CurrentAttitude and ColonyStructure.ContentmentIndex properties SHALL be cleared (set to empty string and 0 respectively) so they are not re-migrated on subsequent loads.

