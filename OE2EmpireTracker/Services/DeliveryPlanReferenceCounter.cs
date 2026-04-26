using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a delivery plan from build plans (BuildPlan.DeliveryPlanUUID).
    /// </summary>
    public class DeliveryPlanReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, int> _buildPlanMap;

        public DeliveryPlanReferenceCounter(IEnumerable<BuildPlan> buildPlans)
        {
            var list = buildPlans ?? Enumerable.Empty<BuildPlan>();

            _buildPlanMap = new Dictionary<string, int>();
            foreach (var bp in list)
            {
                if (!string.IsNullOrEmpty(bp.DeliveryPlanUUID))
                {
                    _buildPlanMap.TryGetValue(bp.DeliveryPlanUUID, out int c);
                    _buildPlanMap[bp.DeliveryPlanUUID] = c + 1;
                }
            }
        }

        /// <summary>
        /// Returns the number of build plans that reference the given delivery plan UUID.
        /// </summary>
        public int CountReferences(string deliveryPlanUUID)
        {
            if (string.IsNullOrEmpty(deliveryPlanUUID))
                return 0;

            _buildPlanMap.TryGetValue(deliveryPlanUUID, out int count);
            return count;
        }
    }
}