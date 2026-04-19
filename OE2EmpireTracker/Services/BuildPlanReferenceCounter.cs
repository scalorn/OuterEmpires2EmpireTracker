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
        private readonly Dictionary<string, int> _stockPlanMap;

        public BuildPlanReferenceCounter(IEnumerable<StockPlan> stockPlans)
        {
            var list = stockPlans ?? Enumerable.Empty<StockPlan>();

            _stockPlanMap = new Dictionary<string, int>();
            foreach (var sp in list)
            {
                if (!string.IsNullOrEmpty(sp.ReplenishmentBuildPlanUUID))
                    _stockPlanMap[sp.ReplenishmentBuildPlanUUID] = _stockPlanMap.GetValueOrDefault(sp.ReplenishmentBuildPlanUUID) + 1;
            }
        }

        /// <summary>
        /// Returns the number of StockPlans that reference the given build plan UUID.
        /// </summary>
        public int CountReferences(string buildPlanUUID)
        {
            if (string.IsNullOrEmpty(buildPlanUUID))
                return 0;

            _stockPlanMap.TryGetValue(buildPlanUUID, out int count);
            return count;
        }
    }
}
