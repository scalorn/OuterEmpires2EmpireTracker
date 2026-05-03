using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class StructureAndBlueprintCacheTests
    {
        private PlayerContext _playerContext;
        private EmpireContext _empireContext;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            _empireContext = EmpireContext.GetInstance();
            _playerContext = PlayerContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        // ------- ColonyStructureViewModel creation via CollectionSortHelper -------

        [Test]
        public void OrderStructures_ReturnsCorrectVMs()
        {
            var colony = new Colony
            {
                UUID = "col-vm-1",
                PlanetName = "TestPlanet",
                ColonyName = "TestColony"
            };

            var bp = new Bp("Power Plant") { UUID = "bp-pp-1", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(bp);
            _playerContext.InvalidateBlueprintCache();

            colony.Structures.Add(new ColonyStructure
            {
                UUID = "str-1",
                FlatpackBlueprintUUID = "bp-pp-1",
                DisplaySequence = 1
            });
            colony.Structures.Add(new ColonyStructure
            {
                UUID = "str-2",
                FlatpackBlueprintUUID = "bp-pp-1",
                DisplaySequence = 2
            });

            var structureVMs = CollectionSortHelper.OrderStructures(colony.Structures)
                .Select(s => new ColonyStructureViewModel(s, _playerContext))
                .ToList();

            Assert.That(structureVMs.Count, Is.EqualTo(2));
            Assert.That(structureVMs[0].Data.UUID, Is.EqualTo("str-1"));
            Assert.That(structureVMs[1].Data.UUID, Is.EqualTo("str-2"));
        }

        [Test]
        public void ColonyService_AddStructure_IncreasesCount()
        {
            var colony = new Colony
            {
                UUID = "col-vm-3",
                PlanetName = "TestPlanet",
                ColonyName = "TestColony",
                OwnerUUID = "player1"
            };

            var bp = new Bp("Mining Rig") { UUID = "bp-mr-1", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(bp);
            _playerContext.InvalidateBlueprintCache();
            _playerContext.AddColony(colony);
            _playerContext.InvalidateColonyCache();

            Assert.That(colony.Structures.Count, Is.EqualTo(0));

            var service = new ColonyService(_playerContext);
            service.AddStructure("col-vm-3", "bp-mr-1");

            Assert.That(colony.Structures.Count, Is.EqualTo(1));
        }

        // ------- GetAllBlueprints cache -------

        [Test]
        public void GetAllBlueprints_ReturnsCombinedPlayerAndGlobal()
        {
            var localBp = new Bp("Local BP") { UUID = "bp-local-1", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(localBp);

            var globalBp = new Bp("Global BP") { UUID = "bp-global-1" };
            _empireContext.AddGlobalBlueprint(globalBp);

            _playerContext.InvalidateBlueprintCache();

            var all = _playerContext.GetAllBlueprints();

            Assert.That(all.Any(b => b.UUID == "bp-local-1"), Is.True, "Should contain local blueprint");
            Assert.That(all.Any(b => b.UUID == "bp-global-1"), Is.True, "Should contain global blueprint");
        }

        [Test]
        public void GetAllBlueprints_ReturnsCachedInstance()
        {
            var first = _playerContext.GetAllBlueprints();
            var second = _playerContext.GetAllBlueprints();

            Assert.That(ReferenceEquals(first, second), Is.True, "Should return same cached list");
        }

        [Test]
        public void GetAllBlueprints_InvalidatedWhenBlueprintListChanges()
        {
            var before = _playerContext.GetAllBlueprints();
            int countBefore = before.Count;

            // Add a new player blueprint and explicitly invalidate the cache
            var newBp = new Bp("New BP") { UUID = "bp-new-all", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(newBp);
            _playerContext.InvalidateBlueprintCache();

            var after = _playerContext.GetAllBlueprints();

            Assert.That(after.Count, Is.EqualTo(countBefore + 1));
            Assert.That(after.Any(b => b.UUID == "bp-new-all"), Is.True);
        }

        [Test]
        public void GetAllBlueprints_InvalidatedByExplicitCall()
        {
            var first = _playerContext.GetAllBlueprints();
            _playerContext.InvalidateAllBlueprintsCache();
            var second = _playerContext.GetAllBlueprints();

            Assert.That(ReferenceEquals(first, second), Is.False, "Should be a new list after invalidation");
        }
    }
}
