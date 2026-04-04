using NUnit.Framework;
using OE2EmpireTracker.Data;
using System;
using System.Linq;
using RE = OE2EmpireTracker.Data.Resource.ResourceEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class ResourceTests
    {
        // -----------------------------------------------------------------------
        // Resources static list
        // -----------------------------------------------------------------------

        [Test]
        public void Resources_ListIsNotEmpty()
        {
            Assert.IsTrue(Resource.Resources.Count > 0);
        }

        [Test]
        public void Resources_FirstEntryIsNoneWithEmptyName()
        {
            var first = Resource.Resources[0];
            Assert.AreEqual(RE.None, first.ID);
            Assert.AreEqual(string.Empty, first.Name);
        }

        [Test]
        public void Resources_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.IsFalse(string.IsNullOrEmpty(r.Name),
                    $"Resource with ID '{r.ID}' has empty Name");
        }

        [Test]
        public void Resources_NoDuplicateIDs()
        {
            var ids = Resource.Resources.Select(r => r.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate Resource IDs found");
        }

        [Test]
        public void Resources_NoDuplicateNames()
        {
            var names = Resource.Resources.Select(r => r.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate Resource Names found");
        }

        [Test]
        public void Resources_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RE)).Cast<RE>();
            var listIDs = Resource.Resources.Select(r => r.ID).ToList();
            foreach (var e in allEnums)
                Assert.IsTrue(listIDs.Contains(e), $"Resources list missing enum value {e}");
        }

        [Test]
        public void Resources_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = Resource.Resources.Where(r => r.ID != RE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.IsTrue(string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal) <= 0,
                    $"'{nonNone[i - 1].Name}' should come before '{nonNone[i].Name}'");
        }

        // -----------------------------------------------------------------------
        // ResourceMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RE)).Cast<RE>();
            foreach (var e in allEnums)
                Assert.IsTrue(Resource.ResourceMapByEnum.ContainsKey(e), $"Map missing enum {e}");
        }

        [Test]
        public void ResourceMapByEnum_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Halogens", Resource.ResourceMapByEnum[RE.Halogens].Name);
        }

        [Test]
        public void ResourceMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.AreEqual(string.Empty, Resource.ResourceMapByEnum[RE.None].Name);
        }

        // -----------------------------------------------------------------------
        // ResourceMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceMapByString_ContainsAllResourceNames()
        {
            foreach (var r in Resource.Resources)
                Assert.IsTrue(Resource.ResourceMapByString.ContainsKey(r.Name),
                    $"Map missing name '{r.Name}'");
        }

        [Test]
        public void ResourceMapByString_LookupReturnsCorrectID()
        {
            Assert.AreEqual(RE.HeavyTransMetals, Resource.ResourceMapByString["Heavy Trans-Metals"].ID);
        }

        [Test]
        public void ResourceMapByString_EmptyKeyReturnsNone()
        {
            Assert.AreEqual(RE.None, Resource.ResourceMapByString[""].ID);
        }

        // -----------------------------------------------------------------------
        // Round-trip
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_EnumToNameToEnum_AllValues()
        {
            foreach (var r in Resource.Resources)
            {
                var byEnum = Resource.ResourceMapByEnum[r.ID];
                var byString = Resource.ResourceMapByString[byEnum.Name];
                Assert.AreEqual(r.ID, byString.ID, $"Round-trip failed for {r.ID}");
            }
        }

        // -----------------------------------------------------------------------
        // ResourceGroup and ResourceClass assignments
        // -----------------------------------------------------------------------

        [Test]
        public void Resources_AllNonNoneEntriesHaveValidResourceGroup()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.AreNotEqual(ResourceGroup.ResourceGroupEnum.None, r.ResourceGroup,
                    $"Resource '{r.Name}' has None ResourceGroup");
        }

        [Test]
        public void Resources_AllNonNoneEntriesHaveValidResourceClass()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.AreNotEqual(ResourceClass.ResourceClassEnum.None, r.ResourceClass,
                    $"Resource '{r.Name}' has None ResourceClass");
        }

        [Test]
        public void Resources_SyntheticResources_HaveSyntheticGroup()
        {
            var synthetics = Resource.Resources.Where(r => r.Name.StartsWith("S1.") || r.Name.StartsWith("S2.")).ToList();
            Assert.IsTrue(synthetics.Count > 0, "Expected at least one synthetic resource");
            foreach (var r in synthetics)
                Assert.AreEqual(ResourceGroup.ResourceGroupEnum.Synthetic, r.ResourceGroup,
                    $"Resource '{r.Name}' should be Synthetic group");
        }
    }
}
