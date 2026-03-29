using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System;
using System.Linq;
using RGE = OE2EmpireTracker.Data.ResourceGroup.ResourceGroupEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class ResourceGroupTests
    {
        // -----------------------------------------------------------------------
        // Groups static list
        // -----------------------------------------------------------------------

        [Test]
        public void Groups_ListIsNotEmpty()
        {
            Assert.IsTrue(ResourceGroup.Groups.Count > 0);
        }

        [Test]
        public void Groups_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourceGroup.Groups[0];
            Assert.AreEqual(RGE.None, first.ID);
            Assert.AreEqual(string.Empty, first.Name);
        }

        [Test]
        public void Groups_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var g in ResourceGroup.Groups.Where(g => g.ID != RGE.None))
                Assert.IsFalse(string.IsNullOrEmpty(g.Name),
                    $"ResourceGroup with ID '{g.ID}' has empty Name");
        }

        [Test]
        public void Groups_NoDuplicateIDs()
        {
            var ids = ResourceGroup.Groups.Select(g => g.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate ResourceGroup IDs found");
        }

        [Test]
        public void Groups_NoDuplicateNames()
        {
            var names = ResourceGroup.Groups.Select(g => g.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate ResourceGroup Names found");
        }

        [Test]
        public void Groups_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RGE)).Cast<RGE>();
            var listIDs = ResourceGroup.Groups.Select(g => g.ID).ToList();
            foreach (var e in allEnums)
                Assert.IsTrue(listIDs.Contains(e), $"Groups list missing enum value {e}");
        }

        [Test]
        public void Groups_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourceGroup.Groups.Where(g => g.ID != RGE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.LessOrEqual(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    0,
                    $"Groups not sorted: '{nonNone[i - 1].Name}' before '{nonNone[i].Name}'");
        }

        // -----------------------------------------------------------------------
        // Synthetic flag
        // -----------------------------------------------------------------------

        [Test]
        public void Groups_SyntheticGroup_HasSyntheticFlagTrue()
        {
            Assert.IsTrue(ResourceGroup.ResourceGroupMapByEnum[RGE.Synthetic].Synthetic);
        }

        [Test]
        public void Groups_NonSyntheticGroups_HaveSyntheticFlagFalse()
        {
            var nonSynthetic = ResourceGroup.Groups
                .Where(g => g.ID != RGE.None && g.ID != RGE.Synthetic);
            foreach (var g in nonSynthetic)
                Assert.IsFalse(g.Synthetic, $"ResourceGroup '{g.Name}' should not be Synthetic");
        }

        // -----------------------------------------------------------------------
        // ResourceGroupMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceGroupMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RGE)).Cast<RGE>();
            foreach (var e in allEnums)
                Assert.IsTrue(ResourceGroup.ResourceGroupMapByEnum.ContainsKey(e),
                    $"ResourceGroupMapByEnum missing key {e}");
        }

        [Test]
        public void ResourceGroupMapByEnum_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Metallic", ResourceGroup.ResourceGroupMapByEnum[RGE.Metallic].Name);
            Assert.AreEqual("Organic", ResourceGroup.ResourceGroupMapByEnum[RGE.Organic].Name);
            Assert.AreEqual("Synthetic", ResourceGroup.ResourceGroupMapByEnum[RGE.Synthetic].Name);
        }

        [Test]
        public void ResourceGroupMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.AreEqual(string.Empty, ResourceGroup.ResourceGroupMapByEnum[RGE.None].Name);
        }

        // -----------------------------------------------------------------------
        // ResourceGroupMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceGroupMapByString_ContainsAllGroupNames()
        {
            foreach (var g in ResourceGroup.Groups)
                Assert.IsTrue(ResourceGroup.ResourceGroupMapByString.ContainsKey(g.Name),
                    $"ResourceGroupMapByString missing key '{g.Name}'");
        }

        [Test]
        public void ResourceGroupMapByString_LookupReturnsCorrectID()
        {
            Assert.AreEqual(RGE.Metallic, ResourceGroup.ResourceGroupMapByString["Metallic"].ID);
            Assert.AreEqual(RGE.Organic, ResourceGroup.ResourceGroupMapByString["Organic"].ID);
            Assert.AreEqual(RGE.Synthetic, ResourceGroup.ResourceGroupMapByString["Synthetic"].ID);
        }

        [Test]
        public void ResourceGroupMapByString_EmptyKeyReturnsNone()
        {
            Assert.AreEqual(RGE.None, ResourceGroup.ResourceGroupMapByString[string.Empty].ID);
        }

        // -----------------------------------------------------------------------
        // Round-trip: enum -> name -> enum
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_EnumToNameToEnum_AllValues()
        {
            var allEnums = Enum.GetValues(typeof(RGE)).Cast<RGE>();
            foreach (var e in allEnums)
            {
                string name = ResourceGroup.ResourceGroupMapByEnum[e].Name;
                RGE roundTripped = ResourceGroup.ResourceGroupMapByString[name].ID;
                Assert.AreEqual(e, roundTripped, $"Round-trip failed for {e}");
            }
        }
    }
}
