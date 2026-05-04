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
    /// Property-based tests for ShipTemplateViewModel edit buffer.
    /// Feature: bl-115-shiptemplate-readonly
    /// </summary>
    [TestFixture]
    public class ShipTemplateViewModelPropertyTests
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

        private static Gen<List<ShipComponentSlot>> ComponentListGen(int count)
        {
            if (count == 0)
            {
                return Gen.Constant(new List<ShipComponentSlot>());
            }

            var gens = new Gen<ShipComponentSlot>[count];
            for (int i = 0; i < count; i++)
            {
                gens[i] = ComponentSlotGen();
            }

            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Gen<ShipTemplate> ValidShipTemplateGen()
        {
            return from name in SafeStringGen()
                   from hullUUID in SafeStringGen()
                   from compCount in Gen.Choose(0, 10)
                   from components in ComponentListGen(compCount)
                   select new ShipTemplate
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       OwnerUUID = Guid.NewGuid().ToString(),
                       HullBlueprintUUID = hullUUID,
                       Components = components,
                   };
        }

        private static Gen<ShipTemplate> ValidShipTemplateWithComponentsGen()
        {
            return from name in SafeStringGen()
                   from hullUUID in SafeStringGen()
                   from compCount in Gen.Choose(1, 10)
                   from components in ComponentListGen(compCount)
                   select new ShipTemplate
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       OwnerUUID = Guid.NewGuid().ToString(),
                       HullBlueprintUUID = hullUUID,
                       Components = components,
                   };
        }

        // Property 1: LoadFrom Round-Trip Preserves All Fields
        // **Validates: Requirements 3.1, 3.2, 3.3**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidShipTemplateGen().ToArbitrary(), template =>
            {
                var ro = new ReadOnlyShipTemplate(template);
                var vm = new ShipTemplateViewModel();
                vm.LoadFrom(ro);

                bool nameMatch = vm.Name == (ro.Name ?? string.Empty);
                bool hullMatch = vm.HullBlueprintUUID == (ro.HullBlueprintUUID ?? string.Empty);
                var roComponents = ro.Components;
                bool countMatch = vm.Components.Count == roComponents.Count;
                bool componentsMatch = countMatch;
                if (countMatch)
                {
                    for (int i = 0; i < vm.Components.Count; i++)
                    {
                        var local = vm.Components[i];
                        var orig = roComponents[i];
                        if (local.SlotType != orig.SlotType
                            || local.SlotIndex != orig.SlotIndex
                            || local.BlueprintUUID != orig.BlueprintUUID
                            || local.CurrentHP != orig.CurrentHP
                            || local.MaxHP != orig.MaxHP
                            || local.MaxRepairPercent != orig.MaxRepairPercent)
                        {
                            componentsMatch = false;
                            break;
                        }
                    }
                }

                return (nameMatch && hullMatch && componentsMatch)
                    .Label("name=" + nameMatch + ", hull=" + hullMatch + ", compMatch=" + componentsMatch);
            });
        }

        // Property 2: IsDirty False Immediately After LoadFrom
        // **Validates: Requirements 6.1, 6.5**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidShipTemplateGen().ToArbitrary(), template =>
            {
                var ro = new ReadOnlyShipTemplate(template);
                var vm = new ShipTemplateViewModel();
                vm.LoadFrom(ro);
                return (!vm.IsDirty).Label(vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // Property 3: IsDirty Detects Name Change
        // **Validates: Requirements 6.1, 6.2**

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsNameChange()
        {
            return Prop.ForAll(
                ValidShipTemplateGen().ToArbitrary(),
                Arb.From(SafeStringGen().Where(s => s.Length > 0)),
                (template, suffix) =>
                {
                    var ro = new ReadOnlyShipTemplate(template);
                    var vm = new ShipTemplateViewModel();
                    vm.LoadFrom(ro);
                    vm.Name = vm.Name + suffix;
                    return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false after changing Name");
                });
        }

        // Property 4: IsDirty Detects HullBlueprintUUID Change
        // **Validates: Requirements 6.1, 6.3**

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsHullBlueprintUUIDChange()
        {
            return Prop.ForAll(
                ValidShipTemplateGen().ToArbitrary(),
                Arb.From(SafeStringGen().Where(s => s.Length > 0)),
                (template, suffix) =>
                {
                    var ro = new ReadOnlyShipTemplate(template);
                    var vm = new ShipTemplateViewModel();
                    vm.LoadFrom(ro);
                    vm.HullBlueprintUUID = vm.HullBlueprintUUID + suffix;
                    return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false after changing HullBlueprintUUID");
                });
        }

        // Property 5: IsDirty Detects Components Change
        // **Validates: Requirements 6.1, 6.4**

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsComponentsChange()
        {
            var mutationGen = Gen.Choose(0, 2);
            return Prop.ForAll(
                ValidShipTemplateWithComponentsGen().ToArbitrary(),
                Arb.From(mutationGen),
                (template, mutation) =>
                {
                    var ro = new ReadOnlyShipTemplate(template);
                    var vm = new ShipTemplateViewModel();
                    vm.LoadFrom(ro);
                    string mutationType;
                    switch (mutation)
                    {
                        case 0:
                            vm.SetComponent("NewSlot", 99, "new-bp-uuid");
                            mutationType = "AddComponent";
                            break;
                        case 1:
                            vm.RemoveComponent(vm.Components[0].SlotType, vm.Components[0].SlotIndex);
                            mutationType = "RemoveComponent";
                            break;
                        default:
                            vm.Components[0].BlueprintUUID = vm.Components[0].BlueprintUUID + "_changed";
                            mutationType = "ModifyComponent";
                            break;
                    }

                    return vm.IsDirty.Label(vm.IsDirty ? "OK" : "IsDirty was false after " + mutationType);
                });
        }
    }
}