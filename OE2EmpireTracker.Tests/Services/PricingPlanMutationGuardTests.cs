using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure PricingPlan entities are only mutated by allowed code.
    /// Feature: bl-123-pricingplan-readonly
    /// Validates: Requirements 17.1, 17.2, 17.3, 20.1
    /// </summary>
    [TestFixture]
    public class PricingPlanMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly string CommonRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker.Common"));

        /// <summary>
        /// Requirement 17.1, 17.2: No direct PricingPlan entity mutation in form or ViewModel.
        /// Scans FormPricingPlan.cs and PricingPlanViewModel.cs for patterns like
        /// _selectedPlan.Name = or plan.Description = that indicate direct entity mutation.
        /// </summary>
        [Test]
        public void PricingPlan_NoDirectEntityMutation_InFormOrViewModel()
        {
            var entityMutationPatterns = new[]
            {
                @"_selectedPlan\.\w+\s*=",
                @"_plan\.\w+\s*=",
                @"plan\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "PricingPlan", "FormPricingPlan.cs"),
                Path.Combine("ViewModels", "PricingPlanViewModel.cs"),
            };

            var violations = ScanFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct PricingPlan entity mutation in form/ViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 17.3, 20.1: ViewModel does not expose mutable PricingPlan via public property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeMutablePricingPlan()
        {
            var filePath = Path.Combine(CommonRoot, "ViewModels", "PricingPlanViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public PricingPlan Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public PricingPlan _"),
                "ViewModel still exposes mutable PricingPlan field");
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