using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a Ship from DeliveryPlans and BuildItems.
    /// Used to prevent deletion of ships that are still in use.
    /// </summary>
    public class ShipReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _deliveryPlanMap;
        private readonly Dictionary<string, int> _buildItemMap;

        public ShipReferenceCounter(
            IEnumerable<DeliveryPlan> deliveryPlans,
            IEnumerable<BuildPlan> buildPlans)
        {
            var deliveryPlanList = deliveryPlans ?? Enumerable.Empty<DeliveryPlan>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();

            _deliveryPlanMap = new Dictionary<string, int>();
            foreach (var plan in deliveryPlanList)
            {
                if (!string.IsNullOrEmpty(plan.ShipUUID))
                {
                    _deliveryPlanMap.TryGetValue(plan.ShipUUID, out int c);
                    _deliveryPlanMap[plan.ShipUUID] = c + 1;
                }
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var bp in buildPlanList)
            {
                if (bp.Items == null) continue;
                foreach (var item in bp.Items)
                {
                    if (item.BuildLocationType == DestinationType.Ship
                        && !string.IsNullOrEmpty(item.BuildLocationUUID))
                    {
                        _buildItemMap.TryGetValue(item.BuildLocationUUID, out int c);
                        _buildItemMap[item.BuildLocationUUID] = c + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of entities referencing the given ship UUID.
        /// Counts DeliveryPlan.ShipUUID and BuildItem.BuildLocationUUID (when Ship) matches.
        /// </summary>
        public int CountReferences(string shipUUID)
        {
            if (string.IsNullOrEmpty(shipUUID))
                return 0;

            _deliveryPlanMap.TryGetValue(shipUUID, out int deliveryPlanCount);
            _buildItemMap.TryGetValue(shipUUID, out int buildItemCount);

            return deliveryPlanCount + buildItemCount;
        }
    }
}