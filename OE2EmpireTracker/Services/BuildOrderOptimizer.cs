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
        /// 1. Seed with CC + Reactor + Hab + Hydro + Ent (the bootstrap set that
        ///    ensures no deficits from the start), then all primaries.
        /// 2. Walk the list simulating each structure. At the first structure where
        ///    a deficit gets worse, insert the highest-priority support at that position.
        /// 3. Restart the walk from the beginning.
        /// 4. Repeat until a full walk completes with no worsening deficits.
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

            // Step 1: Seed the result with the bootstrap set.
            // Every colony starts: CC -> Reactor -> Hab -> Hydro -> Ent
            // This resolves all CC deficits before any primaries are placed.
            var result = new List<ColonyStructure>();
            _currentResult = result;

            SeedBootstrap(result, primaryPool, supportPool, "Flatpacks/ColonyCommandCentre");
            SeedBootstrap(result, primaryPool, supportPool, "Flatpacks/ReactorCore");
            SeedBootstrap(result, primaryPool, supportPool, "Flatpacks/HabitationBlock");
            SeedBootstrap(result, primaryPool, supportPool, "Flatpacks/HydroponicsBay");
            SeedBootstrap(result, primaryPool, supportPool, "Flatpacks/EntertainmentCentreFlatpack");

            Log.Info("Bootstrap: {0} structures seeded", result.Count);

            // Add all primaries after the bootstrap set
            result.AddRange(primaryPool);

            // Step 2-4: Iteratively walk and insert support until no deficits
            var idealWorkers = new IdealColonyStructureWorkers();
            int maxIterations = 500; // safety limit
            int iteration = 0;

            // All deficits must be resolved. The bootstrap set handles the CC's
            // initial deficits. The walk uses IsWorseDeficit to find the first
            // structure that makes any resource deficit worse, then inserts the
            // highest-priority support at that position.

            while (iteration < maxIterations)
            {
                iteration++;
                int insertionIndex = -1;
                ColonyStructureStatus deficitStatus = null;

                // Walk the full list simulating each structure
                ColonyStructureStatus prev = new ColonyStructureStatus();
                var calculator = new ColonyStatusCalculator(new Colony());

                for (int i = 0; i < result.Count; i++)
                {
                    var structure = result[i];
                    Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                    ColonyStructureStatus current = new ColonyStructureStatus();
                    calculator.CalculateBuilt(structure, prev, current, idealWorkers, bp);

                    // Only flag a deficit if this structure made things WORSE.
                    // Inherited deficits (same or better than prev) are not actionable
                    // at this position -- they need to be fixed earlier in the list.
                    // On the first pass they'll be caught when the structure that
                    // originally caused them is reached.
                    if (IsWorseDeficit(prev, current))
                    {
                        insertionIndex = i;
                        deficitStatus = current;
                        Log.Info("Iteration {0}: deficit at [{1}] {2} -- PwrR={3} PwrP={4} HabR={5} HabP={6} FoodR={7} FoodP={8} EntR={9} EntP={10}",
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

        /// <summary>
        /// Pulls one structure of the given blueprint type from the support pool
        /// (or primary pool for CC) and adds it to the result. If none exists in
        /// the pools, creates one from player blueprints.
        /// </summary>
        private void SeedBootstrap(List<ColonyStructure> result,
            List<ColonyStructure> primaryPool, List<ColonyStructure> supportPool,
            string blueprintType)
        {
            // Try support pool first, then primary pool
            var match = supportPool.FirstOrDefault(s =>
            {
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                return bp != null && bp.BluePrintType == blueprintType;
            });
            if (match != null)
            {
                supportPool.Remove(match);
                result.Add(match);
                Log.Info("Bootstrap: seeded '{0}' from support pool", blueprintType);
                return;
            }

            match = primaryPool.FirstOrDefault(s =>
            {
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                return bp != null && bp.BluePrintType == blueprintType;
            });
            if (match != null)
            {
                primaryPool.Remove(match);
                result.Add(match);
                Log.Info("Bootstrap: seeded '{0}' from primary pool", blueprintType);
                return;
            }

            // Not in any pool -- create from player blueprints
            foreach (var bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID != null && bp.BluePrintType == blueprintType)
                {
                    var created = new ColonyStructure
                    {
                        UUID = Guid.NewGuid().ToString(),
                        FlatpackBlueprintUUID = bp.UUID
                    };
                    result.Add(created);
                    Log.Info("Bootstrap: created '{0}' from blueprint {1}", blueprintType, bp.ExtendedName);
                    return;
                }
            }

            Log.Warn("Bootstrap: no blueprint found for '{0}'", blueprintType);
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
        /// Returns true if 'current' has a deficit that is WORSE than 'prev'.
        /// A deficit that was already present in prev and hasn't gotten worse
        /// is not actionable at this position -- it's inherited.
        /// </summary>
        private bool IsWorseDeficit(ColonyStructureStatus prev, ColonyStructureStatus current)
        {
            // Power: new deficit or existing deficit got worse
            decimal prevPowerGap = prev.PowerRequired - prev.PowerProvided;
            decimal currPowerGap = current.PowerRequired - current.PowerProvided;
            if (currPowerGap > 0 && currPowerGap > prevPowerGap) return true;

            // Habitation
            decimal prevHabGap = prev.HabitationRequired - prev.HabitationProvision;
            decimal currHabGap = current.HabitationRequired - current.HabitationProvision;
            if (currHabGap > 0 && currHabGap > prevHabGap) return true;

            // Food
            decimal prevFoodGap = prev.FoodRequired - prev.FoodProvision;
            decimal currFoodGap = current.FoodRequired - current.FoodProvision;
            if (currFoodGap > 0 && currFoodGap > prevFoodGap) return true;

            // Entertainment
            decimal prevEntGap = prev.EntertainmentRequired - prev.EntertainmentProvided;
            decimal currEntGap = current.EntertainmentRequired - current.EntertainmentProvided;
            if (currEntGap > 0 && currEntGap > prevEntGap) return true;

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
