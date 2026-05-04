using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ShipService edge cases and event firing.
    /// Feature: bl-116-ship-readonly
    /// </summary>
    [TestFixture]
    public class ShipServiceTests
    {
        private PlayerContext playerContext;
        private ShipService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new ShipService(playerContext);
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new ShipUpdateRequest
            {
                Name = "Test",
                HullBlueprintUUID = "hull",
                Components = new List<ShipComponentSlot>(),
                Cargo = new ItemBag(),
                Hopper = new ItemBag(),
            };
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
            var request = new ShipCreateRequest { Name = "New" };
            var result = service.Create(request);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new ShipCreateRequest { Name = "Owner" };
            var result = service.Create(request);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        [Test]
        public void Update_FiresShipDataChangedEvent()
        {
            var ship = new Ship { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "EventTest" };
            playerContext.AddShip(ship);
            bool eventFired = false;
            playerContext.ShipDataChanged += (s, e) => eventFired = true;
            var request = new ShipUpdateRequest
            {
                Name = "Updated",
                HullBlueprintUUID = "hull",
                Components = new List<ShipComponentSlot>(),
                Cargo = new ItemBag(),
                Hopper = new ItemBag(),
            };
            service.Update(ship.UUID, request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Create_FiresShipDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.ShipDataChanged += (s, e) => eventFired = true;
            var request = new ShipCreateRequest { Name = "New" };
            service.Create(request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Delete_FiresShipDataChangedEvent()
        {
            var ship = new Ship { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "DeleteTest" };
            playerContext.AddShip(ship);
            bool eventFired = false;
            playerContext.ShipDataChanged += (s, e) => eventFired = true;
            service.Delete(ship.UUID);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void CreateFromTemplate_CopiesFieldsFromTemplate()
        {
            var template = new ShipTemplate
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "Corvette Template",
                HullBlueprintUUID = "corvette-hull",
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-1", CurrentHP = 100, MaxHP = 100, MaxRepairPercent = 1.0m },
                },
            };
            playerContext.AddShipTemplate(template);
            var result = service.CreateFromTemplate(template.UUID);
            Assert.That(result.Name, Is.EqualTo("Corvette Template"));
            Assert.That(result.TemplateUUID, Is.EqualTo(template.UUID));
            Assert.That(result.HullBlueprintUUID, Is.EqualTo("corvette-hull"));
            Assert.That(result.Components.Count, Is.EqualTo(1));
            Assert.That(result.Components[0].SlotType, Is.EqualTo("Engine"));
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CreateFromTemplate_NonExistentTemplate_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => service.CreateFromTemplate("nonexistent-uuid"));
        }
    }
}