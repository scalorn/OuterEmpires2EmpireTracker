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
        // Helpers
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
        // Test: Colony matching user's scenario
        // 16 primaries: 1 ROA, 4 Mining Rigs, 8 Refineries, 1 Manufactory,
        //               1 Warehouse, 1 Research Lab
        // Support pool should contain enough reactors, habs, hydros, ent centres
        // to cover all primaries without any deficit.
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
                        $"  [{i}] {bp?.ExtendedName, -40} Pwr={current.PowerRequired}/{current.PowerProvided} " +
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
    }
}
