using System;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class DeliveryFulfillmentTests
    {
        [Test]
        public void FulfillCommodity_Delivered_SetsFulfilledAndDelivered()
        {
            var colony = new Colony();
            colony.Commodities.Add(new CommodityRequested { Name = "Steel", Requested = 50 });

            var result = DeliveryFulfillment.FulfillCommodity(colony, "Steel", true);

            Assert.That(result, Is.True);
            Assert.That(colony.Commodities[0].Delivered, Is.EqualTo(50));
            Assert.That(colony.Commodities[0].Fulfilled, Is.True);
        }

        [Test]
        public void FulfillCommodity_Undelivered_ResetsToZero()
        {
            var colony = new Colony();
            colony.Commodities.Add(new CommodityRequested { Name = "Steel", Requested = 50, Delivered = 50, Fulfilled = true });

            var result = DeliveryFulfillment.FulfillCommodity(colony, "Steel", false);

            Assert.That(result, Is.True);
            Assert.That(colony.Commodities[0].Delivered, Is.EqualTo(0));
            Assert.That(colony.Commodities[0].Fulfilled, Is.False);
        }

        [Test]
        public void FulfillCommodity_NoMatchingCommodity_ReturnsFalse()
        {
            var colony = new Colony();
            colony.Commodities.Add(new CommodityRequested { Name = "Steel", Requested = 50 });

            var result = DeliveryFulfillment.FulfillCommodity(colony, "Copper", true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void FulfillCommodity_NullColony_ReturnsFalse()
        {
            var result = DeliveryFulfillment.FulfillCommodity(null, "Steel", true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void FulfillCommodity_EmptyName_ReturnsFalse()
        {
            var colony = new Colony();
            var result = DeliveryFulfillment.FulfillCommodity(colony, string.Empty, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void FulfillCommodity_DoesNotAffectOtherCommodities()
        {
            var colony = new Colony();
            colony.Commodities.Add(new CommodityRequested { Name = "Steel", Requested = 50 });
            colony.Commodities.Add(new CommodityRequested { Name = "Copper", Requested = 30 });

            DeliveryFulfillment.FulfillCommodity(colony, "Steel", true);

            Assert.That(colony.Commodities[1].Delivered, Is.EqualTo(0));
            Assert.That(colony.Commodities[1].Fulfilled, Is.False);
        }

        [Test]
        public void StageFlatpack_Delivered_SetsStagedTrue()
        {
            var colony = new Colony();
            var structure = new ColonyStructure { FlatpackBlueprintUUID = "bp-123" };
            colony.Structures.Add(structure);

            var result = DeliveryFulfillment.StageFlatpack(colony, "bp-123", true);

            Assert.That(result, Is.True);
            bool staged;
            structure.Properties.GetBoolean("Staged", false, out staged);
            Assert.That(staged, Is.True);
        }

        [Test]
        public void StageFlatpack_Undelivered_SetsStagedFalse()
        {
            var colony = new Colony();
            var structure = new ColonyStructure { FlatpackBlueprintUUID = "bp-123" };
            structure.Properties.SetProperty("Staged", "True");
            colony.Structures.Add(structure);

            var result = DeliveryFulfillment.StageFlatpack(colony, "bp-123", false);

            Assert.That(result, Is.True);
            bool staged;
            structure.Properties.GetBoolean("Staged", true, out staged);
            Assert.That(staged, Is.False);
        }

        [Test]
        public void StageFlatpack_NoMatchingStructure_ReturnsFalse()
        {
            var colony = new Colony();
            colony.Structures.Add(new ColonyStructure { FlatpackBlueprintUUID = "bp-999" });

            var result = DeliveryFulfillment.StageFlatpack(colony, "bp-123", true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void StageFlatpack_NullColony_ReturnsFalse()
        {
            var result = DeliveryFulfillment.StageFlatpack(null, "bp-123", true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void DeliverWorkers_NewWorker_CreatesItemInWarehouse()
        {
            var colony = new Colony();

            var result = DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 3, true);

            Assert.That(result, Is.True);
            var count = colony.Items.CountByType(ItemType.ItemTypeEnum.WorkDetail, "Blue Collar Detail");
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void DeliverWorkers_ExistingWorker_AddsToQuantity()
        {
            var colony = new Colony();
            var existing = new Item(ItemType.ItemTypeEnum.WorkDetail, "Blue Collar Detail");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Blue Collar Detail";
            existing.Quantity = 2;
            colony.Items.AddItem(existing);

            DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 3, true);

            Assert.That(existing.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void DeliverWorkers_Undelivered_SubtractsQuantity()
        {
            var colony = new Colony();
            var existing = new Item(ItemType.ItemTypeEnum.WorkDetail, "Blue Collar Detail");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Blue Collar Detail";
            existing.Quantity = 5;
            colony.Items.AddItem(existing);

            DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 3, false);

            Assert.That(existing.Quantity, Is.EqualTo(2));
        }

        [Test]
        public void DeliverWorkers_Undelivered_ClampsToZero()
        {
            var colony = new Colony();
            var existing = new Item(ItemType.ItemTypeEnum.WorkDetail, "Blue Collar Detail");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Blue Collar Detail";
            existing.Quantity = 1;
            colony.Items.AddItem(existing);

            DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 5, false);

            Assert.That(existing.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void DeliverWorkers_NullColony_ReturnsFalse()
        {
            var result = DeliveryFulfillment.DeliverWorkers(null, "Blue Collar Detail", "Blue Collar", 1, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void DeliverWorkers_ZeroQuantity_ReturnsFalse()
        {
            var colony = new Colony();
            var result = DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 0, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void DeliverWorkers_SetsCorrectVolume()
        {
            var colony = new Colony();

            DeliveryFulfillment.DeliverWorkers(colony, "Blue Collar Detail", "Blue Collar", 1, true);

            var items = colony.Items.FindByType(ItemType.ItemTypeEnum.WorkDetail, "Blue Collar Detail");
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Volume, Is.EqualTo(OE2EmpireTracker.Constants.GameConstants.WorkerVolume));
        }
    }
}
