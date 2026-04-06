using NUnit.Framework;
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
            Assert.That(Commodity.Commodities.Count > 0, Is.True);
        }

        [Test]
        public void Commodities_AllEntriesHaveNonEmptyID()
        {
            foreach (var c in Commodity.Commodities)
                Assert.That(string.IsNullOrEmpty(c.ID), Is.False,
                    $"Commodity with Name '{c.Name}' has empty ID");
        }

        [Test]
        public void Commodities_AllEntriesHaveNonEmptyName()
        {
            foreach (var c in Commodity.Commodities)
                Assert.That(string.IsNullOrEmpty(c.Name), Is.False,
                    $"Commodity with ID '{c.ID}' has empty Name");
        }

        [Test]
        public void Commodities_AllEntriesHaveKnownIndustry()
        {
            foreach (var c in Commodity.Commodities)
                Assert.That(c.CommodityIndustry, Is.Not.EqualTo(CI.CommodityIndustryEnum.None),
                    $"Commodity '{c.Name}' has no industry assigned");
        }

        [Test]
        public void Commodities_AllEntriesHaveKnownGroup()
        {
            foreach (var c in Commodity.Commodities)
                Assert.That(c.CommodityGroup, Is.Not.EqualTo(CG.CommodityGroupEnum.None),
                    $"Commodity '{c.Name}' has no group assigned");
        }

        [Test]
        public void Commodities_NoDuplicateIDs()
        {
            var ids = Commodity.Commodities.Select(c => c.ID).ToList();
            var distinct = ids.Distinct().ToList();
            Assert.That(ids.Count, Is.EqualTo(distinct.Count),
                    "Duplicate commodity IDs found");
        }

        // -----------------------------------------------------------------------
        // Commodity.ExtendedName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_ContainsCommodityName()
        {
            var commodity = Commodity.Commodities.First();
            Assert.That(commodity.ExtendedName, Does.Contain(commodity.Name));
        }

        [Test]
        public void ExtendedName_ContainsIndustryName()
        {
            var commodity = Commodity.Commodities.First();
            CommodityIndustry industry;
            CI.CommodityIndustryMapByEnum.TryGetValue(commodity.CommodityIndustry, out industry);
            Assert.That(industry, Is.Not.Null);
            Assert.That(commodity.ExtendedName, Does.Contain(industry.Name));
        }

        [Test]
        public void ExtendedName_ContainsGroupName()
        {
            var commodity = Commodity.Commodities.First();
            CommodityGroup group;
            CG.CommodityGroupMapByEnum.TryGetValue(commodity.CommodityGroup, out group);
            Assert.That(group, Is.Not.Null);
            Assert.That(commodity.ExtendedName, Does.Contain(group.Name));
        }

        [Test]
        public void ExtendedName_EmptyNameReturnsEmpty()
        {
            var commodity = new Commodity();
            Assert.That(commodity.ExtendedName, Is.EqualTo(""));
        }

        [Test]
        public void ExtendedName_IndustryWrappedInParentheses()
        {
            var commodity = Commodity.Commodities.First();
            Assert.That(commodity.ExtendedName, Does.Match(@"\(.*\)"));
        }

        [Test]
        public void ExtendedName_GroupWrappedInSquareBrackets()
        {
            var commodity = Commodity.Commodities.First();
            Assert.That(commodity.ExtendedName, Does.Match(@"\[.*\]"));
        }

        // -----------------------------------------------------------------------
        // Commodity lookup dictionaries
        // -----------------------------------------------------------------------

        [Test]
        public void ResourceMapByEnum_ContainsAllCommodities()
        {
            foreach (var c in Commodity.Commodities)
            {
                Assert.That(Commodity.ResourceMapByEnum.ContainsKey(c.ID), Is.True,
                    $"ResourceMapByEnum missing key '{c.ID}'");
            }
        }

        [Test]
        public void ResourceMapByString_ContainsAllCommodities()
        {
            foreach (var c in Commodity.Commodities)
            {
                Assert.That(Commodity.ResourceMapByString.ContainsKey(c.Name), Is.True,
                    $"ResourceMapByString missing key '{c.Name}'");
            }
        }

        [Test]
        public void ResourceMapByEnum_LookupReturnsCorrectCommodity()
        {
            var expected = Commodity.Commodities.First();
            var result = Commodity.ResourceMapByEnum[expected.ID];
            Assert.That(result.Name, Is.EqualTo(expected.Name));
        }

        [Test]
        public void ResourceMapByString_LookupReturnsCorrectCommodity()
        {
            var expected = Commodity.Commodities.First();
            var result = Commodity.ResourceMapByString[expected.Name];
            Assert.That(result.ID, Is.EqualTo(expected.ID));
        }

        // -----------------------------------------------------------------------
        // CommodityGroup static data
        // -----------------------------------------------------------------------

        [Test]
        public void CommodityGroup_ListIsNotEmpty()
        {
            Assert.That(CG.Groups.Count > 0, Is.True);
        }

        [Test]
        public void CommodityGroup_MapByEnum_ContainsNoneEntry()
        {
            Assert.That(CG.CommodityGroupMapByEnum.ContainsKey(CG.CommodityGroupEnum.None), Is.True);
        }

        [Test]
        public void CommodityGroup_MapByEnum_AllGroupsPresent()
        {
            var allEnums = System.Enum.GetValues(typeof(CG.CommodityGroupEnum))
                .Cast<CG.CommodityGroupEnum>();
            foreach (var e in allEnums)
                Assert.That(CG.CommodityGroupMapByEnum.ContainsKey(e), Is.True,
                    $"CommodityGroupMapByEnum missing {e}");
        }

        [Test]
        public void CommodityGroup_MapByString_LookupByName()
        {
            var group = CG.Groups.First(g => g.ID != CG.CommodityGroupEnum.None);
            var result = CG.CommodityGroupMapByString[group.Name];
            Assert.That(result.ID, Is.EqualTo(group.ID));
        }

        // -----------------------------------------------------------------------
        // CommodityIndustry static data
        // -----------------------------------------------------------------------

        [Test]
        public void CommodityIndustry_ListIsNotEmpty()
        {
            Assert.That(CI.Groups.Count > 0, Is.True);
        }

        [Test]
        public void CommodityIndustry_MapByEnum_ContainsNoneEntry()
        {
            Assert.That(CI.CommodityIndustryMapByEnum.ContainsKey(CI.CommodityIndustryEnum.None), Is.True);
        }

        [Test]
        public void CommodityIndustry_MapByEnum_AllIndustriesPresent()
        {
            var allEnums = System.Enum.GetValues(typeof(CI.CommodityIndustryEnum))
                .Cast<CI.CommodityIndustryEnum>();
            foreach (var e in allEnums)
                Assert.That(CI.CommodityIndustryMapByEnum.ContainsKey(e), Is.True,
                    $"CommodityIndustryMapByEnum missing {e}");
        }

        [Test]
        public void CommodityIndustry_MapByString_LookupByName()
        {
            var industry = CI.Groups.First(i => i.ID != CI.CommodityIndustryEnum.None);
            var result = CI.CommodityIndustryMapByString[industry.Name];
            Assert.That(result.ID, Is.EqualTo(industry.ID));
        }
    }
}
