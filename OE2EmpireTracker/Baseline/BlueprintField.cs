using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Baseline
{
    public class BlueprintField
    {
        public enum BlueprintFieldEnum
        {
            None,
            FuelUsed,
            MaxJumpDistance,
        }

        public BlueprintFieldEnum Id { get; set; }
        public string Name { get; set; }
    }
}
