using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Simple DTO capturing one extracted icon from a MarketSample HTML file.
    /// </summary>
    public class ExtractedIcon
    {
        /// <summary>Blueprint name as it appears in the market listing.</summary>
        public string BlueprintName { get; set; }

        /// <summary>CSS sprite position string (e.g. "-328px -62px").</summary>
        public string IconPosition { get; set; }

        /// <summary>The MarketSample HTML filename this icon was found in.</summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// The BlueprintType Id resolved via EmpireContext.FindBlueprintTypeByIcon(),
        /// or null if no matching BlueprintType exists.
        /// </summary>
        public string ResolvedTypeId { get; set; }
    }

    [TestFixture]
    public class IconPositionExtractorTests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetEmpireFilePath();
            EmpireContext.getInstance();
        }

        /// <summary>
        /// Parses every MarketSample*.html file in TestData using
        /// BlueprintScanner.ProcessMarketHtml() and extracts the _IconPosition
        /// property from each parsed blueprint.
        /// </summary>
        private List<ExtractedIcon> ExtractIconsFromAllSamples()
        {
            var results = new List<ExtractedIcon>();
            string testDataDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "TestData");

            var sampleFiles = Directory.GetFiles(testDataDir, "MarketSample*.html");
            var scanner = new BlueprintScanner();

            foreach (string filePath in sampleFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string html = File.ReadAllText(filePath);
                List<MarketBlueprint> parsed = scanner.ProcessMarketHtml(html);

                foreach (var mb in parsed)
                {
                    string iconPos = null;
                    mb.Blueprint.Properties.getString("_IconPosition", null, out iconPos);
                    if (string.IsNullOrEmpty(iconPos))
                        continue;

                    results.Add(new ExtractedIcon
                    {
                        BlueprintName = mb.Blueprint.Name,
                        IconPosition = iconPos,
                        SourceFile = fileName,
                        ResolvedTypeId = mb.Blueprint.BluePrintType
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Loads BaselineData.json from the source tree as a JObject, compares
        /// extracted icon positions against BlueprintType entries, updates changed
        /// positions, and adds new BlueprintType entries for unknown icons.
        /// All changes are logged via TestContext.WriteLine.
        /// </summary>
        private JObject CompareAndUpdateBaselineData(List<ExtractedIcon> extracted)
        {
            // Compute path to source-tree BaselineData.json:
            // TestDirectory is bin/Debug, go up to project root, then up to solution root
            string testDir = TestContext.CurrentContext.TestDirectory;
            string solutionRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", ".."));
            string baselinePath = Path.Combine(solutionRoot, "OE2EmpireTracker", "BaselineData.json");

            string json = File.ReadAllText(baselinePath);
            JObject root = JObject.Parse(json);
            JArray blueprintTypes = (JArray)root["BlueprintType"];

            int updatedCount = 0;
            int addedCount = 0;

            // Group extracted icons by ResolvedTypeId to handle duplicates
            // (same type may appear in multiple sample files)
            var resolvedIcons = extracted
                .Where(e => !string.IsNullOrEmpty(e.ResolvedTypeId))
                .GroupBy(e => e.ResolvedTypeId)
                .ToDictionary(g => g.Key, g => g.First());

            // Compare and update existing BlueprintType entries
            foreach (var kvp in resolvedIcons)
            {
                string typeId = kvp.Key;
                ExtractedIcon icon = kvp.Value;

                JToken entry = blueprintTypes
                    .FirstOrDefault(bt => string.Equals(
                        (string)bt["Id"], typeId, StringComparison.Ordinal));

                if (entry == null)
                    continue;

                string oldPos = (string)entry["IconPosition"];
                if (!string.Equals(oldPos, icon.IconPosition, StringComparison.Ordinal))
                {
                    TestContext.WriteLine(
                        $"UPDATED: {typeId} IconPosition changed from \"{oldPos}\" to \"{icon.IconPosition}\"");
                    entry["IconPosition"] = icon.IconPosition;
                    updatedCount++;
                }
            }

            // Add new BlueprintType entries for unknown icons (no ResolvedTypeId)
            var unknownIcons = extracted
                .Where(e => string.IsNullOrEmpty(e.ResolvedTypeId))
                .GroupBy(e => e.IconPosition)
                .Select(g => g.First())
                .ToList();

            foreach (var icon in unknownIcons)
            {
                // Check if an entry with this icon position already exists
                bool alreadyExists = blueprintTypes.Any(bt =>
                    string.Equals((string)bt["IconPosition"], icon.IconPosition, StringComparison.Ordinal));

                if (alreadyExists)
                    continue;

                var newEntry = new JObject
                {
                    ["Id"] = icon.BlueprintName,
                    ["Name"] = icon.BlueprintName,
                    ["Properties"] = new JArray(),
                    ["ResearchableProperties"] = new JArray(),
                    ["IconPosition"] = icon.IconPosition,
                    ["OutputItemType"] = ""
                };

                blueprintTypes.Add(newEntry);
                addedCount++;
                TestContext.WriteLine(
                    $"ADDED: New BlueprintType \"{icon.BlueprintName}\" with IconPosition \"{icon.IconPosition}\" from [{icon.SourceFile}]");
            }

            TestContext.WriteLine(
                $"CompareAndUpdateBaselineData summary: {updatedCount} updated, {addedCount} added");

            return root;
        }

        [Test]
        public void ExtractIconsFromAllSamples_FindsIcons()
        {
            var icons = ExtractIconsFromAllSamples();

            TestContext.WriteLine($"Total extracted icons: {icons.Count}");
            foreach (var icon in icons)
            {
                string typeLabel = icon.ResolvedTypeId ?? "UNKNOWN";
                TestContext.WriteLine(
                    $"  {icon.IconPosition} -> {typeLabel} ({icon.BlueprintName}) [{icon.SourceFile}]");
            }

            Assert.That(icons, Is.Not.Empty,
                "Expected at least one icon extracted from MarketSample HTML files");
        }
    }
}
