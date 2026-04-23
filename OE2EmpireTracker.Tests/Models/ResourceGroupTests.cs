using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using RGE = OE2EmpireTracker.Models.ResourceGroup.ResourceGroupEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(ResourceGroup.Groups.Count > 0, Is.True);
        }

        [Test]
        public void Groups_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourceGroup.Groups[0];
            Assert.That(first.ID, Is.EqualTo(RGE.None));
            Assert.That(first.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Groups_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var g in ResourceGroup.Groups.Where(g => g.ID != RGE.None))
            {
                Assert.That(
                    string.IsNullOrEmpty(g.Name),
                    Is.False,
                    $"ResourceGroup with ID '{g.ID}' has empty Name");
            }
        }

        [Test]
        public void Groups_NoDuplicateIDs()
        {
            var ids = ResourceGroup.Groups.Select(g => g.ID).ToList();
            Assert.That(
                ids.Count,
                Is.EqualTo(ids.Distinct().Count()),
                "Duplicate ResourceGroup IDs found");
        }

        [Test]
        public void Groups_NoDuplicateNames()
        {
            var names = ResourceGroup.Groups.Select(g => g.Name).ToList();
            Assert.That(
                names.Count,
                Is.EqualTo(names.Distinct().Count()),
                "Duplicate ResourceGroup Names found");
        }

        [Test]
        public void Groups_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RGE)).Cast<RGE>();
            var listIDs = ResourceGroup.Groups.Select(g => g.ID).ToList();
            foreach (var e in allEnums)
            {
                Assert.That(
                    listIDs.Contains(e),
                    Is.True,
                    $"Groups list missing enum value {e}");
            }
        }

        [Test]
        public void Groups_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourceGroup.Groups.Where(g => g.ID != RGE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
            {
                Assert.That(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    Is.LessThanOrEqualTo(0),
                    $"Groups not sorted: '{nonNone[i - 1].Name}' before '{nonNone[i].Name}'");
            }
        }

        // -----------------------------------------------------------------------
        // Synthetic flag
        // -----------------------------------------------------------------------

        [Test]
        public void Groups_SyntheticGroup_HasSyntheticFlagTrue()
        {
            Assert.That(ResourceGroup.ResourceGroupMapByEnum[RGE.Synthetic].Synthetic, Is.True);
        }

        [Test]
        public void Groups_NonSyntheticGroups_HaveSyntheticFlagFalse()
        {
            var nonSynthetic = ResourceGroup.Groups
                .Where(g => g.ID != RGE.None && g.ID != RGE.Synthetic);
            foreach (var g in nonSynthetic)
            {
                Assert.That(
                    g.Synthetic,
                    Is.False,
                    $"ResourceGroup '{g.Name}' should not be Synthetic");
            }
        }

        // -----------------------------------------------------------------------
        // ResourceGroupMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceGroupMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RGE)).Cast<RGE>();
            foreach (var e in allEnums)
            {
                Assert.That(
                    ResourceGroup.ResourceGroupMapByEnum.ContainsKey(e),
                    Is.True,
                    $"ResourceGroupMapByEnum missing key {e}");
            }
        }

        [Test]
        public void ResourceGroupMapByEnum_LookupReturnsCorrectName()
        {
            Assert.That(ResourceGroup.ResourceGroupMapByEnum[RGE.Metallic].Name, Is.EqualTo("Metallic"));
            Assert.That(ResourceGroup.ResourceGroupMapByEnum[RGE.Organic].Name, Is.EqualTo("Organic"));
            Assert.That(ResourceGroup.ResourceGroupMapByEnum[RGE.Synthetic].Name, Is.EqualTo("Synthetic"));
        }

        [Test]
        public void ResourceGroupMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.That(ResourceGroup.ResourceGroupMapByEnum[RGE.None].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ResourceGroupMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceGroupMapByString_ContainsAllGroupNames()
        {
            foreach (var g in ResourceGroup.Groups)
            {
                Assert.That(
                    ResourceGroup.ResourceGroupMapByString.ContainsKey(g.Name),
                    Is.True,
                    $"ResourceGroupMapByString missing key '{g.Name}'");
            }
        }

        [Test]
        public void ResourceGroupMapByString_LookupReturnsCorrectID()
        {
            Assert.That(ResourceGroup.ResourceGroupMapByString["Metallic"].ID, Is.EqualTo(RGE.Metallic));
            Assert.That(ResourceGroup.ResourceGroupMapByString["Organic"].ID, Is.EqualTo(RGE.Organic));
            Assert.That(ResourceGroup.ResourceGroupMapByString["Synthetic"].ID, Is.EqualTo(RGE.Synthetic));
        }

        [Test]
        public void ResourceGroupMapByString_EmptyKeyReturnsNone()
        {
            Assert.That(ResourceGroup.ResourceGroupMapByString[string.Empty].ID, Is.EqualTo(RGE.None));
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
                Assert.That(
                    roundTripped,
                    Is.EqualTo(e),
                    $"Round-trip failed for {e}");
            }
        }
    }
}
