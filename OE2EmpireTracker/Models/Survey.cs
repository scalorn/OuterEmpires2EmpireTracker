using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class Survey : Item
    {
        public Survey(string name /*, int quantity*/) : base(Models.ItemType.ItemTypeEnum.Survey, name /* , quantity */)
        {
            Properties = new Dictionary<string, string>();
            Resources = new Dictionary<string, SurveyResource>();
        }

        public Survey() : base()
        {
            ItemType = Models.ItemType.ItemTypeEnum.Survey;
            Properties = new Dictionary<string, string>();
            Resources = new Dictionary<string, SurveyResource>();
        }

        [JsonIgnore]
        public override string ExtendedName
        {
            get
            {
                string extendedName = string.Empty;
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

        public string OwnerUUID { get; set; } = string.Empty;

        public string ScannedBy { get; set; }

        public string DateTime { get; set; }

        public string PlanetName { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public string SurveyID { get; set; }

        public string ScannerBlueprintUUID { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public Dictionary<string, SurveyResource> Resources { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(SurveyType.Planet)]
        public SurveyType SurveyType { get; set; } = SurveyType.Planet;

        public string AsteroidUUID { get; set; } = string.Empty;

        [JsonIgnore]
        public Dictionary<string, int> ParsedMaxReserves { get; set; }
    }

    public class SurveyResource
    {
        public SurveyResource()
        {
        }

        public SurveyResource(string resource, string purity, string amount)
        {
            Resource = resource;
            Purity = purity;
            Amount = amount;
        }

        [JsonIgnore]
        public string ExtendedName
        {
            get
            {
                string extendedName = Resource + (!string.IsNullOrEmpty(Purity) ? $" ({Purity})" : string.Empty) + (!string.IsNullOrEmpty(Amount) ? $" ({Amount})/h" : string.Empty);
                return extendedName;
            }
        }

        public string Resource { get; set; } = string.Empty;

        public string Purity { get; set; } = string.Empty;

        public string Amount { get; set; } = string.Empty;
    }
}
