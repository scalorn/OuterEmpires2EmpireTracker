using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace OE2EmpireTracker.Models
{
    public enum BuildItemType
    {
        Manufactory,
        Commodity,
        ShipTemplate,
        Mining,
        Refining,
        Research
    }

    public enum BuildItemStatus
    {
        Staged,
        Delivering,
        Ready,
        InProgress,
        Completed
    }

    public class BuildItem
    {
        public string UUID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BuildItemType ItemType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(BuildItemStatus.Staged)]
        public BuildItemStatus Status { get; set; } = BuildItemStatus.Staged;

        public string BlueprintUUID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public string ShipTemplateUUID { get; set; } = string.Empty;

        public int Quantity { get; set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Colony)]
        public DestinationType BuildLocationType { get; set; } = DestinationType.Colony;
        public string BuildLocationUUID { get; set; } = string.Empty;
        public string StructureUUID { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Station)]
        public DestinationType AssemblyLocationType { get; set; } = DestinationType.Station;
        public string AssemblyLocationUUID { get; set; } = string.Empty;

        public string ParentBuildItemUUID { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int SequenceInStructure { get; set; } = 0;
        public string DependsOnUUID { get; set; } = string.Empty;

        public string MiningResource { get; set; } = string.Empty;
        public string MiningSurveyUUID { get; set; } = string.Empty;
        public string RefiningResource { get; set; } = string.Empty;
        public string RefiningPurity { get; set; } = string.Empty;
    }
}
