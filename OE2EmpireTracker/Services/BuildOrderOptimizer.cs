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

        public BuildOrderOptimizer(PlayerContext playerContext)
        {
            _playerContext = playerContext;
        }

        /// <summary>
        /// Returns true if the blueprint provides any support resource.
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
        /// Optimizes the build order.
        ///
        /// 1. Classify into primary/support pools.
        /// 2. Calculate total support needed, ensure pool has enough.
        /// 3. Build the support backbone in repeating groups:
        ///    CC, [Reactor, Hab, Hydro, Ent, ...] ordered by priority.
        /// 4. Insert each primary at the earliest backbone position
        ///    where it doesn't create a deficit.
        /// </summary>
        public List<ColonyStructure> Optimize(Colony colony)
        {
            var primaries = new List<ColonyStructure>();
            var support = new List<ColonyStructure>();

            foreach (var structure in colony.Structures)
            {
                Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null)
                    primaries.Add(structure);
                else if (bp.BluePrintType == "Flatpacks/ColonyCommandCentre")
                    support.Add(structure); // CC goes to support for backbone ordering
                else if (IsSupportStructure(bp))
                    support.Add(structure);
                else
                    primaries.Add(structure);
            }

            Log.Info("Optimizer: {0} primaries, {1} support", primaries.Count, support.Count);

            // --- Calculate total support needed ---
            EnsureSufficientSupport(primaries, support);

            // --- Build the support backbone ---
            // Order: CC first, then repeating groups of [Reactor, Hab, Hydro, Ent]
            // sorted by priority within each type.
            var backbone = BuildBackbone(support);

            Log.Info("Backbone: {0} structures", backbone.Count);

            // --- Insert primaries into the backbone ---
            var idealWorkers = new IdealColonyStructureWorkers();
            var result = new List<ColonyStructure>(backbone);

            foreach (var primary in primaries)
            {
                int insertPos = FindSafeInsertPosition(result, primary, idealWorkers);
                result.Insert(insertPos, primary);

                Blueprint pBp = _playerContext.FindBlueprint(primary.FlatpackBlueprintUUID);
                Log.Info("Inserted primary '{0}' at [{1}]",
                    pBp?.ExtendedName ?? primary.FlatpackBlueprintUUID, insertPos);
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
        // Support calculation
        // -----------------------------------------------------------------------

        private void EnsureSufficientSupport(List<ColonyStructure> primaries, List<ColonyStructure> support)
        {
            var idealWorkers = new IdealColonyStructureWorkers();

            // Simulate everything together to get final resource totals
            var all = new List<ColonyStructure>(support);
            all.AddRange(primaries);
            var finalStatus = SimulateAll(all, idealWorkers);

            // Per-structure provision values
            decimal reactorPower = GetProvision("Flatpacks/ReactorCore", GameConstants.PropPowerProvided);
            decimal habProvision = GetProvision("Flatpacks/HabitationBlock", GameConstants.PropHabitationProvision);
            decimal hydroFood = GetProvision("Flatpacks/HydroponicsBay", GameConstants.PropFoodProvision);
            decimal entProvision = GetProvision("Flatpacks/EntertainmentCentreFlatpack", GameConstants.PropEntertainmentProvided);

            // Iteratively calculate needed support (new support has workers that need support)
            int extraReactors = 0, extraHabs = 0, extraHydros = 0, extraEnts = 0;

            for (int pass = 0; pass < 10; pass++)
            {
                // Current gaps after accounting for already-planned extras
                decimal powerGap = (finalStatus.PowerRequired - finalStatus.PowerProvided)
                    + (extraHydros * 1) + (extraEnts * 2) // new support power costs
                    - (extraReactors * reactorPower);
                decimal habGap = (finalStatus.HabitationRequired - finalStatus.HabitationProvision)
                    + (extraHydros + extraEnts) // new support workers need hab
                    - (extraHabs * habProvision);
                decimal foodGap = (finalStatus.FoodRequired - finalStatus.FoodProvision)
                    + (extraHydros + extraEnts) // new support workers need food
                    - (extraHydros * hydroFood);
                decimal entGap = (finalStatus.EntertainmentRequired - finalStatus.EntertainmentProvided)
                    + (extraHydros + extraEnts) // new support workers need ent
                    - (extraEnts * entProvision);

                int prevTotal = extraReactors + extraHabs + extraHydros + extraEnts;

                if (powerGap > 0 && reactorPower > 0)
                    extraReactors += (int)Math.Ceiling(powerGap / reactorPower);
                if (habGap > 0 && habProvision > 0)
                    extraHabs += (int)Math.Ceiling(habGap / habProvision);
                if (foodGap > 0 && hydroFood > 0)
                    extraHydros += (int)Math.Ceiling(foodGap / hydroFood);
                if (entGap > 0 && entProvision > 0)
                    extraEnts += (int)Math.Ceiling(entGap / entProvision);

                if (extraReactors + extraHabs + extraHydros + extraEnts == prevTotal)
                    break; // converged
            }

            Log.Info("Extra support needed: {0} reactors, {1} habs, {2} hydros, {3} ents",
                extraReactors, extraHabs, extraHydros, extraEnts);

            for (int i = 0; i < extraReactors; i++) CreateAndAdd(support, "Flatpacks/ReactorCore");
            for (int i = 0; i < extraHabs; i++) CreateAndAdd(support, "Flatpacks/HabitationBlock");
            for (int i = 0; i < extraHydros; i++) CreateAndAdd(support, "Flatpacks/HydroponicsBay");
            for (int i = 0; i < extraEnts; i++) CreateAndAdd(support, "Flatpacks/EntertainmentCentreFlatpack");
        }

        private decimal GetProvision(string blueprintType, string property)
        {
            var bp = _playerContext.GetAllBlueprints()
                .FirstOrDefault(b => b.UUID != null && b.BluePrintType == blueprintType);
            if (bp == null) return 0;
            decimal val;
            bp.Properties.getDecimal(property, 0, out val);
            return val;
        }

        private void CreateAndAdd(List<ColonyStructure> pool, string blueprintType)
        {
            var bp = _playerContext.GetAllBlueprints()
                .FirstOrDefault(b => b.UUID != null && b.BluePrintType == blueprintType);
            if (bp == null) return;
            pool.Add(new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bp.UUID
            });
        }

        // -----------------------------------------------------------------------
        // Backbone construction
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds the support backbone ordered by priority.
        /// CC first, then structures sorted so that at every position
        /// the backbone itself has no deficits. Uses the same walk-and-insert
        /// approach but only with support structures.
        /// </summary>
        private List<ColonyStructure> BuildBackbone(List<ColonyStructure> supportPool)
        {
            var backbone = new List<ColonyStructure>();
            var pool = new List<ColonyStructure>(supportPool);
            supportPool.Clear(); // we'll consume everything

            // CC first
            var cc = pool.FirstOrDefault(s =>
            {
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                return bp != null && bp.BluePrintType == "Flatpacks/ColonyCommandCentre";
            });
            if (cc != null) { pool.Remove(cc); backbone.Add(cc); }

            // Sort remaining support by priority:
            // Reactors first (provide power, no workers), then Habs, then Hydros, then Ents
            var sorted = pool.OrderByDescending(s =>
            {
                var bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                if (bp == null) return 0;
                decimal val;
                if (bp.Properties.getDecimal(GameConstants.PropPowerProvided, 0, out val) && val > 0) return 4;
                if (bp.Properties.getDecimal(GameConstants.PropHabitationProvision, 0, out val) && val > 0) return 3;
                if (bp.Properties.getDecimal(GameConstants.PropFoodProvision, 0, out val) && val > 0) return 2;
                if (bp.Properties.getDecimal(GameConstants.PropEntertainmentProvided, 0, out val) && val > 0) return 1;
                return 0;
            }).ToList();

            // Insert each support structure at the earliest position where it doesn't
            // create a deficit. This interleaves Reactor/Hab/Hydro/Ent naturally.
            var idealWorkers = new IdealColonyStructureWorkers();
            foreach (var s in sorted)
            {
                int pos = FindSafeInsertPosition(backbone, s, idealWorkers);
                backbone.Insert(pos, s);
            }

            return backbone;
        }

        // -----------------------------------------------------------------------
        // Primary insertion
        // -----------------------------------------------------------------------

        /// <summary>
        /// Finds the earliest position in the list where inserting the structure
        /// doesn't create any deficit at any position in the resulting list.
        /// Falls back to the end of the list if no safe position exists.
        /// </summary>
        private int FindSafeInsertPosition(List<ColonyStructure> list, ColonyStructure toInsert, IColonyStructureWorkers workers)
        {
            // Try each position from the end backwards to find the earliest safe spot.
            // Start from the end because that's most likely safe (most resources available).
            // Then binary-search-style find the earliest.
            int safePos = list.Count; // default: append at end

            for (int pos = 1; pos <= list.Count; pos++) // skip 0 (CC must stay first)
            {
                // Test inserting at this position
                list.Insert(pos, toInsert);
                bool clean = !HasAnyDeficitAfter(list, pos, workers);
                list.RemoveAt(pos);

                if (clean)
                {
                    safePos = pos;
                    break; // earliest safe position found
                }
            }

            return safePos;
        }

        /// <summary>
        /// Simulates the full list and returns true if any position at or after
        /// startFrom has a deficit.
        /// </summary>
        private bool HasAnyDeficitAfter(List<ColonyStructure> structures, int startFrom, IColonyStructureWorkers workers)
        {
            var calculator = new ColonyStatusCalculator(new Colony());
            ColonyStructureStatus prev = new ColonyStructureStatus();

            for (int i = 0; i < structures.Count; i++)
            {
                var s = structures[i];
                Blueprint bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calculator.CalculateBuilt(s, prev, current, workers, bp);

                if (i >= startFrom && HasDeficit(current))
                    return true;

                prev = current;
            }
            return false;
        }

        // -----------------------------------------------------------------------
        // Simulation helpers
        // -----------------------------------------------------------------------

        private ColonyStructureStatus SimulateAll(List<ColonyStructure> structures, IColonyStructureWorkers workers)
        {
            var calculator = new ColonyStatusCalculator(new Colony());
            ColonyStructureStatus prev = new ColonyStructureStatus();
            foreach (var s in structures)
            {
                Blueprint bp = _playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calculator.CalculateBuilt(s, prev, current, workers, bp);
                prev = current;
            }
            return prev;
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
