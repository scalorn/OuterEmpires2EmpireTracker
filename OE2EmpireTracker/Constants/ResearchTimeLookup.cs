using System.Collections.Generic;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Lookup table for research duration by current evolution level.
    /// Values are in seconds. The rules for growth are opaque and may change
    /// as the game evolves — this table should be updated as new data is discovered.
    /// </summary>
    public static class ResearchTimeLookup
    {
        private static readonly Dictionary<int, long> _researchTimeByEvolution = new Dictionary<int, long>
        {
            { 0,  2 * 86400 },   // Evo 0 -> 1:  2 days
            { 1,  4 * 86400 },   // Evo 1 -> 2:  4 days
            { 2,  6 * 86400 },   // Evo 2 -> 3:  6 days
            { 3,  8 * 86400 },   // Evo 3 -> 4:  8 days
            { 4,  10 * 86400 },  // Evo 4 -> 5:  10 days
            { 5,  12 * 86400 },  // Evo 5 -> 6:  12 days
            { 6,  14 * 86400 },  // Evo 6 -> 7:  14 days
            { 7,  16 * 86400 },  // Evo 7 -> 8:  16 days
            { 8,  18 * 86400 },  // Evo 8 -> 9:  18 days
            { 9,  20 * 86400 },  // Evo 9 -> 10: 20 days
            { 10, 22 * 86400 },  // Evo 10 -> 11: 22 days
            { 11, 24 * 86400 },  // Evo 11 -> 12: 24 days
            { 12, 26 * 86400 },  // Evo 12 -> 13: 26 days
            { 13, 28 * 86400 },  // Evo 13 -> 14: 28 days
            { 14, 30 * 86400 },  // Evo 14 -> 15: 30 days
        };

        /// <summary>
        /// Returns the research duration in seconds for the given current evolution.
        /// Returns 0 if the evolution cannot be researched (>= 15 or not in table).
        /// </summary>
        public static long GetResearchTimeSeconds(int currentEvolution)
        {
            long seconds;
            if (_researchTimeByEvolution.TryGetValue(currentEvolution, out seconds))
                return seconds;
            return 0;
        }

        /// <summary>
        /// Returns true if the given evolution level can be researched further.
        /// </summary>
        public static bool CanResearchEvolution(int currentEvolution)
        {
            return _researchTimeByEvolution.ContainsKey(currentEvolution);
        }
    }
}
