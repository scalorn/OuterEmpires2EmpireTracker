using Newtonsoft.Json;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Baseline
{
    public class ColonyStructure
    {
        public string UUID { get; set; } = null;
        public string FlatpackBlueprintUUID { get; set; } = null;
        public int gameSequence { get; set; } = 0;
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
    }
}
