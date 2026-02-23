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
        public enum ItemTypeEnum
        {
            None,
            SubResource,
            Commodity,
            ShipHull,
            ShipPart,
            ShipArmament,
            Munition,
            WorkDetail,
            Resource,
            Blueprint,
            Flatpack,
            Scan,
            SpaceBuildPackage,
            Share
        }

        [Key]
        [Required]
        public virtual int ID { get; set; }

        public string UUID { get; set; }

        [Required]
        public ItemTypeEnum ItemType { get; set; }

        [Required]
        public virtual string Name { get; set; }

        [Required]
        public int Quantity { get; set; }
        
        public List<SubResource> SubResources { get; set; }
        public List<ItemProperty> ItemProperties { get; set; }

        public Item(ItemTypeEnum itemType, string name, int quantity)
        {
            this.ItemType = itemType;
            this.Name = name;
            this.Quantity = quantity;
            this.SubResources = new List<SubResource>();
            this.ItemProperties = new List<ItemProperty>();
        }

        public Item()
        {
            this.SubResources = new List<SubResource>();
            this.ItemProperties = new List<ItemProperty>();
        }
    }
}
