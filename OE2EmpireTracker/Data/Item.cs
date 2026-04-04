using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using OE2EmpireTracker.Baseline;

namespace OE2EmpireTracker.Data
{
    public class Item
    {
        public string UUID { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Data.ItemType.ItemTypeEnum.None;
        public string BaseItemTypeID { get; set; } = string.Empty;

        [Required]
        public virtual string Name { get; set; }= string.Empty;

        [JsonIgnore]
        public virtual string ExtendedName { 
            get 
            {
                string extendedName = Name;
                if (ItemType == Data.ItemType.ItemTypeEnum.Resource)
                {
                    if (!string.IsNullOrEmpty(ResourcePurity))
                    {
                        extendedName += $" ({ResourcePurity})";
                    }
                }
                if (ItemType == Data.ItemType.ItemTypeEnum.Commodity)
                {
                    Commodity.ResourceMapByEnum.TryGetValue(BaseItemTypeID, out Commodity commodity);
                    if (commodity != null)
                    {
                        extendedName = commodity.ExtendedName;
                    }
                }
                if (ItemType == Data.ItemType.ItemTypeEnum.Survey)
                {
                    Survey survey = EmpireContext.PlayerContext?.FindSurvey(BaseItemTypeID);
                    if (survey != null)
                    {
                        extendedName = $"{survey.PlanetName} ({survey.SurveyID})";
                        if (!string.IsNullOrEmpty(survey.NickName))
                        {
                            extendedName += $" [{survey.NickName}]";
                        }
                    }
                }
                if (ItemType == Data.ItemType.ItemTypeEnum.Blueprint)
                {
                    Blueprint blueprint = EmpireContext.PlayerContext?.FindBlueprint(BaseItemTypeID);
                    if (blueprint != null)
                    {
                        extendedName = "";
                        if (blueprint.Class > 0)
                        {
                            extendedName += $"C{blueprint.Class} ";
                        }
                        if (blueprint.Evolution > 0)
                        {
                            extendedName += $"(Ev{blueprint.Evolution}) ";
                        }
                        extendedName += blueprint.Name + " ";
                        if (!string.IsNullOrEmpty(blueprint.TechLevel))
                        {
                            extendedName += $"({blueprint.TechLevel}) ";
                        }
                        if (!string.IsNullOrEmpty(blueprint.NickName))
                        {
                            extendedName += $"[{blueprint.NickName}] ";
                        }
                        extendedName = extendedName.Trim();
                    }
                }
                return extendedName;
            }
        }

        [Required]
        public virtual string NickName { get; set; }= string.Empty;
        [Required]
        public virtual string Description { get; set; } = string.Empty;
        //[Required]
        public int Quantity { get; set; } = 0;
        public string ResourcePurity { get; set; } = string.Empty;
        public double Volume { get; set; } = 0;
        
        public Item(ItemType.ItemTypeEnum itemType, string name)
        {
            this.ItemType = itemType;
            this.Name = name;
        }

        public Item()
        {
        }
    }
}
