using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a Station from RouteStops, DeliveryPlanStops, BuildItems,
    /// MarketListings, MarketTransactions, SupplyChainStages, StockPlan targets,
    /// and WarehouseOverflowRules.
    /// Used to prevent deletion of stations that are still in use.
    /// </summary>
    public class StationReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _routeStopMap;
        private readonly Dictionary<string, int> _deliveryPlanStopMap;
        private readonly Dictionary<string, int> _buildItemAssemblyMap;
        private readonly Dictionary<string, int> _buildItemBuildMap;
        private readonly Dictionary<string, int> _marketListingMap;
        private readonly Dictionary<string, int> _marketTransactionMap;
        private readonly Dictionary<string, int> _supplyChainMap;
        private readonly Dictionary<string, int> _stockTargetMap;
        private readonly Dictionary<string, int> _overflowDestMap;
        private readonly Dictionary<string, int> _shipLocationMap;

        public StationReferenceCounter(
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<DeliveryPlan> plans,
            IEnumerable<BuildPlan> buildPlans,
            IEnumerable<MarketListing> marketListings = null,
            IEnumerable<MarketTransaction> marketTransactions = null,
            IEnumerable<SupplyChain> supplyChains = null,
            IEnumerable<StockPlan> stockPlans = null,
            IEnumerable<WarehouseOverflowRule> overflowRules = null,
            IEnumerable<Ship> ships = null)
        {
            var routeList = routes ?? Enumerable.Empty<DeliveryRoute>();
            var planList = plans ?? Enumerable.Empty<DeliveryPlan>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();

            _routeStopMap = new Dictionary<string, int>();
            foreach (var route in routeList)
            {
                if (route.Stops == null) continue;
                foreach (var stop in route.Stops)
                {
                    if (stop.DestinationType == DestinationType.Station
                        && !string.IsNullOrEmpty(stop.DestinationUUID))
                    {
                        _routeStopMap.TryGetValue(stop.DestinationUUID, out int c);
                        _routeStopMap[stop.DestinationUUID] = c + 1;
                    }
                }
            }

            _deliveryPlanStopMap = new Dictionary<string, int>();
            foreach (var plan in planList)
            {
                if (plan.Stops == null) continue;
                foreach (var stop in plan.Stops)
                {
                    if (stop.DestinationType == DestinationType.Station
                        && !string.IsNullOrEmpty(stop.DestinationUUID))
                    {
                        _deliveryPlanStopMap.TryGetValue(stop.DestinationUUID, out int c);
                        _deliveryPlanStopMap[stop.DestinationUUID] = c + 1;
                    }
                }
            }

            _buildItemAssemblyMap = new Dictionary<string, int>();
            _buildItemBuildMap = new Dictionary<string, int>();
            foreach (var bp in buildPlanList)
            {
                if (bp.Items == null) continue;
                foreach (var item in bp.Items)
                {
                    if (item.AssemblyLocationType == DestinationType.Station
                        && !string.IsNullOrEmpty(item.AssemblyLocationUUID))
                    {
                        _buildItemAssemblyMap.TryGetValue(item.AssemblyLocationUUID, out int c);
                        _buildItemAssemblyMap[item.AssemblyLocationUUID] = c + 1;
                    }

                    if (item.BuildLocationType == DestinationType.Station
                        && !string.IsNullOrEmpty(item.BuildLocationUUID))
                    {
                        _buildItemBuildMap.TryGetValue(item.BuildLocationUUID, out int c);
                        _buildItemBuildMap[item.BuildLocationUUID] = c + 1;
                    }
                }
            }

            _marketListingMap = new Dictionary<string, int>();
            foreach (var listing in marketListings ?? Enumerable.Empty<MarketListing>())
            {
                if (!string.IsNullOrEmpty(listing.StationUUID))
                {
                    _marketListingMap.TryGetValue(listing.StationUUID, out int c);
                    _marketListingMap[listing.StationUUID] = c + 1;
                }
            }

            _marketTransactionMap = new Dictionary<string, int>();
            foreach (var tx in marketTransactions ?? Enumerable.Empty<MarketTransaction>())
            {
                if (!string.IsNullOrEmpty(tx.StationUUID))
                {
                    _marketTransactionMap.TryGetValue(tx.StationUUID, out int c);
                    _marketTransactionMap[tx.StationUUID] = c + 1;
                }
            }

            _supplyChainMap = new Dictionary<string, int>();
            foreach (var chain in supplyChains ?? Enumerable.Empty<SupplyChain>())
            {
                if (chain.Stages == null) continue;
                foreach (var stage in chain.Stages)
                {
                    if (stage.LocationType == DestinationType.Station
                        && !string.IsNullOrEmpty(stage.LocationUUID))
                    {
                        _supplyChainMap.TryGetValue(stage.LocationUUID, out int c);
                        _supplyChainMap[stage.LocationUUID] = c + 1;
                    }
                }
            }

            _stockTargetMap = new Dictionary<string, int>();
            foreach (var plan in stockPlans ?? Enumerable.Empty<StockPlan>())
            {
                if (plan.Targets == null) continue;
                foreach (var target in plan.Targets)
                {
                    if (target.Scope == StockTargetScope.Station
                        && !string.IsNullOrEmpty(target.LocationUUID))
                    {
                        _stockTargetMap.TryGetValue(target.LocationUUID, out int c);
                        _stockTargetMap[target.LocationUUID] = c + 1;
                    }
                }
            }

            _overflowDestMap = new Dictionary<string, int>();
            foreach (var rule in overflowRules ?? Enumerable.Empty<WarehouseOverflowRule>())
            {
                if (rule.DestinationType == DestinationType.Station
                    && !string.IsNullOrEmpty(rule.DestinationUUID))
                {
                    _overflowDestMap.TryGetValue(rule.DestinationUUID, out int c);
                    _overflowDestMap[rule.DestinationUUID] = c + 1;
                }
            }

            _shipLocationMap = new Dictionary<string, int>();
            foreach (var ship in ships ?? Enumerable.Empty<Ship>())
            {
                if (ship.LocationType == DestinationType.Station
                    && !string.IsNullOrEmpty(ship.LocationUUID))
                {
                    _shipLocationMap.TryGetValue(ship.LocationUUID, out int c);
                    _shipLocationMap[ship.LocationUUID] = c + 1;
                }
            }
        }

        /// <summary>
        /// Returns the total number of entities referencing the given station UUID.
        /// </summary>
        public int CountReferences(string stationUUID)
        {
            if (string.IsNullOrEmpty(stationUUID))
                return 0;

            _routeStopMap.TryGetValue(stationUUID, out int routeCount);
            _deliveryPlanStopMap.TryGetValue(stationUUID, out int planCount);
            _buildItemAssemblyMap.TryGetValue(stationUUID, out int assemblyCount);
            _buildItemBuildMap.TryGetValue(stationUUID, out int buildCount);
            _marketListingMap.TryGetValue(stationUUID, out int listingCount);
            _marketTransactionMap.TryGetValue(stationUUID, out int txCount);
            _supplyChainMap.TryGetValue(stationUUID, out int scCount);
            _stockTargetMap.TryGetValue(stationUUID, out int stCount);
            _overflowDestMap.TryGetValue(stationUUID, out int overflowCount);
            _shipLocationMap.TryGetValue(stationUUID, out int shipCount);

            return routeCount + planCount + assemblyCount + buildCount
                 + listingCount + txCount + scCount + stCount + overflowCount + shipCount;
        }
    }
}
