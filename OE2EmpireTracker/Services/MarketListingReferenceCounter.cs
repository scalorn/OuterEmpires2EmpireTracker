using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts MarketTransaction.ListingUUID references to a MarketListing.
    /// Used to prevent deletion of listings that have recorded transactions.
    /// </summary>
    public class MarketListingReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _transactionMap;

        public MarketListingReferenceCounter(IEnumerable<MarketTransaction> transactions)
        {
            var txList = transactions ?? Enumerable.Empty<MarketTransaction>();
            _transactionMap = new Dictionary<string, int>();
            foreach (var tx in txList)
            {
                if (!string.IsNullOrEmpty(tx.ListingUUID))
                {
                    _transactionMap.TryGetValue(tx.ListingUUID, out int c);
                    _transactionMap[tx.ListingUUID] = c + 1;
                }
            }
        }

        /// <summary>
        /// Returns the number of transactions referencing the given listing UUID.
        /// </summary>
        public int CountReferences(string listingUUID)
        {
            if (string.IsNullOrEmpty(listingUUID))
                return 0;
            _transactionMap.TryGetValue(listingUUID, out int count);
            return count;
        }
    }
}