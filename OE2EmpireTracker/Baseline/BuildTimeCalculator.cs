using System;

namespace OE2EmpireTracker.Baseline
{
    /// <summary>
    /// Calculates structure build time based on the Builder skill level.
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
