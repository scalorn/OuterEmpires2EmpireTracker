using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public static class ColonyInactivityCollector
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Scans all provided colonies and returns ActivityRow instances for every
        /// idle or underutilized production structure.
        /// </summary>
        public static List<ActivityRow> CollectInactivities(
            IEnumerable<Colony> colonies, PlayerContext playerContext)
        {
            var rows = new List<ActivityRow>();

            foreach (var colony in colonies)
            {
                CollectIdleStructures(colony, playerContext, rows);
                CollectUnderutilizedRefiners(colony, playerContext, rows);
                CollectDepletionETAs(colony, playerContext, rows);
                CollectColonyImportStaleness(colony, rows);
            }

            return rows;
        }

        /// <summary>
        /// Emits an ActivityRow for colony import staleness if the colony's LastImportDateTime
        /// is older than 1 day or is null/empty/unparseable.
        /// </summary>
        private static void CollectColonyImportStaleness(Colony colony, List<ActivityRow> rows)
        {
            DateTime parsed;
            if (!SurveyDateTimeParser.TryParseIso(colony.LastImportDateTime, out parsed))
            {
                Log.Debug("CollectColonyImportStaleness: colony={0}, LastImportDateTime='{1}' -- UNPARSEABLE",
                    colony.ColonyName, colony.LastImportDateTime ?? "(null)");

                // Unparseable or null/empty -- treat as maximally stale
                rows.Add(new ActivityRow
                {
                    Type = ActivityType.ColonyImportStaleness,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Colony Import",
                    ProcessDetails = "Unknown since last import",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
                return;
            }

            long elapsedSeconds = (long)(SystemClock.UtcNow - parsed).TotalSeconds;
            if (elapsedSeconds > 86400)
            {
                Log.Debug("CollectColonyImportStaleness: colony={0}, LastImportDateTime='{1}', parsed={2:O}, elapsed={3}s (>{4}s threshold)",
                    colony.ColonyName, colony.LastImportDateTime, parsed, elapsedSeconds, 86400);

                rows.Add(new ActivityRow
                {
                    Type = ActivityType.ColonyImportStaleness,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Colony Import",
                    ProcessDetails = ActivityRow.FormatSeconds(elapsedSeconds) + " since last import",
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
            }
        }

        /// <summary>
        /// Emits ActivityRow entries for each refining group that has a finite depletion ETA
        /// (consumption exceeds mining). Shows "Depleted" when stockpile is zero.
        /// Skips groups where mining meets or exceeds consumption (sustained).
        /// </summary>
        private static void CollectDepletionETAs(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            if (colony.Structures == null)
            {
                return;
            }

            // Collect distinct resource+purity combinations from active refiners
            var refiningGroups = new HashSet<string>();
            foreach (var structure in colony.Structures)
            {
                if (!IsBuiltAndOnline(structure))
                {
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.Refinery)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(structure.RefiningResource) ||
                    string.IsNullOrEmpty(structure.RefiningResourcePurity))
                {
                    continue;
                }

                refiningGroups.Add(structure.RefiningResource + "|" + structure.RefiningResourcePurity);
            }

            foreach (var groupKey in refiningGroups)
            {
                string[] parts = groupKey.Split('|');
                string resource = parts[0];
                string purity = parts[1];

                decimal totalMining = ColonyResourceRateCalculator.GetTotalMiningRate(
                    colony, playerContext, resource, purity);
                decimal totalConsumption = ColonyResourceRateCalculator.GetTotalRefiningConsumption(
                    colony, playerContext, resource, purity);

                int stockpile = GetWarehouseStockpile(colony, resource, purity);

                // Mining meets or exceeds consumption — sustained, no row emitted
                if (totalMining >= totalConsumption)
                {
                    continue;
                }

                decimal netConsumptionRate = totalConsumption - totalMining;

                string processDetails;
                if (stockpile == 0)
                {
                    processDetails = $"Depleted -- {resource} ({purity})";
                }
                else
                {
                    decimal depletionHours = stockpile / netConsumptionRate;
                    long depletionSeconds = (long)(depletionHours * 3600m);
                    string formattedEta = ActivityRow.FormatSeconds(depletionSeconds);
                    processDetails = $"Depletion: {formattedEta} -- {resource} ({purity})";
                }

                rows.Add(new ActivityRow
                {
                    Type = ActivityType.Refining,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = "Resource Depletion",
                    ProcessDetails = processDetails,
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
            }
        }

        /// <summary>
        /// Returns true if the structure's PropertyBag has Built=True and Online=True.
        /// </summary>
        private static bool IsBuiltAndOnline(ColonyStructure structure)
        {
            return structure.IsBuiltAndOnline;
        }

        /// <summary>
        /// Returns true if the structure has an active process timer with time remaining.
        /// </summary>
        private static bool HasActiveProcess(ColonyStructure structure)
        {
            return structure.ProcessCompletionTime != null &&
                   structure.ProcessCompletionTime.TimeRemaining > 0;
        }

        /// <summary>
        /// Formats the source name as "#{DisplaySequence} {blueprint.ExtendedName}".
        /// Returns null if the blueprint cannot be found.
        /// </summary>
        private static string BuildSourceName(ColonyStructure structure, PlayerContext playerContext)
        {
            var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
            if (blueprint == null) return null;

            return $"#{structure.DisplaySequence} {blueprint.ExtendedName}";
        }

        /// <summary>
        /// Scans a colony for idle production structures across all 5 types:
        /// MiningRig, Refinery, ResearchLaboratory, Manufactory, CommodityFactory.
        /// </summary>
        private static void CollectIdleStructures(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            if (colony.Structures == null) return;

            foreach (var structure in colony.Structures)
            {
                if (!IsBuiltAndOnline(structure)) continue;
                if (HasActiveProcess(structure)) continue;

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null) continue;

                string sourceName = $"#{structure.DisplaySequence} {blueprint.ExtendedName}";

                ActivityType? type = null;
                string processDetails = null;

                if (blueprint.BluePrintType == BlueprintTypes.MiningRig)
                {
                    type = ActivityType.Mining;
                    processDetails = string.IsNullOrEmpty(structure.MiningSurveyResource)
                        ? "No survey assigned"
                        : "Idle";
                }
                else if (blueprint.BluePrintType == BlueprintTypes.Refinery)
                {
                    type = ActivityType.Refining;
                    processDetails = string.IsNullOrEmpty(structure.RefiningResource)
                        ? "No resource assigned"
                        : "Idle";
                }
                else if (blueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                {
                    type = ActivityType.Research;
                    processDetails = string.IsNullOrEmpty(structure.ResearchingBlueprintUUID)
                        ? "No blueprint assigned"
                        : "Idle";
                }
                else if (blueprint.BluePrintType == BlueprintTypes.Manufactory)
                {
                    type = ActivityType.Manufacturing;
                    processDetails = string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)
                        ? "No blueprint assigned"
                        : "Idle";
                }
                else if (blueprint.BluePrintType.IsCommodityFactory())
                {
                    type = ActivityType.CommodityManufacturing;
                    processDetails = string.IsNullOrEmpty(structure.ManufacturingCommodityName)
                        ? "No commodity assigned"
                        : "Idle";
                }

                if (type == null) continue;

                rows.Add(new ActivityRow
                {
                    Type = type.Value,
                    SystemName = colony.SystemName,
                    ColonyName = colony.ColonyName,
                    SourceName = sourceName,
                    ProcessDetails = processDetails,
                    CountDown = null,
                    NeedBy = DateTime.MinValue
                });
            }
        }

        /// <summary>
        /// Returns the per-cycle consumption rate for a refiner.
        /// Normal refining: GameConstants.RefiningBaseRate (25).
        /// Synthetic refining: RefiningRecipe.ConsumeRate.
        /// </summary>
        private static int GetRefiningConsumptionRate(ColonyStructure refiner)
        {
            if (!string.IsNullOrEmpty(refiner.RefiningResource) &&
                !string.IsNullOrEmpty(refiner.RefiningResourcePurity))
            {
                var recipe = RefiningRecipes.FindByInput(refiner.RefiningResource, refiner.RefiningResourcePurity);
                if (recipe != null)
                    return recipe.ConsumeRate;
            }

            return GameConstants.RefiningBaseRate;
        }

        /// <summary>
        /// Returns the quantity of a raw resource in the colony warehouse matching
        /// the given resource name and purity.
        /// </summary>
        private static int GetWarehouseStockpile(Colony colony, string resource, string purity)
        {
            return ColonyResourceRateCalculator.GetWarehouseStockpile(colony, resource, purity);
        }

        /// <summary>
        /// Detects active refiners whose total consumption exceeds mining supply
        /// for their resource+purity combination. Flags excess refiners starting
        /// from highest DisplaySequence, with warehouse stockpile sustainability exemption.
        /// A refining group is exempt if the stockpile can sustain the group's excess
        /// consumption for the configured UnderutilizedRefiningStockpileHours threshold.
        /// </summary>
        private static void CollectUnderutilizedRefiners(
            Colony colony, PlayerContext playerContext, List<ActivityRow> rows)
        {
            if (colony.Structures == null)
            {
                return;
            }

            // Collect active refiners
            var activeRefiners = new List<ColonyStructure>();

            foreach (var structure in colony.Structures)
            {
                if (!IsBuiltAndOnline(structure))
                {
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null)
                {
                    continue;
                }

                if (blueprint.BluePrintType == BlueprintTypes.Refinery)
                {
                    activeRefiners.Add(structure);
                }
            }

            if (activeRefiners.Count == 0)
            {
                return;
            }

            int thresholdHours = PreferencesStore.GetInstance()
                .Preferences.Thresholds.UnderutilizedRefiningStockpileHours;

            // Group active refiners by resource+purity
            var refinerGroups = activeRefiners
                .Where(r => !string.IsNullOrEmpty(r.RefiningResource) &&
                            !string.IsNullOrEmpty(r.RefiningResourcePurity))
                .GroupBy(r => r.RefiningResource + "|" + r.RefiningResourcePurity);

            foreach (var group in refinerGroups)
            {
                var refinersInGroup = group.ToList();
                string resource = refinersInGroup[0].RefiningResource;
                string purity = refinersInGroup[0].RefiningResourcePurity;

                // Use ColonyResourceRateCalculator for rate computations
                decimal totalMiningOutput = ColonyResourceRateCalculator.GetTotalMiningRate(
                    colony, playerContext, resource, purity);
                decimal totalConsumption = ColonyResourceRateCalculator.GetTotalRefiningConsumption(
                    colony, playerContext, resource, purity);

                int stockpile = GetWarehouseStockpile(colony, resource, purity);

                // If mining output meets or exceeds consumption, no underutilization (Req 2.4)
                if (totalConsumption <= totalMiningOutput)
                {
                    continue;
                }

                // Per-group sustainability check (Req 2.1, 2.2, 2.3)
                decimal excessConsumption = totalConsumption - totalMiningOutput;
                decimal requiredStockpile = excessConsumption * thresholdHours;

                if (stockpile >= requiredStockpile)
                {
                    continue; // Group exempt — stockpile sustains excess consumption
                }

                // Calculate how much supply each refiner gets, in priority order
                var priorityOrder = CollectionSortHelper.OrderStructures(refinersInGroup);
                var refinerAvailable = new Dictionary<string, decimal>();

                decimal supply = totalMiningOutput;
                foreach (var refiner in priorityOrder)
                {
                    int consumeRate = GetRefiningConsumptionRate(refiner);
                    decimal available = Math.Min(supply, consumeRate);
                    refinerAvailable[refiner.UUID] = available;
                    supply = Math.Max(0m, supply - consumeRate);
                }

                // Flag refiners where available < consumeRate, starting from highest BuildQueueSequence
                var sortedRefiners = CollectionSortHelper.OrderStructuresDescending(refinersInGroup);
                foreach (var refiner in sortedRefiners)
                {
                    int consumeRate = GetRefiningConsumptionRate(refiner);
                    decimal available = refinerAvailable[refiner.UUID];

                    if (available >= consumeRate)
                    {
                        continue; // Fully supplied
                    }

                    // Flag as underutilized
                    string sourceName = BuildSourceName(refiner, playerContext);
                    if (sourceName == null)
                    {
                        continue;
                    }

                    int availableInt = (int)Math.Floor((double)available);
                    rows.Add(new ActivityRow
                    {
                        Type = ActivityType.Refining,
                        SystemName = colony.SystemName,
                        ColonyName = colony.ColonyName,
                        SourceName = sourceName,
                        ProcessDetails = $"Underutilized: {availableInt}/{consumeRate} per cycle",
                        CountDown = null,
                        NeedBy = DateTime.MinValue
                    });
                }
            }
        }
    }
}
