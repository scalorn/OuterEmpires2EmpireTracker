// <copyright file="BlueprintDetailImportTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Validates the blueprint detail import path (Phase 2 â€” CreateBlueprintDetailItem)
    /// works correctly for all known blueprint types from the discovery test data.
    /// </summary>
    [TestFixture]
    public class BlueprintDetailImportTests
    {
        private static readonly string SpecDataDir = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "spec", "game-api-data", "assets"));

        /// <summary>
        /// Gets all blueprint JSON file paths from the spec data directory.
        /// </summary>
        private static IEnumerable<string> AllBlueprintFiles()
        {
            if (!Directory.Exists(SpecDataDir))
            {
                yield break;
            }

            foreach (var file in Directory.GetFiles(SpecDataDir, "blueprint-*.json"))
            {
                yield return file;
            }
        }

        /// <summary>
        /// Returns one representative file path per unique blueprint.type from the API data,
        /// for use as test case sources grouped by type.
        /// </summary>
        private static IEnumerable<TestCaseData> OnePerBlueprintType()
        {
            var seen = new HashSet<string>();
            foreach (var file in AllBlueprintFiles())
            {
                string json = File.ReadAllText(file);
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
                var bpType = envelope?.Data?.Blueprint?.Type ?? string.Empty;

                if (!seen.Contains(bpType))
                {
                    seen.Add(bpType);
                    yield return new TestCaseData(file)
                        .SetName($"Type_{bpType}_{Path.GetFileNameWithoutExtension(file)}");
                }
            }
        }

        /// <summary>
        /// Returns all blueprint files as test case data for the exhaustive import test.
        /// </summary>
        private static IEnumerable<TestCaseData> AllBlueprintTestCases()
        {
            foreach (var file in AllBlueprintFiles())
            {
                yield return new TestCaseData(file)
                    .SetName($"Import_{Path.GetFileNameWithoutExtension(file)}");
            }
        }

        /// <summary>
        /// Replicates ConvertPartTypeIconToPosition from QueueSyncService.
        /// Converts a part type icon code (e.g. "N2", "A22") to a CSS sprite position string.
        /// </summary>
        private static string ConvertPartTypeIconToPosition(string partTypeIcon)
        {
            if (string.IsNullOrEmpty(partTypeIcon) || partTypeIcon.Length < 2)
            {
                return null;
            }

            char letter = char.ToUpperInvariant(partTypeIcon[0]);
            if (letter < 'A' || letter > 'Z')
            {
                return null;
            }

            string rowStr = partTypeIcon.Substring(1);
            if (!int.TryParse(rowStr, out int row))
            {
                return null;
            }

            int column = letter - 'A';
            int x = -((column * 38) + 24);
            int y = -((row * 38) - 14);

            return x + "px " + y + "px";
        }

        /// <summary>
        /// Builds the CrateImporter-compatible JSON entry from a blueprint detail response,
        /// replicating the logic in QueueSyncService.CreateBlueprintDetailItem.
        /// </summary>
        private static JObject BuildImportEntry(AssetBlueprint response)
        {
            var bpInfo = response.Blueprint;

            var entry = new JObject
            {
                ["name"] = bpInfo.Name,
                ["evolution"] = bpInfo.Evolution,
            };

            if (!string.IsNullOrEmpty(bpInfo.PartTypeIcon))
            {
                entry["iconClass"] = "ui_icon_" + bpInfo.PartTypeIcon;

                string iconPos = ConvertPartTypeIconToPosition(bpInfo.PartTypeIcon);
                if (!string.IsNullOrEmpty(iconPos))
                {
                    entry["iconPosition"] = iconPos;
                }
            }

            entry["description"] = bpInfo.Description;

            var propsObj = new JObject();
            if (response.BlueprintProperties != null)
            {
                foreach (var prop in response.BlueprintProperties)
                {
                    string key = !string.IsNullOrEmpty(prop.FriendlyPropertyName)
                        ? prop.FriendlyPropertyName
                        : prop.PropertyName;
                    string value = string.IsNullOrEmpty(prop.Unit)
                        ? prop.PropertyValue.ToString()
                        : prop.PropertyValue + prop.Unit;
                    propsObj[key] = value;
                }
            }

            // Map manufacture time — API value is in hours (e.g. 35 = 35h)
            int mfgHours = bpInfo.ManufactureTime;
            if (mfgHours > 0)
            {
                propsObj[BlueprintPropertyKeys.ManufactureRunTime] = mfgHours + "h";
            }

            // Map manufacture amount (only if > 1, since 1 is the default)
            if (bpInfo.ManufactureAmount > 1)
            {
                propsObj[BlueprintPropertyKeys.AmountManufactured] = bpInfo.ManufactureAmount.ToString();
            }

            entry["properties"] = propsObj;

            var resObj = new JObject();
            if (response.ResourcesRequired != null)
            {
                foreach (var res in response.ResourcesRequired)
                {
                    resObj[res.ResourceName] = res.ResourceAmount.ToString();
                }
            }

            entry["resources"] = resObj;

            return entry;
        }

        /// <summary>
        /// Imports a single blueprint detail file through the CrateImporter and
        /// returns the resulting Blueprint object from the context.
        /// </summary>
        private static Bp ImportAndResolve(string filePath)
        {
            TestHelper.ResetWithCachedData();
            var playerContext = PlayerContext.GetInstance();
            var empireContext = EmpireContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";

            string json = File.ReadAllText(filePath);
            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
            var response = envelope.Data;

            var entry = BuildImportEntry(response);
            var jsonArray = new JArray { entry };
            var importResult = CrateImporter.ImportFromJson(jsonArray.ToString(), playerContext, empireContext);

            Assert.That(importResult.Errors, Is.Empty,
                $"Import errors for {Path.GetFileName(filePath)}: {string.Join("; ", importResult.Errors)}");
            Assert.That(importResult.Entries, Has.Count.EqualTo(1),
                $"Expected exactly 1 import entry for {Path.GetFileName(filePath)}");
            Assert.That(importResult.Entries[0].Action, Is.Not.EqualTo(ImportAction.Skipped),
                $"Blueprint was skipped: {importResult.Entries[0].SkipReason}");

            string importedName = importResult.Entries[0].Name;
            int importedEvo = importResult.Entries[0].Evolution;

            // Resolve the imported blueprint from the appropriate context
            Bp found;
            if (importedEvo == 0)
            {
                found = empireContext.GlobalBlueprintList
                    .FirstOrDefault(b => b.Name == importedName && b.Evolution == importedEvo);
            }
            else
            {
                found = playerContext.BlueprintList
                    .FirstOrDefault(b => b.Name == importedName && b.Evolution == importedEvo);
            }

            Assert.That(found, Is.Not.Null,
                $"Could not find imported blueprint '{importedName}' evo {importedEvo}");

            return found;
        }

        // -----------------------------------------------------------------
        // Test 1: Per-type representative import validates full structure
        // -----------------------------------------------------------------

        /// <summary>
        /// For each unique blueprint type from the API data, imports one representative
        /// blueprint and validates it has: non-empty name, non-empty BluePrintType (resolved
        /// from icon), correct evolution, properties populated, and resources populated
        /// (when the API response has resources).
        /// </summary>
        [TestCaseSource(nameof(OnePerBlueprintType))]
        public void PerType_ImportProducesValidBlueprint(string filePath)
        {
            var blueprint = ImportAndResolve(filePath);

            string json = File.ReadAllText(filePath);
            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
            var response = envelope.Data;

            // Name must be non-empty
            Assert.That(blueprint.Name, Is.Not.Null.And.Not.Empty,
                "Blueprint name should not be empty");

            // BluePrintType must be resolved (from icon position or name classification)
            Assert.That(blueprint.BluePrintType, Is.Not.Null.And.Not.Empty,
                $"BluePrintType not resolved for '{blueprint.Name}' (API type: {response.Blueprint.Type}, icon: {response.Blueprint.PartTypeIcon})");

            // Evolution must match
            Assert.That(blueprint.Evolution, Is.EqualTo(response.Blueprint.Evolution),
                $"Evolution mismatch for '{blueprint.Name}'");

            // Properties must be populated when API provides them
            if (response.BlueprintProperties != null && response.BlueprintProperties.Count > 0)
            {
                Assert.That(blueprint.Properties.Count, Is.GreaterThan(0),
                    $"Properties should be populated for '{blueprint.Name}' (API has {response.BlueprintProperties.Count} properties)");
            }

            // Resources must be populated when API provides them
            if (response.ResourcesRequired != null && response.ResourcesRequired.Count > 0)
            {
                Assert.That(blueprint.Resources.Count, Is.GreaterThan(0),
                    $"Resources should be populated for '{blueprint.Name}' (API has {response.ResourcesRequired.Count} resources)");
            }
        }

        // -----------------------------------------------------------------
        // Test 2: Exhaustive import of ALL blueprint files â€” none fail
        // -----------------------------------------------------------------

        /// <summary>
        /// Runs ALL 260+ blueprint detail files through the import path and asserts
        /// NONE produce a blueprint with empty type, and that properties are populated
        /// when the API provides them.
        /// </summary>
        [TestCaseSource(nameof(AllBlueprintTestCases))]
        public void AllBlueprints_ImportSucceedsWithValidType(string filePath)
        {
            var blueprint = ImportAndResolve(filePath);

            string json = File.ReadAllText(filePath);
            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
            var response = envelope.Data;

            // BluePrintType must always be resolved
            Assert.That(blueprint.BluePrintType, Is.Not.Null.And.Not.Empty,
                $"BluePrintType not resolved for '{blueprint.Name}' " +
                $"(API type: {response.Blueprint.Type}, icon: {response.Blueprint.PartTypeIcon})");

            // Properties must be populated when API provides them
            if (response.BlueprintProperties != null && response.BlueprintProperties.Count > 0)
            {
                Assert.That(blueprint.Properties.Count, Is.GreaterThan(0),
                    $"Properties empty for '{blueprint.Name}' but API has {response.BlueprintProperties.Count}");
            }

            // Resources must be populated when API provides them
            if (response.ResourcesRequired != null && response.ResourcesRequired.Count > 0)
            {
                Assert.That(blueprint.Resources.Count, Is.GreaterThan(0),
                    $"Resources empty for '{blueprint.Name}' but API has {response.ResourcesRequired.Count}");
            }
        }

        // -----------------------------------------------------------------
        // Test 3: Verify icon-to-position conversion produces valid values
        // -----------------------------------------------------------------

        /// <summary>
        /// Verifies that all unique PartTypeIcon values from the test data produce
        /// a non-null icon position, confirming the conversion formula handles all
        /// real-world icon codes.
        /// </summary>
        [Test]
        public void AllPartTypeIcons_ConvertToValidPosition()
        {
            var iconCodes = new HashSet<string>();

            foreach (var file in AllBlueprintFiles())
            {
                string json = File.ReadAllText(file);
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
                var icon = envelope?.Data?.Blueprint?.PartTypeIcon;
                if (!string.IsNullOrEmpty(icon))
                {
                    iconCodes.Add(icon);
                }
            }

            Assert.That(iconCodes, Is.Not.Empty, "Should have found at least one icon code");

            foreach (var icon in iconCodes)
            {
                string position = ConvertPartTypeIconToPosition(icon);
                Assert.That(position, Is.Not.Null.And.Not.Empty,
                    $"Icon code '{icon}' should produce a valid position");
                Assert.That(position, Does.Contain("px"),
                    $"Position for icon '{icon}' should contain 'px': got '{position}'");
            }
        }

        // -----------------------------------------------------------------
        // Test 4: Verify JSON entry construction matches expected format
        // -----------------------------------------------------------------

        /// <summary>
        /// Verifies that BuildImportEntry produces a JObject with all expected fields
        /// when given a complete blueprint detail response.
        /// </summary>
        [Test]
        public void BuildImportEntry_ProducesExpectedFields()
        {
            // Use the first available blueprint file
            var files = AllBlueprintFiles().ToList();
            Assert.That(files, Is.Not.Empty, "No blueprint test data files found");

            string json = File.ReadAllText(files[0]);
            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
            var response = envelope.Data;

            var entry = BuildImportEntry(response);

            Assert.That(entry["name"]?.ToString(), Is.EqualTo(response.Blueprint.Name),
                "Entry name should match blueprint name");
            Assert.That(entry["evolution"]?.Value<int>(), Is.EqualTo(response.Blueprint.Evolution),
                "Entry evolution should match blueprint evolution");

            if (!string.IsNullOrEmpty(response.Blueprint.PartTypeIcon))
            {
                Assert.That(entry["iconClass"]?.ToString(),
                    Is.EqualTo("ui_icon_" + response.Blueprint.PartTypeIcon),
                    "Entry iconClass should be 'ui_icon_' + PartTypeIcon");
                Assert.That(entry["iconPosition"]?.ToString(), Is.Not.Null.And.Not.Empty,
                    "Entry iconPosition should be set when PartTypeIcon is present");
            }

            Assert.That(entry["properties"], Is.InstanceOf<JObject>(),
                "Entry should have a properties object");
            Assert.That(entry["resources"], Is.InstanceOf<JObject>(),
                "Entry should have a resources object");
        }

        // -----------------------------------------------------------------
        // Test 5: Summary coverage report
        // -----------------------------------------------------------------

        /// <summary>
        /// Reports coverage across all blueprint types and verifies every known
        /// type has at least one importable representative.
        /// </summary>
        [Test]
        public void AllBlueprintTypes_HaveAtLeastOneImportableRepresentative()
        {
            var typeToFiles = new Dictionary<string, List<string>>();

            foreach (var file in AllBlueprintFiles())
            {
                string json = File.ReadAllText(file);
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<AssetBlueprint>>(json);
                var bpType = envelope?.Data?.Blueprint?.Type ?? "(null)";

                if (!typeToFiles.ContainsKey(bpType))
                {
                    typeToFiles[bpType] = new List<string>();
                }

                typeToFiles[bpType].Add(file);
            }

            Assert.That(typeToFiles.Keys, Is.Not.Empty,
                "Should find at least one blueprint type in test data");

            TestContext.WriteLine($"Found {typeToFiles.Count} unique blueprint types across {AllBlueprintFiles().Count()} files:");
            foreach (var kvp in typeToFiles.OrderBy(k => k.Key))
            {
                TestContext.WriteLine($"  {kvp.Key}: {kvp.Value.Count} file(s)");
            }

            // Verify each type has at least one that imports successfully
            var failedTypes = new List<string>();
            foreach (var kvp in typeToFiles)
            {
                string firstFile = kvp.Value[0];
                try
                {
                    var blueprint = ImportAndResolve(firstFile);
                    if (string.IsNullOrEmpty(blueprint.BluePrintType))
                    {
                        failedTypes.Add($"{kvp.Key}: resolved empty BluePrintType");
                    }
                }
                catch (System.Exception ex)
                {
                    failedTypes.Add($"{kvp.Key}: {ex.Message}");
                }
            }

            Assert.That(failedTypes, Is.Empty,
                $"Some blueprint types failed to import:\n{string.Join("\n", failedTypes)}");
        }
    }
}
