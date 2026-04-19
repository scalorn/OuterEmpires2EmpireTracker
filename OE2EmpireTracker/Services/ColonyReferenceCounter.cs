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
        private readonly Dictionary<string, int> _supplyChainMap;
        private readonly Dictionary<string, int> _overflowMap;

        public ColonyReferenceCounter(
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<DeliveryPlan> plans,
            IEnumerable<BuildPlan> buildPlans = null,
            IEnumerable<SupplyChain> supplyChains = null,
            IEnumerable<WarehouseOverflowRule> overflowRules = null)
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
                    _routeMap[uuid] = (_routeMap.TryGetValue(uuid, out int _tmp) ? _tmp : 0) + 1;
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
                    _planMap[uuid] = (_planMap.TryGetValue(uuid, out int _tmp) ? _tmp : 0) + 1;
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var bp in buildPlanList)
            {
                if (bp.Items == null) continue;
                foreach (var item in bp.Items)
                {
                    if (item.BuildLocationType == DestinationType.Colony
                        && !string.IsNullOrEmpty(item.BuildLocationUUID))
                    {
                        _buildItemMap.TryGetValue(item.BuildLocationUUID, out int c);
                        _buildItemMap[item.BuildLocationUUID] = c + 1;
                    }
                }
            }

            _supplyChainMap = new Dictionary<string, int>();
            foreach (var chain in supplyChains ?? Enumerable.Empty<SupplyChain>())
            {
                if (chain.Stages == null) continue;
                foreach (var stage in chain.Stages)
                {
                    if (stage.LocationType == DestinationType.Colony
                        && !string.IsNullOrEmpty(stage.LocationUUID))
                    {
                        _supplyChainMap.TryGetValue(stage.LocationUUID, out int c);
                        _supplyChainMap[stage.LocationUUID] = c + 1;
                    }
                }
            }

            _overflowMap = new Dictionary<string, int>();
            foreach (var rule in overflowRules ?? Enumerable.Empty<WarehouseOverflowRule>())
            {
                if (!string.IsNullOrEmpty(rule.ColonyUUID))
                {
                    _overflowMap.TryGetValue(rule.ColonyUUID, out int c);
                    _overflowMap[rule.ColonyUUID] = c + 1;
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
            _supplyChainMap.TryGetValue(colonyUUID, out int supplyChainCount);
            _overflowMap.TryGetValue(colonyUUID, out int overflowCount);

            return new ColonyReferenceReport(routeCount, planCount, buildItemCount, supplyChainCount, overflowCount);
        }
    }

    public class ColonyReferenceReport
    {
        public int TotalCount { get; }
        public int RouteCount { get; }
        public int PlanCount { get; }
        public int BuildItemCount { get; }
        public int SupplyChainCount { get; }
        public int OverflowCount { get; }

        public ColonyReferenceReport(int routeCount, int planCount, int buildItemCount = 0,
            int supplyChainCount = 0, int overflowCount = 0)
        {
            RouteCount = routeCount;
            PlanCount = planCount;
            BuildItemCount = buildItemCount;
            SupplyChainCount = supplyChainCount;
            OverflowCount = overflowCount;
            TotalCount = routeCount + planCount + buildItemCount + supplyChainCount + overflowCount;
        }

        public static readonly ColonyReferenceReport Empty = new ColonyReferenceReport(0, 0, 0, 0, 0);
    }
}
