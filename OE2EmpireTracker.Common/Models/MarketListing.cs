using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class MarketListing
    {
        public string UUID { get; internal set; }
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string StationUUID { get; internal set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; internal set; } = Models.ItemType.ItemTypeEnum.None;
        public string ItemReferenceID { get; internal set; } = string.Empty;
        public string ItemName { get; internal set; } = string.Empty;

        public int Quantity { get; internal set; } = 0;
        public decimal PricePerUnit { get; internal set; } = 0m;

        public int CurrentHP { get; internal set; } = 0;
        public int MaxHP { get; internal set; } = 0;
        public decimal MaxRepairPercent { get; internal set; } = 0m;
    }
}
