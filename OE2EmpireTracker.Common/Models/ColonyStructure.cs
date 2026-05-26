using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Models
{
    public class ColonyStructure
    {
        public ColonyStructure() : base()
        {
            Properties = new PropertyBag();
            AssignedWorkers = new PropertyBag();
        }

        /// <summary>
        /// Returns true if the structure has Built=True and Online=True in its PropertyBag.
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltAndOnline
        {
            get
            {
                bool built;
                Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
                if (!built) return false;
                bool online;
                Properties.GetBoolean(GameConstants.PropOnline, false, out online);
                return online;
            }
        }

        public string UUID { get; set; } = null;

        public string FlatpackBlueprintUUID { get; set; } = null;

        [JsonProperty("displaySequence")]
        public int DisplaySequence { get; set; } = 0;

        [JsonProperty("buildingID")]
        public int BuildingID { get; set; } = 0;

        [JsonProperty("buildQueueSequence")]
        public int BuildQueueSequence { get; set; } = 0;

        public PropertyBag Properties { get; set; }

        public PropertyBag AssignedWorkers { get; set; }

        public CountDownTime BuildCompletionTime { get; set; } = null;

        public CountDownTime ProcessCompletionTime { get; set; } = null;

        public string MiningSurvey { get; set; } = null;

        public string MiningSurveyResource { get; set; } = null;

        public decimal MiningLeftOvers { get; set; } = decimal.Zero;

        public string RefiningResource { get; set; } = null;

        public string RefiningResourcePurity { get; set; } = null;

        public string ResearchingBlueprintUUID { get; set; } = null;

        public string ManufacturingBlueprintUUID { get; set; } = null;

        public string ManufacturingCommodityName { get; set; } = null;

        public int ManufacturingQuantity { get; set; } = 0;

        public int ManufacturingCompleted { get; set; } = 0;

        public bool StagingResources { get; set; } = false;

        [JsonIgnore]
        public Dictionary<string, ColonyStructureStatus> Statuses { get; set; } = new Dictionary<string, ColonyStructureStatus>();

        /// <summary>
        /// Per-structure incremental delta for O(1) status recalculation.
        /// Computed by ColonyStatusCalculator.ComputeStructureDelta() during CalculateBuilt().
        /// </summary>
        [JsonIgnore]
        public StructureStatusDelta StatusDelta { get; set; }

        [JsonProperty("colonyBuildingTypeId")]
        public int ColonyBuildingTypeId { get; set; }

        [JsonProperty("resourceId")]
        public int ResourceId { get; set; }

        [JsonProperty("resourceIcon")]
        public string ResourceIcon { get; set; } = string.Empty;

        [JsonProperty("manufactureAmountPerRun")]
        public int ManufactureAmountPerRun { get; set; }

        [JsonProperty("durabilityCurrent")]
        public decimal DurabilityCurrent { get; set; }

        [JsonProperty("durabilityMax")]
        public decimal DurabilityMax { get; set; }

        [JsonProperty("opsStatusEffects")]
        public List<BuildingStatusEffect> OpsStatusEffects { get; set; } = new List<BuildingStatusEffect>();

        [JsonProperty("industries")]
        public List<BuildingIndustry> Industries { get; set; } = new List<BuildingIndustry>();

        [JsonProperty("detailsRequired")]
        public List<BuildingDetailRequirement> DetailsRequired { get; set; } = new List<BuildingDetailRequirement>();

        [JsonProperty("supportDetailsRequired")]
        public List<BuildingDetailRequirement> SupportDetailsRequired { get; set; } = new List<BuildingDetailRequirement>();

        [JsonProperty("buildingAttributes")]
        public List<BuildingAttribute> BuildingAttributes { get; set; } = new List<BuildingAttribute>();

        [JsonProperty("extraProperties")]
        public List<BuildingExtraProperty> ExtraProperties { get; set; } = new List<BuildingExtraProperty>();

        [JsonProperty("currentAttitude")]
        [Obsolete("Migrated to Colony.WorkerCurrentAttitude")]
        public string CurrentAttitude { get; set; } = string.Empty;

        [JsonProperty("contentmentIndex")]
        [Obsolete("Migrated to Colony.ContentmentIndex")]
        public int ContentmentIndex { get; set; }

        public int WageLevel { get; set; }

        /// <summary>
        /// Backward-compat: reads old "gameSequence" JSON key into DisplaySequence.
        /// Write-only; new saves serialize as "displaySequence".
        /// </summary>
        [JsonProperty("gameSequence")]
        private int GameSequenceLegacy { set { DisplaySequence = value; } }

        private DateTime Completion { get; set; }

        private DateTime WageAdjustmentTime { get; set; }

        /// <summary>
        /// Newtonsoft.Json ShouldSerialize convention: suppresses serialization of deprecated CurrentAttitude.
        /// </summary>
        public bool ShouldSerializeCurrentAttitude() => false;

        /// <summary>
        /// Newtonsoft.Json ShouldSerialize convention: suppresses serialization of deprecated ContentmentIndex.
        /// </summary>
        public bool ShouldSerializeContentmentIndex() => false;
    }
}
