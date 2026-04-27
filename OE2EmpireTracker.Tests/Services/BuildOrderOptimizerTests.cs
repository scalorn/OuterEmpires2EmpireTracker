using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BuildOrderOptimizerTests
    {
        // Type alias constants for readability in pinned test data
        private const string CC = "Flatpacks/ColonyCommandCentre";
        private const string Rx = "Flatpacks/ReactorCore";
        private const string Hb = "Flatpacks/HabitationBlock";
        private const string Hy = "Flatpacks/HydroponicsBay";
        private const string En = "Flatpacks/EntertainmentCentreFlatpack";
        private const string Mi = "Flatpacks/MiningRig";
        private const string Re = "Flatpacks/Refinery";
        private const string Mf = "Flatpacks/Manufactory";
        private const string Wh = "Flatpacks/Warehouse";
        private const string Rs = "Flatpacks/ResearchLaboratory";
        private const string RO = "Flatpacks/RemoteOperationsArray";

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

        [TearDown]
        public void TearDown()
        {
            // Don't reset singletons -- reuse across tests
        }

        // -----------------------------------------------------------------------
        // Test: Colony matching user's scenario
        // 16 primaries: 1 ROA, 4 Mining Rigs, 8 Refineries, 1 Manufactory,
        //               1 Warehouse, 1 Research Lab
        // Support pool should contain enough reactors, habs, hydros, ent centres
        // to cover all primaries without any deficit.
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // Test: Colony matching user's scenario
        // -----------------------------------------------------------------------

        [Test]
        public void Optimize_UserColony_NoDeficitsAfterBootstrap()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            // Add support structures (what a real colony would have)
            // 15 Reactors, 7 Hab Blocks, 7 Hydro Bays, 6 Ent Centres, 1 CC
            colony.Structures.Add(MakeStructure("Flatpacks/ColonyCommandCentre"));
            for (int i = 0; i < 20; i++) colony.Structures.Add(MakeStructure("Flatpacks/ReactorCore"));
            for (int i = 0; i < 8; i++) colony.Structures.Add(MakeStructure("Flatpacks/HabitationBlock"));
            for (int i = 0; i < 8; i++) colony.Structures.Add(MakeStructure("Flatpacks/HydroponicsBay"));
            for (int i = 0; i < 8; i++) colony.Structures.Add(MakeStructure("Flatpacks/EntertainmentCentreFlatpack"));

            // Add primaries
            colony.Structures.Add(MakeStructure("Flatpacks/RemoteOperationsArray"));
            for (int i = 0; i < 4; i++) colony.Structures.Add(MakeStructure("Flatpacks/MiningRig"));
            for (int i = 0; i < 8; i++) colony.Structures.Add(MakeStructure("Flatpacks/Refinery"));
            colony.Structures.Add(MakeStructure("Flatpacks/Manufactory"));
            colony.Structures.Add(MakeStructure("Flatpacks/Warehouse"));
            colony.Structures.Add(MakeStructure("Flatpacks/ResearchLaboratory"));

            var optimizer = new BuildOrderOptimizer(playerContext);
            var result = optimizer.Optimize(colony);

            TestContext.WriteLine($"Input: {colony.Structures.Count} structures");
            TestContext.WriteLine($"Output: {result.Count} structures");
            LogOrder(result);

            // Verify: CC is first
            var firstBp = playerContext.FindBlueprint(result[0].FlatpackBlueprintUUID);
            Assert.That(
                firstBp.BluePrintType,
                Is.EqualTo("Flatpacks/ColonyCommandCentre"),
                "Colony Command Centre must be first");

            // Verify: no deficits at any position where a primary appears.
            // Support-only positions at the start may have transient deficits
            // as resources are being built up.
            // Find the first primary position
            int firstPrimaryPos = -1;
            for (int i = 0; i < result.Count; i++)
            {
                var bp = playerContext.FindBlueprint(result[i].FlatpackBlueprintUUID);
                if (bp != null && !IsSupportType(bp.BluePrintType) && bp.BluePrintType != "Flatpacks/ColonyCommandCentre")
                {
                    firstPrimaryPos = i;
                    break;
                }
            }

            Assert.That(firstPrimaryPos, Is.GreaterThan(0), "Should have at least one primary");

            // Verify no deficits from the first primary onwards
            {
                var calc = new ColonyStatusCalculator(new Colony());
                var iw = new IdealColonyStructureWorkers();
                var prev = new ColonyStructureStatus();

                for (int i = 0; i < result.Count; i++)
                {
                    var s = result[i];
                    var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                    var current = new ColonyStructureStatus();
                    calc.CalculateBuilt(s, prev, current, iw, bp);

                    TestContext.WriteLine(
                        $"  [{i}] {bp?.ExtendedName,-40} Pwr={current.PowerRequired}/{current.PowerProvided} " +
                        $"Hab={current.HabitationRequired}/{current.HabitationProvision} " +
                        $"Food={current.FoodRequired}/{current.FoodProvision} " +
                        $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}" +
                        (i >= firstPrimaryPos && (current.PowerRequired > current.PowerProvided ||
                            current.HabitationRequired > current.HabitationProvision ||
                            current.FoodRequired > current.FoodProvision ||
                            current.EntertainmentRequired > current.EntertainmentProvided) ? " *** DEFICIT ***" : string.Empty));

                    if (i >= firstPrimaryPos)
                    {
                        bool deficit = current.PowerRequired > current.PowerProvided ||
                                       current.HabitationRequired > current.HabitationProvision ||
                                       current.FoodRequired > current.FoodProvision ||
                                       current.EntertainmentRequired > current.EntertainmentProvided;

                        Assert.That(
                            deficit,
                            Is.False,
                            $"Deficit at [{i}] {bp?.ExtendedName}: " + $"Pwr={current.PowerRequired}/{current.PowerProvided} " + $"Hab={current.HabitationRequired}/{current.HabitationProvision} " + $"Food={current.FoodRequired}/{current.FoodProvision} " + $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}");
                    }

                    prev = current;
                }
            }
        }

        [Test]
        public void Optimize_SmallColony_BootstrapCoversFirstPrimary()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            // Minimal colony: CC + 1 Reactor + 1 Hab + 1 Hydro + 1 Ent + 1 Mining Rig
            colony.Structures.Add(MakeStructure("Flatpacks/ColonyCommandCentre"));
            colony.Structures.Add(MakeStructure("Flatpacks/ReactorCore"));
            colony.Structures.Add(MakeStructure("Flatpacks/HabitationBlock"));
            colony.Structures.Add(MakeStructure("Flatpacks/HydroponicsBay"));
            colony.Structures.Add(MakeStructure("Flatpacks/EntertainmentCentreFlatpack"));
            colony.Structures.Add(MakeStructure("Flatpacks/MiningRig"));

            var optimizer = new BuildOrderOptimizer(playerContext);
            var result = optimizer.Optimize(colony);

            TestContext.WriteLine($"Output: {result.Count} structures");
            LogOrder(result);

            // Should be exactly 6 structures (no new ones created)
            // The mining rig at position 5 should have no deficit
            // because the bootstrap provides enough for 1 primary
            int deficitPos = FindFirstDeficit(result);
            // Only CC at [0] may have entertainment deficit
            Assert.That(
                deficitPos,
                Is.EqualTo(-1).Or.EqualTo(0),
                $"Unexpected deficit at position {deficitPos}");
        }

        // -----------------------------------------------------------------------
        // Systematic optimizer tests — build up from base case
        //
        // Blueprint resource values (from BaselineData.json + Reactor fixup):
        //   CC:       Hab=2, Food=2, WhiteCollar=1, UnassignedSpecialist=1
        //   Reactor:  PowerProvided=6, UnassignedSpecialist=1
        //   Hab:      HabProvision=4, PowerReq=1, UnassignedSpecialist=1
        //   Hydro:    FoodProvision=4, PowerReq=1, BlueCollar=1, UnassignedSpecialist=1
        //   Ent:      EntProvided=10, PowerReq=2, BlueCollar=1, UnassignedSpecialist=1
        //   Miner:    PowerReq=2, BlueCollar=1, UnassignedSpecialist=1, UnassignedWhiteCollar=1
        //   Refinery: PowerReq=5, BlueCollar=1, UnassignedSpecialist=1, UnassignedWhiteCollar=1
        //   Manufactory: PowerReq=5, BlueCollar=1, UnassignedSpecialist=1, UnassignedWhiteCollar=1
        //   Warehouse: PowerReq=2, UnassignedSpecialist=1
        //   Research:  PowerReq=4, BlueCollar=1, UnassignedSpecialist=1, UnassignedWhiteCollar=1
        //   ROA:       PowerReq=2, BlueCollar=1, UnassignedSpecialist=1, UnassignedWhiteCollar=1
        //
        // Unassigned workers: counted once (first structure needing that type).
        // Worker costs: 1 hab, 1 food, 2 ent per worker.
        // -----------------------------------------------------------------------

        [Test]
        public void Optimize_CCOnly_CreatesBootstrapSupport()
        {
            var colony = MakeColony(CC);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
            });
        }

        [Test]
        public void Optimize_CC_Miner_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, Mi);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (En, 6, 6, 5, 6, 5, 6, 10, 20),
                (Rx, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Mi, 10, 12, 8, 10, 8, 10, 16, 20),
            });
        }

        [Test]
        public void Optimize_CC_Refinery_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, Re);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (Rx, 4, 12, 4, 6, 4, 6, 8, 10),
                (En, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Rx, 8, 18, 6, 10, 6, 10, 12, 20),
                (Re, 13, 18, 8, 10, 8, 10, 16, 20),
            });
        }

        [Test]
        public void Optimize_CC_Manufactory_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, Mf);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (Rx, 4, 12, 4, 6, 4, 6, 8, 10),
                (En, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Rx, 8, 18, 6, 10, 6, 10, 12, 20),
                (Mf, 13, 18, 8, 10, 8, 10, 16, 20),
            });
        }

        [Test]
        public void Optimize_CC_Warehouse_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, Wh);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (En, 6, 6, 5, 6, 5, 6, 10, 20),
                (Rx, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Wh, 10, 12, 8, 10, 8, 10, 16, 20),
            });
        }

        [Test]
        public void Optimize_CC_ResearchLab_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, Rs);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (Rx, 4, 12, 4, 6, 4, 6, 8, 10),
                (En, 6, 12, 5, 6, 5, 6, 10, 20),
                (Rs, 10, 12, 6, 6, 6, 6, 12, 20),
            });
        }

        [Test]
        public void Optimize_CC_ROA_NoDeficitsAtPrimary()
        {
            var colony = MakeColony(CC, RO);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (En, 6, 6, 5, 6, 5, 6, 10, 20),
                (Rx, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (RO, 10, 12, 8, 10, 8, 10, 16, 20),
            });
        }

        [Test]
        public void Optimize_CC_2Miners_NoDeficitsAtPrimaries()
        {
            var colony = MakeColony(CC, Mi, Mi);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (En, 6, 6, 5, 6, 5, 6, 10, 20),
                (Rx, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Mi, 10, 12, 8, 10, 8, 10, 16, 20),
                (Mi, 12, 12, 9, 10, 9, 10, 18, 20),
            });
        }

        [Test]
        public void Optimize_CC_Miner_Refinery_NoDeficitsAtPrimaries()
        {
            var colony = MakeColony(CC, Mi, Re);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertExactSequence(result, new[]
            {
                (CC, 0, 0, 2, 2, 2, 2, 4, 0),
                (Rx, 0, 6, 2, 2, 2, 2, 4, 0),
                (Hb, 1, 6, 2, 6, 2, 2, 4, 0),
                (Hy, 2, 6, 3, 6, 3, 6, 6, 0),
                (En, 4, 6, 4, 6, 4, 6, 8, 10),
                (En, 6, 6, 5, 6, 5, 6, 10, 20),
                (Rx, 6, 12, 5, 6, 5, 6, 10, 20),
                (Hb, 7, 12, 5, 10, 5, 6, 10, 20),
                (Hy, 8, 12, 6, 10, 6, 10, 12, 20),
                (Mi, 10, 12, 8, 10, 8, 10, 16, 20),
                (Rx, 10, 18, 8, 10, 8, 10, 16, 20),
                (Re, 15, 18, 9, 10, 9, 10, 18, 20),
            });
        }

        [Test]
        public void Optimize_Deterministic_SameInputSameOutput()
        {
            var colony = MakeColony(CC, Mi, Re, Mf);
            var result1 = Optimize(colony);
            var result2 = Optimize(colony);

            Assert.That(result2.Count, Is.EqualTo(result1.Count),
                "Two runs produced different structure counts");

            for (int i = 0; i < result1.Count; i++)
            {
                var bp1 = playerContext.FindBlueprint(result1[i].FlatpackBlueprintUUID);
                var bp2 = playerContext.FindBlueprint(result2[i].FlatpackBlueprintUUID);
                Assert.That(bp2.BluePrintType, Is.EqualTo(bp1.BluePrintType),
                    $"[{i}] Run 1 has {bp1.BluePrintType}, Run 2 has {bp2.BluePrintType}");
            }
        }

        [Test]
        public void Optimize_CC_AllPrimaryTypes_NoDeficits()
        {
            var colony = MakeColony(CC, Mi, Re, Mf, Wh, Rs, RO);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertNoDeficitsFromFirstPrimary(result);

            // Verify all primary types are present
            Assert.That(CountType(result, Mi), Is.EqualTo(1), "Missing MiningRig");
            Assert.That(CountType(result, Re), Is.EqualTo(1), "Missing Refinery");
            Assert.That(CountType(result, Mf), Is.EqualTo(1), "Missing Manufactory");
            Assert.That(CountType(result, Wh), Is.EqualTo(1), "Missing Warehouse");
            Assert.That(CountType(result, Rs), Is.EqualTo(1), "Missing ResearchLaboratory");
            Assert.That(CountType(result, RO), Is.EqualTo(1), "Missing RemoteOperationsArray");
        }

        [Test]
        public void Optimize_PoolExhaustion_CreatesNewStructures()
        {
            // CC + 2 Miners but NO support structures — optimizer must create all support
            var colony = MakeColony(CC, Mi, Mi);
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            // Must have created support structures beyond the 3 inputs
            Assert.That(result.Count, Is.GreaterThan(3),
                "Optimizer should create support structures when pool is empty");

            AssertType(result, 0, CC);
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(CountType(result, Mi), Is.EqualTo(2), "Both miners must be in output");
        }

        [Test]
        public void Optimize_ExcessSupport_AppendedAtEnd()
        {
            var colony = new Colony { UUID = Guid.NewGuid().ToString() };
            colony.Structures.Add(MakeStructure(CC));
            for (int i = 0; i < 20; i++) colony.Structures.Add(MakeStructure(Rx));
            colony.Structures.Add(MakeStructure(Mi));

            var inputUUIDs = colony.Structures.Select(s => s.UUID).ToHashSet();
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            // All 20 input reactors must appear in the output
            Assert.That(CountType(result, Rx), Is.GreaterThanOrEqualTo(20),
                "All 20 input reactors must be in output");

            // All input structures must be preserved
            foreach (var uuid in inputUUIDs)
            {
                Assert.That(result.Any(s => s.UUID == uuid), Is.True,
                    $"Input structure {uuid} missing from output");
            }

            // Miner must have no deficit
            AssertNoDeficitsFromFirstPrimary(result);
        }

        [Test]
        public void Optimize_AllInputStructuresPreserved()
        {
            var colony = MakeColony(
                "Flatpacks/ColonyCommandCentre", "Flatpacks/ReactorCore",
                "Flatpacks/HabitationBlock", "Flatpacks/HydroponicsBay",
                "Flatpacks/EntertainmentCentreFlatpack",
                "Flatpacks/MiningRig", "Flatpacks/Refinery");
            var inputUUIDs = colony.Structures.Select(s => s.UUID).ToHashSet();

            var result = Optimize(colony);
            LogOrderWithStatus(result);

            foreach (var uuid in inputUUIDs)
            {
                Assert.That(result.Any(s => s.UUID == uuid), Is.True,
                    $"Input structure {uuid} missing from output");
            }
        }

        [Test]
        public void Optimize_OutputDoesNotExceed65Structures()
        {
            var colony = new Colony { UUID = Guid.NewGuid().ToString() };
            colony.Structures.Add(MakeStructure("Flatpacks/ColonyCommandCentre"));
            for (int i = 0; i < 4; i++) colony.Structures.Add(MakeStructure("Flatpacks/MiningRig"));
            for (int i = 0; i < 8; i++) colony.Structures.Add(MakeStructure("Flatpacks/Refinery"));
            colony.Structures.Add(MakeStructure("Flatpacks/Manufactory"));
            colony.Structures.Add(MakeStructure("Flatpacks/ResearchLaboratory"));

            var result = Optimize(colony);
            LogOrderWithStatus(result);

            Assert.That(result.Count, Is.LessThanOrEqualTo(65),
                $"Output has {result.Count} structures — exceeds 65 colony limit");
        }

        // -----------------------------------------------------------------------
        // Helpers (used by existing tests)
        // -----------------------------------------------------------------------

        private string FindGlobalBlueprintUUID(string blueprintType)
        {
            var bp = empireContext.GlobalBlueprintList
                .FirstOrDefault(b => b.BluePrintType == blueprintType);
            Assert.That(bp, Is.Not.Null, $"No global blueprint for {blueprintType}");
            return bp.UUID;
        }

        private ColonyStructure MakeStructure(string blueprintType)
        {
            return new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = FindGlobalBlueprintUUID(blueprintType)
            };
        }

        /// <summary>
        /// Walks the optimized list and returns the first position where any
        /// resource is in deficit, or -1 if no deficits exist.
        /// </summary>
        private int FindFirstDeficit(List<ColonyStructure> structures)
        {
            var idealWorkers = new IdealColonyStructureWorkers();
            var calculator = new ColonyStatusCalculator(new Colony());
            ColonyStructureStatus prev = new ColonyStructureStatus();

            for (int i = 0; i < structures.Count; i++)
            {
                var s = structures[i];
                OE2EmpireTracker.Models.Blueprint bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calculator.CalculateBuilt(s, prev, current, idealWorkers, bp);

                bool deficit = current.PowerRequired > current.PowerProvided ||
                               current.HabitationRequired > current.HabitationProvision ||
                               current.FoodRequired > current.FoodProvision ||
                               current.EntertainmentRequired > current.EntertainmentProvided;

                if (deficit)
                {
                    TestContext.WriteLine(
                        $"Deficit at [{i}] {bp?.ExtendedName}: " +
                        $"Pwr={current.PowerRequired}/{current.PowerProvided} " +
                        $"Hab={current.HabitationRequired}/{current.HabitationProvision} " +
                        $"Food={current.FoodRequired}/{current.FoodProvision} " +
                        $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}");
                    return i;
                }

                prev = current;
            }

            return -1;
        }

        private void LogOrder(List<ColonyStructure> structures)
        {
            for (int i = 0; i < structures.Count; i++)
            {
                var bp = playerContext.FindBlueprint(structures[i].FlatpackBlueprintUUID);
                TestContext.WriteLine($"  [{i}] {bp?.ExtendedName ?? structures[i].FlatpackBlueprintUUID}");
            }
        }

        private bool IsSupportType(string blueprintType)
        {
            return blueprintType == "Flatpacks/ReactorCore" ||
                   blueprintType == "Flatpacks/HabitationBlock" ||
                   blueprintType == "Flatpacks/HydroponicsBay" ||
                   blueprintType == "Flatpacks/EntertainmentCentreFlatpack";
        }

        // -----------------------------------------------------------------------
        // Helpers (used by systematic tests)
        // -----------------------------------------------------------------------

        private Colony MakeColony(params string[] blueprintTypes)
        {
            var colony = new Colony { UUID = Guid.NewGuid().ToString() };
            foreach (var type in blueprintTypes)
                colony.Structures.Add(MakeStructure(type));
            return colony;
        }

        private List<ColonyStructure> Optimize(Colony colony)
        {
            return new BuildOrderOptimizer(playerContext).Optimize(colony);
        }

        private void AssertType(List<ColonyStructure> result, int index, string expectedType)
        {
            var bp = playerContext.FindBlueprint(result[index].FlatpackBlueprintUUID);
            Assert.That(bp, Is.Not.Null, $"No blueprint found at [{index}]");
            Assert.That(bp.BluePrintType, Is.EqualTo(expectedType),
                $"[{index}] expected {expectedType}, got {bp.BluePrintType}");
        }

        private int CountType(List<ColonyStructure> result, string blueprintType)
        {
            return result.Count(s =>
            {
                var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                return bp != null && bp.BluePrintType == blueprintType;
            });
        }

        private void AssertNoDeficitsFromFirstPrimary(List<ColonyStructure> result)
        {
            var calc = new ColonyStatusCalculator(new Colony());
            var iw = new IdealColonyStructureWorkers();
            var prev = new ColonyStructureStatus();

            int firstPrimaryPos = -1;
            for (int i = 0; i < result.Count; i++)
            {
                var bp = playerContext.FindBlueprint(result[i].FlatpackBlueprintUUID);
                if (bp != null && !IsSupportType(bp.BluePrintType)
                    && bp.BluePrintType != "Flatpacks/ColonyCommandCentre")
                {
                    firstPrimaryPos = i;
                    break;
                }
            }

            if (firstPrimaryPos < 0) return;

            for (int i = 0; i < result.Count; i++)
            {
                var s = result[i];
                var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calc.CalculateBuilt(s, prev, current, iw, bp);

                if (i >= firstPrimaryPos)
                {
                    bool deficit = current.PowerRequired > current.PowerProvided ||
                                   current.HabitationRequired > current.HabitationProvision ||
                                   current.FoodRequired > current.FoodProvision ||
                                   current.EntertainmentRequired > current.EntertainmentProvided;

                    Assert.That(deficit, Is.False,
                        $"Deficit at [{i}] {bp?.ExtendedName}: " +
                        $"Pwr={current.PowerRequired}/{current.PowerProvided} " +
                        $"Hab={current.HabitationRequired}/{current.HabitationProvision} " +
                        $"Food={current.FoodRequired}/{current.FoodProvision} " +
                        $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}");
                }

                prev = current;
            }
        }

        private void AssertExactSequence(
            List<ColonyStructure> result,
            (string Type, int PwrReq, int PwrProv, int HabReq, int HabProv,
             int FoodReq, int FoodProv, int EntReq, int EntProv)[] expected)
        {
            Assert.That(result.Count, Is.EqualTo(expected.Length),
                $"Expected {expected.Length} structures but got {result.Count}");

            var calc = new ColonyStatusCalculator(new Colony());
            var iw = new IdealColonyStructureWorkers();
            var prev = new ColonyStructureStatus();

            for (int i = 0; i < result.Count; i++)
            {
                var s = result[i];
                var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calc.CalculateBuilt(s, prev, current, iw, bp);

                var e = expected[i];
                Assert.That(bp.BluePrintType, Is.EqualTo(e.Type),
                    $"[{i}] Type: expected {e.Type}, got {bp.BluePrintType}");
                Assert.That(current.PowerRequired, Is.EqualTo(e.PwrReq),
                    $"[{i}] {bp.BluePrintType} PwrReq: expected {e.PwrReq}, got {current.PowerRequired}");
                Assert.That(current.PowerProvided, Is.EqualTo(e.PwrProv),
                    $"[{i}] {bp.BluePrintType} PwrProv: expected {e.PwrProv}, got {current.PowerProvided}");
                Assert.That(current.HabitationRequired, Is.EqualTo(e.HabReq),
                    $"[{i}] {bp.BluePrintType} HabReq: expected {e.HabReq}, got {current.HabitationRequired}");
                Assert.That(current.HabitationProvision, Is.EqualTo(e.HabProv),
                    $"[{i}] {bp.BluePrintType} HabProv: expected {e.HabProv}, got {current.HabitationProvision}");
                Assert.That(current.FoodRequired, Is.EqualTo(e.FoodReq),
                    $"[{i}] {bp.BluePrintType} FoodReq: expected {e.FoodReq}, got {current.FoodRequired}");
                Assert.That(current.FoodProvision, Is.EqualTo(e.FoodProv),
                    $"[{i}] {bp.BluePrintType} FoodProv: expected {e.FoodProv}, got {current.FoodProvision}");
                Assert.That(current.EntertainmentRequired, Is.EqualTo(e.EntReq),
                    $"[{i}] {bp.BluePrintType} EntReq: expected {e.EntReq}, got {current.EntertainmentRequired}");
                Assert.That(current.EntertainmentProvided, Is.EqualTo(e.EntProv),
                    $"[{i}] {bp.BluePrintType} EntProv: expected {e.EntProv}, got {current.EntertainmentProvided}");

                prev = current;
            }
        }

        private void LogOrderWithStatus(List<ColonyStructure> result)
        {
            var calc = new ColonyStatusCalculator(new Colony());
            var iw = new IdealColonyStructureWorkers();
            var prev = new ColonyStructureStatus();

            TestContext.WriteLine($"Output: {result.Count} structures");
            for (int i = 0; i < result.Count; i++)
            {
                var s = result[i];
                var bp = playerContext.FindBlueprint(s.FlatpackBlueprintUUID);
                var current = new ColonyStructureStatus();
                calc.CalculateBuilt(s, prev, current, iw, bp);

                bool deficit = current.PowerRequired > current.PowerProvided ||
                               current.HabitationRequired > current.HabitationProvision ||
                               current.FoodRequired > current.FoodProvision ||
                               current.EntertainmentRequired > current.EntertainmentProvided;

                TestContext.WriteLine(
                    $"  [{i}] {bp?.ExtendedName,-40} " +
                    $"Pwr={current.PowerRequired}/{current.PowerProvided} " +
                    $"Hab={current.HabitationRequired}/{current.HabitationProvision} " +
                    $"Food={current.FoodRequired}/{current.FoodProvision} " +
                    $"Ent={current.EntertainmentRequired}/{current.EntertainmentProvided}" +
                    (deficit ? " *** DEFICIT ***" : string.Empty));

                prev = current;
            }
        }
    }
}
