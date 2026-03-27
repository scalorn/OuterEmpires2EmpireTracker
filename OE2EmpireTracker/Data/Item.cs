using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class Item
    {
        public string UUID { get; set; }

        [Required]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Data.ItemType.ItemTypeEnum.None;
        public string BaseItemTypeID { get; set; } = string.Empty;

        [Required]
        public virtual string Name { get; set; }= string.Empty;

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
