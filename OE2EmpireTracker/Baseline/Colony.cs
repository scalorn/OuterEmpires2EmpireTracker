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
        public string Planet { get; set; }
        public Dictionary<string, OE2EmpireTracker.Data.Item> Items { get; set; }

        public List<ColonyStructure> Structures { get; set; }

        public Colony() : base()
        {
            Items = new Dictionary<string, OE2EmpireTracker.Data.Item>();
            Structures = new List<ColonyStructure>();
        }
    }

    public class ColonyStructure
    {
        public string FlatpackBlueprintUUID { get; set; }
        public int sequence { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> AssignedWorkers { get; set; }

        public double Power { get; set; }
        public double Habitation { get; set; }
        public double Food { get; set; }
        public double Entertainment { get; set; }
        public double WarehouseCapacity { get; set; }
        public int WorkersAssigned {  get; set; }

        bool Online { get; set; }
        bool Building { get; set; }

        DateTime completion { get; set; }
        public string CurrentAttitude { get; set; }
        public int ContentmentIndex { get; set; }

        public int WageLevel { get; set}
        DateTime WageAdjustmentTime { get; set; }



        public ColonyStructure() : base()
        {
            Properties = new Dictionary<string, string>();
        }

    }

    public class CommodityRequested
    {
        public string Name { get; set}
        public int Requested { get; set}
        public int Delivered { get; set}
        DateTime NeedBy { get; set; }
        bool Fulfilled { get; set; }
    }
}
