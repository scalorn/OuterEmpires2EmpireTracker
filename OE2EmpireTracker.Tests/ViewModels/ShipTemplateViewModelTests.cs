using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for ShipTemplateViewModel.
    /// Feature: bl-115-shiptemplate-readonly
    /// </summary>
    [TestFixture]
    public class ShipTemplateViewModelTests
    {
        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new ShipTemplateViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            vm.Reset();
            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.HullBlueprintUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.Components, Is.Empty);
            Assert.That(vm.Original, Is.Null);
        }

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new ShipTemplateViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void IsDirty_ReturnsTrue_ForNewTemplateWithNonEmptyName()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.Name = "Test";
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var vm = new ShipTemplateViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            vm.Name = "Updated";
            vm.HullBlueprintUUID = "new-hull";
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("Updated"));
            Assert.That(req.HullBlueprintUUID, Is.EqualTo("new-hull"));
            Assert.That(req.Original, Is.Not.Null);
        }

        [Test]
        public void BuildCreateRequest_CopiesAllFields()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.Name = "NewTemplate";
            vm.HullBlueprintUUID = "hull-uuid";
            vm.SetComponent("Engine", 0, "bp-1");
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("NewTemplate"));
            Assert.That(req.HullBlueprintUUID, Is.EqualTo("hull-uuid"));
            Assert.That(req.Components.Count, Is.EqualTo(1));
        }

        [Test]
        public void UUID_And_OwnerUUID_PreservedFromLoadFrom()
        {
            var vm = new ShipTemplateViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.UUID, Is.EqualTo("tmpl-uuid"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("owner-uuid"));
        }

        [Test]
        public void SetComponent_AddsNewComponent()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            Assert.That(vm.Components.Count, Is.EqualTo(1));
            Assert.That(vm.Components[0].SlotType, Is.EqualTo("Engine"));
            Assert.That(vm.Components[0].BlueprintUUID, Is.EqualTo("bp-1"));
        }

        [Test]
        public void SetComponent_UpdatesExistingComponent()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            vm.SetComponent("Engine", 0, "bp-2");
            Assert.That(vm.Components.Count, Is.EqualTo(1));
            Assert.That(vm.Components[0].BlueprintUUID, Is.EqualTo("bp-2"));
        }

        [Test]
        public void RemoveComponent_RemovesFromList()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            vm.RemoveComponent("Engine", 0);
            Assert.That(vm.Components, Is.Empty);
        }

        [Test]
        public void ClearComponents_EmptiesList()
        {
            var vm = new ShipTemplateViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            vm.SetComponent("Weapon", 0, "bp-2");
            vm.ClearComponents();
            Assert.That(vm.Components, Is.Empty);
        }

        private static ReadOnlyShipTemplate CreateSampleReadOnly()
        {
            return new ReadOnlyShipTemplate(new ShipTemplate
            {
                UUID = "tmpl-uuid",
                Name = "Test Template",
                OwnerUUID = "owner-uuid",
                HullBlueprintUUID = "hull-uuid",
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-bp", CurrentHP = 100, MaxHP = 100, MaxRepairPercent = 1.0m },
                },
            });
        }
    }
}