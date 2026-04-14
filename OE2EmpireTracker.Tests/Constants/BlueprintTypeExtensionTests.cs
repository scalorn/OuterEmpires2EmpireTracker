using NUnit.Framework;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Tests.Constants
{
    [TestFixture]
    public class BlueprintTypeExtensionTests
    {
        // -----------------------------------------------------------------------
        // IsCommodityFactory -- null and empty
        // -----------------------------------------------------------------------

        [Test]
        public void IsCommodityFactory_Null_ReturnsFalse()
        {
            string input = null;
            Assert.That(input.IsCommodityFactory(), Is.False);
        }

        [Test]
        public void IsCommodityFactory_Empty_ReturnsFalse()
        {
            Assert.That("".IsCommodityFactory(), Is.False);
        }

        // -----------------------------------------------------------------------
        // IsCommodityFactory -- exact prefix without trailing content
        // -----------------------------------------------------------------------

        [Test]
        public void IsCommodityFactory_ExactPrefixOnly_ReturnsFalse()
        {
            // "Flatpacks/CommodityFactory/" with nothing after the trailing slash
            // is not a valid commodity factory variant
            Assert.That("Flatpacks/CommodityFactory/".IsCommodityFactory(), Is.False);
        }

        // -----------------------------------------------------------------------
        // IsCommodityFactory -- valid variants
        // -----------------------------------------------------------------------

        [TestCase("Flatpacks/CommodityFactory/Agridome")]
        [TestCase("Flatpacks/CommodityFactory/AdministrationBlock")]
        [TestCase("Flatpacks/CommodityFactory/ScienceCentre")]
        [TestCase("Flatpacks/CommodityFactory/EngineeringBlock")]
        public void IsCommodityFactory_ValidVariants_ReturnsTrue(string blueprintType)
        {
            Assert.That(blueprintType.IsCommodityFactory(), Is.True);
        }

        // -----------------------------------------------------------------------
        // IsCommodityFactory -- other Flatpacks/ types
        // -----------------------------------------------------------------------

        [TestCase("Flatpacks/MiningRig")]
        [TestCase("Flatpacks/Refinery")]
        [TestCase("Flatpacks/ResearchLaboratory")]
        [TestCase("Flatpacks/Manufactory")]
        public void IsCommodityFactory_OtherFlatpackTypes_ReturnsFalse(string blueprintType)
        {
            Assert.That(blueprintType.IsCommodityFactory(), Is.False);
        }

        // -----------------------------------------------------------------------
        // IsCommodityFactory -- case insensitive
        // -----------------------------------------------------------------------

        [TestCase("flatpacks/commodityfactory/Agridome")]
        [TestCase("FLATPACKS/COMMODITYFACTORY/AGRIDOME")]
        [TestCase("Flatpacks/commodityFactory/Agridome")]
        public void IsCommodityFactory_CaseInsensitive_ReturnsTrue(string blueprintType)
        {
            Assert.That(blueprintType.IsCommodityFactory(), Is.True);
        }

        // -----------------------------------------------------------------------
        // IsCommodityFactory -- non-flatpack types
        // -----------------------------------------------------------------------

        [TestCase("OreHopper")]
        [TestCase("Reactor")]
        [TestCase("SomeRandomType")]
        public void IsCommodityFactory_NonFlatpackTypes_ReturnsFalse(string blueprintType)
        {
            Assert.That(blueprintType.IsCommodityFactory(), Is.False);
        }
    }
}
