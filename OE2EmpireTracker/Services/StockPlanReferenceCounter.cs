using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a stock plan from stock profile entries (StockProfileEntry.StockPlanUUID).
    /// </summary>
    public class StockPlanReferenceCounter
    {
        private readonly Dictionary<string, int> _profileEntryMap;

        public StockPlanReferenceCounter(IEnumerable<StockProfile> stockProfiles)
        {
            var list = stockProfiles ?? Enumerable.Empty<StockProfile>();

            _profileEntryMap = new Dictionary<string, int>();
            foreach (var profile in list)
            {
                if (profile.Entries == null) continue;
                foreach (var entry in profile.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.StockPlanUUID))
                    {
                        _profileEntryMap.TryGetValue(entry.StockPlanUUID, out int c);
                        _profileEntryMap[entry.StockPlanUUID] = c + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of stock profile entries that reference the given stock plan UUID.
        /// </summary>
        public int CountReferences(string stockPlanUUID)
        {
            if (string.IsNullOrEmpty(stockPlanUUID))
                return 0;

            _profileEntryMap.TryGetValue(stockPlanUUID, out int count);
            return count;
        }
    }
}
