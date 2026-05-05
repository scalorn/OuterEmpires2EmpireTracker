using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure SupplyChain entities are only mutated by allowed code.
    /// Feature: BL-120 SupplyChain Immutable Data Model
    /// Validates: Property 7
    /// Validates: Requirements 9.1
    /// </summary>
    [TestFixture]
    public class SupplyChainMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SupplyChainMutationService.cs",
            "SupplyChain.cs",
            "PlayerContext.cs",
            "FormSupplyChain.cs",
            "SupplyChainService.cs",
            "SerializationSorter.cs",
        };

        private static readonly HashSet<string> AllowedStageMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SupplyChainMutationService.cs",
            "SupplyChain.cs",
            "SupplyChainStage.cs",
            "PlayerContext.cs",
            "FormSupplyChain.cs",
            "SupplyChainService.cs",
            "SerializationSorter.cs",
            "SupplyChainViewModel.cs",
        };

        [Test]
        public void SupplyChain_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"chain\.IsActive\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"chain\.Name\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedScalarMutators);
            Assert.That(violations, Is.Empty,
                "Found direct SupplyChain scalar property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void SupplyChain_StagesListMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.Stages\.Add\(", RegexOptions.Compiled),
                new Regex(@"\.Stages\.Remove", RegexOptions.Compiled),
                new Regex(@"\.Stages\.Clear\(", RegexOptions.Compiled),
                new Regex(@"chain\.Stages\s*=[^=]", RegexOptions.Compiled),
            };

            var violations = ScanSourceFilesForPatterns(patterns, AllowedStageMutators);
            Assert.That(violations, Is.Empty,
                "Found direct SupplyChain.Stages list mutation in non-allowed files:\n" + string.Join("\n", violations));
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