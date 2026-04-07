using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class SubResource
    {
        [Required]
        public int SortOrder { get; set; }

        [ForeignKey("Parent")]
        [Key, Column(Order = 0)]
        [Required]
        public int ParentID { get; set; }
        public virtual Item Parent { get; set; }

        [ForeignKey("Child")]
        [Key, Column(Order = 1)]
        [Required]
        public int ChildID { get; set; }
        public virtual Item Child { get; set; }

        public SubResource(int sortOrder, Item parent, Item child)
        {
            this.SortOrder = sortOrder;
            this.Parent = parent;
            this.Child = child;
        }
    }
}
