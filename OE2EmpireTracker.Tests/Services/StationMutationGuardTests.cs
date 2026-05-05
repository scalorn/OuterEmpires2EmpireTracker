using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure Station entities are only mutated by allowed code.
    /// Feature: BL-117 Station Immutable Data Model
    /// Validates: Property 11
    /// Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2, 20.3
    /// </summary>
    [TestFixture]
    public class StationMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StationService.cs",
            "Station.cs",
            "PlayerContext.cs",
        };

        private static readonly HashSet<string> AllowedComponentsMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StationService.cs",
            "Station.cs",
            "SerializationSorter.cs",
        };

        private static readonly HashSet<string> AllowedHoldMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StationService.cs",
            "Station.cs",
            "DeliveryPlanService.cs",
            "MarketService.cs",
        };

        [Test]
        public void Station_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"station\.StationType\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.Ownership\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.StationBlueprintUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.HullCurrentHP\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.HullMaxHP\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.HullMaxRepairPercent\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedScalarMutators);
            Assert.That(violations, Is.Empty,
                "Found direct Station property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Station_ComponentsListMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"station\.Components\.Add\(", RegexOptions.Compiled),
                new Regex(@"station\.Components\.Remove\(", RegexOptions.Compiled),
                new Regex(@"station\.Components\.Clear\(", RegexOptions.Compiled),
                new Regex(@"station\.Components\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedComponentsMutators);
            Assert.That(violations, Is.Empty,
                "Found direct Station.Components list mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Station_HoldsAndMunitionsHoldMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"station\.Holds\[", RegexOptions.Compiled),
                new Regex(@"station\.Holds\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"station\.MunitionsHold\.AddItem\(", RegexOptions.Compiled),
                new Regex(@"station\.MunitionsHold\.Remove\(", RegexOptions.Compiled),
                new Regex(@"station\.MunitionsHold\.Clear\(", RegexOptions.Compiled),
                new Regex(@"station\.MunitionsHold\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedHoldMutators);
            Assert.That(violations, Is.Empty,
                "Found direct Station.Holds/MunitionsHold mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FormStation_NoDirectStationMutation()
        {
            var filePath = Path.Combine(SourceRoot, "Forms", "Station", "FormStation.cs");
            var patterns = new[]
            {
                new Regex(@"_selectedStation\.\w+\s*=", RegexOptions.Compiled),
            };
            var violations = ScanFileForPatterns(filePath, patterns);
            Assert.That(violations, Is.Empty,
                "Found direct Station entity mutation in FormStation:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ViewModel_DoesNotDirectlyMutateStationEntity()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "StationViewModel.cs");
            var content = File.ReadAllText(filePath);
            Assert.That(content, Does.Not.Contain("public Station Data"), "ViewModel still exposes mutable Data property");
        }

        private List<string> ScanForViolations(Regex[] patterns, HashSet<string> allowedFiles)
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
                    if (line.Contains("_viewModel.") || line.Contains("viewModel.")) continue;
                    foreach (var rx in patterns)
                    {
                        if (rx.IsMatch(line))
                        {
                            violations.Add(string.Format("{0}({1}): {2}", relativePath, i + 1, line));
                        }
                    }
                }
            }

            return violations;
        }

        private List<string> ScanFileForPatterns(string filePath, Regex[] patterns)
        {
            var violations = new List<string>();
            if (!File.Exists(filePath)) return violations;
            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*")) continue;
                foreach (var rx in patterns)
                {
                    if (rx.IsMatch(line))
                    {
                        violations.Add(string.Format("({0}): {1}", i + 1, line));
                    }
                }
            }

            return violations;
        }
    }
}
