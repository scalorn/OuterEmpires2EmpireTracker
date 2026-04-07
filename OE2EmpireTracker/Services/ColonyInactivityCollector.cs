using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Services
{
    public static class ColonyInactivityCollector
    {
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
            }

            return rows;
        }

        /// <summary>
        /// Returns true if the structure's PropertyBag has Built=True and Online=True.
        /// </summary>
        private static bool IsBuiltAndOnline(ColonyStructure structure)
        {
            bool built;
            structure.Properties.getBoolean(GameConstants.PropBuilt, false, out built);
            if (!built) return false;

            bool online;
            structure.Properties.getBoolean(GameConstants.PropOnline, false, out online);
            return online;
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
        /// Formats the source name as "#{gameSequence} {blueprint.ExtendedName}".
        /// Returns null if the blueprint cannot be found.
        /// </summary>
        private static string BuildSourceName(ColonyStructure structure, PlayerContext playerContext)
        {
            Blueprint blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
            if (blueprint == null) return null;

            return $"#{structure.gameSequence} {blueprint.ExtendedName}";
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

                Blueprint blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null) continue;

                string sourceName = $"#{structure.gameSequence} {blueprint.ExtendedName}";

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
                else if (blueprint.BluePrintType == BlueprintTypes.CommodityFactory)
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
    }
}
