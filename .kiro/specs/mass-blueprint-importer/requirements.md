# Requirements Document

## Introduction

The Mass Blueprint Importer enables bulk import of blueprints from the Outer Empires 2 in-game market HTML. Players copy market listing HTML from their browser and paste it into the tracker. The importer parses all expanded blueprint listings, resolves each blueprint's type via icon sprite mapping, routes blueprints to the correct storage (global for Government sellers, player-specific otherwise), and creates or updates blueprint records using a composite deduplication key. A results summary is displayed after import.

## Glossary

- **Importer**: The Mass Blueprint Importer service that orchestrates parsing, deduplication, routing, and persistence of market-sourced blueprints.
- **BlueprintScanner**: The existing parser class (`BlueprintScanner`) that contains `ProcessMarketHtml()` for extracting blueprints from market HTML.
- **Market_HTML**: The HTML fragment copied from the in-game market listing page, containing one or more blueprint listings as table rows.
- **Listing_Row**: A `<tr>` element with CSS class `MarketListingRow` representing a single blueprint for sale in the market HTML.
- **Detail_Row**: A `<tr>` element with CSS class `MarketListingRowDetail` immediately following a Listing_Row, containing the expanded properties, resources, and icon for that blueprint.
- **Dedup_Key**: The composite key used to identify a unique blueprint: Name + Evolution + BluePrintType + Class + TechLevel.
- **Global_Storage**: The `EmpireContext.globalBlueprintList` persisted in `BaselineData.json`, used for Government-sold blueprints.
- **Player_Storage**: The `PlayerContext.blueprintList` persisted in `PlayerData.json`, used for player-sold blueprints.
- **Seller_Name**: The text inside a `<span class="ui_text_light_grey">` within the Listing_Row name div, identifying who is selling the blueprint (e.g. "Government").
- **Unexpanded_Listing**: A Listing_Row whose Detail_Row contains no properties, no resources, and no icon — indicating the player did not expand that listing in the game UI.
- **Icon_Position**: The CSS sprite background position (e.g. "-328px -62px") extracted from the `ui_icon_base` div, used to resolve BluePrintType via `BaselineData.json`.
- **Import_Result**: A log entry recording the Dedup_Key, the action taken (created or updated), and the UUID of the affected blueprint.

## Requirements

### Requirement 1: Trigger Market Import from Clipboard

**User Story:** As a player, I want to trigger a market bulk import from the clipboard, so that I can import multiple blueprints at once without manual data entry.

#### Acceptance Criteria

1. THE Importer SHALL provide a UI entry point (menu item or button) to initiate a market bulk import.
2. WHEN the user triggers market import, THE Importer SHALL read the clipboard contents as HTML text.
3. IF the clipboard does not contain HTML text, THEN THE Importer SHALL display a message indicating that no valid market HTML was found on the clipboard.
4. WHEN valid HTML text is found on the clipboard, THE Importer SHALL pass the HTML fragment to `BlueprintScanner.ProcessMarketHtml()` for parsing.

### Requirement 2: Skip Unexpanded Listings

**User Story:** As a player, I want unexpanded market listings to be skipped during import, so that incomplete data does not create broken blueprint records.

#### Acceptance Criteria

1. WHEN a parsed blueprint has zero properties, zero resources, and no resolved BluePrintType, THE Importer SHALL skip that blueprint and not create or update any record.
2. WHEN a blueprint is skipped, THE Importer SHALL log the blueprint name and the reason it was skipped (unexpanded listing).

### Requirement 3: Identify Seller and Route to Correct Storage

**User Story:** As a player, I want Government-sold blueprints stored in global data and player-sold blueprints stored in player data, so that global base blueprints are shared across all player profiles.

#### Acceptance Criteria

1. WHEN the Seller_Name extracted from a Listing_Row equals "Government" (case-insensitive), THE Importer SHALL route that blueprint to Global_Storage.
2. WHEN the Seller_Name extracted from a Listing_Row does not equal "Government", THE Importer SHALL route that blueprint to Player_Storage.
3. WHEN a blueprint is routed to Player_Storage, THE Importer SHALL set the blueprint's `OwnerUUID` to the current player's UUID (`PlayerContext.CurrentPlayerUUID`).
4. IF no player profile is currently selected and a blueprint would be routed to Player_Storage, THEN THE Importer SHALL skip that blueprint and log a warning.

### Requirement 4: Extract Seller Name from Market HTML

**User Story:** As a player, I want the importer to detect who is selling each blueprint, so that blueprints are routed to the correct storage automatically.

#### Acceptance Criteria

1. THE BlueprintScanner SHALL extract the Seller_Name from the `<span class="ui_text_light_grey">` element inside the `MarketListingRowDetailDescription` div of each Listing_Row.
2. WHEN no seller span is found in a Listing_Row, THE BlueprintScanner SHALL treat the Seller_Name as empty.
3. THE BlueprintScanner SHALL include the Seller_Name on each parsed blueprint object so the Importer can use it for routing.

### Requirement 5: Deduplicate Blueprints Using Composite Key

**User Story:** As a player, I want the importer to detect existing blueprints by their composite key, so that duplicate records are not created.

#### Acceptance Criteria

1. THE Importer SHALL identify duplicate blueprints by matching on all five components of the Dedup_Key: Name, Evolution, BluePrintType, Class, and TechLevel.
2. WHEN searching for duplicates, THE Importer SHALL search the target storage list (Global_Storage or Player_Storage) based on the routing decision for that blueprint.
3. WHEN a blueprint with a matching Dedup_Key is found in the target storage, THE Importer SHALL treat it as a duplicate and update the existing record.
4. WHEN no blueprint with a matching Dedup_Key is found in the target storage, THE Importer SHALL create a new blueprint record.

### Requirement 6: Update Existing Blueprints on Duplicate

**User Story:** As a player, I want existing blueprints to be updated with the latest market data when a duplicate is found, so that my blueprint records stay current.

#### Acceptance Criteria

1. WHEN updating an existing blueprint, THE Importer SHALL overwrite the existing blueprint's properties with the market-parsed property values.
2. WHEN updating an existing blueprint, THE Importer SHALL overwrite the existing blueprint's resources with the market-parsed resource values.
3. WHEN updating an existing blueprint, THE Importer SHALL preserve the existing blueprint's Manufacture Run Time property if the market data does not include it.
4. WHEN updating an existing blueprint, THE Importer SHALL preserve the existing blueprint's Power Required property if the market data does not include it.
5. WHEN updating an existing blueprint, THE Importer SHALL preserve the existing blueprint's Description if the market data does not include it.
6. WHEN updating an existing blueprint, THE Importer SHALL preserve the existing blueprint's UUID, OwnerUUID, NickName, TechLevel, and CopyCost.

### Requirement 7: Create New Blueprints

**User Story:** As a player, I want new blueprints to be created when no duplicate exists, so that my blueprint library grows as I import new market data.

#### Acceptance Criteria

1. WHEN creating a new blueprint, THE Importer SHALL assign a new UUID (via `Guid.NewGuid()`).
2. WHEN creating a new blueprint, THE Importer SHALL set the Name, Evolution, Class, BluePrintType, properties, and resources from the parsed market data.
3. WHEN creating a new blueprint, THE Importer SHALL leave Manufacture Run Time empty if the market data does not include it.
4. WHEN creating a new blueprint, THE Importer SHALL leave Power Required empty if the market data does not include it.
5. WHEN creating a new blueprint, THE Importer SHALL leave Description empty if the market data does not include it.
6. WHEN creating a new blueprint routed to Global_Storage, THE Importer SHALL add the blueprint to `EmpireContext.globalBlueprintList`.
7. WHEN creating a new blueprint routed to Player_Storage, THE Importer SHALL add the blueprint to `PlayerContext.blueprintList` with `OwnerUUID` set to the current player's UUID.

### Requirement 8: Log Import Results

**User Story:** As a player, I want to see a log of what was imported, so that I can verify the import worked correctly and troubleshoot issues.

#### Acceptance Criteria

1. FOR EACH processed blueprint, THE Importer SHALL log the Dedup_Key (Name, Evolution, BluePrintType, Class).
2. FOR EACH processed blueprint, THE Importer SHALL log the action taken: "Created" for new blueprints or "Updated" for existing duplicates.
3. FOR EACH processed blueprint, THE Importer SHALL log the UUID of the affected blueprint record.
4. FOR EACH skipped blueprint, THE Importer SHALL log the blueprint name and the skip reason.
5. THE Importer SHALL log a summary line at the end of import containing the total count of created, updated, and skipped blueprints.

### Requirement 9: Persist Changes After Import

**User Story:** As a player, I want imported blueprints to be saved to disk, so that my data is not lost if the application closes.

#### Acceptance Criteria

1. WHEN the import completes with at least one created or updated blueprint in Global_Storage, THE Importer SHALL call `EmpireContext.writeContext()` to persist BaselineData.json.
2. WHEN the import completes with at least one created or updated blueprint in Player_Storage, THE Importer SHALL call `PlayerContext.writeContext()` to persist PlayerData.json.
3. THE Importer SHALL persist changes only once at the end of the import, not after each individual blueprint.

### Requirement 10: Display Import Results Summary

**User Story:** As a player, I want to see a summary of the import results after the operation completes, so that I know what happened without reading log files.

#### Acceptance Criteria

1. WHEN the import completes, THE Importer SHALL display a results summary to the user.
2. THE results summary SHALL include the count of blueprints created, updated, and skipped.
3. THE results summary SHALL list each processed blueprint with its Dedup_Key and the action taken (created, updated, or skipped).
4. THE results summary SHALL distinguish between blueprints routed to Global_Storage and Player_Storage.

### Requirement 11: Notify Open Forms of Data Changes

**User Story:** As a player, I want open blueprint forms to refresh after a bulk import, so that I see the newly imported data without restarting the application.

#### Acceptance Criteria

1. WHEN the import creates or updates blueprints in Player_Storage, THE Importer SHALL fire `PlayerContext.OnBlueprintDataChanged()` to notify open forms.
2. THE Importer SHALL fire the data-changed notification once after all blueprints are processed, not after each individual blueprint.

### Requirement 12: Document HTML Parsing Dependencies

**User Story:** As a developer, I want the CSS classes and sprite position patterns used by the parser documented, so that future game updates can be accommodated without reverse-engineering the parser.

#### Acceptance Criteria

1. THE requirements document SHALL list the CSS class names used to identify market listing elements: `MarketListingRow`, `MarketListingRowDetail`, `MarketListingRowDetailDescription`, `MarketListingRowDetailIcon`, `Market_ShipComponentProperty`, `Market_ShipComponentProperty_Label`, `ui_text_blue_light`, `ui_icon_base`, `ui_text_light_grey`, `EvolutionNumber`, `ScanDetailOutputResourceName_MarketListing`, `ScanDetailOutputResourceDetail`.
2. THE requirements document SHALL note that Icon_Position to BluePrintType mapping is maintained in `BaselineData.json` under `BlueprintType[].IconPosition` and can be updated without code changes.
3. THE requirements document SHALL note that property label remapping is maintained in `BlueprintScanner.PropertyRemap` and can be extended for new or renamed game labels.
