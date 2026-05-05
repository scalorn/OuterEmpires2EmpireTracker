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
    /// Property-based tests for StationService.
    /// Feature: bl-117-station-readonly
    /// </summary>
    [TestFixture]
    public class StationServicePropertyTests
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

        private static Gen<StationUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from bpUUID in SafeStringGen()
                   from stationType in Gen.Choose(0, 2)
                   from ownership in Gen.Choose(0, 1)
                   from hullHP in Gen.Choose(0, 1000)
                   from hullMaxHP in Gen.Choose(0, 1000)
                   from hullRepair in Gen.Choose(0, 100)
                   from compCount in Gen.Choose(0, 5)
                   from components in ComponentListGen(compCount)
                   select new StationUpdateRequest
                   {
                       Name = name,
                       StationType = (StationType)stationType,
                       Ownership = (StationOwnership)ownership,
                       StationBlueprintUUID = bpUUID,
                       HullCurrentHP = hullHP,
                       HullMaxHP = hullMaxHP,
                       HullMaxRepairPercent = (decimal)hullRepair / 100m,
                       Components = components,
                       Hold = new ItemBag(),
                       MunitionsHold = new ItemBag(),
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
                var svc = new StationService(ctx);
                var seed = new Station { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player-uuid", Name = "Seed" };
                ctx.AddStation(seed);
                var result = svc.Update(seed.UUID, request);
                bool nameMatch = result.Name == request.Name;
                bool typeMatch = result.StationType == request.StationType;
                bool ownershipMatch = result.Ownership == request.Ownership;
                bool bpMatch = result.StationBlueprintUUID == request.StationBlueprintUUID;
                bool hpMatch = result.HullCurrentHP == request.HullCurrentHP && result.HullMaxHP == request.HullMaxHP && result.HullMaxRepairPercent == request.HullMaxRepairPercent;
                int expectedCount = request.Components != null ? request.Components.Count : 0;
                bool compMatch = result.Components.Count == expectedCount;
                return (nameMatch && typeMatch && ownershipMatch && bpMatch && hpMatch && compMatch)
                    .Label("name=" + nameMatch + " type=" + typeMatch + " bp=" + bpMatch + " hp=" + hpMatch + " comp=" + compMatch);
            });
        }

        // Property 9: Service.Create Round-Trip
        // **Validates: Requirements 12.2, 12.4, 12.8**
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesFields()
        {
            return Prop.ForAll(Arb.From(SafeStringGen().Select(n => new StationCreateRequest { Name = n })), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new StationService(ctx);
                var result = svc.Create(request);
                if (string.IsNullOrEmpty(result.UUID)) return false.Label("UUID was empty");
                bool nameMatch = result.Name == request.Name || (string.IsNullOrWhiteSpace(request.Name) && result.Name == "New Station");
                bool ownerMatch = result.OwnerUUID == "test-player-uuid";
                return (nameMatch && ownerMatch).Label("name=" + nameMatch + " owner=" + ownerMatch);
            });
        }

        // Property 10: Service.Delete Removes Station
        // **Validates: Requirements 13.1, 13.2**
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesStation()
        {
            return Prop.ForAll(Arb.From(SafeStringGen().Select(n => new StationCreateRequest { Name = n })), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new StationService(ctx);
                var created = svc.Create(request);
                string uuid = created.UUID;
                svc.Delete(uuid);
                var found = ctx.FindMutableStation(uuid);
                return (found == null).Label(found == null ? "OK" : "Station still exists");
            });
        }
    }
}
