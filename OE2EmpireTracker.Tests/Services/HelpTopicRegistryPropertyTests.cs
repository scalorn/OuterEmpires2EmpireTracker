using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    // Feature: user-help-docs, Property 2: Topic registry returns correct mapping or fallback

    [TestFixture]
    public class HelpTopicRegistryPropertyTests
    {
        /// <summary>
        /// The complete set of known form-type -> doc-file mappings.
        /// </summary>
        private static readonly Dictionary<string, string> ExpectedMappings = new Dictionary<string, string>
        {
            { "FormColony", "colonies.md" },
            { "FormBlueprintV2", "blueprints.md" },
            { "FormSurvey", "surveys.md" },
            { "FormDeliveryRoute", "delivery-routes.md" },
            { "FormDeliveryExecution", "delivery-execution.md" },
            { "FormAutoFill", "delivery-routes.md" },
            { "FormPlayerProfile", "player-profiles.md" },
            { "FormColonyActivity", "colony-activity.md" },
            { "FormColonyDailyBuild", "colony-daily-build.md" },
            { "FormAsteroid", "asteroids.md" },
            { "FormPreferences", "preferences.md" },
            { "FormBuildPlanner", "build-planner.md" },
            { "FormPricingPlan", "pricing-plans.md" },
            { "FormSupplyChain", "supply-chains.md" },
            { "FormShipTemplate", "ships.md" },
            { "FormShipInstance", "ships.md" },
            { "FormStation", "stations.md" },
            { "FormMarket", "market.md" },
            { "FormStockTargets", "stock-targets.md" },
            { "FormContacts", "contacts.md" }
        };

        /// <summary>
        /// Property 2: Topic registry returns correct mapping or fallback.
        /// For any string input to GetTopicForForm(), the result should be the mapped
        /// documentation filename if the input matches a registered form type name,
        /// or "README.md" otherwise.
        /// **Validates: Requirements 6.3, 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property GetTopicForForm_ReturnsCorrectMappingOrFallback()
        {
            // Mix random strings with known form type names so we exercise both paths
            var knownKeys = ExpectedMappings.Keys.ToArray();

            var inputGen = Gen.Frequency(
                Tuple.Create(3, Arb.Generate<NonNull<string>>().Select(s => s.Get)),
                Tuple.Create(2, Gen.Elements(knownKeys)));

            return Prop.ForAll(inputGen.ToArbitrary(), input =>
            {
                var result = HelpTopicRegistry.GetTopicForForm(input);

                if (ExpectedMappings.TryGetValue(input, out string expectedFile))
                {
                    return (result == expectedFile)
                        .Label($"Mapped type '{input}' should return '{expectedFile}' but got '{result}'");
                }
                else
                {
                    return (result == "README.md")
                        .Label($"Unmapped type '{input}' should return 'README.md' but got '{result}'");
                }
            });
        }
    }
}
