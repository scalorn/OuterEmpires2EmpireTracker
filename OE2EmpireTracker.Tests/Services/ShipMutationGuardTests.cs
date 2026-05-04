using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure Ship entities are only mutated by allowed code.
    /// Feature: BL-116 Ship Immutable Data Model
    /// Validates: Property 11
    /// Validates: Requirements 18.1, 18.2, 18.3, 21.1, 21.2, 21.3
    /// </summary>
    [TestFixture]
    public class ShipMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ShipService.cs",
            "Ship.cs",
            "PlayerContext.cs",
            "FormShipInstance.cs",
            "ShipTemplateService.cs",
            "FormStation.cs",
            "FormSupplyChain.cs",
            "DeliveryGenerationService.cs",
        };

        private static readonly HashSet<string> AllowedComponentsMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ShipService.cs",
            "Ship.cs",
            "FormShipInstance.cs",
            "SerializationSorter.cs",
            "ShipTemplateService.cs",
        };

        private static readonly HashSet<string> AllowedCargoBagMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ShipService.cs",
            "Ship.cs",
            "FormShipInstance.cs",
        };

        [Test]
        public void Ship_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.TemplateUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.HullBlueprintUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.LocationType\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.LocationUUID\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.HullCurrentHP\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.HullMaxHP\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.HullMaxRepairPercent\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedScalarMutators, "Ship");
            Assert.That(violations, Is.Empty,
                "Found direct Ship property sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Ship_ComponentsListMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.Components\.Add\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Remove\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Clear\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Insert\(", RegexOptions.Compiled),
                new Regex(@"\.Components\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedComponentsMutators, "Ship");
            Assert.That(violations, Is.Empty,
                "Found direct Ship.Components list mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Ship_CargoBagMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.Cargo\.AddItem\(", RegexOptions.Compiled),
                new Regex(@"\.Cargo\.Remove\(", RegexOptions.Compiled),
                new Regex(@"\.Cargo\.Clear\(", RegexOptions.Compiled),
                new Regex(@"\.Cargo\s*=[^=]", RegexOptions.Compiled),
                new Regex(@"\.Hopper\.AddItem\(", RegexOptions.Compiled),
                new Regex(@"\.Hopper\.Remove\(", RegexOptions.Compiled),
                new Regex(@"\.Hopper\.Clear\(", RegexOptions.Compiled),
                new Regex(@"\.Hopper\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedCargoBagMutators, "Ship");
            Assert.That(violations, Is.Empty,
                "Found direct Ship.Cargo/Hopper mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FormShipInstance_NoDirectShipMutation()
        {
            var patterns = new[]
            {
                new Regex(@"_selectedShip\.\w+\s*=", RegexOptions.Compiled),
                new Regex(@"ship\.\w+\s*=[^=]", RegexOptions.Compiled),
            };
            var filePath = Path.Combine(SourceRoot, "Forms", "ShipInstance", "FormShipInstance.cs");
            var violations = ScanFileForPatterns(filePath, patterns);
            Assert.That(violations, Is.Empty,
                "Found direct Ship entity mutation in FormShipInstance:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ViewModel_DoesNotDirectlyMutateShipEntity()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "ShipViewModel.cs");
            var content = File.ReadAllText(filePath);
            Assert.That(content, Does.Not.Contain("public Ship Data"), "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public Ship _"), "ViewModel still exposes mutable Ship field");
        }

        private List<string> ScanForViolations(Regex[] patterns, HashSet<string> allowedFiles, string entityPrefix)
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
                    if (!line.Contains(entityPrefix) && !line.Contains(".Name") && !line.Contains(".UUID") && !line.Contains(".OwnerUUID") && !line.Contains(".HullBlueprintUUID") && !line.Contains(".Components") && !line.Contains(".Cargo") && !line.Contains(".Hopper") && !line.Contains(".LocationType") && !line.Contains(".LocationUUID") && !line.Contains(".HullCurrentHP") && !line.Contains(".HullMaxHP") && !line.Contains(".HullMaxRepairPercent") && !line.Contains(".TemplateUUID")) continue;
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