using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>Warning level for colony tab background colors.</summary>
    public enum TabWarningLevel
    {
        None,
        Yellow,
        Red
    }

    /// <summary>
    /// Pure static service that evaluates colony state and returns a warning level
    /// for the Structures and Worker tab selectors.
    /// </summary>
    public static class TabWarningService
    {
        /// <summary>Structure count at or above which the yellow warning activates.</summary>
        private static int StructureYellowThreshold =>
            PreferencesStore.GetInstance().Preferences.Thresholds.StructureCountYellow;

        /// <summary>Structure count at or above which the red warning activates.</summary>
        private static int StructureRedThreshold =>
            PreferencesStore.GetInstance().Preferences.Thresholds.StructureCountRed;

        /// <summary>Due window at or below which the yellow warning activates for worker requests.</summary>
        private static TimeSpan WorkerYellowWindow =>
            TimeSpan.FromSeconds(PreferencesStore.GetInstance().Preferences.Thresholds.WorkerRequestYellowSeconds);

        /// <summary>Due window at or below which the red warning activates for worker requests.</summary>
        private static TimeSpan WorkerRedWindow =>
            TimeSpan.FromSeconds(PreferencesStore.GetInstance().Preferences.Thresholds.WorkerRequestRedSeconds);

        /// <summary>Elapsed time since last import at or above which the yellow warning activates.</summary>
        private static TimeSpan ColonyImportStalenessYellowWindow =>
            TimeSpan.FromSeconds(PreferencesStore.GetInstance().Preferences.Thresholds.ColonyImportStalenessYellowSeconds);

        /// <summary>Elapsed time since last import at or above which the red warning activates.</summary>
        private static TimeSpan ColonyImportStalenessRedWindow =>
            TimeSpan.FromSeconds(PreferencesStore.GetInstance().Preferences.Thresholds.ColonyImportStalenessRedSeconds);

        /// <summary>
        /// Returns the warning level for the Structures tab based on the colony's structure count.
        /// Red if >= 66, Yellow if >= 60, None otherwise.
        /// </summary>
        public static TabWarningLevel EvaluateStructureWarning(int structureCount)
        {
            if (structureCount >= StructureRedThreshold)
                return TabWarningLevel.Red;
            if (structureCount >= StructureYellowThreshold)
                return TabWarningLevel.Yellow;
            return TabWarningLevel.None;
        }

        /// <summary>
        /// Returns the warning level for the Worker tab based on unfulfilled commodity requests.
        /// Filters to unfulfilled requests with NeedBy != DateTime.MinValue.
        /// Red if any due &lt;= 1 day or overdue, Yellow if any due &lt;= 2 days, None otherwise.
        /// </summary>
        public static TabWarningLevel EvaluateWorkerWarning(
            IEnumerable<CommodityRequested> commodities, DateTime now)
        {
            if (commodities == null)
                return TabWarningLevel.None;

            var level = TabWarningLevel.None;

            foreach (var req in commodities)
            {
                if (req.Fulfilled)
                    continue;
                if (req.NeedBy == DateTime.MinValue)
                    continue;

                TimeSpan dueWindow = req.NeedBy - now;

                if (dueWindow <= WorkerRedWindow)
                    return TabWarningLevel.Red; // Can't get worse — short-circuit

                if (dueWindow <= WorkerYellowWindow)
                    level = TabWarningLevel.Yellow;
            }

            return level;
        }

        /// <summary>
        /// Returns the warning level for the Administration tab based on colony import staleness.
        /// Red if null/empty, parse failure, or elapsed >= 6 days. Yellow if elapsed >= 5 days. None otherwise.
        /// </summary>
        public static TabWarningLevel EvaluateColonyImportStalenessWarning(string lastImportDateTime, DateTime now)
        {
            if (string.IsNullOrEmpty(lastImportDateTime))
                return TabWarningLevel.Red;

            if (!SurveyDateTimeParser.TryParseIso(lastImportDateTime, out DateTime parsed))
                return TabWarningLevel.Red;

            TimeSpan elapsed = now - parsed;

            if (elapsed >= ColonyImportStalenessRedWindow)
                return TabWarningLevel.Red;
            if (elapsed >= ColonyImportStalenessYellowWindow)
                return TabWarningLevel.Yellow;

            return TabWarningLevel.None;
        }
    }
}
