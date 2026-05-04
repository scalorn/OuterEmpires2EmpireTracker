using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure MarketListing entities are only mutated by allowed code.
    /// Feature: BL-114 Market Immutable Data Model
    /// Validates: Property 5
    /// Validates: Requirements 15.1, 15.2, 15.3, 15.4, 18.1, 18.2
    /// </summary>
    [TestFixture]
    public class MarketListingMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        /// <summary>
        /// Allowed files for direct MarketListing property sets.
        /// </summary>
        private static readonly HashSet<string> AllowedMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MarketListingService.cs",
            "MarketService.cs",
            "MarketListing.cs",
            "PlayerContext.cs",
        };

        /// <summary>
        /// MarketListing-specific property set patterns.
        /// </summary>
        private static readonly string[] PropertyPatterns = new[]
        {
            @"\\.ItemName\\s*=[^=]",
            @"\\.ItemType\\s*=[^=]",
            @"\\.Quantity\\s*=[^=]",
            @"\\.PricePerUnit\\s*=[^=]",
            @"\\.StationUUID\\s*=[^=]",
            @"\\.CurrentHP\\s*=[^=]",
            @"\\.MaxHP\\s*=[^=]",
            @"\\.MaxRepairPercent\\s*=[^=]",
            @"\\.OwnerUUID\\s*=[^=]",
            @"\\.ItemReferenceID\\s*=[^=]",
        };

        /// <summary>
        /// Requirement 18.1: Direct MarketListing property sets only appear in allowed files.
        /// </summary>
        [Test]
        public void MarketListing_PropertySets_OnlyInAllowedFiles()
        {
            var compiled = PropertyPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled))
                .ToArray();

            var violations = ScanSourceFilesForPatterns(compiled, AllowedMutators);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct MarketListing property sets in non-allowed files:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 15.2: FormMarket does not directly set properties on MarketListing.
        /// </summary>
        [Test]
        public void FormMarket_NoDirectMarketListingMutation()
        {
            var filesToScan = new[] { Path.Combine("Forms", "Market", "FormMarket.cs") };
            var violations = ScanSpecificFilesForPatterns(filesToScan, PropertyPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct MarketListing entity mutation in FormMarket:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 15.3: FormListingEdit does not directly set properties on MarketListing.
        /// </summary>
        [Test]
        public void FormListingEdit_NoDirectMarketListingMutation()
        {
            var filesToScan = new[] { Path.Combine("Forms", "Market", "FormListingEdit.cs") };
            var violations = ScanSpecificFilesForPatterns(filesToScan, PropertyPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct MarketListing entity mutation in FormListingEdit:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 15.4: FormRecordSale does not directly set properties on MarketListing.
        /// </summary>
        [Test]
        public void FormRecordSale_NoDirectMarketListingMutation()
        {
            var filesToScan = new[] { Path.Combine("Forms", "Market", "FormRecordSale.cs") };
            var violations = ScanSpecificFilesForPatterns(filesToScan, PropertyPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct MarketListing entity mutation in FormRecordSale:\n" +
                string.Join("\n", violations));
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
                    if (line.StartsWith("[Json") || line.Contains("JsonProperty")) continue;
                    if (line.Contains("{ get;") || line.Contains("{ set;")) continue;
                    if (line.Contains("_viewModel.") || line.Contains("viewModel.")) continue;
                    if (line.Contains("request.") || line.Contains("Request.")) continue;

                    foreach (var rx in compiled)
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

        private List<string> ScanSpecificFilesForPatterns(string[] relativeFiles, string[] regexPatterns)
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
                    if (line.Contains("request.") || line.Contains("Request.")) continue;

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
