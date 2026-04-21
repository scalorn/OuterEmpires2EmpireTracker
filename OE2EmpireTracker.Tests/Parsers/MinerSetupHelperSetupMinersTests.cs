using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Integration tests for MinerSetupHelper.SetupMiners orchestrator.
    /// Validates: Requirements 4.1, 4.2, 4.3
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperSetupMinersTests
    {
        private PlayerContext _playerContext;
        private EmpireContext _empireContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            _empireContext = EmpireContext.GetInstance();
            _playerContext = EmpireContext.PlayerContext;
            foreach (var item in _playerContext.SurveyList.ToList()) _playerContext.RemoveSurvey(item);
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        private Colony CreateColony(string ownerUUID, string planetName, string systemName)
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = ownerUUID,
                PlanetName = planetName,
                SystemName = systemName,
                ColonyName = "Test Colony"
            };
        }

        private ColonyStructure CreateMiningRig(string uuid, string resource, string purity)
        {
            return new ColonyStructure
            {
                UUID = uuid,
                MiningSurveyResource = resource,
                RefiningResourcePurity = purity
            };
        }

        private ColonyStructure CreateNonMiningStructure(string uuid)
        {
            return new ColonyStructure
            {
                UUID = uuid
            };
        }

        private Survey CreateRealSurvey(string uuid, string planetName, string surveyId,
            string resourceName, string purity, string amount)
        {
            return new Survey("Test Survey")
            {
                UUID = uuid,
                PlanetName = planetName,
                SurveyID = surveyId,
                Resources = new Dictionary<string, SurveyResource>
                {
                    [resourceName] = new SurveyResource(resourceName, purity, amount)
                }
            };
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// Full pipeline: new mining rig with a real survey available gets survey assigned,
        /// warehouse resource created, and timer started.
        /// </summary>
        [Test]
        public void SetupMiners_FullPipeline_NewMinerWithRealSurvey()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var miner = CreateMiningRig("rig-1", "Iron", "Medium");
            colony.Structures.Add(miner);

            var realSurvey = CreateRealSurvey("survey-1", "Alpha Prime", "SURV-001", "Iron", "Medium", "120");
            _playerContext.AddSurvey(realSurvey);

            var maxRates = new Dictionary<string, decimal> { { "rig-1", 120m } };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: survey assigned
            Assert.That(miner.MiningSurvey, Is.EqualTo("survey-1"));

            // Assert: warehouse resource created
            var warehouseItems = colony.Items.FindResource("Iron", "Medium");
            Assert.That(warehouseItems.Count, Is.EqualTo(1));
            Assert.That(warehouseItems[0].Quantity, Is.EqualTo(0));

            // Assert: timer started
            Assert.That(miner.ProcessCompletionTime, Is.Not.Null);
            Assert.That(miner.ProcessCompletionTime.IsRepeating, Is.True);
            Assert.That(miner.ProcessCompletionTime.RepeatIntervalSeconds,
                Is.EqualTo(GameConstants.SecondsPerHour));
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// When no real survey exists, falls back to default survey creation.
        /// </summary>
        [Test]
        public void SetupMiners_FallsBackToDefaultSurvey_WhenNoRealSurveyExists()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Beta", "Proxima");
            var miner = CreateMiningRig("rig-1", "Gold", "High");
            colony.Structures.Add(miner);

            var maxRates = new Dictionary<string, decimal> { { "rig-1", 50m } };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: default survey assigned
            string expectedUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Beta", "Proxima");
            Assert.That(miner.MiningSurvey, Is.EqualTo(expectedUUID));

            // Assert: default survey created in SurveyList
            var defaultSurvey = _playerContext.SurveyList.FirstOrDefault(s => s.UUID == expectedUUID);
            Assert.That(defaultSurvey, Is.Not.Null);
            Assert.That(defaultSurvey.SurveyID, Is.EqualTo("DEFAULT"));
            Assert.That(defaultSurvey.Resources.ContainsKey("Gold"), Is.True);
            Assert.That(defaultSurvey.Resources["Gold"].Amount, Is.EqualTo("50"));

            // Assert: warehouse resource created
            var warehouseItems = colony.Items.FindResource("Gold", "High");
            Assert.That(warehouseItems.Count, Is.EqualTo(1));

            // Assert: timer started
            Assert.That(miner.ProcessCompletionTime, Is.Not.Null);
            Assert.That(miner.ProcessCompletionTime.IsRepeating, Is.True);
        }

        /// <summary>
        /// Validates: Requirements 4.2, 4.3
        /// Reimport scenario: existing miner with valid real survey is preserved.
        /// </summary>
        [Test]
        public void SetupMiners_PreservesExistingSurvey_OnReimport()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Gamma", "Vega");
            var realSurvey = CreateRealSurvey("survey-1", "Gamma", "SURV-001", "Copper", "Low", "80");
            _playerContext.AddSurvey(realSurvey);

            var miner = CreateMiningRig("rig-1", "Copper", "Low");
            miner.MiningSurvey = "survey-1"; // already assigned from previous import
            colony.Structures.Add(miner);

            var maxRates = new Dictionary<string, decimal> { { "rig-1", 80m } };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: survey preserved
            Assert.That(miner.MiningSurvey, Is.EqualTo("survey-1"));
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// Non-mining structures are skipped entirely.
        /// </summary>
        [Test]
        public void SetupMiners_SkipsNonMiningStructures()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Delta", "Rigel");
            var refinery = CreateNonMiningStructure("refinery-1");
            var lab = CreateNonMiningStructure("lab-1");
            colony.Structures.Add(refinery);
            colony.Structures.Add(lab);

            var maxRates = new Dictionary<string, decimal>();

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: no surveys assigned, no timers started
            Assert.That(refinery.MiningSurvey, Is.Null);
            Assert.That(refinery.ProcessCompletionTime, Is.Null);
            Assert.That(lab.MiningSurvey, Is.Null);
            Assert.That(lab.ProcessCompletionTime, Is.Null);
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// When maxRate is 0, survey is assigned but timer is NOT started.
        /// </summary>
        [Test]
        public void SetupMiners_SkipsTimer_WhenMaxRateIsZero()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Epsilon", "Altair");
            var miner = CreateMiningRig("rig-1", "Silver", "Medium");
            colony.Structures.Add(miner);

            // maxRate = 0 means miner is assigned but not actively mining
            var maxRates = new Dictionary<string, decimal> { { "rig-1", 0m } };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: survey assigned (default since no real survey)
            Assert.That(miner.MiningSurvey, Is.Not.Null.And.Not.Empty);

            // Assert: timer NOT started
            Assert.That(miner.ProcessCompletionTime, Is.Null);
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// Multiple miners on the same colony: each gets its own survey and timer.
        /// </summary>
        [Test]
        public void SetupMiners_HandlesMultipleMiners_WithDifferentResources()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Zeta", "Sirius");
            var ironMiner = CreateMiningRig("rig-iron", "Iron", "Medium");
            var goldMiner = CreateMiningRig("rig-gold", "Gold", "High");
            colony.Structures.Add(ironMiner);
            colony.Structures.Add(goldMiner);

            var ironSurvey = CreateRealSurvey("s-iron", "Zeta", "SURV-001", "Iron", "Medium", "100");
            _playerContext.AddSurvey(ironSurvey);
            // No real survey for Gold -- will fall back to default

            var maxRates = new Dictionary<string, decimal>
            {
                { "rig-iron", 100m },
                { "rig-gold", 75m }
            };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: Iron miner gets real survey
            Assert.That(ironMiner.MiningSurvey, Is.EqualTo("s-iron"));

            // Assert: Gold miner gets default survey
            string defaultUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Zeta", "Sirius");
            Assert.That(goldMiner.MiningSurvey, Is.EqualTo(defaultUUID));

            // Assert: both have timers
            Assert.That(ironMiner.ProcessCompletionTime, Is.Not.Null);
            Assert.That(goldMiner.ProcessCompletionTime, Is.Not.Null);

            // Assert: warehouse has both resources
            Assert.That(colony.Items.FindResource("Iron", "Medium").Count, Is.EqualTo(1));
            Assert.That(colony.Items.FindResource("Gold", "High").Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// CleanupDefaultSurvey runs after all structures are processed,
        /// removing stale resources from the default survey.
        /// </summary>
        [Test]
        public void SetupMiners_CleansUpStaleDefaultSurveyResources()
        {
            // Arrange: colony with a pre-existing default survey that has a stale resource
            var colony = CreateColony("owner-1", "Eta", "Capella");
            string defaultUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Eta", "Capella");

            var defaultSurvey = new Survey("Default Survey")
            {
                UUID = defaultUUID,
                SurveyID = "DEFAULT",
                NickName = "",
                PlanetName = "Eta",
                SystemName = "Capella",
                OwnerUUID = "owner-1"
            };
            defaultSurvey.Resources["Iron"] = new SurveyResource("Iron", "Medium", "100");
            defaultSurvey.Resources["Copper"] = new SurveyResource("Copper", "High", "50");
            _playerContext.AddSurvey(defaultSurvey);

            // Only Iron miner exists -- Copper is stale
            var ironMiner = CreateMiningRig("rig-iron", "Iron", "Medium");
            colony.Structures.Add(ironMiner);

            var maxRates = new Dictionary<string, decimal> { { "rig-iron", 100m } };

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: Iron miner assigned to default survey
            Assert.That(ironMiner.MiningSurvey, Is.EqualTo(defaultUUID));

            // Assert: Copper removed from default survey, Iron preserved
            var updatedSurvey = _playerContext.SurveyList.FirstOrDefault(s => s.UUID == defaultUUID);
            Assert.That(updatedSurvey, Is.Not.Null);
            Assert.That(updatedSurvey.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(updatedSurvey.Resources.ContainsKey("Copper"), Is.False);
        }

        /// <summary>
        /// Validates: Requirements 4.1
        /// Handles null/empty maxRates gracefully -- miners with no entry get maxRate=0.
        /// </summary>
        [Test]
        public void SetupMiners_HandlesNullInputs_Gracefully()
        {
            // Null colony
            Assert.DoesNotThrow(() =>
                MinerSetupHelper.SetupMiners(null, _empireContext, new Dictionary<string, decimal>()));

            // Null empireContext
            Assert.DoesNotThrow(() =>
                MinerSetupHelper.SetupMiners(new Colony(), null, new Dictionary<string, decimal>()));

            // Null maxRates
            Assert.DoesNotThrow(() =>
                MinerSetupHelper.SetupMiners(new Colony(), _empireContext, null));
        }

        /// <summary>
        /// Validates: Requirements 4.1, 4.3
        /// When a miner's UUID is not in maxRates, it defaults to maxRate=0.
        /// Survey is still assigned but timer is not started.
        /// </summary>
        [Test]
        public void SetupMiners_DefaultsToZeroMaxRate_WhenNotInDictionary()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Theta", "Deneb");
            var miner = CreateMiningRig("rig-1", "Titanium", "Low");
            colony.Structures.Add(miner);

            // Empty maxRates -- rig-1 not present
            var maxRates = new Dictionary<string, decimal>();

            // Act
            MinerSetupHelper.SetupMiners(colony, _empireContext, maxRates);

            // Assert: survey assigned (default)
            Assert.That(miner.MiningSurvey, Is.Not.Null.And.Not.Empty);

            // Assert: timer NOT started (maxRate defaults to 0)
            Assert.That(miner.ProcessCompletionTime, Is.Null);
        }
    }
}
