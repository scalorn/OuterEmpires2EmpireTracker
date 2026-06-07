// <copyright file="CrateContentImporterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for CrateContentImporter.
    /// </summary>
    [TestFixture]
    public class CrateContentImporterTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private BlueprintLinkageService blueprintLinkageService;
        private CrateContentImporter importer;

        /// <summary>
        /// Sets up test fixtures.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            empireContext = EmpireContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            blueprintLinkageService = new BlueprintLinkageService(playerContext, empireContext);
            importer = new CrateContentImporter(playerContext, empireContext, blueprintLinkageService);
        }

        // -------------------------------------------------------------------
        // Test: Malformed JSON returns empty result with Success=false
        // Validates: Req 9 AC4 (malformed JSON no exception)
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that malformed JSON does not throw an exception and returns
        /// an empty result with Success set to false.
        /// </summary>
        [Test]
        public void CrateContentImporter_MalformedJson_NoException()
        {
            var parentBag = new ItemBag();
            var visited = new HashSet<int>();

            var result = importer.Import("not valid json {{{{", 999, parentBag, "owner-uuid", visited);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.TotalItems, Is.EqualTo(0));
            Assert.That(result.Imported, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test: All supported TypeC codes map to the correct ItemType
        // Validates: Req 2 AC2 (MapAssetTypeC mapping), Req 8 AC1-AC5
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that each supported TypeC code produces an item with the
        /// correct ItemType when processed through the full Import pipeline.
        /// </summary>
        [Test]
        public void CrateContentImporter_AllItemTypes_Mapped()
        {
            var cargoItems = new List<GameApiAssetCargoItem>
            {
                MakeCargoItem(1, "Bp", "Laser Mk2"),
                MakeCargoItem(2, "R", "Iron (High Purity)"),
                MakeCargoItem(3, "C", "Electronics"),
                MakeCargoItem(4, "L", "Advanced Plastics"),
                MakeCargoItem(5, "S", "Shield Generator"),
                MakeCargoItem(6, "SH", "Frigate Hull"),
                MakeCargoItem(7, "A", "Rail Slugs"),
                MakeCargoItem(8, "F", "Flatpack: Refinery"),
                MakeCargoItem(9, "W", "Workforce Unit"),
                MakeCargoItem(10, "Sh", "Corp Share"),
                MakeCargoItem(11, "D", "Probe"),
                MakeCargoItem(12, "Sc", "Planet Survey"),
            };

            var response = new GameApiAssetDetailResponse { Cargo = cargoItems };
            string json = JsonConvert.SerializeObject(response);

            var parentBag = new ItemBag();
            var visited = new HashSet<int>();

            var result = importer.Import(json, 100, parentBag, "owner-uuid", visited);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(12));
            Assert.That(result.Imported, Is.EqualTo(12));
            Assert.That(result.Failed, Is.EqualTo(0));

            // Verify type counts — codes with known mappings
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Blueprint], Is.EqualTo(1), "Bp → Blueprint");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Resource], Is.EqualTo(1), "R → Resource");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Commodity], Is.EqualTo(1), "C → Commodity");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.ShipPart], Is.EqualTo(1), "S → ShipPart");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.ShipHull], Is.EqualTo(1), "SH → ShipHull");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Munition], Is.EqualTo(1), "A → Munition");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Flatpack], Is.EqualTo(1), "F → Flatpack");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.WorkDetail], Is.EqualTo(1), "W → WorkDetail");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Share], Is.EqualTo(1), "Sh → Share");
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.Survey], Is.EqualTo(1), "Sc → Survey");

            // "L" (CommodityL) and "D" (Deployable) have no mapping in MapAssetTypeC → None
            Assert.That(result.CountsByType[ItemType.ItemTypeEnum.None], Is.EqualTo(2), "L and D → None (unmapped)");
        }

        // -------------------------------------------------------------------
        // Test: Contents bag fully replaced (no merge with old contents)
        // Validates: Req 3 AC1 (replace Contents with ItemBag),
        //            Req 3 AC2 (identify by GameItemId),
        //            Req 3 AC4 (clear previous contents)
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that importing crate contents fully replaces the existing
        /// Contents bag rather than merging with pre-existing items.
        /// </summary>
        [Test]
        public void CrateContentImporter_ContentsBagReplaced()
        {
            int crateGameItemId = 500;

            // Set up a parent bag with a pre-existing crate Item that has old contents
            var oldContentItem = new Item(ItemType.ItemTypeEnum.Resource, "Old Iron")
            {
                UUID = "old-item-uuid-1",
                GameItemId = 9000,
                Quantity = 100,
            };

            var oldContents = new ItemBag();
            oldContents.AddItem(oldContentItem);

            var crateItem = new Item(ItemType.ItemTypeEnum.Crate, "Crate")
            {
                UUID = "crate-uuid-1",
                GameItemId = crateGameItemId,
                Contents = oldContents,
            };

            var parentBag = new ItemBag();
            parentBag.AddItem(crateItem);

            // Build a response with new items
            var cargoItems = new List<GameApiAssetCargoItem>
            {
                MakeCargoItem(1001, "R", "Titanium (High Purity)"),
                MakeCargoItem(1002, "C", "Electronics"),
                MakeCargoItem(1003, "A", "Plasma Rounds"),
            };

            var response = new GameApiAssetDetailResponse { Cargo = cargoItems };
            string json = JsonConvert.SerializeObject(response);

            var visited = new HashSet<int>();

            // Act
            var result = importer.Import(json, crateGameItemId, parentBag, "owner-uuid", visited);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Imported, Is.EqualTo(3));

            // The crate should still be the same item in the parent bag
            var foundCrate = parentBag.Items["crate-uuid-1"];
            Assert.That(foundCrate, Is.Not.Null);
            Assert.That(foundCrate.GameItemId, Is.EqualTo(crateGameItemId));

            // Contents should be fully replaced — old item gone, new items present
            Assert.That(foundCrate.Contents, Is.Not.Null);
            Assert.That(foundCrate.Contents.Count(), Is.EqualTo(3));
            Assert.That(foundCrate.Contents.Items.ContainsKey("old-item-uuid-1"), Is.False, "Old contents should be gone");
        }

        // -------------------------------------------------------------------
        // Test: Crate Item created when not found in parent bag
        // Validates: Req 3 AC3 (create new if missing)
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that when no matching crate Item exists in the parent bag,
        /// a new crate Item is created with the correct GameItemId and populated.
        /// </summary>
        [Test]
        public void CrateContentImporter_CrateCreatedWhenMissing()
        {
            int crateGameItemId = 777;

            // Parent bag has no item with GameItemId=777
            var parentBag = new ItemBag();

            var cargoItems = new List<GameApiAssetCargoItem>
            {
                MakeCargoItem(2001, "R", "Iron (High Purity)"),
            };

            var response = new GameApiAssetDetailResponse { Cargo = cargoItems };
            string json = JsonConvert.SerializeObject(response);
            var visited = new HashSet<int>();

            // Act
            var result = importer.Import(json, crateGameItemId, parentBag, "owner-uuid", visited);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Imported, Is.EqualTo(1));

            // A new crate Item should have been created in parentBag
            Assert.That(parentBag.Count(), Is.EqualTo(1));

            Item createdCrate = null;
            foreach (var kvp in parentBag.Items)
            {
                if (kvp.Value.GameItemId == crateGameItemId)
                {
                    createdCrate = kvp.Value;
                    break;
                }
            }

            Assert.That(createdCrate, Is.Not.Null);
            Assert.That(createdCrate.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Crate));
            Assert.That(createdCrate.Contents, Is.Not.Null);
            Assert.That(createdCrate.Contents.Count(), Is.EqualTo(1));
        }

        private static GameApiAssetCargoItem MakeCargoItem(int id, string typeC, string name)
        {
            return new GameApiAssetCargoItem
            {
                CargoItemId = id,
                TypeC = typeC,
                ResourceName = name,
                Amount = 1,
                Mass = 1.0,
                Volume = 1.0,
            };
        }
    }
}
