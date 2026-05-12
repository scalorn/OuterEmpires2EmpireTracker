using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Result of computing a yield distribution for a single resource+purity combo.
    /// </summary>
    public class YieldDistributionResult
    {
        /// <summary>Gets or sets the ordered list of (binMidpoint, percentage) points for the curve.</summary>
        public List<DistributionPoint> Points { get; set; } = new List<DistributionPoint>();

        /// <summary>Gets or sets the number of surveys that contained the resource+purity combo.</summary>
        public int MatchingSurveyCount { get; set; }

        /// <summary>Gets a value indicating whether fewer than 2 surveys matched (insufficient data).</summary>
        public bool InsufficientData => MatchingSurveyCount < 2;
    }
}
