using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class WarehouseOverflowRule
    {
        public string UUID { get; internal set; }
        public string OwnerUUID { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;
        public string ColonyUUID { get; internal set; } = string.Empty;
        public string ResourceName { get; internal set; } = string.Empty;
        public string ResourcePurity { get; internal set; } = string.Empty;
        public int TriggerThreshold { get; internal set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType DestinationType { get; internal set; } = DestinationType.Station;
        public string DestinationUUID { get; internal set; } = string.Empty;

        public string DeliveryRouteUUID { get; internal set; } = string.Empty;
    }
}
