using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class WarehouseOverflowRule
    {
        public string UUID { get; set; }
        public string OwnerUUID { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string ColonyUUID { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourcePurity { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public OverflowRuleType RuleType { get; set; } = OverflowRuleType.SpecificResource;

        public decimal TriggerThreshold { get; set; } = 0m;

        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType DestinationType { get; set; } = DestinationType.Station;
        public string DestinationUUID { get; set; } = string.Empty;

        public string DeliveryRouteUUID { get; set; } = string.Empty;
    }
}
