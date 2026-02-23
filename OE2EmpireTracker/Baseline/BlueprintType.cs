using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Baseline.BlueprintField;

namespace OE2EmpireTracker.Baseline
{
    public class BlueprintType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<BlueprintFieldEnum> Fields { get; set; }
        public Boolean Universal { get; set; }
    }
}
