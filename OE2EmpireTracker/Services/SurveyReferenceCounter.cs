using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class SurveyReferenceCounter
    {
        private readonly IEnumerable<Colony> _colonies;

        public SurveyReferenceCounter(IEnumerable<Colony> colonies)
        {
            _colonies = colonies ?? Enumerable.Empty<Colony>();
        }

        public SurveyReferenceReport CountReferences(string surveyUUID)
        {
            if (string.IsNullOrEmpty(surveyUUID))
                return SurveyReferenceReport.Empty;

            int minerCount = _colonies
                .Where(c => c.Structures != null)
                .SelectMany(c => c.Structures)
                .Count(s => s.MiningSurvey == surveyUUID);

            return new SurveyReferenceReport(minerCount);
        }
    }

    public class SurveyReferenceReport
    {
        public int TotalCount { get; }
        public int MinerCount { get; }

        public SurveyReferenceReport(int minerCount)
        {
            MinerCount = minerCount;
            TotalCount = minerCount;
        }

        public static readonly SurveyReferenceReport Empty = new SurveyReferenceReport(0);
    }
}
