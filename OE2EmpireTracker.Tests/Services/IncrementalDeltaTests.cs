using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class IncrementalDeltaTests
    {
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static OE2EmpireTracker.Models.Blueprint MakeBlueprint(string type, Dictionary<string, string> properties)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Test " + type);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = type;
            if (properties != null)
            {
                foreach (var kv in properties)
                    bp.Properties.SetProperty(kv.Key, kv.Value);
            }

            return bp;
        }

        private static ColonyStructure MakeStructure(string blueprintUUID, bool built, bool online, bool staged = false)
        {
            var s = new ColonyStructure();
            s.UUID = Guid.NewGuid().ToString();
            s.FlatpackBlueprintUUID = blueprintUUID;
            s.Properties.SetProperty(GameConstants.PropBuilt, built);
            s.Properties.SetProperty(GameConstants.PropOnline, online);
            s.Properties.SetProperty(GameConstants.PropStaged, staged);
            return s;
        }

        /// <summary>
        /// Builds a colony with a reactor, habitation, and mining rig, adds blueprints to PlayerContext,
        /// and returns (colony, blueprints dictionary keyed by structure UUID).
        /// </summary>
        private (Colony colony, Dictionary<string, OE2EmpireTracker.Models.Blueprint> blueprints) BuildTestColony()
        {
            var pc = PlayerContext.GetInstance();

            // Reactor: online, provides power, requires 1 blue collar
            var reactorBp = MakeBlueprint("Power Plant", new Dictionary<string, string>
            {
                { GameConstants.PropPowerProvided, "500" },
                { GameConstants.PropBlueCollarDetail, "1" }
            });
            pc.AddBlueprint(reactorBp);

            // Habitation: online, provides habitation, requires power, 1 white collar
            var habBp = MakeBlueprint("Habitation", new Dictionary<string, string>
            {
                { GameConstants.PropPowerRequired, "50" },
                { GameConstants.PropHabitationProvision, "100" },
                { GameConstants.PropWhiteCollarDetail, "1" },
                { GameConstants.PropFoodProvision, "20" }
            });
            pc.AddBlueprint(habBp);

            // Mining rig: online, requires power, 1 blue collar, 1 specialist
            var minerBp = MakeBlueprint("Mining Rig", new Dictionary<string, string>
            {
                { GameConstants.PropPowerRequired, "75" },
                { GameConstants.PropEntertainmentProvided, "10" },
                { GameConstants.PropWarehouseCapacity, "2000" },
                { GameConstants.PropBlueCollarDetail, "1" },
                { GameConstants.PropSpecialistDetail, "1" }
            });
            pc.AddBlueprint(minerBp);

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var reactor = MakeStructure(reactorBp.UUID, built: true, online: true);
            reactor.AssignedWorkers.SetProperty("BlueCollar1", true);

            var hab = MakeStructure(habBp.UUID, built: true, online: true);
            hab.AssignedWorkers.SetProperty("WhiteCollar1", true);

            var miner = MakeStructure(minerBp.UUID, built: true, online: true);
            miner.AssignedWorkers.SetProperty("BlueCollar1", true);
            miner.AssignedWorkers.SetProperty("Specialist1", true);

            colony.Structures.Add(reactor);
            colony.Structures.Add(hab);
            colony.Structures.Add(miner);

            var blueprints = new Dictionary<string, OE2EmpireTracker.Models.Blueprint>
            {
                { reactor.UUID, reactorBp },
                { hab.UUID, habBp },
                { miner.UUID, minerBp }
            };

            return (colony, blueprints);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up: reset singletons so test blueprints don't leak
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // 4.5a: After CalculateBuilt(), each structure has a non-null StatusDelta
        // -----------------------------------------------------------------------

        [Test]
        public void CalculateBuilt_PopulatesStatusDeltaOnEveryStructure()
        {
            var (colony, _) = BuildTestColony();
            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            foreach (var structure in colony.Structures)
            {
                Assert.That(structure.StatusDelta, Is.Not.Null,
                    $"Structure {structure.UUID} should have a non-null StatusDelta after CalculateBuilt()");
            }
        }

        // -----------------------------------------------------------------------
        // 4.5b: SumAllDeltas produces same totals as the full CalculateBuilt FinalActualStatus
        // -----------------------------------------------------------------------

        [Test]
        public void SumAllDeltas_MatchesFullCalculation()
        {
            var (colony, _) = BuildTestColony();
            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            // Save the running-accumulator FinalActualStatus values (from last structure's cumulative status)
            // The SumAllDeltas call inside CalculateBuilt() already overwrites FinalActualStatus,
            // so we verify the final values are consistent with what the deltas would produce.

            // Manually sum deltas to cross-check
            decimal powerProv = 0, powerReq = 0, habProv = 0, foodProv = 0;
            decimal entProv = 0, whCap = 0;
            int workers = 0, unalloc = 0;

            foreach (var s in colony.Structures)
            {
                var d = s.StatusDelta;
                powerProv += d.PowerProvided;
                powerReq += d.PowerRequired;
                habProv += d.HabitationProvision;
                foodProv += d.FoodProvision;
                entProv += d.EntertainmentProvided;
                whCap += d.WarehouseCapacity;
                workers += d.WorkerCount;
                unalloc += d.UnallocatedCount;
            }

            Assert.That(calc.FinalActualStatus.PowerProvided, Is.EqualTo(powerProv).Within(0.01m),
                "PowerProvided mismatch");
            Assert.That(calc.FinalActualStatus.PowerRequired, Is.EqualTo(powerReq).Within(0.01m),
                "PowerRequired mismatch");
            Assert.That(calc.FinalActualStatus.HabitationProvision, Is.EqualTo(habProv).Within(0.01m),
                "HabitationProvision mismatch");
            Assert.That(calc.FinalActualStatus.FoodProvision, Is.EqualTo(foodProv).Within(0.01m),
                "FoodProvision mismatch");
            Assert.That(calc.FinalActualStatus.EntertainmentProvided, Is.EqualTo(entProv).Within(0.01m),
                "EntertainmentProvided mismatch");
            Assert.That(calc.FinalActualStatus.WarehouseCapacity, Is.EqualTo(whCap).Within(0.01m),
                "WarehouseCapacity mismatch");

            int totalPeople = workers + unalloc;
            Assert.That(calc.FinalActualStatus.HabitationRequired, Is.EqualTo((decimal)totalPeople).Within(0.01m),
                "HabitationRequired mismatch");
            Assert.That(calc.FinalActualStatus.FoodRequired, Is.EqualTo((decimal)totalPeople).Within(0.01m),
                "FoodRequired mismatch");
            Assert.That(calc.FinalActualStatus.EntertainmentRequired, Is.EqualTo((decimal)(totalPeople * 2)).Within(0.01m),
                "EntertainmentRequired mismatch");
        }

        // -----------------------------------------------------------------------
        // 4.5c: RecalculateStructure after state change matches full recalculation
        // -----------------------------------------------------------------------

        [Test]
        public void RecalculateStructure_AfterStateChange_MatchesFullRecalculation()
        {
            var (colony, blueprints) = BuildTestColony();
            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            // Change the miner's state: take it offline
            var miner = colony.Structures[2];
            miner.Properties.SetProperty(GameConstants.PropOnline, false);

            // Use incremental recalculation
            calc.RecalculateStructure(miner);

            // Save incremental results
            decimal incPowerProv = calc.FinalActualStatus.PowerProvided;
            decimal incPowerReq = calc.FinalActualStatus.PowerRequired;
            decimal incHabProv = calc.FinalActualStatus.HabitationProvision;
            decimal incFoodProv = calc.FinalActualStatus.FoodProvision;
            decimal incEntProv = calc.FinalActualStatus.EntertainmentProvided;
            decimal incWhCap = calc.FinalActualStatus.WarehouseCapacity;
            decimal incHabReq = calc.FinalActualStatus.HabitationRequired;
            decimal incFoodReq = calc.FinalActualStatus.FoodRequired;
            decimal incEntReq = calc.FinalActualStatus.EntertainmentRequired;

            // Now do a full recalculation from scratch
            var calc2 = new ColonyStatusCalculator(colony);
            calc2.CalculateBuilt();

            Assert.That(incPowerProv, Is.EqualTo(calc2.FinalActualStatus.PowerProvided).Within(0.01m),
                "PowerProvided mismatch after RecalculateStructure");
            Assert.That(incPowerReq, Is.EqualTo(calc2.FinalActualStatus.PowerRequired).Within(0.01m),
                "PowerRequired mismatch after RecalculateStructure");
            Assert.That(incHabProv, Is.EqualTo(calc2.FinalActualStatus.HabitationProvision).Within(0.01m),
                "HabitationProvision mismatch after RecalculateStructure");
            Assert.That(incFoodProv, Is.EqualTo(calc2.FinalActualStatus.FoodProvision).Within(0.01m),
                "FoodProvision mismatch after RecalculateStructure");
            Assert.That(incEntProv, Is.EqualTo(calc2.FinalActualStatus.EntertainmentProvided).Within(0.01m),
                "EntertainmentProvided mismatch after RecalculateStructure");
            Assert.That(incWhCap, Is.EqualTo(calc2.FinalActualStatus.WarehouseCapacity).Within(0.01m),
                "WarehouseCapacity mismatch after RecalculateStructure");
            Assert.That(incHabReq, Is.EqualTo(calc2.FinalActualStatus.HabitationRequired).Within(0.01m),
                "HabitationRequired mismatch after RecalculateStructure");
            Assert.That(incFoodReq, Is.EqualTo(calc2.FinalActualStatus.FoodRequired).Within(0.01m),
                "FoodRequired mismatch after RecalculateStructure");
            Assert.That(incEntReq, Is.EqualTo(calc2.FinalActualStatus.EntertainmentRequired).Within(0.01m),
                "EntertainmentRequired mismatch after RecalculateStructure");
        }

        // -----------------------------------------------------------------------
        // 4.5d: RecalculateStructure after worker change matches full recalculation
        // -----------------------------------------------------------------------

        [Test]
        public void RecalculateStructure_AfterWorkerChange_MatchesFullRecalculation()
        {
            var (colony, blueprints) = BuildTestColony();
            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            // Unassign the specialist from the miner
            var miner = colony.Structures[2];
            miner.AssignedWorkers.SetProperty("Specialist1", false);

            // Use incremental recalculation
            calc.RecalculateStructure(miner);

            // Save incremental results
            decimal incHabReq = calc.FinalActualStatus.HabitationRequired;
            decimal incFoodReq = calc.FinalActualStatus.FoodRequired;
            decimal incEntReq = calc.FinalActualStatus.EntertainmentRequired;

            // Full recalculation
            var calc2 = new ColonyStatusCalculator(colony);
            calc2.CalculateBuilt();

            Assert.That(incHabReq, Is.EqualTo(calc2.FinalActualStatus.HabitationRequired).Within(0.01m),
                "HabitationRequired mismatch after worker unassign");
            Assert.That(incFoodReq, Is.EqualTo(calc2.FinalActualStatus.FoodRequired).Within(0.01m),
                "FoodRequired mismatch after worker unassign");
            Assert.That(incEntReq, Is.EqualTo(calc2.FinalActualStatus.EntertainmentRequired).Within(0.01m),
                "EntertainmentRequired mismatch after worker unassign");
        }

        // -----------------------------------------------------------------------
        // 4.5e: ComputeStructureDelta with null blueprint returns zero delta
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeStructureDelta_NullBlueprint_ReturnsZeroDelta()
        {
            var colony = new Colony();
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            colony.Structures.Add(structure);

            var calc = new ColonyStatusCalculator(colony);
            var delta = calc.ComputeStructureDelta(structure, null);

            Assert.That(delta.PowerProvided, Is.EqualTo(0m));
            Assert.That(delta.PowerRequired, Is.EqualTo(0m));
            Assert.That(delta.HabitationProvision, Is.EqualTo(0m));
            Assert.That(delta.FoodProvision, Is.EqualTo(0m));
            Assert.That(delta.EntertainmentProvided, Is.EqualTo(0m));
            Assert.That(delta.WarehouseCapacity, Is.EqualTo(0m));
            Assert.That(delta.WorkerCount, Is.EqualTo(0));
            Assert.That(delta.UnallocatedCount, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // 4.5f: Offline structure delta has zero for online-only fields but keeps food
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeStructureDelta_OfflineStructure_OnlyAccumulatesFood()
        {
            var colony = new Colony();
            var bp = MakeBlueprint("Power Plant", new Dictionary<string, string>
            {
                { GameConstants.PropPowerProvided, "100" },
                { GameConstants.PropFoodProvision, "50" },
                { GameConstants.PropHabitationProvision, "30" },
                { GameConstants.PropEntertainmentProvided, "10" },
                { GameConstants.PropWarehouseCapacity, "500" }
            });
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = MakeStructure(bp.UUID, built: true, online: false);
            colony.Structures.Add(structure);

            var calc = new ColonyStatusCalculator(colony);
            var delta = calc.ComputeStructureDelta(structure, bp);

            Assert.That(delta.PowerProvided, Is.EqualTo(0m), "Offline should not provide power");
            Assert.That(delta.HabitationProvision, Is.EqualTo(0m), "Offline should not provide habitation");
            Assert.That(delta.EntertainmentProvided, Is.EqualTo(0m), "Offline should not provide entertainment");
            Assert.That(delta.WarehouseCapacity, Is.EqualTo(0m), "Offline should not provide warehouse");
            Assert.That(delta.FoodProvision, Is.EqualTo(50m), "Food should accumulate regardless of online state");
        }
    }
}
