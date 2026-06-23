# Requirements Document

## Introduction

The OE2 Empire Tracker currently retrieves colony warehouse data via the colony-specific `/v1/colonies/{id}/warehouse` endpoint. The game API also exposes a separate assets system via `/v1/assets/locations` and `/v1/assets/locations/{id}?locationType={type}` that provides a unified view of all cargo across all location types: colonies (Co), stations (St), and ships (Sh).

This feature integrates the assets API endpoints into the sync scheduler to pull inventory data for all locations and merge it into the existing data model. For colonies, this provides an alternative (and potentially more complete) source of warehouse data. For stations and ships, this is the primary mechanism for populating their cargo/inventory.

The GameApiClient already has `GetAssetLocationsAsync` and `GetAssetLocationDetailAsync` methods implemented (from the game-api-discovery-tool spec). This feature adds the sync logic, response DTOs, type mapping, and merge service to consume those endpoints.

**Known Limitation:** Crate contents (typeC=Cr) are not accessible via the API. The API returns crates as items with an amount indicating how many items they contain, but the contents cannot be enumerated. This is a documented gap awaiting a dev fix.

## API Reference

- **Asset Locations:** `GET /v1/assets/locations` — returns all locations with asset counts
- **Asset Location Detail:** `GET /v1/assets/locations/{locationId}?locationType={type}` — returns cargo items for a specific location

All responses are wrapped in a `ServiceResponse<T>` envelope: `{ success, returnCode, returnString, data }`.

## Glossary

- **Assets_API**: The game API endpoints at `/v1/assets/locations` and `/v1/assets/locations/{id}?locationType={type}` that return cargo inventory across all location types.
- **Asset_Location**: A location (colony, station, or ship) that contains cargo items, identified by locationId and locationType.
- **Asset_Sync**: The process of fetching asset data from the Assets API and merging it into the local data model (Colony.Items, Station.Holds, Ship.Cargo).
- **Cargo_Item**: A single item returned by the asset detail endpoint, containing cargoItemId, typeId, amount, resourceName, typeC, mass, volume, properties, and other fields.
- **TypeC_Code**: A short string code in the API response that identifies the category of a cargo item (R=raw resource, C=refined resource, F=flatpack, Bp=blueprint, S=ship part, Sc=survey, W=worker, A=ammo, Sh=share, Cr=crate).
- **Sync_Scheduler**: The existing GameApiSyncScheduler that manages periodic polling and coordinates data retrieval from the Game API.
- **Merge_Service**: The service class responsible for mapping API response data to local model objects and applying changes.
- **Game_API_Client**: The existing GameApiClient HTTP client with GetAssetLocationsAsync and GetAssetLocationDetailAsync methods.
- **Location_Type**: The locationType field in the API response: "St" (station), "Co" (colony), "Sh" (ship).
- **ItemBag**: The local inventory container model that stores items keyed by UUID.
- **Crate_Gap**: The documented limitation where crate contents (typeC=Cr) cannot be enumerated via the API.


## Requirements

### Requirement 1: Asset Locations List Retrieval

**User Story:** As a player, I want the tracker to fetch the list of all locations where I have assets, so that the sync knows which stations, colonies, and ships to pull inventory for.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler performs an asset sync cycle, THE Sync_Scheduler SHALL call GetAssetLocationsAsync on the Game_API_Client to retrieve the full location list.
2. WHEN the Assets API returns HTTP 200 with a valid JSON response, THE Sync_Scheduler SHALL deserialize the response into a list of Asset_Location objects containing locationId, locationType, locationName, systemName, systemId, and assetCount.
3. IF the Assets API returns HTTP 401, THEN THE Sync_Scheduler SHALL invalidate the current credentials and abort the asset sync cycle.
4. IF the Assets API returns HTTP 403, THEN THE Sync_Scheduler SHALL log that the assets.locations.read scope is not granted and skip the asset sync cycle without error.
5. IF the JSON response is malformed, THEN THE Sync_Scheduler SHALL log the error and abort the asset sync cycle without modifying local data.
6. THE Sync_Scheduler SHALL skip locations with assetCount of zero when fetching detail data, to avoid unnecessary API calls.


### Requirement 2: Asset Location Detail Retrieval

**User Story:** As a player, I want the tracker to fetch the cargo details for each location, so that my inventory data is populated across all colonies, stations, and ships.

#### Acceptance Criteria

1. FOR EACH Asset_Location with assetCount greater than zero, THE Sync_Scheduler SHALL call GetAssetLocationDetailAsync with the locationId and locationType.
2. WHEN the Assets API returns HTTP 200 with a valid JSON response, THE Sync_Scheduler SHALL deserialize the response into a list of Cargo_Item objects.
3. IF the Assets API returns HTTP 401, THEN THE Sync_Scheduler SHALL invalidate credentials and abort the remaining asset sync cycle.
4. IF the Assets API returns HTTP 403, THEN THE Sync_Scheduler SHALL log the error and skip the current location.
5. IF the Assets API returns HTTP 404, THEN THE Sync_Scheduler SHALL log the error and skip the current location.
6. IF the JSON response for a location is malformed, THEN THE Sync_Scheduler SHALL log the error and continue to the next location without modifying local data for that location.
7. THE Sync_Scheduler SHALL respect the existing rate limiting (30 requests per minute) when iterating through locations.


### Requirement 3: TypeC Code to ItemType Mapping

**User Story:** As a player, I want the API cargo items to be correctly categorized in my local inventory, so that resources, blueprints, flatpacks, and other item types display correctly.

#### Acceptance Criteria

1. THE Merge_Service SHALL map typeC code "R" (with optional trailing space) to ItemType.Resource.
2. THE Merge_Service SHALL map typeC code "C" (with optional trailing space) to ItemType.Commodity.
3. THE Merge_Service SHALL map typeC code "F" (with optional trailing space) to ItemType.Flatpack.
4. THE Merge_Service SHALL map typeC code "Bp" to ItemType.Blueprint.
5. THE Merge_Service SHALL map typeC code "S" (with optional trailing space) to ItemType.ShipPart.
6. THE Merge_Service SHALL map typeC code "Sc" to ItemType.Survey.
7. THE Merge_Service SHALL map typeC code "W" (with optional trailing space) to ItemType.WorkDetail.
8. THE Merge_Service SHALL map typeC code "A" (with optional trailing space) to ItemType.Munition.
9. THE Merge_Service SHALL map typeC code "Sh" to ItemType.Share.
10. THE Merge_Service SHALL map typeC code "Cr" to ItemType.Crate.
11. THE Merge_Service SHALL trim whitespace from the typeC code before performing the mapping.
12. IF the typeC code does not match any known mapping, THEN THE Merge_Service SHALL map the item to ItemType.None and log a warning including the unrecognized typeC value and the item resourceName.


### Requirement 4: Cargo Item to Item Model Mapping

**User Story:** As a player, I want the API cargo data to be fully mapped into the local Item model, so that all available information (mass, volume, properties, health, evolution) is preserved.

#### Acceptance Criteria

1. THE Merge_Service SHALL map the Cargo_Item resourceName field to Item.Name.
2. THE Merge_Service SHALL map the Cargo_Item amount field to Item.Quantity.
3. THE Merge_Service SHALL map the Cargo_Item mass field to Item.Mass.
4. THE Merge_Service SHALL map the Cargo_Item volume field to Item.Volume.
5. THE Merge_Service SHALL map the Cargo_Item cargoItemId field to Item.GameItemId.
6. THE Merge_Service SHALL map the Cargo_Item healthPercentage field to Item.HealthPercentage.
7. THE Merge_Service SHALL map the Cargo_Item lastRepairHealthPercentage to Item.LastRepairHealthPercentage.
8. THE Merge_Service SHALL map the Cargo_Item evolution field to Item.Evolution.
9. THE Merge_Service SHALL map the Cargo_Item shipPartType field to Item.ShipPartType.
10. THE Merge_Service SHALL map the Cargo_Item jobRef field to Item.JobRef.
11. THE Merge_Service SHALL map the Cargo_Item jobDeliveryLoc field to Item.JobDeliveryLoc.
12. THE Merge_Service SHALL map the Cargo_Item jobName field to Item.JobName.
13. THE Merge_Service SHALL map the Cargo_Item jobTrack field to Item.JobTrack.
14. THE Merge_Service SHALL map the Cargo_Item properties array to Item.ItemProperties, preserving modTypeId, propertyName, propertyValue, unit, and evolution for each property.
15. THE Merge_Service SHALL map the Cargo_Item typeId field to Item.BaseItemTypeID (as a string representation of the integer).


### Requirement 5: Colony Asset Merge

**User Story:** As a player, I want the asset data for my colonies to be merged into the existing colony warehouse (ItemBag), so that my colony inventory is kept current from the API.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler receives asset detail data for a location with locationType "Co", THE Merge_Service SHALL match the location to an existing Colony by comparing the locationId to Colony.ColonyId.
2. IF no matching Colony is found for a colony asset location, THEN THE Merge_Service SHALL log a warning and skip that location.
3. WHEN a Cargo_Item matches an existing item in Colony.Items (matched by GameItemId), THE Merge_Service SHALL update the existing item's quantity, mass, volume, and other API-sourced fields.
4. WHEN a Cargo_Item does not match any existing item in Colony.Items, THE Merge_Service SHALL create a new Item with a generated UUID and add it to Colony.Items.
5. THE Merge_Service SHALL preserve existing local-only item data (NickName, Description) when updating items that already exist.
6. THE Merge_Service SHALL return a boolean indicating whether any changes were made to the colony's items.


### Requirement 6: Station Asset Merge

**User Story:** As a player, I want the asset data for stations to be merged into the station inventory, so that I can see what items I have stored at each station.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler receives asset detail data for a location with locationType "St", THE Merge_Service SHALL match the location to an existing Station by comparing the locationId to a station identifier.
2. IF no matching Station is found, THEN THE Merge_Service SHALL create a new Station with the locationName and systemName from the asset location data.
3. THE Merge_Service SHALL merge cargo items into the Station's primary hold (Holds dictionary, keyed by a default hold name).
4. WHEN a Cargo_Item matches an existing item in the Station hold (matched by GameItemId), THE Merge_Service SHALL update the existing item.
5. WHEN a Cargo_Item does not match any existing item, THE Merge_Service SHALL create a new Item and add it to the Station hold.
6. THE Merge_Service SHALL return a boolean indicating whether any changes were made to the station's inventory.


### Requirement 7: Ship Asset Merge

**User Story:** As a player, I want the asset data for ships to be merged into the ship cargo, so that I can see what items are loaded on each ship.

#### Acceptance Criteria

1. WHEN the Sync_Scheduler receives asset detail data for a location with locationType "Sh", THE Merge_Service SHALL match the location to an existing Ship by comparing the locationId to a ship identifier.
2. IF no matching Ship is found, THEN THE Merge_Service SHALL create a new Ship with the name extracted from the locationName and add it to the player's ship list.
3. THE Merge_Service SHALL merge cargo items into Ship.Cargo (the ship's ItemBag).
4. WHEN a Cargo_Item matches an existing item in Ship.Cargo (matched by GameItemId), THE Merge_Service SHALL update the existing item.
5. WHEN a Cargo_Item does not match any existing item, THE Merge_Service SHALL create a new Item and add it to Ship.Cargo.
6. THE Merge_Service SHALL return a boolean indicating whether any changes were made to the ship's cargo.


### Requirement 8: Sync Scheduling and Integration

**User Story:** As a player, I want the asset sync to run automatically as part of the existing sync cycle, so that my inventory data stays current without manual intervention.

#### Acceptance Criteria

1. THE Sync_Scheduler SHALL include asset sync as part of the periodic sync cycle, after colony sync completes.
2. THE Sync_Scheduler SHALL only perform asset sync when the Game_API_Client has a valid access token.
3. WHEN the asset sync completes with changes, THE Sync_Scheduler SHALL persist the updated data via WriteContext.
4. WHEN the asset sync completes with changes, THE Sync_Scheduler SHALL raise appropriate data-changed events so the UI can refresh.
5. THE Sync_Scheduler SHALL log the total number of locations synced and items processed at the end of each asset sync cycle.
6. IF the asset sync encounters a circuit breaker open condition, THEN THE Sync_Scheduler SHALL skip the asset sync cycle and log the condition.


### Requirement 9: Crate Handling

**User Story:** As a player, I want crates to appear in my inventory as container items, so that I know they exist even though their contents are not yet accessible via the API.

#### Acceptance Criteria

1. WHEN a Cargo_Item has typeC "Cr", THE Merge_Service SHALL create an Item with ItemType.Crate.
2. THE Merge_Service SHALL set the crate Item.Quantity to the amount field from the API (representing the number of items inside the crate).
3. THE Merge_Service SHALL set the crate Item.Name to the resourceName from the API (the crate's label).
4. THE Merge_Service SHALL NOT attempt to fetch or enumerate crate contents (documented Crate_Gap).


### Requirement 10: Resource Purity Extraction

**User Story:** As a player, I want raw resources to have their purity level extracted from the resource name, so that they display correctly in the inventory with purity information.

#### Acceptance Criteria

1. WHEN a Cargo_Item has typeC "R" and the resourceName contains a parenthesized purity descriptor (e.g. "Heavy Post-Trans Metals (Unrefined, Med Purity)"), THE Merge_Service SHALL extract the purity portion and set Item.ResourcePurity.
2. THE Merge_Service SHALL recognize purity descriptors: "High Purity", "Med Purity", "Low Purity".
3. WHEN a Cargo_Item has typeC "R" but the resourceName does not contain a recognized purity descriptor, THE Merge_Service SHALL leave Item.ResourcePurity as empty string.
4. THE Merge_Service SHALL extract the base resource name (without the parenthesized qualifier) for use as the Item.Name when a purity descriptor is present.


### Requirement 11: Response DTO Models

**User Story:** As a developer, I want strongly-typed DTO classes for the asset API responses, so that deserialization is type-safe and maintainable.

#### Acceptance Criteria

1. THE system SHALL define a GameApiAssetLocationsResponse DTO containing a list of asset location entries with fields: locationId (int), locationType (string), locationName (string), systemName (string), systemId (int), assetCount (int).
2. THE system SHALL define a GameApiAssetDetailResponse DTO containing a list of cargo items with fields: cargoItemId (int), typeId (int), amount (int), jobRef (int), jobDeliveryLoc (int), healthPercentage (decimal?), lastRepairHealthPercentage (decimal?), resourceName (string), evolution (int), icon (string), typeC (string), mass (decimal), volume (int), properties (list), jobName (string), jobTrack (string), shipPartType (string).
3. THE system SHALL define a GameApiAssetItemProperty DTO containing fields: modTypeId (int), propertyName (string), friendlyPropertyName (string), propertyValue (decimal), unit (string), evolution (int).
4. THE DTO classes SHALL use Newtonsoft.Json attributes for deserialization, consistent with the existing API response models.


### Requirement 12: Location Matching and Identification

**User Story:** As a player, I want the sync to correctly match API locations to my existing local data, so that inventory updates go to the right colony, station, or ship.

#### Acceptance Criteria

1. THE Merge_Service SHALL match colony locations (locationType "Co") by comparing the API locationId to Colony.ColonyId (the integer game ID already stored on colonies from colony API sync).
2. THE Merge_Service SHALL match station locations (locationType "St") by comparing the API locationId to a GameLocationId field on Station.
3. THE Merge_Service SHALL match ship locations (locationType "Sh") by comparing the API locationId to a GameLocationId field on Ship.
4. WHEN a station or ship is created from asset data, THE Merge_Service SHALL store the API locationId on the entity for future matching.
5. THE Merge_Service SHALL store the systemName and systemId from the asset location on newly created stations and ships.


### Requirement 13: Existing Warehouse Sync Coexistence

**User Story:** As a player, I want the asset sync to coexist with the existing colony warehouse sync, so that both data sources contribute to my inventory without conflicts.

#### Acceptance Criteria

1. THE Sync_Scheduler SHALL use the asset API as the primary source for colony warehouse data when both the colony warehouse endpoint and the asset endpoint return data for the same colony.
2. WHEN the asset sync updates a colony's items, THE Merge_Service SHALL use the same item matching strategy (by GameItemId) as the existing MergeWarehouse method.
3. THE Merge_Service SHALL reuse the existing typeC-to-ItemType mapping logic that is already implemented in ColonyMergeService.MergeWarehouse.
4. THE asset sync SHALL NOT remove items from a colony's ItemBag that are not present in the API response (additive merge, consistent with existing MergeWarehouse behavior).

