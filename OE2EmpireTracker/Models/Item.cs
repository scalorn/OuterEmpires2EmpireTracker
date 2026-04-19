using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class Item
    {
        public string UUID { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public Models.ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
        [DefaultValue("")]
        public string BaseItemTypeID { get; set; } = string.Empty;

        [Required]
        [DefaultValue("")]
        public virtual string Name { get; set; }= string.Empty;

        [JsonIgnore]
        public virtual string ExtendedName { 
            get 
            {
                string extendedName = Name;
                if (ItemType == Models.ItemType.ItemTypeEnum.Resource)
                {
                    if (!string.IsNullOrEmpty(ResourcePurity))
                    {
                        extendedName += $" ({ResourcePurity})";
                    }
                }
                if (ItemType == Models.ItemType.ItemTypeEnum.Commodity)
                {
                    Commodity.ResourceMapByEnum.TryGetValue(BaseItemTypeID, out Commodity commodity);
                    if (commodity != null)
                    {
                        extendedName = commodity.ExtendedName;
                    }
                }
                if (ItemType == Models.ItemType.ItemTypeEnum.Survey)
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
                if (ItemType == Models.ItemType.ItemTypeEnum.Blueprint)
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
        [DefaultValue("")]
        public virtual string NickName { get; set; }= string.Empty;
        [Required]
        [DefaultValue("")]
        public virtual string Description { get; set; } = string.Empty;
        //[Required]
        public int Quantity { get; set; } = 0;
        [DefaultValue("")]
        public string ResourcePurity { get; set; } = string.Empty;
        public decimal Volume { get; set; } = 0m;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ItemBag Contents { get; set; }

        public int CurrentHP { get; set; } = 0;
        public int MaxHP { get; set; } = 0;
        public decimal MaxRepairPercent { get; set; } = 0m;

        public Item(Models.ItemType.ItemTypeEnum itemType, string name)
        {
            this.ItemType = itemType;
            this.Name = name;
        }

        public Item()
        {
        }
    }
}
