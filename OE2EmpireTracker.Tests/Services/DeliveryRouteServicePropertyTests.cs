using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for DeliveryRouteService.
    /// Feature: bl-112-deliveryroute-readonly, Properties 5, 6, 7 from the design document.
    /// </summary>
    [TestFixture]
    public class DeliveryRouteServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generators (reuses the ValidDeliveryRouteGen pattern from ViewModel property tests)
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

        private static Gen<DeliveryRouteUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from stopCount in Gen.Choose(0, 5)
                   from stops in GenStops(stopCount)
                   select new DeliveryRouteUpdateRequest
                   {
                       Name = name,
                       Stops = stops,
                   };
        }

        private static Gen<DeliveryRouteCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from stopCount in Gen.Choose(0, 5)
                   from stops in GenStops(stopCount)
                   select new DeliveryRouteCreateRequest
                   {
                       Name = name,
                       Stops = stops,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Update Round-Trip
        // Feature: bl-112-deliveryroute-readonly, Property 5
        // **Validates: Requirements 12.3, 12.4, 12.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesNameAndStops()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryRouteService(ctx);

                var seed = new DeliveryRoute
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    Name = "Seed",
                };
                ctx.AddDeliveryRoute(seed);

                var result = svc.Update(seed.UUID, request);

                bool nameMatch = result.Name == request.Name;

                bool countMatch = result.Stops.Count == (request.Stops != null ? request.Stops.Count : 0);
                bool stopsMatch = countMatch;
                if (countMatch && request.Stops != null)
                {
                    for (int i = 0; i < result.Stops.Count; i++)
                    {
                        var actual = result.Stops[i];
                        var expected = request.Stops[i];
                        if (actual.ColonyUUID != expected.ColonyUUID
                            || actual.DestinationType != expected.DestinationType
                            || actual.DestinationUUID != expected.DestinationUUID
                            || actual.Purpose != expected.Purpose
                            || actual.FuelEstimate != expected.FuelEstimate
                            || actual.Sequence != (i + 1))
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
        // Property 6: Service.Create Round-Trip
        // Feature: bl-112-deliveryroute-readonly, Property 6
        // **Validates: Requirements 13.2, 13.4, 13.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesNameAndStopsWithNonEmptyUUID()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryRouteService(ctx);

                var result = svc.Create(request);

                if (string.IsNullOrEmpty(result.UUID))
                {
                    return false.Label("UUID was empty");
                }

                bool nameMatch = result.Name == request.Name;

                bool countMatch = result.Stops.Count == (request.Stops != null ? request.Stops.Count : 0);
                bool stopsMatch = countMatch;
                if (countMatch && request.Stops != null)
                {
                    for (int i = 0; i < result.Stops.Count; i++)
                    {
                        var actual = result.Stops[i];
                        var expected = request.Stops[i];
                        if (actual.ColonyUUID != expected.ColonyUUID
                            || actual.DestinationType != expected.DestinationType
                            || actual.DestinationUUID != expected.DestinationUUID
                            || actual.Purpose != expected.Purpose
                            || actual.FuelEstimate != expected.FuelEstimate
                            || actual.Sequence != (i + 1))
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
        // Property 7: Service.Delete Removes Route
        // Feature: bl-112-deliveryroute-readonly, Property 7
        // **Validates: Requirements 14.1, 14.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesRoute()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryRouteService(ctx);

                var created = svc.Create(request);
                string uuid = created.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutableDeliveryRoute(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Route still exists after Delete");
            });
        }
    }
}