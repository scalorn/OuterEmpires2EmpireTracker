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
        public void GetAllTopics_Returns9Topics_WithCorrectFilenames()
        {
            var topics = HelpTopicRegistry.GetAllTopics();

            Assert.That(topics.Count, Is.EqualTo(9));

            var expectedFilenames = new[]
            {
                "README.md",
                "getting-started.md",
                "colonies.md",
                "blueprints.md",
                "surveys.md",
                "delivery-routes.md",
                "player-profiles.md",
                "background-processing.md",
                "window-state.md"
            };

            var actualFilenames = topics.Select(t => t.FileName).ToArray();
            Assert.That(actualFilenames, Is.EqualTo(expectedFilenames));
        }

        /// <summary>
        /// Validates: Requirements 6.3
        /// Each of the 9 form-type mappings returns the correct documentation file.
        /// </summary>
        [TestCase("FormColony", "colonies.md")]
        [TestCase("FormBlueprint", "blueprints.md")]
        [TestCase("FormSurvey", "surveys.md")]
        [TestCase("FormDeliveryRoute", "delivery-routes.md")]
        [TestCase("FormDeliveryExecution", "delivery-routes.md")]
        [TestCase("FormAutoFill", "delivery-routes.md")]
        [TestCase("FormPlayerProfile", "player-profiles.md")]
        [TestCase("FormColonyActivity", "colonies.md")]
        [TestCase("FormColonyDailyBuild", "colonies.md")]
        public void GetTopicForForm_MappedType_ReturnsCorrectFile(string formType, string expectedFile)
        {
            var result = HelpTopicRegistry.GetTopicForForm(formType);
            Assert.That(result, Is.EqualTo(expectedFile));
        }
    }
}
