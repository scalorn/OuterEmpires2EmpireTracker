using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Optimizes the build order of colony structures so that Power, Habitation,
    /// Food, and Entertainment constraints are satisfied after each build step.
    /// </summary>
    public class BuildOrderOptimizer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly PlayerContext _playerContext;
        private List<ColonyStructure> _currentResult;

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

            decimal val;
            if (blueprint.Properties.getDecimal("PowerProvided", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal("HabitationProvision", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal("FoodProvision", 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal("EntertainmentProvided", 0, out val) && val > 0) return true;
            return false;
        }

        /// <summary>
        /// Optimizes the build order of the colony's structures.
        /// Only reorders staged/unbuilt structures. Built structures stay in place.
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var result = new List<ColonyStructure>();
            _currentResult = result;
            var builtStructures = new List<ColonyStructure>();
            var primaryPool = new List<ColonyStructure>();
            var supportPool = new List<ColonyStructure>();

            // Separate structures into built (keep in place) and unbuilt (to optimize)
            foreach (var structure in colony.Structures)
            {
                bool built = false;
                structure.Properties.getBoolean(GameConstants.PropBuilt, false, out built);
                if (built)
                {
                    builtStructures.Add(structure);
                    continue;
                }

                Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
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

            Log.Info("Optimizer pools: {0} built, {1} primary, {2} support",
                builtStructures.Count, primaryPool.Count, supportPool.Count);

            // Simulate building each primary structure, inserting support as needed
            var idealWorkers = new IdealColonyStructureWorkers();
            ColonyStructureStatus runningStatus = SimulateStatus(result, idealWorkers);

            // Check if built structures already have deficits that need support
            while (HasDeficit(runningStatus))
            {
                ColonyStructure bestSupport = FindBestSupport(supportPool, runningStatus, idealWorkers, runningStatus);
                if (bestSupport != null)
                {
                    supportPool.Remove(bestSupport);
                }
                else
                {
                    bestSupport = CreateSupportStructure(runningStatus);
                    if (bestSupport == null) break;
                }
                result.Add(bestSupport);
                Blueprint supportBp = _playerContext.FindBlueprint(bestSupport.FlatpackBlueprintUUID);
                runningStatus = SimulateOneMore(runningStatus, bestSupport, supportBp, idealWorkers);
            }

            foreach (var primary in primaryPool)
            {
                // Simulate adding this primary structure
                Blueprint primaryBp = _playerContext.FindBlueprint(primary.FlatpackBlueprintUUID);
                ColonyStructureStatus afterPrimary = SimulateOneMore(runningStatus, primary, primaryBp, idealWorkers);

                Log.Debug("Primary {0}: PowerReq={1} PowerProv={2} HabReq={3} HabProv={4} FoodReq={5} FoodProv={6}",
                    primaryBp?.ExtendedName ?? primary.FlatpackBlueprintUUID,
                    afterPrimary.PowerRequired, afterPrimary.PowerProvided,
                    afterPrimary.HabitationRequired, afterPrimary.HabitationProvision,
                    afterPrimary.FoodRequired, afterPrimary.FoodProvision);

                // Check for deficits and insert support structures to fix them
                while (HasDeficit(afterPrimary))
                {
                    ColonyStructure bestSupport = FindBestSupport(supportPool, afterPrimary, idealWorkers, runningStatus);

                    if (bestSupport != null)
                    {
                        supportPool.Remove(bestSupport);
                    }
                    else
                    {
                        // No existing support structure can help — create one from player blueprints
                        bestSupport = CreateSupportStructure(afterPrimary);
                        if (bestSupport == null)
                        {
                            Log.Warn("No support structure available for deficit. Breaking.");
                            break;
                        }
                    }

                    result.Add(bestSupport);

                    // Update running status with the support structure
                    Blueprint supportBp = _playerContext.FindBlueprint(bestSupport.FlatpackBlueprintUUID);
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
                Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
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
                Blueprint bp = _playerContext.FindBlueprint(candidate.FlatpackBlueprintUUID);
                if (bp == null) continue;

                int score = 0;
                decimal val;

                // Score based on which deficits this support addresses
                if (afterPrimary.PowerRequired > afterPrimary.PowerProvided &&
                    bp.Properties.getDecimal("PowerProvided", 0, out val) && val > 0)
                    score += 4;

                if (afterPrimary.HabitationRequired > afterPrimary.HabitationProvision &&
                    bp.Properties.getDecimal("HabitationProvision", 0, out val) && val > 0)
                    score += 3;

                if (afterPrimary.FoodRequired > afterPrimary.FoodProvision &&
                    bp.Properties.getDecimal("FoodProvision", 0, out val) && val > 0)
                    score += 2;

                if (afterPrimary.EntertainmentRequired > afterPrimary.EntertainmentProvided &&
                    bp.Properties.getDecimal("EntertainmentProvided", 0, out val) && val > 0)
                    score += 1;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Creates a new support structure from player blueprints to address the deficit.
        /// Returns null if no suitable blueprint is available.
        /// </summary>
        private ColonyStructure CreateSupportStructure(ColonyStructureStatus afterPrimary)
        {
            // Determine which deficit to address (priority order)
            string[] deficitProperties;
            if (afterPrimary.PowerRequired > afterPrimary.PowerProvided)
                deficitProperties = new[] { "PowerProvided" };
            else if (afterPrimary.HabitationRequired > afterPrimary.HabitationProvision)
                deficitProperties = new[] { "HabitationProvision" };
            else if (afterPrimary.FoodRequired > afterPrimary.FoodProvision)
                deficitProperties = new[] { "FoodProvision" };
            else if (afterPrimary.EntertainmentRequired > afterPrimary.EntertainmentProvided)
                deficitProperties = new[] { "EntertainmentProvided" };
            else
                return null;

            // Find a blueprint that provides this resource (player + global)
            foreach (var bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                if (!bp.BluePrintType.IsFlatpack()) continue;

                // Check MaxPerColony limit
                long maxPerColony = 0;
                bp.Properties.getLong("MaxPerColony", 0, out maxPerColony);
                if (maxPerColony > 0)
                {
                    int currentCount = _currentResult.Count(s => s.FlatpackBlueprintUUID == bp.UUID);
                    if (currentCount >= maxPerColony) continue;
                }

                decimal val;
                foreach (string prop in deficitProperties)
                {
                    if (bp.Properties.getDecimal(prop, 0, out val) && val > 0)
                    {
                        Log.Info("Auto-creating support structure: {0} (provides {1})", bp.ExtendedName, prop);
                        return new ColonyStructure
                        {
                            UUID = System.Guid.NewGuid().ToString(),
                            FlatpackBlueprintUUID = bp.UUID
                        };
                    }
                }
            }

            Log.Warn("No player blueprint found to address deficit");
            return null;
        }
    }
}
