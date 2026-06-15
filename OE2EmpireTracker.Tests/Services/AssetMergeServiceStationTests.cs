// <copyright file="AssetMergeServiceStationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for AssetMergeService.MergeStationAssets.
    /// Validates: Requirement 6, Criteria 1-6 (station asset merge).
    /// </summary>
    [TestFixture]
    public class AssetMergeServiceStationTests
    {
        // -------------------------------------------------------------------
        // Test 1: Null station returns false
        // Validates: Requirement 6, Criterion 1 (match station by identifier)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NullStation_ReturnsFalse()
        {
            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem { Id = 1, Amount = 10, ResourceName = "Iron", TypeC = "R" },
            };
            var hold = new ItemBag();

            bool result = AssetMergeService.MergeStationAssets(apiItems, null, hold);

            Assert.That(result, Is.False);
            Assert.That(hold.Count(), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 2: Null targetHold returns false
        // Validates: Requirement 6, Criterion 3 (merge into primary hold)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NullTargetHold_ReturnsFalse()
        {
            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem { Id = 1, Amount = 10, ResourceName = "Iron", TypeC = "R" },
            };
            var station = new Station { UUID = "station-001", GameLocationId = 100 };

            bool result = AssetMergeService.MergeStationAssets(apiItems, station, null);

            Assert.That(result, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 3: Null apiItems returns false
        // Validates: Requirement 6, Criterion 6 (return boolean for changes)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NullApiItems_ReturnsFalse()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();

            bool result = AssetMergeService.MergeStationAssets(null, station, hold);

            Assert.That(result, Is.False);
            Assert.That(hold.Count(), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 4: Empty apiItems returns false
        // Validates: Requirement 6, Criterion 6 (return boolean for changes)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_EmptyApiItems_ReturnsFalse()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();

            bool result = AssetMergeService.MergeStationAssets(new List<AssetCargoItem>(), station, hold);

            Assert.That(result, Is.False);
            Assert.That(hold.Count(), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 5: Creates new items in target hold when no match exists
        // Validates: Requirement 6, Criteria 3, 5 (merge into hold, create new)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NewItems_CreatesInTargetHold()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();
            station.Holds[AssetMergeService.DefaultHoldName] = hold;

            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem
                {
                    Id = 501,
                    TypeId = 10,
                    Amount = 25,
                    ResourceName = "Titanium",
                    TypeC = "R",
                    Mass = 2.5,
                    Volume = 5,
                },
                new AssetCargoItem
                {
                    Id = 502,
                    TypeId = 20,
                    Amount = 3,
                    ResourceName = "Plasma Cannon",
                    TypeC = "A",
                    Mass = 15.0,
                    Volume = 10,
                },
            };

            bool result = AssetMergeService.MergeStationAssets(apiItems, station, hold);

            Assert.That(result, Is.True);
            Assert.That(hold.Count(), Is.EqualTo(2));

            var items = hold.Items.Values.ToList();
            var titanium = items.First(i => i.GameItemId == 501);
            Assert.That(titanium.Name, Is.EqualTo("Titanium"));
            Assert.That(titanium.Quantity, Is.EqualTo(25));
            Assert.That(titanium.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));

            var cannon = items.First(i => i.GameItemId == 502);
            Assert.That(cannon.Name, Is.EqualTo("Plasma Cannon"));
            Assert.That(cannon.Quantity, Is.EqualTo(3));
            Assert.That(cannon.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Munition));
        }

        // -------------------------------------------------------------------
        // Test 6: Updates existing items when GameItemId matches
        // Validates: Requirement 6, Criterion 4 (update existing by GameItemId)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_ExistingItem_UpdatesByGameItemId()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();
            station.Holds[AssetMergeService.DefaultHoldName] = hold;

            var existingItem = new Item
            {
                UUID = "existing-item-uuid",
                GameItemId = 601,
                Name = "Old Name",
                Quantity = 5,
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Volume = 2m,
            };
            hold.AddItem(existingItem);

            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem
                {
                    Id = 601,
                    TypeId = 30,
                    Amount = 50,
                    ResourceName = "Updated Commodity",
                    TypeC = "C",
                    Mass = 3.0,
                    Volume = 8,
                },
            };

            bool result = AssetMergeService.MergeStationAssets(apiItems, station, hold);

            Assert.That(result, Is.True);
            Assert.That(hold.Count(), Is.EqualTo(1));

            var updated = hold.Items["existing-item-uuid"];
            Assert.That(updated.GameItemId, Is.EqualTo(601));
            Assert.That(updated.Name, Is.EqualTo("Updated Commodity"));
            Assert.That(updated.Quantity, Is.EqualTo(50));
            Assert.That(updated.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Commodity));
            Assert.That(updated.Volume, Is.EqualTo(8m));
        }

        // -------------------------------------------------------------------
        // Test 7: Returns true when changes made, false when no changes
        // Validates: Requirement 6, Criterion 6 (change detection return)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NoChanges_ReturnsFalse()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();
            station.Holds[AssetMergeService.DefaultHoldName] = hold;

            var existingItem = new Item
            {
                UUID = "existing-item-uuid",
                GameItemId = 701,
                Name = "Stable Item",
                Quantity = 10,
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Mass = 5m,
                Volume = 3m,
                BaseItemTypeID = "Stable Item",
                ShipPartType = string.Empty,
                JobName = string.Empty,
                JobTrack = string.Empty,
                ResourcePurity = string.Empty,
            };
            hold.AddItem(existingItem);

            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem
                {
                    Id = 701,
                    TypeId = 40,
                    Amount = 10,
                    ResourceName = "Stable Item",
                    TypeC = "C",
                    Mass = 5.0,
                    Volume = 3,
                    ShipPartType = string.Empty,
                    JobName = string.Empty,
                    JobTrack = string.Empty,
                },
            };

            bool result = AssetMergeService.MergeStationAssets(apiItems, station, hold);

            Assert.That(result, Is.False);
            Assert.That(hold.Count(), Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Test 8: Returns true when changes are made (new item added)
        // Validates: Requirement 6, Criterion 6 (change detection return)
        // -------------------------------------------------------------------

        [Test]
        public void MergeStationAssets_NewItemAdded_ReturnsTrue()
        {
            var station = new Station { UUID = "station-001", GameLocationId = 100 };
            var hold = new ItemBag();
            station.Holds[AssetMergeService.DefaultHoldName] = hold;

            var apiItems = new List<AssetCargoItem>
            {
                new AssetCargoItem
                {
                    Id = 801,
                    TypeId = 50,
                    Amount = 1,
                    ResourceName = "New Widget",
                    TypeC = "F",
                },
            };

            bool result = AssetMergeService.MergeStationAssets(apiItems, station, hold);

            Assert.That(result, Is.True);
            Assert.That(hold.Count(), Is.EqualTo(1));
        }
    }
}
