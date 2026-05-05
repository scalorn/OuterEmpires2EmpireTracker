using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for StationViewModel.
    /// Feature: bl-117-station-readonly
    /// </summary>
    [TestFixture]
    public class StationViewModelTests
    {
        private Station CreateTestStation()
        {
            var station = new Station
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Station",
                OwnerUUID = "player-1",
                StationType = StationType.Starbase,
                Ownership = StationOwnership.PlayerOwned,
                StationBlueprintUUID = "bp-123",
                HullCurrentHP = 500,
                HullMaxHP = 1000,
                HullMaxRepairPercent = 0.8m,
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { SlotType = "Engine", SlotIndex = 0, BlueprintUUID = "eng-1", CurrentHP = 100, MaxHP = 200, MaxRepairPercent = 0.9m },
                },
            };
            var holdBag = new ItemBag();
            holdBag.AddItem(new Item { UUID = "item-1", Name = "Iron", ItemType = ItemType.ItemTypeEnum.Resource, Quantity = 50, ResourcePurity = "High" });
            station.Holds["player-1"] = holdBag;
            station.MunitionsHold.AddItem(new Item { UUID = "mun-1", Name = "Missile", ItemType = ItemType.ItemTypeEnum.Munition, Quantity = 10 });
            return station;
        }

        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.Reset();
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.StationType, Is.EqualTo(StationType.Station));
            Assert.That(vm.Ownership, Is.EqualTo(StationOwnership.Government));
            Assert.That(vm.HullCurrentHP, Is.EqualTo(0));
            Assert.That(vm.Components.Count, Is.EqualTo(0));
        }

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new StationViewModel();
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void IsDirty_ReturnsTrue_ForNewStationWithNonEmptyName()
        {
            var vm = new StationViewModel();
            vm.Reset();
            vm.Name = "New Station";
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.Name = "Updated";
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Name, Is.EqualTo("Updated"));
            Assert.That(req.StationType, Is.EqualTo(StationType.Starbase));
            Assert.That(req.Ownership, Is.EqualTo(StationOwnership.PlayerOwned));
            Assert.That(req.StationBlueprintUUID, Is.EqualTo("bp-123"));
            Assert.That(req.HullCurrentHP, Is.EqualTo(500));
            Assert.That(req.Components.Count, Is.EqualTo(1));
            Assert.That(req.Hold, Is.Not.Null);
            Assert.That(req.MunitionsHold, Is.Not.Null);
        }

        [Test]
        public void BuildCreateRequest_CopiesName()
        {
            var vm = new StationViewModel();
            vm.Reset();
            vm.Name = "My Station";
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("My Station"));
        }

        [Test]
        public void UUID_And_OwnerUUID_PreservedFromLoadFrom()
        {
            var station = CreateTestStation();
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(station), "player-1");
            Assert.That(vm.UUID, Is.EqualTo(station.UUID));
            Assert.That(vm.OwnerUUID, Is.EqualTo(station.OwnerUUID));
        }

        [Test]
        public void SetComponentCondition_UpdatesComponentHPFields()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.SetComponentCondition("Engine", 0, 50, 0.5m);
            Assert.That(vm.Components[0].CurrentHP, Is.EqualTo(50));
            Assert.That(vm.Components[0].MaxRepairPercent, Is.EqualTo(0.5m));
        }

        [Test]
        public void SetHullComponent_UpdatesHullHPFields()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.SetHullComponent(250, 0.6m);
            Assert.That(vm.HullCurrentHP, Is.EqualTo(250));
            Assert.That(vm.HullMaxRepairPercent, Is.EqualTo(0.6m));
        }

        [Test]
        public void ClearComponents_EmptiesList()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.ClearComponents();
            Assert.That(vm.Components.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddHoldItem_AddsItemToHold()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            int before = vm.Hold.Count();
            vm.AddHoldItem(new Item { UUID = "new-item", Name = "Copper", ItemType = ItemType.ItemTypeEnum.Resource, Quantity = 5 });
            Assert.That(vm.Hold.Count(), Is.EqualTo(before + 1));
        }

        [Test]
        public void RemoveHoldItem_RemovesItemFromHold()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.RemoveHoldItem("item-1");
            Assert.That(vm.Hold.ContainsKey("item-1"), Is.False);
        }

        [Test]
        public void AddMunitionsItem_AddsItemToMunitionsHold()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            int before = vm.MunitionsHold.Count();
            vm.AddMunitionsItem(new Item { UUID = "new-mun", Name = "Torpedo", ItemType = ItemType.ItemTypeEnum.Munition, Quantity = 3 });
            Assert.That(vm.MunitionsHold.Count(), Is.EqualTo(before + 1));
        }

        [Test]
        public void RemoveMunitionsItem_RemovesItemFromMunitionsHold()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            vm.RemoveMunitionsItem("mun-1");
            Assert.That(vm.MunitionsHold.ContainsKey("mun-1"), Is.False);
        }

        [Test]
        public void StationType_And_Ownership_PreservedFromLoadFrom()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            Assert.That(vm.StationType, Is.EqualTo(StationType.Starbase));
            Assert.That(vm.Ownership, Is.EqualTo(StationOwnership.PlayerOwned));
        }

        [Test]
        public void HullCurrentHP_HullMaxHP_HullMaxRepairPercent_PreservedFromLoadFrom()
        {
            var vm = new StationViewModel();
            vm.LoadFrom(new ReadOnlyStation(CreateTestStation()), "player-1");
            Assert.That(vm.HullCurrentHP, Is.EqualTo(500));
            Assert.That(vm.HullMaxHP, Is.EqualTo(1000));
            Assert.That(vm.HullMaxRepairPercent, Is.EqualTo(0.8m));
        }
    }
}
