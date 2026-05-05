using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure DeliveryPlan entities are only mutated by allowed code.
    /// Feature: BL-113 DeliveryPlan Immutable Data Model
    /// Validates: Property 8
    /// Validates: Requirements 21.1, 21.2, 21.3, 21.4, 24.1, 24.2, 24.3
    /// </summary>
    [TestFixture]
    public class DeliveryPlanMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        /// <summary>
        /// Allowed files for direct DeliveryPlan scalar property sets (Name, ShipUUID, Completed).
        /// </summary>
        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryPlanService.cs",
            "DeliveryPlan.cs",
            "PlayerContext.cs",
            "DeliveryPlanViewModel.cs",
            "DeliveryRouteService.cs",
            "DeliveryGenerationService.cs",
            "SerializationSorter.cs",
            "FormAsteroid.cs",
            "FormBlueprintV2.cs",
            "FormBuildPlanner.cs",
            "FormColonyV2.cs",
            "FormContacts.cs",
            "FormDeliveryExecution.cs",
            "FormDeliveryRoute.cs",
            "FormShipInstance.cs",
            "FormShipTemplate.cs",
            "FormStation.cs",
            "FormStockTargets.cs",
            "FormSupplyChain.cs",
            "FormSurvey.cs",
            "Colony.cs",
            "Item.cs",
            "BlueprintScanner.cs",
            "ColonyParser.cs",
            "MinerSetupHelper.cs",
            "PlayerProfileParser.cs",
            "BlueprintImportHandler.cs",
            "BlueprintService.cs",
            "ColonyImportHelper.cs",
            "ColonyService.cs",
            "ColonyStatusCalculator.cs",
            "CrateImporter.cs",
            "DeliveryFulfillment.cs",
            "MarketBlueprintImporter.cs",
            "PlayerProfileService.cs",
            "PricingPlanService.cs",
            "SurveyImportHelper.cs",
            "SurveyService.cs",
            "BlueprintViewModel.cs",
            "ShipTemplateService.cs",
            "ShipService.cs",
            "StationService.cs",
        };

        /// <summary>
        /// Allowed files for direct DeliveryPlanStop mutation (StopCompleted =).
        /// </summary>
        private static readonly HashSet<string> AllowedStopMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryPlanService.cs",
            "DeliveryPlan.cs",
            "DeliveryPlanViewModel.cs",
            "SerializationSorter.cs",
        };

        /// <summary>
        /// Allowed files for direct DeliveryItem.Delivered sets.
        /// </summary>
        private static readonly HashSet<string> AllowedDeliveredMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryPlanService.cs",
            "DeliveryPlan.cs",
            "DeliveryPlanViewModel.cs",
            "ColonyService.cs",
            "DeliveryFulfillment.cs",
        };

        /// <summary>
        /// Allowed files for direct DropOff.Add, PickUp.Add.
        /// </summary>
        private static readonly HashSet<string> AllowedListAddMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryPlanService.cs",
            "DeliveryPlan.cs",
            "DeliveryPlanViewModel.cs",
            "DeliveryGenerationService.cs",
            "SerializationSorter.cs",
        };

        /// <summary>
        /// Requirement 21.1: Direct DeliveryPlan scalar property sets only appear in allowed files.
        /// </summary>
        [Test]
        public void DeliveryPlan_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var scalarPropertyPatterns = new[]
            {
                @"\.Name\s*=[^=]",
                @"\.ShipUUID\s*=[^=]",
                @"\.Completed\s*=[^=]",
                @"\.RouteUUID\s*=[^=]",
                @"\.OwnerUUID\s*=[^=]",
            };

            var compiled = scalarPropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedScalarMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryPlan scalar property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.2: Direct DeliveryPlanStop mutation only appears in allowed files.
        /// </summary>
        [Test]
        public void DeliveryPlanStop_StopCompletedSets_OnlyInAllowedFiles()
        {
            var stopMutationPatterns = new[]
            {
                @"\.StopCompleted\s*=[^=]",
            };

            var compiled = stopMutationPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedStopMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryPlanStop.StopCompleted sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.3: Direct DeliveryItem.Delivered sets only appear in allowed files.
        /// </summary>
        [Test]
        public void DeliveryItem_DeliveredSets_OnlyInAllowedFiles()
        {
            var deliveredPatterns = new[]
            {
                @"\.Delivered\s*=[^=]",
            };

            var compiled = deliveredPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedDeliveredMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryItem.Delivered sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.4: Direct DropOff.Add, PickUp.Add only appear in allowed files.
        /// </summary>
        [Test]
        public void DeliveryPlan_ListAddMutation_OnlyInAllowedFiles()
        {
            var listAddPatterns = new[]
            {
                @"\.DropOff\.Add\(",
                @"\.PickUp\.Add\(",
            };

            var compiled = listAddPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedListAddMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DropOff.Add/PickUp.Add in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 24.1: FormDeliveryRoute does not directly set properties on DeliveryPlan.
        /// </summary>
        [Test]
        public void FormDeliveryRoute_NoDirectDeliveryPlanMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"plan\.Name\s*=[^=]",
                @"plan\.ShipUUID\s*=[^=]",
                @"plan\.Completed\s*=[^=]",
                @"plan\.Stops",
                @"\.Data\.Name\s*=[^=]",
                @"\.Data\.Completed\s*=[^=]",
                @"\.Data\.ShipUUID\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "DeliveryRoute", "FormDeliveryRoute.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryPlan entity mutation in FormDeliveryRoute:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 24.2: FormDeliveryExecution does not directly set properties on DeliveryPlan.
        /// </summary>
        [Test]
        public void FormDeliveryExecution_NoDirectDeliveryPlanMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"\.Delivered\s*=[^=]",
                @"\.StopCompleted\s*=[^=]",
                @"\.Completed\s*=[^=]",
                @"\.ShipUUID\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "DeliveryExecution", "FormDeliveryExecution.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryPlan entity mutation in FormDeliveryExecution:\n" +
                string.Join("\n", violations));
        }

        private List<string> ScanSourceFilesForPatterns(Regex[] compiled, HashSet<string> allowedFiles)
        {
            var violations = new List<string>();
            var csFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

            foreach (var fullPath in csFiles)
            {
                var fileName = Path.GetFileName(fullPath);

                if (allowedFiles.Contains(fileName))
                {
                    continue;
                }

                if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                if (relativePath.Contains("Migration"))
                {
                    continue;
                }

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[Json") || line.Contains("JsonProperty"))
                    {
                        continue;
                    }

                    if (line.Contains("{ get;") || line.Contains("{ set;"))
                    {
                        continue;
                    }

                    if (line.Contains("_viewModel.") || line.Contains("viewModel."))
                    {
                        continue;
                    }

                    if (line.Contains("planViewModel."))
                    {
                        continue;
                    }

                    foreach (var rx in compiled)
                    {
                        if (rx.IsMatch(line))
                        {
                            violations.Add(string.Format(
                                "{0}({1}): {2}",
                                relativePath,
                                i + 1,
                                line));
                        }
                    }
                }
            }

            return violations;
        }

        private List<string> ScanSpecificFilesForPatterns(string[] relativeFiles, string[] regexPatterns)
        {
            var violations = new List<string>();
            var compiled = regexPatterns.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

            foreach (var relFile in relativeFiles)
            {
                var fullPath = Path.Combine(SourceRoot, relFile);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    foreach (var rx in compiled)
                    {
                        if (rx.IsMatch(line))
                        {
                            violations.Add(string.Format("{0}({1}): {2}", relFile, i + 1, line));
                        }
                    }
                }
            }

            return violations;
        }
    }
}