using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class ItemProperty
    {
        public enum ItemPropertyTypeEnum
        {
            None,
        }

        [Key, Column(Order = 1)]
        [Required]
        public ItemPropertyTypeEnum ItemPropertyType { get; set; }

        [Key, Column(Order = 0)]
        [ForeignKey("Parent")]
        [Required]
        public int ParentID { get; set; }
        public virtual Item Parent { get; set; }

        public decimal BaseValue { get; set; }
        public decimal AdjustedValue { get; set; }

        public ItemProperty(ItemPropertyTypeEnum itemPropertyType, decimal baseValue, decimal adjustedValue = 0.0m)
        {
            this.ItemPropertyType = itemPropertyType;
            this.BaseValue = baseValue;
            this.AdjustedValue = adjustedValue;
        }

        public ItemProperty()
        {
        }
    }
}
