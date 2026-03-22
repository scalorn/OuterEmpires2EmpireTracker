using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OE2EmpireTracker.Data;

namespace OE2EmpireTracker.Baseline
{
    public class Survey : Item
    {
        public string ScannedBy { get; set; }
        public string DateTime { get; set; }
        public string PlanetName { get; set; }
        public string SurveyID { get; set; }
        public string ScannerBlueprintUUID { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, SurveyResource> Resources { get; set; }
        public Survey(string name /*, int quantity*/) : base(Data.ItemType.ItemTypeEnum.Survey, name /* , quantity */)
        {
            Properties = new Dictionary<string, string>();
            Resources = new Dictionary<string, SurveyResource>();
        }

        public Survey() : base()
        {
            ItemType = Data.ItemType.ItemTypeEnum.Survey;
            Properties = new Dictionary<string, string>();
            Resources = new Dictionary<string, SurveyResource>();
        }
    }
    public class SurveyResource
    {
        public string Purity { get; set; }
        public string Amount { get; set; }
    }
}
