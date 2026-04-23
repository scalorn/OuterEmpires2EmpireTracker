using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for MinerSetupHelper.AssignSurvey.
    /// Validates: Requirements 1.4, 1.5, 3.1, 3.2, 3.3
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperAssignSurveyTests
    {
        private PlayerContext _playerContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            EmpireContext.GetInstance();
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

        private ColonyStructure CreateMiningRig(string uuid, string resource, string purity, string existingSurvey = null)
        {
            return new ColonyStructure
            {
                UUID = uuid,
                MiningSurveyResource = resource,
                RefiningResourcePurity = purity,
                MiningSurvey = existingSurvey
            };
        }

        private Survey CreateSurvey(string uuid, string planetName, string surveyId,
            string resourceName, string purity, string amount)
        {
            var survey = new Survey("Test Survey")
            {
                UUID = uuid,
                PlanetName = planetName,
                SurveyID = surveyId,
                Resources = new Dictionary<string, SurveyResource>
                {
                    [resourceName] = new SurveyResource(resourceName, purity, amount)
                }
            };

            return survey;
        }

        /// <summary>
        /// Validates: Requirement 3.1 -- preserve existing valid real survey on reimport.
        /// </summary>
        [Test]
        public void AssignSurvey_PreservesValidRealSurvey()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var realSurvey = CreateSurvey("real-1", "Alpha Prime", "SURV-001", "Iron", "Medium", "120");
            _playerContext.AddSurvey(realSurvey);

            var structure = CreateMiningRig("rig-1", "Iron", "Medium", "real-1");

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 120m);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(structure.MiningSurvey, Is.EqualTo("real-1"));
        }

        /// <summary>
        /// Validates: Requirement 3.3 -- upgrade DEFAULT survey to real when real becomes available.
        /// </summary>
        [Test]
        public void AssignSurvey_UpgradesDefaultToReal_WhenRealSurveyAvailable()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            string defaultUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Alpha Prime", "Sol");
            var defaultSurvey = CreateSurvey(defaultUUID, "Alpha Prime", "DEFAULT", "Iron", "Medium", "100");
            var realSurvey = CreateSurvey("real-1", "Alpha Prime", "SURV-001", "Iron", "Medium", "130");
            _playerContext.AddSurvey(defaultSurvey);
            _playerContext.AddSurvey(realSurvey);

            var structure = CreateMiningRig("rig-1", "Iron", "Medium", defaultUUID);

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 130m);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(structure.MiningSurvey, Is.EqualTo("real-1"));
        }

        /// <summary>
        /// Validates: Requirement 1.4 -- assign best real survey when no existing survey.
        /// </summary>
        [Test]
        public void AssignSurvey_AssignsBestSurvey_WhenNoExistingSurvey()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var survey1 = CreateSurvey("s1", "Alpha Prime", "SURV-001", "Iron", "Medium", "100");
            var survey2 = CreateSurvey("s2", "Alpha Prime", "SURV-002", "Iron", "Medium", "150");
            _playerContext.AddSurvey(survey1);
            _playerContext.AddSurvey(survey2);

            var structure = CreateMiningRig("rig-1", "Iron", "Medium");

            // Act: maxRate 140 is closer to 150
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 140m);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(structure.MiningSurvey, Is.EqualTo("s2"));
        }

        /// <summary>
        /// Validates: Requirement 1.5 -- fall back to default survey when no real survey exists.
        /// </summary>
        [Test]
        public void AssignSurvey_FallsBackToDefault_WhenNoRealSurveyExists()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var structure = CreateMiningRig("rig-1", "Iron", "Medium");

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 100m);

            // Assert
            Assert.That(result, Is.True);
            string expectedUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Alpha Prime", "Sol");
            Assert.That(structure.MiningSurvey, Is.EqualTo(expectedUUID));
            // Verify default survey was created in SurveyList
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
            Assert.That(_playerContext.SurveyList[0].SurveyID, Is.EqualTo("DEFAULT"));
        }

        /// <summary>
        /// Validates: Requirement 3.2 -- when existing survey is deleted, assign new best survey.
        /// </summary>
        [Test]
        public void AssignSurvey_HandlesDeletedSurvey_AssignsNewBest()
        {
            // Arrange: structure references a survey UUID that no longer exists
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var realSurvey = CreateSurvey("real-2", "Alpha Prime", "SURV-002", "Iron", "Medium", "200");
            _playerContext.AddSurvey(realSurvey);

            var structure = CreateMiningRig("rig-1", "Iron", "Medium", "deleted-survey-uuid");

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 200m);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(structure.MiningSurvey, Is.EqualTo("real-2"));
        }

        /// <summary>
        /// Validates: Requirement 3.3 -- keep default when no real survey available.
        /// </summary>
        [Test]
        public void AssignSurvey_KeepsDefault_WhenNoRealSurveyAvailable()
        {
            // Arrange: structure has a DEFAULT survey and no real surveys exist
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            string defaultUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Alpha Prime", "Sol");
            var defaultSurvey = CreateSurvey(defaultUUID, "Alpha Prime", "DEFAULT", "Iron", "Medium", "100");
            _playerContext.AddSurvey(defaultSurvey);

            var structure = CreateMiningRig("rig-1", "Iron", "Medium", defaultUUID);

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 100m);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(structure.MiningSurvey, Is.EqualTo(defaultUUID));
        }

        [Test]
        public void AssignSurvey_ReturnsFalse_WhenNoMiningSurveyResource()
        {
            // Arrange: structure with no resource assigned
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var structure = new ColonyStructure { UUID = "rig-1" };

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 100m);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(structure.MiningSurvey, Is.Null);
        }

        [Test]
        public void AssignSurvey_HandlesDeletedSurvey_FallsBackToDefault_WhenNoRealExists()
        {
            // Arrange: structure references a deleted survey and no real surveys exist
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            var structure = CreateMiningRig("rig-1", "Iron", "Medium", "deleted-uuid");

            // Act
            bool result = MinerSetupHelper.AssignSurvey(structure, colony, _playerContext, 50m);

            // Assert
            Assert.That(result, Is.True);
            string expectedUUID = DeterministicUUID.GenerateDefaultSurvey("owner-1", "Alpha Prime", "Sol");
            Assert.That(structure.MiningSurvey, Is.EqualTo(expectedUUID));
        }
    }
}
