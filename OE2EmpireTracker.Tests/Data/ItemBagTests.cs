using NUnit.Framework;
using OE2EmpireTracker.Data;
using System;
using IT = OE2EmpireTracker.Data.ItemType.ItemTypeEnum;

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
            Assert.IsTrue(_bag.ContainsKey("uuid-1"));
        }

        [Test]
        public void Count_ReflectsNumberOfDistinctStacks()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 20));
            Assert.AreEqual(2, _bag.Count());
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
            Assert.IsTrue(result);
            Assert.IsFalse(_bag.ContainsKey("uuid-1"));
        }

        [Test]
        public void Remove_UnknownUUID_ReturnsFalse()
        {
            bool result = _bag.Remove("no-such-uuid");
            Assert.IsFalse(result);
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
            Assert.AreEqual(0, _bag.Count());
        }

        // -----------------------------------------------------------------------
        // CountByType — core new functionality
        // -----------------------------------------------------------------------

        [Test]
        public void CountByType_SingleStack_ReturnsQuantity()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 50));
            Assert.AreEqual(50, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_MultipleStacks_SumsQuantities()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 20));
            Assert.AreEqual(50, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_DifferentBaseID_NotIncluded()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Resource, "Gold", 10));
            Assert.AreEqual(30, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_DifferentItemType_NotIncluded()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 30));
            _bag.AddItem(MakeItem(IT.Commodity, "Iron", 10));
            Assert.AreEqual(30, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_NoMatchingItems_ReturnsZero()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Gold", 10));
            Assert.AreEqual(0, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_EmptyBag_ReturnsZero()
        {
            Assert.AreEqual(0, _bag.CountByType(IT.Resource, "Iron"));
        }

        [Test]
        public void CountByType_BaseItemTypeIDIsCaseSensitive()
        {
            _bag.AddItem(MakeItem(IT.Resource, "Iron", 10));
            // "iron" (lowercase) should not match "Iron"
            Assert.AreEqual(0, _bag.CountByType(IT.Resource, "iron"));
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

            Assert.AreEqual(150, total);
            Assert.AreEqual(70, locked);
            Assert.AreEqual(80, available);
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

            Assert.AreEqual(100, available);
        }
    }
}
