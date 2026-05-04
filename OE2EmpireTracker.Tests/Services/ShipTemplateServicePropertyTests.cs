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
    /// Property-based tests for ShipTemplateService.
    /// Feature: bl-115-shiptemplate-readonly
    /// </summary>
    [TestFixture]
    public class ShipTemplateServicePropertyTests
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

        private static Gen<ShipTemplateUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from hullUUID in SafeStringGen()
                   from compCount in Gen.Choose(0, 5)
                   from components in ComponentListGen(compCount)
                   select new ShipTemplateUpdateRequest
                   {
                       Name = name,
                       HullBlueprintUUID = hullUUID,
                       Components = components,
                   };
        }

        private static Gen<ShipTemplateCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from hullUUID in SafeStringGen()
                   from compCount in Gen.Choose(0, 5)
                   from components in ComponentListGen(compCount)
                   select new ShipTemplateCreateRequest
                   {
                       Name = name,
                       HullBlueprintUUID = hullUUID,
                       Components = components,
                   };
        }

        // Property 6: Service.Update Round-Trip
        // **Validates: Requirements 11.3, 11.4, 11.7**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipTemplateService(ctx);
                var seed = new ShipTemplate
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    Name = "Seed",
                    HullBlueprintUUID = "old-hull",
                };
                ctx.AddShipTemplate(seed);
                var result = svc.Update(seed.UUID, request);
                bool nameMatch = result.Name == request.Name;
                bool hullMatch = result.HullBlueprintUUID == request.HullBlueprintUUID;
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

                return (nameMatch && hullMatch && compMatch)
                    .Label("name=" + nameMatch + ", hull=" + hullMatch + ", comp=" + compMatch);
            });
        }

        // Property 7: Service.Create Round-Trip
        // **Validates: Requirements 12.2, 12.4, 12.8**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipTemplateService(ctx);
                var result = svc.Create(request);
                if (string.IsNullOrEmpty(result.UUID)) return false.Label("UUID was empty");
                bool nameMatch = result.Name == request.Name;
                bool hullMatch = result.HullBlueprintUUID == request.HullBlueprintUUID;
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

                return (nameMatch && hullMatch && compMatch)
                    .Label("name=" + nameMatch + ", hull=" + hullMatch + ", comp=" + compMatch);
            });
        }

        // Property 8: Service.Delete Removes Template
        // **Validates: Requirements 13.1, 13.2**

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesTemplate()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ShipTemplateService(ctx);
                var created = svc.Create(request);
                string uuid = created.UUID;
                svc.Delete(uuid);
                var found = ctx.FindMutableShipTemplate(uuid);
                return (found == null).Label(found == null ? "OK" : "Template still exists after Delete");
            });
        }
    }
}