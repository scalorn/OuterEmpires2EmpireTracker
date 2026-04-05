using Newtonsoft.Json;
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
        public string OwnerUUID { get; set; } = string.Empty;
        public string ScannedBy { get; set; }
        public string DateTime { get; set; }
        public string PlanetName { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public string SurveyID { get; set; }
        public string ScannerBlueprintUUID { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, SurveyResource> Resources { get; set; }

        [JsonIgnore]
        public override string ExtendedName
        {
            get
            {
                string extendedName = "";
                if (!string.IsNullOrEmpty(PlanetName))
                {
                    extendedName += $"{PlanetName}";
                }
                if (!string.IsNullOrEmpty(SurveyID))
                {
                    extendedName += $" ({SurveyID})";
                }
                if (!string.IsNullOrEmpty(NickName))
                {
                    extendedName += $" [{NickName}]";
                }
                return extendedName.Trim();
            }
        }


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
        public string Resource { get; set; } = string.Empty;
        public string Purity { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;

        [JsonIgnore]
        public string ExtendedName
        {
            get
            {
                string extendedName = Resource + (!string.IsNullOrEmpty(Purity) ? $" ({Purity})" : "") + (!string.IsNullOrEmpty(Amount) ? $" ({Amount})/h" : "");
                return extendedName;
            }
        }
        public SurveyResource()
        {

        }

        public SurveyResource(string resource, string purity, string amount)
        {
            Resource = resource;
            Purity = purity;
            Amount = amount;
        }
    }
}
