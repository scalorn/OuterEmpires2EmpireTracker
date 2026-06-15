using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class MarketListing
    {
        public string UUID { get; set; }
        public string OwnerUUID { get; set; } = string.Empty;
        public string StationUUID { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
        public string ItemReferenceID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; } = 0;
        public decimal PricePerUnit { get; set; } = 0m;

        public int CurrentHP { get; set; } = 0;
        public int MaxHP { get; set; } = 0;
        public decimal MaxRepairPercent { get; set; } = 0m;

        // API sync fields (nullable — populated only for synced orders)
        public long? MarketId { get; set; }
        public bool BuyOrder { get; set; }
        public string BaseItemTypeID { get; set; } = string.Empty;
        public string ResourcePurity { get; set; } = string.Empty;

        // Raw API identifiers (for re-sync dedup)
        public string GameTypeCode { get; set; } = string.Empty;
        public long? GameTypeId { get; set; }
        public string GameSubTypeId { get; set; } = string.Empty;

        // Location details
        public string LocationName { get; set; } = string.Empty;
        public int? SystemId { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public int? GameLocationId { get; set; }

        // Order details
        public int? AmountRemaining { get; set; }
        public int? AmountOriginal { get; set; }
        public int? AmountSold { get; set; }
        public decimal? EscrowRemaining { get; set; }
        public decimal? SalesTaxEstimate { get; set; }
        public decimal? ValueRemaining { get; set; }
        public int? Evolution { get; set; }
        public double? HealthPercentage { get; set; }

        // Seller / buyer info
        public string SellerName { get; set; } = string.Empty;
        public string SellerFactionTag { get; set; } = string.Empty;
        public bool PrivateSale { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerFactionTag { get; set; } = string.Empty;

        // Outbid / undercut flags (own orders)
        public bool IsOutbid { get; set; }
        public bool IsUndercut { get; set; }

        // Timing
        public string PlacedDT { get; set; } = string.Empty;
        public string ExpiresDT { get; set; } = string.Empty;

        // Sync tracking
        public string SyncedByCharacterUUID { get; set; } = string.Empty;
        public string SyncTimestamp { get; set; } = string.Empty;
    }
}
