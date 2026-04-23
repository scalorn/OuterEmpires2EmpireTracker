using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for market transaction recording and profit/loss computation.
    /// </summary>
    public static class MarketService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Records a sell transaction: decrements the listing quantity, creates a
        /// MarketTransaction with condition and faction snapshots.
        /// Returns the created transaction, or null if quantity exceeds listing availability.
        /// </summary>
        /// <param name="listing">The market listing being sold from.</param>
        /// <param name="quantitySold">Number of units sold.</param>
        /// <param name="pricePerUnit">Price per unit for the sale.</param>
        /// <param name="counterpartyName">Name of the buyer.</param>
        /// <param name="counterpartyFactionName">Faction name of the buyer.</param>
        /// <param name="stationUUID">UUID of the station where the sale occurs.</param>
        /// <returns>The created MarketTransaction, or null if quantity exceeds listing.</returns>
        public static MarketTransaction RecordSale(
            MarketListing listing,
            int quantitySold,
            decimal pricePerUnit,
            string counterpartyName,
            string counterpartyFactionName,
            string stationUUID)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));

            var sw = Stopwatch.StartNew();

            if (quantitySold <= 0)
            {
                Log.Warn("RecordSale: rejected non-positive quantity {0}", quantitySold);
                sw.Stop();
                Log.Info("PERF RecordSale: rejected in {0}ms", sw.ElapsedMilliseconds);
                return null;
            }

            if (quantitySold > listing.Quantity)
            {
                Log.Warn("RecordSale: quantity {0} exceeds listing availability {1} for listing {2}",
                    quantitySold, listing.Quantity, listing.UUID);
                sw.Stop();
                Log.Info("PERF RecordSale: rejected in {0}ms", sw.ElapsedMilliseconds);
                return null;
            }

            listing.Quantity -= quantitySold;

            var transaction = new MarketTransaction
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = listing.OwnerUUID,
                TransactionType = TransactionType.Sell,
                ItemType = listing.ItemType,
                ItemReferenceID = listing.ItemReferenceID,
                ItemName = listing.ItemName,
                Quantity = quantitySold,
                PricePerUnit = pricePerUnit,
                TotalPrice = pricePerUnit * quantitySold,
                Counterparty = counterpartyName ?? string.Empty,
                CounterpartyFaction = counterpartyFactionName ?? string.Empty,
                StationUUID = stationUUID ?? string.Empty,
                Timestamp = SystemClock.UtcNow.ToString("o"),
                ListingUUID = listing.UUID ?? string.Empty,
                CurrentHP = listing.CurrentHP,
                MaxHP = listing.MaxHP,
                MaxRepairPercent = listing.MaxRepairPercent
            };

            sw.Stop();
            Log.Info("PERF RecordSale: recorded sale of {0}x '{1}' at {2}/unit in {3}ms",
                quantitySold, listing.ItemName, pricePerUnit, sw.ElapsedMilliseconds);

            return transaction;
        }

        /// <summary>
        /// Records a buy transaction and adds the purchased items to the station hold
        /// for the transaction's owner.
        /// Returns the created transaction.
        /// </summary>
        /// <param name="itemType">Type of item purchased.</param>
        /// <param name="itemName">Name of the item purchased.</param>
        /// <param name="itemReferenceID">Reference ID of the item (e.g. blueprint UUID).</param>
        /// <param name="quantity">Number of units purchased.</param>
        /// <param name="pricePerUnit">Price per unit paid.</param>
        /// <param name="stationUUID">UUID of the station where the purchase occurs.</param>
        /// <param name="counterpartyName">Name of the seller.</param>
        /// <param name="counterpartyFactionName">Faction name of the seller.</param>
        /// <param name="ownerUUID">UUID of the player making the purchase.</param>
        /// <param name="stationFinder">Delegate to resolve station by UUID.</param>
        /// <returns>The created MarketTransaction.</returns>
        public static MarketTransaction RecordPurchase(
            ItemType.ItemTypeEnum itemType,
            string itemName,
            string itemReferenceID,
            int quantity,
            decimal pricePerUnit,
            string stationUUID,
            string counterpartyName,
            string counterpartyFactionName,
            string ownerUUID,
            Func<string, Station> stationFinder)
        {
            if (stationFinder == null) throw new ArgumentNullException(nameof(stationFinder));

            var sw = Stopwatch.StartNew();

            var transaction = new MarketTransaction
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = ownerUUID ?? string.Empty,
                TransactionType = TransactionType.Buy,
                ItemType = itemType,
                ItemReferenceID = itemReferenceID ?? string.Empty,
                ItemName = itemName ?? string.Empty,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                TotalPrice = pricePerUnit * quantity,
                Counterparty = counterpartyName ?? string.Empty,
                CounterpartyFaction = counterpartyFactionName ?? string.Empty,
                StationUUID = stationUUID ?? string.Empty,
                Timestamp = SystemClock.UtcNow.ToString("o")
            };

            if (!string.IsNullOrEmpty(stationUUID) && !string.IsNullOrEmpty(ownerUUID))
            {
                Station station = stationFinder(stationUUID);
                if (station != null)
                {
                    if (!station.Holds.TryGetValue(ownerUUID, out ItemBag hold))
                    {
                        hold = new ItemBag();
                        station.Holds[ownerUUID] = hold;
                    }

                    for (int i = 0; i < quantity; i++)
                    {
                        var item = new Item(itemType, itemName ?? string.Empty)
                        {
                            UUID = Guid.NewGuid().ToString(),
                            BaseItemTypeID = itemReferenceID ?? string.Empty,
                            Quantity = 1
                        };

                        hold.AddItem(item);
                    }

                    Log.Debug("RecordPurchase: added {0} items to station {1} hold for player {2}",
                        quantity, stationUUID, ownerUUID);
                }
                else
                {
                    Log.Warn("RecordPurchase: station {0} not found, items not added to hold", stationUUID);
                }
            }

            sw.Stop();
            Log.Info("PERF RecordPurchase: recorded purchase of {0}x '{1}' at {2}/unit in {3}ms",
                quantity, itemName, pricePerUnit, sw.ElapsedMilliseconds);

            return transaction;
        }

        /// <summary>
        /// Computes profit/loss summary from a list of market transactions with optional filters.
        /// </summary>
        /// <param name="transactions">All transactions to consider.</param>
        /// <param name="startDate">Optional start date filter (inclusive). Null to skip.</param>
        /// <param name="endDate">Optional end date filter (inclusive). Null to skip.</param>
        /// <param name="itemNameFilter">Optional item name filter (exact match). Null to skip.</param>
        /// <param name="stationUUIDFilter">Optional station UUID filter. Null to skip.</param>
        /// <returns>A ProfitLossSummary with totals and per-item breakdown.</returns>
        public static ProfitLossSummary ComputeProfitLoss(
            IEnumerable<MarketTransaction> transactions,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string itemNameFilter = null,
            string stationUUIDFilter = null)
        {
            if (transactions == null) throw new ArgumentNullException(nameof(transactions));

            var sw = Stopwatch.StartNew();
            var summary = new ProfitLossSummary();

            foreach (var tx in transactions)
            {
                if (!PassesFilters(tx, startDate, endDate, itemNameFilter, stationUUIDFilter))
                    continue;

                if (!summary.ItemBreakdown.TryGetValue(tx.ItemName, out ProfitLossItemBreakdown breakdown))
                {
                    breakdown = new ProfitLossItemBreakdown { ItemName = tx.ItemName };
                    summary.ItemBreakdown[tx.ItemName] = breakdown;
                }

                if (tx.TransactionType == TransactionType.Sell)
                {
                    decimal revenue = tx.TotalPrice;
                    summary.TotalSalesRevenue += revenue;
                    breakdown.SalesRevenue += revenue;
                    breakdown.QuantitySold += tx.Quantity;
                }
                else if (tx.TransactionType == TransactionType.Buy)
                {
                    decimal cost = tx.TotalPrice;
                    summary.TotalPurchaseCost += cost;
                    breakdown.PurchaseCost += cost;
                    breakdown.QuantityBought += tx.Quantity;
                }
            }

            summary.NetProfitLoss = summary.TotalSalesRevenue - summary.TotalPurchaseCost;

            foreach (var breakdown in summary.ItemBreakdown.Values)
            {
                breakdown.NetProfitLoss = breakdown.SalesRevenue - breakdown.PurchaseCost;
            }

            sw.Stop();
            Log.Info("PERF ComputeProfitLoss: computed over {0} item(s), net={1} in {2}ms",
                summary.ItemBreakdown.Count, summary.NetProfitLoss, sw.ElapsedMilliseconds);

            return summary;
        }

        /// <summary>
        /// Checks whether a transaction passes the optional filters.
        /// </summary>
        private static bool PassesFilters(
            MarketTransaction tx,
            DateTime? startDate,
            DateTime? endDate,
            string itemNameFilter,
            string stationUUIDFilter)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                if (!DateTime.TryParse(tx.Timestamp, out DateTime txDate))
                    return false;

                if (startDate.HasValue && txDate < startDate.Value)
                    return false;

                if (endDate.HasValue && txDate > endDate.Value)
                    return false;
            }

            if (itemNameFilter != null &&
                !string.Equals(tx.ItemName, itemNameFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (stationUUIDFilter != null &&
                !string.Equals(tx.StationUUID, stationUUIDFilter, StringComparison.Ordinal))
                return false;

            return true;
        }
    }
}
