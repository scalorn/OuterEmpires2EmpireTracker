using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure Colony entities are only mutated by allowed code.
    /// Feature: BL-109 Colony Immutable Data Model
    /// Validates: Requirements 29.1, 29.2, 29.3, 32.1, 32.2
    /// </summary>
    [TestFixture]
    public class ColonyMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        /// <summary>
        /// Allowed files for direct Colony scalar property sets.
        /// </summary>
        private static readonly HashSet<string> AllowedScalarMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ColonyService.cs",
            "ColonyParser.cs",
            "Colony.cs",
            "ColonyImportHelper.cs",
            "SurveyParser.cs",
            "SurveyImportHelper.cs",
            "SurveyService.cs",
            "FormAsteroid.cs",
        };

        /// <summary>
        /// Allowed files for direct Colony.Structures list mutation.
        /// </summary>
        private static readonly HashSet<string> AllowedStructureMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ColonyService.cs",
            "ColonyParser.cs",
            "Colony.cs",
            "ColonyImportHelper.cs",
            "ColonyBootstrap.cs",
            "BuildOrderOptimizer.cs",
            "FormColonyV2.cs",
            "SerializationSorter.cs",
            "ColonyStructureViewModel.cs",
        };

        /// <summary>
        /// Requirement 32.1: Direct Colony scalar property sets only appear in allowed files.
        /// Scans all non-test .cs files for property set patterns
        /// (PlanetName, ColonyName, SystemName) and asserts they only appear in
        /// allowed mutator files, migration code, and test code.
        /// Note: PlanetName and SystemName are shared across Colony, Survey, and Asteroid.
        /// </summary>
        [Test]
        public void Colony_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var scalarPropertyPatterns = new[]
            {
                @"\.PlanetName\s*=[^=]",
                @"\.ColonyName\s*=[^=]",
                @"\.SystemName\s*=[^=]",
            };

            var compiled = scalarPropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedScalarMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Colony scalar property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 32.2: Direct Colony.Structures list mutation only appears in allowed files.
        /// Scans all non-test .cs files for Structures list mutation patterns
        /// (Add, Remove, Clear, AddRange) and asserts they only appear in
        /// allowed mutator files (ColonyService, ColonyParser, Colony.cs,
        /// ColonyImportHelper, ColonyBootstrap, BuildOrderOptimizer,
        /// FormColonyV2 optimize, SerializationSorter, ColonyStructureViewModel),
        /// migration code, and test code.
        /// </summary>
        [Test]
        public void Colony_StructuresListMutation_OnlyInAllowedFiles()
        {
            var structureMutationPatterns = new[]
            {
                @"\.Structures\.Add\(",
                @"\.Structures\.AddRange\(",
                @"\.Structures\.Remove\(",
                @"\.Structures\.Clear\(",
                @"\.Structures\s*=[^=]",
            };

            var compiled = structureMutationPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedStructureMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Colony.Structures list mutation in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 29.2: FormColonyV2 does not directly set properties on Colony.
        /// </summary>
        [Test]
        public void FormColonyV2_NoDirectColonyMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_colony\.\w+\s*=",
                @"colony\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "ColonyV2", "FormColonyV2.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Colony entity mutation in FormColonyV2:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 29.3: ColonyViewModel does not directly set properties on Colony.
        /// </summary>
        [Test]
        public void ColonyViewModel_NoDirectColonyMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_colony\.\w+\s*=",
                @"colony\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("ViewModels", "ColonyViewModel.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Colony entity mutation in ColonyViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 3.2: ViewModel does not expose mutable Colony via public property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeMutableColony()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "ColonyViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public Colony Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public Colony _colony"),
                "ViewModel still exposes mutable _colony field");
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
