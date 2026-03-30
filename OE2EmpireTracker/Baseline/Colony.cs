using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Data.Item;

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
    }

    public class ColonyStructure
    {
        public string UUID { get; set; }
        public string FlatpackBlueprintUUID { get; set; }
        public int gameSequence { get; set; }
        public int buildQueueSequence { get; set; }
        public PropertyBag Properties { get; set; }
        public PropertyBag AssignedWorkers { get; set; }
        public CountDownTime CompletionTime { get; set; }

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
