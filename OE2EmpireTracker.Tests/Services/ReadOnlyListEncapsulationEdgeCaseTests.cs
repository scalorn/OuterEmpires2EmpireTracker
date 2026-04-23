using System;
using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Edge case unit tests for read-only list encapsulation.
    /// Validates: Requirements 3.5, 3.6, 7.4
    /// </summary>
    [TestFixture]
    public class ReadOnlyListEncapsulationEdgeCaseTests
    {
        private string _tempDir;
        private PlayerContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OE2Tests_Edge_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var playerJson = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(playerJson, "{\"DataVersion\":7, \"CurrentPlayerUUID\":\"\", \"PlayerProfile\":[], \"Blueprint\":[], \"Survey\":[], \"Colony\":[], \"DeliveryRoute\":[], \"DeliveryPlan\":[], \"PricingPlan\":[], \"BuildPlan\":[], \"ShipTemplate\":[], \"Ship\":[], \"Station\":[], \"MarketListing\":[], \"MarketTransaction\":[], \"StockPlan\":[], \"StockProfile\":[], \"SupplyChain\":[], \"WarehouseOverflowRule\":[], \"Faction\":[], \"ExternalCharacter\":[], \"Asteroid\":[]}");

            var baselineJson = Path.Combine(_tempDir, "BaselineData.json");
            File.WriteAllText(baselineJson, "{\"BlueprintType\":[], \"ShipClass\":[], \"TechLevel\":[], \"Evolution\":[], \"Resource\":[], \"ResourceGroup\":[], \"Commodity\":[], \"GlobalBlueprint\":[]}");

            MigrationRunner.SuppressUI = true;
            EmpireContext.Reset();
            EmpireContext.FilePath = baselineJson;
            PlayerContext.FilePath = playerJson;
            PlayerContext.Reset();
            _ctx = PlayerContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Test]
        public void AddBlueprint_NullUUID_AddedToList_FindReturnsNullForNull()
        {
            var bp = new Bp("Null UUID Blueprint") { UUID = null };
            _ctx.AddBlueprint(bp);

            Assert.That(_ctx.BlueprintList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindBlueprint(null), Is.Null);
        }

        [Test]
        public void AddSurvey_NullUUID_AddedToList_FindReturnsNullForNull()
        {
            var survey = new Survey("Null UUID Survey") { UUID = null, PlanetName = "X" };
            _ctx.AddSurvey(survey);

            Assert.That(_ctx.SurveyList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindSurvey(null), Is.Null);
        }

        [Test]
        public void AddColony_NullUUID_AddedToList_FindReturnsNullForNull()
        {
            var colony = new Colony { UUID = null, PlanetName = "X", ColonyName = "X" };
            _ctx.AddColony(colony);

            Assert.That(_ctx.ColonyList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindColony(null), Is.Null);
        }

        [Test]
        public void RemoveBlueprint_NonExistent_NoException_ListUnchanged()
        {
            var existing = new Bp("Existing") { UUID = "bp-1" };
            _ctx.AddBlueprint(existing);

            var nonExistent = new Bp("Ghost") { UUID = "bp-999" };
            Assert.DoesNotThrow(() => _ctx.RemoveBlueprint(nonExistent));
            Assert.That(_ctx.BlueprintList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindBlueprint("bp-1"), Is.SameAs(existing));
        }

        [Test]
        public void RemoveSurvey_NonExistent_NoException_ListUnchanged()
        {
            var existing = new Survey("Existing") { UUID = "sv-1", PlanetName = "X" };
            _ctx.AddSurvey(existing);

            var nonExistent = new Survey("Ghost") { UUID = "sv-999", PlanetName = "Y" };
            Assert.DoesNotThrow(() => _ctx.RemoveSurvey(nonExistent));
            Assert.That(_ctx.SurveyList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindSurvey("sv-1"), Is.SameAs(existing));
        }

        [Test]
        public void RemoveColony_NonExistent_NoException_ListUnchanged()
        {
            var existing = new Colony { UUID = "col-1", PlanetName = "X", ColonyName = "X" };
            _ctx.AddColony(existing);

            var nonExistent = new Colony { UUID = "col-999", PlanetName = "Y", ColonyName = "Y" };
            Assert.DoesNotThrow(() => _ctx.RemoveColony(nonExistent));
            Assert.That(_ctx.ColonyList.Count, Is.EqualTo(1));
            Assert.That(_ctx.FindColony("col-1"), Is.SameAs(existing));
        }

        [Test]
        public void BindingSourceBlueprint_CountReflectsAddAndRemove()
        {
            var bs = _ctx.BindingSourceBlueprint;
            Assert.That(bs.Count, Is.EqualTo(0));

            var bp = new Bp("Test") { UUID = "bp-bs-1" };
            _ctx.AddBlueprint(bp);
            Assert.That(bs.Count, Is.EqualTo(1));

            _ctx.RemoveBlueprint(bp);
            Assert.That(bs.Count, Is.EqualTo(0));
        }

        [Test]
        public void BindingSourceSurvey_CountReflectsAddAndRemove()
        {
            var bs = _ctx.BindingSourceSurvey;
            Assert.That(bs.Count, Is.EqualTo(0));

            var survey = new Survey("Test") { UUID = "sv-bs-1", PlanetName = "X" };
            _ctx.AddSurvey(survey);
            Assert.That(bs.Count, Is.EqualTo(1));

            _ctx.RemoveSurvey(survey);
            Assert.That(bs.Count, Is.EqualTo(0));
        }

        [Test]
        public void BindingSourceColony_CountReflectsAddAndRemove()
        {
            var bs = _ctx.BindingSourceColony;
            Assert.That(bs.Count, Is.EqualTo(0));

            var colony = new Colony { UUID = "col-bs-1", PlanetName = "X", ColonyName = "X" };
            _ctx.AddColony(colony);
            Assert.That(bs.Count, Is.EqualTo(1));

            _ctx.RemoveColony(colony);
            Assert.That(bs.Count, Is.EqualTo(0));
        }
    }
}
