using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum TransactionType
    {
        Buy,
        Sell
    }

    public class MarketTransaction
    {
        public string UUID { get; internal set; }
        public string OwnerUUID { get; internal set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionType TransactionType { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; internal set; } = Models.ItemType.ItemTypeEnum.None;
        public string ItemReferenceID { get; internal set; } = string.Empty;
        public string ItemName { get; internal set; } = string.Empty;

        public int Quantity { get; internal set; } = 0;
        public decimal PricePerUnit { get; internal set; } = 0m;
        public decimal TotalPrice { get; internal set; } = 0m;
        public string Counterparty { get; internal set; } = string.Empty;
        public string CounterpartyFaction { get; internal set; } = string.Empty;
        public string StationUUID { get; internal set; } = string.Empty;
        public string Timestamp { get; internal set; } = string.Empty;
        public string Notes { get; internal set; } = string.Empty;
        public string ListingUUID { get; internal set; } = string.Empty;

        public int CurrentHP { get; internal set; } = 0;
        public int MaxHP { get; internal set; } = 0;
        public decimal MaxRepairPercent { get; internal set; } = 0m;
    }
}
