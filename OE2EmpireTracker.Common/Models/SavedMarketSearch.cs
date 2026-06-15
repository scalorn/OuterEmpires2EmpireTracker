using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class SavedMarketSearch
    {
        public string UUID { get; set; }

        public string CharacterUUID { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;

        public string BaseItemTypeID { get; set; } = string.Empty;

        public string ResourcePurity { get; set; } = string.Empty;

        public string SearchText { get; set; } = string.Empty;

        public bool? BuyOrdersOnly { get; set; }

        public string GameTypeCode { get; set; } = "all";

        public string LastExecutedTimestamp { get; set; } = string.Empty;

        public int LastResultCount { get; set; }
    }
}
