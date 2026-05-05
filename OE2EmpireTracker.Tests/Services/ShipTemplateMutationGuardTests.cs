using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure ShipTemplate entities are only mutated by allowed code.
    /// Feature: BL-115 ShipTemplate Immutable Data Model
    /// Validates: Property 9
    /// Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2
    /// </summary>
    [TestFixture]
    public class ShipTemplateMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ShipTemplateService.cs",
            "ShipTemplate.cs",
            "PlayerContext.cs",
            "FormShipInstance.cs",
            "ShipService.cs",
        };

        private static readonly HashSet<string> AllowedComponentsMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ShipTemplateService.cs",
            "ShipTemplate.cs",
            "FormShipInstance.cs",
            "SerializationSorter.cs",
            "ShipService.cs",
            "StationService.cs",
        };

        [Test]
        public void ShipTemplate_HullBlueprintUUIDSets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.HullBlueprintUUID\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedScalarMutators, "HullBlueprintUUID");
            Assert.That(violations, Is.Empty,
                "Found direct ShipTemplate.HullBlueprintUUID sets in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ShipTemplate_ComponentsListMutation_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                new Regex(@"\.Components\.Add\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Remove\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Clear\(", RegexOptions.Compiled),
                new Regex(@"\.Components\.Insert\(", RegexOptions.Compiled),
                new Regex(@"\.Components\s*=[^=]", RegexOptions.Compiled),
            };
            var violations = ScanForViolations(patterns, AllowedComponentsMutators, "ShipTemplate");
            Assert.That(violations, Is.Empty,
                "Found direct ShipTemplate.Components list mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FormShipTemplate_NoDirectShipTemplateMutation()
        {
            var patterns = new[]
            {
                new Regex(@"_selectedTemplate\.\w+\s*=", RegexOptions.Compiled),
                new Regex(@"template\.\w+\s*=[^=]", RegexOptions.Compiled),
            };
            var filePath = Path.Combine(SourceRoot, "Forms", "ShipTemplate", "FormShipTemplate.cs");
            var violations = ScanFileForPatterns(filePath, patterns);
            Assert.That(violations, Is.Empty,
                "Found direct ShipTemplate entity mutation in FormShipTemplate:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ViewModel_DoesNotExposeMutableShipTemplate()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "ShipTemplateViewModel.cs");
            var content = File.ReadAllText(filePath);
            Assert.That(content, Does.Not.Contain("public ShipTemplate Data"), "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public ShipTemplate _"), "ViewModel still exposes mutable ShipTemplate field");
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
                    if (!line.Contains(entityPrefix) && !line.Contains(".Name") && !line.Contains(".UUID") && !line.Contains(".OwnerUUID") && !line.Contains(".HullBlueprintUUID") && !line.Contains(".Components")) continue;
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