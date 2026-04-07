using NUnit.Framework;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string baselineDataPath = System.IO.Path.Combine(baseDir, @"..\..\..\OE2EmpireTracker\BaselineData.json");
            EmpireContext.FilePath = System.IO.Path.GetFullPath(baselineDataPath);

            string playerDataPath = System.IO.Path.Combine(baseDir, @"..\..\..\OE2EmpireTracker\PlayerData.json");
            PlayerContext.FilePath = System.IO.Path.GetFullPath(playerDataPath);
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
    }
}
