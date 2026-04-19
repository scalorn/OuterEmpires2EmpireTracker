using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts how many StockPlans reference a given BuildPlan via ReplenishmentBuildPlanUUID.
    /// </summary>
    public class BuildPlanReferenceCounter
    {
        private readonly IEnumerable<StockPlan> _stockPlans;

        public BuildPlanReferenceCounter(IEnumerable<StockPlan> stockPlans)
        {
            _stockPlans = stockPlans ?? Enumerable.Empty<StockPlan>();
        }

        /// <summary>
        /// Returns the number of StockPlans that reference the given build plan UUID.
        /// </summary>
        public int CountReferences(string buildPlanUUID)
        {
            if (string.IsNullOrEmpty(buildPlanUUID))
                return 0;

            return _stockPlans
                .Count(sp => sp.ReplenishmentBuildPlanUUID == buildPlanUUID);
        }
    }
}
