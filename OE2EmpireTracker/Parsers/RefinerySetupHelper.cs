using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Static helper for automating refinery setup during colony import/reimport.
    /// Handles warehouse resource seeding and timer start for refineries.
    /// </summary>
    public static class RefinerySetupHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Runs warehouse resource seeding and timer setup for all refineries in the colony.
        /// Called after the merge loop in ParseColonyBuildingsFromJson.
        /// </summary>
        public static void SetupRefineries(Colony colony, EmpireContext empireContext)
        {
            if (colony == null || empireContext == null)
            {
                return;
            }

            int timersStarted = 0;
            int warehouseResourcesCreated = 0;

            Log.Info("SetupRefineries starting for colony {0}: {1} structures to scan",
                colony.PlanetName, colony.Structures.Count);

            foreach (var structure in colony.Structures)
            {
                // Identify refineries by non-empty RefiningResource AND RefiningResourcePurity
                if (string.IsNullOrEmpty(structure.RefiningResource) ||
                    string.IsNullOrEmpty(structure.RefiningResourcePurity))
                {
                    // Log why this structure was skipped -- helps diagnose import issues
                    if (!string.IsNullOrEmpty(structure.RefiningResource) ||
                        !string.IsNullOrEmpty(structure.RefiningResourcePurity) ||
                        !string.IsNullOrEmpty(structure.MiningSurveyResource))
                    {
                        Log.Debug("Skipped structure {0} (FlatpackBP={1}): RefiningResource='{2}', RefiningResourcePurity='{3}', MiningSurveyResource='{4}'",
                            structure.UUID,
                            structure.FlatpackBlueprintUUID ?? "(null)",
                            structure.RefiningResource ?? "(null)",
                            structure.RefiningResourcePurity ?? "(null)",
                            structure.MiningSurveyResource ?? "(null)");
                    }

                    continue;
                }

                Log.Info("Processing refinery {0} (FlatpackBP={1}): RefiningResource='{2}', RefiningResourcePurity='{3}'",
                    structure.UUID,
                    structure.FlatpackBlueprintUUID ?? "(null)",
                    structure.RefiningResource,
                    structure.RefiningResourcePurity);

                // Step a: Ensure warehouse resource exists (Req 8.1, 8.2)
                int itemCountBefore = colony.Items.Count();
                MinerSetupHelper.EnsureWarehouseResource(colony, structure.RefiningResource, structure.RefiningResourcePurity);
                if (colony.Items.Count() > itemCountBefore)
                {
                    warehouseResourcesCreated++;
                }

                // Step b: Check if built and online (Req 8.3)
                if (!structure.IsBuiltAndOnline)
                {
                    Log.Info("Skipped timer for refinery {0}: not built or not online",
                        structure.UUID);
                    continue;
                }

                // Step c: Preserve existing active repeating timer (Req 8.4)
                if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IsRepeating)
                {
                    Log.Info("Preserved existing active timer on refinery {0}", structure.UUID);
                    continue;
                }

                // Step d: Create repeating timer aligned to next clock-hour boundary (Req 8.3)
                // Reuse MinerSetupHelper.SetupTimer with a non-zero rate to trigger timer creation
                MinerSetupHelper.SetupTimer(structure, 1m);
                if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IsRepeating)
                {
                    timersStarted++;
                }
            }

            Log.Info("SetupRefineries complete for colony {0}: {1} timers started, {2} warehouse resources created",
                colony.PlanetName, timersStarted, warehouseResourcesCreated);
        }
    }
}
