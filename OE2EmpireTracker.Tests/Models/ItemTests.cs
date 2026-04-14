using NUnit.Framework;
using OE2EmpireTracker.Models;
using IT = OE2EmpireTracker.Models.ItemType.ItemTypeEnum;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(item.ItemType, Is.EqualTo(IT.None));
        }

        [Test]
        public void DefaultConstructor_DefaultsAreEmpty()
        {
            var item = new Item();
            Assert.That(item.Name, Is.EqualTo(string.Empty));
            Assert.That(item.BaseItemTypeID, Is.EqualTo(string.Empty));
            Assert.That(item.NickName, Is.EqualTo(string.Empty));
            Assert.That(item.Description, Is.EqualTo(string.Empty));
            Assert.That(item.ResourcePurity, Is.EqualTo(string.Empty));
            Assert.That(item.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void TypedConstructor_SetsItemTypeAndName()
        {
            var item = new Item(IT.Resource, "Iron");
            Assert.That(item.ItemType, Is.EqualTo(IT.Resource));
            Assert.That(item.Name, Is.EqualTo("Iron"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- None / plain name
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NoSpecialType_ReturnsName()
        {
            var item = new Item { Name = "Widget", ItemType = IT.None };
            Assert.That(item.ExtendedName, Is.EqualTo("Widget"));
        }

        [Test]
        public void ExtendedName_EmptyName_ReturnsEmpty()
        {
            var item = new Item { Name = string.Empty, ItemType = IT.None };
            Assert.That(item.ExtendedName, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Resource
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
            Assert.That(item.ExtendedName, Is.EqualTo("Iron (Refined)"));
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
            Assert.That(item.ExtendedName, Is.EqualTo("Iron"));
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
            Assert.That(item.ExtendedName, Is.EqualTo("Iron"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Commodity
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
            Assert.That(item.ExtendedName, Is.EqualTo(commodity.ExtendedName));
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
            Assert.That(item.ExtendedName, Is.EqualTo("Unknown Commodity"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Survey and Blueprint (PlayerContext not available)
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
            Assert.That(item.ExtendedName, Is.EqualTo("Survey Fallback"));
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
            Assert.That(item.ExtendedName, Is.EqualTo("Blueprint Fallback"));
        }

        // -----------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------

        [Test]
        public void Quantity_DefaultIsZero()
        {
            var item = new Item();
            Assert.That(item.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void UUID_CanBeSetAndRead()
        {
            var item = new Item { UUID = "test-uuid" };
            Assert.That(item.UUID, Is.EqualTo("test-uuid"));
        }
    }
}
