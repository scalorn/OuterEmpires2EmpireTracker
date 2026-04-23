using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class SurveyReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IEnumerable<Colony> _colonies;
        private readonly Dictionary<string, int> _buildItemMap;

        public SurveyReferenceCounter(IEnumerable<Colony> colonies, IEnumerable<BuildPlan> buildPlans = null)
        {
            _colonies = colonies ?? Enumerable.Empty<Colony>();

            _buildItemMap = new Dictionary<string, int>();
            foreach (var plan in buildPlans ?? Enumerable.Empty<BuildPlan>())
            {
                if (plan.Items == null) continue;
                foreach (var item in plan.Items)
                {
                    if (!string.IsNullOrEmpty(item.MiningSurveyUUID))
                    {
                        _buildItemMap.TryGetValue(item.MiningSurveyUUID, out int c);
                        _buildItemMap[item.MiningSurveyUUID] = c + 1;
                    }
                }
            }
        }

        public SurveyReferenceReport CountReferences(string surveyUUID)
        {
            if (string.IsNullOrEmpty(surveyUUID))
                return SurveyReferenceReport.Empty;

            int minerCount = _colonies
                .Where(c => c.Structures != null)
                .SelectMany(c => c.Structures)
                .Count(s => s.MiningSurvey == surveyUUID);

            _buildItemMap.TryGetValue(surveyUUID, out int buildItemCount);

            return new SurveyReferenceReport(minerCount, buildItemCount);
        }
    }

    public class SurveyReferenceReport
    {
        public int TotalCount { get; }
        public int MinerCount { get; }
        public int BuildItemCount { get; }

        public SurveyReferenceReport(int minerCount, int buildItemCount = 0)
        {
            MinerCount = minerCount;
            BuildItemCount = buildItemCount;
            TotalCount = minerCount + buildItemCount;
        }

        public static readonly SurveyReferenceReport Empty = new SurveyReferenceReport(0, 0);
    }
}
