// <copyright file="AssetMergeServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for AssetMergeService.MergeColonyAssets.
    /// Validates: Req 5, Criteria 1-6; Req 13, Criteria 2-4.
    /// </summary>
    [TestFixture]
    public class AssetMergeServiceTests
    {
        // -------------------------------------------------------------------
        // Test 1: MergeColonyAssets with null colony returns false
        // Validates: Req 5, Criterion 1
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_NullColony_ReturnsFalse()
        {
            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem { CargoItemId = 1, Amount = 10, ResourceName = "Iron", TypeC = AssetTypeCodes.Resource },
            };

            bool result = AssetMergeService.MergeColonyAssets(apiItems, null);

            Assert.That(result, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 2: MergeColonyAssets with null/empty apiItems returns false
        // Validates: Req 5, Criterion 2
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_NullApiItems_ReturnsFalse()
        {
            var colony = new Colony { ColonyId = 100 };

            bool result = AssetMergeService.MergeColonyAssets(null, colony);

            Assert.That(result, Is.False);
        }

        [Test]
        public void MergeColonyAssets_EmptyApiItems_ReturnsFalse()
        {
            var colony = new Colony { ColonyId = 100 };

            bool result = AssetMergeService.MergeColonyAssets(new List<GameApiAssetCargoItem>(), colony);

            Assert.That(result, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 3: MergeColonyAssets creates new items when no match exists
        // Validates: Req 5, Criterion 3
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_NewItem_CreatesInColonyItems()
        {
            var colony = new Colony { ColonyId = 42 };
            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 999,
                    TypeId = 5,
                    Amount = 50,
                    ResourceName = "Iron",
                    TypeC = AssetTypeCodes.Resource,
                    Mass = 2.5,
                    Volume = 10,
                },
            };

            bool result = AssetMergeService.MergeColonyAssets(apiItems, colony);

            Assert.That(result, Is.True);
            Assert.That(colony.Items.Count(), Is.EqualTo(1));

            var item = colony.Items.Items.Values.First();
            Assert.That(item.GameItemId, Is.EqualTo(999));
            Assert.That(item.Name, Is.EqualTo("Iron"));
            Assert.That(item.Quantity, Is.EqualTo(50));
            Assert.That(item.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
        }

        // -------------------------------------------------------------------
        // Test 4: MergeColonyAssets updates existing items when GameItemId matches
        // Validates: Req 5, Criterion 4; Req 13, Criterion 2
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_ExistingItem_UpdatesByGameItemId()
        {
            var colony = new Colony { ColonyId = 42 };
            var existingItem = new Item
            {
                UUID = "existing-uuid-001",
                GameItemId = 500,
                Name = "Iron",
                Quantity = 10,
                ItemType = ItemType.ItemTypeEnum.Resource,
                ResourcePurity = string.Empty,
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 500,
                    TypeId = 5,
                    Amount = 75,
                    ResourceName = "Iron",
                    TypeC = AssetTypeCodes.Resource,
                    Mass = 3.0,
                    Volume = 15,
                },
            };

            bool result = AssetMergeService.MergeColonyAssets(apiItems, colony);

            Assert.That(result, Is.True);
            Assert.That(colony.Items.Count(), Is.EqualTo(1));
            Assert.That(existingItem.Quantity, Is.EqualTo(75));
            Assert.That(existingItem.GameItemId, Is.EqualTo(500));
        }

        // -------------------------------------------------------------------
        // Test 5: MergeColonyAssets preserves NickName and Description
        // Validates: Req 5, Criterion 5 (local-only field preservation)
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_PreservesLocalOnlyFields()
        {
            var colony = new Colony { ColonyId = 42 };
            var existingItem = new Item
            {
                UUID = "existing-uuid-002",
                GameItemId = 600,
                Name = "Copper",
                Quantity = 20,
                ItemType = ItemType.ItemTypeEnum.Resource,
                NickName = "My Copper Stash",
                Description = "Reserved for manufacturing",
                ResourcePurity = string.Empty,
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 600,
                    TypeId = 6,
                    Amount = 100,
                    ResourceName = "Copper",
                    TypeC = AssetTypeCodes.Resource,
                },
            };

            AssetMergeService.MergeColonyAssets(apiItems, colony);

            Assert.That(existingItem.NickName, Is.EqualTo("My Copper Stash"));
            Assert.That(existingItem.Description, Is.EqualTo("Reserved for manufacturing"));
            Assert.That(existingItem.Quantity, Is.EqualTo(100));
        }

        // -------------------------------------------------------------------
        // Test 6: MergeColonyAssets returns true when changes, false when none
        // Validates: Req 5, Criterion 6 (change detection return value)
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyAssets_NoChanges_ReturnsFalse()
        {
            var colony = new Colony { ColonyId = 42 };
            var existingItem = new Item
            {
                UUID = "existing-uuid-003",
                GameItemId = 700,
                Name = "Titanium",
                Quantity = 30,
                ItemType = ItemType.ItemTypeEnum.Resource,
                Volume = 5m,
                BaseItemTypeID = "Titanium",
                ResourcePurity = string.Empty,
                ShipPartType = string.Empty,
                JobName = string.Empty,
                JobTrack = string.Empty,
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 700,
                    TypeId = 8,
                    Amount = 30,
                    ResourceName = "Titanium",
                    TypeC = AssetTypeCodes.Resource,
                    Volume = 5,
                },
            };

            bool result = AssetMergeService.MergeColonyAssets(apiItems, colony);

            Assert.That(result, Is.False);
        }

        [Test]
        public void MergeColonyAssets_WithChanges_ReturnsTrue()
        {
            var colony = new Colony { ColonyId = 42 };
            var existingItem = new Item
            {
                UUID = "existing-uuid-004",
                GameItemId = 800,
                Name = "Gold",
                Quantity = 5,
                ItemType = ItemType.ItemTypeEnum.Resource,
                ResourcePurity = string.Empty,
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 800,
                    TypeId = 9,
                    Amount = 50,
                    ResourceName = "Gold",
                    TypeC = AssetTypeCodes.Resource,
                },
            };

            bool result = AssetMergeService.MergeColonyAssets(apiItems, colony);

            Assert.That(result, Is.True);
        }
    }
}
