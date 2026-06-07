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
