using NUnit.Framework;
using OE2EmpireTracker.Models;
using System;
using IT = OE2EmpireTracker.Models.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ItemBagTests
    {
        private ItemBag _bag;

        [SetUp]
        public void SetUp()
        {
            _bag = new ItemBag();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static Item MakeItem(IT itemType, string baseID, int quantity, string uuid = null)
        {
            return new Item
            {
                UUID = uuid ?? Guid.NewGuid().ToString(),
                ItemType = itemType,
                BaseItemTypeID = baseID,
                Quantity = quantity
            };
        }

        private static Item MakeResource(string baseID, string purity, int quantity, string uuid = null)
        {
            return new Item
            {
                UUID = uuid ?? Guid.NewGuid().ToString(),
                ItemType = IT.Resource,
                BaseItemTypeID = baseID,
                ResourcePurity = purity,
                Quantity = quantity
            };
        }

        // -----------------------------------------------------------------------
        // AddItem / ContainsKey / Count
        // -----------------------------------------------------------------------

        [Test]
        public void AddItem_ItemIsRetrievableByUUID()
        {
            var item = MakeItem(IT.Resource, "Iron", 10, "uuid-1");
            _bag.AddItem(item);
            Assert.That(_bag.ContainsKey("uuid-1"), Is.True);
        }

        [Test]
        public void Count_ReflectsNumberOfDistinctStacks()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 20));
            Assert.That(_bag.Count(), Is.EqualTo(2));
        }

        [Test]
        public void AddItem_DuplicateUUID_Throws()
        {
            var item = MakeItem(IT.Resource, "Iron", 10, "uuid-dup");
            _bag.AddItem(item);
            Assert.Throws<ArgumentException>(() => _bag.AddItem(item));
        }

        // -----------------------------------------------------------------------
        // Remove
        // -----------------------------------------------------------------------

        [Test]
        public void Remove_ExistingItem_ReturnsTrueAndRemoves()
        {
            var item = MakeItem(IT.Resource, "Iron", 10, "uuid-1");
            _bag.AddItem(item);
            bool result = _bag.Remove("uuid-1");
            Assert.That(result, Is.True);
            Assert.That(_bag.ContainsKey("uuid-1"), Is.False);
        }

        [Test]
        public void Remove_UnknownUUID_ReturnsFalse()
        {
            bool result = _bag.Remove("no-such-uuid");
            Assert.That(result, Is.False);
        }

        // -----------------------------------------------------------------------
        // Clear
        // -----------------------------------------------------------------------

        [Test]
        public void Clear_EmptiesTheBag()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            _bag.AddItem(MakeItem(IT.Commodity, "Steel Beams", 5));
            _bag.Clear();
            Assert.That(_bag.Count(), Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // CountByType -- core new functionality
        // -----------------------------------------------------------------------

        [Test]
        public void CountByType_SingleStack_ReturnsQuantity()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 50));
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(50));
        }

        [Test]
        public void CountByType_MultipleStacks_SumsQuantities()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 20));
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(50));
        }

        [Test]
        public void CountByType_DifferentBaseID_NotIncluded()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Resource, "Gold", 10));
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(30));
        }

        [Test]
        public void CountByType_DifferentItemType_NotIncluded()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Commodity, "Iron", 10));
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(30));
        }

        [Test]
        public void CountByType_NoMatchingItems_ReturnsZero()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Gold", 10));
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(0));
        }

        [Test]
        public void CountByType_EmptyBag_ReturnsZero()
        {
            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(0));
        }

        [Test]
        public void CountByType_BaseItemTypeIDIsCaseSensitive()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            // "iron" (lowercase) should not match "Iron"
            Assert.That(_bag.CountByType(IT.Resource, "iron"), Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // CountByType paired with LockTracking.GetLockedQuantity
        // -----------------------------------------------------------------------

        [Test]
        public void AvailableQuantity_TotalMinusLocked_IsCorrect()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 100));
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 50));

            var locks = new LockTracking();
            locks.LockItem("process-A", IT.Resource, "Iron", 40);
            locks.LockItem("process-B", IT.Resource, "Iron", 30);

            int total = _bag.CountByType(IT.Resource, "Iron");           // 150
            int locked = locks.GetLockedQuantity(IT.Resource, "Iron");   // 70
            int available = total - locked;                               // 80

            Assert.That(total, Is.EqualTo(150));
            Assert.That(locked, Is.EqualTo(70));
            Assert.That(available, Is.EqualTo(80));
        }

        [Test]
        public void AvailableQuantity_AfterClearingLocks_EqualsTotal()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 100));

            var locks = new LockTracking();
            locks.LockItem("process-A", IT.Resource, "Iron", 60);
            locks.ClearLocksForProcess("process-A");

            int available = _bag.CountByType(IT.Resource, "Iron") -
                            locks.GetLockedQuantity(IT.Resource, "Iron");

            Assert.That(available, Is.EqualTo(100));
        }

        // -----------------------------------------------------------------------
        // Secondary Index — FindByType
        // -----------------------------------------------------------------------

        [Test]
        public void FindByType_ReturnsMatchingItems()
        {
            var iron1 = MakeItem(IT.Resource, "Iron", 10);
            var iron2 = MakeItem(IT.Resource, "Iron", 20);
            var gold = MakeItem(IT.Resource, "Gold", 5);
            _bag.AddItem(iron1);
            _bag.AddItem(iron2);
            _bag.AddItem(gold);

            var result = _bag.FindByType(IT.Resource, "Iron");

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(iron1));
            Assert.That(result, Does.Contain(iron2));
        }

        [Test]
        public void FindByType_NoMatch_ReturnsEmptyList()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Gold", 5));
            var result = _bag.FindByType(IT.Commodity, "Steel");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindByType_EmptyBag_ReturnsEmptyList()
        {
            var result = _bag.FindByType(IT.Resource, "Iron");
            Assert.That(result, Is.Empty);
        }

        // -----------------------------------------------------------------------
        // Secondary Index — CountByType with index
        // -----------------------------------------------------------------------

        [Test]
        public void CountByType_UsesIndex_SumsCorrectly()
        {
            _bag.AddItem(MakeItem(IT.Commodity, "Steel", 10));
            _bag.AddItem(MakeItem(IT.Commodity, "Steel", 25));
            _bag.AddItem(MakeItem(IT.Commodity, "Copper Wire", 7));

            Assert.That(_bag.CountByType(IT.Commodity, "Steel"), Is.EqualTo(35));
            Assert.That(_bag.CountByType(IT.Commodity, "Copper Wire"), Is.EqualTo(7));
        }

        // -----------------------------------------------------------------------
        // Secondary Index — FindResource
        // -----------------------------------------------------------------------

        [Test]
        public void FindResource_ReturnsMatchingResourceAndPurity()
        {
            var ironHigh = MakeResource("Iron", "High", 10);
            var ironLow = MakeResource("Iron", "Low", 20);
            var goldHigh = MakeResource("Gold", "High", 5);
            _bag.AddItem(ironHigh);
            _bag.AddItem(ironLow);
            _bag.AddItem(goldHigh);

            var result = _bag.FindResource("Iron", "High");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.SameAs(ironHigh));
        }

        [Test]
        public void FindResource_MultipleStacksSamePurity_ReturnsAll()
        {
            var r1 = MakeResource("Iron", "High", 10);
            var r2 = MakeResource("Iron", "High", 20);
            _bag.AddItem(r1);
            _bag.AddItem(r2);

            var result = _bag.FindResource("Iron", "High");
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void FindResource_NoMatch_ReturnsEmptyList()
        {
            _bag.AddItem(MakeResource("Iron", "High", 10));
            var result = _bag.FindResource("Iron", "Low");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindResource_EmptyBag_ReturnsEmptyList()
        {
            var result = _bag.FindResource("Iron", "High");
            Assert.That(result, Is.Empty);
        }

        // -----------------------------------------------------------------------
        // Cache Invalidation — Add then query
        // -----------------------------------------------------------------------

        [Test]
        public void FindByType_AfterAddItem_FindsNewItem()
        {
            // Prime the index
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            _bag.FindByType(IT.Resource, "Iron");

            // Add another item — index should be invalidated
            var newItem = MakeItem(IT.Resource, "Iron", 20);
            _bag.AddItem(newItem);

            var result = _bag.FindByType(IT.Resource, "Iron");
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(newItem));
        }

        [Test]
        public void FindResource_AfterAddItem_FindsNewItem()
        {
            _bag.AddItem(MakeResource("Iron", "High", 10));
            _bag.FindResource("Iron", "High");

            var newItem = MakeResource("Iron", "High", 5);
            _bag.AddItem(newItem);

            var result = _bag.FindResource("Iron", "High");
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(newItem));
        }

        // -----------------------------------------------------------------------
        // Cache Invalidation — Remove then query
        // -----------------------------------------------------------------------

        [Test]
        public void FindByType_AfterRemove_NoLongerFindsRemovedItem()
        {
            var item = MakeItem(IT.Resource, "Iron", 10, "uuid-rem");
            _bag.AddItem(item);
            _bag.FindByType(IT.Resource, "Iron"); // prime index

            _bag.Remove("uuid-rem");

            var result = _bag.FindByType(IT.Resource, "Iron");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindResource_AfterRemove_NoLongerFindsRemovedItem()
        {
            var item = MakeResource("Iron", "High", 10);
            item.UUID = "uuid-rem-res";
            _bag.AddItem(item);
            _bag.FindResource("Iron", "High"); // prime index

            _bag.Remove("uuid-rem-res");

            var result = _bag.FindResource("Iron", "High");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CountByType_AfterRemove_ReflectsRemoval()
        {
            var item = MakeItem(IT.Resource, "Iron", 50, "uuid-cnt");
            _bag.AddItem(item);
            _bag.CountByType(IT.Resource, "Iron"); // prime index

            _bag.Remove("uuid-cnt");

            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Cache Invalidation — Clear then query
        // -----------------------------------------------------------------------

        [Test]
        public void FindByType_AfterClear_ReturnsEmpty()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            _bag.FindByType(IT.Resource, "Iron"); // prime index

            _bag.Clear();

            Assert.That(_bag.FindByType(IT.Resource, "Iron"), Is.Empty);
        }

        [Test]
        public void CountByType_AfterClear_ReturnsZero()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 50));
            _bag.CountByType(IT.Resource, "Iron"); // prime index

            _bag.Clear();

            Assert.That(_bag.CountByType(IT.Resource, "Iron"), Is.EqualTo(0));
        }

        [Test]
        public void FindResource_AfterClear_ReturnsEmpty()
        {
            _bag.AddItem(MakeResource("Iron", "High", 10));
            _bag.FindResource("Iron", "High"); // prime index

            _bag.Clear();

            Assert.That(_bag.FindResource("Iron", "High"), Is.Empty);
        }
    }
}
