using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Models
{
    public class ColonyStructure
    {
        public string UUID { get; set; } = null;
        public string FlatpackBlueprintUUID { get; set; } = null;
        [JsonProperty("displaySequence")]
        public int DisplaySequence { get; set; } = 0;

        /// <summary>
        /// Backward-compat: reads old "gameSequence" JSON key into DisplaySequence.
        /// Write-only; new saves serialize as "displaySequence".
        /// </summary>
        [JsonProperty("gameSequence")]
        private int GameSequenceLegacy { set { DisplaySequence = value; } }
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

        DateTime Completion { get; set; }
        public string CurrentAttitude { get; set; } = string.Empty;
        public int ContentmentIndex { get; set; }

        public int WageLevel { get; set; }
        DateTime WageAdjustmentTime { get; set; }

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
    }
}
