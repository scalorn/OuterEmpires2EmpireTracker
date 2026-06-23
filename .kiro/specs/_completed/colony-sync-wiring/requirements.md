# Requirements Document

## Introduction

This spec covers wiring the ColonyMergeService (implemented in the colony-api-sync spec) into production use, adding a manual "Sync Colony" button to FormColonyV2 for on-demand single-colony synchronization, and extending the sync to include worker/commodity-demand data from the game API's `/v1/colonies/{colonyId}/workers` endpoint.

The colony-api-sync spec implemented the merge logic and scheduler orchestration with virtual method stubs. This spec connects those stubs to the real PlayerContext, adds a user-facing manual sync trigger, and adds the workers endpoint integration (client method, DTOs, merge logic) for both automatic and manual sync paths.

## Glossary

- **GameApiSyncScheduler**: The background scheduler that periodically syncs player data from the game API. Contains virtual methods for PlayerContext access.
- **GameApiContext**: Top-level singleton coordinating all game API components. Owns the scheduler instance and is responsible for wiring production overrides.
- **PlayerContext**: Singleton managing all player data in memory. Provides colony lists, persistence (WriteContext), and change notification events (ColonyDataChanged).
- **ColonyMergeService**: Static class implementing colony list merge, building merge, and warehouse merge logic (implemented in colony-api-sync spec).
- **FormColonyV2**: The WinForms colony management form. Already subscribes to ColonyDataChanged for refresh.
- **Production_Scheduler**: A subclass of GameApiSyncScheduler that overrides virtual methods to connect to PlayerContext.
- **Sync_Button**: The manual sync button control on FormColonyV2.
- **GameApiClient**: The HTTP client for game API calls, accessed via GameApiContext.Instance.Client.
- **CommodityRequested**: Local model representing a workforce commodity demand on a colony (Name, Requested, Delivered, NeedBy, Fulfilled).
- **WorkforceCommodityDemand**: API response object from `/v1/colonies/{colonyId}/workers` representing a commodity the workforce needs (typeName, amount, requiredBy, fulfilled).

## Requirements

### Requirement 1: Production Scheduler Subclass

**User Story:** As a developer, I want the GameApiSyncScheduler's virtual methods wired to PlayerContext in production, so that colony sync actually reads and writes real player data.

#### Acceptance Criteria

1. WHEN GameApiContext.Initialize() creates the scheduler, THE Production_Scheduler SHALL be instantiated instead of the base GameApiSyncScheduler
2. WHEN GetPlayerProfile is called with a player UUID, THE Production_Scheduler SHALL return the matching PlayerProfile from PlayerContext (or null if not found)
3. WHEN GetPlayerColonies is called with a player UUID, THE Production_Scheduler SHALL return the mutable colony list filtered by OwnerUUID from PlayerContext
4. WHEN WriteContext is called, THE Production_Scheduler SHALL call PlayerContext.WriteContext() to persist data to disk
5. WHEN RaiseColonyDataChanged is called, THE Production_Scheduler SHALL invoke PlayerContext.OnColonyDataChanged with an empty string colony UUID to signal a bulk refresh

### Requirement 2: Scheduler Wiring Integration

**User Story:** As a developer, I want the production scheduler to be transparent to the rest of the system, so that existing GameApiContext consumers work without changes.

#### Acceptance Criteria

1. THE GameApiContext SHALL expose the scheduler via its existing SyncScheduler property with no type change (the property type remains GameApiSyncScheduler)
2. THE Production_Scheduler SHALL accept the same constructor parameters as GameApiSyncScheduler plus a PlayerContext reference
3. WHEN GameApiContext is disposed, THE Production_Scheduler SHALL stop and release resources identically to the base scheduler

### Requirement 3: Manual Sync Button — UI Placement

**User Story:** As a player, I want a "Sync Colony" button on the colony form, so that I can manually refresh a colony's data from the game API without waiting for the next automatic sync cycle.

#### Acceptance Criteria

1. THE FormColonyV2 SHALL display a Sync_Button in the colony header area (near the colony name/selection controls)
2. THE Sync_Button SHALL display the text "Sync" with a tooltip explaining "Sync buildings, warehouse, and workers from game API"
3. WHILE no colony is selected, THE Sync_Button SHALL be disabled
4. WHILE GameApiContext.Instance is null (game API not configured), THE Sync_Button SHALL be disabled
5. WHILE GameApiContext.Instance is not null AND a colony is selected, THE Sync_Button SHALL be enabled

### Requirement 4: Manual Sync Button — Sync Execution

**User Story:** As a player, I want the sync button to fetch fresh buildings, warehouse, and worker data for my selected colony, so that I can see the latest state without restarting the app.

#### Acceptance Criteria

1. WHEN the user clicks the Sync_Button, THE FormColonyV2 SHALL call GetColonyBuildingsAsync, GetColonyWarehouseAsync, and GetColonyWorkersAsync for the selected colony's ColonyId via GameApiContext.Instance.Client
2. WHEN the sync is initiated, THE FormColonyV2 SHALL obtain the access token by calling the token exchange on GameApiContext.Instance.Client using the current player's credentials from GameApiContext.Instance.CredentialManager
3. WHEN building data is received successfully, THE FormColonyV2 SHALL call ColonyMergeService.MergeBuildings to merge the response into the selected colony
4. WHEN warehouse data is received successfully, THE FormColonyV2 SHALL call ColonyMergeService.MergeWarehouse to merge the response into the selected colony
5. WHEN worker data is received successfully, THE FormColonyV2 SHALL call ColonyMergeService.MergeWorkers to merge the response into the selected colony
6. WHEN any merge produces changes, THE FormColonyV2 SHALL call PlayerContext.WriteContext() and PlayerContext.OnColonyDataChanged to persist and notify
7. IF the selected colony has no ColonyId (value is 0), THEN THE FormColonyV2 SHALL show a message indicating the colony has not been synced from the API yet and abort the sync

### Requirement 5: Manual Sync Button — Status Feedback

**User Story:** As a player, I want to see whether the sync is in progress, succeeded, or failed, so that I know the current state of my data.

#### Acceptance Criteria

1. WHEN sync begins, THE Sync_Button SHALL change its text to "Syncing..." and become disabled to prevent double-clicks
2. WHEN sync completes successfully, THE Sync_Button SHALL briefly display "Done" for 2 seconds, then revert to "Sync"
3. IF sync fails (network error, HTTP 401, HTTP 403, or other error), THEN THE Sync_Button SHALL briefly display "Error" for 2 seconds, then revert to "Sync"
4. WHEN sync completes (success or failure), THE Sync_Button SHALL re-enable (assuming a colony is still selected and API is configured)
5. IF the token exchange fails (HTTP 401 from token endpoint), THEN THE FormColonyV2 SHALL log the error and display "Error" status without crashing

### Requirement 6: Manual Sync Button — Rate Limiting

**User Story:** As a developer, I want the manual sync to respect rate limits, so that users cannot spam the game API with rapid repeated clicks.

#### Acceptance Criteria

1. THE FormColonyV2 SHALL enforce a minimum 10-second cooldown between manual sync attempts
2. WHILE the cooldown is active, THE Sync_Button SHALL remain disabled
3. WHEN the cooldown expires, THE Sync_Button SHALL re-enable (assuming a colony is still selected and API is configured)

### Requirement 7: Colony ID Availability

**User Story:** As a developer, I want the colony's game API ID to be accessible for manual sync, so that the button can call the correct API endpoints.

#### Acceptance Criteria

1. THE Colony model SHALL expose a ColonyId property (int) that stores the game API colony identifier
2. WHEN ColonyMergeService.MergeColonyList runs, THE ColonyMergeService SHALL populate ColonyId on matched colonies from the API response
3. WHILE a colony has ColonyId equal to 0, THE FormColonyV2 SHALL treat it as not yet synced from the API

### Requirement 8: Automatic Refresh After Sync

**User Story:** As a player, I want the colony display to update automatically after a manual sync completes, so that I see the latest data without manually navigating away and back.

#### Acceptance Criteria

1. WHEN manual sync completes successfully and changes were merged, THE FormColonyV2 SHALL refresh the colony display via the existing ColonyDataChanged event handler
2. THE FormColonyV2 SHALL NOT require the user to reselect the colony or switch tabs to see updated data

### Requirement 9: Error Handling and Resilience

**User Story:** As a player, I want the manual sync to handle errors gracefully, so that a failed sync does not corrupt my local data or crash the application.

#### Acceptance Criteria

1. IF GetColonyBuildingsAsync returns HTTP 401, THEN THE FormColonyV2 SHALL log the error, display "Error" status, and transition the connection monitor to DisconnectedInvalidKey
2. IF GetColonyBuildingsAsync returns HTTP 403, THEN THE FormColonyV2 SHALL log that the buildings scope is not granted and display "Error" status
3. IF GetColonyBuildingsAsync returns HTTP 404, THEN THE FormColonyV2 SHALL log that the colony was not found on the server and display "Error" status
4. IF a network exception occurs during sync, THEN THE FormColonyV2 SHALL log the exception and display "Error" status without crashing
5. IF warehouse sync fails but building sync succeeded, THE FormColonyV2 SHALL still persist and display the building changes (partial success is acceptable)
6. THE FormColonyV2 SHALL execute all sync operations on a background thread and marshal UI updates back to the UI thread via BeginInvoke
7. IF worker sync fails (HTTP 403 scope not granted, HTTP 404, or network error) but buildings/warehouse succeeded, THE FormColonyV2 SHALL still persist and display the building/warehouse changes (partial success is acceptable)


### Requirement 10: Workers API Client Method

**User Story:** As a developer, I want a GameApiClient method to fetch colony worker data, so that both automatic and manual sync can retrieve workforce and commodity demand information.

#### Acceptance Criteria

1. THE GameApiClient SHALL expose a `GetColonyWorkersAsync(string appId, string accessToken, int colonyId)` method returning `(bool Success, string Json)`
2. THE method SHALL follow the same pattern as GetColonyBuildingsAsync: rate-limit token acquisition, Polly policies, HTTP status code handling (200→success, 401→"401", 403→"403", 404→"404")
3. THE method SHALL call `GET /v1/colonies/{colonyId}/workers` with `Authorization: Bearer {accessToken}` and `X-App-Id: {appId}` headers

### Requirement 11: Workers Response DTOs

**User Story:** As a developer, I want typed DTOs for the workers API response, so that the JSON can be deserialized into strongly-typed objects for merge logic.

#### Acceptance Criteria

1. THE GameApiColonyWorkersResponse SHALL contain: WorkerCurrentAttitude (int), ColonyModifiers (list), ColonyCapacities (object), WorkforceCommodityDemands (list), WorkforceOverview (object), WorkforceDetail (list), Wages (object)
2. THE GameApiCommodityDemand DTO SHALL contain: Id (int), TypeC (string), CommodityType (string), TypeId (int), TypeName (string), Amount (int), RequiredBy (DateTime), Fulfilled (bool)
3. THE GameApiWorkforceOverview DTO SHALL contain: BlueCollarAllocated (int), BlueCollarUnallocated (int), WhiteCollarAllocated (int), WhiteCollarUnallocated (int), SpecialistAllocated (int), SpecialistUnallocated (int)
4. THE GameApiColonyWorkerDetail DTO SHALL contain: WorkerId (int), WorkerTypeId (int), BuildingTypeId (int), Name (string), DownTools (int)
5. THE GameApiColonyWages DTO SHALL contain: CurrentWagePercentage (int), GalacticWageStandard (int), CurrentWageBillPerCycle (int), LastWageChange (DateTime?)
6. THE GameApiColonyModifier DTO SHALL contain: Id (int?), Description (string), ModifierNumber (int), Positive (bool), Temporary (bool)
7. THE GameApiColonyCapacities DTO SHALL contain: PowerDraw (double), PowerGenerated (int), LuxuriesNeeded (int), LuxuriesAvailable (int), HabitationNeeded (int), HabitationAvailable (int), WarehouseUsed (int), WarehouseCapacity (int), FoodNeeded (int), FoodAvailable (int)

### Requirement 12: Worker Merge Logic — Commodity Demands

**User Story:** As a player, I want my colony's commodity requests to be updated from the game API, so that I always see the current workforce demands without manual data entry.

#### Acceptance Criteria

1. THE ColonyMergeService SHALL expose a `MergeWorkers(GameApiColonyWorkersResponse apiWorkers, Colony colony)` method returning bool (true if any data changed)
2. WHEN MergeWorkers is called, THE method SHALL replace the colony's Commodities list with the API's workforceCommodityDemands, mapping: TypeName→Name, Amount→Requested, RequiredBy→NeedBy, Fulfilled→Fulfilled
3. WHEN MergeWorkers is called, THE method SHALL set Delivered to Amount when Fulfilled is true, and 0 when Fulfilled is false
4. WHEN the API returns an empty workforceCommodityDemands list, THE method SHALL clear the colony's Commodities list (empty means no demands)
5. THE method SHALL return true if the Commodities list content changed (different count, different items, or different field values), false if identical

### Requirement 13: Worker Merge Logic — Workforce Overview

**User Story:** As a player, I want my colony's worker allocation data updated from the game API, so that I can see how many workers are assigned vs unassigned.

#### Acceptance Criteria

1. WHEN MergeWorkers is called, THE method SHALL update Colony.WorkerCurrentAttitude from the API response
2. WHEN MergeWorkers is called AND the API provides WorkforceOverview, THE method SHALL update the colony's worker allocation counts (BlueCollarAllocated, BlueCollarUnallocated, WhiteCollarAllocated, WhiteCollarUnallocated, SpecialistAllocated, SpecialistUnallocated)
3. WHEN MergeWorkers is called AND the API provides Wages, THE method SHALL update Colony.WageLevel from CurrentWagePercentage

### Requirement 14: Automatic Scheduler — Workers Integration

**User Story:** As a player, I want the automatic background sync to also fetch worker data for each colony, so that commodity demands stay current without manual intervention.

#### Acceptance Criteria

1. WHEN SyncColoniesAsync fetches per-colony details, THE scheduler SHALL also call GetColonyWorkersAsync for each colony with RemoteAccess > 0
2. WHEN worker data is received successfully, THE scheduler SHALL call ColonyMergeService.MergeWorkers to merge the response into the colony
3. IF GetColonyWorkersAsync returns HTTP 403, THE scheduler SHALL set a workersScopeAvailable flag to false and skip workers for all remaining colonies in the cycle (same pattern as buildings/warehouse scope caching)
4. IF GetColonyWorkersAsync returns HTTP 404 or a network error, THE scheduler SHALL log and continue (worker sync failure does not block buildings/warehouse sync)
