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
    /// Unit tests for MinerSetupHelper.CreateOrUpdateDefaultSurvey.
    /// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperCreateOrUpdateDefaultSurveyTests
    {
        private PlayerContext _playerContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            EmpireContext.GetInstance();
            _playerContext = EmpireContext.PlayerContext;
            _playerContext.SurveyList.Clear();
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

        [Test]
        public void CreateOrUpdateDefaultSurvey_CreatesNewSurvey_WhenNoneExists()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");

            // Act
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Iron", "Medium", 123.45m, _playerContext);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
            Assert.That(result.SurveyID, Is.EqualTo("DEFAULT"));
            Assert.That(result.NickName, Is.EqualTo(""));
            Assert.That(result.PlanetName, Is.EqualTo("Alpha Prime"));
            Assert.That(result.SystemName, Is.EqualTo("Sol"));
            Assert.That(result.OwnerUUID, Is.EqualTo("owner-1"));
            Assert.That(result.Name, Is.EqualTo("Default Survey"));

            // Verify resource entry
            Assert.That(result.Resources.ContainsKey("Iron"), Is.True);
            var resource = result.Resources["Iron"];
            Assert.That(resource.Resource, Is.EqualTo("Iron"));
            Assert.That(resource.Purity, Is.EqualTo("Medium"));
            Assert.That(resource.Amount, Is.EqualTo("123.45"));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_UsesDeterministicUUID()
        {
            // Arrange
            var colony = CreateColony("owner-1", "Alpha Prime", "Sol");
            string expectedUUID = DeterministicUUID.GenerateDefaultSurvey(
                "owner-1", "Alpha Prime", "Sol");

            // Act
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Iron", "Medium", 100m, _playerContext);

            // Assert
            Assert.That(result.UUID, Is.EqualTo(expectedUUID));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_UpdatesExistingSurvey_WhenAlreadyExists()
        {
            // Arrange: create a default survey first
            var colony = CreateColony("owner-2", "Beta Colony", "Proxima");
            MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Iron", "Medium", 100m, _playerContext);
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));

            // Act: add a second resource to the same default survey
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Copper", "High", 200m, _playerContext);

            // Assert: still only one survey, but now with two resources
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
            Assert.That(result.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(result.Resources.ContainsKey("Copper"), Is.True);
            Assert.That(result.Resources["Copper"].Amount, Is.EqualTo("200"));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_UpdatesExistingResourceAmount()
        {
            // Arrange: create default survey with Iron at 100
            var colony = CreateColony("owner-3", "Gamma", "Vega");
            MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Iron", "Medium", 100m, _playerContext);

            // Act: update Iron with a new maxRate
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Iron", "Medium", 250m, _playerContext);

            // Assert: amount updated, still one survey with one resource
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
            Assert.That(result.Resources["Iron"].Amount, Is.EqualTo("250"));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_SetsSurveyID_ToDefault()
        {
            // Arrange
            var colony = CreateColony("owner-4", "Delta", "Rigel");

            // Act
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Gold", "Low", 50m, _playerContext);

            // Assert
            Assert.That(result.SurveyID, Is.EqualTo("DEFAULT"));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_SetsAmountToZero_WhenMaxRateIsZero()
        {
            // Arrange
            var colony = CreateColony("owner-5", "Epsilon", "Altair");

            // Act
            var result = MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Silver", "High", 0m, _playerContext);

            // Assert
            Assert.That(result.Resources["Silver"].Amount, Is.EqualTo("0"));
        }

        [Test]
        public void CreateOrUpdateDefaultSurvey_AddsSurveyToSurveyList()
        {
            // Arrange
            var colony = CreateColony("owner-6", "Zeta", "Deneb");
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(0));

            // Act
            MinerSetupHelper.CreateOrUpdateDefaultSurvey(
                colony, "Titanium", "Medium", 75m, _playerContext);

            // Assert
            Assert.That(_playerContext.SurveyList, Has.Count.EqualTo(1));
            Assert.That(_playerContext.SurveyList[0].SurveyID, Is.EqualTo("DEFAULT"));
        }
    }
}
