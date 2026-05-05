using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for StationViewModel edit buffer.
    /// Feature: bl-117-station-readonly
    /// </summary>
    [TestFixture]
    public class StationViewModelPropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<ShipComponentSlot> ComponentSlotGen()
        {
            return from slotType in SafeStringGen()
                   from slotIndex in Gen.Choose(0, 10)
                   from bpUUID in SafeStringGen()
                   from currentHP in Gen.Choose(0, 1000)
                   from maxHP in Gen.Choose(0, 1000)
                   from maxRepair in Gen.Choose(0, 100)
                   select new ShipComponentSlot
                   {
                       SlotType = slotType,
                       SlotIndex = slotIndex,
                       BlueprintUUID = bpUUID,
                       CurrentHP = currentHP,
                       MaxHP = maxHP,
                       MaxRepairPercent = (decimal)maxRepair / 100m,
                   };
        }

        private static Gen<Item> ItemGen()
        {
            return from name in SafeStringGen()
                   from qty in Gen.Choose(1, 100)
                   from purity in SafeStringGen()
                   from hp in Gen.Choose(0, 1000)
                   from maxHp in Gen.Choose(0, 1000)
                   from maxRepair in Gen.Choose(0, 100)
                   select new Item
                   {
                       UUID = Guid.NewGuid().ToString(),
                       ItemType = ItemType.ItemTypeEnum.Resource,
                       BaseItemTypeID = name,
                       Name = name,
                       Quantity = qty,
                       ResourcePurity = purity,
                       CurrentHP = hp,
                       MaxHP = maxHp,
                       MaxRepairPercent = (decimal)maxRepair / 100m,
                   };
        }

        private static Gen<Station> ValidStationGen()
        {
            return from name in SafeStringGen()
                   from bpUUID in SafeStringGen()
                   from stationType in Gen.Choose(0, 2)
                   from ownership in Gen.Choose(0, 1)
                   from hullHP in Gen.Choose(0, 1000)
                   from hullMaxHP in Gen.Choose(0, 1000)
                   from hullRepair in Gen.Choose(0, 100)
                   from compCount in Gen.Choose(0, 5)
                   from components in GenComponents(compCount)
                   from holdCount in Gen.Choose(0, 3)
                   from holdItems in GenItems(holdCount)
                   from munCount in Gen.Choose(0, 3)
                   from munItems in GenItems(munCount)
                   select BuildStation(name, bpUUID, stationType, ownership, hullHP, hullMaxHP, hullRepair, components, holdItems, munItems);
        }

        private static Gen<List<ShipComponentSlot>> GenComponents(int count)
        {
            if (count == 0) return Gen.Constant(new List<ShipComponentSlot>());
            var gens = new Gen<ShipComponentSlot>[count];
            for (int i = 0; i < count; i++) gens[i] = ComponentSlotGen();
            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Gen<List<Item>> GenItems(int count)
        {
            if (count == 0) return Gen.Constant(new List<Item>());
            var gens = new Gen<Item>[count];
            for (int i = 0; i < count; i++) gens[i] = ItemGen();
            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Station BuildStation(string name, string bpUUID, int stationType, int ownership, int hullHP, int hullMaxHP, int hullRepair, List<ShipComponentSlot> components, List<Item> holdItems, List<Item> munItems)
        {
            string playerUUID = "test-player";
            var station = new Station
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                OwnerUUID = playerUUID,
                StationBlueprintUUID = bpUUID,
                StationType = (StationType)stationType,
                Ownership = (StationOwnership)ownership,
                HullCurrentHP = hullHP,
                HullMaxHP = hullMaxHP,
                HullMaxRepairPercent = (decimal)hullRepair / 100m,
                Components = components,
            };
            var holdBag = new ItemBag();
            foreach (var item in holdItems) holdBag.AddItem(item);
            station.Holds[playerUUID] = holdBag;
            var munBag = new ItemBag();
            foreach (var item in munItems) munBag.AddItem(item);
            station.MunitionsHold = munBag;
            return station;
        }

        // Property 1: LoadFrom Round-Trip Preserves All Fields
        // **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidStationGen().ToArbitrary(), station =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                bool nameMatch = vm.Name == (ro.Name ?? string.Empty);
                bool typeMatch = vm.StationType == ro.StationType;
                bool ownershipMatch = vm.Ownership == ro.Ownership;
                bool bpMatch = vm.StationBlueprintUUID == (ro.StationBlueprintUUID ?? string.Empty);
                bool hpMatch = vm.HullCurrentHP == ro.HullCurrentHP && vm.HullMaxHP == ro.HullMaxHP && vm.HullMaxRepairPercent == ro.HullMaxRepairPercent;
                var roComps = ro.Components;
                bool compMatch = vm.Components.Count == roComps.Count;
                if (compMatch)
                {
                    for (int i = 0; i < vm.Components.Count; i++)
                    {
                        var lc = vm.Components[i];
                        var orig = roComps[i];
                        if (lc.SlotType != orig.SlotType || lc.SlotIndex != orig.SlotIndex || lc.BlueprintUUID != orig.BlueprintUUID || lc.CurrentHP != orig.CurrentHP || lc.MaxHP != orig.MaxHP || lc.MaxRepairPercent != orig.MaxRepairPercent)
                        {
                            compMatch = false;
                            break;
                        }
                    }
                }

                return (nameMatch && typeMatch && ownershipMatch && bpMatch && hpMatch && compMatch)
                    .Label("name=" + nameMatch + " type=" + typeMatch + " own=" + ownershipMatch + " bp=" + bpMatch + " hp=" + hpMatch + " comp=" + compMatch);
            });
        }

        // Property 2: IsDirty False Immediately After LoadFrom
        // **Validates: Requirements 6.1, 6.10**
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidStationGen().ToArbitrary(), station =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                return (!vm.IsDirty).Label(vm.IsDirty ? "IsDirty was true" : "OK");
            });
        }

        // Property 3: IsDirty Detects Name Change
        // **Validates: Requirements 6.1, 6.2**
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsNameChange()
        {
            return Prop.ForAll(ValidStationGen().ToArbitrary(), Arb.From(SafeStringGen().Where(s => s.Length > 0)), (station, suffix) =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                vm.Name = vm.Name + suffix;
                return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false");
            });
        }

        // Property 4: IsDirty Detects StationBlueprintUUID Change
        // **Validates: Requirements 6.1, 6.5**
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsStationBlueprintUUIDChange()
        {
            return Prop.ForAll(ValidStationGen().ToArbitrary(), Arb.From(SafeStringGen().Where(s => s.Length > 0)), (station, suffix) =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                vm.StationBlueprintUUID = vm.StationBlueprintUUID + suffix;
                return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false");
            });
        }

        // Property 5: IsDirty Detects Components Change
        // **Validates: Requirements 6.1, 6.7**
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsComponentsChange()
        {
            var stationWithComps = from name in SafeStringGen()
                                   from bpUUID in SafeStringGen()
                                   from compCount in Gen.Choose(1, 5)
                                   from components in GenComponents(compCount)
                                   select BuildStation(name, bpUUID, 0, 0, 100, 100, 100, components, new List<Item>(), new List<Item>());
            return Prop.ForAll(stationWithComps.ToArbitrary(), station =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                vm.Components.Add(new ShipComponentSlot { SlotType = "NewSlot", SlotIndex = 99, BlueprintUUID = "new-bp" });
                return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false after adding component");
            });
        }

        // Property 6: IsDirty Detects StationType Change
        // **Validates: Requirements 6.1, 6.3**
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsStationTypeChange()
        {
            return Prop.ForAll(ValidStationGen().ToArbitrary(), station =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                vm.StationType = vm.StationType == StationType.Station ? StationType.Starbase : StationType.Station;
                return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false");
            });
        }

        // Property 7: IsDirty Detects Hold Change
        // **Validates: Requirements 6.1, 6.8**
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsHoldChange()
        {
            var stationWithHold = from name in SafeStringGen()
                                  from bpUUID in SafeStringGen()
                                  from holdCount in Gen.Choose(1, 3)
                                  from holdItems in GenItems(holdCount)
                                  select BuildStation(name, bpUUID, 0, 0, 100, 100, 100, new List<ShipComponentSlot>(), holdItems, new List<Item>());
            return Prop.ForAll(stationWithHold.ToArbitrary(), station =>
            {
                var ro = new ReadOnlyStation(station);
                var vm = new StationViewModel();
                vm.LoadFrom(ro, "test-player");
                vm.AddHoldItem(new Item { UUID = Guid.NewGuid().ToString(), Name = "Extra", ItemType = ItemType.ItemTypeEnum.Resource, Quantity = 1 });
                return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false after adding hold item");
            });
        }
    }
}
