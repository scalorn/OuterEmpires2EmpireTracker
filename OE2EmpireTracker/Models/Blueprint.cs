using Newtonsoft.Json;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OE2EmpireTracker.Models
{
    public class Blueprint : Item
    {
        public string OwnerUUID { get; set; } = string.Empty;

        //[NotMapped]
        public string baseBlueprintUUID { get; set; }

        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private string _bluePrintType;
        public string BluePrintType
        {
            get => _bluePrintType;
            set
            {
                if (_bluePrintType != value)
                {
                    Log.Info($"BluePrintType changing from \"{_bluePrintType}\" to \"{value}\" on \"{Name}\" (UUID={UUID})\n{System.Environment.StackTrace}");
                }
                _bluePrintType = value;
            }
        }

        //[NotMapped]
        public int Evolution { get; set; }

        //[NotMapped]
        public string TechLevel { get; set; }

        //[NotMapped]
        //public int ManufactureRunTime { get; set; }

        //[NotMapped]
        public int Class { get; set; }

        public int CopyCost { get; set; }

        //[NotMapped]
        //public int MaxAllowedOnShip { get; set; }

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
        public override string ExtendedName {
            get {
                if (UUID == null)
                {
                    return string.Empty;
                }
                string extendedName = "";
                if (Class > 0)
                {
                    extendedName += $"C{Class} ";
                }
                if (Evolution > 0) {
                    extendedName += "Ev(" + Evolution + ") ";
                }
                extendedName += OutputItemName + " ";
                if (TechLevel != null)
                {
                    extendedName += "(" + TechLevel + ") ";
                }
                if (!String.IsNullOrEmpty(NickName))
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

        public Blueprint() : base(Models.ItemType.ItemTypeEnum.Blueprint, "")
        {
            Properties = new PropertyBag();
            Resources = new Dictionary<string, string>();
        }

    }
}
