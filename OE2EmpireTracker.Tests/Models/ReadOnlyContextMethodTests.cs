using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using static OE2EmpireTracker.Tests.Models.ReadOnlyWrapperGenerators;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Property-based tests for PlayerContext/EmpireContext read-only list methods.
    /// Feature: readonly-data-wrappers, Properties 5 and 6
    /// </summary>
    [TestFixture]
    public class ReadOnlyContextMethodTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OE2Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var playerJson = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(playerJson,
                "{\"DataVersion\":7, \"CurrentPlayerUUID\":\"\", " +
                "\"PlayerProfile\":[], \"Blueprint\":[], \"Survey\":[], \"Colony\":[], " +
                "\"DeliveryRoute\":[], \"DeliveryPlan\":[], \"PricingPlan\":[], " +
                "\"BuildPlan\":[], \"ShipTemplate\":[], \"Ship\":[], \"Station\":[], " +
                "\"MarketListing\":[], \"MarketTransaction\":[], \"StockPlan\":[], " +
                "\"StockProfile\":[], \"SupplyChain\":[], \"WarehouseOverflowRule\":[], " +
                "\"Faction\":[], \"ExternalCharacter\":[], \"Asteroid\":[]}");

            var baselineJson = Path.Combine(_tempDir, "BaselineData.json");
            File.WriteAllText(baselineJson,
                "{\"BlueprintType\":[], \"ShipClass\":[], \"TechLevel\":[], " +
                "\"Evolution\":[], \"Resource\":[], \"ResourceGroup\":[], " +
                "\"Commodity\":[], \"GlobalBlueprint\":[]}");

            MigrationRunner.SuppressUI = true;
            EmpireContext.Reset();
            PlayerContext.Reset();
            EmpireContext.FilePath = baselineJson;
            PlayerContext.FilePath = playerJson;
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
            }
        }

        private PlayerContext FreshPlayerContext()
        {
            PlayerContext.Reset();
            return PlayerContext.GetInstance();
        }

        // ---------------------------------------------------------------
        // Property 5: GetReadOnly List Preservation
        // Populate PlayerContext backing lists with random entities, call
        // GetReadOnly*List(), verify same count and UUID-wise match at
        // each position.
        // **Validates: Requirements 7.1-7.21, 8.1-8.6**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetReadOnlyBlueprintList_PreservesCountAndUUIDs(OE2EmpireTracker.Models.Blueprint[] blueprints)
        {
            var pc = FreshPlayerContext();
            var toAdd = blueprints.Take(5).ToArray();
            foreach (var bp in toAdd)
                pc.AddBlueprint(bp);

            var roList = pc.GetReadOnlyBlueprintList();
            bool countMatch = roList.Count == pc.BlueprintList.Count;
            bool uuidsMatch = roList.Select(r => r.UUID)
                .SequenceEqual(pc.BlueprintList.Select(b => b.UUID));
            return (countMatch && uuidsMatch)
                .Label($"count: {roList.Count} vs {pc.BlueprintList.Count}");
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetReadOnlyColonyList_PreservesCountAndUUIDs(Colony[] colonies)
        {
            var pc = FreshPlayerContext();
            var toAdd = colonies.Take(5).ToArray();
            foreach (var c in toAdd)
                pc.AddColony(c);

            var roList = pc.GetReadOnlyColonyList();
            bool countMatch = roList.Count == pc.ColonyList.Count;
            bool uuidsMatch = roList.Select(r => r.UUID)
                .SequenceEqual(pc.ColonyList.Select(c => c.UUID));
            return (countMatch && uuidsMatch)
                .Label($"count: {roList.Count} vs {pc.ColonyList.Count}");
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetReadOnlySurveyList_PreservesCountAndUUIDs(Survey[] surveys)
        {
            var pc = FreshPlayerContext();
            var toAdd = surveys.Take(5).ToArray();
            foreach (var s in toAdd)
                pc.AddSurvey(s);

            var roList = pc.GetReadOnlySurveyList();
            bool countMatch = roList.Count == pc.SurveyList.Count;
            bool uuidsMatch = roList.Select(r => r.UUID)
                .SequenceEqual(pc.SurveyList.Select(s => s.UUID));
            return (countMatch && uuidsMatch)
                .Label($"count: {roList.Count} vs {pc.SurveyList.Count}");
        }

        // ---------------------------------------------------------------
        // Property 6: Current-Player Filtering
        // Populate backing list with entities having mixed OwnerUUIDs,
        // set CurrentPlayerUUID, call GetCurrentPlayerReadOnly*(), verify
        // only matching entities returned and all are wrapped.
        // **Validates: Requirements 9.1-9.17**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetCurrentPlayerReadOnlyColonies_FiltersCorrectly(Colony[] colonies)
        {
            var pc = FreshPlayerContext();
            var playerUUID = Guid.NewGuid().ToString();
            var otherUUID = Guid.NewGuid().ToString();

            var toAdd = colonies.Take(5).ToArray();
            for (int i = 0; i < toAdd.Length; i++)
            {
                toAdd[i].OwnerUUID = (i % 2 == 0) ? playerUUID : otherUUID;
                pc.AddColony(toAdd[i]);
            }

            pc.CurrentPlayerUUID = playerUUID;
            var roList = pc.GetCurrentPlayerReadOnlyColonies();

            int expectedCount = toAdd.Count(c => c.OwnerUUID == playerUUID);
            bool countMatch = roList.Count == expectedCount;
            bool allOwned = roList.All(r => r.OwnerUUID == playerUUID);
            bool noOther = roList.All(r => r.OwnerUUID != otherUUID);

            return (countMatch && allOwned && noOther)
                .Label($"expected {expectedCount}, got {roList.Count}, allOwned={allOwned}");
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetCurrentPlayerReadOnlyBlueprints_FiltersCorrectly(OE2EmpireTracker.Models.Blueprint[] blueprints)
        {
            var pc = FreshPlayerContext();
            var playerUUID = Guid.NewGuid().ToString();
            var otherUUID = Guid.NewGuid().ToString();

            var toAdd = blueprints.Take(5).ToArray();
            for (int i = 0; i < toAdd.Length; i++)
            {
                toAdd[i].OwnerUUID = (i % 2 == 0) ? playerUUID : otherUUID;
                pc.AddBlueprint(toAdd[i]);
            }

            pc.CurrentPlayerUUID = playerUUID;
            var roList = pc.GetCurrentPlayerReadOnlyBlueprints();

            int expectedCount = toAdd.Count(b => b.OwnerUUID == playerUUID);
            bool countMatch = roList.Count == expectedCount;
            bool allOwned = roList.All(r => r.OwnerUUID == playerUUID);
            bool noOther = roList.All(r => r.OwnerUUID != otherUUID);

            return (countMatch && allOwned && noOther)
                .Label($"expected {expectedCount}, got {roList.Count}, allOwned={allOwned}");
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property GetCurrentPlayerReadOnlySurveys_FiltersCorrectly(Survey[] surveys)
        {
            var pc = FreshPlayerContext();
            var playerUUID = Guid.NewGuid().ToString();
            var otherUUID = Guid.NewGuid().ToString();

            var toAdd = surveys.Take(5).ToArray();
            for (int i = 0; i < toAdd.Length; i++)
            {
                toAdd[i].OwnerUUID = (i % 2 == 0) ? playerUUID : otherUUID;
                pc.AddSurvey(toAdd[i]);
            }

            pc.CurrentPlayerUUID = playerUUID;
            var roList = pc.GetCurrentPlayerReadOnlySurveys();

            int expectedCount = toAdd.Count(s => s.OwnerUUID == playerUUID);
            bool countMatch = roList.Count == expectedCount;
            bool allOwned = roList.All(r => r.OwnerUUID == playerUUID);
            bool noOther = roList.All(r => r.OwnerUUID != otherUUID);

            return (countMatch && allOwned && noOther)
                .Label($"expected {expectedCount}, got {roList.Count}, allOwned={allOwned}");
        }
    }
}