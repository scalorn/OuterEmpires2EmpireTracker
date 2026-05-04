using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for ShipViewModel.
    /// Feature: bl-116-ship-readonly
    /// </summary>
    [TestFixture]
    public class ShipViewModelTests
    {
        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            vm.Reset();
            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.TemplateUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.HullBlueprintUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.LocationType, Is.EqualTo(DestinationType.Station));
            Assert.That(vm.LocationUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.HullCurrentHP, Is.EqualTo(0));
            Assert.That(vm.HullMaxHP, Is.EqualTo(0));
            Assert.That(vm.HullMaxRepairPercent, Is.EqualTo(0m));
            Assert.That(vm.Components, Is.Empty);
            Assert.That(vm.Cargo.Count(), Is.EqualTo(0));
            Assert.That(vm.Hopper.Count(), Is.EqualTo(0));
            Assert.That(vm.Original, Is.Null);
        }

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void IsDirty_ReturnsTrue_ForNewShipWithNonEmptyName()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            vm.Name = "Test";
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            vm.Name = "Updated";
            vm.HullBlueprintUUID = "new-hull";
            vm.LocationType = DestinationType.Colony;
            vm.LocationUUID = "colony-uuid";
            vm.HullCurrentHP = 50;
            vm.HullMaxHP = 200;
            vm.HullMaxRepairPercent = 0.9m;
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("Updated"));
            Assert.That(req.HullBlueprintUUID, Is.EqualTo("new-hull"));
            Assert.That(req.LocationType, Is.EqualTo(DestinationType.Colony));
            Assert.That(req.LocationUUID, Is.EqualTo("colony-uuid"));
            Assert.That(req.HullCurrentHP, Is.EqualTo(50));
            Assert.That(req.HullMaxHP, Is.EqualTo(200));
            Assert.That(req.HullMaxRepairPercent, Is.EqualTo(0.9m));
            Assert.That(req.Original, Is.Not.Null);
            Assert.That(req.Components, Is.Not.Null);
            Assert.That(req.Cargo, Is.Not.Null);
            Assert.That(req.Hopper, Is.Not.Null);
        }

        [Test]
        public void BuildCreateRequest_CopiesName()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            vm.Name = "NewShip";
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("NewShip"));
        }

        [Test]
        public void UUID_And_OwnerUUID_PreservedFromLoadFrom()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.UUID, Is.EqualTo("ship-uuid"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("owner-uuid"));
        }

        [Test]
        public void SetComponent_AddsNewComponent()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            Assert.That(vm.Components.Count, Is.EqualTo(1));
            Assert.That(vm.Components[0].SlotType, Is.EqualTo("Engine"));
            Assert.That(vm.Components[0].BlueprintUUID, Is.EqualTo("bp-1"));
        }

        [Test]
        public void RemoveComponent_RemovesFromList()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            vm.RemoveComponent("Engine", 0);
            Assert.That(vm.Components, Is.Empty);
        }

        [Test]
        public void ClearComponents_EmptiesList()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            vm.SetComponent("Engine", 0, "bp-1");
            vm.SetComponent("Weapon", 0, "bp-2");
            vm.ClearComponents();
            Assert.That(vm.Components, Is.Empty);
        }

        [Test]
        public void AddCargoItem_AddsItemToCargo()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            var item = new Item { UUID = "item-1", Name = "Ore", Quantity = 10 };
            vm.AddCargoItem(item);
            Assert.That(vm.Cargo.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RemoveCargoItem_RemovesItemFromCargo()
        {
            var vm = new ShipViewModel();
            vm.Reset();
            var item = new Item { UUID = "item-1", Name = "Ore", Quantity = 10 };
            vm.AddCargoItem(item);
            vm.RemoveCargoItem("item-1");
            Assert.That(vm.Cargo.Count(), Is.EqualTo(0));
        }

        [Test]
        public void LocationType_And_LocationUUID_PreservedFromLoadFrom()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.LocationType, Is.EqualTo(DestinationType.Colony));
            Assert.That(vm.LocationUUID, Is.EqualTo("loc-uuid"));
        }

        [Test]
        public void HullCurrentHP_HullMaxHP_HullMaxRepairPercent_PreservedFromLoadFrom()
        {
            var vm = new ShipViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            Assert.That(vm.HullCurrentHP, Is.EqualTo(80));
            Assert.That(vm.HullMaxHP, Is.EqualTo(100));
            Assert.That(vm.HullMaxRepairPercent, Is.EqualTo(0.75m));
        }

        private static ReadOnlyShip CreateSampleReadOnly()
        {
            var ship = new Ship
            {
                UUID = "ship-uuid",
                Name = "Test Ship",
                OwnerUUID = "owner-uuid",
                TemplateUUID = "tmpl-uuid",
                HullBlueprintUUID = "hull-uuid",
                LocationType = DestinationType.Colony,
                LocationUUID = "loc-uuid",
                HullCurrentHP = 80,
                HullMaxHP = 100,
                HullMaxRepairPercent = 0.75m,
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-bp", CurrentHP = 100, MaxHP = 100, MaxRepairPercent = 1.0m },
                },
            };
            ship.Cargo.AddItem(new Item { UUID = "cargo-1", Name = "Iron Ore", Quantity = 5 });
            return new ReadOnlyShip(ship);
        }
    }
}