using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for StationService edge cases and event firing.
    /// Feature: bl-117-station-readonly
    /// </summary>
    [TestFixture]
    public class StationServiceTests
    {
        private PlayerContext playerContext;
        private StationService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new StationService(playerContext);
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new StationUpdateRequest { Name = "Test", Components = new List<ShipComponentSlot>(), Hold = new ItemBag(), MunitionsHold = new ItemBag() };
            Assert.Throws<InvalidOperationException>(() => service.Update("nonexistent-uuid", request));
        }

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var request = new StationCreateRequest { Name = "New" };
            var result = service.Create(request);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new StationCreateRequest { Name = "Owner" };
            var result = service.Create(request);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        [Test]
        public void Update_FiresStationDataChangedEvent()
        {
            var station = new Station { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "EventTest" };
            playerContext.AddStation(station);
            bool eventFired = false;
            playerContext.StationDataChanged += (s, e) => eventFired = true;
            var request = new StationUpdateRequest { Name = "Updated", Components = new List<ShipComponentSlot>(), Hold = new ItemBag(), MunitionsHold = new ItemBag() };
            service.Update(station.UUID, request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Create_FiresStationDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.StationDataChanged += (s, e) => eventFired = true;
            service.Create(new StationCreateRequest { Name = "New" });
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Delete_FiresStationDataChangedEvent()
        {
            var station = new Station { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "DeleteTest" };
            playerContext.AddStation(station);
            bool eventFired = false;
            playerContext.StationDataChanged += (s, e) => eventFired = true;
            service.Delete(station.UUID);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Update_ReplacesComponentsList()
        {
            var station = new Station { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "CompTest" };
            playerContext.AddStation(station);
            var comps = new List<ShipComponentSlot> { new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-1" } };
            var request = new StationUpdateRequest { Name = "CompTest", Components = comps, Hold = new ItemBag(), MunitionsHold = new ItemBag() };
            var result = service.Update(station.UUID, request);
            Assert.That(result.Components.Count, Is.EqualTo(1));
            Assert.That(result.Components[0].SlotType, Is.EqualTo("Engine"));
        }

        [Test]
        public void Update_ReplacesHoldAndMunitionsHold()
        {
            var station = new Station { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "HoldTest" };
            playerContext.AddStation(station);
            var hold = new ItemBag();
            hold.AddItem(new Item { UUID = "h1", Name = "Iron", ItemType = ItemType.ItemTypeEnum.Resource, Quantity = 10 });
            var mun = new ItemBag();
            mun.AddItem(new Item { UUID = "m1", Name = "Missile", ItemType = ItemType.ItemTypeEnum.Munition, Quantity = 5 });
            var request = new StationUpdateRequest { Name = "HoldTest", Components = new List<ShipComponentSlot>(), Hold = hold, MunitionsHold = mun };
            var result = service.Update(station.UUID, request);
            Assert.That(result.Holds.ContainsKey("test-player-uuid"), Is.True);
            Assert.That(result.MunitionsHold.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Create_PopulatesNameFromRequest()
        {
            var result = service.Create(new StationCreateRequest { Name = "Custom Name" });
            Assert.That(result.Name, Is.EqualTo("Custom Name"));
        }
    }
}
