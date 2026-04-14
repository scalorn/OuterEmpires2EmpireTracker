using Newtonsoft.Json;
using OE2EmpireTracker.Constants;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class ColonyStructure
    {
        public string UUID { get; set; } = null;
        public string FlatpackBlueprintUUID { get; set; } = null;
        public int displaySequence { get; set; } = 0;

        /// <summary>
        /// Backward-compat: reads old "gameSequence" JSON key into displaySequence.
        /// Write-only; new saves serialize as "displaySequence".
        /// </summary>
        [JsonProperty("gameSequence")]
        private int gameSequenceLegacy { set { displaySequence = value; } }
        public int buildingID { get; set; } = 0;
        public int buildQueueSequence { get; set; } = 0;
        public PropertyBag Properties { get; set; }
        public PropertyBag AssignedWorkers { get; set; }
        public CountDownTime BuildCompletionTime { get; set; } = null;
        public CountDownTime ProcessCompletionTime { get; set; } = null;

        public string MiningSurvey { get; set; } = null;
        public string MiningSurveyResource { get; set; } = null;
        public Decimal MiningLeftOvers { get; set; } = Decimal.Zero;

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

        DateTime completion { get; set; }
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
                Properties.getBoolean(GameConstants.PropBuilt, false, out built);
                if (!built) return false;
                bool online;
                Properties.getBoolean(GameConstants.PropOnline, false, out online);
                return online;
            }
        }
    }
}
