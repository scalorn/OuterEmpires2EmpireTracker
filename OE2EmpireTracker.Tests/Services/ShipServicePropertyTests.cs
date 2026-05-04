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
    /// Property-based tests for ShipService.
    /// Feature: bl-116-ship-readonly
    /// </summary>
    [TestFixture]
    public class ShipServicePropertyTests
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
            if (count == 0) return Gen.Constant(new List<ShipComponentSlot>());
            var gens = new Gen<ShipComponentSlot>[count];
            for (int i = 0; i < count; i++) gens[i] = ComponentSlotGen();
            return Gen.Sequence(gens).Select(s => s.ToList());
        }

        private static Gen<ShipUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from templateUUID in SafeStringGen()
                   from hullUUID in SafeStringGen()
                   from locType in Gen.Choose(0, 2)
                   from locUUID in SafeStringGen()
                   from hullHP in Gen.Choose(0, 1000)
                   from hullMaxHP in Gen.Choose(0, 1000)
                   from hullRepair in Gen.Choose(0, 100)
                   from compCount in Gen.Choose(0, 5)
                   from components in ComponentListGen(compCount)
                   select new ShipUpdateRequest
                   {
                       Name = name,
                       TemplateUUID = templateUUID,
                       HullBlueprintUUID = hullUUID,
                       LocationType = (DestinationType)locType,
                       LocationUUID = locUUID,
                       HullCurrentHP = hullHP,
                       HullMaxHP = hullMaxHP,
                       HullMaxRepairPercent = (decimal)hullRepair / 100m,
                       Components = components,
                       Cargo = new ItemBag(),
                       Hopper = new ItemBag(),
                   };
        }

        private static Gen<ShipCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   select new ShipCreateRequest
                   {
                       Name = name,
                   };
        }

        // Property 8: Service.Update Round-Trip
        // **Validates: Requirements 11.3, 11.4, 11.5, 11.6, 11.9**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipService(ctx);
                var seed = new Ship
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    Name = "Seed",
                    HullBlueprintUUID = "old-hull",
                };
                ctx.AddShip(seed);
                var result = svc.Update(seed.UUID, request);
                bool nameMatch = result.Name == request.Name;
                bool hullMatch = result.HullBlueprintUUID == request.HullBlueprintUUID;
                bool locTypeMatch = result.LocationType == request.LocationType;
                bool locUUIDMatch = result.LocationUUID == request.LocationUUID;
                bool hpMatch = result.HullCurrentHP == request.HullCurrentHP
                    && result.HullMaxHP == request.HullMaxHP
                    && result.HullMaxRepairPercent == request.HullMaxRepairPercent;
                int expectedCount = request.Components != null ? request.Components.Count : 0;
                bool countMatch = result.Components.Count == expectedCount;
                bool compMatch = countMatch;
                if (countMatch && request.Components != null)
                {
                    var sortedR = result.Components.OrderBy(x => x.SlotType).ThenBy(x => x.SlotIndex).ToList();
                    var sortedE = request.Components.OrderBy(x => x.SlotType).ThenBy(x => x.SlotIndex).ToList();
                    for (int i = 0; i < sortedR.Count; i++)
                    {
                        var a = sortedR[i];
                        var e = sortedE[i];
                        if (a.SlotType != e.SlotType || a.SlotIndex != e.SlotIndex || a.BlueprintUUID != e.BlueprintUUID
                            || a.CurrentHP != e.CurrentHP || a.MaxHP != e.MaxHP || a.MaxRepairPercent != e.MaxRepairPercent)
                        {
                            compMatch = false;
                            break;
                        }
                    }
                }

                return (nameMatch && hullMatch && locTypeMatch && locUUIDMatch && hpMatch && compMatch)
                    .Label("name=" + nameMatch + ", hull=" + hullMatch + ", loc=" + locTypeMatch + ", hp=" + hpMatch + ", comp=" + compMatch);
            });
        }

        // Property 9: Service.Create Round-Trip
        // **Validates: Requirements 12.2, 12.4, 12.8**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipService(ctx);
                var result = svc.Create(request);
                if (string.IsNullOrEmpty(result.UUID)) return false.Label("UUID was empty");
                bool nameMatch = result.Name == request.Name || (string.IsNullOrWhiteSpace(request.Name) && result.Name == "New Ship");
                bool ownerMatch = result.OwnerUUID == "test-player-uuid";
                return (nameMatch && ownerMatch)
                    .Label("name=" + nameMatch + ", owner=" + ownerMatch);
            });
        }

        // Property 10: Service.Delete Removes Ship
        // **Validates: Requirements 13.1, 13.2**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesShip()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipService(ctx);
                var created = svc.Create(request);
                string uuid = created.UUID;
                svc.Delete(uuid);
                var found = ctx.FindMutableShip(uuid);
                return (found == null).Label(found == null ? "OK" : "Ship still exists after Delete");
            });
        }
    }
}