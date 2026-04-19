using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class ColonyReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _routeMap;
        private readonly Dictionary<string, int> _planMap;
        private readonly Dictionary<string, int> _buildItemMap;

        public ColonyReferenceCounter(
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<DeliveryPlan> plans,
            IEnumerable<BuildPlan> buildPlans = null)
        {
            var routeList = routes ?? Enumerable.Empty<DeliveryRoute>();
            var planList = plans ?? Enumerable.Empty<DeliveryPlan>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();

            _routeMap = new Dictionary<string, int>();
            foreach (var route in routeList)
            {
                if (route.Stops == null) continue;
                var colonyUUIDs = new HashSet<string>();
                foreach (var stop in route.Stops)
                {
                    if (!string.IsNullOrEmpty(stop.ColonyUUID))
                        colonyUUIDs.Add(stop.ColonyUUID);
                }
                foreach (var uuid in colonyUUIDs)
                    _routeMap[uuid] = _routeMap.GetValueOrDefault(uuid) + 1;
            }

            _planMap = new Dictionary<string, int>();
            foreach (var plan in planList)
            {
                if (plan.Stops == null) continue;
                var colonyUUIDs = new HashSet<string>();
                foreach (var stop in plan.Stops)
                {
                    if (!string.IsNullOrEmpty(stop.ColonyUUID))
                        colonyUUIDs.Add(stop.ColonyUUID);
                }
                foreach (var uuid in colonyUUIDs)
                    _planMap[uuid] = _planMap.GetValueOrDefault(uuid) + 1;
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var bp in buildPlanList)
            {
                if (bp.Items == null) continue;
                foreach (var item in bp.Items)
                {
                    if (item.BuildLocationType == DestinationType.Colony
                        && !string.IsNullOrEmpty(item.BuildLocationUUID))
                        _buildItemMap[item.BuildLocationUUID] = _buildItemMap.GetValueOrDefault(item.BuildLocationUUID) + 1;
                }
            }
        }

        public ColonyReferenceReport CountReferences(string colonyUUID)
        {
            if (string.IsNullOrEmpty(colonyUUID))
                return ColonyReferenceReport.Empty;

            _routeMap.TryGetValue(colonyUUID, out int routeCount);
            _planMap.TryGetValue(colonyUUID, out int planCount);
            _buildItemMap.TryGetValue(colonyUUID, out int buildItemCount);

            return new ColonyReferenceReport(routeCount, planCount, buildItemCount);
        }
    }

    public class ColonyReferenceReport
    {
        public int TotalCount { get; }
        public int RouteCount { get; }
        public int PlanCount { get; }
        public int BuildItemCount { get; }

        public ColonyReferenceReport(int routeCount, int planCount, int buildItemCount = 0)
        {
            RouteCount = routeCount;
            PlanCount = planCount;
            BuildItemCount = buildItemCount;
            TotalCount = routeCount + planCount + buildItemCount;
        }

        public static readonly ColonyReferenceReport Empty = new ColonyReferenceReport(0, 0, 0);
    }
}
