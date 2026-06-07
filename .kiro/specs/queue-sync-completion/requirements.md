# Requirements Document

## Introduction

The QueueSyncService was implemented with many work item factories as stubs — they make API calls through the queue (getting parallelism, rate governing, retry, 401/429 handling) but discard the response data without parsing or persisting it. The old GameApiSyncScheduler had real implementations that parsed JSON, merged data into PlayerContext, and fired UI events. This spec completes all Priority 1 stubs (achieving parity with the old sync) and adds Priority 2 ship configuration parsing.

## Glossary

- **Queue_Sync_Service**: The service in `QueueSyncService.cs` that orchestrates background API synchronization using GameApiRequestQueue for parallel dispatch.
- **ProfileMergeService**: A new standalone service class extracted from GameApiSyncScheduler.MergeProfileData that handles merging remote profile data into local PlayerProfile.
- **ColonyMergeService**: The existing static service that provides MergeColonyList, MergeBuildings, MergeWarehouse, and MergeWorkers methods.
- **AssetMergeService**: The existing static service that provides MergeColonyAssets, MergeStationAssets, and MergeShipAssets methods.
- **BankingService**: The existing static service that provides ImportBalanceAsync and ImportTransactionsAsync methods.
- **ColonyMergeResult**: The result object from ColonyMergeService.MergeColonyList containing Created/Updated/Skipped counts and a ColonyIdToUUIDMap dictionary.
- **ColonyIdToUUIDMap**: A Dictionary&lt;int, string&gt; mapping game API ColonyId integers to local Colony UUIDs, produced by MergeColonyList and needed by child colony work items.
- **GameApiServiceResponse**: A generic envelope DTO wrapping API responses with Success flag and Data payload.
- **WorkItem**: A unit of work dispatched by GameApiRequestQueue, consisting of a label and an async delegate that may return cascaded follow-up items.
- **PlayerContext**: The singleton that manages player data in-memory and persists to disk via WriteContext.


## Requirements

### Requirement 1: ProfileMergeService Extraction

**User Story:** As a developer, I want profile merge logic extracted into a standalone ProfileMergeService class, so that QueueSyncService can call it without depending on GameApiSyncScheduler.

#### Acceptance Criteria

1. THE ProfileMergeService SHALL be a static class containing MergeProfileData, MergeRank, and MergeSkills methods extracted from GameApiSyncScheduler.
2. THE ProfileMergeService SHALL accept a local PlayerProfile and a GameApiProfileResponse and return a boolean indicating whether any fields changed.
3. THE ProfileMergeService SHALL apply the "API wins" merge strategy for all game-authoritative fields (Faction, CitizenId, SkillPoints, CharacterId, FirstName, LastName, ActiveTimeMinutes).
4. THE ProfileMergeService SHALL merge rank data (Public, Private, Military) from the remote Ranks object into local PlayerRank instances.
5. THE ProfileMergeService SHALL merge skill levels from the remote Skills list, preserving local TrainingStarted and CompletionTime fields.
6. THE ProfileMergeService SHALL log each field conflict at Info level with both old and new values.

### Requirement 2: Character Profile Work Item Completion

**User Story:** As a player, I want the character profile sync to actually merge remote data into my local profile, so that my profile stays up to date.

#### Acceptance Criteria

1. WHEN the CharacterProfile work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as GameApiServiceResponse&lt;GameApiProfileResponse&gt;.
2. WHEN deserialization succeeds, THE Queue_Sync_Service SHALL call ProfileMergeService.MergeProfileData with the local player profile and the deserialized remote profile.
3. IF deserialization fails with a JsonException, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.


### Requirement 3: Banking Balance Work Item Completion

**User Story:** As a player, I want the banking balance sync to parse and store my account balance, so that the app displays my current credits.

#### Acceptance Criteria

1. WHEN the BankingBalance work item receives a successful response, THE Queue_Sync_Service SHALL delegate to BankingService.ImportBalanceAsync with the API client, AppId, and access token.
2. WHEN ImportBalanceAsync returns a non-null decimal value, THE Queue_Sync_Service SHALL call PlayerContext.SetBankingBalance with the returned value.
3. WHEN the balance is successfully updated, THE Queue_Sync_Service SHALL raise the BankingDataChanged event.
4. IF ImportBalanceAsync returns null, THEN THE Queue_Sync_Service SHALL log a warning and not modify the stored balance.

### Requirement 4: Colony List Work Item Completion

**User Story:** As a player, I want the colony list sync to merge remote colonies into my local colony list and pass the ColonyId-to-UUID mapping to child items, so that per-colony detail fetches know which local colony to update.

#### Acceptance Criteria

1. WHEN the ColonyList work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as GameApiServiceResponse&lt;GameApiColonyListResponse&gt;.
2. WHEN deserialization succeeds, THE Queue_Sync_Service SHALL call ColonyMergeService.MergeColonyList with the API colonies, local colony list, and player UUID.
3. THE Queue_Sync_Service SHALL store the ColonyMergeResult.ColonyIdToUUIDMap so that cascaded colony detail work items can locate the correct local Colony by API ColonyId.
4. WHEN the merge result indicates changes (HasChanges is true), THE Queue_Sync_Service SHALL raise the ColonyDataChanged event.
5. THE cascaded ColonyBuildings, ColonyWarehouse, and ColonyWorkers work items SHALL each receive both the ColonyId (for the API call) and the local Colony UUID (for merge targeting).


### Requirement 5: Colony Buildings Work Item Completion

**User Story:** As a player, I want the colony buildings sync to merge building data into my local colony structures, so that my colony view reflects the current buildings from the game.

#### Acceptance Criteria

1. WHEN the ColonyBuildings work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as GameApiServiceResponse&lt;GameApiColonyBuildingsResponse&gt;.
2. WHEN deserialization succeeds and Data.Buildings is non-null, THE Queue_Sync_Service SHALL resolve the target Colony using the ColonyId-to-UUID mapping and call ColonyMergeService.MergeBuildings with the buildings list and the local Colony.
3. IF the ColonyId cannot be resolved to a local Colony UUID, THEN THE Queue_Sync_Service SHALL log a warning and skip the merge.
4. IF deserialization fails, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.

### Requirement 6: Colony Warehouse Work Item Completion

**User Story:** As a player, I want the colony warehouse sync to merge warehouse contents into my local colony, so that my inventory view shows current items.

#### Acceptance Criteria

1. WHEN the ColonyWarehouse work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as GameApiServiceResponse&lt;GameApiColonyWarehouseResponse&gt;.
2. WHEN deserialization succeeds and Data.Contents is non-null, THE Queue_Sync_Service SHALL resolve the target Colony and call ColonyMergeService.MergeWarehouse with the contents, the local Colony, and linkage services (BlueprintLinkageService and SurveyLinkageService).
3. IF the ColonyId cannot be resolved to a local Colony UUID, THEN THE Queue_Sync_Service SHALL log a warning and skip the merge.
4. IF deserialization fails, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.

### Requirement 7: Colony Workers Work Item Completion

**User Story:** As a player, I want the colony workers sync to merge worker data into my local colony, so that my colony view reflects current workforce allocation.

#### Acceptance Criteria

1. WHEN the ColonyWorkers work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as GameApiServiceResponse&lt;GameApiColonyWorkersResponse&gt;.
2. WHEN deserialization succeeds and Data is non-null, THE Queue_Sync_Service SHALL resolve the target Colony and call ColonyMergeService.MergeWorkers with the response data and the local Colony.
3. IF the ColonyId cannot be resolved to a local Colony UUID, THEN THE Queue_Sync_Service SHALL log a warning and skip the merge.
4. IF deserialization fails, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.


### Requirement 8: Asset Location Detail — Generic Cargo Merge and Detail Cascading

**User Story:** As a player, I want the asset location detail sync to merge ALL cargo items (not just crate/bp/survey) into the appropriate local entity, so that colony, station, and ship inventories reflect current contents.

#### Acceptance Criteria

1. WHEN the AssetLocationDetail work item receives a successful response with cargo items, THE Queue_Sync_Service SHALL call the appropriate AssetMergeService method based on the location type code: MergeColonyAssets for "Co", MergeStationAssets for "St", MergeShipAssets for "Sh".
2. WHEN the location type is "Co", THE Queue_Sync_Service SHALL resolve the Colony by matching locationId to Colony.ColonyId and pass the cargo items to AssetMergeService.MergeColonyAssets.
3. WHEN the location type is "St", THE Queue_Sync_Service SHALL find or create a Station by GameLocationId and pass the cargo items to AssetMergeService.MergeStationAssets with the player's hold ItemBag.
4. WHEN the location type is "Sh", THE Queue_Sync_Service SHALL find or create a Ship by GameLocationId and pass the cargo items to AssetMergeService.MergeShipAssets.
5. WHEN a station cannot be found by GameLocationId, THE Queue_Sync_Service SHALL attempt name-based fallback matching, then create a new Station if no match exists (replicating the old RouteAssetMerge behavior).
6. WHEN a ship cannot be found by GameLocationId, THE Queue_Sync_Service SHALL attempt name-based fallback matching, then create a new Ship if no match exists.
7. AFTER the generic cargo merge completes, THE Queue_Sync_Service SHALL cascade Crate, Blueprint, and Survey detail work items for matching cargo entries (CreateCrateDetailItem for Crate entries, CreateBlueprintDetailItem for Blueprint entries if not fresh, CreateSurveyDetailItem for Survey entries if not fresh).
8. WHEN the location type is unknown, THE Queue_Sync_Service SHALL log a warning and skip the cargo merge and detail cascading.

### Requirement 9: Ship Cargo Work Item Completion

**User Story:** As a player, I want the ship cargo endpoint to merge cargo items into my active ship and cascade detail items for special cargo types, so that my ship inventory view reflects current contents including crate, blueprint, and survey details.

#### Acceptance Criteria

1. WHEN the ShipCargo work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response as an array of GameApiAssetCargoItem objects.
2. THE Queue_Sync_Service SHALL identify the player's active ship (the ship matching the current GameLocationId context or the first ship in the player's ship list).
3. WHEN the active ship is identified, THE Queue_Sync_Service SHALL call AssetMergeService.MergeShipAssets with the deserialized cargo items and the ship.
4. AFTER the generic cargo merge completes, THE Queue_Sync_Service SHALL cascade detail work items for special cargo entries: CreateCrateDetailItem for each Crate cargo entry, CreateBlueprintDetailItem for each Blueprint cargo entry if not fresh, and CreateSurveyDetailItem for each Survey cargo entry if not fresh.
5. IF no active ship can be identified, THEN THE Queue_Sync_Service SHALL log a warning and skip the merge and detail cascading.
6. IF deserialization fails, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.


### Requirement 10: Ship Configuration Work Item Completion

**User Story:** As a player, I want the ship configuration endpoint to populate my ship's component slots and hull HP, so that the app can display my ship's fitted equipment and health.

#### Acceptance Criteria

1. WHEN the ShipConfiguration work item receives a successful response, THE Queue_Sync_Service SHALL deserialize the response into a ship configuration DTO containing component entries and hull HP data.
2. THE Queue_Sync_Service SHALL match or create a Ship entity by the shipId/GameLocationId from the response.
3. WHEN a matching ship is found, THE Queue_Sync_Service SHALL map each component entry to a ShipComponentSlot with SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent.
4. THE Queue_Sync_Service SHALL replace the ship's Components list with the mapped slots from the response.
5. THE Queue_Sync_Service SHALL set HullCurrentHP, HullMaxHP, and HullMaxRepairPercent on the Ship from the hull HP data in the response.
6. IF no ship can be matched or created from the response, THEN THE Queue_Sync_Service SHALL log a warning and skip the merge.
7. IF deserialization fails, THEN THE Queue_Sync_Service SHALL log the error and not modify local data.

### Requirement 11: UI Event Firing

**User Story:** As a player, I want the UI to refresh automatically when sync merges new data, so that I see updated information without restarting the app.

#### Acceptance Criteria

1. WHEN colony data is merged and changes are detected, THE Queue_Sync_Service SHALL raise the ColonyDataChanged event.
2. WHEN banking balance is updated, THE Queue_Sync_Service SHALL raise the BankingDataChanged event.
3. WHEN asset data is merged (colony assets, station assets, or ship assets) and changes are detected, THE Queue_Sync_Service SHALL raise the AssetDataChanged event.
4. THE Queue_Sync_Service SHALL batch event raising to occur after all merge operations for a given work item complete (not per-item within a merge).

### Requirement 12: Error Handling in Completed Work Items

**User Story:** As a developer, I want each completed work item to handle deserialization and merge errors gracefully, so that a single bad response does not abort the sync cycle.

#### Acceptance Criteria

1. IF deserialization throws a JsonException in any work item, THEN THE Queue_Sync_Service SHALL log the exception with the work item label and continue without modifying local data.
2. IF a merge service method throws an unexpected exception, THEN THE Queue_Sync_Service SHALL log the exception with the work item label and continue processing other items.
3. THE Queue_Sync_Service SHALL not call WriteContext for data from a work item that encountered a deserialization or merge error.
4. THE Queue_Sync_Service SHALL truncate logged JSON response bodies to 500 characters to prevent log flooding from large malformed responses.


### Requirement 13: Ship Configuration Response Model

**User Story:** As a developer, I want a DTO for the ship configuration API response, so that the configuration work item can deserialize and map the data.

#### Acceptance Criteria

1. THE GameApiShipConfigurationResponse DTO SHALL include a list of component entries, each with: slotType (string), slotIndex (int), blueprintId (int), currentHp (int), maxHp (int), and maxRepairPercent (decimal).
2. THE GameApiShipConfigurationResponse DTO SHALL include hull-level fields: hullCurrentHp (int), hullMaxHp (int), hullMaxRepairPercent (decimal), and shipId (int).
3. THE DTO SHALL be deserialized from the GameApiServiceResponse envelope pattern consistent with other API responses.
4. THE component blueprintId field SHALL be used to resolve a local Blueprint UUID via the existing GameApiBlueprintId index (or stored as-is if no match exists).

### Requirement 14: Colony Work Item Context Passing

**User Story:** As a developer, I want cascaded colony detail work items to receive the ColonyId-to-UUID context from the parent ColonyList work item, so that they can locate the correct local Colony for merging.

#### Acceptance Criteria

1. WHEN CreateColonyListItem cascades child items, THE Queue_Sync_Service SHALL pass the ColonyMergeResult.ColonyIdToUUIDMap to each CreateColonyBuildingsItem, CreateColonyWarehouseItem, and CreateColonyWorkersItem factory.
2. WHEN a colony detail work item executes, THE Queue_Sync_Service SHALL use the ColonyIdToUUIDMap to resolve the local Colony by UUID before calling the merge service.
3. IF the ColonyIdToUUIDMap does not contain the target ColonyId, THEN THE Queue_Sync_Service SHALL attempt direct lookup by ColonyId in the player's colony list as a fallback.
4. THE context passing mechanism SHALL not require global mutable state — each cascaded work item SHALL capture its required context via closure or parameter.



### Requirement 15: Consistent Cargo Detail Cascading

**User Story:** As a developer, I want all work items that process cargo lists to cascade detail items using the same logic, so that crate, blueprint, and survey details are handled consistently regardless of the cargo source.

#### Acceptance Criteria

1. THE Queue_Sync_Service SHALL apply the same cargo detail cascading logic in all work items that process cargo lists: CreateAssetLocationDetailItem (for colonies, stations, and ships) and CreateShipCargoItem (for the active ship).
2. WHEN a cargo list contains Crate entries, THE Queue_Sync_Service SHALL always cascade a CreateCrateDetailItem for each Crate entry.
3. WHEN a cargo list contains Blueprint entries, THE Queue_Sync_Service SHALL cascade a CreateBlueprintDetailItem for each Blueprint entry only if the local blueprint data is not fresh (freshness check fails).
4. WHEN a cargo list contains Survey entries, THE Queue_Sync_Service SHALL cascade a CreateSurveyDetailItem for each Survey entry only if the local survey data is not fresh (freshness check fails).
5. THE cascading logic SHALL be identical across all cargo-processing work items — the same freshness thresholds, the same work item factory methods, and the same skip conditions SHALL apply regardless of whether the cargo source is a colony, station, ship location detail, or the active ship cargo endpoint.
