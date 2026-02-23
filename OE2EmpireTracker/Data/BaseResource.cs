using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Data.Item;

namespace OE2EmpireTracker.Data
{
    public class BaseResource
    {
        public enum ResourceClassEnum
        {
            None,
            CommonElements,
            UncommonElements,
            RareElements,
            VeryRareElements,
            SyntheticElements,
        }
        public enum PurityEnum
        {
            None,
            Refined,
            UnrefinedLow,
            UnrefinedMedium,
            UnrefinedHigh,
        }

        [Key]
        [Required]
        public int ID { get; set; }

        public string UUID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool Mineable { get; set; }

        [Required]
        public ResourceClassEnum ResourceClass { get; set; }

        public BaseResource(string name, bool mineable, ResourceClassEnum resourceClass)
        {
            this.Name = name;
            this.Mineable = mineable;
            this.ResourceClass = resourceClass;
        }

        public BaseResource()
        {
        }
    }
}
