using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum StockTargetScope
    {
        EmpireWide,
        Colony,
        Station
    }

    public class StockTarget
    {
        public string UUID { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; internal set; } = Models.ItemType.ItemTypeEnum.None;
        public string ItemReferenceID { get; internal set; } = string.Empty;
        public string ItemName { get; internal set; } = string.Empty;
        public string ShipTemplateUUID { get; internal set; } = string.Empty;

        public int TargetQuantity { get; internal set; } = 0;
        public int CriticalThreshold { get; internal set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        [System.ComponentModel.DefaultValue(StockTargetScope.EmpireWide)]
        public StockTargetScope Scope { get; internal set; } = StockTargetScope.EmpireWide;
        public string LocationUUID { get; internal set; } = string.Empty;
    }
}
