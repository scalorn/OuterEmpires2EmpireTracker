using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class Item
    {
        public Item(Models.ItemType.ItemTypeEnum itemType, string name)
        {
            this.ItemType = itemType;
            this.Name = name;
        }

        public Item()
        {
        }

        /// <summary>
        /// Optional display name resolver for items that reference other entities.
        /// Set by the application at startup. Returns display name for a given item type and UUID, or null.
        /// </summary>
        public static Func<ItemType.ItemTypeEnum, string, string> DisplayNameResolver { get; internal set; }

        [JsonIgnore]
        public virtual string ExtendedName
        {
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
                    var resolvedName = DisplayNameResolver?.Invoke(ItemType, BaseItemTypeID);
                    if (resolvedName != null)
                    {
                        extendedName = resolvedName;
                    }
                }

                if (ItemType == Models.ItemType.ItemTypeEnum.Blueprint)
                {
                    var resolvedName = DisplayNameResolver?.Invoke(ItemType, BaseItemTypeID);
                    if (resolvedName != null)
                    {
                        extendedName = resolvedName;
                    }
                }

                return extendedName;
            }
        }

        public string UUID { get; internal set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public Models.ItemType.ItemTypeEnum ItemType { get; internal set; } = Models.ItemType.ItemTypeEnum.None;

        [DefaultValue("")]
        public string BaseItemTypeID { get; internal set; } = string.Empty;

        [Required]
        [DefaultValue("")]
        public virtual string Name { get; internal set; } = string.Empty;

        [Required]
        [DefaultValue("")]
        public virtual string NickName { get; internal set; } = string.Empty;

        [Required]
        [DefaultValue("")]
        public virtual string Description { get; internal set; } = string.Empty;

        // [Required]
        public int Quantity { get; internal set; } = 0;

        [DefaultValue("")]
        public string ResourcePurity { get; internal set; } = string.Empty;

        public decimal Volume { get; internal set; } = 0m;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ItemBag Contents { get; internal set; }

        public int CurrentHP { get; internal set; } = 0;

        public int MaxHP { get; internal set; } = 0;

        public decimal MaxRepairPercent { get; internal set; } = 0m;
    }
}
