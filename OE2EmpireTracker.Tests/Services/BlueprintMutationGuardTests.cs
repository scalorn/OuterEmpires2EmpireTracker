using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure Blueprint entities are only mutated by allowed code.
    /// Feature: BL-108 Blueprint Immutable Data Model
    /// Validates: No direct entity mutation in form or ViewModel.
    /// </summary>
    [TestFixture]
    public class BlueprintMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly string CommonRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker.Common"));

        /// <summary>
        /// No direct Blueprint entity mutation in form or ViewModel.
        /// Scans FormBlueprintV2 and BlueprintViewModel for patterns like
        /// _blueprint.Name = or blueprint.Evolution = that indicate direct entity mutation.
        /// </summary>
        [Test]
        public void Blueprint_NoDirectEntityMutation_InFormOrViewModel()
        {
            var entityMutationPatterns = new[]
            {
                @"_blueprint\.\w+\s*=",
                @"blueprint\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "BlueprintV2", "FormBlueprintV2.cs"),
                Path.Combine("ViewModels", "BlueprintViewModel.cs"),
            };

            var violations = ScanFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct Blueprint entity mutation in form/ViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// ViewModel does not expose mutable Data property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeDataProperty()
        {
            var filePath = Path.Combine(CommonRoot, "ViewModels", "BlueprintViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public Blueprint Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public Blueprint _blueprint"),
                "ViewModel still exposes mutable _blueprint field");
        }

        private List<string> ScanFilesForPatterns(string[] relativeFiles, string[] regexPatterns)
        {
            var violations = new List<string>();
            var compiled = regexPatterns.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

            foreach (var relFile in relativeFiles)
            {
                var fullPath = Path.Combine(SourceRoot, relFile);
                if (!File.Exists(fullPath)) continue;

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*")) continue;

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
