using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum StationType
    {
        Outpost,
        Station,
        Starbase
    }

    public enum StationOwnership
    {
        Government,
        PlayerOwned
    }

    public class Station
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public StationType StationType { get; internal set; } = Models.StationType.Station;

        [JsonConverter(typeof(StringEnumConverter))]
        public StationOwnership Ownership { get; internal set; } = StationOwnership.Government;

        public string OwnerUUID { get; internal set; } = string.Empty;

        public Dictionary<string, ItemBag> Holds { get; internal set; } = new Dictionary<string, ItemBag>();

        public List<ShipComponentSlot> Components { get; internal set; } = new List<ShipComponentSlot>();
        public string StationBlueprintUUID { get; internal set; } = string.Empty;

        public ItemBag MunitionsHold { get; internal set; } = new ItemBag();

        public int HullCurrentHP { get; internal set; } = 0;
        public int HullMaxHP { get; internal set; } = 0;
        public decimal HullMaxRepairPercent { get; internal set; } = 0m;
    }
}
