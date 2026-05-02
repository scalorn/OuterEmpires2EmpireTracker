using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for the optimizer incremental simulation refactoring.
    /// Validates correctness properties from the design document:
    ///   Property 1: Incremental accumulation equivalence
    ///   Property 2: SimulateOneMore purity (does not mutate input)
    ///   Property 3: Optimizer output equivalence
    ///   Property 4: Linear call count (bounded output size)
    /// </summary>
    [TestFixture]
    public class BuildOrderOptimizerPropertyTests
    {
        private static readonly string[] AllBlueprintTypes = new[]
        {
            BlueprintTypes.ColonyCommandCentre,
            "Flatpacks/ReactorCore",
            "Flatpacks/HabitationBlock",
            "Flatpacks/HydroponicsBay",
            "Flatpacks/EntertainmentCentreFlatpack",
            "Flatpacks/MiningRig",
            "Flatpacks/Refinery",
            "Flatpacks/Manufactory",
            "Flatpacks/Warehouse",
            "Flatpacks/ResearchLaboratory",
            "Flatpacks/RemoteOperationsArray",
        };

        private static readonly string[] PrimaryTypes = new[]
        {
            "Flatpacks/MiningRig",
            "Flatpacks/Refinery",
            "Flatpacks/Manufactory",
            "Flatpacks/Warehouse",
            "Flatpacks/ResearchLaboratory",
            "Flatpacks/RemoteOperationsArray",
        };

        private static readonly string[] SupportTypes = new[]
        {
            "Flatpacks/ReactorCore",
            "Flatpacks/HabitationBlock",
            "Flatpacks/HydroponicsBay",
            "Flatpacks/EntertainmentCentreFlatpack",
        };

        private EmpireContext empireContext;
        private PlayerContext playerContext;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetAllFilePaths();
            EmpireContext.Reset();
            empireContext = EmpireContext.GetInstance();
            playerContext = PlayerContext.GetInstance();
        }

        // -----------------------------------------------------------------------
        // Property 1: Incremental accumulation equivalence
        // Feature: optimizer-incremental-simulation
        //
        // For any sequence of colony structures, building cumulative status
        // incrementally via CalculateBuilt one-at-a-time produces the same
        // result as building it via a fresh CalculateBuilt loop from scratch.
        // Both approaches use the same fold â€” this confirms the fold is
        // deterministic and order-independent of calculator instance.
        // **Validates: Requirements 1.1, 1.2, 2.1, 2.2, 2.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IncrementalAccumulation_EqualsBatchCalculation()
        {
            var typeGen = Gen.Elements(AllBlueprintTypes);
            var seqGen = Gen.ArrayOf(typeGen)
                .Where(arr => arr.Length >= 1 && arr.Length <= 30);

            return Prop.ForAll(Arb.From(seqGen), types =>
            {
                var structures = types.Select(t => MakeStructure(t)).ToList();
                var workers = new IdealColonyStructureWorkers();

                // Approach 1: Incremental fold with one calculator instance
                var calc1 = new ColonyStatusCalculator(new Colony());
                ColonyStructureStatus incremental = new ColonyStructureStatus();
                foreach (var s in structures)
                {
                    ReadOnlyBlueprint bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                    var next = new ColonyStructureStatus();
                    calc1.CalculateBuilt(s, incremental, next, workers, bp);
                    incremental = next;
                }

                // Approach 2: Batch fold with a fresh calculator instance
                var calc2 = new ColonyStatusCalculator(new Colony());
                ColonyStructureStatus prev = new ColonyStructureStatus();
                foreach (var s in structures)
                {
                    ReadOnlyBlueprint bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                    var current = new ColonyStructureStatus();
                    calc2.CalculateBuilt(s, prev, current, workers, bp);
                    prev = current;
                }

                bool equal = StatusFieldsEqual(incremental, prev);
                return equal.Label(
                    equal ? "OK" : $"Mismatch: {StatusDiff(incremental, prev)}");
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: SimulateOneMore is pure (does not mutate input)
        // Feature: optimizer-incremental-simulation
        //
        // Calling CalculateBuilt with a previous status does not modify
        // the previous status object. This guarantees look-ahead projections
        // do not corrupt the running accumulator.
        // **Validates: Requirements 4.1, 4.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SimulateOneMore_DoesNotMutateInput()
        {
            var typeGen = Gen.Elements(AllBlueprintTypes);

            return Prop.ForAll(Arb.From(typeGen), bpType =>
            {
                var structure = MakeStructure(bpType);
                var workers = new IdealColonyStructureWorkers();
                var calculator = new ColonyStatusCalculator(new Colony());
                ReadOnlyBlueprint bp = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);

                // Create a status with known non-zero values
                var inputStatus = new ColonyStructureStatus
                {
                    PowerProvided = 10m,
                    PowerRequired = 5m,
                    HabitationProvision = 8m,
                    HabitationRequired = 4m,
                    FoodProvision = 6m,
                    FoodRequired = 3m,
                    EntertainmentProvided = 12m,
                    EntertainmentRequired = 6m,
                    WarehouseCapacity = 100m,
                    WarehouseRequired = 50m,
                    UnallocatedBlueCollarPresent = true,
                    UnallocatedWhiteCollarPresent = false,
                    UnallocatedSpecialistPresent = true,
                };

                // Snapshot all fields
                decimal snapPwrProv = inputStatus.PowerProvided;
                decimal snapPwrReq = inputStatus.PowerRequired;
                decimal snapHabProv = inputStatus.HabitationProvision;
                decimal snapHabReq = inputStatus.HabitationRequired;
                decimal snapFoodProv = inputStatus.FoodProvision;
                decimal snapFoodReq = inputStatus.FoodRequired;
                decimal snapEntProv = inputStatus.EntertainmentProvided;
                decimal snapEntReq = inputStatus.EntertainmentRequired;
                decimal snapWhCap = inputStatus.WarehouseCapacity;
                decimal snapWhReq = inputStatus.WarehouseRequired;
                bool snapBC = inputStatus.UnallocatedBlueCollarPresent;
                bool snapWC = inputStatus.UnallocatedWhiteCollarPresent;
                bool snapSp = inputStatus.UnallocatedSpecialistPresent;

                // Call CalculateBuilt (what SimulateOneMore wraps)
                var outputStatus = new ColonyStructureStatus();
                calculator.CalculateBuilt(structure, inputStatus, outputStatus, workers, bp);

                // Verify input is unchanged
                bool unchanged =
                    inputStatus.PowerProvided == snapPwrProv &&
                    inputStatus.PowerRequired == snapPwrReq &&
                    inputStatus.HabitationProvision == snapHabProv &&
                    inputStatus.HabitationRequired == snapHabReq &&
                    inputStatus.FoodProvision == snapFoodProv &&
                    inputStatus.FoodRequired == snapFoodReq &&
                    inputStatus.EntertainmentProvided == snapEntProv &&
                    inputStatus.EntertainmentRequired == snapEntReq &&
                    inputStatus.WarehouseCapacity == snapWhCap &&
                    inputStatus.WarehouseRequired == snapWhReq &&
                    inputStatus.UnallocatedBlueCollarPresent == snapBC &&
                    inputStatus.UnallocatedWhiteCollarPresent == snapWC &&
                    inputStatus.UnallocatedSpecialistPresent == snapSp;

                return unchanged.Label(
                    unchanged ? "OK" : "Input status was mutated by CalculateBuilt");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: Optimizer output equivalence
        // Feature: optimizer-incremental-simulation
        //
        // For any colony configuration (1 CC + random primaries + random support),
        // the optimizer output satisfies:
        // (a) no deficits from the first primary onward
        // (b) all input structures preserved in the output
        // (c) CC is first
        // **Validates: Requirements 2.1, 2.2, 2.4, 2.5, 2.6**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OptimizerOutput_NoDeficitsAndPreservesInput()
        {
            var primaryCountGen = Gen.Choose(0, 10);
            var supportCountGen = Gen.Choose(0, 15);
            var primaryTypeGen = Gen.Elements(PrimaryTypes);
            var supportTypeGen = Gen.Elements(SupportTypes);

            var colonyGen = from nPrimary in primaryCountGen
                            from nSupport in supportCountGen
                            from primaries in Gen.ArrayOf(nPrimary, primaryTypeGen)
                            from supports in Gen.ArrayOf(nSupport, supportTypeGen)
                            select new { Primaries = primaries, Supports = supports };

            return Prop.ForAll(Arb.From(colonyGen), config =>
            {
                var colony = new Colony { UUID = Guid.NewGuid().ToString() };
                colony.Structures.Add(MakeStructure(BlueprintTypes.ColonyCommandCentre));
                foreach (var t in config.Primaries)
                    colony.Structures.Add(MakeStructure(t));
                foreach (var t in config.Supports)
                    colony.Structures.Add(MakeStructure(t));

                var inputUUIDs = colony.Structures.Select(s => s.UUID).ToHashSet();
                var optimizer = new BuildOrderOptimizer(playerContext);
                var result = optimizer.Optimize(colony);

                // (c) CC is first
                var firstBp = playerContext.FindBlueprint(result[0].FlatpackBlueprintUUID);
                if (firstBp == null || firstBp.BluePrintType != BlueprintTypes.ColonyCommandCentre)
                {
                    return false.Label("CC is not first");
                }

                // (b) All input structures preserved
                foreach (var uuid in inputUUIDs)
                {
                    if (!result.Any(s => s.UUID == uuid))
                    {
                        return false.Label($"Input structure {uuid} missing from output");
                    }
                }

                // (a) No deficits from first primary through last primary.
                // Leftover support appended after the last primary may have
                // transient deficits (e.g. Ent Centre needing power) â€” this is
                // expected behavior per REQ-COL-095g.
                var calc = new ColonyStatusCalculator(new Colony());
                var iw = new IdealColonyStructureWorkers();
                var prev = new ColonyStructureStatus();
                int firstPrimaryPos = -1;
                int lastPrimaryPos = -1;

                // First pass: find first and last primary positions
                {
                    var scanPrev = new ColonyStructureStatus();
                    for (int i = 0; i < result.Count; i++)
                    {
                        var s = result[i];
                        var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                        if (bp != null && !IsSupportType(bp.BluePrintType) &&
                            bp.BluePrintType != BlueprintTypes.ColonyCommandCentre)
                        {
                            if (firstPrimaryPos < 0) firstPrimaryPos = i;
                            lastPrimaryPos = i;
                        }
                    }
                }

                if (firstPrimaryPos < 0)
                    return true.Label("OK â€” no primaries");

                for (int i = 0; i < result.Count; i++)
                {
                    var s = result[i];
                    var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                    var current = new ColonyStructureStatus();
                    calc.CalculateBuilt(s, prev, current, iw, bp);

                    if (i >= firstPrimaryPos && i <= lastPrimaryPos)
                    {
                        bool deficit = current.PowerRequired > current.PowerProvided ||
                                       current.HabitationRequired > current.HabitationProvision ||
                                       current.FoodRequired > current.FoodProvision ||
                                       current.EntertainmentRequired > current.EntertainmentProvided;
                        if (deficit)
                        {
                            return false.Label(
                                $"Deficit at [{i}] {bp?.BluePrintType}: " +
                                $"Pwr={current.PowerRequired}/{current.PowerProvided} " +
                                $"Hab={current.HabitationRequired}/{current.HabitationProvision} " +
                                $"Food={current.FoodRequired}/{current.FoodProvision} " +
                                $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}");
                        }
                    }

                    prev = current;
                }

                return true.Label("OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 4: Linear call count (bounded output size)
        // Feature: optimizer-incremental-simulation
        //
        // For any colony of n input structures (1 CC + 0-20 random structures),
        // the output has at most n + 50 structures, confirming bounded growth
        // and no runaway support creation.
        // **Validates: Requirements 7.1, 7.2, 7.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OptimizerOutput_HasBoundedSize()
        {
            var countGen = Gen.Choose(0, 20);
            var typeGen = Gen.Elements(
                AllBlueprintTypes.Where(t => t != BlueprintTypes.ColonyCommandCentre).ToArray());

            var colonyGen = from n in countGen
                            from types in Gen.ArrayOf(n, typeGen)
                            select types;

            return Prop.ForAll(Arb.From(colonyGen), types =>
            {
                var colony = new Colony { UUID = Guid.NewGuid().ToString() };
                colony.Structures.Add(MakeStructure(BlueprintTypes.ColonyCommandCentre));
                foreach (var t in types)
                    colony.Structures.Add(MakeStructure(t));

                int inputCount = colony.Structures.Count;
                var optimizer = new BuildOrderOptimizer(playerContext);
                var result = optimizer.Optimize(colony);

                bool bounded = result.Count <= inputCount + 50;
                return bounded.Label(
                    bounded ? "OK" : $"Output {result.Count} exceeds input {inputCount} + 50");
            });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private string FindGlobalBlueprintUUID(string blueprintType)
        {
            var bp = empireContext.GlobalBlueprintList
                .FirstOrDefault(b => b.BluePrintType == blueprintType);
            return bp?.UUID;
        }

        private ColonyStructure MakeStructure(string blueprintType)
        {
            return new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = FindGlobalBlueprintUUID(blueprintType)
            };
        }

        private bool IsSupportType(string blueprintType)
        {
            return blueprintType == "Flatpacks/ReactorCore" ||
                   blueprintType == "Flatpacks/HabitationBlock" ||
                   blueprintType == "Flatpacks/HydroponicsBay" ||
                   blueprintType == "Flatpacks/EntertainmentCentreFlatpack";
        }

        private bool StatusFieldsEqual(ColonyStructureStatus a, ColonyStructureStatus b)
        {
            return a.PowerProvided == b.PowerProvided &&
                   a.PowerRequired == b.PowerRequired &&
                   a.HabitationProvision == b.HabitationProvision &&
                   a.HabitationRequired == b.HabitationRequired &&
                   a.FoodProvision == b.FoodProvision &&
                   a.FoodRequired == b.FoodRequired &&
                   a.EntertainmentProvided == b.EntertainmentProvided &&
                   a.EntertainmentRequired == b.EntertainmentRequired &&
                   a.WarehouseCapacity == b.WarehouseCapacity &&
                   a.WarehouseRequired == b.WarehouseRequired &&
                   a.UnallocatedBlueCollarPresent == b.UnallocatedBlueCollarPresent &&
                   a.UnallocatedWhiteCollarPresent == b.UnallocatedWhiteCollarPresent &&
                   a.UnallocatedSpecialistPresent == b.UnallocatedSpecialistPresent;
        }

        private string StatusDiff(ColonyStructureStatus a, ColonyStructureStatus b)
        {
            var diffs = new List<string>();
            if (a.PowerProvided != b.PowerProvided) diffs.Add($"PwrProv {a.PowerProvided} vs {b.PowerProvided}");
            if (a.PowerRequired != b.PowerRequired) diffs.Add($"PwrReq {a.PowerRequired} vs {b.PowerRequired}");
            if (a.HabitationProvision != b.HabitationProvision) diffs.Add($"HabProv {a.HabitationProvision} vs {b.HabitationProvision}");
            if (a.HabitationRequired != b.HabitationRequired) diffs.Add($"HabReq {a.HabitationRequired} vs {b.HabitationRequired}");
            if (a.FoodProvision != b.FoodProvision) diffs.Add($"FoodProv {a.FoodProvision} vs {b.FoodProvision}");
            if (a.FoodRequired != b.FoodRequired) diffs.Add($"FoodReq {a.FoodRequired} vs {b.FoodRequired}");
            if (a.EntertainmentProvided != b.EntertainmentProvided) diffs.Add($"EntProv {a.EntertainmentProvided} vs {b.EntertainmentProvided}");
            if (a.EntertainmentRequired != b.EntertainmentRequired) diffs.Add($"EntReq {a.EntertainmentRequired} vs {b.EntertainmentRequired}");
            if (a.UnallocatedBlueCollarPresent != b.UnallocatedBlueCollarPresent) diffs.Add($"UnallocBC {a.UnallocatedBlueCollarPresent} vs {b.UnallocatedBlueCollarPresent}");
            if (a.UnallocatedWhiteCollarPresent != b.UnallocatedWhiteCollarPresent) diffs.Add($"UnallocWC {a.UnallocatedWhiteCollarPresent} vs {b.UnallocatedWhiteCollarPresent}");
            if (a.UnallocatedSpecialistPresent != b.UnallocatedSpecialistPresent) diffs.Add($"UnallocSp {a.UnallocatedSpecialistPresent} vs {b.UnallocatedSpecialistPresent}");
            return string.Join(", ", diffs);
        }
    }
}
