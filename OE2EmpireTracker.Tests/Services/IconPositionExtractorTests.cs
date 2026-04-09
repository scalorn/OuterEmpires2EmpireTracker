using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OE2EmpireTracker.Models.CommodityIndustry;

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

        /// <summary>
        /// The user-visible property keys found on this blueprint (excluding internal
        /// properties that start with '_'). Used to populate BlueprintType.Properties
        /// for new or empty entries.
        /// </summary>
        public List<string> PropertyNames { get; set; } = new List<string>();
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
                        ResolvedTypeId = mb.Blueprint.BluePrintType,
                        PropertyNames = mb.Blueprint.Properties.Properties.Keys
                            .Where(k => !k.StartsWith("_"))
                            .ToList()
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

            // --- Pre-process: Reclassify Ore Hopper blueprints ---
            // Ore Hoppers share the CargoPod icon, so the scanner resolves them as CargoPod.
            // Reclassify any blueprint with "Ore Hopper" in the name to have null ResolvedTypeId
            // so they get handled in the unknown-icons section with special OreHopper logic.
            foreach (var icon in extracted)
            {
                if (icon.BlueprintName != null &&
                    icon.BlueprintName.IndexOf("Ore Hopper", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    icon.ResolvedTypeId = null;
                }
            }

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

                // Fill in empty Properties arrays from extracted property names
                JArray existingProps = entry["Properties"] as JArray;
                if (existingProps != null && existingProps.Count == 0 && icon.PropertyNames.Count > 0)
                {
                    entry["Properties"] = new JArray(icon.PropertyNames.ToArray());
                    TestContext.WriteLine(
                        $"UPDATED: {typeId} Properties populated with {icon.PropertyNames.Count} entries from [{icon.SourceFile}]");
                    updatedCount++;
                }
            }

            // --- Match commodity factory flatpacks by name to per-industry entries ---
            // Name-based matching is a fallback for entries that don't yet have an IconPosition.
            // On the first run, per-industry entries have null IconPosition so the scanner can't
            // resolve them by icon. After the extractor populates IconPositions, subsequent runs
            // will resolve them by icon and they won't appear as unknowns here.
            var unknownAll = extracted
                .Where(e => string.IsNullOrEmpty(e.ResolvedTypeId))
                .ToList();

            // Build a lookup from commodity industry display name to per-industry BlueprintType Id
            var industryNameToTypeId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (CommodityIndustryEnum industry in Enum.GetValues(typeof(CommodityIndustryEnum)))
            {
                if (industry == CommodityIndustryEnum.None) continue;
                string displayName = CommodityIndustryMapByEnum.ContainsKey(industry)
                    ? CommodityIndustryMapByEnum[industry].Name
                    : industry.ToString();
                string typeId = BlueprintTypes.CommodityFactoryPrefix + industry.ToString();
                industryNameToTypeId[displayName] = typeId;
                // Also map with " Flatpack" suffix for direct matching
                industryNameToTypeId[displayName + " Flatpack"] = typeId;
            }

            // Also build a lookup for "Off-World" vs "OffWorld" variant
            // The game HTML uses "Off-World Living Institute" but the model uses "OffWorld Living Institute"
            if (!industryNameToTypeId.ContainsKey("Off-World Living Institute Flatpack"))
            {
                string offWorldTypeId = BlueprintTypes.CommodityFactoryPrefix + CommodityIndustryEnum.OffWorldLivingInstitute.ToString();
                industryNameToTypeId["Off-World Living Institute Flatpack"] = offWorldTypeId;
                industryNameToTypeId["Off-World Living Institute"] = offWorldTypeId;
            }

            var remainingUnknowns = new List<ExtractedIcon>();

            foreach (var icon in unknownAll)
            {
                string matchedTypeId;
                if (industryNameToTypeId.TryGetValue(icon.BlueprintName, out matchedTypeId))
                {
                    // Found a commodity factory match — update the per-industry entry's IconPosition
                    icon.ResolvedTypeId = matchedTypeId; // Mark as resolved for coverage gap report
                    JToken entry = blueprintTypes
                        .FirstOrDefault(bt => string.Equals(
                            (string)bt["Id"], matchedTypeId, StringComparison.Ordinal));

                    if (entry != null)
                    {
                        string oldPos = (string)entry["IconPosition"];
                        if (!string.Equals(oldPos, icon.IconPosition, StringComparison.Ordinal))
                        {
                            TestContext.WriteLine(
                                $"UPDATED (commodity factory): {matchedTypeId} IconPosition set to \"{icon.IconPosition}\" (was \"{oldPos}\") from [{icon.SourceFile}]");
                            entry["IconPosition"] = icon.IconPosition;
                            updatedCount++;
                        }
                    }
                    else
                    {
                        TestContext.WriteLine(
                            $"WARNING: Matched commodity factory \"{icon.BlueprintName}\" to {matchedTypeId} but entry not found in BaselineData");
                    }
                }
                else if (icon.BlueprintName.IndexOf("Ore Hopper", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // OreHopper blueprint — update or create the OreHopper entry
                    icon.ResolvedTypeId = BlueprintTypes.OreHopper; // Mark as resolved for coverage gap report
                    JToken oreHopperEntry = blueprintTypes
                        .FirstOrDefault(bt => string.Equals(
                            (string)bt["Id"], BlueprintTypes.OreHopper, StringComparison.Ordinal));

                    if (oreHopperEntry != null)
                    {
                        string oldPos = (string)oreHopperEntry["IconPosition"];
                        if (!string.Equals(oldPos, icon.IconPosition, StringComparison.Ordinal))
                        {
                            TestContext.WriteLine(
                                $"UPDATED (OreHopper): {BlueprintTypes.OreHopper} IconPosition set to \"{icon.IconPosition}\" (was \"{oldPos}\") from [{icon.SourceFile}]");
                            oreHopperEntry["IconPosition"] = icon.IconPosition;
                            updatedCount++;
                        }

                        // Fill in empty Properties arrays from extracted property names
                        JArray existingProps = oreHopperEntry["Properties"] as JArray;
                        if (existingProps != null && existingProps.Count == 0 && icon.PropertyNames.Count > 0)
                        {
                            oreHopperEntry["Properties"] = new JArray(icon.PropertyNames.ToArray());
                            TestContext.WriteLine(
                                $"UPDATED (OreHopper): {BlueprintTypes.OreHopper} Properties populated with {icon.PropertyNames.Count} entries from [{icon.SourceFile}]");
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Create new OreHopper entry with properties from the first extracted blueprint
                        var newEntry = new JObject
                        {
                            ["Id"] = BlueprintTypes.OreHopper,
                            ["Name"] = "Ore Hopper",
                            ["Properties"] = new JArray(icon.PropertyNames.ToArray()),
                            ["ResearchableProperties"] = new JArray(),
                            ["IconPosition"] = icon.IconPosition,
                            ["OutputItemType"] = "ShipPart"
                        };
                        blueprintTypes.Add(newEntry);
                        addedCount++;
                        TestContext.WriteLine(
                            $"ADDED: New BlueprintType \"{BlueprintTypes.OreHopper}\" with IconPosition \"{icon.IconPosition}\" from [{icon.SourceFile}]");
                    }
                }
                else
                {
                    remainingUnknowns.Add(icon);
                }
            }

            // Add new BlueprintType entries for truly unknown icons
            var unknownIcons = remainingUnknowns
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
                    ["Properties"] = new JArray(icon.PropertyNames.ToArray()),
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

        /// <summary>
        /// Default Properties array for commodity factory BlueprintType entries.
        /// Matches the properties defined in Task 4.1 for per-industry entries.
        /// </summary>
        private static readonly string[] DefaultCommodityFactoryProperties = new[]
        {
            "Commodity Industry",
            "Manufacture Run Time",
            "Mass",
            "Cargo Volume Size",
            "Structural Integrity",
            "Power Required",
            "Can Research",
            "Can Manufacture",
            "Max Per Colony",
            "Blue Collar Detail",
            "Unassigned White Collar Detail"
        };

        /// <summary>
        /// Ensures all 14 per-industry CommodityFactory BlueprintType entries exist
        /// in the BaselineData JObject. Iterates over all CommodityIndustryEnum values
        /// (excluding None), checks for existing entries by Id, and creates missing
        /// entries with null IconPosition and default properties.
        /// </summary>
        private void EnsureCommodityFactoryEntries(JObject baselineRoot)
        {
            JArray blueprintTypes = (JArray)baselineRoot["BlueprintType"];
            int addedCount = 0;

            foreach (CommodityIndustryEnum industry in Enum.GetValues(typeof(CommodityIndustryEnum)))
            {
                if (industry == CommodityIndustryEnum.None)
                    continue;

                string industryName = industry.ToString();
                string expectedId = BlueprintTypes.CommodityFactoryPrefix + industryName;

                bool exists = blueprintTypes.Any(bt =>
                    string.Equals((string)bt["Id"], expectedId, StringComparison.Ordinal));

                if (exists)
                    continue;

                // Get the display name from the CommodityIndustry model
                string displayName = CommodityIndustryMapByEnum.ContainsKey(industry)
                    ? CommodityIndustryMapByEnum[industry].Name
                    : industryName;

                var newEntry = new JObject
                {
                    ["Id"] = expectedId,
                    ["Name"] = displayName + " Flatpack",
                    ["Universal"] = true,
                    ["Properties"] = new JArray(DefaultCommodityFactoryProperties),
                    ["ResearchableProperties"] = new JArray(),
                    ["IconPosition"] = null,
                    ["OutputItemType"] = "Flatpack"
                };

                blueprintTypes.Add(newEntry);
                addedCount++;
                TestContext.WriteLine(
                    $"ENSURED: Added missing CommodityFactory entry \"{expectedId}\" ({displayName} Flatpack)");
            }

            TestContext.WriteLine(
                $"EnsureCommodityFactoryEntries summary: {addedCount} entries added");
        }

        /// <summary>
        /// Writes the updated BaselineData JObject to both the main application
        /// directory and the test project TestData directory, keeping them in sync.
        /// </summary>
        private void WriteBothBaselineFiles(JObject baselineRoot)
        {
            string testDir = TestContext.CurrentContext.TestDirectory;
            string solutionRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", ".."));

            string mainPath = Path.Combine(solutionRoot, "OE2EmpireTracker", "BaselineData.json");
            string testPath = Path.Combine(solutionRoot, "OE2EmpireTracker.Tests", "TestData", "BaselineData.json");

            string json = JsonConvert.SerializeObject(baselineRoot, Formatting.Indented);

            File.WriteAllText(mainPath, json);
            TestContext.WriteLine($"Wrote BaselineData to: {mainPath}");

            File.WriteAllText(testPath, json);
            TestContext.WriteLine($"Wrote BaselineData to: {testPath}");
        }

        /// <summary>
        /// Compares all BlueprintType Ids in BaselineData against extracted icons
        /// and logs a dedicated "Coverage Gap Report" identifying uncovered types.
        /// Distinguishes between types with stale IconPosition (has a value but no
        /// HTML coverage to verify it) vs types with null IconPosition (never characterized).
        /// CommodityFactory variants are listed in a separate section.
        /// </summary>
        private void ProduceCoverageGapReport(JObject baselineRoot, List<ExtractedIcon> extracted)
        {
            JArray blueprintTypes = (JArray)baselineRoot["BlueprintType"];

            // 1. Collect all BlueprintType Ids from BaselineData
            var allTypeIds = blueprintTypes
                .Select(bt => (string)bt["Id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            // 2. Collect all unique ResolvedTypeIds from extracted icons
            var coveredTypeIds = new HashSet<string>(
                extracted
                    .Where(e => !string.IsNullOrEmpty(e.ResolvedTypeId))
                    .Select(e => e.ResolvedTypeId),
                StringComparer.Ordinal);

            // 3. Find uncovered types (in BaselineData but not in extracted set)
            var uncoveredTypes = allTypeIds
                .Where(id => !coveredTypeIds.Contains(id))
                .ToList();

            // 4. Classify each uncovered type and separate CommodityFactory variants
            var staleTypes = new List<string>();
            var missingTypes = new List<string>();
            var staleCommodityVariants = new List<string>();
            var missingCommodityVariants = new List<string>();

            foreach (string typeId in uncoveredTypes)
            {
                JToken entry = blueprintTypes
                    .FirstOrDefault(bt => string.Equals(
                        (string)bt["Id"], typeId, StringComparison.Ordinal));

                if (entry == null)
                    continue;

                string iconPosition = (string)entry["IconPosition"];
                bool hasIconPosition = !string.IsNullOrEmpty(iconPosition);
                bool isCommodityFactory = typeId.IsCommodityFactory();

                if (isCommodityFactory)
                {
                    if (hasIconPosition)
                        staleCommodityVariants.Add(typeId);
                    else
                        missingCommodityVariants.Add(typeId);
                }
                else
                {
                    if (hasIconPosition)
                        staleTypes.Add(typeId);
                    else
                        missingTypes.Add(typeId);
                }
            }

            // 5. Log the Coverage Gap Report
            TestContext.WriteLine("");
            TestContext.WriteLine("========================================");
            TestContext.WriteLine("       COVERAGE GAP REPORT");
            TestContext.WriteLine("========================================");
            TestContext.WriteLine($"Total BlueprintTypes in BaselineData: {allTypeIds.Count}");
            TestContext.WriteLine($"Types with HTML coverage: {coveredTypeIds.Count}");
            TestContext.WriteLine($"Types without HTML coverage: {uncoveredTypes.Count}");
            TestContext.WriteLine("");

            // 6. List non-CommodityFactory uncovered types
            if (staleTypes.Count > 0 || missingTypes.Count > 0)
            {
                TestContext.WriteLine("--- Uncovered BlueprintTypes ---");
                foreach (string typeId in staleTypes)
                {
                    TestContext.WriteLine($"  STALE:   {typeId}  (has IconPosition but no HTML coverage)");
                }
                foreach (string typeId in missingTypes)
                {
                    TestContext.WriteLine($"  MISSING: {typeId}  (null IconPosition — never characterized)");
                }
                TestContext.WriteLine("");
            }

            // 7. Separately list CommodityFactory variants
            if (staleCommodityVariants.Count > 0 || missingCommodityVariants.Count > 0)
            {
                TestContext.WriteLine("--- Uncovered CommodityFactory Variants ---");
                foreach (string typeId in staleCommodityVariants)
                {
                    TestContext.WriteLine($"  STALE:   {typeId}  (has IconPosition but no HTML coverage)");
                }
                foreach (string typeId in missingCommodityVariants)
                {
                    TestContext.WriteLine($"  MISSING: {typeId}  (null IconPosition — never characterized)");
                }
                TestContext.WriteLine("");
            }

            if (uncoveredTypes.Count == 0)
            {
                TestContext.WriteLine("All BlueprintTypes have HTML coverage. No gaps detected.");
            }

            TestContext.WriteLine("========================================");
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

        /// <summary>
        /// Main "run on demand" test that the developer executes whenever the game
        /// updates its sprite sheet. Calls all helpers in sequence:
        /// extract → compare &amp; update → ensure commodity entries → write files → produce gap report.
        /// </summary>
        [Test]
        public void ExtractAndUpdateIconPositions()
        {
            // 1. Extract all icons from every MarketSample*.html file
            var extracted = ExtractIconsFromAllSamples();
            Assert.That(extracted, Is.Not.Empty,
                "Expected at least one icon extracted from MarketSample HTML files");

            // 2. Compare extracted positions against BaselineData and apply updates
            var baselineRoot = CompareAndUpdateBaselineData(extracted);
            Assert.That(baselineRoot, Is.Not.Null,
                "CompareAndUpdateBaselineData should return a non-null JObject");

            // 3. Ensure all 14 per-industry CommodityFactory entries exist
            EnsureCommodityFactoryEntries(baselineRoot);

            // 4. Write updated BaselineData to both main and test directories
            WriteBothBaselineFiles(baselineRoot);

            // 5. Produce the coverage gap report
            ProduceCoverageGapReport(baselineRoot, extracted);
        }
    }
}
