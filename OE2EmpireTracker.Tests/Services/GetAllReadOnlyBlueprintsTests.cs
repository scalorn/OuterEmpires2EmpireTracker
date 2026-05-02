using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class GetAllReadOnlyBlueprintsTests
    {
        private PlayerContext _playerContext;
        private EmpireContext _empireContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            TestHelper.SetAllFilePaths();
            _empireContext = EmpireContext.GetInstance();
            _playerContext = PlayerContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            TestHelper.SetAllFilePaths();
        }

        [Test]
        public void GetAllReadOnlyBlueprints_ReturnsReadOnlyBlueprintInstances()
        {
            var localBp = new Bp("Local BP") { UUID = "bp-ro-1", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(localBp);
            _playerContext.InvalidateBlueprintCache();

            var all = _playerContext.GetAllReadOnlyBlueprints();

            Assert.That(all, Is.Not.Empty, "Should return at least one blueprint");
            Assert.That(all.All(b => b is ReadOnlyBlueprint), Is.True, "All items should be ReadOnlyBlueprint instances");
        }

        [Test]
        public void GetAllReadOnlyBlueprints_IncludesPlayerAndGlobalBlueprints()
        {
            var localBp = new Bp("Player BP") { UUID = "bp-ro-local", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(localBp);

            var globalBp = new Bp("Global BP") { UUID = "bp-ro-global" };
            _empireContext.AddGlobalBlueprint(globalBp);

            _playerContext.InvalidateBlueprintCache();

            var all = _playerContext.GetAllReadOnlyBlueprints();

            Assert.That(all.Any(b => b.UUID == "bp-ro-local"), Is.True, "Should contain player blueprint");
            Assert.That(all.Any(b => b.UUID == "bp-ro-global"), Is.True, "Should contain global blueprint");
        }

        [Test]
        public void GetAllReadOnlyBlueprints_CountMatchesGetAllBlueprints()
        {
            var localBp = new Bp("Count BP") { UUID = "bp-ro-count", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(localBp);
            _playerContext.InvalidateBlueprintCache();

            var mutableAll = _playerContext.GetAllBlueprints();
            var readOnlyAll = _playerContext.GetAllReadOnlyBlueprints();

            Assert.That(readOnlyAll.Count, Is.EqualTo(mutableAll.Count), "Read-only list should have same count as mutable list");
        }
    }
}
