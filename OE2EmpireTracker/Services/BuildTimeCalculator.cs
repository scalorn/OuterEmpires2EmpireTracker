using System;
using NLog;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Calculates structure build time based on the Builder skill level.
    /// Uses double (not decimal) intentionally -- this is a transient time
    /// calculation that produces a long, not a persisted game data value.
    /// </summary>
    public static class BuildTimeCalculator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Calculates build time in seconds based on Builder skill level.
        /// Formula: BaseBuildTimeSeconds * (1 - level * BuilderRatePerLevel), minimum 1 second.
        /// </summary>
        public static long Calculate(int builderSkillLevel)
        {
            double seconds = GameConstants.BaseBuildTimeSeconds * (1.0 - (builderSkillLevel * (double)GameConstants.BuilderRatePerLevel));
            return Math.Max(1, (long)seconds);
        }
    }
}
