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
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for read-only list encapsulation.
    /// Feature: readonly-list-encapsulation
    /// </summary>
    [TestFixture]
    public class ReadOnlyListEncapsulationPropertyTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OE2Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var playerJson = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(playerJson, "{\"DataVersion\":7,\"CurrentPlayerUUID\":\"\",\"PlayerProfile\":[],\"Blueprint\":[],\"Survey\":[],\"Colony\":[],\"DeliveryRoute\":[],\"DeliveryPlan\":[],\"PricingPlan\":[],\"BuildPlan\":[],\"ShipTemplate\":[],\"Ship\":[],\"Station\":[],\"MarketListing\":[],\"MarketTransaction\":[],\"StockPlan\":[],\"StockProfile\":[],\"SupplyChain\":[],\"WarehouseOverflowRule\":[],\"Faction\":[],\"ExternalCharacter\":[],\"Asteroid\":[]}");

            var baselineJson = Path.Combine(_tempDir, "BaselineData.json");
            File.WriteAllText(baselineJson, "{\"BlueprintType\":[],\"ShipClass\":[],\"TechLevel\":[],\"Evolution\":[],\"Resource\":[],\"ResourceGroup\":[],\"Commodity\":[],\"GlobalBlueprint\":[]}");

            MigrationRunner.SuppressUI = true;
            EmpireContext.Reset();
            EmpireContext.FilePath = baselineJson;
            PlayerContext.FilePath = playerJson;
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        private PlayerContext FreshPlayerContext()
        {
            PlayerContext.Reset();
            return PlayerContext.GetInstance();
        }

        #region Property 1: Add-then-Find round trip

        // Feature: readonly-list-encapsulation, Property 1: Add-then-Find round trip
        /// <summary>
        /// For Blueprint, Survey, Colony, Station â€” add entity via mutation method,
        /// Find by UUID, assert same instance returned.
        /// **Validates: Requirements 3.5, 4.3, 5.1, 5.5, 6.1, 6.2, 6.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddBlueprint_ThenFindByUUID_ReturnsSameInstance()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var bp = new Bp { UUID = id, Name = "TestBP" };
                ctx.AddBlueprint(bp);
                var found = ctx.FindBlueprint(id);
                return (found == bp)
                    .Label($"FindBlueprint('{id}') returned same instance: {found == bp}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddSurvey_ThenFindByUUID_ReturnsSameInstance()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var survey = new Survey { UUID = id, PlanetName = "TestPlanet" };
                ctx.AddSurvey(survey);
                var found = ctx.FindSurvey(id);
                return (found == survey)
                    .Label($"FindSurvey('{id}') returned same instance: {found == survey}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddColony_ThenFindByUUID_ReturnsSameInstance()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var colony = new Colony { UUID = id, ColonyName = "TestColony" };
                ctx.AddColony(colony);
                var found = ctx.FindColony(id);
                return (found == colony)
                    .Label($"FindColony('{id}') returned same instance: {found == colony}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddStation_ThenFindByUUID_ReturnsSameInstance()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var station = new Station { UUID = id, Name = "TestStation" };
                ctx.AddStation(station);
                var found = ctx.FindStation(id);
                return (found == station)
                    .Label($"FindStation('{id}') returned same instance: {found == station}");
            });
        }

        #endregion

        #region Property 2: Remove-then-Find returns null

        // Feature: readonly-list-encapsulation, Property 2: Remove-then-Find returns null
        /// <summary>
        /// Add entity, remove it, Find by UUID, assert null returned.
        /// **Validates: Requirements 3.6, 4.4, 5.2, 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveBlueprint_ThenFindByUUID_ReturnsNull()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var bp = new Bp { UUID = id, Name = "TestBP" };
                ctx.AddBlueprint(bp);
                ctx.RemoveBlueprint(bp);
                var found = ctx.FindBlueprint(id);
                return (found == null)
                    .Label($"FindBlueprint('{id}') after remove returned null: {found == null}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveSurvey_ThenFindByUUID_ReturnsNull()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var survey = new Survey { UUID = id, PlanetName = "TestPlanet" };
                ctx.AddSurvey(survey);
                ctx.RemoveSurvey(survey);
                var found = ctx.FindSurvey(id);
                return (found == null)
                    .Label($"FindSurvey('{id}') after remove returned null: {found == null}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveColony_ThenFindByUUID_ReturnsNull()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var colony = new Colony { UUID = id, ColonyName = "TestColony" };
                ctx.AddColony(colony);
                ctx.RemoveColony(colony);
                var found = ctx.FindColony(id);
                return (found == null)
                    .Label($"FindColony('{id}') after remove returned null: {found == null}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveStation_ThenFindByUUID_ReturnsNull()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var station = new Station { UUID = id, Name = "TestStation" };
                ctx.AddStation(station);
                ctx.RemoveStation(station);
                var found = ctx.FindStation(id);
                return (found == null)
                    .Label($"FindStation('{id}') after remove returned null: {found == null}");
            });
        }

        #endregion

        #region Property 4: Invalidate-then-Find rebuilds correctly

        // Feature: readonly-list-encapsulation, Property 4: Invalidate-then-Find rebuilds correctly
        /// <summary>
        /// Add entities, call Invalidate, call Find, assert correct entity returned.
        /// **Validates: Requirements 10.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InvalidateBlueprint_ThenFind_RebuildsCorrectly()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var bp = new Bp { UUID = id, Name = "TestBP" };
                ctx.AddBlueprint(bp);
                ctx.InvalidateBlueprintCache();
                var found = ctx.FindBlueprint(id);
                return (found == bp)
                    .Label($"After invalidate, FindBlueprint('{id}') returned same instance: {found == bp}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InvalidateSurvey_ThenFind_RebuildsCorrectly()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var survey = new Survey { UUID = id, PlanetName = "TestPlanet" };
                ctx.AddSurvey(survey);
                ctx.InvalidateSurveyCache();
                var found = ctx.FindSurvey(id);
                return (found == survey)
                    .Label($"After invalidate, FindSurvey('{id}') returned same instance: {found == survey}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InvalidateColony_ThenFind_RebuildsCorrectly()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var id = Guid.NewGuid().ToString();
                var colony = new Colony { UUID = id, ColonyName = "TestColony" };
                ctx.AddColony(colony);
                ctx.InvalidateColonyCache();
                var found = ctx.FindColony(id);
                return (found == colony)
                    .Label($"After invalidate, FindColony('{id}') returned same instance: {found == colony}");
            });
        }

        #endregion

        #region Property 5: Snapshot independence

        // Feature: readonly-list-encapsulation, Property 5: Snapshot independence
        /// <summary>
        /// Get snapshot, mutate backing list via Add/Remove, assert snapshot unchanged.
        /// **Validates: Requirements 12.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SnapshotColony_IsIndependentOfAdd()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var colony1 = new Colony { UUID = Guid.NewGuid().ToString(), ColonyName = "Colony1" };
                ctx.AddColony(colony1);

                var snapshot = ctx.SnapshotColonyList();
                int snapshotCount = snapshot.Count;

                var colony2 = new Colony { UUID = Guid.NewGuid().ToString(), ColonyName = "Colony2" };
                ctx.AddColony(colony2);

                return (snapshot.Count == snapshotCount)
                    .Label($"Snapshot count {snapshot.Count} == original {snapshotCount} after Add");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SnapshotColony_IsIndependentOfRemove()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var colony1 = new Colony { UUID = Guid.NewGuid().ToString(), ColonyName = "Colony1" };
                ctx.AddColony(colony1);

                var snapshot = ctx.SnapshotColonyList();
                int snapshotCount = snapshot.Count;

                ctx.RemoveColony(colony1);

                return (snapshot.Count == snapshotCount && snapshot.Contains(colony1))
                    .Label($"Snapshot count {snapshot.Count} == {snapshotCount}, contains colony: {snapshot.Contains(colony1)}");
            });
        }

        #endregion

        #region Property 6: Blueprint mutation invalidates derived caches

        // Feature: readonly-list-encapsulation, Property 6: Blueprint mutation invalidates derived caches
        /// <summary>
        /// Add blueprint, verify CountBlueprintsByType reflects it.
        /// **Validates: Requirements 13.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddBlueprint_CountBlueprintsByType_ReflectsAddition()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var ownerUUID = Guid.NewGuid().ToString();
                var profile = new PlayerProfile { UUID = ownerUUID, Name = "TestPlayer" };
                ctx.AddPlayerProfile(profile);
                ctx.CurrentPlayerUUID = ownerUUID;

                string bpType = "Hull";
                int countBefore = ctx.CountBlueprintsByType(bpType);

                var bp = new Bp
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "TestBP",
                    BluePrintType = bpType,
                    OwnerUUID = ownerUUID
                };
                ctx.AddBlueprint(bp);

                int countAfter = ctx.CountBlueprintsByType(bpType);
                return (countAfter == countBefore + 1)
                    .Label($"CountBlueprintsByType('{bpType}'): before={countBefore}, after={countAfter}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveBlueprint_CountBlueprintsByType_ReflectsRemoval()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var ownerUUID = Guid.NewGuid().ToString();
                var profile = new PlayerProfile { UUID = ownerUUID, Name = "TestPlayer" };
                ctx.AddPlayerProfile(profile);
                ctx.CurrentPlayerUUID = ownerUUID;

                string bpType = "Hull";
                var bp = new Bp
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "TestBP",
                    BluePrintType = bpType,
                    OwnerUUID = ownerUUID
                };
                ctx.AddBlueprint(bp);
                int countAfterAdd = ctx.CountBlueprintsByType(bpType);

                ctx.RemoveBlueprint(bp);
                int countAfterRemove = ctx.CountBlueprintsByType(bpType);

                return (countAfterRemove == countAfterAdd - 1)
                    .Label($"CountBlueprintsByType('{bpType}'): afterAdd={countAfterAdd}, afterRemove={countAfterRemove}");
            });
        }

        #endregion

        #region Property 7: BuildPlan mutation invalidates build item indexes

        // Feature: readonly-list-encapsulation, Property 7: BuildPlan mutation invalidates build item indexes
        /// <summary>
        /// Add build plan with items, verify GetBuildItemsByBlueprint returns them.
        /// **Validates: Requirements 13.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AddBuildPlan_GetBuildItemsByBlueprint_ReturnsItems()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var blueprintUUID = Guid.NewGuid().ToString();
                var buildItem = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    BlueprintUUID = blueprintUUID,
                    ItemName = "TestItem",
                    Quantity = 1
                };
                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "TestPlan",
                    Items = new List<BuildItem> { buildItem }
                };

                ctx.AddBuildPlan(plan);
                var items = ctx.GetBuildItemsByBlueprint(blueprintUUID);

                return (items.Count == 1 && items[0].UUID == buildItem.UUID)
                    .Label($"GetBuildItemsByBlueprint returned {items.Count} items, expected 1");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemoveBuildPlan_GetBuildItemsByBlueprint_ReturnsEmpty()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), _ =>
            {
                var ctx = FreshPlayerContext();
                var blueprintUUID = Guid.NewGuid().ToString();
                var buildItem = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    BlueprintUUID = blueprintUUID,
                    ItemName = "TestItem",
                    Quantity = 1
                };
                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "TestPlan",
                    Items = new List<BuildItem> { buildItem }
                };

                ctx.AddBuildPlan(plan);
                ctx.GetBuildItemsByBlueprint(blueprintUUID); // force index build

                ctx.RemoveBuildPlan(plan);
                var items = ctx.GetBuildItemsByBlueprint(blueprintUUID);

                return (items.Count == 0)
                    .Label($"GetBuildItemsByBlueprint after remove returned {items.Count} items, expected 0");
            });
        }

        #endregion
    }
}
