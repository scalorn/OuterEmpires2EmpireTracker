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
    /// Property-based tests for SupplyChainMutationService.
    /// Feature: bl-120-supplychain-readonly
    /// </summary>
    [TestFixture]
    public class SupplyChainMutationServicePropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<SupplyChainStageType> StageTypeGen()
        {
            return Gen.Elements(
                SupplyChainStageType.Mine,
                SupplyChainStageType.AsteroidMine,
                SupplyChainStageType.PickUp,
                SupplyChainStageType.Refine,
                SupplyChainStageType.Deliver,
                SupplyChainStageType.Research);
        }

        private static Gen<SupplyChainStage> StageGen()
        {
            return from seq in Gen.Choose(1, 100)
                   from stageType in StageTypeGen()
                   from resource in SafeStringGen()
                   select new SupplyChainStage
                   {
                       Sequence = seq,
                       StageType = stageType,
                       ResourceName = resource,
                   };
        }

        private static Gen<SupplyChainUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from stages in Gen.ListOf(count, StageGen())
                   select new SupplyChainUpdateRequest
                   {
                       Name = name,
                       IsActive = active,
                       Stages = stages.ToList(),
                   };
        }

        private static Gen<SupplyChainCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from stages in Gen.ListOf(count, StageGen())
                   select new SupplyChainCreateRequest
                   {
                       Name = name,
                       IsActive = active,
                       Stages = stages.ToList(),
                   };
        }

        /// <summary>
        /// Property 4: Service.Update Round-Trip
        /// **Validates: Requirements 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new SupplyChainMutationService(ctx);
                var seed = new SupplyChain { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player", Name = "Seed" };
                ctx.AddSupplyChain(seed);
                var result = svc.Update(seed.UUID, request);
                return (result.Name == request.Name)
                    .And(result.IsActive == request.IsActive)
                    .And(result.Stages.Count == (request.Stages != null ? request.Stages.Count : 0));
            });
        }

        /// <summary>
        /// Property 5: Service.Create Round-Trip
        /// **Validates: Requirements 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new SupplyChainMutationService(ctx);
                var result = svc.Create(request);
                return (result.Name == request.Name)
                    .And(result.IsActive == request.IsActive)
                    .And(!string.IsNullOrEmpty(result.UUID))
                    .And(result.Stages.Count == (request.Stages != null ? request.Stages.Count : 0));
            });
        }

        /// <summary>
        /// Property 6: Service.Delete Removes Chain
        /// **Validates: Requirements 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesChain()
        {
            return Prop.ForAll(Arb.From(SafeStringGen()), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new SupplyChainMutationService(ctx);
                var req = new SupplyChainCreateRequest { Name = name, IsActive = true, Stages = new List<SupplyChainStage>() };
                var created = svc.Create(req);
                svc.Delete(created.UUID);
                var found = ctx.FindSupplyChain(created.UUID);
                return (found == null).ToProperty();
            });
        }
    }
}