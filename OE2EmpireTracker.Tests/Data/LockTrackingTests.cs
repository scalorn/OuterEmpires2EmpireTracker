using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Data;
using System.Collections.Generic;
using System.Linq;
using IT = OE2EmpireTracker.Data.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class LockTrackingTests
    {
        private LockTracking _tracking;
        private const string ProcessA = "process-uuid-A";
        private const string ProcessB = "process-uuid-B";

        [SetUp]
        public void SetUp()
        {
            _tracking = new LockTracking();
        }

        // -----------------------------------------------------------------------
        // ItemKey
        // -----------------------------------------------------------------------

        [Test]
        public void ItemKey_EqualityByValue()
        {
            var a = new ItemKey(IT.Resource, "Iron");
            var b = new ItemKey(IT.Resource, "Iron");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void ItemKey_DifferentType_NotEqual()
        {
            var a = new ItemKey(IT.Resource, "Iron");
            var b = new ItemKey(IT.Commodity, "Iron");
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void ItemKey_DifferentID_NotEqual()
        {
            var a = new ItemKey(IT.Resource, "Iron");
            var b = new ItemKey(IT.Resource, "Gold");
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void ItemKey_ToString_ProducesExpectedFormat()
        {
            var key = new ItemKey(IT.Resource, "Iron");
            Assert.AreEqual("Resource:Iron", key.ToString());
        }

        [Test]
        public void ItemKey_Parse_RoundTrips()
        {
            var original = new ItemKey(IT.Commodity, "Steel Beams");
            var parsed = ItemKey.Parse(original.ToString());
            Assert.AreEqual(original, parsed);
        }

        [Test]
        public void ItemKey_Parse_NullOrEmpty_ReturnsNoneKey()
        {
            var key = ItemKey.Parse(null);
            Assert.AreEqual(IT.None, key.ItemType);
        }

        [Test]
        public void ItemKey_Parse_NoSeparator_ReturnsNoneType()
        {
            var key = ItemKey.Parse("NoColonHere");
            Assert.AreEqual(IT.None, key.ItemType);
            Assert.AreEqual("NoColonHere", key.BaseItemTypeID);
        }

        [Test]
        public void ItemKey_UsableAsDictionaryKey()
        {
            var dict = new Dictionary<ItemKey, int>();
            var key = new ItemKey(IT.Resource, "Iron");
            dict[key] = 10;
            Assert.AreEqual(10, dict[new ItemKey(IT.Resource, "Iron")]);
        }

        // -----------------------------------------------------------------------
        // LockItem — single item
        // -----------------------------------------------------------------------

        [Test]
        public void LockItem_SingleItem_LockedQuantityReflected()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            Assert.AreEqual(50, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void LockItem_SameItemTwice_QuantitiesAccumulate()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 30);
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 20);
            Assert.AreEqual(50, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void LockItem_DifferentItems_TrackedIndependently()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 5);

            Assert.AreEqual(10, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
            Assert.AreEqual(5, _tracking.GetLockedQuantity(IT.Commodity, "Steel Beams"));
        }

        [Test]
        public void LockItem_NullProcessUUID_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                _tracking.LockItem(null, IT.Resource, "Iron", 10));
        }

        [Test]
        public void LockItem_EmptyProcessUUID_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                _tracking.LockItem(string.Empty, IT.Resource, "Iron", 10));
        }

        // -----------------------------------------------------------------------
        // LockItems — multiple items
        // -----------------------------------------------------------------------

        [Test]
        public void LockItems_MultipleItems_AllLocked()
        {
            var items = new List<ItemLock>
            {
                new ItemLock(new ItemKey(IT.Resource, "Iron"), 100),
                new ItemLock(new ItemKey(IT.Resource, "Gold"), 25),
                new ItemLock(new ItemKey(IT.Commodity, "Steel Beams"), 10),
            };

            _tracking.LockItems(ProcessA, items);

            Assert.AreEqual(100, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
            Assert.AreEqual(25, _tracking.GetLockedQuantity(IT.Resource, "Gold"));
            Assert.AreEqual(10, _tracking.GetLockedQuantity(IT.Commodity, "Steel Beams"));
        }

        [Test]
        public void LockItems_EmptyList_NoLocksAdded()
        {
            _tracking.LockItems(ProcessA, new List<ItemLock>());
            Assert.AreEqual(0, _tracking.GetLocksForProcess(ProcessA).Count);
        }

        // -----------------------------------------------------------------------
        // GetLockedQuantity — across processes
        // -----------------------------------------------------------------------

        [Test]
        public void GetLockedQuantity_MultipleProcesses_SumsAll()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 30);
            _tracking.LockItem(ProcessB, IT.Resource, "Iron", 20);

            Assert.AreEqual(50, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void GetLockedQuantity_UnknownItem_ReturnsZero()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            Assert.AreEqual(0, _tracking.GetLockedQuantity(IT.Resource, "Gold"));
        }

        [Test]
        public void GetLockedQuantity_NoLocks_ReturnsZero()
        {
            Assert.AreEqual(0, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
        }

        // -----------------------------------------------------------------------
        // GetLocksForProcess
        // -----------------------------------------------------------------------

        [Test]
        public void GetLocksForProcess_ReturnsOnlyThatProcessLocks()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessB, IT.Resource, "Gold", 5);

            var locks = _tracking.GetLocksForProcess(ProcessA);

            Assert.AreEqual(1, locks.Count);
            Assert.AreEqual(new ItemKey(IT.Resource, "Iron"), locks[0].Key);
            Assert.AreEqual(10, locks[0].Quantity);
        }

        [Test]
        public void GetLocksForProcess_UnknownProcess_ReturnsEmptyList()
        {
            var locks = _tracking.GetLocksForProcess("no-such-process");
            Assert.IsNotNull(locks);
            Assert.AreEqual(0, locks.Count);
        }

        [Test]
        public void GetLocksForProcess_MultipleItems_AllReturned()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 3);

            var locks = _tracking.GetLocksForProcess(ProcessA);

            Assert.AreEqual(2, locks.Count);
            Assert.IsTrue(locks.Any(l => l.Key.Equals(new ItemKey(IT.Resource, "Iron")) && l.Quantity == 10));
            Assert.IsTrue(locks.Any(l => l.Key.Equals(new ItemKey(IT.Commodity, "Steel Beams")) && l.Quantity == 3));
        }

        // -----------------------------------------------------------------------
        // ClearLocksForProcess
        // -----------------------------------------------------------------------

        [Test]
        public void ClearLocksForProcess_RemovesAllLocksForThatProcess()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.ClearLocksForProcess(ProcessA);

            Assert.AreEqual(0, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
            Assert.AreEqual(0, _tracking.GetLocksForProcess(ProcessA).Count);
        }

        [Test]
        public void ClearLocksForProcess_DoesNotAffectOtherProcesses()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.LockItem(ProcessB, IT.Resource, "Iron", 20);

            _tracking.ClearLocksForProcess(ProcessA);

            Assert.AreEqual(20, _tracking.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void ClearLocksForProcess_UnknownProcess_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _tracking.ClearLocksForProcess("no-such-process"));
        }

        // -----------------------------------------------------------------------
        // JSON serialization round-trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_SingleProcessSingleItem_Preserved()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 42);

            string json = JsonConvert.SerializeObject(_tracking);
            var restored = JsonConvert.DeserializeObject<LockTracking>(json);

            Assert.AreEqual(42, restored.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void JsonRoundTrip_MultipleProcessesMultipleItems_AllPreserved()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 3);
            _tracking.LockItem(ProcessB, IT.Resource, "Gold", 7);

            string json = JsonConvert.SerializeObject(_tracking);
            var restored = JsonConvert.DeserializeObject<LockTracking>(json);

            Assert.AreEqual(10, restored.GetLockedQuantity(IT.Resource, "Iron"));
            Assert.AreEqual(3, restored.GetLockedQuantity(IT.Commodity, "Steel Beams"));
            Assert.AreEqual(7, restored.GetLockedQuantity(IT.Resource, "Gold"));
            Assert.AreEqual(1, restored.GetLocksForProcess(ProcessA).Count(l => l.Key.BaseItemTypeID == "Iron"));
        }

        [Test]
        public void JsonRoundTrip_EmptyTracking_ProducesEmptyObject()
        {
            string json = JsonConvert.SerializeObject(_tracking);
            Assert.AreEqual("{}", json);

            var restored = JsonConvert.DeserializeObject<LockTracking>(json);
            Assert.AreEqual(0, restored.GetLockedQuantity(IT.Resource, "Iron"));
        }

        [Test]
        public void JsonRoundTrip_AfterClear_ClearedLocksNotRestored()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.LockItem(ProcessB, IT.Resource, "Gold", 10);
            _tracking.ClearLocksForProcess(ProcessA);

            string json = JsonConvert.SerializeObject(_tracking);
            var restored = JsonConvert.DeserializeObject<LockTracking>(json);

            Assert.AreEqual(0, restored.GetLockedQuantity(IT.Resource, "Iron"));
            Assert.AreEqual(10, restored.GetLockedQuantity(IT.Resource, "Gold"));
        }
    }
}
