using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System;
using System.Linq;
using PE = OE2EmpireTracker.Data.ResourcePurity.PurityEnum;

namespace OE2EmpireTracker.Tests.Data
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
            Assert.IsTrue(ResourcePurity.Purities.Count > 0);
        }

        [Test]
        public void Purities_FirstEntryIsNoneWithEmptyName()
        {
            var first = ResourcePurity.Purities[0];
            Assert.AreEqual(PE.None, first.ID);
            Assert.AreEqual(string.Empty, first.Name);
        }

        [Test]
        public void Purities_AllNonNoneEntriesHaveNonEmptyName()
        {
            foreach (var p in ResourcePurity.Purities.Where(p => p.ID != PE.None))
                Assert.IsFalse(string.IsNullOrEmpty(p.Name),
                    $"ResourcePurity with ID '{p.ID}' has empty Name");
        }

        [Test]
        public void Purities_NoDuplicateIDs()
        {
            var ids = ResourcePurity.Purities.Select(p => p.ID).ToList();
            Assert.AreEqual(ids.Distinct().Count(), ids.Count, "Duplicate purity IDs found");
        }

        [Test]
        public void Purities_NoDuplicateNames()
        {
            var names = ResourcePurity.Purities.Select(p => p.Name).ToList();
            Assert.AreEqual(names.Distinct().Count(), names.Count, "Duplicate purity Names found");
        }

        [Test]
        public void Purities_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(PE)).Cast<PE>();
            var listIDs = ResourcePurity.Purities.Select(p => p.ID).ToList();
            foreach (var e in allEnums)
                Assert.IsTrue(listIDs.Contains(e), $"Purities list missing enum value {e}");
        }

        [Test]
        public void Purities_NonNoneEntriesAreSortedAlphabetically()
        {
            var nonNone = ResourcePurity.Purities.Where(p => p.ID != PE.None).ToList();
            for (int i = 1; i < nonNone.Count; i++)
                Assert.LessOrEqual(
                    string.Compare(nonNone[i - 1].Name, nonNone[i].Name, StringComparison.Ordinal),
                    0,
                    $"Purities not sorted: '{nonNone[i - 1].Name}' before '{nonNone[i].Name}'");
        }

        // -----------------------------------------------------------------------
        // Refined flag
        // -----------------------------------------------------------------------

        [Test]
        public void Purities_RefinedEntry_HasRefinedFlagTrue()
        {
            Assert.IsTrue(ResourcePurity.ItemTypeMapByEnum[PE.Refined].Refined);
        }

        [Test]
        public void Purities_UnrefinedEntries_HaveRefinedFlagFalse()
        {
            var unrefined = ResourcePurity.Purities
                .Where(p => p.ID != PE.None && p.ID != PE.Refined);
            foreach (var p in unrefined)
                Assert.IsFalse(p.Refined, $"Purity '{p.Name}' should not have Refined = true");
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByEnum
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByEnum_ContainsAllEnumValues()
        {
            var allEnums = Enum.GetValues(typeof(PE)).Cast<PE>();
            foreach (var e in allEnums)
                Assert.IsTrue(ResourcePurity.ItemTypeMapByEnum.ContainsKey(e),
                    $"ItemTypeMapByEnum missing key {e}");
        }

        [Test]
        public void ItemTypeMapByEnum_LookupReturnsCorrectName()
        {
            Assert.AreEqual("Refined", ResourcePurity.ItemTypeMapByEnum[PE.Refined].Name);
            Assert.AreEqual("High", ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedHigh].Name);
            Assert.AreEqual("Medium", ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedMedium].Name);
            Assert.AreEqual("Low", ResourcePurity.ItemTypeMapByEnum[PE.UnrefinedLow].Name);
        }

        [Test]
        public void ItemTypeMapByEnum_NoneEntryHasEmptyName()
        {
            Assert.AreEqual(string.Empty, ResourcePurity.ItemTypeMapByEnum[PE.None].Name);
        }

        // -----------------------------------------------------------------------
        // ItemTypeMapByString
        // -----------------------------------------------------------------------

        [Test]
        public void ItemTypeMapByString_ContainsAllPurityNames()
        {
            foreach (var p in ResourcePurity.Purities)
                Assert.IsTrue(ResourcePurity.ItemTypeMapByString.ContainsKey(p.Name),
                    $"ItemTypeMapByString missing key '{p.Name}'");
        }

        [Test]
        public void ItemTypeMapByString_LookupReturnsCorrectID()
        {
            Assert.AreEqual(PE.Refined, ResourcePurity.ItemTypeMapByString["Refined"].ID);
            Assert.AreEqual(PE.UnrefinedHigh, ResourcePurity.ItemTypeMapByString["High"].ID);
            Assert.AreEqual(PE.UnrefinedMedium, ResourcePurity.ItemTypeMapByString["Medium"].ID);
            Assert.AreEqual(PE.UnrefinedLow, ResourcePurity.ItemTypeMapByString["Low"].ID);
        }

        [Test]
        public void ItemTypeMapByString_EmptyKeyReturnsNone()
        {
            Assert.AreEqual(PE.None, ResourcePurity.ItemTypeMapByString[string.Empty].ID);
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
                Assert.AreEqual(e, roundTripped, $"Round-trip failed for {e}");
            }
        }
    }
}
