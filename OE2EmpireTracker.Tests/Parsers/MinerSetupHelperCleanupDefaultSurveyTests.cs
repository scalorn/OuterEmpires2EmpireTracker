using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for MinerSetupHelper.CleanupDefaultSurvey.
    /// Validates: Requirements 6.9
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperCleanupDefaultSurveyTests
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
                UUID = System.Guid.NewGuid().ToString(),
                OwnerUUID = ownerUUID,
                PlanetName = planetName,
                SystemName = systemName,
                ColonyName = "Test Colony"
            };
        }

        private Survey CreateDefaultSurvey(Colony colony, Dictionary<string, SurveyResource> resources)
        {
            string uuid = DeterministicUUID.GenerateDefaultSurvey(
                colony.OwnerUUID, colony.PlanetName, colony.SystemName);
            var survey = new Survey("Default Survey")
            {
                UUID = uuid,
                SurveyID = "DEFAULT",
                NickName = "",
                PlanetName = colony.PlanetName,
                SystemName = colony.SystemName,
                OwnerUUID = colony.OwnerUUID
            };
            foreach (var kvp in resources)
            {
                survey.Resources[kvp.Key] = kvp.Value;
            }

            _playerContext.AddSurvey(survey);
            return survey;
        }

        private ColonyStructure CreateMiningStructure(string miningSurvey, string miningSurveyResource)
        {
            return new ColonyStructure
            {
                UUID = System.Guid.NewGuid().ToString(),
                MiningSurvey = miningSurvey,
                MiningSurveyResource = miningSurveyResource
            };
        }

        [Test]
        public void CleanupDefaultSurvey_RemovesStaleResources()
        {
            // Arrange: default survey has Iron and Copper, but only Iron is actively mined
            var colony = CreateColony("owner-1", "Alpha", "Sol");
            var defaultSurvey = CreateDefaultSurvey(colony, new Dictionary<string, SurveyResource>
            {
                { "Iron", new SurveyResource("Iron", "Medium", "100") },
                { "Copper", new SurveyResource("Copper", "High", "50") }
            });

            colony.Structures.Add(CreateMiningStructure(defaultSurvey.UUID, "Iron"));

            // Act
            MinerSetupHelper.CleanupDefaultSurvey(colony, _playerContext, _empireContext);

            // Assert: Copper removed, Iron preserved
            Assert.That(defaultSurvey.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(defaultSurvey.Resources.ContainsKey("Copper"), Is.False);
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
        }

        [Test]
        public void CleanupDefaultSurvey_DeletesEmptyDefaultSurvey()
        {
            // Arrange: default survey has one resource, but no miner references it
            var colony = CreateColony("owner-2", "Beta", "Proxima");
            var defaultSurvey = CreateDefaultSurvey(colony, new Dictionary<string, SurveyResource>
            {
                { "Gold", new SurveyResource("Gold", "Low", "25") }
            });

            // No structures reference this default survey

            // Act
            MinerSetupHelper.CleanupDefaultSurvey(colony, _playerContext, _empireContext);

            // Assert: survey removed entirely
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(0));
        }

        [Test]
        public void CleanupDefaultSurvey_PreservesActiveResources()
        {
            // Arrange: default survey has two resources, both actively mined
            var colony = CreateColony("owner-3", "Gamma", "Vega");
            var defaultSurvey = CreateDefaultSurvey(colony, new Dictionary<string, SurveyResource>
            {
                { "Iron", new SurveyResource("Iron", "Medium", "100") },
                { "Copper", new SurveyResource("Copper", "High", "50") }
            });

            colony.Structures.Add(CreateMiningStructure(defaultSurvey.UUID, "Iron"));
            colony.Structures.Add(CreateMiningStructure(defaultSurvey.UUID, "Copper"));

            // Act
            MinerSetupHelper.CleanupDefaultSurvey(colony, _playerContext, _empireContext);

            // Assert: both resources preserved, survey still exists
            Assert.That(defaultSurvey.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(defaultSurvey.Resources.ContainsKey("Copper"), Is.True);
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
        }

        [Test]
        public void CleanupDefaultSurvey_NoOp_WhenNoDefaultSurveyExists()
        {
            // Arrange: colony with no default survey
            var colony = CreateColony("owner-4", "Delta", "Rigel");
            colony.Structures.Add(CreateMiningStructure("some-real-survey", "Iron"));

            // Act -- should not throw
            MinerSetupHelper.CleanupDefaultSurvey(colony, _playerContext, _empireContext);

            // Assert: survey list unchanged
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(0));
        }

        [Test]
        public void CleanupDefaultSurvey_IgnoresMinersPointingToOtherSurveys()
        {
            // Arrange: default survey has Iron, but the miner mining Iron points to a real survey
            var colony = CreateColony("owner-5", "Epsilon", "Altair");
            var defaultSurvey = CreateDefaultSurvey(colony, new Dictionary<string, SurveyResource>
            {
                { "Iron", new SurveyResource("Iron", "Medium", "100") }
            });

            // Miner mines Iron but points to a different (real) survey
            colony.Structures.Add(CreateMiningStructure("real-survey-uuid", "Iron"));

            // Act
            MinerSetupHelper.CleanupDefaultSurvey(colony, _playerContext, _empireContext);

            // Assert: Iron removed because no miner points to the default survey for it
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(0));
        }
    }
}
