using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class StoredPriceStats
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;

        public string BaseItemTypeID { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string ResourcePurity { get; set; } = string.Empty;

        public string GameTypeCode { get; set; } = string.Empty;

        public long GameTypeId { get; set; }

        public decimal? LowPrice { get; set; }

        public decimal? AvgPrice { get; set; }

        public decimal? HighPrice { get; set; }

        public int SampleCount { get; set; }

        public string SearchRadius { get; set; } = string.Empty;

        public int DaysSearched { get; set; }

        public string OrderType { get; set; } = string.Empty;

        public string FetchedTimestamp { get; set; } = string.Empty;

        public string FetchedByCharacterUUID { get; set; } = string.Empty;
    }
}
