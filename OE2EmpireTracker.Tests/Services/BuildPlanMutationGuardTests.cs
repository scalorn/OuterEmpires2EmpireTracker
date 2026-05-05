using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure BuildPlan entities are only mutated by allowed code.
    /// Feature: BL-118 BuildPlan Immutable Data Model
    /// Validates: Property 8
    /// Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2
    /// </summary>
    [TestFixture]
    public class BuildPlanMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BuildPlanMutationService.cs",
            "BuildPlan.cs",
            "PlayerContext.cs",
            "BuildPlanExecutionService.cs",
            "FormBuildPlanner.cs",
            "AutoAssignService.cs",
            "DeliveryGenerationService.cs",
            "SerializationSorter.cs",
            "FormColonyV2.cs",
            "FormStockTargets.cs",
            "FormSupplyChain.cs",
            "StockTargetMutationService.cs",
            "SupplyChainMutationService.cs",
        };

        private static readonly HashSet<string> AllowedItemMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BuildPlanMutationService.cs",
            "BuildPlan.cs",
            "BuildItem.cs",
            "PlayerContext.cs",
            "BuildPlanExecutionService.cs",
            "FormBuildPlanner.cs",
            "AutoAssignService.cs",
            "DeliveryGenerationService.cs",
            "SerializationSorter.cs",
            "BuildPlanService.cs",
            "ShipBuildService.cs",
        };

        [Test]
        public void BuildPlan_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.IsActive\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedScalarMutators);
            Assert.That(violations, Is.Empty,
                "Found direct BuildPlan scalar property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void BuildPlan_ItemsListMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"plan\.Items\.Add\(", RegexOptions.Compiled),
                new Regex(@"plan\.Items\.Remove", RegexOptions.Compiled),
                new Regex(@"plan\.Items\.Clear\(", RegexOptions.Compiled),
                new Regex(@"plan\.Items\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"_selectedPlan\.Items", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedItemMutators);
            Assert.That(violations, Is.Empty,
                "Found direct BuildPlan.Items list mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void BuildItem_PropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"buildItem\.Status\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"buildItem\.BuildLocationUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"buildItem\.StructureUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"buildItem\.SequenceInStructure\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"buildItem\.DependsOnUUID\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedItemMutators);
            Assert.That(violations, Is.Empty,
                "Found direct BuildItem property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        private List<string> ScanSourceFilesForPatterns(Regex[] compiled, HashSet<string> allowedFiles)
        {
            var violations = new List<string>();
            var csFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);
            foreach (var fullPath in csFiles)
            {
                var fileName = Path.GetFileName(fullPath);
                if (allowedFiles.Contains(fileName)) continue;
                if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) continue;
                var relativePath = fullPath.Substring(SourceRoot.Length + 1);
                if (relativePath.Contains("Migration")) continue;
                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*")) continue;
                    if (line.Contains("{ get;") || line.Contains("{ set;")) continue;
                    if (line.Contains("_viewModel.")) continue;
                    foreach (var rx in compiled)
                    {
                        if (rx.IsMatch(line))
                            violations.Add(string.Format("{0}({1}): {2}", relativePath, i + 1, line));
                    }
                }
            }

            return violations;
        }
    }
}
