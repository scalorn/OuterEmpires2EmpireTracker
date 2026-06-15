using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using ApiMarketListing = OE2EmpireTracker.Common.Client.Generated.MarketListing;
using ApiMarketListings = OE2EmpireTracker.Common.Client.Generated.MarketListings;

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
    }
}
