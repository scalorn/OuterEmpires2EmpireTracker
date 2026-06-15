using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum StockTargetScope
    {
        EmpireWide,
        Colony,
        Station,
        Market,
        StationPlusMarket
    }

    public class StockTarget
    {
        public string UUID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
        public string ItemReferenceID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ShipTemplateUUID { get; set; } = string.Empty;

        public int TargetQuantity { get; set; } = 0;
        public int CriticalThreshold { get; set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        [System.ComponentModel.DefaultValue(StockTargetScope.EmpireWide)]
        public StockTargetScope Scope { get; set; } = StockTargetScope.EmpireWide;
        public string LocationUUID { get; set; } = string.Empty;
    }
}
