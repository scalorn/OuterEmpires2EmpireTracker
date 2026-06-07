# Requirements Document

## Introduction

Import full crate contents from the game API into the tracker's data model. When the QueueSyncService discovers crate items during cargo/hold discovery, it fetches the crate detail endpoint and populates the local crate Item's Contents bag with all item types (resources, commodities, blueprints, ship parts, ammunition, etc.). Blueprints found inside crates are dual-tracked: they appear in the crate's Contents bag and are also upserted into the master blueprint lists.

This is a new service separate from the existing `CrateImporter` (which handles browser-scraped blueprint JSON). The existing `CrateImporter` remains unchanged.

## Glossary

- **Crate_Content_Importer**: The new service that parses game API crate detail responses and populates the local Item.Contents bag
- **QueueSyncService**: The background sync orchestrator that discovers assets via the game API and cascades detail work items
- **Game_API**: The Outer Empires 2 REST API at `/v1/assets/crates/{id}`
- **Contents_Bag**: The `ItemBag` property on an `Item` of type Crate that holds the crate's child items
- **Cargo_Item**: A single entry in the API crate response, with TypeC code, name, quantity, and type-specific fields
- **Master_Blueprint_List**: The combined global and player blueprint lists managed by EmpireContext and PlayerContext
- **GameItemId**: The integer identifier assigned by the game API to each cargo item, used to match local Items to their API counterparts
- **Freshness**: Not applicable — crate contents are always re-fetched during sync regardless of local state

## Requirements

### Requirement 1: Crate Content Fetch Trigger

**User Story:** As a player, I want crate contents to be automatically fetched during API sync, so that my local data reflects what is inside each crate without manual intervention.

#### Acceptance Criteria

1. WHEN the QueueSyncService encounters a cargo item with TypeC equal to the Crate asset type code, THE QueueSyncService SHALL create a detail work item that fetches the crate contents from the Game_API endpoint `/v1/assets/crates/{id}`.
2. THE QueueSyncService SHALL always fetch crate detail regardless of any freshness state — crate content fetches are never skipped.
3. IF the Game_API returns an error response for a crate detail request, THEN THE QueueSyncService SHALL log a warning and continue processing remaining work items without failing the overall sync.

### Requirement 2: API Response Parsing

**User Story:** As a player, I want the game API crate response to be correctly parsed into local Item objects, so that all item types inside a crate are represented in my local data.

#### Acceptance Criteria

1. WHEN the Game_API returns a successful crate detail response, THE Crate_Content_Importer SHALL parse each Cargo_Item entry into a local Item object.
2. THE Crate_Content_Importer SHALL map the Cargo_Item TypeC code to the corresponding ItemType enum value using the existing AssetMergeService.MapAssetTypeC mapping.
3. THE Crate_Content_Importer SHALL preserve all fields returned by the API on each parsed Item: GameItemId, Name, Quantity, ResourcePurity, Volume, Mass, CurrentHP, MaxHP, MaxRepairPercent, HealthPercentage, LastRepairHealthPercentage, Evolution, ShipPartType, JobRef, JobDeliveryLoc, JobName, JobTrack, and ItemProperties.
4. THE Crate_Content_Importer SHALL log each successful TypeC-to-ItemType mapping at debug level, including the TypeC code and resolved ItemType.
5. IF the MapAssetTypeC mapping returns null for a Cargo_Item TypeC code, THEN THE Crate_Content_Importer SHALL assign ItemType.None and log a warning with the unrecognized code.
6. IF the MapAssetTypeC mapping returns a valid ItemType for a TypeC code, THEN THE Crate_Content_Importer SHALL use the returned ItemType regardless of whether the code is in the recognized constants list.

### Requirement 3: Contents Bag Population

**User Story:** As a player, I want the crate's local Contents bag to be fully replaced with the latest API data on each sync, so that my local view always matches the game state.

#### Acceptance Criteria

1. WHEN the Crate_Content_Importer completes parsing all Cargo_Items, THE Crate_Content_Importer SHALL replace the target crate Item's Contents_Bag with an ItemBag containing all parsed Items.
2. THE Crate_Content_Importer SHALL identify the target crate Item by matching the GameItemId field on existing Item objects in the parent container (ship cargo, station hold, or colony warehouse).
3. IF no local crate Item with the matching GameItemId exists in any container, THEN THE Crate_Content_Importer SHALL create a new crate Item with the matching GameItemId in the appropriate parent container and populate its Contents_Bag with the parsed items.
4. WHEN replacing contents, THE Crate_Content_Importer SHALL clear any previously stored contents before writing the new set — partial merges are not permitted.
5. IF the Contents_Bag replacement operation fails after successful parsing, THEN THE Crate_Content_Importer SHALL log an error and leave the crate's Contents_Bag unchanged, preserving the previous state.

### Requirement 4: Blueprint Dual-Tracking

**User Story:** As a player, I want blueprints found inside crates to appear both in the crate contents and in my master blueprint list, so that I have a complete blueprint inventory and also know where physical copies exist.

#### Acceptance Criteria

1. WHEN a parsed Cargo_Item has a TypeC code indicating a Blueprint, THE Crate_Content_Importer SHALL add the item to the Contents_Bag AND trigger an upsert into the Master_Blueprint_List.
2. THE blueprint upsert SHALL use the existing dedup logic (BlueprintService.FindBestMatch) to determine whether to create or update an existing blueprint entry.
3. WHEN the blueprint upsert creates or updates a blueprint entry in the Master_Blueprint_List, THE Crate_Content_Importer SHALL set the BaseItemTypeID on the corresponding crate content Item to the UUID of the matched or created blueprint.
4. WHEN a blueprint is created or updated via crate import, THE Crate_Content_Importer SHALL fire the BlueprintDataChanged event after all entries are processed.

### Requirement 5: Nested Crate Support

**User Story:** As a player, I want the data model to support nested crates for future game features, so that the tracker remains compatible if the game adds crate nesting.

#### Acceptance Criteria

1. WHEN a Cargo_Item inside a crate has TypeC equal to the Crate asset type code, THE Crate_Content_Importer SHALL create an Item of type Crate in the parent crate's Contents_Bag.
2. THE Crate_Content_Importer SHALL recursively fetch and populate the nested crate's contents by creating an additional detail work item for the nested crate's GameItemId.
3. THE data model SHALL not enforce a nesting depth limit — the importer processes whatever the API returns.
4. THE Crate_Content_Importer SHALL maintain a set of visited GameItemIds during recursive processing and SHALL skip any nested crate whose GameItemId has already been processed, logging a warning about the detected cycle.
5. IF nested crate creation or population fails due to constraints, THEN THE Crate_Content_Importer SHALL log a warning, skip the problematic nested crate, and continue importing the parent crate's remaining items.

### Requirement 6: Persistence

**User Story:** As a player, I want imported crate contents to persist across application restarts, so that I do not lose data between sync sessions.

#### Acceptance Criteria

1. WHEN the Crate_Content_Importer has finished populating a crate's Contents_Bag, THE Crate_Content_Importer SHALL trigger a WriteContext call to persist the updated parent container.
2. THE persistence layer SHALL serialize the Contents_Bag as a nested JSON structure within the parent Item, preserving all Item fields on each child entry.
3. THE persistence layer SHALL deserialize Contents_Bag on load, restoring the full Item hierarchy.
4. IF the WriteContext call fails, THEN THE Crate_Content_Importer SHALL retain the imported contents in memory and retry persistence on the next sync cycle.

### Requirement 7: QueueSyncService Integration

**User Story:** As a player, I want crate content import to integrate cleanly with the existing sync pipeline, so that crate contents are populated as part of the standard discovery flow.

#### Acceptance Criteria

1. WHEN the QueueSyncService processes a crate detail response, THE QueueSyncService SHALL invoke the Crate_Content_Importer instead of the existing CrateImporter for content population.
2. THE QueueSyncService SHALL continue to invoke the existing CrateImporter for blueprint extraction from the same response (preserving backward compatibility with the scraper-based import path).
3. IF the crate detail response contains blueprint entries, THEN THE QueueSyncService SHALL ensure both the content population (Crate_Content_Importer) and blueprint extraction (existing CrateImporter) execute without conflict.
4. IF the crate detail response contains no blueprint entries, THEN THE QueueSyncService SHALL skip blueprint extraction and invoke only the Crate_Content_Importer for content population.
5. IF content population completes successfully but blueprint extraction fails (or vice versa), THEN THE QueueSyncService SHALL retain the successful result and log an error for the failed operation without rolling back the successful one.
6. THE QueueSyncService SHALL pass the crate's GameItemId to the Crate_Content_Importer so it can locate the target crate Item in the local data model.

### Requirement 8: All Item Types Supported

**User Story:** As a player, I want every item type the game supports to be correctly imported into crate contents, so that nothing is lost during sync.

#### Acceptance Criteria

1. THE Crate_Content_Importer SHALL support all item types defined in the AssetTypeCodes constants: Blueprint, Resource, Commodity, ShipPart, ShipHull, Ammunition, Deployable, Share, Workforce, Flatpack, CommodityL, and Survey.
2. WHEN a Resource-typed Cargo_Item is parsed, THE Crate_Content_Importer SHALL preserve the ResourcePurity field from the API response.
3. WHEN a ShipPart-typed Cargo_Item is parsed, THE Crate_Content_Importer SHALL preserve damage fields (CurrentHP, MaxHP, MaxRepairPercent, HealthPercentage) and the ShipPartType field.
4. IF a ShipPart-typed Cargo_Item is missing one or more damage fields from the API response, THEN THE Crate_Content_Importer SHALL accept the item and leave the missing fields at their default values rather than rejecting the item.
5. WHEN a Cargo_Item includes ItemProperties in the API response, THE Crate_Content_Importer SHALL populate the Item.ItemProperties list with all returned key-value pairs.

### Requirement 9: Error Handling and Logging

**User Story:** As a developer, I want comprehensive logging during crate content import, so that issues can be diagnosed without reproducing them.

#### Acceptance Criteria

1. WHEN a crate content import begins, THE Crate_Content_Importer SHALL log an informational message including the crate's GameItemId and the number of items in the response.
2. WHEN a crate content import completes, THE Crate_Content_Importer SHALL log a summary including counts of items imported by type and any errors encountered.
3. IF an individual Cargo_Item fails to parse, THEN THE Crate_Content_Importer SHALL log the error with the item's position and available identifying information, skip the item, and continue processing remaining items.
4. IF the API response JSON is malformed, THEN THE Crate_Content_Importer SHALL log the parse error and return an empty result without throwing an exception to the caller.
5. THE Crate_Content_Importer SHALL only trigger individual item error handling when items are actually present in the response — empty responses SHALL NOT produce item-level error log entries.
6. IF error logging itself fails for an individual Cargo_Item, THEN THE Crate_Content_Importer SHALL continue processing remaining items without interruption.

### Requirement 10: Crate Content Importer — Round-Trip Property

**User Story:** As a developer, I want confidence that parsing and serialization are consistent, so that no data is silently lost between sync cycles.

#### Acceptance Criteria

1. FOR ALL valid crate content API responses, parsing the response into Items and then serializing those Items to JSON and parsing again SHALL produce equivalent Item objects (round-trip property).
2. THE round-trip property SHALL hold for all supported item types including Resources with purity, ShipParts with damage fields, and Items with ItemProperties lists.
