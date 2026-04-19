using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class AsteroidReferenceReport
    {
        public int SurveyCount { get; set; }
        public int BuildItemCount { get; set; }
        public int RouteStopCount { get; set; }
        public int TotalCount => SurveyCount + BuildItemCount + RouteStopCount;

        public static readonly AsteroidReferenceReport Empty = new AsteroidReferenceReport();
    }

    /// <summary>
    /// Counts references to an Asteroid from Surveys, BuildItems, and RouteStops.
    /// Used to prevent deletion of asteroids that are still in use.
    /// </summary>
    public class AsteroidReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _surveyMap;
        private readonly Dictionary<string, int> _buildItemMap;
        private readonly Dictionary<string, int> _routeStopMap;

        public AsteroidReferenceCounter(
            IEnumerable<Survey> surveys,
            IEnumerable<BuildPlan> buildPlans,
            IEnumerable<DeliveryRoute> routes)
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
        }

        public AsteroidReferenceReport CountReferences(string asteroidUUID)
        {
            if (string.IsNullOrEmpty(asteroidUUID))
                return AsteroidReferenceReport.Empty;

            _surveyMap.TryGetValue(asteroidUUID, out int surveyCount);
            _buildItemMap.TryGetValue(asteroidUUID, out int buildItemCount);
            _routeStopMap.TryGetValue(asteroidUUID, out int routeStopCount);

            return new AsteroidReferenceReport
            {
                SurveyCount = surveyCount,
                BuildItemCount = buildItemCount,
                RouteStopCount = routeStopCount
            };
        }
    }
}