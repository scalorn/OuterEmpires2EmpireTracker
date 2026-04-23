using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using RE = OE2EmpireTracker.Models.Resource.ResourceEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(Resource.Resources.Count > 0, Is.True);
        }

        [Test]
        public void Resources_FirstEntryIsNoneWithEmptyName()
        {
            var first = Resource.Resources[0];
            Assert.That(first.ID, Is.EqualTo(RE.None));
            Assert.That(first.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Resources_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.That(
                    string.IsNullOrEmpty(r.Name),
                    Is.False,
                    $"Resource with ID '{r.ID}' has empty Name");
        }

        [Test]
        public void Resources_NoDuplicateIDs()
        {
            var ids = Resource.Resources.Select(r => r.ID).ToList();
            Assert.That(
                ids.Count,
                Is.EqualTo(ids.Distinct().Count()),
                "Duplicate Resource IDs found");
        }

        [Test]
        public void Resources_NoDuplicateNames()
        {
            var names = Resource.Resources.Select(r => r.Name).ToList();
            Assert.That(
                names.Count,
                Is.EqualTo(names.Distinct().Count()),
                "Duplicate Resource Names found");
        }

        [Test]
        public void Resources_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(RE)).Cast<RE>();
            var listIDs = Resource.Resources.Select(r => r.ID).ToList();
            foreach (var e in allEnums)
                Assert.That(
                    listIDs.Contains(e),
                    Is.True,
                    $"Resources list missing enum value {e}");
        }

        [Test]
        public void Resources_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = Resource.Resources.Where(r => r.ID != RE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.That(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal) <= 0,
                    Is.True,
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
                Assert.That(
                    Resource.ResourceMapByEnum.ContainsKey(e),
                    Is.True,
                    $"Map missing enum {e}");
        }

        [Test]
        public void ResourceMapByEnum_LookupReturnsCorrectName()
        {
            Assert.That(Resource.ResourceMapByEnum[RE.Halogens].Name, Is.EqualTo("Halogens"));
        }

        [Test]
        public void ResourceMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.That(Resource.ResourceMapByEnum[RE.None].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ResourceMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceMapByString_ContainsAllResourceNames()
        {
            foreach (var r in Resource.Resources)
                Assert.That(
                    Resource.ResourceMapByString.ContainsKey(r.Name),
                    Is.True,
                    $"Map missing name '{r.Name}'");
        }

        [Test]
        public void ResourceMapByString_LookupReturnsCorrectID()
        {
            Assert.That(Resource.ResourceMapByString["Heavy Trans-Metals"].ID, Is.EqualTo(RE.HeavyTransMetals));
        }

        [Test]
        public void ResourceMapByString_EmptyKeyReturnsNone()
        {
            Assert.That(Resource.ResourceMapByString[string.Empty].ID, Is.EqualTo(RE.None));
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
                Assert.That(
                    byString.ID,
                    Is.EqualTo(r.ID),
                    $"Round-trip failed for {r.ID}");
            }
        }

        // -----------------------------------------------------------------------
        // ResourceGroup and ResourceClass assignments
        // -----------------------------------------------------------------------

        [Test]
        public void Resources_AllNonNoneEntriesHaveValidResourceGroup()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.That(
                    r.ResourceGroup,
                    Is.Not.EqualTo(ResourceGroup.ResourceGroupEnum.None),
                    $"Resource '{r.Name}' has None ResourceGroup");
        }

        [Test]
        public void Resources_AllNonNoneEntriesHaveValidResourceClass()
        {
            foreach (var r in Resource.Resources.Where(r => r.ID != RE.None))
                Assert.That(
                    r.ResourceClass,
                    Is.Not.EqualTo(ResourceClass.ResourceClassEnum.None),
                    $"Resource '{r.Name}' has None ResourceClass");
        }

        [Test]
        public void Resources_SyntheticResources_HaveSyntheticGroup()
        {
            var synthetics = Resource.Resources.Where(r => r.Name.StartsWith("S1.") || r.Name.StartsWith("S2.")).ToList();
            Assert.That(
                synthetics.Count > 0,
                Is.True,
                "Expected at least one synthetic resource");
            foreach (var r in synthetics)
                Assert.That(
                    r.ResourceGroup,
                    Is.EqualTo(ResourceGroup.ResourceGroupEnum.Synthetic),
                    $"Resource '{r.Name}' should be Synthetic group");
        }
    }
}
