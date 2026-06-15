using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum MarketAlertType
    {
        SellOrderAppears,
        BuyOrderAppears
    }

    public enum PriceCondition
    {
        AnyPrice,
        AtOrBelow,
        AtOrAbove
    }

    public class MarketAlert
    {
        public string UUID { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public MarketAlertType AlertType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;

        public string BaseItemTypeID { get; set; } = string.Empty;

        public string ResourcePurity { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public PriceCondition PriceCondition { get; set; } = PriceCondition.AnyPrice;

        public decimal? PriceThreshold { get; set; }

        public string StationUUID { get; set; } = string.Empty;

        public string LastTriggeredTimestamp { get; set; } = string.Empty;

        public long? LastTriggeredMarketId { get; set; }
    }
}
