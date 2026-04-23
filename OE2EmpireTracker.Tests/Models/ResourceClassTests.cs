using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using RCE = OE2EmpireTracker.Models.ResourceClass.ResourceClassEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(ResourceClass.Classes.Count > 0, Is.True);
        }

        [Test]
        public void Classes_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourceClass.Classes[0];
            Assert.That(first.ID, Is.EqualTo(RCE.None));
            Assert.That(first.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Classes_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var c in ResourceClass.Classes.Where(c => c.ID != RCE.None))
                Assert.That(string.IsNullOrEmpty(c.Name), Is.False,
                    $"ResourceClass with ID '{c.ID}' has empty Name");
        }

        [Test]
        public void Classes_NoDuplicateIDs()
        {
            var ids = ResourceClass.Classes.Select(c => c.ID).ToList();
            Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()),
                    "Duplicate ResourceClass IDs found");
        }

        [Test]
        public void Classes_NoDuplicateNames()
        {
            var names = ResourceClass.Classes.Select(c => c.Name).ToList();
            Assert.That(names.Count, Is.EqualTo(names.Distinct().Count()),
                    "Duplicate ResourceClass Names found");
        }

        [Test]
        public void Classes_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RCE)).Cast<RCE>();
            var listIDs = ResourceClass.Classes.Select(c => c.ID).ToList();
            foreach (var e in allEnums)
                Assert.That(listIDs.Contains(e), Is.True,
                    $"Classes list missing enum value {e}");
        }

        [Test]
        public void Classes_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourceClass.Classes.Where(c => c.ID != RCE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.That(string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal), Is.LessThanOrEqualTo(0),
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
                Assert.That(ResourceClass.ClassMapByEnum.ContainsKey(e), Is.True,
                    $"ClassMapByEnum missing key {e}");
        }

        [Test]
        public void ClassMapByEnum_LookupReturnsCorrectName()
        {
            Assert.That(ResourceClass.ClassMapByEnum[RCE.CommonElements].Name, Is.EqualTo("Common Elements"));
            Assert.That(ResourceClass.ClassMapByEnum[RCE.UncommonElements].Name, Is.EqualTo("Uncommon Elements"));
            Assert.That(ResourceClass.ClassMapByEnum[RCE.RareElements].Name, Is.EqualTo("Rare Elements"));
            Assert.That(ResourceClass.ClassMapByEnum[RCE.VeryRareElements].Name, Is.EqualTo("Very Rare Elements"));
            Assert.That(ResourceClass.ClassMapByEnum[RCE.SyntheticElements].Name, Is.EqualTo("Synthetic Elements"));
        }

        [Test]
        public void ClassMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.That(ResourceClass.ClassMapByEnum[RCE.None].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ClassMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ClassMapByString_ContainsAllClassNames()
        {
            foreach (var c in ResourceClass.Classes)
                Assert.That(ResourceClass.ClassMapByString.ContainsKey(c.Name), Is.True,
                    $"ClassMapByString missing key '{c.Name}'");
        }

        [Test]
        public void ClassMapByString_LookupReturnsCorrectID()
        {
            Assert.That(ResourceClass.ClassMapByString["Common Elements"].ID, Is.EqualTo(RCE.CommonElements));
            Assert.That(ResourceClass.ClassMapByString["Uncommon Elements"].ID, Is.EqualTo(RCE.UncommonElements));
            Assert.That(ResourceClass.ClassMapByString["Rare Elements"].ID, Is.EqualTo(RCE.RareElements));
            Assert.That(ResourceClass.ClassMapByString["Very Rare Elements"].ID, Is.EqualTo(RCE.VeryRareElements));
            Assert.That(ResourceClass.ClassMapByString["Synthetic Elements"].ID, Is.EqualTo(RCE.SyntheticElements));
        }

        [Test]
        public void ClassMapByString_EmptyKeyReturnsNone()
        {
            Assert.That(ResourceClass.ClassMapByString[string.Empty].ID, Is.EqualTo(RCE.None));
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
                Assert.That(roundTripped, Is.EqualTo(e),
                    $"Round-trip failed for {e}");
            }
        }
    }
}
