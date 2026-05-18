using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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

        public string UUID { get; internal set; } = null;

        public string FlatpackBlueprintUUID { get; internal set; } = null;

        [JsonProperty("displaySequence")]
        public int DisplaySequence { get; internal set; } = 0;

        [JsonProperty("buildingID")]
        public int BuildingID { get; internal set; } = 0;

        [JsonProperty("buildQueueSequence")]
        public int BuildQueueSequence { get; internal set; } = 0;

        public PropertyBag Properties { get; internal set; }

        public PropertyBag AssignedWorkers { get; internal set; }

        public CountDownTime BuildCompletionTime { get; internal set; } = null;

        public CountDownTime ProcessCompletionTime { get; internal set; } = null;

        public string MiningSurvey { get; internal set; } = null;

        public string MiningSurveyResource { get; internal set; } = null;

        public decimal MiningLeftOvers { get; internal set; } = decimal.Zero;

        public string RefiningResource { get; internal set; } = null;

        public string RefiningResourcePurity { get; internal set; } = null;

        public string ResearchingBlueprintUUID { get; internal set; } = null;

        public string ManufacturingBlueprintUUID { get; internal set; } = null;

        public string ManufacturingCommodityName { get; internal set; } = null;

        public int ManufacturingQuantity { get; internal set; } = 0;

        public int ManufacturingCompleted { get; internal set; } = 0;

        public bool StagingResources { get; internal set; } = false;

        [JsonIgnore]
        public Dictionary<string, ColonyStructureStatus> Statuses { get; internal set; } = new Dictionary<string, ColonyStructureStatus>();

        /// <summary>
        /// Per-structure incremental delta for O(1) status recalculation.
        /// Computed by ColonyStatusCalculator.ComputeStructureDelta() during CalculateBuilt().
        /// </summary>
        [JsonIgnore]
        public StructureStatusDelta StatusDelta { get; internal set; }

        public string CurrentAttitude { get; internal set; } = string.Empty;

        public int ContentmentIndex { get; internal set; }

        public int WageLevel { get; internal set; }

        /// <summary>
        /// Backward-compat: reads old "gameSequence" JSON key into DisplaySequence.
        /// Write-only; new saves serialize as "displaySequence".
        /// </summary>
        [JsonProperty("gameSequence")]
        private int GameSequenceLegacy { set { DisplaySequence = value; } }

        private DateTime Completion { get; set; }

        private DateTime WageAdjustmentTime { get; set; }
    }
}
