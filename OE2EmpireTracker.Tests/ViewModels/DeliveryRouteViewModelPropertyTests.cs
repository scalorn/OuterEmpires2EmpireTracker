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
    /// Property-based tests for DeliveryRouteViewModel edit buffer.
    /// Feature: bl-112-deliveryroute-readonly
    /// Validates: LoadFrom round-trip, IsDirty false after LoadFrom, IsDirty detects changes.
    /// </summary>
    [TestFixture]
    public class DeliveryRouteViewModelPropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random DeliveryRoute with all fields populated
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

        private static Gen<RouteStopPurpose> PurposeGen()
        {
            return Gen.Elements(
                RouteStopPurpose.Cargo,
                RouteStopPurpose.Refuel,
                RouteStopPurpose.CargoAndRefuel);
        }

        private static Gen<decimal> FuelEstimateGen()
        {
            return Gen.Choose(0, 99999).Select(i => (decimal)i / 100m);
        }

        private static Gen<RouteStop> RouteStopGen(int sequence)
        {
            return from colonyUUID in SafeStringGen()
                   from destType in DestinationTypeGen()
                   from destUUID in SafeStringGen()
                   from purpose in PurposeGen()
                   from fuel in FuelEstimateGen()
                   select new RouteStop
                   {
                       ColonyUUID = colonyUUID,
                       Sequence = sequence,
                       DestinationType = destType,
                       DestinationUUID = destUUID,
                       Purpose = purpose,
                       FuelEstimate = fuel,
                   };
        }

        private static Gen<DeliveryRoute> ValidDeliveryRouteGen()
        {
            return from name in SafeStringGen()
                   from stopCount in Gen.Choose(0, 10)
                   from stops in GenStops(stopCount)
                   select new DeliveryRoute
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       OwnerUUID = Guid.NewGuid().ToString(),
                       Stops = stops,
                   };
        }

        private static Gen<List<RouteStop>> GenStops(int count)
        {
            if (count == 0)
            {
                return Gen.Constant(new List<RouteStop>());
            }

            var gens = new Gen<RouteStop>[count];
            for (int i = 0; i < count; i++)
            {
                gens[i] = RouteStopGen(i);
            }

            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Gen<DeliveryRoute> ValidDeliveryRouteWithStopsGen()
        {
            return from name in SafeStringGen()
                   from stopCount in Gen.Choose(1, 10)
                   from stops in GenStops(stopCount)
                   select new DeliveryRoute
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       OwnerUUID = Guid.NewGuid().ToString(),
                       Stops = stops,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all fields
        // Feature: bl-112-deliveryroute-readonly, Property 1
        // **Validates: Requirements 4.1, 4.2, 4.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidDeliveryRouteGen().ToArbitrary(), route =>
            {
                var ro = new ReadOnlyDeliveryRoute(route);
                var vm = new DeliveryRouteViewModel();
                vm.LoadFrom(ro);

                bool nameMatch = vm.Name == (ro.Name ?? string.Empty);
                var roStops = ro.Stops;
                bool countMatch = vm.Stops.Count == roStops.Count;

                bool stopsMatch = countMatch;
                if (countMatch)
                {
                    for (int i = 0; i < vm.Stops.Count; i++)
                    {
                        var local = vm.Stops[i];
                        var orig = roStops[i];
                        if (local.ColonyUUID != orig.ColonyUUID
                            || local.Sequence != orig.Sequence
                            || local.DestinationType != orig.DestinationType
                            || local.DestinationUUID != orig.DestinationUUID
                            || local.Purpose != orig.Purpose
                            || local.FuelEstimate != orig.FuelEstimate)
                        {
                            stopsMatch = false;
                            break;
                        }
                    }
                }

                return (nameMatch && stopsMatch)
                    .Label(
                        "name=" + nameMatch
                        + ", stopsCount=" + countMatch
                        + ", stopsMatch=" + stopsMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: IsDirty false immediately after LoadFrom
        // Feature: bl-112-deliveryroute-readonly, Property 2
        // **Validates: Requirements 7.1, 7.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidDeliveryRouteGen().ToArbitrary(), route =>
            {
                var ro = new ReadOnlyDeliveryRoute(route);
                var vm = new DeliveryRouteViewModel();
                vm.LoadFrom(ro);

                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects Name change
        // Feature: bl-112-deliveryroute-readonly, Property 3
        // **Validates: Requirements 7.1, 7.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsNameChange()
        {
            return Prop.ForAll(
                ValidDeliveryRouteGen().ToArbitrary(),
                Arb.From(SafeStringGen().Where(s => s.Length > 0)),
                (route, suffix) =>
                {
                    var ro = new ReadOnlyDeliveryRoute(route);
                    var vm = new DeliveryRouteViewModel();
                    vm.LoadFrom(ro);

                    vm.Name = vm.Name + suffix;

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing Name");
                });
        }

        // -----------------------------------------------------------------------
        // Property 4: IsDirty detects Stops change
        // Feature: bl-112-deliveryroute-readonly, Property 4
        // **Validates: Requirements 7.1, 7.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsStopsChange()
        {
            var mutationGen = Gen.Choose(0, 2);

            return Prop.ForAll(
                ValidDeliveryRouteWithStopsGen().ToArbitrary(),
                Arb.From(mutationGen),
                (route, mutation) =>
                {
                    var ro = new ReadOnlyDeliveryRoute(route);
                    var vm = new DeliveryRouteViewModel();
                    vm.LoadFrom(ro);

                    string mutationType;
                    switch (mutation)
                    {
                        case 0:
                            vm.AddStop("new-dest-uuid");
                            mutationType = "AddStop";
                            break;
                        case 1:
                            vm.RemoveStop(0);
                            mutationType = "RemoveStop";
                            break;
                        default:
                            if (vm.Stops.Count >= 2)
                            {
                                vm.MoveStopDown(0);
                                mutationType = "MoveStopDown(reorder)";
                            }
                            else
                            {
                                vm.AddStop("extra-stop");
                                mutationType = "AddStop(fallback)";
                            }

                            break;
                    }

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after " + mutationType);
                });
        }
    }
}
