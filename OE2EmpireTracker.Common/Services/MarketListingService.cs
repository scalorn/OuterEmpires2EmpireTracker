using System;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all MarketListing mutation. The forms never touch the entity directly.
    /// Only this service (plus JSON deserialization, migration code, and the existing static
    /// MarketService.RecordSale called by this service) mutates MarketListing objects.
    /// </summary>
    public class MarketListingService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public MarketListingService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Creates a new MarketListing with generated UUID, sets OwnerUUID from current player,
        /// populates all fields from request, adds to PlayerContext, invalidates cache, persists,
        /// fires event, returns ReadOnlyMarketListing.
        /// </summary>
        public ReadOnlyMarketListing CreateListing(MarketListingCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = _playerContext.CurrentPlayerUUID,
                ItemName = request.ItemName,
                ItemType = request.ItemType,
                ItemReferenceID = request.ItemReferenceID,
                StationUUID = request.StationUUID,
                Quantity = request.Quantity,
                PricePerUnit = request.PricePerUnit,
                CurrentHP = request.CurrentHP,
                MaxHP = request.MaxHP,
                MaxRepairPercent = request.MaxRepairPercent,
            };

            Log.Info(
                "MarketListingService.CreateListing: item='{0}' UUID={1}",
                listing.ItemName,
                listing.UUID);

            _playerContext.AddMarketListing(listing);
            _playerContext.InvalidateMarketListingCache();
            _playerContext.WriteContext();
            _playerContext.OnMarketDataChanged();
            return new ReadOnlyMarketListing(listing);
        }

        /// <summary>
        /// Applies changes from the update request to an existing listing, persists, and fires event.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyMarketListing UpdateListing(string uuid, MarketListingUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var listing = _playerContext.FindMutableMarketListing(uuid);
            if (listing == null)
            {
                throw new InvalidOperationException("Listing not found: " + uuid);
            }

            Log.Info(
                "MarketListingService.UpdateListing: UUID={0} item='{1}' -> '{2}'",
                uuid,
                listing.ItemName,
                request.ItemName);

            listing.ItemName = request.ItemName;
            listing.ItemType = request.ItemType;
            listing.ItemReferenceID = request.ItemReferenceID;
            listing.StationUUID = request.StationUUID;
            listing.Quantity = request.Quantity;
            listing.PricePerUnit = request.PricePerUnit;
            listing.CurrentHP = request.CurrentHP;
            listing.MaxHP = request.MaxHP;
            listing.MaxRepairPercent = request.MaxRepairPercent;

            _playerContext.InvalidateMarketListingCache();
            _playerContext.WriteContext();
            _playerContext.OnMarketDataChanged();
            return new ReadOnlyMarketListing(listing);
        }

        /// <summary>
        /// Removes a market listing. No-op if UUID is empty or not found.
        /// </summary>
        public void DeleteListing(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var listing = _playerContext.FindMutableMarketListing(uuid);
            if (listing == null)
            {
                return;
            }

            Log.Info(
                "MarketListingService.DeleteListing: UUID={0} item='{1}'",
                uuid,
                listing.ItemName);

            _playerContext.RemoveMarketListing(listing);
            _playerContext.InvalidateMarketListingCache();
            _playerContext.WriteContext();
            _playerContext.OnMarketDataChanged();
        }

        /// <summary>
        /// Records a sale: looks up mutable listing, delegates to static MarketService.RecordSale,
        /// adds transaction to PlayerContext, invalidates caches, persists, fires event.
        /// Returns the transaction (or null if validation failed).
        /// Throws InvalidOperationException if listing UUID not found.
        /// </summary>
        public MarketTransaction RecordSale(
            string listingUUID,
            int quantity,
            decimal pricePerUnit,
            string counterparty,
            string counterpartyFaction,
            string stationUUID)
        {
            if (string.IsNullOrEmpty(listingUUID))
            {
                throw new ArgumentNullException(nameof(listingUUID));
            }

            var listing = _playerContext.FindMutableMarketListing(listingUUID);
            if (listing == null)
            {
                throw new InvalidOperationException("Listing not found: " + listingUUID);
            }

            var transaction = MarketService.RecordSale(
                listing,
                quantity,
                pricePerUnit,
                counterparty,
                counterpartyFaction,
                stationUUID);

            if (transaction == null)
            {
                return null;
            }

            Log.Info(
                "MarketListingService.RecordSale: {0}x '{1}' at {2}/unit",
                quantity,
                listing.ItemName,
                pricePerUnit);

            _playerContext.AddMarketTransaction(transaction);
            _playerContext.InvalidateMarketListingCache();
            _playerContext.WriteContext();
            _playerContext.OnMarketDataChanged();
            return transaction;
        }
    }
}
