using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a delivery route across delivery plans,
    /// warehouse overflow rules, and supply chain stages.
    /// </summary>
    public class DeliveryRouteReferenceCounter
    {
        private readonly Dictionary<string, int> _planMap;
        private readonly Dictionary<string, int> _overflowMap;
        private readonly Dictionary<string, int> _supplyChainMap;

        public DeliveryRouteReferenceCounter(
            IEnumerable<DeliveryPlan> plans,
            IEnumerable<WarehouseOverflowRule> overflowRules = null,
            IEnumerable<SupplyChain> supplyChains = null)
        {
            var planList = plans ?? Enumerable.Empty<DeliveryPlan>();

            _planMap = new Dictionary<string, int>();
            foreach (var p in planList)
            {
                if (!string.IsNullOrEmpty(p.RouteUUID))
                {
                    _planMap.TryGetValue(p.RouteUUID, out int c);
                    _planMap[p.RouteUUID] = c + 1;
                }
            }

            _overflowMap = new Dictionary<string, int>();
            foreach (var rule in overflowRules ?? Enumerable.Empty<WarehouseOverflowRule>())
            {
                if (!string.IsNullOrEmpty(rule.DeliveryRouteUUID))
                {
                    _overflowMap.TryGetValue(rule.DeliveryRouteUUID, out int c);
                    _overflowMap[rule.DeliveryRouteUUID] = c + 1;
                }
            }

            _supplyChainMap = new Dictionary<string, int>();
            foreach (var chain in supplyChains ?? Enumerable.Empty<SupplyChain>())
            {
                if (chain.Stages == null) continue;
                foreach (var stage in chain.Stages)
                {
                    if (!string.IsNullOrEmpty(stage.DeliveryRouteUUID))
                    {
                        _supplyChainMap.TryGetValue(stage.DeliveryRouteUUID, out int c);
                        _supplyChainMap[stage.DeliveryRouteUUID] = c + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Counts how many entities reference the given route UUID.
        /// </summary>
        public DeliveryRouteReferenceReport CountReferences(string routeUUID)
        {
            if (string.IsNullOrEmpty(routeUUID))
                return DeliveryRouteReferenceReport.Empty;

            _planMap.TryGetValue(routeUUID, out int planCount);
            _overflowMap.TryGetValue(routeUUID, out int overflowCount);
            _supplyChainMap.TryGetValue(routeUUID, out int supplyChainCount);
            return new DeliveryRouteReferenceReport(planCount, overflowCount, supplyChainCount);
        }
    }

    /// <summary>
    /// Immutable report of references to a delivery route.
    /// </summary>
    public class DeliveryRouteReferenceReport
    {
        public static readonly DeliveryRouteReferenceReport Empty =
            new DeliveryRouteReferenceReport(0, 0, 0);

        public DeliveryRouteReferenceReport(
            int deliveryPlanCount,
            int overflowRuleCount = 0,
            int supplyChainStageCount = 0)
        {
            DeliveryPlanCount = deliveryPlanCount;
            OverflowRuleCount = overflowRuleCount;
            SupplyChainStageCount = supplyChainStageCount;
            TotalCount = deliveryPlanCount + overflowRuleCount + supplyChainStageCount;
        }

        public int TotalCount { get; }

        public int DeliveryPlanCount { get; }

        public int OverflowRuleCount { get; }

        public int SupplyChainStageCount { get; }
    }
}
