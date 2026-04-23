using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Models
{
    public class Blueprint : Item
    {
        public string OwnerUUID { get; set; } = string.Empty;

        public string BaseBlueprintUUID { get; set; }

        [DefaultValue(null)]
        public string LegacyUUID { get; set; }

        public string BluePrintType { get; set; }

        public int Evolution { get; set; }

        public string TechLevel { get; set; }

        public int Class { get; set; }

        public int CopyCost { get; set; }

        public PropertyBag Properties { get; set; }
        public Dictionary<string, string> Resources { get; set; }

        [JsonIgnore]
        public string OutputItemName
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                    return string.Empty;

                if (!string.IsNullOrEmpty(BluePrintType) &&
                    BluePrintType.IsFlatpack() &&
                    Name.EndsWith(" Flatpack", StringComparison.OrdinalIgnoreCase))
                {
                    return Name.Substring(0, Name.Length - " Flatpack".Length);
                }

                return Name;
            }
        }

        [JsonIgnore]
        public override string ExtendedName
        {
            get
            {
                if (UUID == null)
                {
                    return string.Empty;
                }

                string extendedName = string.Empty;
                if (Class > 0)
                {
                    extendedName += $"C{Class} ";
                }

                if (Evolution > 0)
                {
                    extendedName += "Ev(" + Evolution + ") ";
                }

                extendedName += Name + " ";
                if (TechLevel != null)
                {
                    extendedName += "(" + TechLevel + ") ";
                }

                if (!string.IsNullOrEmpty(NickName))
                {
                    extendedName += "[" + NickName + "] ";
                }

                extendedName = extendedName.Trim();
                return extendedName;
            }
        }

        public Blueprint(string name /*, int quantity*/) : base(Models.ItemType.ItemTypeEnum.Blueprint, name /* , quantity */)
        {
            Properties = new PropertyBag();
            Resources = new Dictionary<string, string>();
        }

        public Blueprint() : base(Models.ItemType.ItemTypeEnum.Blueprint, string.Empty)
        {
            Properties = new PropertyBag();
            Resources = new Dictionary<string, string>();
        }
    }
}
