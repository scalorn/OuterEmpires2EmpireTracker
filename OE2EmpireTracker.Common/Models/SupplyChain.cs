using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum SupplyChainStageType
    {
        Mine,
        AsteroidMine,
        PickUp,
        Refine,
        Deliver,
        Research
    }

    public class SupplyChain
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;
        public List<SupplyChainStage> Stages { get; internal set; } = new List<SupplyChainStage>();
    }

    public class SupplyChainStage
    {
        public int Sequence { get; internal set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        public SupplyChainStageType StageType { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType LocationType { get; internal set; }
        public string LocationUUID { get; internal set; } = string.Empty;

        public string ResourceName { get; internal set; } = string.Empty;
        public string ResourcePurity { get; internal set; } = string.Empty;

        public int AccumulationThreshold { get; internal set; } = 0;
        public decimal ProductionRatePerHour { get; internal set; } = 0m;

        public string DeliveryRouteUUID { get; internal set; } = string.Empty;
    }
}
