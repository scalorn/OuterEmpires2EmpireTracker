using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ShipTemplateService edge cases and event firing.
    /// Feature: bl-115-shiptemplate-readonly
    /// </summary>
    [TestFixture]
    public class ShipTemplateServiceTests
    {
        private PlayerContext playerContext;
        private ShipTemplateService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new ShipTemplateService(playerContext);
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new ShipTemplateUpdateRequest { Name = "Test", HullBlueprintUUID = "hull", Components = new List<ShipComponentSlot>() };
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
            var request = new ShipTemplateCreateRequest { Name = "New", HullBlueprintUUID = "hull", Components = new List<ShipComponentSlot>() };
            var result = service.Create(request);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new ShipTemplateCreateRequest { Name = "Owner", HullBlueprintUUID = "hull", Components = new List<ShipComponentSlot>() };
            var result = service.Create(request);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        [Test]
        public void Update_FiresShipTemplateDataChangedEvent()
        {
            var tmpl = new ShipTemplate { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "EventTest" };
            playerContext.AddShipTemplate(tmpl);
            bool eventFired = false;
            playerContext.ShipTemplateDataChanged += (s, e) => eventFired = true;
            var request = new ShipTemplateUpdateRequest { Name = "Updated", HullBlueprintUUID = "hull", Components = new List<ShipComponentSlot>() };
            service.Update(tmpl.UUID, request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Create_FiresShipTemplateDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.ShipTemplateDataChanged += (s, e) => eventFired = true;
            var request = new ShipTemplateCreateRequest { Name = "New", HullBlueprintUUID = "hull", Components = new List<ShipComponentSlot>() };
            service.Create(request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Delete_FiresShipTemplateDataChangedEvent()
        {
            var tmpl = new ShipTemplate { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "DeleteTest" };
            playerContext.AddShipTemplate(tmpl);
            bool eventFired = false;
            playerContext.ShipTemplateDataChanged += (s, e) => eventFired = true;
            service.Delete(tmpl.UUID);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Update_ReplacesComponentsList()
        {
            var tmpl = new ShipTemplate { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "CompTest", HullBlueprintUUID = "hull" };
            playerContext.AddShipTemplate(tmpl);
            var request = new ShipTemplateUpdateRequest
            {
                Name = "CompTest",
                HullBlueprintUUID = "hull",
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-1", CurrentHP = 50, MaxHP = 100, MaxRepairPercent = 0.8m },
                },
            };
            var result = service.Update(tmpl.UUID, request);
            Assert.That(result.Components.Count, Is.EqualTo(1));
            Assert.That(result.Components[0].SlotType, Is.EqualTo("Engine"));
            Assert.That(result.Components[0].BlueprintUUID, Is.EqualTo("eng-1"));
        }

        [Test]
        public void Create_PopulatesComponentsFromRequest()
        {
            var request = new ShipTemplateCreateRequest
            {
                Name = "CompCreate",
                HullBlueprintUUID = "hull",
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Weapon", SlotIndex = 0, BlueprintUUID = "wpn-1", CurrentHP = 75, MaxHP = 100, MaxRepairPercent = 0.9m },
                    new ShipComponentSlot { SlotType = "Shield", SlotIndex = 0, BlueprintUUID = "shd-1", CurrentHP = 60, MaxHP = 80, MaxRepairPercent = 1.0m },
                },
            };
            var result = service.Create(request);
            Assert.That(result.Components.Count, Is.EqualTo(2));
            Assert.That(result.Components.Any(c => c.SlotType == "Weapon"), Is.True);
            Assert.That(result.Components.Any(c => c.SlotType == "Shield"), Is.True);
        }
    }
}