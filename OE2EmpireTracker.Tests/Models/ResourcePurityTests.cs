using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using PE = OE2EmpireTracker.Models.ResourcePurity.PurityEnum;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ResourcePurityTests
    {
        // -----------------------------------------------------------------------
        // Purities static list
        // -----------------------------------------------------------------------

        [Test]
        public void Purities_ListIsNotEmpty()
        {
            Assert.That(ResourcePurity.Purities.Count > 0, Is.True);
        }

        [Test]
        public void Purities_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourcePurity.Purities[0];
            Assert.That(first.ID, Is.EqualTo(PE.None));
            Assert.That(first.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Purities_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var p in ResourcePurity.Purities.Where(p => p.ID != PE.None))
            {
                Assert.That(
                    string.IsNullOrEmpty(p.Name),
                    Is.False,
                    $"ResourcePurity with ID '{p.ID}' has empty Name");
            }
        }

        [Test]
        public void Purities_NoDuplicateIDs()
        {
            var ids = ResourcePurity.Purities.Select(p => p.ID).ToList();
            Assert.That(
                ids.Count,
                Is.EqualTo(ids.Distinct().Count()),
                "Duplicate purity IDs found");
        }

        [Test]
        public void Purities_NoDuplicateNames()
        {
            var names = ResourcePurity.Purities.Select(p => p.Name).ToList();
            Assert.That(
                names.Count,
                Is.EqualTo(names.Distinct().Count()),
                "Duplicate purity Names found");
        }

        [Test]
        public void Purities_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(PE)).Cast<PE>();
            var listIDs = ResourcePurity.Purities.Select(p => p.ID).ToList();
            foreach (var e in allEnums)
            {
                Assert.That(
                    listIDs.Contains(e),
                    Is.True,
                    $"Purities list missing enum value {e}");
            }
        }

        [Test]
        public void Purities_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourcePurity.Purities.Where(p => p.ID != PE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
            {
                Assert.That(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    Is.LessThanOrEqualTo(0),
                    $"Purities not sorted: '{nonNone[i - 1].Name}' before '{nonNone[i].Name}'");
            }
        }

        // -----------------------------------------------------------------------
        // Refined flag
        // -----------------------------------------------------------------------

        [Test]
        public void Purities_RefinedEntry_HasRefinedFlagTrue()
        {
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.Refined].Refined, Is.True);
        }

        [Test]
        public void Purities_UnrefinedEntries_HaveRefinedFlagFalse()
        {
            var unrefined = ResourcePurity.Purities
                .Where(p => p.ID != PE.None && p.ID != PE.Refined);
            foreach (var p in unrefined)
            {
                Assert.That(
                    p.Refined,
                    Is.False,
                    $"Purity '{p.Name}' should not have Refined = true");
            }
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(PE)).Cast<PE>();
            foreach (var e in allEnums)
            {
                Assert.That(
                    ResourcePurity.ItemTypeMapByEnum.ContainsKey(e),
                    Is.True,
                    $"ItemTypeMapByEnum missing key {e}");
            }
        }

        [Test]
        public void ItemTypeMapByEnum_LookupReturnsCorrectName()
        {
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.Refined].Name, Is.EqualTo("Refined"));
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedHigh].Name, Is.EqualTo("High"));
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedMedium].Name, Is.EqualTo("Medium"));
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedLow].Name, Is.EqualTo("Low"));
        }

        [Test]
        public void ItemTypeMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.That(ResourcePurity.ItemTypeMapByEnum[PE.None].Name, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByString_ContainsAllPurityNames()
        {
            foreach (var p in ResourcePurity.Purities)
            {
                Assert.That(
                    ResourcePurity.ItemTypeMapByString.ContainsKey(p.Name),
                    Is.True,
                    $"ItemTypeMapByString missing key '{p.Name}'");
            }
        }

        [Test]
        public void ItemTypeMapByString_LookupReturnsCorrectID()
        {
            Assert.That(ResourcePurity.ItemTypeMapByString["Refined"].ID, Is.EqualTo(PE.Refined));
            Assert.That(ResourcePurity.ItemTypeMapByString["High"].ID, Is.EqualTo(PE.UnrefinedHigh));
            Assert.That(ResourcePurity.ItemTypeMapByString["Medium"].ID, Is.EqualTo(PE.UnrefinedMedium));
            Assert.That(ResourcePurity.ItemTypeMapByString["Low"].ID, Is.EqualTo(PE.UnrefinedLow));
        }

        [Test]
        public void ItemTypeMapByString_EmptyKeyReturnsNone()
        {
            Assert.That(ResourcePurity.ItemTypeMapByString[string.Empty].ID, Is.EqualTo(PE.None));
        }

        // -----------------------------------------------------------------------
        // Round-trip: enum -> name -> enum
        // -----------------------------------------------------------------------

        [Test]
        public void RoundTrip_EnumToNameToEnum_AllValues()
        {
            var allEnums = Enum.GetValues(typeof(PE)).Cast<PE>();
            foreach (var e in allEnums)
            {
                string name = ResourcePurity.ItemTypeMapByEnum[e].Name;
                PE roundTripped = ResourcePurity.ItemTypeMapByString[name].ID;
                Assert.That(
                    roundTripped,
                    Is.EqualTo(e),
                    $"Round-trip failed for {e}");
            }
        }
    }
}
