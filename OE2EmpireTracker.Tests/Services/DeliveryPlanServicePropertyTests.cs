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
    /// Property-based tests for DeliveryPlanService.
    /// Feature: bl-113-deliveryplan-readonly, Properties 1-7 from the design document.
    /// </summary>
    [TestFixture]
    public class DeliveryPlanServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generators
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
                   select new DeliveryItem
                   {
                       ItemType = itemType,
                       BaseItemTypeID = baseId,
                       Name = name,
                       ResourcePurity = purity,
                       Quantity = qty,
                       Delivered = false,
                   };
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
                   from destType in DestinationTypeGen()
                   from destUUID in SafeStringGen()
                   from dropCount in Gen.Choose(0, 3)
                   from dropOff in GenItems(dropCount)
                   from pickCount in Gen.Choose(0, 3)
                   from pickUp in GenItems(pickCount)
                   select new DeliveryPlanStop
                   {
                       ColonyUUID = colonyUUID,
                       Sequence = sequence,
                       StopCompleted = false,
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

        private static Gen<DeliveryPlanUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from stopCount in Gen.Choose(0, 5)
                   from stops in GenPlanStops(stopCount)
                   select new DeliveryPlanUpdateRequest
                   {
                       Name = name,
                       Stops = stops,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 1: Service.Create Round-Trip
        // **Validates: Requirements 8.2, 8.4, 8.5, 8.9**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesNameAndRouteUUID()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name, routeUUID) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var result = svc.Create(name, routeUUID);

                bool uuidNonEmpty = !string.IsNullOrEmpty(result.UUID);
                bool nameMatch = result.Name == name;
                bool routeMatch = result.RouteUUID == routeUUID;

                return (uuidNonEmpty && nameMatch && routeMatch)
                    .Label(
                        "uuid=" + uuidNonEmpty
                        + ", name=" + nameMatch
                        + ", route=" + routeMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: Service.UpdatePlan Round-Trip
        // **Validates: Requirements 10.3, 10.4, 10.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property UpdatePlan_RoundTrip_PreservesNameAndStops()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create("Seed", "route-1");

                var result = svc.UpdatePlan(created.UUID, request);

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
                            || actual.DropOff.Count != expected.DropOff.Count
                            || actual.PickUp.Count != expected.PickUp.Count)
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
        // Property 3: Service.Delete Removes Plan
        // **Validates: Requirements 9.1, 9.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesPlan()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create(name, "route-1");
                string uuid = created.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutableDeliveryPlan(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Plan still exists after Delete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.MarkItemDelivered Sets Flag
        // **Validates: Requirements 13.1, 13.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property MarkItemDelivered_SetsFlag()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (itemName, baseId) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create("MarkTest", "route-1");
                var destInfo = new StopDestinationInfo
                {
                    ColonyUUID = "colony-1",
                    Sequence = 0,
                    DestinationType = DestinationType.Colony,
                    DestinationUUID = "dest-1",
                };
                var itemInfo = new DeliveryItemInfo
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = baseId,
                    Name = itemName,
                    Quantity = 10,
                    ResourcePurity = string.Empty,
                };
                svc.AddDropOffItem(created.UUID, destInfo, itemInfo);

                var result = svc.MarkItemDelivered(created.UUID, 0, 0, "DropOff", true);

                var stop = result.Stops.FirstOrDefault(s => s.Sequence == 0);
                bool delivered = stop != null && stop.DropOff.Count > 0 && stop.DropOff[0].Delivered;

                return delivered.Label(delivered ? "OK" : "Item not marked delivered");
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.MarkStopComplete Sets Flag
        // **Validates: Requirements 14.1, 14.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property MarkStopComplete_SetsFlag()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create(name, "route-1");
                var destInfo = new StopDestinationInfo
                {
                    ColonyUUID = "colony-1",
                    Sequence = 0,
                    DestinationType = DestinationType.Colony,
                    DestinationUUID = "dest-1",
                };
                var itemInfo = new DeliveryItemInfo
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = "Steel",
                    Name = "Steel",
                    Quantity = 5,
                    ResourcePurity = string.Empty,
                };
                svc.AddDropOffItem(created.UUID, destInfo, itemInfo);

                var result = svc.MarkStopComplete(created.UUID, 0);

                var stop = result.Stops.FirstOrDefault(s => s.Sequence == 0);
                bool completed = stop != null && stop.StopCompleted;

                return completed.Label(completed ? "OK" : "Stop not marked complete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.MarkPlanComplete Sets Flag
        // **Validates: Requirements 15.1, 15.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property MarkPlanComplete_SetsFlag()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create(name, "route-1");

                var result = svc.MarkPlanComplete(created.UUID);

                return result.Completed.Label(
                    result.Completed ? "OK" : "Plan not marked complete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 7: AddDropOffItem Increases Item Count
        // **Validates: Requirements 11.2, 11.6**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property AddDropOffItem_IncreasesItemCount()
        {
            return Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (itemName, baseId) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new DeliveryPlanService(ctx);

                var created = svc.Create("CountTest", "route-1");
                var destInfo = new StopDestinationInfo
                {
                    ColonyUUID = "colony-1",
                    Sequence = 0,
                    DestinationType = DestinationType.Colony,
                    DestinationUUID = "dest-1",
                };
                var itemInfo = new DeliveryItemInfo
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = baseId,
                    Name = itemName,
                    Quantity = 10,
                    ResourcePurity = string.Empty,
                };

                var result = svc.AddDropOffItem(created.UUID, destInfo, itemInfo);

                var stop = result.Stops.FirstOrDefault(s => s.DestinationUUID == "dest-1");
                bool hasItem = stop != null && stop.DropOff.Count == 1;

                return hasItem.Label(hasItem ? "OK" : "DropOff item not added");
            });
        }
    }
}