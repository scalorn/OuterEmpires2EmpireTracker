using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System;
using System.Linq;
using ITE = OE2EmpireTracker.Data.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class ItemTypeTests
    {
        // -----------------------------------------------------------------------
        // ItemTypes static list
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypes_ListIsNotEmpty()
        {
            Assert.IsTrue(ItemType.ItemTypes.Count > 0);
        }

        [Test]
        public void ItemTypes_FirstEntryIsNoneWithEmptyName()
        {
            var first = ItemType.ItemTypes[0];
            Assert.AreEqual(ITE.None, first.ID);
            Assert.AreEqual(string.Empty, first.Name);
        }

        [Test]
        public void ItemTypes_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var it in ItemType.ItemTypes.Where(t => t.ID != ITE.None))
                Assert.IsFalse(string.IsNullOrEmpty(it.Name),
                    $"ItemType with ID '{it.ID}' has empty Name");
        }

        [Test]
        public void ItemTypes_NoDuplicateIDs()
        {
            var ids = ItemType.ItemTypes.Select(t => t.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate ItemType IDs found");
        }

        [Test]
        public void ItemTypes_NoDuplicateNames()
        {
            var names = ItemType.ItemTypes.Select(t => t.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate ItemType Names found");
        }

        [Test]
        public void ItemTypes_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(ITE)).Cast<ITE>();
            var listIDs = ItemType.ItemTypes.Select(t => t.ID).ToList();
            foreach (var e in allEnums)
                Assert.IsTrue(listIDs.Contains(e), $"ItemTypes list missing enum value {e}");
        }

        [Test]
        public void ItemTypes_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ItemType.ItemTypes.Where(t => t.ID != ITE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.LessOrEqual(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    0,
                    $"ItemTypes not sorted: '{nonNone[i - 1].Name}' should come before '{nonNone[i].Name}'");
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(ITE)).Cast<ITE>();
            foreach (var e in allEnums)
                Assert.IsTrue(ItemType.ItemTypeMapByEnum.ContainsKey(e),
                    $"ItemTypeMapByEnum missing key {e}");
        }

        [Test]
        public void ItemTypeMapByEnum_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Resource", ItemType.ItemTypeMapByEnum[ITE.Resource].Name);
            Assert.AreEqual("Commodity", ItemType.ItemTypeMapByEnum[ITE.Commodity].Name);
            Assert.AreEqual("Blueprint", ItemType.ItemTypeMapByEnum[ITE.Blueprint].Name);
            Assert.AreEqual("Survey", ItemType.ItemTypeMapByEnum[ITE.Survey].Name);
            Assert.AreEqual("Work Detail", ItemType.ItemTypeMapByEnum[ITE.WorkDetail].Name);
        }

        [Test]
        public void ItemTypeMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.AreEqual(string.Empty, ItemType.ItemTypeMapByEnum[ITE.None].Name);
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByString_ContainsAllItemTypeNames()
        {
            foreach (var it in ItemType.ItemTypes)
                Assert.IsTrue(ItemType.ItemTypeMapByString.ContainsKey(it.Name),
                    $"ItemTypeMapByString missing key '{it.Name}'");
        }

        [Test]
        public void ItemTypeMapByString_LookupReturnsCorrectID()
        {
            Assert.AreEqual(ITE.Resource, ItemType.ItemTypeMapByString["Resource"].ID);
            Assert.AreEqual(ITE.Commodity, ItemType.ItemTypeMapByString["Commodity"].ID);
            Assert.AreEqual(ITE.Blueprint, ItemType.ItemTypeMapByString["Blueprint"].ID);
            Assert.AreEqual(ITE.Survey, ItemType.ItemTypeMapByString["Survey"].ID);
            Assert.AreEqual(ITE.WorkDetail, ItemType.ItemTypeMapByString["Work Detail"].ID);
        }

        [Test]
        public void ItemTypeMapByString_EmptyKeyReturnsNone()
        {
            Assert.AreEqual(ITE.None, ItemType.ItemTypeMapByString[string.Empty].ID);
        }

        // -----------------------------------------------------------------------
        // Round-trip: enum -> name -> enum
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_EnumToNameToEnum_AllValues()
        {
            var allEnums = Enum.GetValues(typeof(ITE)).Cast<ITE>();
            foreach (var e in allEnums)
            {
                string name = ItemType.ItemTypeMapByEnum[e].Name;
                ITE roundTripped = ItemType.ItemTypeMapByString[name].ID;
                Assert.AreEqual(e, roundTripped, $"Round-trip failed for {e}");
            }
        }
    }
}
