using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System.Linq;
using CG = OE2EmpireTracker.Data.CommodityGroup;
using CI = OE2EmpireTracker.Data.CommodityIndustry;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class CommodityTests
    {
        // -----------------------------------------------------------------------
        // Commodity static list
        // -----------------------------------------------------------------------

        [Test]
        public void Commodities_ListIsNotEmpty()
        {
            Assert.IsTrue(Commodity.Commodities.Count > 0);
        }

        [Test]
        public void Commodities_AllEntriesHaveNonEmptyID()
        {
            foreach (var c in Commodity.Commodities)
                Assert.IsFalse(string.IsNullOrEmpty(c.ID), $"Commodity with Name '{c.Name}' has empty ID");
        }

        [Test]
        public void Commodities_AllEntriesHaveNonEmptyName()
        {
            foreach (var c in Commodity.Commodities)
                Assert.IsFalse(string.IsNullOrEmpty(c.Name), $"Commodity with ID '{c.ID}' has empty Name");
        }

        [Test]
        public void Commodities_AllEntriesHaveKnownIndustry()
        {
            foreach (var c in Commodity.Commodities)
                Assert.AreNotEqual(CI.CommodityIndustryEnum.None, c.CommodityIndustry,
                    $"Commodity '{c.Name}' has no industry assigned");
        }

        [Test]
        public void Commodities_AllEntriesHaveKnownGroup()
        {
            foreach (var c in Commodity.Commodities)
                Assert.AreNotEqual(CG.CommodityGroupEnum.None, c.CommodityGroup,
                    $"Commodity '{c.Name}' has no group assigned");
        }

        [Test]
        public void Commodities_NoDuplicateIDs()
        {
            var ids = Commodity.Commodities.Select(c => c.ID).ToList();
            var distinct = ids.Distinct().ToList();
            Assert.AreEqual(distinct.Count, ids.Count, "Duplicate commodity IDs found");
        }

        // -----------------------------------------------------------------------
        // Commodity.ExtendedName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_ContainsCommodityName()
        {
            var commodity = Commodity.Commodities.First();
            StringAssert.Contains(commodity.Name, commodity.ExtendedName);
        }

        [Test]
        public void ExtendedName_ContainsIndustryName()
        {
            var commodity = Commodity.Commodities.First();
            CommodityIndustry industry;
            CI.CommodityIndustryMapByEnum.TryGetValue(commodity.CommodityIndustry, out industry);
            Assert.IsNotNull(industry);
            StringAssert.Contains(industry.Name, commodity.ExtendedName);
        }

        [Test]
        public void ExtendedName_ContainsGroupName()
        {
            var commodity = Commodity.Commodities.First();
            CommodityGroup group;
            CG.CommodityGroupMapByEnum.TryGetValue(commodity.CommodityGroup, out group);
            Assert.IsNotNull(group);
            StringAssert.Contains(group.Name, commodity.ExtendedName);
        }

        [Test]
        public void ExtendedName_EmptyNameReturnsEmpty()
        {
            var commodity = new Commodity();
            Assert.AreEqual("", commodity.ExtendedName);
        }

        [Test]
        public void ExtendedName_IndustryWrappedInParentheses()
        {
            var commodity = Commodity.Commodities.First();
            StringAssert.IsMatch(@"\(.*\)", commodity.ExtendedName);
        }

        [Test]
        public void ExtendedName_GroupWrappedInSquareBrackets()
        {
            var commodity = Commodity.Commodities.First();
            StringAssert.IsMatch(@"\[.*\]", commodity.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // Commodity lookup dictionaries
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceMapByEnum_ContainsAllCommodities()
        {
            foreach (var c in Commodity.Commodities)
            {
                Assert.IsTrue(Commodity.ResourceMapByEnum.ContainsKey(c.ID),
                    $"ResourceMapByEnum missing key '{c.ID}'");
            }
        }

        [Test]
        public void ResourceMapByString_ContainsAllCommodities()
        {
            foreach (var c in Commodity.Commodities)
            {
                Assert.IsTrue(Commodity.ResourceMapByString.ContainsKey(c.Name),
                    $"ResourceMapByString missing key '{c.Name}'");
            }
        }

        [Test]
        public void ResourceMapByEnum_LookupReturnsCorrectCommodity()
        {
            var expected = Commodity.Commodities.First();
            var result = Commodity.ResourceMapByEnum[expected.ID];
            Assert.AreEqual(expected.Name, result.Name);
        }

        [Test]
        public void ResourceMapByString_LookupReturnsCorrectCommodity()
        {
            var expected = Commodity.Commodities.First();
            var result = Commodity.ResourceMapByString[expected.Name];
            Assert.AreEqual(expected.ID, result.ID);
        }

        // -----------------------------------------------------------------------
        // CommodityGroup static data
        // -----------------------------------------------------------------------

        [Test]
        public void CommodityGroup_ListIsNotEmpty()
        {
            Assert.IsTrue(CG.Groups.Count > 0);
        }

        [Test]
        public void CommodityGroup_MapByEnum_ContainsNoneEntry()
        {
            Assert.IsTrue(CG.CommodityGroupMapByEnum.ContainsKey(CG.CommodityGroupEnum.None));
        }

        [Test]
        public void CommodityGroup_MapByEnum_AllGroupsPresent()
        {
            var allEnums = System.Enum.GetValues(typeof(CG.CommodityGroupEnum))
                .Cast<CG.CommodityGroupEnum>();
            foreach (var e in allEnums)
                Assert.IsTrue(CG.CommodityGroupMapByEnum.ContainsKey(e),
                    $"CommodityGroupMapByEnum missing {e}");
        }

        [Test]
        public void CommodityGroup_MapByString_LookupByName()
        {
            var group = CG.Groups.First(g => g.ID != CG.CommodityGroupEnum.None);
            var result = CG.CommodityGroupMapByString[group.Name];
            Assert.AreEqual(group.ID, result.ID);
        }

        // -----------------------------------------------------------------------
        // CommodityIndustry static data
        // -----------------------------------------------------------------------

        [Test]
        public void CommodityIndustry_ListIsNotEmpty()
        {
            Assert.IsTrue(CI.Groups.Count > 0);
        }

        [Test]
        public void CommodityIndustry_MapByEnum_ContainsNoneEntry()
        {
            Assert.IsTrue(CI.CommodityIndustryMapByEnum.ContainsKey(CI.CommodityIndustryEnum.None));
        }

        [Test]
        public void CommodityIndustry_MapByEnum_AllIndustriesPresent()
        {
            var allEnums = System.Enum.GetValues(typeof(CI.CommodityIndustryEnum))
                .Cast<CI.CommodityIndustryEnum>();
            foreach (var e in allEnums)
                Assert.IsTrue(CI.CommodityIndustryMapByEnum.ContainsKey(e),
                    $"CommodityIndustryMapByEnum missing {e}");
        }

        [Test]
        public void CommodityIndustry_MapByString_LookupByName()
        {
            var industry = CI.Groups.First(i => i.ID != CI.CommodityIndustryEnum.None);
            var result = CI.CommodityIndustryMapByString[industry.Name];
            Assert.AreEqual(industry.ID, result.ID);
        }
    }
}
