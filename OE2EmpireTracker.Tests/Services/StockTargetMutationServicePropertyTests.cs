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
    /// Property-based tests for StockTargetMutationService.
    /// Feature: bl-119-stocktargets-readonly
    /// </summary>
    [TestFixture]
    public class StockTargetMutationServicePropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<StockTarget> StockTargetGen()
        {
            return from uuid in SafeStringGen()
                   from name in SafeStringGen()
                   from qty in Gen.Choose(1, 100)
                   select new StockTarget
                   {
                       UUID = uuid,
                       ItemType = ItemType.ItemTypeEnum.Commodity,
                       ItemName = name,
                       TargetQuantity = qty,
                   };
        }

        private static Gen<StockPlanUpdateRequest> PlanUpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from repUUID in SafeStringGen()
                   from count in Gen.Choose(0, 3)
                   from targets in Gen.ListOf(count, StockTargetGen())
                   select new StockPlanUpdateRequest
                   {
                       Name = name,
                       IsActive = active,
                       ReplenishmentBuildPlanUUID = repUUID,
                       Targets = targets.ToList(),
                   };
        }

        private static Gen<StockPlanCreateRequest> PlanCreateRequestGen()
        {
            return from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from repUUID in SafeStringGen()
                   from count in Gen.Choose(0, 3)
                   from targets in Gen.ListOf(count, StockTargetGen())
                   select new StockPlanCreateRequest
                   {
                       Name = name,
                       IsActive = active,
                       ReplenishmentBuildPlanUUID = repUUID,
                       Targets = targets.ToList(),
                   };
        }

        /// <summary>
        /// Property 5: Service.UpdatePlan Round-Trip
        /// **Validates: Requirements 9.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property UpdatePlan_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(PlanUpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new StockTargetMutationService(ctx);
                var seed = new StockPlan { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player", Name = "Seed" };
                ctx.AddStockPlan(seed);
                var result = svc.UpdatePlan(seed.UUID, request);
                return (result.Name == request.Name)
                    .And(result.IsActive == request.IsActive)
                    .And(result.ReplenishmentBuildPlanUUID == request.ReplenishmentBuildPlanUUID)
                    .And(result.Targets.Count == (request.Targets != null ? request.Targets.Count : 0));
            });
        }

        /// <summary>
        /// Property 6: Service.CreatePlan Round-Trip
        /// **Validates: Requirements 9.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property CreatePlan_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(PlanCreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new StockTargetMutationService(ctx);
                var result = svc.CreatePlan(request);
                return (result.Name == request.Name)
                    .And(result.IsActive == request.IsActive)
                    .And(!string.IsNullOrEmpty(result.UUID))
                    .And(result.Targets.Count == (request.Targets != null ? request.Targets.Count : 0));
            });
        }

        /// <summary>
        /// Property 7: Service.DeletePlan Removes Plan
        /// **Validates: Requirements 9.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property DeletePlan_RemovesPlan()
        {
            return Prop.ForAll(Arb.From(SafeStringGen()), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new StockTargetMutationService(ctx);
                var req = new StockPlanCreateRequest { Name = name, IsActive = true, ReplenishmentBuildPlanUUID = string.Empty, Targets = new List<StockTarget>() };
                var created = svc.CreatePlan(req);
                svc.DeletePlan(created.UUID);
                var found = ctx.FindMutableStockPlan(created.UUID);
                return (found == null).ToProperty();
            });
        }
    }
}
