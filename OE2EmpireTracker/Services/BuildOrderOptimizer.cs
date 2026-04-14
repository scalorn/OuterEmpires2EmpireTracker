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
        /// Only reorders staged/unbuilt structures. Built structures stay in place.
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var result = new List<ColonyStructure>();
            _currentResult = result;
            var primaryPool = new List<ColonyStructure>();
            var supportPool = new List<ColonyStructure>();

            // All structures go through the optimizer -- built structures are not kept
            // in their original order because the importer groups them by flatpack type,
            // not by the order they were actually built. The optimizer produces the best
            // guess at a correct build order.
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

            Log.Info("Optimizer pools: {0} primary, {1} support (all structures optimized)",
                primaryPool.Count, supportPool.Count);

            // Colony Command Centre must always be first -- it's the foundation of every colony.
            // Check both pools since it provides habitation/food and gets classified as support.
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

            // Log input order
            Log.Info("Optimizer INPUT order:");
            for (int i = 0; i < colony.Structures.Count; i++)
            {
                var s = colony.Structures[i];
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                Log.Info("  [{0}] {1} (UUID={2})", i, bp?.ExtendedName ?? s.FlatpackBlueprintUUID, s.UUID);
            }

            // Simulate building each primary structure, inserting support as needed
            var idealWorkers = new IdealColonyStructureWorkers();
            ColonyStructureStatus runningStatus = SimulateStatus(result, idealWorkers);

            // NOTE: We do NOT fix deficits from the CC here. The CC's entertainment
            // deficit is inherent and will be resolved naturally when the first primary
            // triggers support insertion. Fixing it eagerly would place Entertainment
            // Centre before Hab Block, which is wrong -- Hab can be built first without
            // making things worse.

            foreach (var primary in primaryPool)
            {
                // Simulate adding this primary structure
                Blueprint primaryBp = _playerContext.FindBlueprint(primary.FlatpackBlueprintUUID);
                ColonyStructureStatus afterPrimary = SimulateOneMore(runningStatus, primary, primaryBp, idealWorkers);

                Log.Info("Primary {0}: PowerReq={1} PowerProv={2} HabReq={3} HabProv={4} FoodReq={5} FoodProv={6}",
                    primaryBp?.ExtendedName ?? primary.FlatpackBlueprintUUID,
                    afterPrimary.PowerRequired, afterPrimary.PowerProvided,
                    afterPrimary.HabitationRequired, afterPrimary.HabitationProvision,
                    afterPrimary.FoodRequired, afterPrimary.FoodProvision);

                // Check for deficits and insert support structures to fix them.
                // We check BOTH afterPrimary (the state after the primary is added) AND
                // runningStatus (the state after each support insertion). Support structures
                // like Entertainment Centre require power and have workers, so they can
                // create new deficits at their own position in the build order.
                while (HasDeficit(afterPrimary) || HasDeficit(runningStatus))
                {
                    // Determine which status to fix -- prioritize runningStatus deficits
                    // since those represent structures already placed in the build order
                    ColonyStructureStatus deficitStatus = HasDeficit(runningStatus) ? runningStatus : afterPrimary;

                    ColonyStructure bestSupport = FindBestSupport(supportPool, deficitStatus, idealWorkers, runningStatus);

                    if (bestSupport != null)
                    {
                        supportPool.Remove(bestSupport);
                    }
                    else
                    {
                        // No existing support structure can help -- create one from player blueprints
                        bestSupport = CreateSupportStructure(deficitStatus);
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
                    Log.Info("Inserted support '{0}' before primary '{1}' -- running: PwrR={2} PwrP={3} HabR={4} HabP={5} FoodR={6} FoodP={7} EntR={8} EntP={9}",
                        supportBp?.ExtendedName ?? bestSupport.FlatpackBlueprintUUID,
                        primaryBp?.ExtendedName ?? primary.FlatpackBlueprintUUID,
                        runningStatus.PowerRequired, runningStatus.PowerProvided,
                        runningStatus.HabitationRequired, runningStatus.HabitationProvision,
                        runningStatus.FoodRequired, runningStatus.FoodProvision,
                        runningStatus.EntertainmentRequired, runningStatus.EntertainmentProvided);

                    // Re-simulate primary after adding support
                    afterPrimary = SimulateOneMore(runningStatus, primary, primaryBp, idealWorkers);
                    Log.Info("  afterPrimary: PwrR={0} PwrP={1} HabR={2} HabP={3} FoodR={4} FoodP={5} EntR={6} EntP={7} deficit={8}",
                        afterPrimary.PowerRequired, afterPrimary.PowerProvided,
                        afterPrimary.HabitationRequired, afterPrimary.HabitationProvision,
                        afterPrimary.FoodRequired, afterPrimary.FoodProvision,
                        afterPrimary.EntertainmentRequired, afterPrimary.EntertainmentProvided,
                        HasDeficit(afterPrimary));
                }

                result.Add(primary);
                runningStatus = afterPrimary;
            }

            // Append remaining unused support structures at the end
            foreach (var leftover in supportPool)
            {
                result.Add(leftover);
                Blueprint leftoverBp = _playerContext.FindBlueprint(leftover.FlatpackBlueprintUUID);
                runningStatus = SimulateOneMore(runningStatus, leftover, leftoverBp, idealWorkers);
            }
            int leftoverCount = supportPool.Count;
            supportPool.Clear();

            // Fill remaining deficits by creating new support structures from player blueprints.
            // This handles the case where the user added staged primaries that need more support
            // than what was in the original pool.
            int created = 0;
            while (HasDeficit(runningStatus))
            {
                ColonyStructure newSupport = CreateSupportStructure(runningStatus);
                if (newSupport == null)
                {
                    Log.Warn("Cannot fill remaining deficit -- no suitable blueprint found. " +
                        "PowerReq={0} PowerProv={1} HabReq={2} HabProv={3} FoodReq={4} FoodProv={5} EntReq={6} EntProv={7}",
                        runningStatus.PowerRequired, runningStatus.PowerProvided,
                        runningStatus.HabitationRequired, runningStatus.HabitationProvision,
                        runningStatus.FoodRequired, runningStatus.FoodProvision,
                        runningStatus.EntertainmentRequired, runningStatus.EntertainmentProvided);
                    break;
                }
                result.Add(newSupport);
                Blueprint newBp = _playerContext.FindBlueprint(newSupport.FlatpackBlueprintUUID);
                Log.Info("Created support to fill deficit: {0}", newBp?.ExtendedName ?? newSupport.FlatpackBlueprintUUID);
                runningStatus = SimulateOneMore(runningStatus, newSupport, newBp, idealWorkers);
                created++;
            }
            if (created > 0)
                Log.Info("Created {0} additional support structure(s) to fill deficits", created);

            Log.Info("Build order optimized: {0} structures ({1} primary, {2} leftover support, {3} created)",
                result.Count, primaryPool.Count, leftoverCount, created);

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
