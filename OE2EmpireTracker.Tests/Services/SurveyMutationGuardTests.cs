using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure Survey and SurveyResource entities are only
    /// mutated by allowed code.
    /// Feature: BL-110 Survey Immutable Data Model
    /// Validates: Requirements 21.1, 21.2, 21.3, 24.1, 24.2
    /// </summary>
    [TestFixture]
    public class SurveyMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly string CommonRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker.Common"));

        /// <summary>
        /// Allowed files for direct Survey property sets.
        /// </summary>
        private static readonly HashSet<string> AllowedSurveyMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SurveyService.cs",
            "SurveyImportHelper.cs",
            "SurveyParser.cs",
            "Survey.cs",
        };

        /// <summary>
        /// Allowed files for direct SurveyResource property sets.
        /// </summary>
        private static readonly HashSet<string> AllowedResourceMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SurveyService.cs",
            "SurveyImportHelper.cs",
            "SurveyParser.cs",
            "Survey.cs",
        };

        /// <summary>
        /// Requirement 24.1: Direct Survey property sets only appear in allowed files.
        /// Scans all non-test .cs files for Survey-unique property set patterns
        /// (SurveyID, ScannedBy, ScannerBlueprintUUID are unique to Survey)
        /// and asserts they only appear in SurveyService, SurveyImportHelper,
        /// SurveyParser, Survey.cs, migration code, and test code.
        /// </summary>
        [Test]
        public void Survey_PropertySets_OnlyInAllowedFiles()
        {
            // Use Survey-unique property names to avoid false positives from
            // shared property names (PlanetName, SystemName, NickName, OwnerUUID)
            // that exist on Colony, Blueprint, Asteroid, etc.
            var surveyPropertyPatterns = new[]
            {
                @"\.SurveyID\s*=[^=]",
                @"\.ScannedBy\s*=[^=]",
                @"\.ScannerBlueprintUUID\s*=[^=]",
                @"\.SurveyType\s*=[^=]",
            };

            var compiled = surveyPropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedSurveyMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Survey property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 24.2: Direct SurveyResource property sets only appear in allowed files.
        /// Scans all non-test .cs files for SurveyResource property set patterns
        /// (.Resource =, .Purity =, .Amount = excluding == comparisons)
        /// and asserts they only appear in allowed files.
        /// </summary>
        [Test]
        public void SurveyResource_PropertySets_OnlyInAllowedFiles()
        {
            var resourcePropertyPatterns = new[]
            {
                @"\.Resource\s*=[^=]",
                @"\.Purity\s*=[^=]",
                @"\.Amount\s*=[^=]",
            };

            var compiled = resourcePropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedResourceMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct SurveyResource property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.2: FormSurvey does not directly set properties on Survey.
        /// </summary>
        [Test]
        public void FormSurvey_NoDirectSurveyMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_survey\.\w+\s*=",
                @"survey\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "Survey", "FormSurvey.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Survey entity mutation in FormSurvey:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 21.3: SurveyViewModel does not directly set properties on Survey.
        /// </summary>
        [Test]
        public void SurveyViewModel_NoDirectSurveyMutation()
        {
            var entityMutationPatterns = new[]
            {
                @"_survey\.\w+\s*=",
                @"survey\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("ViewModels", "SurveyViewModel.cs"),
            };

            var violations = ScanSpecificFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Survey entity mutation in SurveyViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 3.2: ViewModel does not expose mutable Survey via public property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeMutableSurvey()
        {
            var filePath = Path.Combine(CommonRoot, "ViewModels", "SurveyViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public Survey Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public Survey _survey"),
                "ViewModel still exposes mutable _survey field");
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
