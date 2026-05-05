using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Comprehensive mutation audit tests that verify the immutable data model
    /// is fully enforced across all entity types.
    /// Feature: BL-125 Final Mutation Audit
    /// Validates: Requirements 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 4.3
    /// </summary>
    [TestFixture]
    public class ComprehensiveMutationAuditTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly string TestRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker.Tests"));

        /// <summary>
        /// All entity types that must have mutation guard test coverage.
        /// </summary>
        private static readonly string[] RequiredMutationGuardEntities = new[]
        {
            "Blueprint",
            "Colony",
            "Survey",
            "PlayerProfile",
            "DeliveryRoute",
            "DeliveryPlan",
            "PricingPlan",
            "ShipTemplate",
            "Ship",
            "Station",
            "BuildPlan",
            "StockTarget",
            "SupplyChain",
            "Contacts",
            "Asteroid",
            "MarketListing",
        };

        /// <summary>
        /// Files allowed to call WriteContext().
        /// Services, PlayerContext itself, EmpireContext, import handlers, and test code.
        /// Forms and ViewModels that still call WriteContext() are tracked as accepted
        /// baseline until their respective BL items migrate them to service-only persistence.
        /// </summary>
        private static readonly HashSet<string> AllowedWriteContextCallers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core context and services
            "PlayerContext.cs",
            "EmpireContext.cs",
            "BlueprintService.cs",
            "ColonyService.cs",
            "SurveyService.cs",
            "PlayerProfileService.cs",
            "DeliveryRouteService.cs",
            "DeliveryPlanService.cs",
            "PricingPlanService.cs",
            "ShipTemplateService.cs",
            "ShipBuildService.cs",
            "ShipService.cs",
            "StationService.cs",
            "BuildPlanMutationService.cs",
            "StockTargetMutationService.cs",
            "SupplyChainMutationService.cs",
            "ContactsService.cs",
            "AsteroidService.cs",
            "MarketListingService.cs",
            "MarketService.cs",
            "PreferencesStore.cs",
            "BackgroundProcessor.cs",
            "DeliveryFulfillment.cs",
            "BlueprintImportHandler.cs",
            "CrateImporter.cs",
            "MarketBlueprintImporter.cs",

            // Accepted baseline: Forms that persist via WriteContext() pending migration
            "MainWindow.cs",
            "FormBuildPlanner.cs",
            "FormColonyDailyBuild.cs",
            "FormColonyV2.cs",
            "FormShipTemplate.cs",
            "FormStockTargets.cs",

            // Accepted baseline: ViewModels that persist via WriteContext() pending migration
            "BlueprintViewModel.cs",
        };

        /// <summary>
        /// Requirement 1.1, 1.2: Every migrated entity type SHALL have a mutation guard test file.
        /// Verifies that a test file named {EntityType}MutationGuardTests.cs exists
        /// in the test project Services folder for each required entity type.
        /// </summary>
        [Test]
        public void AllEntityTypes_HaveMutationGuardCoverage()
        {
            var missingTests = new List<string>();

            foreach (var entityType in RequiredMutationGuardEntities)
            {
                var expectedFile = Path.Combine(TestRoot, "Services", entityType + "MutationGuardTests.cs");
                if (!File.Exists(expectedFile))
                {
                    missingTests.Add(entityType + "MutationGuardTests.cs");
                }
            }

            Assert.That(
                missingTests,
                Is.Empty,
                "Missing mutation guard test files for entity types:\n" +
                string.Join("\n", missingTests));
        }

        /// <summary>
        /// Requirement 2.1, 2.2, 2.3: WriteContext() SHALL only appear in service classes,
        /// PlayerContext, and test code. SHALL NOT appear in Form*.cs or *ViewModel.cs files
        /// outside the accepted baseline.
        /// </summary>
        [Test]
        public void WriteContext_OnlyCalledFromServices()
        {
            var writeContextPattern = new Regex(@"WriteContext\s*\(", RegexOptions.Compiled);
            var violations = new List<string>();

            var csFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

            foreach (var fullPath in csFiles)
            {
                var fileName = Path.GetFileName(fullPath);

                // Skip allowed files
                if (AllowedWriteContextCallers.Contains(fileName))
                {
                    continue;
                }

                // Skip Designer.cs files
                if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = fullPath.Substring(SourceRoot.Length + 1);

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Skip comments
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    // Skip string literals containing WriteContext (e.g. in test assertions)
                    if (line.Contains("\"") && line.Contains("WriteContext"))
                    {
                        continue;
                    }

                    if (writeContextPattern.IsMatch(line))
                    {
                        violations.Add(string.Format(
                            "{0}({1}): {2}",
                            relativePath,
                            i + 1,
                            line));
                    }
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                "Found WriteContext() calls in non-service files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 3.1, 4.1, 4.2: No Form*.cs file (excluding Designer.cs) SHALL
        /// directly set entity properties. Forms should only set ViewModel local fields.
        /// </summary>
        [Test]
        public void NoFormDirectlyMutatesEntities()
        {
            var entityMutationPatterns = new[]
            {
                new Regex(@"\._(?:blueprint|colony|survey|profile|route|plan|template|ship|station|buildPlan|stockTarget|supplyChain|contacts|asteroid|listing)\.\w+\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.Data\.\w+\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = new List<string>();
            var formFiles = Directory.GetFiles(SourceRoot, "Form*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                .Where(f => !f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var fullPath in formFiles)
            {
                var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                var lines = File.ReadAllLines(fullPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Skip comments
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    // Skip ViewModel field sets (these are local edit buffer writes)
                    if (line.Contains("_viewModel.") || line.Contains("viewModel."))
                    {
                        continue;
                    }

                    // Skip property declarations
                    if (line.Contains("{ get;") || line.Contains("{ set;"))
                    {
                        continue;
                    }

                    // Skip local variable assignments (e.g. var data = ...)
                    if (line.StartsWith("var ") || line.StartsWith("string ") ||
                        line.StartsWith("int ") || line.StartsWith("bool ") ||
                        line.StartsWith("double ") || line.StartsWith("decimal ") ||
                        line.StartsWith("float ") || line.StartsWith("Guid "))
                    {
                        continue;
                    }

                    foreach (var rx in entityMutationPatterns)
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

            Assert.That(
                violations,
                Is.Empty,
                "Found direct entity mutation in Form files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 3.2, 4.1, 4.3: No *ViewModel.cs file SHALL directly set entity
        /// properties. ViewModels should only set local fields.
        /// </summary>
        [Test]
        public void NoViewModelDirectlyMutatesEntities()
        {
            var entityMutationPatterns = new[]
            {
                new Regex(@"\._(?:blueprint|colony|survey|profile|route|plan|template|ship|station|buildPlan|stockTarget|supplyChain|contacts|asteroid|listing)\.\w+\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.Data\.\w+\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = new List<string>();
            var viewModelFiles = Directory.GetFiles(
                Path.Combine(SourceRoot, "ViewModels"), "*ViewModel.cs", SearchOption.AllDirectories)
                .ToArray();

            foreach (var fullPath in viewModelFiles)
            {
                var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                var lines = File.ReadAllLines(fullPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Skip comments
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*"))
                    {
                        continue;
                    }

                    // Skip property declarations
                    if (line.Contains("{ get;") || line.Contains("{ set;"))
                    {
                        continue;
                    }

                    // Skip local field assignments (ViewModel's own fields)
                    if (Regex.IsMatch(line, @"^_\w+\s*=") || Regex.IsMatch(line, @"^this\._\w+\s*="))
                    {
                        continue;
                    }

                    foreach (var rx in entityMutationPatterns)
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

            Assert.That(
                violations,
                Is.Empty,
                "Found direct entity mutation in ViewModel files:\n" +
                string.Join("\n", violations));
        }
    }
}
