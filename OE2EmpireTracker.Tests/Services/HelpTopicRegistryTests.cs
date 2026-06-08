using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class HelpTopicRegistryTests
    {
        /// <summary>
        /// Validates: Requirements 1.3
        /// GetAllTopics returns exactly 9 topics with the expected filenames in order.
        /// </summary>
        [Test]
        public void GetAllTopics_Returns27Topics_WithCorrectFilenames()
        {
            var topics = HelpTopicRegistry.GetAllTopics();

            Assert.That(topics.Count, Is.EqualTo(27));

            var expectedFilenames = new[]
            {
                "README.md",
                "getting-started.md",
                "colonies.md",
                "colony-activity.md",
                "colony-daily-build.md",
                "blueprints.md",
                "surveys.md",
                "asteroids.md",
                "delivery-routes.md",
                "delivery-execution.md",
                "build-planner.md",
                "pricing-plans.md",
                "supply-chains.md",
                "ships.md",
                "stations.md",
                "market.md",
                "stock-targets.md",
                "contacts.md",
                "player-profiles.md",
                "systems.md",
                "sharing.md",
                "banking.md",
                "mail.md",
                "game-api-status.md",
                "background-processing.md",
                "window-state.md",
                "preferences.md"
            };

            var actualFilenames = topics.Select(t => t.FileName).ToArray();
            Assert.That(actualFilenames, Is.EqualTo(expectedFilenames));
        }

        /// <summary>
        /// Validates: Requirements 6.3
        /// Each of the 9 form-type mappings returns the correct documentation file.
        /// </summary>
        [TestCase("FormColonyV2", "colonies.md")]
        [TestCase("FormBlueprintV2", "blueprints.md")]
        [TestCase("FormSurvey", "surveys.md")]
        [TestCase("FormDeliveryRoute", "delivery-routes.md")]
        [TestCase("FormDeliveryExecution", "delivery-execution.md")]
        [TestCase("FormAutoFill", "delivery-routes.md")]
        [TestCase("FormPlayerProfile", "player-profiles.md")]
        [TestCase("FormColonyActivity", "colony-activity.md")]
        [TestCase("FormColonyDailyBuild", "colony-daily-build.md")]
        [TestCase("FormAsteroid", "asteroids.md")]
        [TestCase("FormPreferences", "preferences.md")]
        [TestCase("FormBuildPlanner", "build-planner.md")]
        [TestCase("FormPricingPlan", "pricing-plans.md")]
        [TestCase("FormSupplyChain", "supply-chains.md")]
        [TestCase("FormShipTemplate", "ships.md")]
        [TestCase("FormShipInstance", "ships.md")]
        [TestCase("FormStation", "stations.md")]
        [TestCase("FormMarket", "market.md")]
        [TestCase("FormStockTargets", "stock-targets.md")]
        [TestCase("FormContacts", "contacts.md")]
        [TestCase("FormSystem", "systems.md")]
        [TestCase("FormSharing", "sharing.md")]
        [TestCase("FormBanking", "banking.md")]
        [TestCase("FormBankingEntry", "banking.md")]
        [TestCase("FormMail", "mail.md")]
        [TestCase("FormGameApiStatus", "game-api-status.md")]
        public void GetTopicForForm_MappedType_ReturnsCorrectFile(string formType, string expectedFile)
        {
            var result = HelpTopicRegistry.GetTopicForForm(formType);
            Assert.That(result, Is.EqualTo(expectedFile));
        }
    }
}
