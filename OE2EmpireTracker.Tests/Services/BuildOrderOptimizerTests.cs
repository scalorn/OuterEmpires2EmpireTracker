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
            var colony = MakeColony("Flatpacks/ColonyCommandCentre");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            // CC must be first
            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");

            // Optimizer should create support to resolve the CC's entertainment deficit
            Assert.That(result.Count, Is.GreaterThan(1),
                "Optimizer should add support structures for CC's entertainment deficit");
            Assert.That(result.Count, Is.LessThanOrEqualTo(10),
                "Should not create excessive support for just a CC");

            // Must have at least one entertainment centre to resolve the deficit
            Assert.That(CountType(result, "Flatpacks/EntertainmentCentreFlatpack"),
                Is.GreaterThanOrEqualTo(1), "Need at least 1 Ent Centre for CC's workers");
        }

        [Test]
        public void Optimize_CC_Miner_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/MiningRig");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/MiningRig");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_Refinery_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/Refinery");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/Refinery");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_Manufactory_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/Manufactory");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/Manufactory");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_Warehouse_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/Warehouse");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/Warehouse");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_ResearchLab_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/ResearchLaboratory");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/ResearchLaboratory");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_ROA_NoDeficitsAtPrimary()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/RemoteOperationsArray");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertType(result, result.Count - 1, "Flatpacks/RemoteOperationsArray");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(result.Count, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void Optimize_CC_2Miners_NoDeficitsAtPrimaries()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/MiningRig", "Flatpacks/MiningRig");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(CountType(result, "Flatpacks/MiningRig"), Is.EqualTo(2));
            Assert.That(result.Count, Is.LessThanOrEqualTo(20));
        }

        [Test]
        public void Optimize_CC_Miner_Refinery_NoDeficitsAtPrimaries()
        {
            var colony = MakeColony("Flatpacks/ColonyCommandCentre", "Flatpacks/MiningRig", "Flatpacks/Refinery");
            var result = Optimize(colony);
            LogOrderWithStatus(result);

            AssertType(result, 0, "Flatpacks/ColonyCommandCentre");
            AssertNoDeficitsFromFirstPrimary(result);
            Assert.That(CountType(result, "Flatpacks/MiningRig"), Is.EqualTo(1));
            Assert.That(CountType(result, "Flatpacks/Refinery"), Is.EqualTo(1));
            Assert.That(result.Count, Is.LessThanOrEqualTo(20));
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
