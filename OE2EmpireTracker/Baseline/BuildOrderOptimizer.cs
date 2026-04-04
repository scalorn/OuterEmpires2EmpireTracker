using NLog;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Baseline
{
    /// <summary>
    /// Optimizes the build order of colony structures so that Power, Habitation,
    /// Food, and Entertainment constraints are satisfied after each build step.
    /// </summary>
    public class BuildOrderOptimizer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly PlayerContext _playerContext;

        public BuildOrderOptimizer(PlayerContext playerContext)
        {
            _playerContext = playerContext;
        }

        /// <summary>
        /// Returns true if the blueprint provides any support resource
        /// (Power, Habitation, Food, Entertainment).
        /// </summary>
        public bool IsSupportStructure(Blueprint blueprint)
        {
            if (blueprint == null) return false;

            double val;
            if (blueprint.Properties.getDouble("PowerProvided", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDouble("HabitationProvision", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDouble("FoodProvision", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDouble("EntertainmentProvided", 0, out val) && val > 0) return true;
            return false;
        }

        /// <summary>
        /// Optimizes the build order of the colony's structures.
        /// Only reorders staged/unbuilt structures. Built structures stay in place.
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var result = new List<ColonyStructure>();
            var builtStructures = new List<ColonyStructure>();
            var primaryPool = new List<ColonyStructure>();
            var supportPool = new List<ColonyStructure>();

            // Separate structures into built (keep in place) and unbuilt (to optimize)
            foreach (var structure in colony.Structures)
            {
                bool built = false;
                structure.Properties.getBoolean("Built", false, out built);
                if (built)
                {
                    builtStructures.Add(structure);
                    continue;
                }

                Blueprint bp = _playerContext.findBlueprint(structure.FlatpackBlueprintUUID);
                if (bp != null && IsSupportStructure(bp))
                {
                    supportPool.Add(structure);
                }
                else
                {
                    primaryPool.Add(structure);
                }
            }

            // Start with built structures
            result.AddRange(builtStructures);

            // Simulate building each primary structure, inserting support as needed
            var idealWorkers = new IdealColonyStructureWorkers();
            ColonyStructureStatus runningStatus = SimulateStatus(result, idealWorkers);

            foreach (var primary in primaryPool)
            {
                // Simulate adding this primary structure
                Blueprint primaryBp = _playerContext.findBlueprint(primary.FlatpackBlueprintUUID);
                ColonyStructureStatus afterPrimary = SimulateOneMore(runningStatus, primary, primaryBp, idealWorkers);

                // Check for deficits and insert support structures to fix them
                while (HasDeficit(afterPrimary) && supportPool.Count > 0)
                {
                    ColonyStructure bestSupport = FindBestSupport(supportPool, afterPrimary, idealWorkers, runningStatus);
                    if (bestSupport == null) break; // No support can help

                    supportPool.Remove(bestSupport);
                    result.Add(bestSupport);

                    // Update running status with the support structure
                    Blueprint supportBp = _playerContext.findBlueprint(bestSupport.FlatpackBlueprintUUID);
                    runningStatus = SimulateOneMore(runningStatus, bestSupport, supportBp, idealWorkers);

                    // Re-simulate primary after adding support
                    afterPrimary = SimulateOneMore(runningStatus, primary, primaryBp, idealWorkers);
                }

                result.Add(primary);
                runningStatus = afterPrimary;
            }

            // Append remaining unused support structures at the end
            result.AddRange(supportPool);

            Log.Info("Build order optimized: {0} structures ({1} built, {2} primary, {3} support)",
                result.Count, builtStructures.Count, primaryPool.Count, supportPool.Count);

            return result;
        }

        private ColonyStructureStatus SimulateStatus(List<ColonyStructure> structures, IColonyStructureWorkers workerSource)
        {
            var calculator = new ColonyStatusCalculator(new Colony());
            ColonyStructureStatus prev = new ColonyStructureStatus();

            foreach (var structure in structures)
            {
                Blueprint bp = _playerContext.findBlueprint(structure.FlatpackBlueprintUUID);
                ColonyStructureStatus current = new ColonyStructureStatus();
                calculator.CalculateBuilt(structure, prev, current, workerSource, bp);
                prev = current;
            }
            return prev;
        }

        private ColonyStructureStatus SimulateOneMore(ColonyStructureStatus runningStatus,
            ColonyStructure structure, Blueprint blueprint, IColonyStructureWorkers workerSource)
        {
            var calculator = new ColonyStatusCalculator(new Colony());
            ColonyStructureStatus result = new ColonyStructureStatus();
            calculator.CalculateBuilt(structure, runningStatus, result, workerSource, blueprint);
            return result;
        }

        private bool HasDeficit(ColonyStructureStatus status)
        {
            return status.PowerRequired > status.PowerProvided ||
                   status.HabitationRequired > status.HabitationProvision ||
                   status.FoodRequired > status.FoodProvision ||
                   status.EntertainmentRequired > status.EntertainmentProvided;
        }

        /// <summary>
        /// Finds the support structure that best addresses the current deficit.
        /// Prioritizes: Power > Habitation > Food > Entertainment.
        /// </summary>
        private ColonyStructure FindBestSupport(List<ColonyStructure> supportPool,
            ColonyStructureStatus afterPrimary, IColonyStructureWorkers workerSource,
            ColonyStructureStatus runningStatus)
        {
            ColonyStructure best = null;
            int bestScore = 0;

            foreach (var candidate in supportPool)
            {
                Blueprint bp = _playerContext.findBlueprint(candidate.FlatpackBlueprintUUID);
                if (bp == null) continue;

                int score = 0;
                double val;

                // Score based on which deficits this support addresses
                if (afterPrimary.PowerRequired > afterPrimary.PowerProvided &&
                    bp.Properties.getDouble("PowerProvided", 0, out val) && val > 0)
                    score += 4;

                if (afterPrimary.HabitationRequired > afterPrimary.HabitationProvision &&
                    bp.Properties.getDouble("HabitationProvision", 0, out val) && val > 0)
                    score += 3;

                if (afterPrimary.FoodRequired > afterPrimary.FoodProvision &&
                    bp.Properties.getDouble("FoodProvision", 0, out val) && val > 0)
                    score += 2;

                if (afterPrimary.EntertainmentRequired > afterPrimary.EntertainmentProvided &&
                    bp.Properties.getDouble("EntertainmentProvided", 0, out val) && val > 0)
                    score += 1;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
