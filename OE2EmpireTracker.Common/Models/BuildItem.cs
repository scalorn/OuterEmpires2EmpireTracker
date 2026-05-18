using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        public string UUID { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BuildItemType ItemType { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(BuildItemStatus.Staged)]
        public BuildItemStatus Status { get; internal set; } = BuildItemStatus.Staged;

        public string BlueprintUUID { get; internal set; } = string.Empty;
        public string ItemName { get; internal set; } = string.Empty;
        public string CommodityName { get; internal set; } = string.Empty;
        public string ShipTemplateUUID { get; internal set; } = string.Empty;

        public int Quantity { get; internal set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Colony)]
        public DestinationType BuildLocationType { get; internal set; } = DestinationType.Colony;
        public string BuildLocationUUID { get; internal set; } = string.Empty;
        public string StructureUUID { get; internal set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Station)]
        public DestinationType AssemblyLocationType { get; internal set; } = DestinationType.Station;
        public string AssemblyLocationUUID { get; internal set; } = string.Empty;

        public string ParentBuildItemUUID { get; internal set; } = string.Empty;

        public string Recipient { get; internal set; } = string.Empty;
        public string Notes { get; internal set; } = string.Empty;
        public int SequenceInStructure { get; internal set; } = 0;
        public string DependsOnUUID { get; internal set; } = string.Empty;

        public string MiningResource { get; internal set; } = string.Empty;
        public string MiningSurveyUUID { get; internal set; } = string.Empty;
        public string RefiningResource { get; internal set; } = string.Empty;
        public string RefiningPurity { get; internal set; } = string.Empty;
    }
}
