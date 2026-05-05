using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Mutation guard tests for StockPlan and StockProfile.
    /// Ensures direct property sets only appear in allowed files.
    /// Feature: bl-119-stocktargets-readonly
    /// </summary>
    [TestFixture]
    public class StockTargetMutationGuardTests
    {
        private static readonly string SourceRoot =
            Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StockTargetMutationService.cs",
            "StockPlan.cs",
            "StockProfile.cs",
            "PlayerContext.cs",
            "SerializationSorter.cs",
            "BuildPlanMutationService.cs",
            "BuildPlanExecutionService.cs",
            "DeliveryPlanService.cs",
            "PricingPlanService.cs",
            "PlayerProfileService.cs",
            "PlayerProfileParser.cs",
            "FormStockTargets.cs",
        };

        /// <summary>
        /// Property 8: No Direct Mutation Outside Service
        /// **Validates: Requirements 12.1**
        /// </summary>
        [Test]
        public void StockPlan_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"plan\.Name\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"plan\.IsActive\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"plan\.ReplenishmentBuildPlanUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"plan\.OwnerUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"plan\.Targets\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedMutators);
            Assert.That(violations, Is.Empty,
                "Found direct StockPlan property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void StockProfile_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"profile\.Name\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"profile\.IsActive\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"profile\.OwnerUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"profile\.Entries\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedMutators);
            Assert.That(violations, Is.Empty,
                "Found direct StockProfile property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        private List<string> ScanSourceFilesForPatterns(Regex[] compiled, HashSet<string> allowedFiles)
        {
            var violations = new List<string>();
            var csFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("Designer.cs") && !f.Contains("\\obj\\"));

            foreach (var fullPath in csFiles)
            {
                var fileName = Path.GetFileName(fullPath);
                if (allowedFiles.Contains(fileName)) continue;

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var pattern in compiled)
                    {
                        if (pattern.IsMatch(lines[i]))
                        {
                            var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                            violations.Add(string.Format("{0}({1}): {2}", relativePath, i + 1, lines[i].Trim()));
                        }
                    }
                }
            }

            return violations;
        }
    }
}
