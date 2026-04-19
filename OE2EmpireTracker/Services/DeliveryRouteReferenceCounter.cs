using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a delivery route across delivery plans.
    /// Will be expanded to include WarehouseOverflowRules and SupplyChainStages
    /// when those models are created in Iteration 1.
    /// </summary>
    public class DeliveryRouteReferenceCounter
    {
        private readonly IEnumerable<DeliveryPlan> _plans;

        public DeliveryRouteReferenceCounter(IEnumerable<DeliveryPlan> plans)
        {
            _plans = plans ?? Enumerable.Empty<DeliveryPlan>();
        }

        /// <summary>
        /// Counts how many entities reference the given route UUID.
        /// </summary>
        public DeliveryRouteReferenceReport CountReferences(string routeUUID)
        {
            if (string.IsNullOrEmpty(routeUUID))
                return DeliveryRouteReferenceReport.Empty;

            int planCount = _plans
                .Count(p => p.RouteUUID == routeUUID);

            return new DeliveryRouteReferenceReport(planCount);
        }
    }

    /// <summary>
    /// Immutable report of references to a delivery route.
    /// </summary>
    public class DeliveryRouteReferenceReport
    {
        public int TotalCount { get; }
        public int DeliveryPlanCount { get; }

        public DeliveryRouteReferenceReport(int deliveryPlanCount)
        {
            DeliveryPlanCount = deliveryPlanCount;
            TotalCount = deliveryPlanCount;
        }

        public static readonly DeliveryRouteReferenceReport Empty = new DeliveryRouteReferenceReport(0);
    }
}
