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
    /// Property-based tests for DeliveryPlanViewModel edit buffer.
    /// Feature: bl-113-deliveryplan-readonly
    /// Validates: LoadFrom round-trip preserves all fields.
    /// </summary>
    [TestFixture]
    public class DeliveryPlanViewModelPropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random DeliveryPlan with all fields populated
        // -----------------------------------------------------------------------

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<DestinationType> DestinationTypeGen()
        {
            return Gen.Elements(
                DestinationType.Colony,
                DestinationType.Station,
                DestinationType.Asteroid);
        }

        private static Gen<ItemType.ItemTypeEnum> ItemTypeGen()
        {
            return Gen.Elements(
                ItemType.ItemTypeEnum.Commodity,
                ItemType.ItemTypeEnum.Resource,
                ItemType.ItemTypeEnum.Flatpack,
                ItemType.ItemTypeEnum.WorkDetail);
        }

        private static Gen<DeliveryItem> DeliveryItemGen()
        {
            return from itemType in ItemTypeGen()
                   from baseId in SafeStringGen()
                   from name in SafeStringGen()
                   from purity in SafeStringGen()
                   from qty in Gen.Choose(1, 500)
                   from delivered in Arb.Generate<bool>()
                   select new DeliveryItem
                   {
                       ItemType = itemType,
                       BaseItemTypeID = baseId,
                       Name = name,
                       ResourcePurity = purity,
                       Quantity = qty,
                       Delivered = delivered,
                   };
        }

        private static Gen<List<DeliveryItem>> DeliveryItemListGen(int maxCount)
        {
            return from count in Gen.Choose(0, maxCount)
                   from items in GenItems(count)
                   select items;
        }

        private static Gen<List<DeliveryItem>> GenItems(int count)
        {
            if (count == 0)
            {
                return Gen.Constant(new List<DeliveryItem>());
            }

            var gens = new Gen<DeliveryItem>[count];
            for (int i = 0; i < count; i++)
            {
                gens[i] = DeliveryItemGen();
            }

            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Gen<DeliveryPlanStop> PlanStopGen(int sequence)
        {
            return from colonyUUID in SafeStringGen()
                   from stopCompleted in Arb.Generate<bool>()
                   from destType in DestinationTypeGen()
                   from destUUID in SafeStringGen()
                   from dropOff in DeliveryItemListGen(3)
                   from pickUp in DeliveryItemListGen(3)
                   select new DeliveryPlanStop
                   {
                       ColonyUUID = colonyUUID,
                       Sequence = sequence,
                       StopCompleted = stopCompleted,
                       DestinationType = destType,
                       DestinationUUID = destUUID,
                       DropOff = dropOff,
                       PickUp = pickUp,
                   };
        }

        private static Gen<List<DeliveryPlanStop>> GenPlanStops(int count)
        {
            if (count == 0)
            {
                return Gen.Constant(new List<DeliveryPlanStop>());
            }

            var gens = new Gen<DeliveryPlanStop>[count];
            for (int i = 0; i < count; i++)
            {
                gens[i] = PlanStopGen(i);
            }

            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        internal static Gen<DeliveryPlan> ValidDeliveryPlanGen()
        {
            return from name in SafeStringGen()
                   from ownerUUID in SafeStringGen()
                   from routeUUID in SafeStringGen()
                   from shipUUID in SafeStringGen()
                   from completed in Arb.Generate<bool>()
                   from stopCount in Gen.Choose(0, 5)
                   from stops in GenPlanStops(stopCount)
                   select new DeliveryPlan
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       OwnerUUID = ownerUUID,
                       RouteUUID = routeUUID,
                       ShipUUID = shipUUID,
                       Completed = completed,
                       Stops = stops,
                   };
        }

        // -----------------------------------------------------------------------
        // Property: LoadFrom Round-Trip Preserves All Fields
        // Feature: bl-113-deliveryplan-readonly, Property 1
        // **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidDeliveryPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyDeliveryPlan(plan);
                var vm = new DeliveryPlanViewModel();
                vm.LoadFrom(ro);

                bool nameMatch = vm.Name == (ro.Name ?? string.Empty);
                bool uuidMatch = vm.UUID == ro.UUID;
                bool ownerMatch = vm.OwnerUUID == (ro.OwnerUUID ?? string.Empty);
                bool routeMatch = vm.RouteUUID == (ro.RouteUUID ?? string.Empty);
                bool shipMatch = vm.ShipUUID == (ro.ShipUUID ?? string.Empty);
                bool completedMatch = vm.Completed == ro.Completed;

                var roStops = ro.Stops;
                bool countMatch = vm.Stops.Count == roStops.Count;

                bool stopsMatch = countMatch;
                if (countMatch)
                {
                    for (int i = 0; i < vm.Stops.Count; i++)
                    {
                        var local = vm.Stops[i];
                        var orig = roStops[i];
                        if (local.ColonyUUID != (orig.ColonyUUID ?? string.Empty)
                            || local.Sequence != orig.Sequence
                            || local.StopCompleted != orig.StopCompleted
                            || local.DestinationType != orig.DestinationType
                            || local.DestinationUUID != (orig.DestinationUUID ?? string.Empty))
                        {
                            stopsMatch = false;
                            break;
                        }

                        if (local.DropOff.Count != orig.DropOff.Count)
                        {
                            stopsMatch = false;
                            break;
                        }

                        for (int j = 0; j < local.DropOff.Count; j++)
                        {
                            var li = local.DropOff[j];
                            var oi = orig.DropOff[j];
                            if (li.ItemType != oi.ItemType
                                || li.BaseItemTypeID != (oi.BaseItemTypeID ?? string.Empty)
                                || li.Name != (oi.Name ?? string.Empty)
                                || li.ResourcePurity != (oi.ResourcePurity ?? string.Empty)
                                || li.Quantity != oi.Quantity
                                || li.Delivered != oi.Delivered)
                            {
                                stopsMatch = false;
                                break;
                            }
                        }

                        if (!stopsMatch) break;

                        if (local.PickUp.Count != orig.PickUp.Count)
                        {
                            stopsMatch = false;
                            break;
                        }

                        for (int j = 0; j < local.PickUp.Count; j++)
                        {
                            var li = local.PickUp[j];
                            var oi = orig.PickUp[j];
                            if (li.ItemType != oi.ItemType
                                || li.BaseItemTypeID != (oi.BaseItemTypeID ?? string.Empty)
                                || li.Name != (oi.Name ?? string.Empty)
                                || li.ResourcePurity != (oi.ResourcePurity ?? string.Empty)
                                || li.Quantity != oi.Quantity
                                || li.Delivered != oi.Delivered)
                            {
                                stopsMatch = false;
                                break;
                            }
                        }

                        if (!stopsMatch) break;
                    }
                }

                return (nameMatch && uuidMatch && ownerMatch && routeMatch && shipMatch && completedMatch && stopsMatch)
                    .Label(
                        "name=" + nameMatch
                        + ", uuid=" + uuidMatch
                        + ", owner=" + ownerMatch
                        + ", route=" + routeMatch
                        + ", ship=" + shipMatch
                        + ", completed=" + completedMatch
                        + ", stopsCount=" + countMatch
                        + ", stopsMatch=" + stopsMatch);
            });
        }
    }
}