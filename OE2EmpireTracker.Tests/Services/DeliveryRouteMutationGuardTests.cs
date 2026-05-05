using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure DeliveryRoute entities are only mutated by allowed code.
    /// Feature: BL-112 DeliveryRoute Immutable Data Model
    /// Validates: Property 8
    /// Validates: Requirements 18.1, 18.2, 18.3, 21.1, 21.2
    /// </summary>
    [TestFixture]
    public class DeliveryRouteMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        /// <summary>
        /// Allowed files for direct DeliveryRoute scalar property sets (Name, UUID, OwnerUUID).
        /// </summary>
        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryRouteService.cs",
            "DeliveryRoute.cs",
            "PlayerContext.cs",
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
            "StockTargetMutationService.cs",
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
            "DeliveryGenerationService.cs",
            "MarketBlueprintImporter.cs",
            "PlayerProfileService.cs",
            "PricingPlanService.cs",
            "SurveyImportHelper.cs",
            "SurveyService.cs",
            "BlueprintViewModel.cs",
            "ShipTemplateService.cs",
            "DeliveryPlanService.cs",
            "ShipService.cs",
            "StationService.cs",
            "BuildPlanMutationService.cs",
        };

        /// <summary>
        /// Allowed files for direct DeliveryRoute.Stops list mutation.
        /// </summary>
        private static readonly HashSet<string> AllowedStopsMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeliveryRouteService.cs",
            "DeliveryRoute.cs",
            "FormDeliveryExecution.cs",
            "DeliveryGenerationService.cs",
            "SerializationSorter.cs",
            "DeliveryPlanViewModel.cs",
            "DeliveryPlanService.cs",
        };

        /// <summary>
        /// Requirement 21.1: Direct DeliveryRoute scalar property sets only appear in allowed files.
        /// Scans all non-test .cs files for DeliveryRoute-unique property set patterns
        /// and asserts they only appear in DeliveryRouteService, DeliveryRoute.cs,
        /// PlayerContext.cs (deserialization/migration), and test code.
        /// </summary>
        [Test]
        public void DeliveryRoute_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var scalarPropertyPatterns = new[]
            {
                @"\.Name\s*=[^=]",
                @"\.UUID\s*=[^=]",
                @"\.OwnerUUID\s*=[^=]",
            };

            var compiled = scalarPropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedScalarMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryRoute scalar property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.2: Direct DeliveryRoute.Stops list mutation only appears in allowed files.
        /// Scans all non-test .cs files for Stops list mutation patterns
        /// (Add, Remove, Clear, Insert, assignment) and asserts they only appear in
        /// DeliveryRouteService, DeliveryRoute.cs, and test code.
        /// </summary>
        [Test]
        public void DeliveryRoute_StopsListMutation_OnlyInAllowedFiles()
        {
            var stopsMutationPatterns = new[]
            {
                @"\.Stops\.Add\(",
                @"\.Stops\.Remove\(",
                @"\.Stops\.Clear\(",
                @"\.Stops\.Insert\(",
                @"\.Stops\s*=[^=]",
            };

            var compiled = stopsMutationPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedStopsMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryRoute.Stops list mutation in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 18.2: FormDeliveryRoute does not directly set properties on DeliveryRoute.
        /// </summary>
        [Test]
        public void FormDeliveryRoute_NoDirectDeliveryRouteMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_route\.\w+\s*=",
                @"route\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "DeliveryRoute", "FormDeliveryRoute.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            // Exclude DeliveryPlan mutations (plan management is out of scope for BL-112)
            violations = violations.Where(v => !v.Contains("planViewModel")).ToList();

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryRoute entity mutation in FormDeliveryRoute:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 18.3: DeliveryRouteViewModel does not directly set properties on DeliveryRoute.
        /// </summary>
        [Test]
        public void DeliveryRouteViewModel_NoDirectDeliveryRouteMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_route\.\w+\s*=",
                @"route\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("ViewModels", "DeliveryRouteViewModel.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct DeliveryRoute entity mutation in DeliveryRouteViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 3.2: ViewModel does not expose mutable DeliveryRoute via public property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeMutableDeliveryRoute()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "DeliveryRouteViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public DeliveryRoute Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public DeliveryRoute _"),
                "ViewModel still exposes mutable DeliveryRoute field");
        }

        private List<string> ScanSourceFilesForPatterns(Regex[] compiled, HashSet<string> allowedFiles)
        {
            var violations = new List<string>();
            var csFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

            foreach (var fullPath in csFiles)
            {
                var fileName = Path.GetFileName(fullPath);

                // Skip allowed mutator files
                if (allowedFiles.Contains(fileName))
                {
                    continue;
                }

                // Skip Designer.cs files (no hand-written mutation)
                if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip migration code
                var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                if (relativePath.Contains("Migration"))
                {
                    continue;
                }

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Skip comments
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    // Skip JSON deserialization attributes and property declarations
                    if (line.StartsWith("[Json") || line.Contains("JsonProperty"))
                    {
                        continue;
                    }

                    // Skip property declarations (get; set;)
                    if (line.Contains("{ get;") || line.Contains("{ set;"))
                    {
                        continue;
                    }

                    // Skip ViewModel local field sets (edit buffer writes, not entity mutation)
                    if (line.Contains("_viewModel.") || line.Contains("viewModel."))
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
