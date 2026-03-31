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

        public Colony() : base()
        {
            Items = new ItemBag();
            Structures = new List<ColonyStructure>();
            Commodities = new List<CommodityRequested>();
        }

        public void ProcessColony()
        {
            foreach (ColonyStructure structure in Structures)
            {
                if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IntervalsPassed > 0)
                {
                    Blueprint FlatpackBlueprint = PlayerContext.getInstance().findBlueprint(structure.FlatpackBlueprintUUID);
                    if (FlatpackBlueprint.BluePrintType == "Flatpacks/MiningRig")
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
                }
            }
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

        public Dictionary<string, ColonyStructureStatus> Statuses { get; set; } = new Dictionary<string, ColonyStructureStatus>();

        public double Power { get; set; }
        public double Habitation { get; set; }
        public double Food { get; set; }
        public double Entertainment { get; set; }
        public double WarehouseCapacity { get; set; }
        public int WorkersAssigned { get; set; }

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
        DateTime NeedBy { get; set; }
        bool Fulfilled { get; set; }
    }
}
