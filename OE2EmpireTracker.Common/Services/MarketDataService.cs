using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using ApiMarketBuyOrder = OE2EmpireTracker.Common.Client.Generated.MarketBuyOrder;
using ApiMarketBuyOrders = OE2EmpireTracker.Common.Client.Generated.MarketBuyOrders;
using ApiMarketCompetitor = OE2EmpireTracker.Common.Client.Generated.MarketCompetitor;
using ApiMarketCompetitorOrders = OE2EmpireTracker.Common.Client.Generated.MarketCompetitorOrders;
using ApiMarketListing = OE2EmpireTracker.Common.Client.Generated.MarketListing;
using ApiMarketListings = OE2EmpireTracker.Common.Client.Generated.MarketListings;
using ApiMarketOrderCompetitors = OE2EmpireTracker.Common.Client.Generated.MarketOrderCompetitors;
using ApiMarketPriceStats = OE2EmpireTracker.Common.Client.Generated.MarketPriceStats;
using ApiMarketSellOrder = OE2EmpireTracker.Common.Client.Generated.MarketSellOrder;
using ApiMarketSellOrders = OE2EmpireTracker.Common.Client.Generated.MarketSellOrders;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Handles domain mapping, merge logic, and deduplication for market data
    /// synced from the Game API. Called by QueueSyncService after receiving typed
    /// DTOs from IGameApiTypedClient.
    /// </summary>
    public class MarketDataService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        private readonly SystemGridIndex _gridIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketDataService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for data persistence.</param>
        /// <param name="gridIndex">The spatial index for range queries.</param>
        public MarketDataService(PlayerContext playerContext, SystemGridIndex gridIndex)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _gridIndex = gridIndex ?? throw new ArgumentNullException(nameof(gridIndex));
        }

        /// <summary>
        /// Occurs when a market alert is triggered by matching orders.
        /// </summary>
        public event EventHandler<MarketAlertTriggeredEventArgs> MarketAlertTriggered;

        /// <summary>
        /// Maps DTO entries to MarketListing domain objects, upserts by MarketId,
        /// and deduplicates. Does NOT handle stale removal (separate task).
        /// </summary>
        /// <param name="dto">The MarketListings DTO from the API.</param>
        /// <param name="characterUUID">The syncing character's UUID.</param>
        /// <param name="systemId">The system the character was in during sync.</param>
        /// <param name="rangeJas">The trade range in JAS used for this sync.</param>
        public void ProcessMarketListings(
            ApiMarketListings dto,
            string characterUUID,
            int systemId,
            int rangeJas)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            ICollection<ApiMarketListing> listings = dto.Listings;
            if (listings == null || listings.Count == 0)
            {
                Log.Debug("ProcessMarketListings: no listings in DTO for character {0}", characterUUID);
                return;
            }

            string syncTimestamp = SystemClock.UtcNow.ToString("o");
            int upserted = 0;
            int created = 0;

            foreach (ApiMarketListing entry in listings)
            {
                MarketListing existing = _playerContext.FindMarketListingByMarketId(entry.MarketId);

                if (existing != null)
                {
                    UpdateExistingListing(existing, entry, characterUUID, syncTimestamp);
                    upserted++;
                }
                else
                {
                    MarketListing newListing = MapToNewListing(entry, characterUUID, syncTimestamp);
                    _playerContext.AddMarketListing(newListing);
                    created++;
                }
            }

            _playerContext.InvalidateMarketListingByMarketIdCache();

            Log.Info(
                "ProcessMarketListings: character={0} system={1} range={2} — created={3} updated={4} total={5}",
                characterUUID,
                systemId,
                rangeJas,
                created,
                upserted,
                listings.Count);
        }

        /// <summary>
        /// Removes stale orders from the merged dataset. An order is stale if:
        /// - It has a non-null MarketId (synced, not manual)
        /// - Its SystemId is in metadata.SystemsInRange (confirmably within range)
        /// - Its MarketId is NOT in the freshMarketIds set (absent from latest results)
        ///
        /// Orders with null SystemId are ambiguous and always preserved.
        /// Orders with null MarketId are manual entries and always preserved.
        /// Orders outside the character's range are always preserved.
        /// </summary>
        /// <param name="freshMarketIds">Set of MarketIds present in the latest API results.</param>
        /// <param name="characterUUID">The syncing character's UUID.</param>
        /// <param name="metadata">Sync metadata containing SystemsInRange for stale detection.</param>
        public void RemoveStaleOrders(HashSet<long> freshMarketIds, string characterUUID, CharacterSyncMetadata metadata)
        {
            if (freshMarketIds == null)
            {
                throw new ArgumentNullException(nameof(freshMarketIds));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            List<MarketListing> allListings = _playerContext.SnapshotMarketListingList();
            var staleListings = new List<MarketListing>();

            foreach (MarketListing listing in allListings)
            {
                // Manual entries (no MarketId) are never removed
                if (!listing.MarketId.HasValue)
                {
                    continue;
                }

                // Ambiguous orders (no SystemId) are always preserved
                if (!listing.SystemId.HasValue)
                {
                    continue;
                }

                // Orders outside the character's range are always preserved
                if (!metadata.SystemsInRange.Contains(listing.SystemId.Value))
                {
                    continue;
                }

                // Order is within range but absent from fresh results — mark as stale
                if (!freshMarketIds.Contains(listing.MarketId.Value))
                {
                    staleListings.Add(listing);
                }
            }

            foreach (MarketListing stale in staleListings)
            {
                _playerContext.RemoveMarketListing(stale);
            }

            if (staleListings.Count > 0)
            {
                _playerContext.InvalidateMarketListingByMarketIdCache();
            }

            Log.Info(
                "RemoveStaleOrders: character={0} freshIds={1} removed={2}",
                characterUUID,
                freshMarketIds.Count,
                staleListings.Count);
        }

        /// <summary>
        /// Maps own buy order DTOs to MarketListing entries with OwnerUUID set to the
        /// character who placed the orders. Upserts by MarketId (same pattern as
        /// ProcessMarketListings). Sets BuyOrder=true and maps escrow, amounts,
        /// outbid flag, timing, and location fields.
        /// </summary>
        /// <param name="dto">The MarketBuyOrders DTO from the API.</param>
        /// <param name="characterUUID">The character UUID who owns these orders.</param>
        public void ProcessOwnBuyOrders(ApiMarketBuyOrders dto, string characterUUID)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            ICollection<ApiMarketBuyOrder> orders = dto.Orders;
            if (orders == null || orders.Count == 0)
            {
                Log.Debug("ProcessOwnBuyOrders: no orders in DTO for character {0}", characterUUID);
                return;
            }

            string syncTimestamp = SystemClock.UtcNow.ToString("o");
            int upserted = 0;
            int created = 0;

            foreach (ApiMarketBuyOrder entry in orders)
            {
                MarketListing existing = _playerContext.FindMarketListingByMarketId(entry.MarketId);

                if (existing != null)
                {
                    UpdateFromBuyOrder(existing, entry, characterUUID, syncTimestamp);
                    upserted++;
                }
                else
                {
                    MarketListing newListing = MapBuyOrderToListing(entry, characterUUID, syncTimestamp);
                    _playerContext.AddMarketListing(newListing);
                    created++;
                }
            }

            _playerContext.InvalidateMarketListingByMarketIdCache();

            Log.Info(
                "ProcessOwnBuyOrders: character={0} — created={1} updated={2} total={3}",
                characterUUID,
                created,
                upserted,
                orders.Count);
        }

        /// <summary>
        /// Maps own sell order DTOs to MarketListing entries with OwnerUUID set to the
        /// character who placed the orders. Upserts by MarketId (same pattern as
        /// ProcessMarketListings). Sets BuyOrder=false and maps amounts, tax estimate,
        /// undercut flag, timing, and location fields.
        /// </summary>
        /// <param name="dto">The MarketSellOrders DTO from the API.</param>
        /// <param name="characterUUID">The character UUID who owns these orders.</param>
        public void ProcessOwnSellOrders(ApiMarketSellOrders dto, string characterUUID)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            ICollection<ApiMarketSellOrder> orders = dto.Orders;
            if (orders == null || orders.Count == 0)
            {
                Log.Debug("ProcessOwnSellOrders: no orders in DTO for character {0}", characterUUID);
                return;
            }

            string syncTimestamp = SystemClock.UtcNow.ToString("o");
            int upserted = 0;
            int created = 0;

            foreach (ApiMarketSellOrder entry in orders)
            {
                MarketListing existing = _playerContext.FindMarketListingByMarketId(entry.MarketId);

                if (existing != null)
                {
                    UpdateFromSellOrder(existing, entry, characterUUID, syncTimestamp);
                    upserted++;
                }
                else
                {
                    MarketListing newListing = MapSellOrderToListing(entry, characterUUID, syncTimestamp);
                    _playerContext.AddMarketListing(newListing);
                    created++;
                }
            }

            _playerContext.InvalidateMarketListingByMarketIdCache();

            Log.Info(
                "ProcessOwnSellOrders: character={0} — created={1} updated={2} total={3}",
                characterUUID,
                created,
                upserted,
                orders.Count);
        }

        /// <summary>
        /// Maps buy order competitor DTOs to MarketListing entries linked to the
        /// player's own buy orders. Each competitor is an order that outbids the
        /// player's buy order. Upserts by MarketId (same pattern as other process
        /// methods). Sets CompetitorForMarketId to link to the own order being outbid.
        /// </summary>
        /// <param name="dto">The MarketCompetitorOrders DTO from the API.</param>
        /// <param name="characterUUID">The character UUID whose orders are being outbid.</param>
        public void ProcessBuyCompetitors(ApiMarketCompetitorOrders dto, string characterUUID)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            ICollection<ApiMarketOrderCompetitors> orders = dto.Orders;
            if (orders == null || orders.Count == 0)
            {
                Log.Debug("ProcessBuyCompetitors: no orders in DTO for character {0}", characterUUID);
                return;
            }

            string syncTimestamp = SystemClock.UtcNow.ToString("o");
            int upserted = 0;
            int created = 0;

            foreach (ApiMarketOrderCompetitors orderGroup in orders)
            {
                long ownOrderMarketId = orderGroup.MarketId;
                ICollection<ApiMarketCompetitor> competitors = orderGroup.Competitors;
                if (competitors == null || competitors.Count == 0)
                {
                    continue;
                }

                foreach (ApiMarketCompetitor competitor in competitors)
                {
                    MarketListing existing = _playerContext.FindMarketListingByMarketId(competitor.MarketId);

                    if (existing != null)
                    {
                        UpdateFromCompetitor(existing, competitor, ownOrderMarketId, true, characterUUID, syncTimestamp);
                        upserted++;
                    }
                    else
                    {
                        MarketListing newListing = MapCompetitorToListing(competitor, ownOrderMarketId, true, characterUUID, syncTimestamp);
                        _playerContext.AddMarketListing(newListing);
                        created++;
                    }
                }
            }

            _playerContext.InvalidateMarketListingByMarketIdCache();

            Log.Info(
                "ProcessBuyCompetitors: character={0} — created={1} updated={2}",
                characterUUID,
                created,
                upserted);
        }

        /// <summary>
        /// Maps sell order competitor DTOs to MarketListing entries linked to the
        /// player's own sell orders. Each competitor is an order that undercuts the
        /// player's sell order. Upserts by MarketId (same pattern as other process
        /// methods). Sets CompetitorForMarketId to link to the own order being undercut.
        /// </summary>
        /// <param name="dto">The MarketCompetitorOrders DTO from the API.</param>
        /// <param name="characterUUID">The character UUID whose orders are being undercut.</param>
        public void ProcessSellCompetitors(ApiMarketCompetitorOrders dto, string characterUUID)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            ICollection<ApiMarketOrderCompetitors> orders = dto.Orders;
            if (orders == null || orders.Count == 0)
            {
                Log.Debug("ProcessSellCompetitors: no orders in DTO for character {0}", characterUUID);
                return;
            }

            string syncTimestamp = SystemClock.UtcNow.ToString("o");
            int upserted = 0;
            int created = 0;

            foreach (ApiMarketOrderCompetitors orderGroup in orders)
            {
                long ownOrderMarketId = orderGroup.MarketId;
                ICollection<ApiMarketCompetitor> competitors = orderGroup.Competitors;
                if (competitors == null || competitors.Count == 0)
                {
                    continue;
                }

                foreach (ApiMarketCompetitor competitor in competitors)
                {
                    MarketListing existing = _playerContext.FindMarketListingByMarketId(competitor.MarketId);

                    if (existing != null)
                    {
                        UpdateFromCompetitor(existing, competitor, ownOrderMarketId, false, characterUUID, syncTimestamp);
                        upserted++;
                    }
                    else
                    {
                        MarketListing newListing = MapCompetitorToListing(competitor, ownOrderMarketId, false, characterUUID, syncTimestamp);
                        _playerContext.AddMarketListing(newListing);
                        created++;
                    }
                }
            }

            _playerContext.InvalidateMarketListingByMarketIdCache();

            Log.Info(
                "ProcessSellCompetitors: character={0} — created={1} updated={2}",
                characterUUID,
                created,
                upserted);
        }

        /// <summary>
        /// Maps a price statistics DTO to a <see cref="StoredPriceStats"/> object and
        /// upserts it into <see cref="MarketSyncData.PriceStats"/> keyed by composite
        /// item identity (ItemType + BaseItemTypeID + ResourcePurity + GameTypeCode + OrderType).
        /// </summary>
        /// <param name="dto">The MarketPriceStats DTO from the API.</param>
        /// <param name="characterUUID">The character who fetched the price data.</param>
        /// <param name="itemName">The resolved item name for display.</param>
        public void ProcessPriceStats(ApiMarketPriceStats dto, string characterUUID, string itemName)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            string gameTypeCode = dto.OrderType ?? string.Empty;
            string orderType = dto.OrderType ?? string.Empty;

            var stats = new StoredPriceStats
            {
                ItemType = MapGameTypeCodeToItemType(gameTypeCode),
                BaseItemTypeID = itemName ?? string.Empty,
                ItemName = itemName ?? string.Empty,
                ResourcePurity = string.Empty,
                GameTypeCode = gameTypeCode,
                GameTypeId = 0,
                LowPrice = dto.LowPrice.HasValue ? (decimal)dto.LowPrice.Value : (decimal?)null,
                AvgPrice = dto.AvgPrice.HasValue ? (decimal)dto.AvgPrice.Value : (decimal?)null,
                HighPrice = dto.HighPrice.HasValue ? (decimal)dto.HighPrice.Value : (decimal?)null,
                SampleCount = dto.SampleCount,
                SearchRadius = dto.SearchRadius ?? string.Empty,
                DaysSearched = dto.DaysSearched,
                OrderType = orderType,
                FetchedTimestamp = SystemClock.UtcNow.ToString("o"),
                FetchedByCharacterUUID = characterUUID,
            };

            UpsertPriceStats(stats);

            Log.Info(
                "ProcessPriceStats: character={0} item=\"{1}\" orderType={2} samples={3}",
                characterUUID,
                itemName,
                orderType,
                dto.SampleCount);
        }

        /// <summary>
        /// Auto-populates a pricing plan's resource prices from stored price stats.
        /// For each resource key in the pricing plan, finds matching StoredPriceStats
        /// and uses AvgPrice. Returns a result showing which resources were updated
        /// and which had no matching price data.
        /// </summary>
        /// <param name="pricingPlanUUID">The UUID of the pricing plan to populate.</param>
        /// <param name="characterUUID">The character UUID requesting the auto-populate.</param>
        /// <returns>A result indicating which resources were updated or missing data.</returns>
        public MarketPricePopulationResult AutoPopulatePricingPlan(string pricingPlanUUID, string characterUUID)
        {
            if (string.IsNullOrEmpty(pricingPlanUUID))
            {
                throw new ArgumentNullException(nameof(pricingPlanUUID));
            }

            var result = new MarketPricePopulationResult();
            PricingPlan plan = _playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == pricingPlanUUID);
            if (plan == null)
            {
                Log.Warn("AutoPopulatePricingPlan: plan not found UUID={0}", pricingPlanUUID);
                return result;
            }

            List<StoredPriceStats> allStats = _playerContext.MarketSyncData.PriceStats;
            var resourceKeys = new List<string>(plan.ResourcePrices.Keys);

            foreach (string resourceKey in resourceKeys)
            {
                // Keys are "{ResourceName}|{Purity}" e.g. "Alkali Metals|Refined"
                string[] parts = resourceKey.Split('|');
                string resourceName = parts.Length > 0 ? parts[0] : string.Empty;
                string purity = parts.Length > 1 ? parts[1] : string.Empty;

                StoredPriceStats match = FindMatchingPriceStats(allStats, resourceName, purity);
                if (match != null && match.AvgPrice.HasValue)
                {
                    plan.ResourcePrices[resourceKey] = match.AvgPrice.Value;
                    result.UpdatedResources.Add(resourceKey);
                }
                else
                {
                    result.MissingResources.Add(resourceKey);
                }
            }

            Log.Info(
                "AutoPopulatePricingPlan: plan=\"{0}\" updated={1} missing={2}",
                plan.Name,
                result.UpdatedResources.Count,
                result.MissingResources.Count);

            return result;
        }

        /// <summary>
        /// Validates that the given type code and type ID are valid for a price lookup.
        /// Returns true if valid (caller may proceed with the price fetch), false otherwise.
        /// </summary>
        /// <param name="typeCode">The Game API type code (e.g. "R", "C", "BP").</param>
        /// <param name="typeId">The Game API type ID for the item.</param>
        /// <returns>True if the parameters are valid for a price lookup; otherwise false.</returns>
        public bool ResolveItemForPriceLookup(string typeCode, long typeId)
        {
            if (string.IsNullOrEmpty(typeCode))
            {
                return false;
            }

            if (typeId <= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns all synced market listings visible to the given character.
        /// Includes all public orders plus private orders where the character is
        /// the seller or the named buyer. Excludes competitor entries.
        /// </summary>
        /// <param name="characterUUID">The character UUID to filter visibility for.</param>
        /// <returns>A read-only list of visible market listings.</returns>
        public IReadOnlyList<MarketListing> GetVisibleOrders(string characterUUID)
        {
            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            string characterName = ResolveCharacterName(characterUUID);
            List<MarketListing> allListings = _playerContext.SnapshotMarketListingList();
            var visible = new List<MarketListing>();

            foreach (MarketListing listing in allListings)
            {
                // Only include synced orders (non-null MarketId)
                if (!listing.MarketId.HasValue)
                {
                    continue;
                }

                // Exclude competitor entries — they are shown separately
                if (listing.CompetitorForMarketId.HasValue)
                {
                    continue;
                }

                // Public orders are visible to everyone
                if (!listing.PrivateSale)
                {
                    visible.Add(listing);
                    continue;
                }

                // Private sale: visible to the seller (OwnerUUID or SyncedByCharacterUUID match)
                if (listing.OwnerUUID == characterUUID || listing.SyncedByCharacterUUID == characterUUID)
                {
                    visible.Add(listing);
                    continue;
                }

                // Private sale: visible to the named buyer (by character name)
                if (!string.IsNullOrEmpty(characterName)
                    && !string.IsNullOrEmpty(listing.BuyerName)
                    && string.Equals(listing.BuyerName, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    visible.Add(listing);
                }
            }

            return visible;
        }

        /// <summary>
        /// Returns the character's own orders (listings where OwnerUUID matches).
        /// Only includes synced orders (non-null MarketId). Excludes competitor entries.
        /// </summary>
        /// <param name="characterUUID">The character UUID whose orders to return.</param>
        /// <returns>A read-only list of the character's own market listings.</returns>
        public IReadOnlyList<MarketListing> GetOwnOrders(string characterUUID)
        {
            if (string.IsNullOrEmpty(characterUUID))
            {
                throw new ArgumentNullException(nameof(characterUUID));
            }

            List<MarketListing> allListings = _playerContext.SnapshotMarketListingList();
            var ownOrders = new List<MarketListing>();

            foreach (MarketListing listing in allListings)
            {
                // Only include synced orders (non-null MarketId)
                if (!listing.MarketId.HasValue)
                {
                    continue;
                }

                // Exclude competitor entries
                if (listing.CompetitorForMarketId.HasValue)
                {
                    continue;
                }

                // Must be owned by the character
                if (listing.OwnerUUID == characterUUID)
                {
                    ownOrders.Add(listing);
                }
            }

            return ownOrders;
        }

        /// <summary>
        /// Evaluates all enabled market alerts against freshly-synced orders.
        /// For each enabled alert, filters fresh orders by alert type, item composite key,
        /// station, and price condition. Excludes orders already seen (matching
        /// LastTriggeredMarketId). Fires <see cref="MarketAlertTriggered"/> for each
        /// alert with matching orders and updates tracking fields.
        /// </summary>
        /// <param name="freshOrders">The newly-synced or updated orders to evaluate.</param>
        public void EvaluateAlerts(IReadOnlyList<MarketListing> freshOrders)
        {
            if (freshOrders == null)
            {
                throw new ArgumentNullException(nameof(freshOrders));
            }

            if (freshOrders.Count == 0)
            {
                return;
            }

            List<MarketAlert> alerts = _playerContext.MarketSyncData.Alerts;
            if (alerts == null || alerts.Count == 0)
            {
                return;
            }

            foreach (MarketAlert alert in alerts)
            {
                if (!alert.Enabled)
                {
                    continue;
                }

                List<MarketListing> matches = FindMatchingOrders(alert, freshOrders);
                if (matches.Count == 0)
                {
                    continue;
                }

                // Update tracking fields
                MarketListing lastMatch = matches[matches.Count - 1];
                alert.LastTriggeredTimestamp = SystemClock.UtcNow.ToString("o");
                if (lastMatch.MarketId.HasValue)
                {
                    alert.LastTriggeredMarketId = lastMatch.MarketId.Value;
                }

                // Fire the event
                OnMarketAlertTriggered(new MarketAlertTriggeredEventArgs(alert, matches));

                Log.Info(
                    "EvaluateAlerts: alert \"{0}\" triggered with {1} matching order(s)",
                    alert.Name,
                    matches.Count);
            }
        }

        /// <summary>
        /// Raises the <see cref="MarketAlertTriggered"/> event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnMarketAlertTriggered(MarketAlertTriggeredEventArgs e)
        {
            MarketAlertTriggered?.Invoke(this, e);
        }

        /// <summary>
        /// Finds orders from the fresh list that match the given alert's criteria.
        /// Filters by alert type (buy/sell), item composite key, optional station,
        /// price condition, and excludes already-triggered market IDs.
        /// </summary>
        private static List<MarketListing> FindMatchingOrders(
            MarketAlert alert,
            IReadOnlyList<MarketListing> freshOrders)
        {
            var matches = new List<MarketListing>();

            // Determine expected BuyOrder value based on AlertType
            bool expectedBuyOrder = alert.AlertType == MarketAlertType.BuyOrderAppears;

            foreach (MarketListing order in freshOrders)
            {
                // Filter by AlertType: SellOrderAppears → BuyOrder=false, BuyOrderAppears → BuyOrder=true
                if (order.BuyOrder != expectedBuyOrder)
                {
                    continue;
                }

                // Filter by item composite key (ItemType + BaseItemTypeID + ResourcePurity)
                if (order.ItemType != alert.ItemType)
                {
                    continue;
                }

                if (!string.Equals(order.BaseItemTypeID, alert.BaseItemTypeID, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(order.ResourcePurity, alert.ResourcePurity, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Filter by StationUUID (optional — only if alert specifies one)
                if (!string.IsNullOrEmpty(alert.StationUUID))
                {
                    if (!string.Equals(order.StationUUID, alert.StationUUID, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // Evaluate price condition
                if (!EvaluatePriceCondition(alert, order))
                {
                    continue;
                }

                // Exclude orders already seen (MarketId == LastTriggeredMarketId)
                if (alert.LastTriggeredMarketId.HasValue
                    && order.MarketId.HasValue
                    && order.MarketId.Value == alert.LastTriggeredMarketId.Value)
                {
                    continue;
                }

                matches.Add(order);
            }

            return matches;
        }

        /// <summary>
        /// Evaluates whether an order meets the alert's price condition.
        /// </summary>
        private static bool EvaluatePriceCondition(MarketAlert alert, MarketListing order)
        {
            switch (alert.PriceCondition)
            {
                case PriceCondition.AnyPrice:
                    return true;

                case PriceCondition.AtOrBelow:
                    if (!alert.PriceThreshold.HasValue)
                    {
                        return true;
                    }

                    return order.PricePerUnit <= alert.PriceThreshold.Value;

                case PriceCondition.AtOrAbove:
                    if (!alert.PriceThreshold.HasValue)
                    {
                        return true;
                    }

                    return order.PricePerUnit >= alert.PriceThreshold.Value;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Maps an API DTO entry to a new MarketListing domain object.
        /// </summary>
        private static MarketListing MapToNewListing(
            ApiMarketListing entry,
            string characterUUID,
            string syncTimestamp)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = entry.MarketId,
                BuyOrder = entry.BuyOrder,
                GameTypeCode = entry.Type ?? string.Empty,
                GameTypeId = entry.TypeId,
                GameSubTypeId = entry.SubTypeId?.ToString() ?? string.Empty,
                ItemType = MapGameTypeCodeToItemType(entry.Type),
                BaseItemTypeID = ResolveBaseItemTypeId(entry),
                ResourcePurity = ResolveResourcePurity(entry),
                ItemName = entry.Description ?? string.Empty,
                LocationName = entry.LocationName ?? string.Empty,
                AmountRemaining = entry.AmountRemaining,
                Evolution = entry.Evolution,
                HealthPercentage = entry.HealthPercentage,
                SellerName = entry.SellerName ?? string.Empty,
                SellerFactionTag = entry.SellerFactionTag ?? string.Empty,
                PrivateSale = entry.PrivateSale,
                BuyerName = entry.PrivateSaleTo ?? string.Empty,
                SyncedByCharacterUUID = characterUUID,
                SyncTimestamp = syncTimestamp,
            };

            // Map price to PricePerUnit for domain consistency
            listing.PricePerUnit = (decimal)entry.Price;
            listing.Quantity = entry.AmountRemaining;

            return listing;
        }

        /// <summary>
        /// Updates an existing listing's fields from a fresh API entry.
        /// </summary>
        private static void UpdateExistingListing(
            MarketListing existing,
            ApiMarketListing entry,
            string characterUUID,
            string syncTimestamp)
        {
            existing.BuyOrder = entry.BuyOrder;
            existing.GameTypeCode = entry.Type ?? string.Empty;
            existing.GameTypeId = entry.TypeId;
            existing.GameSubTypeId = entry.SubTypeId?.ToString() ?? string.Empty;
            existing.ItemType = MapGameTypeCodeToItemType(entry.Type);
            existing.BaseItemTypeID = ResolveBaseItemTypeId(entry);
            existing.ResourcePurity = ResolveResourcePurity(entry);
            existing.ItemName = entry.Description ?? string.Empty;
            existing.LocationName = entry.LocationName ?? string.Empty;
            existing.AmountRemaining = entry.AmountRemaining;
            existing.Evolution = entry.Evolution;
            existing.HealthPercentage = entry.HealthPercentage;
            existing.SellerName = entry.SellerName ?? string.Empty;
            existing.SellerFactionTag = entry.SellerFactionTag ?? string.Empty;
            existing.PrivateSale = entry.PrivateSale;
            existing.BuyerName = entry.PrivateSaleTo ?? string.Empty;
            existing.SyncedByCharacterUUID = characterUUID;
            existing.SyncTimestamp = syncTimestamp;

            // Keep domain fields in sync
            existing.PricePerUnit = (decimal)entry.Price;
            existing.Quantity = entry.AmountRemaining;
        }

        /// <summary>
        /// Maps a Game API type code string to the domain ItemType enum.
        /// </summary>
        private static ItemType.ItemTypeEnum MapGameTypeCodeToItemType(string typeCode)
        {
            switch (typeCode)
            {
                case AssetTypeCodes.Resource:
                    return ItemType.ItemTypeEnum.Resource;
                case AssetTypeCodes.Commodity:
                    return ItemType.ItemTypeEnum.Commodity;
                case AssetTypeCodes.MarketBlueprint:
                    return ItemType.ItemTypeEnum.Blueprint;
                case AssetTypeCodes.MarketHull:
                    return ItemType.ItemTypeEnum.ShipHull;
                case AssetTypeCodes.MarketPart:
                    return ItemType.ItemTypeEnum.ShipPart;
                case AssetTypeCodes.Flatpack:
                    return ItemType.ItemTypeEnum.Flatpack;
                default:
                    return ItemType.ItemTypeEnum.None;
            }
        }

        /// <summary>
        /// Resolves the BaseItemTypeID from the DTO entry based on item type.
        /// For resources and commodities, uses the Description (item name).
        /// For blueprints, hulls, parts, and flatpacks, uses the BaseItemTypeId field
        /// (the blueprint UUID from the API).
        /// </summary>
        private static string ResolveBaseItemTypeId(ApiMarketListing entry)
        {
            string typeCode = entry.Type ?? string.Empty;

            switch (typeCode)
            {
                case AssetTypeCodes.Resource:
                case AssetTypeCodes.Commodity:
                    // Resources and commodities use the item name as BaseItemTypeID
                    return entry.Description ?? string.Empty;
                case AssetTypeCodes.MarketBlueprint:
                case AssetTypeCodes.MarketHull:
                case AssetTypeCodes.MarketPart:
                case AssetTypeCodes.Flatpack:
                    // Blueprint-based items use the BaseItemTypeId (blueprint UUID)
                    return entry.BaseItemTypeId?.ToString() ?? string.Empty;
                default:
                    return entry.Description ?? string.Empty;
            }
        }

        /// <summary>
        /// Resolves the resource purity from the DTO entry.
        /// For resources, the GroupA field contains the purity string (e.g. "High", "Medium", "Low", "Refined").
        /// For non-resource items, returns empty string.
        /// </summary>
        private static string ResolveResourcePurity(ApiMarketListing entry)
        {
            string typeCode = entry.Type ?? string.Empty;
            if (typeCode == AssetTypeCodes.Resource)
            {
                return entry.GroupA ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Maps an own buy order DTO to a new MarketListing domain object.
        /// </summary>
        private static MarketListing MapBuyOrderToListing(
            ApiMarketBuyOrder entry,
            string characterUUID,
            string syncTimestamp)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = entry.MarketId,
                OwnerUUID = characterUUID,
                BuyOrder = true,
                GameTypeCode = entry.TypeC ?? string.Empty,
                GameTypeId = entry.TypeId,
                ItemName = entry.ItemName ?? string.Empty,
                ItemType = MapGameTypeCodeToItemType(entry.TypeC),
                LocationName = entry.LocationName ?? string.Empty,
                SystemId = (int)entry.SystemId,
                SystemName = entry.SystemName ?? string.Empty,
                GameLocationId = (int)entry.SystemObjectId,
                AmountRemaining = entry.AmountRemaining,
                AmountOriginal = entry.AmountOriginal,
                AmountSold = entry.AmountFilled,
                EscrowRemaining = (decimal)entry.EscrowRemaining,
                IsOutbid = entry.IsOutbid,
                PlacedDT = entry.PlacedDT.ToString("o"),
                ExpiresDT = entry.ExpiresDT.ToString("o"),
                PricePerUnit = (decimal)entry.Price,
                Quantity = entry.AmountRemaining,
                SyncedByCharacterUUID = characterUUID,
                SyncTimestamp = syncTimestamp,
            };

            return listing;
        }

        /// <summary>
        /// Updates an existing listing from a fresh own buy order DTO.
        /// </summary>
        private static void UpdateFromBuyOrder(
            MarketListing existing,
            ApiMarketBuyOrder entry,
            string characterUUID,
            string syncTimestamp)
        {
            existing.OwnerUUID = characterUUID;
            existing.BuyOrder = true;
            existing.GameTypeCode = entry.TypeC ?? string.Empty;
            existing.GameTypeId = entry.TypeId;
            existing.ItemName = entry.ItemName ?? string.Empty;
            existing.ItemType = MapGameTypeCodeToItemType(entry.TypeC);
            existing.LocationName = entry.LocationName ?? string.Empty;
            existing.SystemId = (int)entry.SystemId;
            existing.SystemName = entry.SystemName ?? string.Empty;
            existing.GameLocationId = (int)entry.SystemObjectId;
            existing.AmountRemaining = entry.AmountRemaining;
            existing.AmountOriginal = entry.AmountOriginal;
            existing.AmountSold = entry.AmountFilled;
            existing.EscrowRemaining = (decimal)entry.EscrowRemaining;
            existing.IsOutbid = entry.IsOutbid;
            existing.PlacedDT = entry.PlacedDT.ToString("o");
            existing.ExpiresDT = entry.ExpiresDT.ToString("o");
            existing.PricePerUnit = (decimal)entry.Price;
            existing.Quantity = entry.AmountRemaining;
            existing.SyncedByCharacterUUID = characterUUID;
            existing.SyncTimestamp = syncTimestamp;
        }

        /// <summary>
        /// Maps an own sell order DTO to a new MarketListing domain object.
        /// </summary>
        private static MarketListing MapSellOrderToListing(
            ApiMarketSellOrder entry,
            string characterUUID,
            string syncTimestamp)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = entry.MarketId,
                OwnerUUID = characterUUID,
                BuyOrder = false,
                GameTypeCode = entry.TypeC ?? string.Empty,
                GameTypeId = entry.TypeId,
                ItemName = entry.ItemName ?? string.Empty,
                ItemType = MapGameTypeCodeToItemType(entry.TypeC),
                LocationName = entry.LocationName ?? string.Empty,
                SystemId = (int)entry.SystemId,
                SystemName = entry.SystemName ?? string.Empty,
                GameLocationId = (int)entry.SystemObjectId,
                AmountRemaining = entry.AmountRemaining,
                AmountOriginal = entry.AmountOriginal,
                AmountSold = entry.AmountSold,
                ValueRemaining = (decimal)entry.ValueRemaining,
                Evolution = entry.Evolution,
                HealthPercentage = entry.HealthPercentage,
                SalesTaxEstimate = (decimal)entry.SalesTaxEstimate,
                IsUndercut = entry.IsUndercut,
                PrivateSale = entry.CharacterIdTo.HasValue,
                BuyerName = entry.PrivateSaleToName ?? string.Empty,
                PlacedDT = entry.PlacedDT.ToString("o"),
                ExpiresDT = entry.ExpiresDT.ToString("o"),
                PricePerUnit = (decimal)entry.Price,
                Quantity = entry.AmountRemaining,
                SyncedByCharacterUUID = characterUUID,
                SyncTimestamp = syncTimestamp,
            };

            return listing;
        }

        /// <summary>
        /// Updates an existing listing from a fresh own sell order DTO.
        /// </summary>
        private static void UpdateFromSellOrder(
            MarketListing existing,
            ApiMarketSellOrder entry,
            string characterUUID,
            string syncTimestamp)
        {
            existing.OwnerUUID = characterUUID;
            existing.BuyOrder = false;
            existing.GameTypeCode = entry.TypeC ?? string.Empty;
            existing.GameTypeId = entry.TypeId;
            existing.ItemName = entry.ItemName ?? string.Empty;
            existing.ItemType = MapGameTypeCodeToItemType(entry.TypeC);
            existing.LocationName = entry.LocationName ?? string.Empty;
            existing.SystemId = (int)entry.SystemId;
            existing.SystemName = entry.SystemName ?? string.Empty;
            existing.GameLocationId = (int)entry.SystemObjectId;
            existing.AmountRemaining = entry.AmountRemaining;
            existing.AmountOriginal = entry.AmountOriginal;
            existing.AmountSold = entry.AmountSold;
            existing.ValueRemaining = (decimal)entry.ValueRemaining;
            existing.Evolution = entry.Evolution;
            existing.HealthPercentage = entry.HealthPercentage;
            existing.SalesTaxEstimate = (decimal)entry.SalesTaxEstimate;
            existing.IsUndercut = entry.IsUndercut;
            existing.PrivateSale = entry.CharacterIdTo.HasValue;
            existing.BuyerName = entry.PrivateSaleToName ?? string.Empty;
            existing.PlacedDT = entry.PlacedDT.ToString("o");
            existing.ExpiresDT = entry.ExpiresDT.ToString("o");
            existing.PricePerUnit = (decimal)entry.Price;
            existing.Quantity = entry.AmountRemaining;
            existing.SyncedByCharacterUUID = characterUUID;
            existing.SyncTimestamp = syncTimestamp;
        }

        /// <summary>
        /// Maps a competitor DTO to a new MarketListing domain object.
        /// </summary>
        private static MarketListing MapCompetitorToListing(
            ApiMarketCompetitor entry,
            long ownOrderMarketId,
            bool isBuyCompetitor,
            string characterUUID,
            string syncTimestamp)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = entry.MarketId,
                BuyOrder = isBuyCompetitor,
                CompetitorForMarketId = ownOrderMarketId,
                LocationName = entry.LocationName ?? string.Empty,
                AmountRemaining = entry.AmountRemaining,
                SellerName = entry.PilotName ?? string.Empty,
                PricePerUnit = (decimal)entry.Price,
                Quantity = entry.AmountRemaining,
                SyncedByCharacterUUID = characterUUID,
                SyncTimestamp = syncTimestamp,
            };

            return listing;
        }

        /// <summary>
        /// Updates an existing listing from a fresh competitor DTO.
        /// </summary>
        private static void UpdateFromCompetitor(
            MarketListing existing,
            ApiMarketCompetitor entry,
            long ownOrderMarketId,
            bool isBuyCompetitor,
            string characterUUID,
            string syncTimestamp)
        {
            existing.BuyOrder = isBuyCompetitor;
            existing.CompetitorForMarketId = ownOrderMarketId;
            existing.LocationName = entry.LocationName ?? string.Empty;
            existing.AmountRemaining = entry.AmountRemaining;
            existing.SellerName = entry.PilotName ?? string.Empty;
            existing.PricePerUnit = (decimal)entry.Price;
            existing.Quantity = entry.AmountRemaining;
            existing.SyncedByCharacterUUID = characterUUID;
            existing.SyncTimestamp = syncTimestamp;
        }

        /// <summary>
        /// Resolves the character name for the given UUID by looking up the player profile.
        /// Returns empty string if no profile is found.
        /// </summary>
        private string ResolveCharacterName(string characterUUID)
        {
            PlayerProfile profile = _playerContext.FindPlayerProfile(characterUUID);
            if (profile != null && !string.IsNullOrEmpty(profile.Name))
            {
                return profile.Name;
            }

            return string.Empty;
        }

        /// <summary>
        /// Finds stored price stats matching the given resource name and purity.
        /// Searches by BaseItemTypeID (resource name) and ResourcePurity, preferring
        /// more recent data.
        /// </summary>
        private StoredPriceStats FindMatchingPriceStats(
            List<StoredPriceStats> allStats,
            string resourceName,
            string purity)
        {
            StoredPriceStats best = null;

            foreach (StoredPriceStats stats in allStats)
            {
                if (!string.Equals(stats.BaseItemTypeID, resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(stats.ResourcePurity, purity, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(purity)
                    && !string.IsNullOrEmpty(stats.ResourcePurity))
                {
                    continue;
                }

                if (!stats.AvgPrice.HasValue)
                {
                    continue;
                }

                if (best == null)
                {
                    best = stats;
                }
                else
                {
                    // Prefer more recent data
                    if (string.Compare(stats.FetchedTimestamp, best.FetchedTimestamp, StringComparison.Ordinal) > 0)
                    {
                        best = stats;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Upserts a <see cref="StoredPriceStats"/> entry into the PriceStats collection.
        /// Matches by composite key: ItemType + BaseItemTypeID + ResourcePurity + GameTypeCode + OrderType.
        /// </summary>
        private void UpsertPriceStats(StoredPriceStats stats)
        {
            List<StoredPriceStats> priceStats = _playerContext.MarketSyncData.PriceStats;

            for (int i = 0; i < priceStats.Count; i++)
            {
                StoredPriceStats existing = priceStats[i];
                if (existing.ItemType == stats.ItemType
                    && existing.BaseItemTypeID == stats.BaseItemTypeID
                    && existing.ResourcePurity == stats.ResourcePurity
                    && existing.GameTypeCode == stats.GameTypeCode
                    && existing.OrderType == stats.OrderType)
                {
                    priceStats[i] = stats;
                    return;
                }
            }

            priceStats.Add(stats);
        }
    }
}
