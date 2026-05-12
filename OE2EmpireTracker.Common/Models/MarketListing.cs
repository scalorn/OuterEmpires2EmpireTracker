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
    }
}
