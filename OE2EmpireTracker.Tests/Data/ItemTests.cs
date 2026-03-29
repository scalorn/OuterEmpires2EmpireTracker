using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using IT = OE2EmpireTracker.Data.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class ItemTests
    {
        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_ItemTypeIsNone()
        {
            var item = new Item();
            Assert.AreEqual(IT.None, item.ItemType);
        }

        [Test]
        public void DefaultConstructor_DefaultsAreEmpty()
        {
            var item = new Item();
            Assert.AreEqual(string.Empty, item.Name);
            Assert.AreEqual(string.Empty, item.BaseItemTypeID);
            Assert.AreEqual(string.Empty, item.NickName);
            Assert.AreEqual(string.Empty, item.Description);
            Assert.AreEqual(string.Empty, item.ResourcePurity);
            Assert.AreEqual(0, item.Quantity);
        }

        [Test]
        public void TypedConstructor_SetsItemTypeAndName()
        {
            var item = new Item(IT.Resource, "Iron");
            Assert.AreEqual(IT.Resource, item.ItemType);
            Assert.AreEqual("Iron", item.Name);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — None / plain name
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NoSpecialType_ReturnsName()
        {
            var item = new Item { Name = "Widget", ItemType = IT.None };
            Assert.AreEqual("Widget", item.ExtendedName);
        }

        [Test]
        public void ExtendedName_EmptyName_ReturnsEmpty()
        {
            var item = new Item { Name = string.Empty, ItemType = IT.None };
            Assert.AreEqual(string.Empty, item.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Resource
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_Resource_WithPurity_AppendsPurityInParentheses()
        {
            var item = new Item
            {
                ItemType = IT.Resource,
                Name = "Iron",
                ResourcePurity = "Refined"
            };
            Assert.AreEqual("Iron (Refined)", item.ExtendedName);
        }

        [Test]
        public void ExtendedName_Resource_NoPurity_ReturnsNameOnly()
        {
            var item = new Item
            {
                ItemType = IT.Resource,
                Name = "Iron",
                ResourcePurity = string.Empty
            };
            Assert.AreEqual("Iron", item.ExtendedName);
        }

        [Test]
        public void ExtendedName_Resource_NullPurity_ReturnsNameOnly()
        {
            var item = new Item
            {
                ItemType = IT.Resource,
                Name = "Iron",
                ResourcePurity = null
            };
            Assert.AreEqual("Iron", item.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Commodity
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_Commodity_KnownID_ReturnsCommodityExtendedName()
        {
            // Pick a real commodity from the static list
            var commodity = Commodity.Commodities[0];
            var item = new Item
            {
                ItemType = IT.Commodity,
                Name = commodity.Name,
                BaseItemTypeID = commodity.ID
            };
            Assert.AreEqual(commodity.ExtendedName, item.ExtendedName);
        }

        [Test]
        public void ExtendedName_Commodity_UnknownID_FallsBackToName()
        {
            var item = new Item
            {
                ItemType = IT.Commodity,
                Name = "Unknown Commodity",
                BaseItemTypeID = "no-such-id"
            };
            Assert.AreEqual("Unknown Commodity", item.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Survey and Blueprint (PlayerContext not available)
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_Survey_NoPlayerContext_FallsBackToName()
        {
            // EmpireContext.PlayerContext is null in unit test context
            var item = new Item
            {
                ItemType = IT.Survey,
                Name = "Survey Fallback",
                BaseItemTypeID = "some-uuid"
            };
            Assert.AreEqual("Survey Fallback", item.ExtendedName);
        }

        [Test]
        public void ExtendedName_Blueprint_NoPlayerContext_FallsBackToName()
        {
            var item = new Item
            {
                ItemType = IT.Blueprint,
                Name = "Blueprint Fallback",
                BaseItemTypeID = "some-uuid"
            };
            Assert.AreEqual("Blueprint Fallback", item.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------

        [Test]
        public void Quantity_DefaultIsZero()
        {
            var item = new Item();
            Assert.AreEqual(0, item.Quantity);
        }

        [Test]
        public void UUID_CanBeSetAndRead()
        {
            var item = new Item { UUID = "test-uuid" };
            Assert.AreEqual("test-uuid", item.UUID);
        }
    }
}
