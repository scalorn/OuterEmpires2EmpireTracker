# Requirements Document

## Introduction

This feature integrates market data from the OE2 Public API into the Empire Tracker tool. The tool syncs market data from all configured characters into a unified local data store, giving players a merged view of the public market across all their characters' visible ranges. Players browse this synced data offline with local filtering and sorting.

A key integration point is the Stock Targets system: players can define stock targets that include items listed for sale on the market. When the target quantity exceeds current sell order quantity (minus items already in production), the replenishment cascade fires to manufacture replacements.

The sync model merges public orders from all characters into a shared dataset. Private sales are visible only to the seller and buyer. Stale order removal uses system coordinates and trade range to determine which orders a character should see — only removing orders confirmed to be within a character's range that no longer appear in their results.

## Glossary

- **Game_API**: The OE2 Public API at `oe2-pub-api-dev.azure-api.net` providing character-scoped market data via OAuth2-style JWT authentication.
- **Market_Sync_Service**: The service responsible for calling Game API market endpoints and persisting responses into the local data store.
- **Synced_Order**: A public market order (buy or sell) pulled from the Game API and stored locally in the merged dataset.
- **Price_Stats**: Low/average/high price statistics for an item, fetched from the Game API and stored locally.
- **Own_Order**: A buy or sell order placed by the player's character, synced from the Game API with private details (escrow, competitors).
- **Competitor_Order**: An order by another player that outbids or undercuts the player's own orders.
- **Trade_Network_Range**: The maximum distance in JAS from which market data is visible, calculated as base 150 + 10 per trade skill level.
- **Market_Form**: The existing `FormMarket` WinForms form that displays manual listings, transactions, and profit/loss summaries.
- **Stock_Plan**: A named group of stock targets that defines inventory goals and triggers replenishment.
- **Stock_Target**: A single item/quantity goal within a stock plan, with scope (empire-wide, colony, station, or market).
- **Market_Stock_Target**: A stock target whose scope counts the player's own sell orders as current inventory.
- **Merged_Market_Dataset**: The combined set of public market orders from all synced characters, stored locally.
- **Sync_Metadata**: Per-character record of the system, range, and timestamp of the last sync, used for stale order removal.
- **Item_Catalog**: The Game API's searchable index of item types and IDs used to resolve items for price lookups.
- **Credential_Store**: The DPAPI-encrypted local storage for API keys (app_id, client_id, secret) as designed in BL-047.

## Requirements

### Requirement 1: Market Data Sync

**User Story:** As a player, I want to sync market data from the game API into a unified local store, so that I can browse market information offline and see the combined view across all my characters.

#### Acceptance Criteria

1. WHEN the player triggers a market sync for a character, THE Market_Sync_Service SHALL call the Game API `/v1/market/listings` endpoint with the character's maximum trade range and persist the results into the Merged_Market_Dataset.
2. THE Market_Sync_Service SHALL merge public orders from all synced characters into a single Merged_Market_Dataset, deduplicating by market ID.
3. WHEN syncing, THE Market_Sync_Service SHALL record Sync_Metadata for the character including the system the character was in, the range used, and the sync timestamp.
4. WHEN a previously-stored order falls within the syncing character's system and range but is absent from the fresh API results, THE Market_Sync_Service SHALL remove that order from the Merged_Market_Dataset.
5. THE Market_Sync_Service SHALL not remove orders that are outside the syncing character's range, as no information about those orders can be inferred.
6. IF the sync fails partway through, THEN THE Market_Sync_Service SHALL retain the previous data unchanged and report the failure to the player.

### Requirement 2: Private Sale Handling

**User Story:** As a player, I want private sales to be visible to both the seller and the intended buyer, so that I can track private deals across my characters.

#### Acceptance Criteria

1. WHEN a synced listing has `privateSale=true`, THE Market_Sync_Service SHALL store the order associated with the character who synced it rather than in the shared public dataset.
2. WHEN Character A creates a private sale to Character B, and both characters sync, THE Market_Form SHALL display that order to both Character A and Character B.
3. THE Market_Form SHALL not display private sale orders to characters other than the seller and the named buyer.

### Requirement 3: Own Orders Display

**User Story:** As a player, I want to view my synced buy and sell orders with private details, so that I can manage my market position.

#### Acceptance Criteria

1. WHEN the player triggers a sync, THE Market_Sync_Service SHALL call the Game API `/v1/market/orders/buy` and `/v1/market/orders/sell` endpoints and persist per-character own order data.
2. THE Market_Form SHALL display the character's synced buy orders showing item name, price, quantity remaining vs original, escrow remaining, location, system name, expiry time, and outbid status.
3. THE Market_Form SHALL display the character's synced sell orders showing item name, price, quantity remaining vs original vs sold, location, system name, expiry time, health percentage, sales tax estimate, and undercut status.
4. WHEN an order has the `isOutbid` flag set (buy) or `isUndercut` flag set (sell), THE Market_Form SHALL visually highlight that order.
5. THE Market_Form SHALL show the last-synced timestamp so the player knows how fresh the data is.

### Requirement 4: Competitor Analysis

**User Story:** As a player, I want to see who is outbidding or undercutting my orders, so that I can decide whether to adjust my prices.

#### Acceptance Criteria

1. WHEN the player triggers a sync, THE Market_Sync_Service SHALL call the Game API `/v1/market/orders/buy/competitors` for the character's synced buy orders and persist the competitor data.
2. WHEN the player triggers a sync, THE Market_Sync_Service SHALL call the Game API `/v1/market/orders/sell/competitors` for the character's synced sell orders and persist the competitor data.
3. THE Market_Form SHALL display competitor orders for a selected own order, showing their price, quantity remaining, location, and pilot name.
4. THE Market_Form SHALL display competitors ordered from most threatening (closest in price) to least threatening.

### Requirement 5: Price Statistics

**User Story:** As a player, I want to look up and store price statistics for items, so that I can reference market pricing offline.

#### Acceptance Criteria

1. WHEN the player requests price statistics for an item, THE Market_Sync_Service SHALL call the Game API `/v1/market/prices` endpoint and persist the result locally.
2. THE Market_Form SHALL display stored price statistics showing low price, average price, high price, sample count, search radius, and look-back window.
3. WHERE the player specifies a look-back window (1–90 days), THE Market_Sync_Service SHALL pass the `daysBack` parameter to the API.
4. WHERE the player requests buy-order pricing, THE Market_Sync_Service SHALL pass `buyOrders=true` to the API.
5. WHEN the player searches for an item by name, THE Market_Sync_Service SHALL call the `/v1/market/items` endpoint to resolve the type code and type ID, then fetch prices.

### Requirement 6: Market Listings Browsing

**User Story:** As a player, I want to browse the merged market dataset with local filters, so that I can find items for purchase or gauge demand.

#### Acceptance Criteria

1. THE Market_Form SHALL display the Merged_Market_Dataset in a dedicated tab showing item name, price, quantity remaining, location, distance, seller name, faction tag, and order type (buy/sell).
2. THE Market_Form SHALL provide local filtering on the merged data by order type (Buy/Sell/All), item type, and free-text search on item name or location.
3. THE Market_Form SHALL provide local sorting on the merged data by price, quantity, distance, item name, and location.
4. IF no synced data exists, THEN THE Market_Form SHALL display a message prompting the player to configure credentials and run a sync.

### Requirement 7: Stock Target Market Scope

**User Story:** As a player, I want to set stock targets for items I keep listed on the market, so that the replenishment system builds replacements when my listings run low.

#### Acceptance Criteria

1. THE StockTarget model SHALL support a new scope value `Market` that counts the player's own synced sell orders for the target item as current inventory.
2. WHEN checking a Market-scoped stock target, THE StockTargetService SHALL count the total `amountRemaining` across the player's synced sell orders matching the target item.
3. WHEN calculating the shortfall for a Market-scoped target, THE StockTargetService SHALL subtract items already in production (incomplete build items in the linked build plan for the same item) from the deficit.
4. WHEN the adjusted shortfall is greater than zero and the target quantity is greater than zero, THE StockTargetService SHALL generate replenishment build items in the linked build plan to cover only the remaining deficit.
5. THE Stock Target form SHALL allow the player to create a target with Market scope, specifying the item and desired listing quantity.

### Requirement 8: Market Stock Target with Station Scope

**User Story:** As a player, I want to target market listings at a specific station, so that I maintain stock at particular trading locations.

#### Acceptance Criteria

1. WHERE a Market-scoped stock target specifies a location UUID, THE StockTargetService SHALL count only sell orders at that station when determining current quantity.
2. WHERE a Market-scoped stock target has no location UUID, THE StockTargetService SHALL count all of the player's sell orders for that item across all stations.
3. THE Stock Target form SHALL allow the player to optionally specify a station for a Market-scoped target.

### Requirement 9: Authentication and Credential Management

**User Story:** As a player, I want to configure API credentials per character and have the tool manage token lifecycle, so that syncs run without manual re-authentication.

#### Acceptance Criteria

1. THE Credential_Store SHALL store the app_id, client_id, and per-character secret encrypted with Windows DPAPI in `%LOCALAPPDATA%\OE2EmpireTracker\secrets.dat`.
2. WHEN the player configures credentials for a character, THE Market_Sync_Service SHALL validate them by attempting a token exchange against `/v1/auth/token`.
3. WHEN a token is needed and no valid token exists, THE Market_Sync_Service SHALL exchange the stored credentials for a new JWT.
4. WHEN a token is within 5 minutes of expiry, THE Market_Sync_Service SHALL proactively refresh the token before the next API call.
5. IF token exchange fails due to invalid credentials, THEN THE Market_Sync_Service SHALL notify the player and disable sync for that character until credentials are corrected.
6. THE Market_Sync_Service SHALL include `Authorization: Bearer <token>` and `X-App-Id: <app_id>` headers on every authenticated API call.

### Requirement 10: Rate Limiting and Resilience

**User Story:** As a player, I want the tool to handle API rate limits gracefully during sync, so that syncs complete reliably.

#### Acceptance Criteria

1. WHEN the Game API returns HTTP 429 (Too Many Requests), THE Market_Sync_Service SHALL retry the request with exponential backoff using Polly.
2. THE Market_Sync_Service SHALL apply a circuit breaker policy that opens after 5 consecutive failures and remains open for 30 seconds before attempting a half-open probe.
3. WHILE the circuit breaker is open, THE Market_Sync_Service SHALL abort the current sync and inform the player that the API is temporarily unavailable.
4. THE Market_Sync_Service SHALL respect rate limit headers in API responses and throttle subsequent requests within the same sync.
5. WHERE the API does not provide rate limit headers, THE Market_Sync_Service SHALL apply a default throttle interval between requests as a safety measure.

### Requirement 11: Scope-Aware Feature Gating

**User Story:** As a player, I want the tool to gracefully handle missing API scopes, so that partial access still works.

#### Acceptance Criteria

1. WHEN the token lacks a required scope for a sync operation, THE Market_Sync_Service SHALL skip that operation and log which scope is missing.
2. THE Market_Sync_Service SHALL track which scopes were granted in the most recent token exchange.
3. THE Market_Form SHALL display which scopes are granted and which are missing, without disabling access to feature sections that lack their required scope.
4. IF no credentials are configured for the current character, THEN THE Market_Form SHALL show a configuration prompt in the API data area.

### Requirement 12: Offline Graceful Degradation

**User Story:** As a player, I want the existing manual market tracking to work regardless of API availability.

#### Acceptance Criteria

1. THE Market_Form SHALL always display and operate the existing manual Listings, Transactions, and Summary tabs regardless of API availability or credential configuration.
2. THE Market_Form SHALL clearly separate API-synced data from user-entered data in the UI layout.
3. WHILE synced data is stale (older than a configurable threshold), THE Market_Form SHALL display a visual indicator that the data may be outdated.

### Requirement 13: Pricing Plan Auto-Population

**User Story:** As a player, I want to auto-populate my pricing plans with synced market prices, so that I can keep resource costs current.

#### Acceptance Criteria

1. WHEN the player has synced price statistics and a pricing plan selected, THE Market_Form SHALL offer an option to auto-populate resource prices from the stored market data.
2. WHEN auto-population is triggered, THE Market_Sync_Service SHALL use the stored average price for each resource to update the pricing plan.
3. THE Market_Form SHALL show which resources were updated and which had no price data available.
4. THE Market_Form SHALL require explicit player confirmation before overwriting existing pricing plan values.

### Requirement 14: Stale Order Removal Logic

**User Story:** As a player, I want stale orders to be automatically removed from the merged dataset only when a character confirms they should no longer exist, so that the dataset stays accurate without losing information.

#### Acceptance Criteria

1. WHEN a character syncs, THE Market_Sync_Service SHALL compute which orders in the Merged_Market_Dataset are within that character's system and trade range using system coordinates and distance calculations.
2. WHEN a previously-stored order is within the syncing character's computed range but absent from the fresh API results, THE Market_Sync_Service SHALL remove it from the Merged_Market_Dataset.
3. THE Market_Sync_Service SHALL not remove orders that are outside the syncing character's computed range.
4. THE Market_Sync_Service SHALL use the existing system coordinate data and distance calculation logic from the delivery routes feature for range determination.

