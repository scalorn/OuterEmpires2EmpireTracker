using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class DictionaryCacheTests
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

        // ------- FindBlueprint -------

        [Test]
        public void FindBlueprint_ReturnsCorrectBlueprint_AfterCachBuilt()
        {
            // Add a known blueprint
            var bp = new Bp("Test Blueprint") { UUID = "bp-001", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(bp);
            _playerContext.InvalidateBlueprintCache();

            var result = _playerContext.FindBlueprint("bp-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("bp-001"));
            Assert.That(result.Name, Is.EqualTo("Test Blueprint"));
        }

        [Test]
        public void FindBlueprint_NullId_ReturnsNull()
        {
            var result = _playerContext.FindBlueprint(null);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBlueprint_EmptyId_ReturnsNull()
        {
            var result = _playerContext.FindBlueprint(string.Empty);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBlueprint_InvalidId_ReturnsNull()
        {
            var result = _playerContext.FindBlueprint("nonexistent-id");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBlueprint_CacheInvalidated_WhenBlueprintListChanges()
        {
            // Build the cache by calling FindBlueprint
            _playerContext.FindBlueprint("trigger-cache-build");

            // Add a new blueprint and invalidate
            var bp = new Bp("New BP") { UUID = "bp-new", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(bp);
            _playerContext.InvalidateBlueprintCache();

            // Should find the newly added blueprint
            var result = _playerContext.FindBlueprint("bp-new");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("New BP"));
        }

        [Test]
        public void FindBlueprint_FallsBackToGlobalBlueprint()
        {
            // Add a blueprint only to the global list
            var globalBp = new Bp("Global BP") { UUID = "global-001" };
            _empireContext.AddGlobalBlueprint(globalBp);
            _empireContext.InvalidateGlobalBlueprintCache();

            var result = _playerContext.FindBlueprint("global-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Global BP"));
        }

        [Test]
        public void FindBlueprint_PrefersLocalOverGlobal()
        {
            var localBp = new Bp("Local BP") { UUID = "bp-dup", OwnerUUID = "player1" };
            _playerContext.AddBlueprint(localBp);
            _playerContext.InvalidateBlueprintCache();

            var globalBp = new Bp("Global BP") { UUID = "bp-dup" };
            _empireContext.AddGlobalBlueprint(globalBp);
            _empireContext.InvalidateGlobalBlueprintCache();

            var result = _playerContext.FindBlueprint("bp-dup");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Local BP"));
        }

        // ------- FindGlobalBlueprint -------

        [Test]
        public void FindGlobalBlueprint_ReturnsCorrectBlueprint()
        {
            var bp = new Bp("Global Test") { UUID = "gbp-001" };
            _empireContext.AddGlobalBlueprint(bp);
            _empireContext.InvalidateGlobalBlueprintCache();

            var result = _empireContext.FindGlobalBlueprint("gbp-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Global Test"));
        }

        [Test]
        public void FindGlobalBlueprint_NullId_ReturnsNull()
        {
            Assert.That(_empireContext.FindGlobalBlueprint(null), Is.Null);
        }

        [Test]
        public void FindGlobalBlueprint_EmptyId_ReturnsNull()
        {
            Assert.That(_empireContext.FindGlobalBlueprint(string.Empty), Is.Null);
        }

        // ------- FindSurvey -------

        [Test]
        public void FindSurvey_ReturnsCorrectSurvey()
        {
            var survey = new Survey("Test Survey") { UUID = "sv-001", OwnerUUID = "player1", PlanetName = "Earth" };
            _playerContext.AddSurvey(survey);
            _playerContext.InvalidateSurveyCache();

            var result = _playerContext.FindSurvey("sv-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("sv-001"));
        }

        [Test]
        public void FindSurvey_NullId_ReturnsNull()
        {
            Assert.That(_playerContext.FindSurvey(null), Is.Null);
        }

        [Test]
        public void FindSurvey_EmptyId_ReturnsNull()
        {
            Assert.That(_playerContext.FindSurvey(string.Empty), Is.Null);
        }

        [Test]
        public void FindSurvey_InvalidId_ReturnsNull()
        {
            Assert.That(_playerContext.FindSurvey("nonexistent"), Is.Null);
        }

        [Test]
        public void FindSurvey_CacheInvalidated_WhenSurveyListChanges()
        {
            _playerContext.FindSurvey("trigger-cache");

            var survey = new Survey("New Survey") { UUID = "sv-new", OwnerUUID = "player1", PlanetName = "Mars" };
            _playerContext.AddSurvey(survey);
            _playerContext.InvalidateSurveyCache();

            var result = _playerContext.FindSurvey("sv-new");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("New Survey"));
        }

        // ------- FindColony -------

        [Test]
        public void FindColony_ReturnsCorrectColony()
        {
            var colony = new Colony { UUID = "col-001", OwnerUUID = "player1", PlanetName = "Venus", ColonyName = "Base Alpha" };
            _playerContext.AddColony(colony);
            _playerContext.InvalidateColonyCache();

            var result = _playerContext.FindColony("col-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("col-001"));
            Assert.That(result.ColonyName, Is.EqualTo("Base Alpha"));
        }

        [Test]
        public void FindColony_NullId_ReturnsNull()
        {
            Assert.That(_playerContext.FindColony(null), Is.Null);
        }

        [Test]
        public void FindColony_EmptyId_ReturnsNull()
        {
            Assert.That(_playerContext.FindColony(string.Empty), Is.Null);
        }

        [Test]
        public void FindColony_InvalidId_ReturnsNull()
        {
            Assert.That(_playerContext.FindColony("nonexistent"), Is.Null);
        }

        [Test]
        public void FindColony_CacheInvalidated_WhenColonyListChanges()
        {
            _playerContext.FindColony("trigger-cache");

            var colony = new Colony { UUID = "col-new", OwnerUUID = "player1", PlanetName = "Jupiter", ColonyName = "Base Beta" };
            _playerContext.AddColony(colony);
            _playerContext.InvalidateColonyCache();

            var result = _playerContext.FindColony("col-new");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ColonyName, Is.EqualTo("Base Beta"));
        }

        // ------- CascadeDeletePlayer invalidates all caches -------

        [Test]
        public void CascadeDeletePlayer_InvalidatesCaches()
        {
            string playerUUID = "player-to-delete";
            var bp = new Bp("DeleteMe BP") { UUID = "bp-del", OwnerUUID = playerUUID };
            var survey = new Survey("DeleteMe Survey") { UUID = "sv-del", OwnerUUID = playerUUID, PlanetName = "X" };
            var colony = new Colony { UUID = "col-del", OwnerUUID = playerUUID, PlanetName = "X", ColonyName = "X" };

            _playerContext.AddBlueprint(bp);
            _playerContext.AddSurvey(survey);
            _playerContext.AddColony(colony);
            _playerContext.InvalidateBlueprintCache();
            _playerContext.InvalidateSurveyCache();
            _playerContext.InvalidateColonyCache();

            // Build caches
            Assert.That(_playerContext.FindBlueprint("bp-del"), Is.Not.Null);
            Assert.That(_playerContext.FindSurvey("sv-del"), Is.Not.Null);
            Assert.That(_playerContext.FindColony("col-del"), Is.Not.Null);

            // Cascade delete
            _playerContext.CascadeDeletePlayer(playerUUID);

            // Items should no longer be found
            Assert.That(_playerContext.FindBlueprint("bp-del"), Is.Null);
            Assert.That(_playerContext.FindSurvey("sv-del"), Is.Null);
            Assert.That(_playerContext.FindColony("col-del"), Is.Null);
        }
    }
}
