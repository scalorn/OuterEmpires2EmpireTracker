using NUnit.Framework;
using OE2EmpireTracker.Models;
using System;
using IT = OE2EmpireTracker.Models.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Data
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
        // CountByType — core new functionality
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
    }
}
