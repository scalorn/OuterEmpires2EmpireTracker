using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

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

        public BuildOrderOptimizer(PlayerContext playerContext)
        {
            _playerContext = playerContext;
        }

        public bool IsSupportStructure(ReadOnlyBlueprint blueprint)
        {
            if (blueprint == null) return false;
            decimal val;
            if (blueprint.Properties.GetDecimal(GameConstants.PropPowerProvided, 0, out val) && val > 0) return true;
            if (blueprint.Properties.GetDecimal(GameConstants.PropHabitationProvision, 0, out val) && val > 0) return true;
            if (blueprint.Properties.GetDecimal(GameConstants.PropFoodProvision, 0, out val) && val > 0) return true;
            if (blueprint.Properties.GetDecimal(GameConstants.PropEntertainmentProvided, 0, out val) && val > 0) return true;
            return false;
        }

        /// <summary>
        /// Optimizes the build order.
        ///
        /// 1. Start with CC + Reactor + Hab + Hydro + Ent (no deficits).
        /// 2. For each primary:
        ///    a. Fix any existing deficits by adding support (power > hab > food > ent).
        ///       If a support structure would itself cause a deficit, fix that first.
        ///    b. Look ahead: after placing this primary, would adding a Hab + Hydro
        ///       cause a deficit? If so, pre-place the support needed (Ent, Reactor).
        ///    c. Place the primary.
        /// 3. Append leftover support at the end.
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var primaries = new List<ColonyStructure>();
            var supportPool = new List<ColonyStructure>();

            // Sort by BuildQueueSequence so primaries preserve the user's intended order.
            // The underlying list is already sorted on load (InitColonies), but sort here
            // defensively in case the list was modified after load.
            var orderedStructures = CollectionSortHelper.OrderStructures(colony.Structures);

            foreach (var structure in orderedStructures)
            {
                var bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null)
                    primaries.Add(structure);
                else if (IsSupportStructure(bp) || bp.BluePrintType == BlueprintTypes.ColonyCommandCentre)
                    supportPool.Add(structure);
                else
                    primaries.Add(structure);
            }

            Log.Info("Optimizer: {0} primaries, {1} support", primaries.Count, supportPool.Count);

            var result = new List<ColonyStructure>();
            var idealWorkers = new IdealColonyStructureWorkers();

            // Running accumulator â€” tracks cumulative resource state incrementally.
            // Updated via SimulateOneMore each time a structure is appended to result.
            ColonyStructureStatus accumulator = new ColonyStructureStatus();

            // Bootstrap: CC, Reactor, Hab, Hydro, Ent
            PlaceFromPool(result, supportPool, BlueprintTypes.ColonyCommandCentre);
            PlaceFromPool(result, supportPool, "Flatpacks/ReactorCore");
            PlaceFromPool(result, supportPool, "Flatpacks/HabitationBlock");
            PlaceFromPool(result, supportPool, "Flatpacks/HydroponicsBay");
            PlaceFromPool(result, supportPool, "Flatpacks/EntertainmentCentreFlatpack");

            // Update accumulator for each bootstrap structure
            for (int i = 0; i < result.Count; i++)
            {
                var bp = _playerContext.FindBlueprint(result[i].FlatpackBlueprintUUID);
                accumulator = SimulateOneMore(accumulator, result[i], bp, idealWorkers);
            }

            // Process each primary
            foreach (var primary in primaries)
            {
                // Step A: Fix any existing deficits (accumulator is already current)
                int beforeCount = result.Count;
                FixDeficits(result, supportPool, accumulator, idealWorkers, ref accumulator);
                if (result.Count > beforeCount)
                    Log.Info("Step A: added {0} support before primary", result.Count - beforeCount);

                // Step B: Look ahead -- after placing this primary, would there be
                // a deficit? Also check if adding a hab + hydro after would cause one.
                var primaryBp = _playerContext.FindBlueprint(primary.FlatpackBlueprintUUID);
                ColonyStructureStatus afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);

                // First fix deficits the primary itself would cause
                if (HasDeficit(afterPrimary))
                {
                    FixDeficits(result, supportPool, afterPrimary, idealWorkers, ref accumulator);
                    afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
                }

                // Then look further ahead: after the primary + hab + hydro,
                // would hab/food/ent go into deficit? (Hab/Hydro workers need these.)
                // Don't fix POWER here -- power deficits from future primaries will
                // be handled by their own Step B. Fixing power in the look-ahead
                // double-counts and over-provisions reactors.
                var habBp = FindBlueprintByType("Flatpacks/HabitationBlock");
                var hydroBp = FindBlueprintByType("Flatpacks/HydroponicsBay");
                ColonyStructureStatus afterFutureSupport = afterPrimary;
                if (habBp != null)
                    afterFutureSupport = SimulateOneMore(afterFutureSupport, null, habBp, idealWorkers);
                if (hydroBp != null)
                    afterFutureSupport = SimulateOneMore(afterFutureSupport, null, hydroBp, idealWorkers);

                // Fix hab, food, and entertainment deficits from the look-ahead.
                // After fixing these, also check if the new support structures
                // pushed power into deficit (hab/hydro/ent all need power).
                if (afterFutureSupport.HabitationRequired > accumulator.HabitationProvision)
                {
                    PlaceSupportSafe(result, supportPool, GameConstants.PropHabitationProvision, idealWorkers, ref accumulator);
                }

                if (afterFutureSupport.FoodRequired > accumulator.FoodProvision)
                {
                    PlaceSupportSafe(result, supportPool, GameConstants.PropFoodProvision, idealWorkers, ref accumulator);
                }

                if (afterFutureSupport.EntertainmentRequired > accumulator.EntertainmentProvided)
                {
                    PlaceSupportSafe(result, supportPool, GameConstants.PropEntertainmentProvided, idealWorkers, ref accumulator);
                }

                // Final check: after all look-ahead placements, verify the primary
                // won't cause a deficit. This catches power deficits from support
                // structures placed by the look-ahead or Step B.
                afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
                if (HasDeficit(afterPrimary))
                {
                    FixDeficits(result, supportPool, afterPrimary, idealWorkers, ref accumulator);
                }

                // Step C: Place the primary and advance the accumulator
                result.Add(primary);
                accumulator = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
                Log.Info(
                    "Placed primary '{0}' at [{1}]",
                    primaryBp?.ExtendedName ?? primary.FlatpackBlueprintUUID,
                    result.Count - 1);
            }

            // Append leftover support, fixing deficits as needed
            foreach (var leftover in supportPool)
            {
                var lBp = _playerContext.FindBlueprint(leftover.FlatpackBlueprintUUID);
                ColonyStructureStatus afterLeftover = SimulateOneMore(accumulator, leftover, lBp, idealWorkers);
                if (HasDeficit(afterLeftover))
                {
                    // The leftover support itself would cause a deficit -- create support for it
                    var tempPool = new List<ColonyStructure>(); // empty pool, force creation
                    FixDeficits(result, tempPool, afterLeftover, idealWorkers, ref accumulator);
                }

                result.Add(leftover);
                accumulator = SimulateOneMore(accumulator, leftover, lBp, idealWorkers);
            }

            Log.Info("Build order optimized: {0} structures", result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                var bp = _playerContext.FindBlueprint(result[i].FlatpackBlueprintUUID);
                Log.Info("  [{0}] {1}", i, bp?.ExtendedName ?? result[i].FlatpackBlueprintUUID);
            }

            return result;
        }

        // -----------------------------------------------------------------------
        // Deficit resolution
        // -----------------------------------------------------------------------

        /// <summary>
        /// Adds support structures to the result list until the given target status
        /// has no deficits. The target status represents the state AFTER some future
        /// structure (e.g. a primary) that isn't in the result list yet.
        /// </summary>
        private void FixDeficits(
            List<ColonyStructure> result,
            List<ColonyStructure> pool,
            ColonyStructureStatus targetStatus,
            IColonyStructureWorkers workers,
            ref ColonyStructureStatus accumulator)
        {
            // Keep placing support until the result list has enough resources
            // to satisfy the target status (which includes a future primary).
            for (int safety = 0; safety < 50; safety++)
            {
                // Use accumulator directly instead of SimulateAll
                string needed = null;
                if (targetStatus.PowerRequired > accumulator.PowerProvided)
                    needed = GameConstants.PropPowerProvided;
                else if (targetStatus.HabitationRequired > accumulator.HabitationProvision)
                    needed = GameConstants.PropHabitationProvision;
                else if (targetStatus.FoodRequired > accumulator.FoodProvision)
                    needed = GameConstants.PropFoodProvision;
                else if (targetStatus.EntertainmentRequired > accumulator.EntertainmentProvided)
                    needed = GameConstants.PropEntertainmentProvided;

                // Also check if the result list itself has a deficit
                if (needed == null)
                    needed = GetHighestPriorityDeficit(accumulator);

                if (needed == null) break;

                PlaceSupportSafe(result, pool, needed, workers, ref accumulator);
            }
        }

        /// <summary>
        /// Places a support structure for the given deficit type. If placing it
        /// would cause a new deficit, places the prerequisite first.
        /// </summary>
        private void PlaceSupportSafe(
            List<ColonyStructure> result,
            List<ColonyStructure> pool,
            string deficitType,
            IColonyStructureWorkers workers,
            ref ColonyStructureStatus accumulator)
        {
            // Get the support structure from pool or create it
            ColonyStructure support = TakeFromPool(pool, deficitType);
            if (support == null)
            {
                support = CreateStructure(deficitType, result);
                if (support == null) return;
            }

            // Simulate placing it -- would it cause a NEW deficit?
            var bp = _playerContext.FindBlueprint(support.FlatpackBlueprintUUID);
            ColonyStructureStatus afterStatus = SimulateOneMore(accumulator, support, bp, workers);

            // Check for new deficits caused by this support structure.
            // Place prerequisites BEFORE this structure.
            // Power: Ent Centre needs 2 power, Hydro/Hab need 1
            if (afterStatus.PowerRequired > afterStatus.PowerProvided &&
                !(accumulator.PowerRequired > accumulator.PowerProvided))
            {
                PlaceSupportSafe(result, pool, GameConstants.PropPowerProvided, workers, ref accumulator);
            }

            // Entertainment: Hydro/Ent workers need entertainment
            if (afterStatus.EntertainmentRequired > afterStatus.EntertainmentProvided &&
                !(accumulator.EntertainmentRequired > accumulator.EntertainmentProvided) &&
                deficitType != GameConstants.PropEntertainmentProvided)
            {
                PlaceSupportSafe(result, pool, GameConstants.PropEntertainmentProvided, workers, ref accumulator);
            }

            // Hab: workers need habitation
            if (afterStatus.HabitationRequired > afterStatus.HabitationProvision &&
                !(accumulator.HabitationRequired > accumulator.HabitationProvision) &&
                deficitType != GameConstants.PropHabitationProvision)
            {
                PlaceSupportSafe(result, pool, GameConstants.PropHabitationProvision, workers, ref accumulator);
            }

            // Food: workers need food
            if (afterStatus.FoodRequired > afterStatus.FoodProvision &&
                !(accumulator.FoodRequired > accumulator.FoodProvision) &&
                deficitType != GameConstants.PropFoodProvision)
            {
                PlaceSupportSafe(result, pool, GameConstants.PropFoodProvision, workers, ref accumulator);
            }

            result.Add(support);
            accumulator = SimulateOneMore(accumulator, support, bp, workers);
            Log.Info("  Placed support '{0}' at [{1}]", bp?.ExtendedName ?? "?", result.Count - 1);
        }

        private string GetHighestPriorityDeficit(ColonyStructureStatus status)
        {
            if (status.PowerRequired > status.PowerProvided) return GameConstants.PropPowerProvided;
            if (status.HabitationRequired > status.HabitationProvision) return GameConstants.PropHabitationProvision;
            if (status.FoodRequired > status.FoodProvision) return GameConstants.PropFoodProvision;
            if (status.EntertainmentRequired > status.EntertainmentProvided) return GameConstants.PropEntertainmentProvided;
            return null;
        }

        // -----------------------------------------------------------------------
        // Pool management
        // -----------------------------------------------------------------------

        private ColonyStructure TakeFromPool(List<ColonyStructure> pool, string provisionProperty)
        {
            foreach (var candidate in pool)
            {
                var bp = _playerContext.FindBlueprint(candidate.FlatpackBlueprintUUID);
                if (bp == null) continue;
                decimal val;
                if (bp.Properties.GetDecimal(provisionProperty, 0, out val) && val > 0)
                {
                    pool.Remove(candidate);
                    return candidate;
                }
            }

            return null;
        }

        private void PlaceFromPool(List<ColonyStructure> result, List<ColonyStructure> pool, string blueprintType)
        {
            var match = pool.FirstOrDefault(s =>
            {
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                return bp != null && bp.BluePrintType == blueprintType;
            });
            if (match != null)
            {
                pool.Remove(match);
                result.Add(match);
            }
            else
            {
                var created = CreateStructureByType(blueprintType);
                if (created != null) result.Add(created);
            }
        }

        private ColonyStructure CreateStructure(string provisionProperty, List<ColonyStructure> currentResult)
        {
            foreach (var bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null || !bp.BluePrintType.IsFlatpack()) continue;

                // Respect MaxPerColony limit
                long maxPerColony = 0;
                bp.Properties.GetLong(GameConstants.PropMaxPerColony, 0, out maxPerColony);
                if (maxPerColony > 0)
                {
                    int currentCount = currentResult.Count(s => s.FlatpackBlueprintUUID == bp.UUID);
                    if (currentCount >= maxPerColony) continue;
                }

                decimal val;
                if (bp.Properties.GetDecimal(provisionProperty, 0, out val) && val > 0)
                {
                    Log.Info("  Created support: {0} (provides {1}={2})", bp.ExtendedName, provisionProperty, val);
                    return new ColonyStructure
                    {
                        UUID = Guid.NewGuid().ToString(),
                        FlatpackBlueprintUUID = bp.UUID
                    };
                }
            }

            Log.Warn("  CreateStructure: no blueprint found for '{0}'", provisionProperty);
            return null;
        }

        private ColonyStructure CreateStructureByType(string blueprintType)
        {
            var bp = _playerContext.GetAllBlueprints()
                .FirstOrDefault(b => b.UUID != null && b.BluePrintType == blueprintType);
            if (bp == null) return null;
            return new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID
            };
        }

        private ReadOnlyBlueprint FindBlueprintByType(string blueprintType)
        {
            var bp = _playerContext.GetAllBlueprints()
                .FirstOrDefault(b => b.UUID != null && b.BluePrintType == blueprintType);
            return bp != null ? new ReadOnlyBlueprint(bp) : null;
        }

        // -----------------------------------------------------------------------
        // Simulation
        // -----------------------------------------------------------------------

        private ColonyStructureStatus SimulateOneMore(
            ColonyStructureStatus prev,
            ColonyStructure structure,
            ReadOnlyBlueprint blueprint,
            IColonyStructureWorkers workers)
        {
            var calculator = new ColonyStatusCalculator(new Colony());
            var current = new ColonyStructureStatus();
            // structure can be null for look-ahead simulation with just a blueprint
            calculator.CalculateBuilt(
                structure ?? new ColonyStructure { UUID = Guid.NewGuid().ToString() },
                prev,
                current,
                workers,
                blueprint);
            return current;
        }

        private bool HasDeficit(ColonyStructureStatus status)
        {
            return status.PowerRequired > status.PowerProvided ||
                   status.HabitationRequired > status.HabitationProvision ||
                   status.FoodRequired > status.FoodProvision ||
                   status.EntertainmentRequired > status.EntertainmentProvided;
        }
    }
}
