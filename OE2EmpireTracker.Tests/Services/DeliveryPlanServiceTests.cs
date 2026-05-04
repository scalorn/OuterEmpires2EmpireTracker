using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for DeliveryPlanService edge cases and event firing.
    /// Feature: bl-113-deliveryplan-readonly
    /// Validates: Requirements 8.2, 8.3, 8.9, 9.2, 9.5, 10.3, 10.7, 10.8, 11.2, 12.2, 13.2, 14.2, 15.2, 16.2
    /// </summary>
    [TestFixture]
    public class DeliveryPlanServiceTests
    {
        private PlayerContext playerContext;
        private DeliveryPlanService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new DeliveryPlanService(playerContext);
        }

        // -------------------------------------------------------------------
        // UpdatePlan with non-existent UUID throws InvalidOperationException
        // -------------------------------------------------------------------

        [Test]
        public void UpdatePlan_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new DeliveryPlanUpdateRequest
            {
                Name = "Test",
                Stops = new List<DeliveryPlanStop>(),
            };

            Assert.Throws<InvalidOperationException>(
                () => service.UpdatePlan("nonexistent-uuid", request));
        }

        // -------------------------------------------------------------------
        // Delete with empty UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        // -------------------------------------------------------------------
        // Delete with non-existent UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Create assigns non-empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var result = service.Create("NewPlan", "route-1");

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Create sets OwnerUUID to current player UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var result = service.Create("OwnerTest", "route-1");

            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Create sets RouteUUID from parameter
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsRouteUUID_FromParameter()
        {
            var result = service.Create("RouteTest", "my-route-uuid");

            Assert.That(result.RouteUUID, Is.EqualTo("my-route-uuid"));
        }

        // -------------------------------------------------------------------
        // UpdatePlan fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void UpdatePlan_FiresDeliveryDataChangedEvent()
        {
            var created = service.Create("EventTest", "route-1");

            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            var request = new DeliveryPlanUpdateRequest
            {
                Name = "Updated",
                Stops = new List<DeliveryPlanStop>(),
            };
            service.UpdatePlan(created.UUID, request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Create fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresDeliveryDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            service.Create("NewPlan", "route-1");

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Delete fires DeliveryDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Delete_FiresDeliveryDataChangedEvent()
        {
            var created = service.Create("DeleteEventTest", "route-1");

            bool eventFired = false;
            playerContext.DeliveryDataChanged += (s, e) => eventFired = true;

            service.Delete(created.UUID);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // MarkItemDelivered sets Delivered flag
        // -------------------------------------------------------------------

        [Test]
        public void MarkItemDelivered_SetsDeliveredFlag()
        {
            var created = service.Create("MarkTest", "route-1");
            var destInfo = new StopDestinationInfo
            {
                ColonyUUID = "colony-1",
                Sequence = 0,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "dest-1",
            };
            var itemInfo = new DeliveryItemInfo
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "Steel",
                Name = "Steel",
                Quantity = 10,
                ResourcePurity = string.Empty,
            };
            service.AddDropOffItem(created.UUID, destInfo, itemInfo);

            var result = service.MarkItemDelivered(created.UUID, 0, 0, "DropOff", true);

            Assert.That(result.Stops[0].DropOff[0].Delivered, Is.True);
        }

        // -------------------------------------------------------------------
        // MarkStopComplete sets StopCompleted flag
        // -------------------------------------------------------------------

        [Test]
        public void MarkStopComplete_SetsStopCompletedFlag()
        {
            var created = service.Create("StopTest", "route-1");
            var destInfo = new StopDestinationInfo
            {
                ColonyUUID = "colony-1",
                Sequence = 0,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "dest-1",
            };
            var itemInfo = new DeliveryItemInfo
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "Steel",
                Name = "Steel",
                Quantity = 5,
                ResourcePurity = string.Empty,
            };
            service.AddDropOffItem(created.UUID, destInfo, itemInfo);

            var result = service.MarkStopComplete(created.UUID, 0);

            Assert.That(result.Stops[0].StopCompleted, Is.True);
        }

        // -------------------------------------------------------------------
        // MarkPlanComplete sets Completed flag
        // -------------------------------------------------------------------

        [Test]
        public void MarkPlanComplete_SetsCompletedFlag()
        {
            var created = service.Create("CompleteTest", "route-1");

            var result = service.MarkPlanComplete(created.UUID);

            Assert.That(result.Completed, Is.True);
        }

        // -------------------------------------------------------------------
        // SetShipUUID sets ShipUUID field
        // -------------------------------------------------------------------

        [Test]
        public void SetShipUUID_SetsShipUUIDField()
        {
            var created = service.Create("ShipTest", "route-1");

            var result = service.SetShipUUID(created.UUID, "ship-uuid-123");

            Assert.That(result.ShipUUID, Is.EqualTo("ship-uuid-123"));
        }

        // -------------------------------------------------------------------
        // AddDropOffItem adds item to stop
        // -------------------------------------------------------------------

        [Test]
        public void AddDropOffItem_AddsItemToStop()
        {
            var created = service.Create("AddTest", "route-1");
            var destInfo = new StopDestinationInfo
            {
                ColonyUUID = "colony-1",
                Sequence = 0,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "dest-1",
            };
            var itemInfo = new DeliveryItemInfo
            {
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = "Iron",
                Name = "Iron",
                Quantity = 100,
                ResourcePurity = "High",
            };

            var result = service.AddDropOffItem(created.UUID, destInfo, itemInfo);

            Assert.That(result.Stops.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(result.Stops[0].DropOff[0].Quantity, Is.EqualTo(100));
            Assert.That(result.Stops[0].DropOff[0].ResourcePurity, Is.EqualTo("High"));
        }

        // -------------------------------------------------------------------
        // RemoveDropOffItems removes items from stop
        // -------------------------------------------------------------------

        [Test]
        public void RemoveDropOffItems_RemovesItemsFromStop()
        {
            var created = service.Create("RemoveTest", "route-1");
            var destInfo = new StopDestinationInfo
            {
                ColonyUUID = "colony-1",
                Sequence = 0,
                DestinationType = DestinationType.Colony,
                DestinationUUID = "dest-1",
            };
            service.AddDropOffItem(created.UUID, destInfo, new DeliveryItemInfo
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "A",
                Name = "A",
                Quantity = 1,
                ResourcePurity = string.Empty,
            });
            service.AddDropOffItem(created.UUID, destInfo, new DeliveryItemInfo
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                BaseItemTypeID = "B",
                Name = "B",
                Quantity = 2,
                ResourcePurity = string.Empty,
            });

            var result = service.RemoveDropOffItems(created.UUID, destInfo, new[] { 0 });

            Assert.That(result.Stops[0].DropOff.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff[0].BaseItemTypeID, Is.EqualTo("B"));
        }
    }
}