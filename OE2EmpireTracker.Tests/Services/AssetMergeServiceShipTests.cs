// <copyright file="AssetMergeServiceShipTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for AssetMergeService.MergeShipAssets.
    /// Validates: Req 7, Criteria 1-6 (ship asset merge).
    /// </summary>
    [TestFixture]
    public class AssetMergeServiceShipTests
    {
        /// <summary>
        /// MergeShipAssets returns false when ship is null.
        /// Validates: Req 7.1 (null ship guard).
        /// </summary>
        [Test]
        public void MergeShipAssets_NullShip_ReturnsFalse()
        {
            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem { CargoItemId = 1, ResourceName = "Iron", TypeC = "R", Amount = 10 },
            };

            bool result = AssetMergeService.MergeShipAssets(apiItems, null);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// MergeShipAssets returns false when apiItems is null.
        /// Validates: Req 7.6 (no changes when no input).
        /// </summary>
        [Test]
        public void MergeShipAssets_NullApiItems_ReturnsFalse()
        {
            var ship = new Ship { UUID = "ship-1", GameLocationId = 100 };

            bool result = AssetMergeService.MergeShipAssets(null, ship);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// MergeShipAssets returns false when apiItems is empty.
        /// Validates: Req 7.6 (no changes when no input).
        /// </summary>
        [Test]
        public void MergeShipAssets_EmptyApiItems_ReturnsFalse()
        {
            var ship = new Ship { UUID = "ship-2", GameLocationId = 200 };

            bool result = AssetMergeService.MergeShipAssets(new List<GameApiAssetCargoItem>(), ship);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// MergeShipAssets creates new items in Ship.Cargo when no match exists.
        /// Validates: Req 7.3, 7.5 (merge into Ship.Cargo, create new items).
        /// </summary>
        [Test]
        public void MergeShipAssets_NewItems_CreatesInCargo()
        {
            var ship = new Ship { UUID = "ship-3", GameLocationId = 300 };
            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 501,
                    ResourceName = "Titanium",
                    TypeC = "R",
                    Amount = 25,
                    Mass = 1.5,
                    Volume = 3,
                },
                new GameApiAssetCargoItem
                {
                    CargoItemId = 502,
                    ResourceName = "Plasma Cannon",
                    TypeC = "A",
                    Amount = 2,
                    Mass = 10.0,
                    Volume = 5,
                },
            };

            bool result = AssetMergeService.MergeShipAssets(apiItems, ship);

            Assert.That(result, Is.True);
            Assert.That(ship.Cargo.Count(), Is.EqualTo(2));

            var items = ship.Cargo.Items.Values.ToList();
            var titanium = items.First(i => i.GameItemId == 501);
            Assert.That(titanium.Name, Is.EqualTo("Titanium"));
            Assert.That(titanium.Quantity, Is.EqualTo(25));
            Assert.That(titanium.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));

            var cannon = items.First(i => i.GameItemId == 502);
            Assert.That(cannon.Name, Is.EqualTo("Plasma Cannon"));
            Assert.That(cannon.Quantity, Is.EqualTo(2));
            Assert.That(cannon.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Munition));
        }

        /// <summary>
        /// MergeShipAssets updates existing items when GameItemId matches.
        /// Validates: Req 7.4 (update existing item matched by GameItemId).
        /// </summary>
        [Test]
        public void MergeShipAssets_ExistingItem_UpdatesByGameItemId()
        {
            var ship = new Ship { UUID = "ship-4", GameLocationId = 400 };
            var existingItem = new Item
            {
                UUID = "existing-item-uuid",
                GameItemId = 601,
                Name = "Steel Plates",
                Quantity = 10,
                ItemType = ItemType.ItemTypeEnum.Commodity,
            };
            ship.Cargo.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 601,
                    ResourceName = "Steel Plates",
                    TypeC = "C",
                    Amount = 50,
                    Mass = 5.0,
                    Volume = 8,
                },
            };

            bool result = AssetMergeService.MergeShipAssets(apiItems, ship);

            Assert.That(result, Is.True);
            Assert.That(ship.Cargo.Count(), Is.EqualTo(1));
            Assert.That(existingItem.Quantity, Is.EqualTo(50));
            Assert.That(existingItem.UUID, Is.EqualTo("existing-item-uuid"));
        }

        /// <summary>
        /// MergeShipAssets returns true when changes are made, false when no changes.
        /// Validates: Req 7.6 (change detection return value).
        /// </summary>
        [Test]
        public void MergeShipAssets_NoChanges_ReturnsFalse()
        {
            var ship = new Ship { UUID = "ship-5", GameLocationId = 500 };
            var existingItem = new Item
            {
                UUID = "no-change-item-uuid",
                GameItemId = 701,
                Name = "Fuel Cells",
                Quantity = 100,
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Mass = 2.0m,
                Volume = 4m,
                BaseItemTypeID = "55",
                ResourcePurity = string.Empty,
                ShipPartType = string.Empty,
                JobName = string.Empty,
                JobTrack = string.Empty,
            };
            ship.Cargo.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 701,
                    ResourceName = "Fuel Cells",
                    TypeC = "C",
                    Amount = 100,
                    Mass = 2.0,
                    Volume = 4,
                    TypeId = 55,
                    ShipPartType = string.Empty,
                    JobName = string.Empty,
                    JobTrack = string.Empty,
                },
            };

            bool result = AssetMergeService.MergeShipAssets(apiItems, ship);

            Assert.That(result, Is.False);
        }
    }
}
