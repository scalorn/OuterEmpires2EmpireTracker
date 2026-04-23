using System.Collections.Generic;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Lookup table for research duration by current evolution level.
    /// Values are in seconds. The rules for growth are opaque and may change
    /// as the game evolves -- this table should be updated as new data is discovered.
    /// Data can be loaded from BaselineData.json or falls back to hardcoded values.
    /// </summary>
    public static class ResearchTimeLookup
    {
        private static Dictionary<int, long> _researchTimeByEvolution = BuildLookup(GetFallbackResearchTimes());
        private static List<ResearchTimeEntry> _researchTimes = GetFallbackResearchTimes();

        /// <summary>
        /// The current list of research time entries (for serialization).
        /// </summary>
        public static IReadOnlyList<ResearchTimeEntry> ResearchTimes => _researchTimes.AsReadOnly();

        /// <summary>
        /// Replaces the research time data with externally-loaded entries (e.g. from BaselineData.json).
        /// THREADING: BackgroundProcessor must be stopped before calling this method.
        /// </summary>
        public static void SetResearchTimes(List<ResearchTimeEntry> entries)
        {
            _researchTimes = new List<ResearchTimeEntry>(entries);
            _researchTimeByEvolution = BuildLookup(_researchTimes);
        }

        /// <summary>
        /// Returns the hardcoded fallback research time list. Used when BaselineData.json
        /// does not contain a ResearchTime array.
        /// </summary>
        public static List<ResearchTimeEntry> GetFallbackResearchTimes()
        {
            return new List<ResearchTimeEntry>
            {
                new ResearchTimeEntry { Evolution = 0,  ResearchTimeSeconds = 2 * 86400 },   // Evo 0 -> 1:  2 days
                new ResearchTimeEntry { Evolution = 1,  ResearchTimeSeconds = 4 * 86400 },   // Evo 1 -> 2:  4 days
                new ResearchTimeEntry { Evolution = 2,  ResearchTimeSeconds = 6 * 86400 },   // Evo 2 -> 3:  6 days
                new ResearchTimeEntry { Evolution = 3,  ResearchTimeSeconds = 8 * 86400 },   // Evo 3 -> 4:  8 days
                new ResearchTimeEntry { Evolution = 4,  ResearchTimeSeconds = 10 * 86400 },  // Evo 4 -> 5:  10 days
                new ResearchTimeEntry { Evolution = 5,  ResearchTimeSeconds = 12 * 86400 },  // Evo 5 -> 6:  12 days
                new ResearchTimeEntry { Evolution = 6,  ResearchTimeSeconds = 14 * 86400 },  // Evo 6 -> 7:  14 days
                new ResearchTimeEntry { Evolution = 7,  ResearchTimeSeconds = 16 * 86400 },  // Evo 7 -> 8:  16 days
                new ResearchTimeEntry { Evolution = 8,  ResearchTimeSeconds = 18 * 86400 },  // Evo 8 -> 9:  18 days
                new ResearchTimeEntry { Evolution = 9,  ResearchTimeSeconds = 20 * 86400 },  // Evo 9 -> 10: 20 days
                new ResearchTimeEntry { Evolution = 10, ResearchTimeSeconds = 22 * 86400 },  // Evo 10 -> 11: 22 days
                new ResearchTimeEntry { Evolution = 11, ResearchTimeSeconds = 24 * 86400 },  // Evo 11 -> 12: 24 days
                new ResearchTimeEntry { Evolution = 12, ResearchTimeSeconds = 26 * 86400 },  // Evo 12 -> 13: 26 days
                new ResearchTimeEntry { Evolution = 13, ResearchTimeSeconds = 28 * 86400 },  // Evo 13 -> 14: 28 days
                new ResearchTimeEntry { Evolution = 14, ResearchTimeSeconds = 30 * 86400 },  // Evo 14 -> 15: 30 days
            };
        }

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

        private static Dictionary<int, long> BuildLookup(List<ResearchTimeEntry> entries)
        {
            var dict = new Dictionary<int, long>();
            foreach (var entry in entries)
            {
                dict[entry.Evolution] = entry.ResearchTimeSeconds;
            }

            return dict;
        }
    }
}
