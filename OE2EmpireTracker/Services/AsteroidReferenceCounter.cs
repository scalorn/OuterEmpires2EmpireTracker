using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class AsteroidReferenceReport
    {
        public static readonly AsteroidReferenceReport Empty = new AsteroidReferenceReport();

        public int SurveyCount { get; set; }

        public int BuildItemCount { get; set; }

        public int RouteStopCount { get; set; }

        public int SupplyChainStageCount { get; set; }

        public int TotalCount => SurveyCount + BuildItemCount + RouteStopCount + SupplyChainStageCount;
    }

    /// <summary>
    /// Counts references to an Asteroid from Surveys, BuildItems, RouteStops,
    /// and SupplyChainStages.
    /// Used to prevent deletion of asteroids that are still in use.
    /// </summary>
    public class AsteroidReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _surveyMap;
        private readonly Dictionary<string, int> _buildItemMap;
        private readonly Dictionary<string, int> _routeStopMap;
        private readonly Dictionary<string, int> _supplyChainMap;

        public AsteroidReferenceCounter(
            IEnumerable<Survey> surveys,
            IEnumerable<BuildPlan> buildPlans,
            IEnumerable<DeliveryRoute> routes,
            IEnumerable<SupplyChain> supplyChains = null)
        {
            var surveyList = surveys ?? Enumerable.Empty<Survey>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();
            var routeList = routes ?? Enumerable.Empty<DeliveryRoute>();

            _surveyMap = new Dictionary<string, int>();
            foreach (var survey in surveyList)
            {
                if (!string.IsNullOrEmpty(survey.AsteroidUUID))
                {
                    _surveyMap.TryGetValue(survey.AsteroidUUID, out int c);
                    _surveyMap[survey.AsteroidUUID] = c + 1;
                }
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var bp in buildPlanList)
            {
                if (bp.Items == null) continue;
                foreach (var item in bp.Items)
                {
                    if (item.BuildLocationType == DestinationType.Asteroid
                        && !string.IsNullOrEmpty(item.BuildLocationUUID))
                    {
                        _buildItemMap.TryGetValue(item.BuildLocationUUID, out int c);
                        _buildItemMap[item.BuildLocationUUID] = c + 1;
                    }
                }
            }

            _routeStopMap = new Dictionary<string, int>();
            foreach (var route in routeList)
            {
                if (route.Stops == null) continue;
                foreach (var stop in route.Stops)
                {
                    if (stop.DestinationType == DestinationType.Asteroid
                        && !string.IsNullOrEmpty(stop.DestinationUUID))
                    {
                        _routeStopMap.TryGetValue(stop.DestinationUUID, out int c);
                        _routeStopMap[stop.DestinationUUID] = c + 1;
                    }
                }
            }

            _supplyChainMap = new Dictionary<string, int>();
            foreach (var chain in supplyChains ?? Enumerable.Empty<SupplyChain>())
            {
                if (chain.Stages == null) continue;
                foreach (var stage in chain.Stages)
                {
                    if (stage.LocationType == DestinationType.Asteroid
                        && !string.IsNullOrEmpty(stage.LocationUUID))
                    {
                        _supplyChainMap.TryGetValue(stage.LocationUUID, out int c);
                        _supplyChainMap[stage.LocationUUID] = c + 1;
                    }
                }
            }
        }

        public AsteroidReferenceReport CountReferences(string asteroidUUID)
        {
            if (string.IsNullOrEmpty(asteroidUUID))
                return AsteroidReferenceReport.Empty;

            _surveyMap.TryGetValue(asteroidUUID, out int surveyCount);
            _buildItemMap.TryGetValue(asteroidUUID, out int buildItemCount);
            _routeStopMap.TryGetValue(asteroidUUID, out int routeStopCount);
            _supplyChainMap.TryGetValue(asteroidUUID, out int supplyChainCount);

            return new AsteroidReferenceReport
            {
                SurveyCount = surveyCount,
                BuildItemCount = buildItemCount,
                RouteStopCount = routeStopCount,
                SupplyChainStageCount = supplyChainCount
            };
        }
    }
}
