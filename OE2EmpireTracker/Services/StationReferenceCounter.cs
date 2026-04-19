using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a Station from RouteStops, DeliveryPlanStops, and BuildItems.
    /// Used to prevent deletion of stations that are still in use.
    /// </summary>
    public class StationReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _routeStopMap;
        private readonly Dictionary<string, int> _deliveryPlanStopMap;
        private readonly Dictionary<string, int> _buildItemAssemblyMap;
        private readonly Dictionary<string, int> _buildItemBuildMap;

        public StationReferenceCounter(
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<DeliveryPlan> plans,
            IEnumerable<BuildPlan> buildPlans)
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

            return routeCount + planCount + assemblyCount + buildCount;
        }
    }
}