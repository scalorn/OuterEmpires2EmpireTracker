using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class ColonyReferenceCounter
    {
        private readonly IEnumerable<DeliveryRoute> _routes;
        private readonly IEnumerable<DeliveryPlan> _plans;

        public ColonyReferenceCounter(
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<DeliveryPlan> plans)
        {
            _routes = routes ?? Enumerable.Empty<DeliveryRoute>();
            _plans = plans ?? Enumerable.Empty<DeliveryPlan>();
        }

        public ColonyReferenceReport CountReferences(string colonyUUID)
        {
            if (string.IsNullOrEmpty(colonyUUID))
                return ColonyReferenceReport.Empty;

            int routeCount = _routes
                .Count(r => r.Stops != null && r.Stops.Any(s => s.ColonyUUID == colonyUUID));

            int planCount = _plans
                .Count(p => p.Stops != null && p.Stops.Any(s => s.ColonyUUID == colonyUUID));

            return new ColonyReferenceReport(routeCount, planCount);
        }
    }

    public class ColonyReferenceReport
    {
        public int TotalCount { get; }
        public int RouteCount { get; }
        public int PlanCount { get; }

        public ColonyReferenceReport(int routeCount, int planCount)
        {
            RouteCount = routeCount;
            PlanCount = planCount;
            TotalCount = routeCount + planCount;
        }

        public static readonly ColonyReferenceReport Empty = new ColonyReferenceReport(0, 0);
    }
}
