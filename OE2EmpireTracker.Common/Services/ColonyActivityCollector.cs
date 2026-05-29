using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
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
                CollectOverflowPredictions(colony, playerContext, rows);
            }

            return rows;
        }

        /// <summary>
        /// Evaluates active overflow rules for the colony and emits prediction rows
        /// when a threshold is already exceeded or will be exceeded within the
        /// configured prediction horizon.
        /// </summary>
        public static void CollectOverflowPredictions(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            int horizonHours = PreferencesStore.GetInstance()
                .Preferences.Thresholds.OverflowPredictionHorizonHours;

            var rules = playerContext.WarehouseOverflowRuleList;

            foreach (var rule in rules)
            {
                if (!rule.IsActive)
                {
                    continue;
                }

                if (rule.ColonyUUID != colony.UUID)
                {
                    continue;
                }

                if (rule.RuleType == OverflowRuleType.SpecificResource)
                {
                    EvaluateSpecificResourceRule(colony, playerContext, rule, horizonHours, rows);
                }
                else if (rule.RuleType == OverflowRuleType.TotalWarehouse)
                {
                    EvaluateTotalWarehouseRule(colony, playerContext, rule, horizonHours, rows);
                }
            }
        }

        private static void CollectStructureActivities(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            if (colony.Structures == null) return;

            foreach (var structure in colony.Structures)
            {
                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null) continue;

                string sourceName = $"#{structure.DisplaySequence} {blueprint.ExtendedName}";

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
            int outputRate = GameConstants.GetRefiningOutputRate(structure.RefiningResourcePurity, baseRate);
            return $"{baseRate}:{outputRate} {structure.RefiningResource} ({structure.RefiningResourcePurity})";
        }

        private static string GetResearchDetails(ColonyStructure structure, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                return string.Empty;

            var bp = playerContext.FindBlueprint(structure.ResearchingBlueprintUUID);
            if (bp == null)
                return string.Empty;

            return $"Evo {bp.Evolution}->{bp.Evolution + 1} {bp.Name}";
        }

        private static string GetManufacturingDetails(ColonyStructure structure, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                return string.Empty;

            var bp2 = playerContext.FindBlueprint(structure.ManufacturingBlueprintUUID);
            if (bp2 == null)
                return string.Empty;

            int displayProgress = Math.Min(structure.ManufacturingCompleted + 1, structure.ManufacturingQuantity);
            return $"({displayProgress}/{structure.ManufacturingQuantity}) {bp2.ExtendedName}";
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

        private static void EvaluateSpecificResourceRule(
            Colony colony,
            PlayerContext playerContext,
            WarehouseOverflowRule rule,
            int horizonHours,
            List<ActivityRow> rows)
        {
            var items = colony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
            int currentStockpile = items.Sum(i => i.Quantity);

            if (currentStockpile >= rule.TriggerThreshold)
            {
                rows.Add(new ActivityRow
                {
                    Type = ActivityType.OverflowPrediction,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Overflow Rule",
                    ProcessDetails = $"Overflow triggered -- {rule.ResourceName} ({rule.ResourcePurity})",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
                return;
            }

            decimal netRate = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, playerContext, rule.ResourceName, rule.ResourcePurity);

            if (netRate <= 0m)
            {
                return;
            }

            decimal hoursUntilTrigger = (rule.TriggerThreshold - currentStockpile) / netRate;

            if (hoursUntilTrigger <= horizonHours)
            {
                long seconds = (long)(hoursUntilTrigger * 3600m);
                string formattedTime = ActivityRow.FormatSeconds(seconds);
                rows.Add(new ActivityRow
                {
                    Type = ActivityType.OverflowPrediction,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Overflow Rule",
                    ProcessDetails = $"Overflow in {formattedTime} -- {rule.ResourceName} ({rule.ResourcePurity})",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
            }
        }

        private static void EvaluateTotalWarehouseRule(
            Colony colony,
            PlayerContext playerContext,
            WarehouseOverflowRule rule,
            int horizonHours,
            List<ActivityRow> rows)
        {
            decimal currentVolume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            if (currentVolume >= rule.TriggerThreshold)
            {
                rows.Add(new ActivityRow
                {
                    Type = ActivityType.OverflowPrediction,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Overflow Rule",
                    ProcessDetails = "Overflow triggered -- Total Warehouse",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
                return;
            }

            decimal netVolumeRate = ColonyResourceRateCalculator.GetNetWarehouseVolumeGrowthRate(
                colony, playerContext);

            if (netVolumeRate <= 0m)
            {
                return;
            }

            decimal hoursUntilTrigger = (rule.TriggerThreshold - currentVolume) / netVolumeRate;

            if (hoursUntilTrigger <= horizonHours)
            {
                long seconds = (long)(hoursUntilTrigger * 3600m);
                string formattedTime = ActivityRow.FormatSeconds(seconds);
                rows.Add(new ActivityRow
                {
                    Type = ActivityType.OverflowPrediction,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Overflow Rule",
                    ProcessDetails = $"Overflow in {formattedTime} -- Total Warehouse",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
            }
        }
    }
}
