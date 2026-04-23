using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using ITE = OE2EmpireTracker.Models.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(ItemType.ItemTypes.Count > 0, Is.True);
        }

        [Test]
        public void ItemTypes_FirstEntryIsNoneWithEmptyName()
        {
            var first = ItemType.ItemTypes[0];
            Assert.That(first.ID, Is.EqualTo(ITE.None));
            Assert.That(first.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ItemTypes_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var it in ItemType.ItemTypes.Where(t => t.ID != ITE.None))
                Assert.That(
                    string.IsNullOrEmpty(it.Name),
                    Is.False,
                    $"ItemType with ID '{it.ID}' has empty Name");
        }

        [Test]
        public void ItemTypes_NoDuplicateIDs()
        {
            var ids = ItemType.ItemTypes.Select(t => t.ID).ToList();
            Assert.That(
                ids.Count,
                Is.EqualTo(ids.Distinct().Count()),
                "Duplicate ItemType IDs found");
        }

        [Test]
        public void ItemTypes_NoDuplicateNames()
        {
            var names = ItemType.ItemTypes.Select(t => t.Name).ToList();
            Assert.That(
                names.Count,
                Is.EqualTo(names.Distinct().Count()),
                "Duplicate ItemType Names found");
        }

        [Test]
        public void ItemTypes_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(ITE)).Cast<ITE>();
            var listIDs = ItemType.ItemTypes.Select(t => t.ID).ToList();
            foreach (var e in allEnums)
                Assert.That(
                    listIDs.Contains(e),
                    Is.True,
                    $"ItemTypes list missing enum value {e}");
        }

        [Test]
        public void ItemTypes_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ItemType.ItemTypes.Where(t => t.ID != ITE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.That(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    Is.LessThanOrEqualTo(0),
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
                Assert.That(
                    ItemType.ItemTypeMapByEnum.ContainsKey(e),
                    Is.True,
                    $"ItemTypeMapByEnum missing key {e}");
        }

        [Test]
        public void ItemTypeMapByEnum_LookupReturnsCorrectName()
        {
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.Resource].Name, Is.EqualTo("Resource"));
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.Commodity].Name, Is.EqualTo("Commodity"));
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.Blueprint].Name, Is.EqualTo("Blueprint"));
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.Survey].Name, Is.EqualTo("Survey"));
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.WorkDetail].Name, Is.EqualTo("Work Detail"));
        }

        [Test]
        public void ItemTypeMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.That(ItemType.ItemTypeMapByEnum[ITE.None].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByString_ContainsAllItemTypeNames()
        {
            foreach (var it in ItemType.ItemTypes)
                Assert.That(
                    ItemType.ItemTypeMapByString.ContainsKey(it.Name),
                    Is.True,
                    $"ItemTypeMapByString missing key '{it.Name}'");
        }

        [Test]
        public void ItemTypeMapByString_LookupReturnsCorrectID()
        {
            Assert.That(ItemType.ItemTypeMapByString["Resource"].ID, Is.EqualTo(ITE.Resource));
            Assert.That(ItemType.ItemTypeMapByString["Commodity"].ID, Is.EqualTo(ITE.Commodity));
            Assert.That(ItemType.ItemTypeMapByString["Blueprint"].ID, Is.EqualTo(ITE.Blueprint));
            Assert.That(ItemType.ItemTypeMapByString["Survey"].ID, Is.EqualTo(ITE.Survey));
            Assert.That(ItemType.ItemTypeMapByString["Work Detail"].ID, Is.EqualTo(ITE.WorkDetail));
        }

        [Test]
        public void ItemTypeMapByString_EmptyKeyReturnsNone()
        {
            Assert.That(ItemType.ItemTypeMapByString[string.Empty].ID, Is.EqualTo(ITE.None));
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
                Assert.That(
                    roundTripped,
                    Is.EqualTo(e),
                    $"Round-trip failed for {e}");
            }
        }
    }
}
