using System;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Calculates structure build time based on the Builder skill level.
    /// Uses double (not decimal) intentionally -- this is a transient time
    /// calculation that produces a long, not a persisted game data value.
    /// </summary>
    public static class BuildTimeCalculator
    {
        /// <summary>
        /// Calculates build time in seconds based on Builder skill level.
        /// Formula: 86400 * (1 - level * 0.02), minimum 1 second.
        /// </summary>
        public static long Calculate(int builderSkillLevel)
        {
            double seconds = 86400.0 * (1.0 - builderSkillLevel * 0.02);
            return Math.Max(1, (long)seconds);
        }
    }
}
