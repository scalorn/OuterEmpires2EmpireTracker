using OE2EmpireTracker.Baseline;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OE2EmpireTracker.Data
{
    public class Resource : Item
    {
        [Required]
        public ResourcePurity.PurityEnum Purity { get; set; }

        [ForeignKey("BaseResource")]
        [Required]
        public int BaseResourceID { get; set; }
        public virtual BaseResource BaseResource { get; set; }

        public override string Name
        {
            get
            {
                if (BaseResource != null)
                {
                    if (Purity != ResourcePurity.PurityEnum.None && Purity != ResourcePurity.PurityEnum.Refined)
                    {
                        base.Name = BaseResource.Name + " (Unrefined, " + Purity.ToString() + ")";
                    }
                    else
                    {
                        base.Name = BaseResource.Name;
                    }
                }
                else
                {
                    base.Name = "Unknown";
                }
                return base.Name;
            }
            set { }
        }


        public Resource(BaseResource baseResource, ResourcePurity.PurityEnum purity /*, int quantity*/) : base(Data.ItemType.ItemTypeEnum.Resource, "" /*, quantity */)
        {
            this.Purity = purity;
            this.BaseResource = BaseResource;
        }

        public Resource()
        {

        }
    }
}
