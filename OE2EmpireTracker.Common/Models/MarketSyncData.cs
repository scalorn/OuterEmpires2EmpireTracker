using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class MarketSyncData
    {
        public List<StoredPriceStats> PriceStats { get; set; } = new List<StoredPriceStats>();

        public List<CharacterSyncMetadata> SyncMetadata { get; set; } = new List<CharacterSyncMetadata>();

        public List<MarketAlert> Alerts { get; set; } = new List<MarketAlert>();

        public List<SavedMarketSearch> SavedSearches { get; set; } = new List<SavedMarketSearch>();
    }
}
