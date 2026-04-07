using NUnit.Framework;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    using BpModel = OE2EmpireTracker.Models.Blueprint;
    [TestFixture]
    public class MarketBlueprintImporterTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private static readonly string TestPlayerUUID = "test-player-uuid-001";

        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;
        private string _tempBaselineDataPath;
        private string _tempPlayerDataPath;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string baselineDataPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\OE2EmpireTracker\BaselineData.json"));
            string playerDataPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\OE2EmpireTracker\PlayerData.json"));

            // Save original paths so we can restore them
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            // Create temp copies so writeContext() never corrupts the real data files
            _tempBaselineDataPath = Path.Combine(Path.GetTempPath(), "MarketImporterTest_BaselineData.json");
            _tempPlayerDataPath = Path.Combine(Path.GetTempPath(), "MarketImporterTest_PlayerData.json");
            File.Copy(baselineDataPath, _tempBaselineDataPath, true);
            File.Copy(playerDataPath, _tempPlayerDataPath, true);

            EmpireContext.FilePath = _tempBaselineDataPath;
            PlayerContext.FilePath = _tempPlayerDataPath;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            // Restore original file paths and reset singletons
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;

            // Clean up temp files
            if (File.Exists(_tempBaselineDataPath)) File.Delete(_tempBaselineDataPath);
            if (File.Exists(_tempPlayerDataPath)) File.Delete(_tempPlayerDataPath);
            // SafeFileWriter also creates .bak files
            if (File.Exists(_tempBaselineDataPath + ".bak")) File.Delete(_tempBaselineDataPath + ".bak");
            if (File.Exists(_tempPlayerDataPath + ".bak")) File.Delete(_tempPlayerDataPath + ".bak");
        }

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            empireContext = EmpireContext.getInstance();
            playerContext = PlayerContext.getInstance();

            // Clear any existing blueprints from both lists
            empireContext.globalBlueprintList.Clear();
            playerContext.blueprintList.Clear();

            // Set a current player for player blueprint tests
            playerContext.CurrentPlayerUUID = TestPlayerUUID;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static MarketBlueprint MakeMarketBlueprint(
            string name, string seller, string bpType = "Reactor",
            int evolution = 0, int cls = 1, string techLevel = null,
            Dictionary<string, string> props = null,
            Dictionary<string, string> resources = null)
        {
            var bp = new BpModel(name);
            bp.BluePrintType = bpType;
            bp.Evolution = evolution;
            bp.Class = cls;
            bp.TechLevel = techLevel;
            if (props != null)
            {
                foreach (var kvp in props)
                    bp.Properties.setProperty(kvp.Key, kvp.Value);
            }
            else
            {
                // Default: at least one property so it's not treated as unexpanded
                bp.Properties.setProperty("Health", "100");
            }
            if (resources != null)
                bp.Resources = resources;
            return new MarketBlueprint { Blueprint = bp, SellerName = seller };
        }

        private static MarketBlueprint MakeUnexpandedBlueprint(string name, string seller)
        {
            var bp = new BpModel(name);
            // Zero properties, no BluePrintType → unexpanded
            bp.BluePrintType = null;
            bp.Properties = new PropertyBag();
            return new MarketBlueprint { Blueprint = bp, SellerName = seller };
        }

        // -----------------------------------------------------------------------
        // Unit Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Validates: Requirements 3, 7
        /// Government seller → blueprint added to globalBlueprintList
        /// </summary>
        [Test]
        public void Import_GovernmentSeller_CreatesGlobalBlueprint()
        {
            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("AMX-SS Reactor Core", "Government")
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(1));
            Assert.That(empireContext.globalBlueprintList[0].Name, Is.EqualTo("AMX-SS Reactor Core"));
            Assert.That(result.Entries[0].Storage, Is.EqualTo("Global"));
        }

        /// <summary>
        /// Validates: Requirements 3, 7
        /// Non-Government seller → blueprint added to playerContext.blueprintList with OwnerUUID
        /// </summary>
        [Test]
        public void Import_PlayerSeller_CreatesPlayerBlueprint()
        {
            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("Fighter Bomber", "SomePlayer")
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(playerContext.blueprintList.Count, Is.EqualTo(1));
            Assert.That(playerContext.blueprintList[0].Name, Is.EqualTo("Fighter Bomber"));
            Assert.That(playerContext.blueprintList[0].OwnerUUID, Is.EqualTo(TestPlayerUUID));
            Assert.That(result.Entries[0].Storage, Is.EqualTo("Player"));
        }

        /// <summary>
        /// Validates: Requirement 2
        /// Unexpanded listing (zero properties, no BluePrintType) → skipped
        /// </summary>
        [Test]
        public void Import_UnexpandedListing_IsSkipped()
        {
            var list = new List<MarketBlueprint>
            {
                MakeUnexpandedBlueprint("Unknown Item", "Government")
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(result.CreatedCount, Is.EqualTo(0));
            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(0));
            Assert.That(result.Entries[0].SkipReason, Is.EqualTo("Unexpanded listing"));
        }

        /// <summary>
        /// Validates: Requirement 3
        /// Player-routed blueprint with no current player → skipped
        /// </summary>
        [Test]
        public void Import_NoCurrentPlayer_SkipsPlayerBlueprint()
        {
            playerContext.CurrentPlayerUUID = string.Empty;

            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("Fighter Bomber", "SomePlayer")
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(result.CreatedCount, Is.EqualTo(0));
            Assert.That(playerContext.blueprintList.Count, Is.EqualTo(0));
            Assert.That(result.Entries[0].SkipReason, Is.EqualTo("No current player selected"));
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// Duplicate key match → existing blueprint updated, not duplicated
        /// </summary>
        [Test]
        public void Import_DuplicateKey_UpdatesExistingBlueprint()
        {
            // Pre-populate global list with an existing blueprint
            var existing = new BpModel("AMX-SS Reactor Core");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BluePrintType = "Reactor";
            existing.Evolution = 0;
            existing.Class = 1;
            existing.TechLevel = null;
            existing.Properties.setProperty("Health", "50");
            empireContext.globalBlueprintList.Add(existing);

            // Import one with matching dedup key but different property value
            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("AMX-SS Reactor Core", "Government", "Reactor", 0, 1, null,
                    new Dictionary<string, string> { { "Health", "200" } })
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.UpdatedCount, Is.EqualTo(1));
            Assert.That(result.CreatedCount, Is.EqualTo(0));
            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(1));
            // Property should be updated
            string healthVal;
            empireContext.globalBlueprintList[0].Properties.getString("Health", null, out healthVal);
            Assert.That(healthVal, Is.EqualTo("200"));
            // UUID preserved
            Assert.That(empireContext.globalBlueprintList[0].UUID, Is.EqualTo(existing.UUID));
        }

        /// <summary>
        /// Validates: Requirement 6
        /// Protected fields (UUID, OwnerUUID, NickName, CopyCost, Description,
        /// Manufacture Run Time, Power Required) preserved on update
        /// </summary>
        [Test]
        public void Import_Update_PreservesProtectedFields()
        {
            var existing = new BpModel("AMX-SS Reactor Core");
            existing.UUID = "original-uuid-123";
            existing.OwnerUUID = "original-owner-456";
            existing.NickName = "My Reactor";
            existing.CopyCost = 5000;
            existing.Description = "A fine reactor";
            existing.BluePrintType = "Reactor";
            existing.Evolution = 0;
            existing.Class = 1;
            existing.TechLevel = null;
            existing.Properties.setProperty("Health", "50");
            existing.Properties.setProperty("Manufacture Run Time", "3600");
            existing.Properties.setProperty("Power Required", "100");
            empireContext.globalBlueprintList.Add(existing);

            // Import with matching dedup key — incoming does NOT have protected properties
            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("AMX-SS Reactor Core", "Government", "Reactor", 0, 1, null,
                    new Dictionary<string, string> { { "Health", "200" }, { "Damage", "50" } })
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            var updated = empireContext.globalBlueprintList[0];
            Assert.That(result.UpdatedCount, Is.EqualTo(1));
            // Protected scalar fields
            Assert.That(updated.UUID, Is.EqualTo("original-uuid-123"));
            Assert.That(updated.OwnerUUID, Is.EqualTo("original-owner-456"));
            Assert.That(updated.NickName, Is.EqualTo("My Reactor"));
            Assert.That(updated.CopyCost, Is.EqualTo(5000));
            Assert.That(updated.Description, Is.EqualTo("A fine reactor"));
            // Protected property-bag fields restored
            string mrt;
            updated.Properties.getString("Manufacture Run Time", null, out mrt);
            Assert.That(mrt, Is.EqualTo("3600"));
            string pr;
            updated.Properties.getString("Power Required", null, out pr);
            Assert.That(pr, Is.EqualTo("100"));
            // Non-protected property updated
            string health;
            updated.Properties.getString("Health", null, out health);
            Assert.That(health, Is.EqualTo("200"));
        }

        /// <summary>
        /// Validates: Requirement 8
        /// Import result counts match expectations
        /// </summary>
        [Test]
        public void Import_ResultCounts_MatchExpectations()
        {
            // Pre-populate one existing global blueprint for update
            var existing = new BpModel("Existing Reactor");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BluePrintType = "Reactor";
            existing.Evolution = 0;
            existing.Class = 1;
            existing.Properties.setProperty("Health", "50");
            empireContext.globalBlueprintList.Add(existing);

            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("New Reactor", "Government"),                          // created
                MakeMarketBlueprint("Existing Reactor", "Government", "Reactor", 0, 1),    // updated
                MakeUnexpandedBlueprint("Collapsed Item", "Government"),                   // skipped
                MakeMarketBlueprint("Player Weapon", "SomePlayer", "Weapon"),              // created (player)
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(result.CreatedCount, Is.EqualTo(2));
            Assert.That(result.UpdatedCount, Is.EqualTo(1));
            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(result.Entries.Count, Is.EqualTo(4));
        }

        /// <summary>
        /// Validates: Requirements 3, 7
        /// Government → global, other → player
        /// </summary>
        [Test]
        public void Import_RoutingCorrectness_GovernmentGlobal_OtherPlayer()
        {
            var list = new List<MarketBlueprint>
            {
                MakeMarketBlueprint("Gov Reactor", "Government", "Reactor"),
                MakeMarketBlueprint("Player Reactor", "TraderJoe", "Reactor", 0, 2),
            };

            var result = MarketBlueprintImporter.Import(list, playerContext, empireContext);

            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(1));
            Assert.That(empireContext.globalBlueprintList[0].Name, Is.EqualTo("Gov Reactor"));

            Assert.That(playerContext.blueprintList.Count, Is.EqualTo(1));
            Assert.That(playerContext.blueprintList[0].Name, Is.EqualTo("Player Reactor"));
            Assert.That(playerContext.blueprintList[0].OwnerUUID, Is.EqualTo(TestPlayerUUID));

            Assert.That(result.Entries[0].Storage, Is.EqualTo("Global"));
            Assert.That(result.Entries[1].Storage, Is.EqualTo("Player"));
        }

        // -----------------------------------------------------------------------
        // Property-Based Tests
        // -----------------------------------------------------------------------

        private static readonly Random Rng = new Random(42);

        private static readonly string[] BpTypes = { "Reactor", "Weapon", "Shield", "Hull", "MainDrive" };
        private static readonly string[] TechLevels = { null, "Hi-Tech", "Junker", "MilSpec", "Rugged", "Service", "Standard" };
        private static readonly string[] Sellers = { "Government", "TraderJoe", "SpacePirate", "MerchantGuild" };

        private MarketBlueprint MakeRandomMarketBlueprint(int seed)
        {
            var rng = new Random(seed);
            string name = "BP_" + rng.Next(1, 20);
            string seller = Sellers[rng.Next(Sellers.Length)];
            string bpType = BpTypes[rng.Next(BpTypes.Length)];
            int evolution = rng.Next(0, 4);
            int cls = rng.Next(1, 6);
            string techLevel = TechLevels[rng.Next(TechLevels.Length)];

            var props = new Dictionary<string, string>();
            int propCount = rng.Next(1, 5);
            for (int i = 0; i < propCount; i++)
                props["Prop" + i] = "" + rng.Next(1, 1000);

            return MakeMarketBlueprint(name, seller, bpType, evolution, cls, techLevel, props);
        }

        /// <summary>
        /// **Validates: Requirements 5, 6**
        /// Property 1: Dedup key uniqueness
        /// For any set of market blueprints, if two have the same dedup key,
        /// only one record should exist in the target storage after import.
        /// </summary>
        [Test]
        public void Property_DedupKeyUniqueness()
        {
            for (int trial = 0; trial < 50; trial++)
            {
                // Reset state for each trial
                empireContext.globalBlueprintList.Clear();
                playerContext.blueprintList.Clear();
                playerContext.CurrentPlayerUUID = TestPlayerUUID;

                var rng = new Random(trial * 7);
                int count = rng.Next(2, 10);
                var blueprints = new List<MarketBlueprint>();

                for (int i = 0; i < count; i++)
                    blueprints.Add(MakeRandomMarketBlueprint(trial * 1000 + i));

                // Add some intentional duplicates
                if (blueprints.Count >= 2)
                {
                    var dup = MakeMarketBlueprint(
                        blueprints[0].Blueprint.Name,
                        blueprints[0].SellerName,
                        blueprints[0].Blueprint.BluePrintType,
                        blueprints[0].Blueprint.Evolution,
                        blueprints[0].Blueprint.Class,
                        blueprints[0].Blueprint.TechLevel,
                        new Dictionary<string, string> { { "Health", "999" } });
                    blueprints.Add(dup);
                }

                MarketBlueprintImporter.Import(blueprints, playerContext, empireContext);

                // Verify: no two blueprints in the same storage share a dedup key
                AssertNoDuplicateKeys(empireContext.globalBlueprintList, "global", trial);
                AssertNoDuplicateKeys(playerContext.blueprintList, "player", trial);
            }
        }

        private void AssertNoDuplicateKeys(BindingList<BpModel> list, string storageName, int trial)
        {
            var keys = list.Select(bp => $"{bp.Name}|{bp.Evolution}|{bp.BluePrintType}|{bp.Class}|{bp.TechLevel}").ToList();
            var distinct = keys.Distinct().ToList();
            Assert.That(keys.Count, Is.EqualTo(distinct.Count),
                $"Duplicate dedup keys found in {storageName} storage on trial {trial}: " +
                string.Join(", ", keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key)));
        }

        /// <summary>
        /// **Validates: Requirements 3, 7**
        /// Property 2: Routing correctness
        /// Government seller → globalBlueprintList, other → blueprintList with OwnerUUID set
        /// </summary>
        [Test]
        public void Property_RoutingCorrectness()
        {
            for (int trial = 0; trial < 50; trial++)
            {
                empireContext.globalBlueprintList.Clear();
                playerContext.blueprintList.Clear();
                playerContext.CurrentPlayerUUID = TestPlayerUUID;

                var rng = new Random(trial * 13);
                int count = rng.Next(1, 8);
                var blueprints = new List<MarketBlueprint>();

                for (int i = 0; i < count; i++)
                    blueprints.Add(MakeRandomMarketBlueprint(trial * 2000 + i));

                var result = MarketBlueprintImporter.Import(blueprints, playerContext, empireContext);

                foreach (var entry in result.Entries)
                {
                    if (entry.Action == ImportAction.Skipped) continue;

                    bool isGov = string.Equals(entry.SellerName, "Government", StringComparison.OrdinalIgnoreCase);

                    if (isGov)
                    {
                        Assert.That(entry.Storage, Is.EqualTo("Global"),
                            $"Trial {trial}: Government seller '{entry.Name}' should route to Global");
                        var found = empireContext.globalBlueprintList.FirstOrDefault(b => b.UUID == entry.UUID);
                        Assert.That(found, Is.Not.Null,
                            $"Trial {trial}: Government blueprint '{entry.Name}' not found in globalBlueprintList");
                    }
                    else
                    {
                        Assert.That(entry.Storage, Is.EqualTo("Player"),
                            $"Trial {trial}: Non-Government seller '{entry.Name}' should route to Player");
                        var found = playerContext.blueprintList.FirstOrDefault(b => b.UUID == entry.UUID);
                        Assert.That(found, Is.Not.Null,
                            $"Trial {trial}: Player blueprint '{entry.Name}' not found in blueprintList");
                        Assert.That(found.OwnerUUID, Is.EqualTo(TestPlayerUUID),
                            $"Trial {trial}: Player blueprint '{entry.Name}' should have OwnerUUID set");
                    }
                }
            }
        }

        /// <summary>
        /// **Validates: Requirement 6**
        /// Property 3: Protected field preservation
        /// UUID, OwnerUUID, NickName, CopyCost, Description, Manufacture Run Time, Power Required
        /// must be unchanged after update.
        /// </summary>
        [Test]
        public void Property_ProtectedFieldPreservation()
        {
            for (int trial = 0; trial < 50; trial++)
            {
                empireContext.globalBlueprintList.Clear();
                playerContext.CurrentPlayerUUID = TestPlayerUUID;

                var rng = new Random(trial * 17);
                string name = "ProtBP_" + rng.Next(1, 100);
                string bpType = BpTypes[rng.Next(BpTypes.Length)];
                int evolution = rng.Next(0, 4);
                int cls = rng.Next(1, 6);
                string techLevel = TechLevels[rng.Next(TechLevels.Length)];

                // Create existing blueprint with protected fields set
                var existing = new BpModel(name);
                existing.UUID = "protected-uuid-" + trial;
                existing.OwnerUUID = "protected-owner-" + trial;
                existing.NickName = "Nick" + trial;
                existing.CopyCost = 1000 + trial;
                existing.Description = "Desc" + trial;
                existing.BluePrintType = bpType;
                existing.Evolution = evolution;
                existing.Class = cls;
                existing.TechLevel = techLevel;
                existing.Properties.setProperty("Health", "50");
                existing.Properties.setProperty("Manufacture Run Time", "MRT_" + trial);
                existing.Properties.setProperty("Power Required", "PR_" + trial);
                empireContext.globalBlueprintList.Add(existing);

                // Import with matching dedup key — incoming has different non-protected props
                var incoming = MakeMarketBlueprint(name, "Government", bpType, evolution, cls, techLevel,
                    new Dictionary<string, string> { { "Health", "999" }, { "Damage", "42" } });

                MarketBlueprintImporter.Import(
                    new List<MarketBlueprint> { incoming }, playerContext, empireContext);

                var updated = empireContext.globalBlueprintList[0];

                Assert.That(updated.UUID, Is.EqualTo("protected-uuid-" + trial),
                    $"Trial {trial}: UUID must be preserved");
                Assert.That(updated.OwnerUUID, Is.EqualTo("protected-owner-" + trial),
                    $"Trial {trial}: OwnerUUID must be preserved");
                Assert.That(updated.NickName, Is.EqualTo("Nick" + trial),
                    $"Trial {trial}: NickName must be preserved");
                Assert.That(updated.CopyCost, Is.EqualTo(1000 + trial),
                    $"Trial {trial}: CopyCost must be preserved");
                Assert.That(updated.Description, Is.EqualTo("Desc" + trial),
                    $"Trial {trial}: Description must be preserved");

                string mrt;
                updated.Properties.getString("Manufacture Run Time", null, out mrt);
                Assert.That(mrt, Is.EqualTo("MRT_" + trial),
                    $"Trial {trial}: Manufacture Run Time must be preserved");

                string pr;
                updated.Properties.getString("Power Required", null, out pr);
                Assert.That(pr, Is.EqualTo("PR_" + trial),
                    $"Trial {trial}: Power Required must be preserved");
            }
        }

        /// <summary>
        /// **Validates: Requirements 8, 10**
        /// Property 4: Import result completeness
        /// created + updated + skipped == total input count
        /// </summary>
        [Test]
        public void Property_ImportResultCompleteness()
        {
            for (int trial = 0; trial < 50; trial++)
            {
                empireContext.globalBlueprintList.Clear();
                playerContext.blueprintList.Clear();
                playerContext.CurrentPlayerUUID = TestPlayerUUID;

                var rng = new Random(trial * 23);
                int count = rng.Next(1, 12);
                var blueprints = new List<MarketBlueprint>();

                for (int i = 0; i < count; i++)
                {
                    // Mix in some unexpanded listings
                    if (rng.Next(4) == 0)
                        blueprints.Add(MakeUnexpandedBlueprint("Unexpanded_" + i, Sellers[rng.Next(Sellers.Length)]));
                    else
                        blueprints.Add(MakeRandomMarketBlueprint(trial * 3000 + i));
                }

                var result = MarketBlueprintImporter.Import(blueprints, playerContext, empireContext);

                int total = result.CreatedCount + result.UpdatedCount + result.SkippedCount;
                Assert.That(total, Is.EqualTo(blueprints.Count),
                    $"Trial {trial}: created({result.CreatedCount}) + updated({result.UpdatedCount}) + skipped({result.SkippedCount}) = {total} != input count {blueprints.Count}");
                Assert.That(result.Entries.Count, Is.EqualTo(blueprints.Count),
                    $"Trial {trial}: Entries count should match input count");
            }
        }

        // -----------------------------------------------------------------------
        // Integration Tests — Idempotency with real MarketSample HTML files
        // -----------------------------------------------------------------------

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }

        private List<MarketBlueprint> ParseHtml(string html)
        {
            var scanner = new BlueprintScanner();
            return scanner.ProcessMarketHtml(html);
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// Import MarketSampleReactor.html once → verify blueprints created with correct count
        /// </summary>
        [Test]
        public void Integration_ReactorImportOnce_CreatesExpectedBlueprints()
        {
            string html = LoadTestData("MarketSampleReactor.html");
            var parsed = ParseHtml(html);

            var result = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            Assert.That(result.CreatedCount, Is.GreaterThan(0),
                "Should create at least one blueprint from reactor sample");
            // Created + Updated + Skipped should account for all entries
            Assert.That(result.CreatedCount + result.UpdatedCount + result.SkippedCount,
                Is.EqualTo(parsed.Count),
                "Total result entries should match parsed count");
            // Total blueprints across both storages should match created count
            int totalStored = empireContext.globalBlueprintList.Count + playerContext.blueprintList.Count;
            Assert.That(totalStored, Is.EqualTo(result.CreatedCount),
                "Total stored blueprints should match created count (first import, no prior data)");

            // Every stored blueprint should have properties and a name
            foreach (var bp in empireContext.globalBlueprintList.Concat(playerContext.blueprintList))
            {
                Assert.That(bp.Name, Is.Not.Null.And.Not.Empty,
                    "Each blueprint should have a name");
                Assert.That(bp.Properties.Count, Is.GreaterThan(0),
                    $"Blueprint '{bp.Name}' should have properties");
            }
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// Import same reactor file 3 times → verify no duplicate blueprints,
        /// all "Updated" on 2nd/3rd import
        /// </summary>
        [Test]
        public void Integration_ReactorImport3Times_NoDuplicates_AllUpdatedOnReimport()
        {
            string html = LoadTestData("MarketSampleReactor.html");
            var parsed = ParseHtml(html);

            // First import
            var result1 = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);
            int countAfterFirst = empireContext.globalBlueprintList.Count;
            int createdFirst = result1.CreatedCount;

            Assert.That(createdFirst, Is.GreaterThan(0), "First import should create blueprints");

            // Second import — re-parse to get fresh objects
            parsed = ParseHtml(html);
            var result2 = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(countAfterFirst),
                "Blueprint count should not change after second import");
            Assert.That(result2.CreatedCount, Is.EqualTo(0),
                "Second import should create zero new blueprints");
            Assert.That(result2.UpdatedCount + result2.SkippedCount, Is.EqualTo(parsed.Count),
                "All entries on second import should be Updated or Skipped");
            // All non-skipped entries should be Updated
            foreach (var entry in result2.Entries.Where(e => e.Action != ImportAction.Skipped))
            {
                Assert.That(entry.Action, Is.EqualTo(ImportAction.Updated),
                    $"Blueprint '{entry.Name}' should be Updated on second import, was {entry.Action}");
            }

            // Third import
            parsed = ParseHtml(html);
            var result3 = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(countAfterFirst),
                "Blueprint count should not change after third import");
            Assert.That(result3.CreatedCount, Is.EqualTo(0),
                "Third import should create zero new blueprints");
            foreach (var entry in result3.Entries.Where(e => e.Action != ImportAction.Skipped))
            {
                Assert.That(entry.Action, Is.EqualTo(ImportAction.Updated),
                    $"Blueprint '{entry.Name}' should be Updated on third import, was {entry.Action}");
            }
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// After 3 imports, each blueprint's property count unchanged from first import
        /// </summary>
        [Test]
        public void Integration_ReactorImport3Times_PropertyCountsStable()
        {
            string html = LoadTestData("MarketSampleReactor.html");

            // First import
            var parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Snapshot property counts after first import
            var propCountsAfterFirst = empireContext.globalBlueprintList
                .ToDictionary(bp => bp.UUID, bp => bp.Properties.Count);

            // Second import
            parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Third import
            parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Verify property counts unchanged
            foreach (var bp in empireContext.globalBlueprintList)
            {
                Assert.That(propCountsAfterFirst.ContainsKey(bp.UUID), Is.True,
                    $"Blueprint '{bp.Name}' UUID should be stable across imports");
                Assert.That(bp.Properties.Count, Is.EqualTo(propCountsAfterFirst[bp.UUID]),
                    $"Blueprint '{bp.Name}' property count changed from {propCountsAfterFirst[bp.UUID]} to {bp.Properties.Count} after 3 imports");
            }
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// After 3 imports, each blueprint's resource count unchanged from first import
        /// </summary>
        [Test]
        public void Integration_ReactorImport3Times_ResourceCountsStable()
        {
            string html = LoadTestData("MarketSampleReactor.html");

            // First import
            var parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Snapshot resource counts after first import
            var resCountsAfterFirst = empireContext.globalBlueprintList
                .ToDictionary(bp => bp.UUID, bp => bp.Resources.Count);

            // Second import
            parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Third import
            parsed = ParseHtml(html);
            MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Verify resource counts unchanged
            foreach (var bp in empireContext.globalBlueprintList)
            {
                Assert.That(resCountsAfterFirst.ContainsKey(bp.UUID), Is.True,
                    $"Blueprint '{bp.Name}' UUID should be stable across imports");
                Assert.That(bp.Resources.Count, Is.EqualTo(resCountsAfterFirst[bp.UUID]),
                    $"Blueprint '{bp.Name}' resource count changed from {resCountsAfterFirst[bp.UUID]} to {bp.Resources.Count} after 3 imports");
            }
        }

        /// <summary>
        /// Validates: Requirements 5, 6
        /// Import MarketSampleAllWeaponTypes.html 2 times → verify weapon blueprints not duplicated
        /// </summary>
        [Test]
        public void Integration_WeaponTypesImport2Times_NoDuplicates()
        {
            string html = LoadTestData("MarketSampleAllWeaponTypes.html");

            // First import
            var parsed = ParseHtml(html);
            var result1 = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);
            int countAfterFirst = empireContext.globalBlueprintList.Count;

            Assert.That(result1.CreatedCount, Is.GreaterThan(0),
                "First weapon import should create blueprints");

            // Second import
            parsed = ParseHtml(html);
            var result2 = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            Assert.That(empireContext.globalBlueprintList.Count, Is.EqualTo(countAfterFirst),
                "Weapon blueprint count should not change after second import");
            Assert.That(result2.CreatedCount, Is.EqualTo(0),
                "Second weapon import should create zero new blueprints");
            foreach (var entry in result2.Entries.Where(e => e.Action != ImportAction.Skipped))
            {
                Assert.That(entry.Action, Is.EqualTo(ImportAction.Updated),
                    $"Weapon blueprint '{entry.Name}' should be Updated on second import, was {entry.Action}");
            }

            // Verify no duplicate dedup keys
            var keys = empireContext.globalBlueprintList
                .Select(bp => $"{bp.Name}|{bp.Evolution}|{bp.BluePrintType}|{bp.Class}|{bp.TechLevel}")
                .ToList();
            var distinct = keys.Distinct().ToList();
            Assert.That(keys.Count, Is.EqualTo(distinct.Count),
                "No duplicate dedup keys should exist after 2 weapon imports: " +
                string.Join(", ", keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key)));
        }

        /// <summary>
        /// **Validates: Requirements 5**
        /// Property 5: TechLevel extraction (verified via real HTML names)
        /// For any blueprint name ending with a parenthesized known TechLevel value,
        /// the TechLevel field must be set to that value and the parenthesized portion
        /// must be stripped from the Name.
        /// </summary>
        [Test]
        public void Property_TechLevelExtraction_RealHtml()
        {
            // Known TechLevel values
            var knownTechLevels = new HashSet<string>
                { "Hi-Tech", "Junker", "MilSpec", "Rugged", "Service", "Standard" };

            // Parse all MarketSample files and verify TechLevel extraction
            string baseDir = TestContext.CurrentContext.TestDirectory;
            var sampleFiles = Directory.GetFiles(
                Path.Combine(baseDir, "TestData"), "MarketSample*.html");

            Assert.That(sampleFiles.Length, Is.GreaterThan(0), "No MarketSample files found");

            int techLevelCount = 0;
            int noTechLevelCount = 0;

            foreach (var file in sampleFiles)
            {
                string html = File.ReadAllText(file);
                var parsed = ParseHtml(html);

                foreach (var mb in parsed)
                {
                    var bp = mb.Blueprint;

                    if (bp.TechLevel != null)
                    {
                        // TechLevel should be a known value
                        Assert.That(knownTechLevels.Contains(bp.TechLevel), Is.True,
                            $"Blueprint '{bp.Name}' has unknown TechLevel '{bp.TechLevel}' in {Path.GetFileName(file)}");
                        // Name should NOT contain the TechLevel in parentheses
                        Assert.That(bp.Name, Does.Not.EndWith($"({bp.TechLevel})"),
                            $"Blueprint name '{bp.Name}' should have TechLevel stripped in {Path.GetFileName(file)}");
                        techLevelCount++;
                    }
                    else
                    {
                        // Name should not end with a known TechLevel in parentheses
                        foreach (var tl in knownTechLevels)
                        {
                            Assert.That(bp.Name, Does.Not.EndWith($"({tl})"),
                                $"Blueprint '{bp.Name}' should have TechLevel '{tl}' extracted in {Path.GetFileName(file)}");
                        }
                        noTechLevelCount++;
                    }
                }
            }

            // Sanity: we should find at least some blueprints with TechLevel
            Assert.That(techLevelCount, Is.GreaterThan(0),
                "Should find at least one blueprint with a known TechLevel across all sample files");
            Assert.That(noTechLevelCount, Is.GreaterThan(0),
                "Should find at least one blueprint without TechLevel across all sample files");

            TestContext.WriteLine($"TechLevel extraction: {techLevelCount} with TechLevel, {noTechLevelCount} without");
        }

        /// <summary>
        /// **Validates: Requirements 5, 6**
        /// Property 6: Import idempotency
        /// For any market HTML input, importing the same HTML N times (N >= 2) must produce
        /// the same number of blueprint records in storage as importing it once.
        /// No duplicate blueprints should be created. On the second and subsequent imports,
        /// all entries should report as "Updated" (not "Created"), and the blueprint's
        /// property count and resource count must remain unchanged.
        /// </summary>
        [Test]
        public void Property_ImportIdempotency_AllSampleFiles()
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            var sampleFiles = Directory.GetFiles(
                Path.Combine(baseDir, "TestData"), "MarketSample*.html");

            Assert.That(sampleFiles.Length, Is.GreaterThan(0), "No MarketSample files found");

            foreach (var file in sampleFiles)
            {
                // Reset state for each file
                empireContext.globalBlueprintList.Clear();
                playerContext.blueprintList.Clear();
                playerContext.CurrentPlayerUUID = TestPlayerUUID;

                string fileName = Path.GetFileName(file);
                string html = File.ReadAllText(file);

                // First import
                var parsed1 = ParseHtml(html);
                var result1 = MarketBlueprintImporter.Import(parsed1, playerContext, empireContext);
                int countAfterFirst = empireContext.globalBlueprintList.Count + playerContext.blueprintList.Count;

                // Snapshot property and resource counts
                var propCounts = new Dictionary<string, int>();
                var resCounts = new Dictionary<string, int>();
                foreach (var bp in empireContext.globalBlueprintList)
                {
                    propCounts[bp.UUID] = bp.Properties.Count;
                    resCounts[bp.UUID] = bp.Resources.Count;
                }
                foreach (var bp in playerContext.blueprintList)
                {
                    propCounts[bp.UUID] = bp.Properties.Count;
                    resCounts[bp.UUID] = bp.Resources.Count;
                }

                // Second import
                var parsed2 = ParseHtml(html);
                var result2 = MarketBlueprintImporter.Import(parsed2, playerContext, empireContext);
                int countAfterSecond = empireContext.globalBlueprintList.Count + playerContext.blueprintList.Count;

                Assert.That(countAfterSecond, Is.EqualTo(countAfterFirst),
                    $"[{fileName}] Blueprint count changed from {countAfterFirst} to {countAfterSecond} after second import");
                Assert.That(result2.CreatedCount, Is.EqualTo(0),
                    $"[{fileName}] Second import should create zero new blueprints, created {result2.CreatedCount}");

                // All non-skipped entries should be Updated
                foreach (var entry in result2.Entries.Where(e => e.Action != ImportAction.Skipped))
                {
                    Assert.That(entry.Action, Is.EqualTo(ImportAction.Updated),
                        $"[{fileName}] Blueprint '{entry.Name}' should be Updated on second import, was {entry.Action}");
                }

                // Verify property and resource counts stable
                foreach (var bp in empireContext.globalBlueprintList.Concat(playerContext.blueprintList))
                {
                    if (propCounts.ContainsKey(bp.UUID))
                    {
                        Assert.That(bp.Properties.Count, Is.EqualTo(propCounts[bp.UUID]),
                            $"[{fileName}] Blueprint '{bp.Name}' property count changed after second import");
                        Assert.That(bp.Resources.Count, Is.EqualTo(resCounts[bp.UUID]),
                            $"[{fileName}] Blueprint '{bp.Name}' resource count changed after second import");
                    }
                }

                // Verify no duplicate dedup keys in either storage
                AssertNoDuplicateKeys(empireContext.globalBlueprintList, $"global ({fileName})", 0);
                AssertNoDuplicateKeys(playerContext.blueprintList, $"player ({fileName})", 0);
            }
        }
    }
}
