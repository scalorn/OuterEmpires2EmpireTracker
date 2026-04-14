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
            if (blueprint.Properties.getDecimal(GameConstants.PropPowerProvided, 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal(GameConstants.PropHabitationProvision, 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal(GameConstants.PropFoodProvision, 0, out val) && val > 0) return true;
            if (blueprint.Properties.getDecimal(GameConstants.PropEntertainmentProvided, 0, out val) && val > 0) return true;
            return false;
        }

        /// <summary>
        /// Optimizes the build order of the colony's structures.
        /// 
        /// Algorithm:
        /// 1. Place CC first, then all primaries in order.
        /// 2. Walk the list simulating each structure. At the first deficit,
        ///    insert the highest-priority support (power > hab > food > ent) at that position.
        /// 3. Restart the walk from the beginning (insertion changes subsequent positions).
        /// 4. Repeat until a full walk completes with no deficits (except the acceptable
        ///    initial entertainment deficit from the CC).
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var primaryPool = new List<ColonyStructure>();
            var supportPool = new List<ColonyStructure>();

            foreach (var structure in colony.Structures)
            {
                Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null)
                {
                    Log.Info("Optimizer: structure UUID={0} flatpack={1} -- blueprint not found, treating as primary",
                        structure.UUID, structure.FlatpackBlueprintUUID);
                    primaryPool.Add(structure);
                }
                else if (IsSupportStructure(bp))
                {
                    Log.Info("Optimizer: structure UUID={0} '{1}' -- classified as SUPPORT",
                        structure.UUID, bp.ExtendedName);
                    supportPool.Add(structure);
                }
                else
                {
                    Log.Info("Optimizer: structure UUID={0} '{1}' -- classified as PRIMARY",
                        structure.UUID, bp.ExtendedName);
                    primaryPool.Add(structure);
                }
            }

            Log.Info("Optimizer pools: {0} primary, {1} support", primaryPool.Count, supportPool.Count);

            // Log input order
            Log.Info("Optimizer INPUT order:");
            for (int i = 0; i < colony.Structures.Count; i++)
            {
                var s = colony.Structures[i];
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                Log.Info("  [{0}] {1} (UUID={2})", i, bp?.ExtendedName ?? s.FlatpackBlueprintUUID, s.UUID);
            }

            // Step 1: Build initial list -- CC first, then all primaries
            var result = new List<ColonyStructure>();
            _currentResult = result;

            var commandCentres = primaryPool
                .Concat(supportPool)
                .Where(s =>
                {
                    var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                    return bp != null && bp.BluePrintType == "Flatpacks/ColonyCommandCentre";
                })
                .ToList();
            foreach (var cc in commandCentres)
            {
                primaryPool.Remove(cc);
                supportPool.Remove(cc);
                result.Add(cc);
                Log.Info("Optimizer: placed Colony Command Centre first (UUID={0})", cc.UUID);
            }

            // Add all primaries after CC
            result.AddRange(primaryPool);

            // Step 2-4: Iteratively walk and insert support until no deficits
            var idealWorkers = new IdealColonyStructureWorkers();
            int maxIterations = 500; // safety limit
            int iteration = 0;

            // Track the entertainment deficit that exists from the CC.
            // We allow entertainment to be in deficit as long as it's not WORSE
            // than the baseline. This prevents the optimizer from endlessly
            // inserting Entertainment Centres + Reactors at the start.
            decimal baselineEntDeficit = 0;
            {
                ColonyStructureStatus ccStatus = SimulateStatus(result, idealWorkers);
                if (ccStatus.EntertainmentRequired > ccStatus.EntertainmentProvided)
                    baselineEntDeficit = ccStatus.EntertainmentRequired - ccStatus.EntertainmentProvided;
            }

            while (iteration < maxIterations)
            {
                iteration++;
                int insertionIndex = -1;
                ColonyStructureStatus deficitStatus = null;

                // Walk the list, simulating each structure
                ColonyStructureStatus prev = new ColonyStructureStatus();
                var calculator = new ColonyStatusCalculator(new Colony());

                for (int i = 0; i < result.Count; i++)
                {
                    var structure = result[i];
                    Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                    ColonyStructureStatus current = new ColonyStructureStatus();
                    calculator.CalculateBuilt(structure, prev, current, idealWorkers, bp);

                    if (HasDeficitExcludingBaselineEnt(current, baselineEntDeficit))
                    {
                        insertionIndex = i;
                        deficitStatus = current;
                        Log.Debug("Iteration {0}: deficit at [{1}] {2} -- PwrR={3} PwrP={4} HabR={5} HabP={6} FoodR={7} FoodP={8} EntR={9} EntP={10}",
                            iteration, i, bp?.ExtendedName ?? structure.FlatpackBlueprintUUID,
                            current.PowerRequired, current.PowerProvided,
                            current.HabitationRequired, current.HabitationProvision,
                            current.FoodRequired, current.FoodProvision,
                            current.EntertainmentRequired, current.EntertainmentProvided);
                        break;
                    }
                    prev = current;
                }

                if (insertionIndex < 0)
                {
                    Log.Info("Optimizer: no deficits found after {0} iteration(s)", iteration);
                    break;
                }

                // Find or create a support structure to fix this deficit
                ColonyStructure support = FindBestSupport(supportPool, deficitStatus, idealWorkers, prev);
                if (support != null)
                {
                    supportPool.Remove(support);
                }
                else
                {
                    support = CreateSupportStructure(deficitStatus);
                    if (support == null)
                    {
                        Log.Warn("Optimizer: cannot resolve deficit at [{0}] -- no suitable blueprint. Stopping.", insertionIndex);
                        break;
                    }
                }

                Blueprint supportBp = _playerContext.FindBlueprint(support.FlatpackBlueprintUUID);
                Log.Info("Iteration {0}: inserting '{1}' at [{2}]",
                    iteration, supportBp?.ExtendedName ?? support.FlatpackBlueprintUUID, insertionIndex);
                result.Insert(insertionIndex, support);
            }

            if (iteration >= maxIterations)
                Log.Warn("Optimizer: hit iteration limit ({0})", maxIterations);

            // Append any remaining unused support at the end
            int leftoverCount = supportPool.Count;
            result.AddRange(supportPool);

            Log.Info("Build order optimized: {0} structures in {1} iterations ({2} leftover support)",
                result.Count, iteration, leftoverCount);

            // Log output order
            Log.Info("Optimizer OUTPUT order:");
            for (int i = 0; i < result.Count; i++)
            {
                var s = result[i];
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                Log.Info("  [{0}] {1} (UUID={2})", i, bp?.ExtendedName ?? s.FlatpackBlueprintUUID, s.UUID);
            }

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
        /// Returns true if there's a deficit in power, hab, or food, OR if the
        /// entertainment deficit exceeds the baseline (inherited from CC).
        /// The baseline entertainment deficit is tolerated at the start of the
        /// build order and will be resolved naturally as structures are added.
        /// </summary>
        private bool HasDeficitExcludingBaselineEnt(ColonyStructureStatus status, decimal baselineEntDeficit)
        {
            if (status.PowerRequired > status.PowerProvided) return true;
            if (status.HabitationRequired > status.HabitationProvision) return true;
            if (status.FoodRequired > status.FoodProvision) return true;

            // Entertainment: only flag if the deficit is WORSE than the baseline
            decimal entDeficit = status.EntertainmentRequired - status.EntertainmentProvided;
            if (entDeficit > baselineEntDeficit) return true;

            return false;
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
                    bp.Properties.getDecimal(GameConstants.PropPowerProvided, 0, out val) && val > 0)
                    score += 4;

                if (afterPrimary.HabitationRequired > afterPrimary.HabitationProvision &&
                    bp.Properties.getDecimal(GameConstants.PropHabitationProvision, 0, out val) && val > 0)
                    score += 3;

                if (afterPrimary.FoodRequired > afterPrimary.FoodProvision &&
                    bp.Properties.getDecimal(GameConstants.PropFoodProvision, 0, out val) && val > 0)
                    score += 2;

                if (afterPrimary.EntertainmentRequired > afterPrimary.EntertainmentProvided &&
                    bp.Properties.getDecimal(GameConstants.PropEntertainmentProvided, 0, out val) && val > 0)
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
                deficitProperties = new[] { GameConstants.PropPowerProvided };
            else if (afterPrimary.HabitationRequired > afterPrimary.HabitationProvision)
                deficitProperties = new[] { GameConstants.PropHabitationProvision };
            else if (afterPrimary.FoodRequired > afterPrimary.FoodProvision)
                deficitProperties = new[] { GameConstants.PropFoodProvision };
            else if (afterPrimary.EntertainmentRequired > afterPrimary.EntertainmentProvided)
                deficitProperties = new[] { GameConstants.PropEntertainmentProvided };
            else
                return null;

            // Find a blueprint that provides this resource (player + global)
            foreach (var bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                if (!bp.BluePrintType.IsFlatpack()) continue;

                // Check MaxPerColony limit
                long maxPerColony = 0;
                bp.Properties.getLong(GameConstants.PropMaxPerColony, 0, out maxPerColony);
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
