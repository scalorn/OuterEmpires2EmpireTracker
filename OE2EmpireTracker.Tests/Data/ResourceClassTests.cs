using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System;
using System.Linq;
using RCE = OE2EmpireTracker.Data.ResourceClass.ResourceClassEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class ResourceClassTests
    {
        // -----------------------------------------------------------------------
        // Classes static list
        // -----------------------------------------------------------------------

        [Test]
        public void Classes_ListIsNotEmpty()
        {
            Assert.IsTrue(ResourceClass.Classes.Count > 0);
        }

        [Test]
        public void Classes_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourceClass.Classes[0];
            Assert.AreEqual(RCE.None, first.ID);
            Assert.AreEqual(string.Empty, first.Name);
        }

        [Test]
        public void Classes_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var c in ResourceClass.Classes.Where(c => c.ID != RCE.None))
                Assert.IsFalse(string.IsNullOrEmpty(c.Name),
                    $"ResourceClass with ID '{c.ID}' has empty Name");
        }

        [Test]
        public void Classes_NoDuplicateIDs()
        {
            var ids = ResourceClass.Classes.Select(c => c.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate ResourceClass IDs found");
        }

        [Test]
        public void Classes_NoDuplicateNames()
        {
            var names = ResourceClass.Classes.Select(c => c.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate ResourceClass Names found");
        }

        [Test]
        public void Classes_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RCE)).Cast<RCE>();
            var listIDs = ResourceClass.Classes.Select(c => c.ID).ToList();
            foreach (var e in allEnums)
                Assert.IsTrue(listIDs.Contains(e), $"Classes list missing enum value {e}");
        }

        [Test]
        public void Classes_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourceClass.Classes.Where(c => c.ID != RCE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.LessOrEqual(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    0,
                    $"Classes not sorted: '{nonNone[i - 1].Name}' before '{nonNone[i].Name}'");
        }

        // -----------------------------------------------------------------------
        // ClassMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ClassMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RCE)).Cast<RCE>();
            foreach (var e in allEnums)
                Assert.IsTrue(ResourceClass.ClassMapByEnum.ContainsKey(e),
                    $"ClassMapByEnum missing key {e}");
        }

        [Test]
        public void ClassMapByEnum_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Common Elements",    ResourceClass.ClassMapByEnum[RCE.CommonElements].Name);
            Assert.AreEqual("Uncommon Elements",  ResourceClass.ClassMapByEnum[RCE.UncommonElements].Name);
            Assert.AreEqual("Rare Elements",      ResourceClass.ClassMapByEnum[RCE.RareElements].Name);
            Assert.AreEqual("Very Rare Elements", ResourceClass.ClassMapByEnum[RCE.VeryRareElements].Name);
            Assert.AreEqual("Synthetic Elements", ResourceClass.ClassMapByEnum[RCE.SyntheticElements].Name);
        }

        [Test]
        public void ClassMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.AreEqual(string.Empty, ResourceClass.ClassMapByEnum[RCE.None].Name);
        }

        // -----------------------------------------------------------------------
        // ClassMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ClassMapByString_ContainsAllClassNames()
        {
            foreach (var c in ResourceClass.Classes)
                Assert.IsTrue(ResourceClass.ClassMapByString.ContainsKey(c.Name),
                    $"ClassMapByString missing key '{c.Name}'");
        }

        [Test]
        public void ClassMapByString_LookupReturnsCorrectID()
        {
            Assert.AreEqual(RCE.CommonElements,    ResourceClass.ClassMapByString["Common Elements"].ID);
            Assert.AreEqual(RCE.UncommonElements,  ResourceClass.ClassMapByString["Uncommon Elements"].ID);
            Assert.AreEqual(RCE.RareElements,      ResourceClass.ClassMapByString["Rare Elements"].ID);
            Assert.AreEqual(RCE.VeryRareElements,  ResourceClass.ClassMapByString["Very Rare Elements"].ID);
            Assert.AreEqual(RCE.SyntheticElements, ResourceClass.ClassMapByString["Synthetic Elements"].ID);
        }

        [Test]
        public void ClassMapByString_EmptyKeyReturnsNone()
        {
            Assert.AreEqual(RCE.None, ResourceClass.ClassMapByString[string.Empty].ID);
        }

        // -----------------------------------------------------------------------
        // Round-trip: enum -> name -> enum
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_EnumToNameToEnum_AllValues()
        {
            var allEnums = Enum.GetValues(typeof(RCE)).Cast<RCE>();
            foreach (var e in allEnums)
            {
                string name = ResourceClass.ClassMapByEnum[e].Name;
                RCE roundTripped = ResourceClass.ClassMapByString[name].ID;
                Assert.AreEqual(e, roundTripped, $"Round-trip failed for {e}");
            }
        }
    }
}
