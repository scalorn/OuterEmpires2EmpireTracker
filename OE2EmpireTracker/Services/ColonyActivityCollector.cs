using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Services
{
    public enum ActivityType
    {
        Building,
        Manufacturing,
        CommodityManufacturing,
        CommodityRequest,
        Research,
        Mining,
        Refining,
        ColonyImportStaleness
    }

    public class ActivityRow
    {
        public ActivityType Type { get; set; }
        public string SystemName { get; set; }
        public string ColonyName { get; set; }
        public string SourceName { get; set; }
        public string ProcessDetails { get; set; }

        /// <summary>
        /// Reference to the CountDownTime for structure-based activities.
        /// Null for CommodityRequest rows.
        /// </summary>
        public CountDownTime CountDown { get; set; }

        /// <summary>
        /// For CommodityRequest rows: the NeedBy DateTime.
        /// For structure rows: DateTime.MinValue (unused).
        /// </summary>
        public DateTime NeedBy { get; set; }

        /// <summary>
        /// Returns the current seconds remaining for sorting and display.
        /// </summary>
        public long GetSecondsRemaining()
        {
            if (CountDown != null)
            {
                // Repeating timer with elapsed intervals: show 0 until background processor runs
                if (CountDown.IsRepeating && CountDown.IntervalsPassed > 0)
                    return 0;
                return Math.Max(0, CountDown.TimeRemaining);
            }
            long seconds = (long)(NeedBy - SystemClock.UtcNow).TotalSeconds;
            return Math.Max(0, seconds);
        }

        /// <summary>
        /// Returns the formatted time remaining string in "Xd Yh Zm Ws" format.
        /// </summary>
        public string GetTimeRemainingString()
        {
            if (CountDown != null)
            {
                // Repeating timer with elapsed intervals: show 0s until background processor runs
                if (CountDown.IsRepeating && CountDown.IntervalsPassed > 0)
                    return "0s";
                return CountDown.TimeRemainingString;
            }
            long seconds = GetSecondsRemaining();
            if (seconds <= 0) return "0s";
            return FormatSeconds(seconds);
        }

        /// <summary>
        /// Formats seconds as "Xd Yh Zm Ws" matching CountDownTime.TimeRemainingString format.
        /// </summary>
        public static string FormatSeconds(long seconds)
        {
            if (seconds <= 0) return "0s";
            var ts = TimeSpan.FromSeconds(seconds);
            string result = string.Empty;
            bool started = false;

            if (ts.Days > 0)
            {
                result = $"{ts.Days}d";
                started = true;
            }

            if (started || ts.Hours > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Hours}h";
                started = true;
            }

            if (started || ts.Minutes > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Minutes}m";
                started = true;
            }

            if (started || ts.Seconds > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Seconds}s";
            }

            return result;
        }
    }

    public static class ColonyActivityCollector
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Scans all provided colonies and returns ActivityRow instances for every
        /// active timer and unfulfilled commodity request.
        /// </summary>
        public static List<ActivityRow> CollectActivities(
            IEnumerable<Colony> colonies, PlayerContext playerContext)
        {
            var rows = new List<ActivityRow>();

            foreach (var colony in colonies)
            {
                CollectStructureActivities(colony, playerContext, rows);
                CollectCommodityActivities(colony, rows);
            }

            return rows;
        }

        private static void CollectStructureActivities(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            if (colony.Structures == null) return;

            foreach (var structure in colony.Structures)
            {
                Blueprint blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null) continue;

                string sourceName = $"#{structure.displaySequence} {blueprint.ExtendedName}";

                // BuildCompletionTime takes priority over ProcessCompletionTime
                if (structure.BuildCompletionTime != null &&
                    structure.BuildCompletionTime.TimeRemaining > 0)
                {
                    rows.Add(new ActivityRow
                    {
                        Type = ActivityType.Building,
                        SystemName = colony.SystemName,
                        ColonyName = colony.ColonyName,
                        SourceName = sourceName,
                        ProcessDetails = "Building",
                        CountDown = structure.BuildCompletionTime,
                        NeedBy = DateTime.MinValue
                    });
                    continue;
                }

                if (structure.ProcessCompletionTime != null &&
                    structure.ProcessCompletionTime.TimeRemaining > 0)
                {
                    ActivityType type;
                    string details;

                    if (blueprint.BluePrintType == BlueprintTypes.MiningRig)
                    {
                        type = ActivityType.Mining;
                        details = GetMiningDetails(structure, playerContext);
                    }
                    else if (blueprint.BluePrintType == BlueprintTypes.Refinery)
                    {
                        type = ActivityType.Refining;
                        details = GetRefiningDetails(structure);
                    }
                    else if (blueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    {
                        type = ActivityType.Research;
                        details = GetResearchDetails(structure, playerContext);
                    }
                    else if (blueprint.BluePrintType == BlueprintTypes.Manufactory)
                    {
                        type = ActivityType.Manufacturing;
                        details = GetManufacturingDetails(structure, playerContext);
                    }
                    else if (blueprint.BluePrintType.IsCommodityFactory())
                    {
                        type = ActivityType.CommodityManufacturing;
                        details = GetCommodityManufacturingDetails(structure);
                    }
                    else
                    {
                        continue;
                    }

                    rows.Add(new ActivityRow
                    {
                        Type = type,
                        SystemName = colony.SystemName,
                        ColonyName = colony.ColonyName,
                        SourceName = sourceName,
                        ProcessDetails = details,
                        CountDown = structure.ProcessCompletionTime,
                        NeedBy = DateTime.MinValue
                    });
                }
            }
        }

        private static string GetMiningDetails(ColonyStructure structure, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(structure.MiningSurvey) ||
                string.IsNullOrEmpty(structure.MiningSurveyResource))
                return string.Empty;

            Survey survey = playerContext.FindSurvey(structure.MiningSurvey);
            if (survey == null || !survey.Resources.ContainsKey(structure.MiningSurveyResource))
                return string.Empty;

            SurveyResource resource = survey.Resources[structure.MiningSurveyResource];
            return $"{resource.Amount}/h {resource.Resource} ({resource.Purity})";
        }

        private static string GetRefiningDetails(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.RefiningResource) ||
                string.IsNullOrEmpty(structure.RefiningResourcePurity))
                return string.Empty;

            var recipe = RefiningRecipes.FindByInput(
                structure.RefiningResource, structure.RefiningResourcePurity);

            if (recipe != null)
            {
                return $"{recipe.ConsumeRate}:{recipe.ProduceRate} {recipe.OutputResource}";
            }

            int baseRate = GameConstants.RefiningBaseRate;
            int outputRate = GetRefiningOutputRate(structure.RefiningResourcePurity, baseRate);
            return $"{baseRate}:{outputRate} {structure.RefiningResource} ({structure.RefiningResourcePurity})";
        }

        private static int GetRefiningOutputRate(string purity, int baseRate)
        {
            switch (purity)
            {
                case GameConstants.PurityLow: return baseRate * GameConstants.PurityMultiplierLow;
                case GameConstants.PurityMedium: return baseRate * GameConstants.PurityMultiplierMedium;
                case GameConstants.PurityHigh: return baseRate * GameConstants.PurityMultiplierHigh;
                default: return baseRate * GameConstants.PurityMultiplierLow;
            }
        }

        private static string GetResearchDetails(ColonyStructure structure, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                return string.Empty;

            Blueprint bp = playerContext.FindBlueprint(structure.ResearchingBlueprintUUID);
            if (bp == null)
                return string.Empty;

            return $"Evo {bp.Evolution}->{bp.Evolution + 1} {bp.Name}";
        }

        private static string GetManufacturingDetails(ColonyStructure structure, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                return string.Empty;

            Blueprint bp = playerContext.FindBlueprint(structure.ManufacturingBlueprintUUID);
            if (bp == null)
                return string.Empty;

            int displayProgress = Math.Min(structure.ManufacturingCompleted + 1, structure.ManufacturingQuantity);
            return $"({displayProgress}/{structure.ManufacturingQuantity}) {bp.ExtendedName}";
        }

        private static string GetCommodityManufacturingDetails(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingCommodityName))
                return string.Empty;

            int displayProgress = Math.Min(structure.ManufacturingCompleted + 1, structure.ManufacturingQuantity);
            return $"({displayProgress}/{structure.ManufacturingQuantity}) {structure.ManufacturingCommodityName} x{GameConstants.CommoditiesPerCycle}";
        }

        private static void CollectCommodityActivities(Colony colony, List<ActivityRow> rows)
        {
            if (colony.Commodities == null) return;

            foreach (var commodity in colony.Commodities)
            {
                if (commodity.Fulfilled) continue;

                rows.Add(new ActivityRow
                {
                    Type = ActivityType.CommodityRequest,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Commodity Request",
                    ProcessDetails = $"{commodity.Name} x{commodity.Requested}",
                    CountDown = null,
                    NeedBy = commodity.NeedBy
                });
            }
        }
    }
}
