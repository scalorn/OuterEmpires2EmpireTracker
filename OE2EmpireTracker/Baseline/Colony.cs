using Newtonsoft.Json;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Data.Item;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace OE2EmpireTracker.Baseline
{
    public class Colony
    {
        public string UUID { get; set; }
        public string PlanetName { get; set; }
        public string ColonyName { get; set; }
        public ItemBag Items { get; set; }

        public List<ColonyStructure> Structures { get; set; }

        public List<CommodityRequested> Commodities { get; set; }
        public OE2EmpireTracker.Data.LockTracking Locks { get; set; }

        public Colony() : base()
        {
            Items = new ItemBag();
            Structures = new List<ColonyStructure>();
            Commodities = new List<CommodityRequested>();
            Locks = new OE2EmpireTracker.Data.LockTracking();
        }

        public void ProcessColony()
        {
            foreach (ColonyStructure structure in Structures)
            {
                if (structure.ProcessCompletionTime != null &&
                    (structure.ProcessCompletionTime.IntervalsPassed > 0 || 
                     (!structure.ProcessCompletionTime.IsRepeating && structure.ProcessCompletionTime.TimeRemaining <= 0)))
                {
                    Blueprint FlatpackBlueprint = PlayerContext.getInstance().findBlueprint(structure.FlatpackBlueprintUUID);
                    if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                    {
                        Survey survey = PlayerContext.getInstance().findSurvey(structure.MiningSurvey);
                        SurveyResource surveyResource = survey.Resources[structure.MiningSurveyResource];
                        List<Item> items = Items.FindResource(surveyResource.Resource, surveyResource.Purity);
                        int quantityInt = 0;
                        Decimal leftOver = structure.MiningLeftOvers;
                        Item item = null;
                        if (items.Count > 0)
                        {
                            item = items[0];
                        }
                        else
                        {
                            item = new Item();
                            item.UUID = Guid.NewGuid().ToString();
                            item.ItemType = ItemType.ItemTypeEnum.Resource;
                            item.BaseItemTypeID = surveyResource.Resource;
                            item.Name = surveyResource.Resource;
                            item.ResourcePurity = surveyResource.Purity;
                            item.Quantity = 0;
                            Items.AddItem(item);
                        }

                        while (structure.ProcessCompletionTime.IntervalsPassed > 0)
                        {
                            Decimal quantity = Decimal.Parse(surveyResource.Amount) + leftOver;

                            /// TODO: FIXME: Need to adjust for extraction bonus.
                            // quantity *= (1 + playerProfile.getExtractionBonus());
                            quantityInt += (int)quantity;

                            leftOver += (quantity - quantityInt);
                            structure.ProcessCompletionTime.ConsumeIntervals(1);
                        }
                        item.Quantity += quantityInt;
                        structure.MiningLeftOvers = leftOver;
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                    {
                        ProcessRefinery(structure);
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    {
                        ProcessResearchLab(structure);
                    }
                }
            }
        }

        private void ProcessRefinery(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.RefiningResource) ||
                string.IsNullOrEmpty(structure.RefiningResourcePurity))
                return;

            int baseRate = 25;
            int outputMultiplier;
            switch (structure.RefiningResourcePurity)
            {
                case "Low": outputMultiplier = 1; break;
                case "Medium": outputMultiplier = 3; break;
                case "High": outputMultiplier = 5; break;
                default: outputMultiplier = 1; break;
            }

            // Find unrefined source in warehouse
            List<Item> sourceItems = Items.FindResource(structure.RefiningResource, structure.RefiningResourcePurity);

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                // Determine how much unrefined resource is available
                int available = 0;
                Item sourceItem = null;
                if (sourceItems.Count > 0)
                {
                    sourceItem = sourceItems[0];
                    available = sourceItem.Quantity;
                }

                int consumed = Math.Min(baseRate, available);
                if (consumed <= 0)
                {
                    structure.ProcessCompletionTime.ConsumeIntervals(1);
                    continue;
                }

                // Consume unrefined
                sourceItem.Quantity -= consumed;

                // Remove from warehouse if fully consumed
                if (sourceItem.Quantity <= 0)
                {
                    Items.Remove(sourceItem.UUID);
                    sourceItems.Remove(sourceItem);
                }

                // Produce refined
                int produced = consumed * outputMultiplier;
                List<Item> refinedItems = Items.FindResource(structure.RefiningResource, "Refined");
                Item refinedItem;
                if (refinedItems.Count > 0)
                {
                    refinedItem = refinedItems[0];
                }
                else
                {
                    refinedItem = new Item();
                    refinedItem.UUID = Guid.NewGuid().ToString();
                    refinedItem.ItemType = ItemType.ItemTypeEnum.Resource;
                    refinedItem.BaseItemTypeID = structure.RefiningResource;
                    refinedItem.Name = structure.RefiningResource;
                    refinedItem.ResourcePurity = "Refined";
                    refinedItem.Volume = 1;
                    refinedItem.Quantity = 0;
                    Items.AddItem(refinedItem);
                }
                refinedItem.Quantity += produced;

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
        }

        private void ProcessResearchLab(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                return;

            PlayerContext pc = PlayerContext.getInstance();
            Blueprint sourceBp = pc.findBlueprint(structure.ResearchingBlueprintUUID);
            if (sourceBp == null)
                return;

            // Research is a one-shot timer — check if time has expired
            if (structure.ProcessCompletionTime.TimeRemaining > 0)
                return;

            // Create the evolved blueprint
            Blueprint newBp = new Blueprint(sourceBp.Name);
            newBp.UUID = System.Guid.NewGuid().ToString();
            newBp.BluePrintType = sourceBp.BluePrintType;
            newBp.Class = sourceBp.Class;
            newBp.TechLevel = sourceBp.TechLevel;
            newBp.Evolution = sourceBp.Evolution + 1;
            newBp.CopyCost = sourceBp.CopyCost;
            newBp.baseBlueprintUUID = sourceBp.baseBlueprintUUID;
            newBp.Description = sourceBp.Description;
            newBp.NickName = ""; // User must set this
            // Properties copied, resources left empty for user to import
            foreach (var prop in sourceBp.Properties.Properties)
            {
                newBp.Properties.setProperty(prop.Key, prop.Value);
            }
            // Resources intentionally empty — user imports via Blueprint Form

            // Add to player's blueprint list
            pc.blueprintList.Add(newBp);

            // Add an item to the colony warehouse
            Item bpItem = new Item(ItemType.ItemTypeEnum.Blueprint, newBp.Name);
            bpItem.UUID = System.Guid.NewGuid().ToString();
            bpItem.BaseItemTypeID = newBp.UUID;
            bpItem.Quantity = 1;
            bpItem.Volume = 0;
            Items.AddItem(bpItem);

            structure.ProcessCompletionTime.ConsumeIntervals(1);
        }
    }
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

    public class CommodityRequested
    {
        public string Name { get; set; }
        public int Requested { get; set; }
        public int Delivered { get; set; }
        public DateTime NeedBy { get; set; }
        public bool Fulfilled { get; set; }
    }
}
