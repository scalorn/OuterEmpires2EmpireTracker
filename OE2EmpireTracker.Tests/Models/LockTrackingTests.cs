using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;
using IT = OE2EmpireTracker.Models.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(b, Is.EqualTo(a));
        }

        [Test]
        public void ItemKey_DifferentType_NotEqual()
        {
            var a = new ItemKey(IT.Resource, "Iron");
            var b = new ItemKey(IT.Commodity, "Iron");
            Assert.That(b, Is.Not.EqualTo(a));
        }

        [Test]
        public void ItemKey_DifferentID_NotEqual()
        {
            var a = new ItemKey(IT.Resource, "Iron");
            var b = new ItemKey(IT.Resource, "Gold");
            Assert.That(b, Is.Not.EqualTo(a));
        }

        [Test]
        public void ItemKey_ToString_ProducesExpectedFormat()
        {
            var key = new ItemKey(IT.Resource, "Iron");
            Assert.That(key.ToString(), Is.EqualTo("Resource:Iron"));
        }

        [Test]
        public void ItemKey_Parse_RoundTrips()
        {
            var original = new ItemKey(IT.Commodity, "Steel Beams");
            var parsed = ItemKey.Parse(original.ToString());
            Assert.That(parsed, Is.EqualTo(original));
        }

        [Test]
        public void ItemKey_Parse_NullOrEmpty_ReturnsNoneKey()
        {
            var key = ItemKey.Parse(null);
            Assert.That(key.ItemType, Is.EqualTo(IT.None));
        }

        [Test]
        public void ItemKey_Parse_NoSeparator_ReturnsNoneType()
        {
            var key = ItemKey.Parse("NoColonHere");
            Assert.That(key.ItemType, Is.EqualTo(IT.None));
            Assert.That(key.BaseItemTypeID, Is.EqualTo("NoColonHere"));
        }

        [Test]
        public void ItemKey_UsableAsDictionaryKey()
        {
            var dict = new Dictionary<ItemKey, int>();
            var key = new ItemKey(IT.Resource, "Iron");
            dict[key] = 10;
            Assert.That(dict[new ItemKey(IT.Resource, "Iron")], Is.EqualTo(10));
        }

        // -----------------------------------------------------------------------
        // LockItem — single item
        // -----------------------------------------------------------------------

        [Test]
        public void LockItem_SingleItem_LockedQuantityReflected()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(50));
        }

        [Test]
        public void LockItem_SameItemTwice_QuantitiesAccumulate()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 30);
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 20);
            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(50));
        }

        [Test]
        public void LockItem_DifferentItems_TrackedIndependently()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 5);

            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(10));
            Assert.That(_tracking.GetLockedQuantity(IT.Commodity, "Steel Beams"), Is.EqualTo(5));
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

            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(100));
            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Gold"), Is.EqualTo(25));
            Assert.That(_tracking.GetLockedQuantity(IT.Commodity, "Steel Beams"), Is.EqualTo(10));
        }

        [Test]
        public void LockItems_EmptyList_NoLocksAdded()
        {
            _tracking.LockItems(ProcessA, new List<ItemLock>());
            Assert.That(_tracking.GetLocksForProcess(ProcessA).Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // GetLockedQuantity — across processes
        // -----------------------------------------------------------------------

        [Test]
        public void GetLockedQuantity_MultipleProcesses_SumsAll()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 30);
            _tracking.LockItem(ProcessB, IT.Resource, "Iron", 20);

            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(50));
        }

        [Test]
        public void GetLockedQuantity_UnknownItem_ReturnsZero()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Gold"), Is.EqualTo(0));
        }

        [Test]
        public void GetLockedQuantity_NoLocks_ReturnsZero()
        {
            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(0));
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

            Assert.That(locks.Count, Is.EqualTo(1));
            Assert.That(locks[0].Key, Is.EqualTo(new ItemKey(IT.Resource, "Iron")));
            Assert.That(locks[0].Quantity, Is.EqualTo(10));
        }

        [Test]
        public void GetLocksForProcess_UnknownProcess_ReturnsEmptyList()
        {
            var locks = _tracking.GetLocksForProcess("no-such-process");
            Assert.That(locks, Is.Not.Null);
            Assert.That(locks.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetLocksForProcess_MultipleItems_AllReturned()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 3);

            var locks = _tracking.GetLocksForProcess(ProcessA);

            Assert.That(locks.Count, Is.EqualTo(2));
            Assert.That(locks.Any(l => l.Key.Equals(new ItemKey(IT.Resource, "Iron")) && l.Quantity == 10), Is.True);
            Assert.That(locks.Any(l => l.Key.Equals(new ItemKey(IT.Commodity, "Steel Beams")) && l.Quantity == 3), Is.True);
        }

        // -----------------------------------------------------------------------
        // ClearLocksForProcess
        // -----------------------------------------------------------------------

        [Test]
        public void ClearLocksForProcess_RemovesAllLocksForThatProcess()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.ClearLocksForProcess(ProcessA);

            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(0));
            Assert.That(_tracking.GetLocksForProcess(ProcessA).Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearLocksForProcess_DoesNotAffectOtherProcesses()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.LockItem(ProcessB, IT.Resource, "Iron", 20);

            _tracking.ClearLocksForProcess(ProcessA);

            Assert.That(_tracking.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(20));
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

            Assert.That(restored.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(42));
        }

        [Test]
        public void JsonRoundTrip_MultipleProcessesMultipleItems_AllPreserved()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 10);
            _tracking.LockItem(ProcessA, IT.Commodity, "Steel Beams", 3);
            _tracking.LockItem(ProcessB, IT.Resource, "Gold", 7);

            string json = JsonConvert.SerializeObject(_tracking);
            var restored = JsonConvert.DeserializeObject<LockTracking>(json);

            Assert.That(restored.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(10));
            Assert.That(restored.GetLockedQuantity(IT.Commodity, "Steel Beams"), Is.EqualTo(3));
            Assert.That(restored.GetLockedQuantity(IT.Resource, "Gold"), Is.EqualTo(7));
            Assert.That(restored.GetLocksForProcess(ProcessA).Count(l => l.Key.BaseItemTypeID == "Iron"), Is.EqualTo(1));
        }

        [Test]
        public void JsonRoundTrip_EmptyTracking_ProducesEmptyObject()
        {
            string json = JsonConvert.SerializeObject(_tracking);
            Assert.That(json, Is.EqualTo("{}"));

            var restored = JsonConvert.DeserializeObject<LockTracking>(json);
            Assert.That(restored.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(0));
        }

        [Test]
        public void JsonRoundTrip_AfterClear_ClearedLocksNotRestored()
        {
            _tracking.LockItem(ProcessA, IT.Resource, "Iron", 50);
            _tracking.LockItem(ProcessB, IT.Resource, "Gold", 10);
            _tracking.ClearLocksForProcess(ProcessA);

            string json = JsonConvert.SerializeObject(_tracking);
            var restored = JsonConvert.DeserializeObject<LockTracking>(json);

            Assert.That(restored.GetLockedQuantity(IT.Resource, "Iron"), Is.EqualTo(0));
            Assert.That(restored.GetLockedQuantity(IT.Resource, "Gold"), Is.EqualTo(10));
        }
    }
}
