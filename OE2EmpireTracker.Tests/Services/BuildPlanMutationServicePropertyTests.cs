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
    /// Property-based tests for BuildPlanMutationService.
    /// Feature: bl-118-buildplan-readonly
    /// </summary>
    [TestFixture]
    public class BuildPlanMutationServicePropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<BuildItem> BuildItemGen()
        {
            return from uuid in SafeStringGen()
                   from name in SafeStringGen()
                   from qty in Gen.Choose(1, 100)
                   select new BuildItem
                   {
                       UUID = uuid,
                       ItemType = BuildItemType.Manufactory,
                       ItemName = name,
                       Quantity = qty,
                   };
        }

        private static Gen<BuildPlanUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from desc in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from items in Gen.ListOf(count, BuildItemGen())
                   select new BuildPlanUpdateRequest
                   {
                       Name = name,
                       Description = desc,
                       IsActive = active,
                       Items = items.ToList(),
                   };
        }

        private static Gen<BuildPlanCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from desc in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from items in Gen.ListOf(count, BuildItemGen())
                   select new BuildPlanCreateRequest
                   {
                       Name = name,
                       Description = desc,
                       IsActive = active,
                       Items = items.ToList(),
                   };
        }

        /// <summary>
        /// Property 5: Service.Update Round-Trip
        /// **Validates: Requirements 11.3, 11.4, 11.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new BuildPlanMutationService(ctx);
                var seed = new BuildPlan { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player", Name = "Seed" };
                ctx.AddBuildPlan(seed);
                var result = svc.Update(seed.UUID, request);
                return (result.Name == request.Name)
                    .And(result.Description == request.Description)
                    .And(result.IsActive == request.IsActive)
                    .And(result.Items.Count == (request.Items != null ? request.Items.Count : 0));
            });
        }

        /// <summary>
        /// Property 6: Service.Create Round-Trip
        /// **Validates: Requirements 12.2, 12.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new BuildPlanMutationService(ctx);
                var result = svc.Create(request);
                return (result.Name == request.Name)
                    .And(result.Description == request.Description)
                    .And(result.IsActive == request.IsActive)
                    .And(!string.IsNullOrEmpty(result.UUID))
                    .And(result.Items.Count == (request.Items != null ? request.Items.Count : 0));
            });
        }

        /// <summary>
        /// Property 7: Service.Delete Removes Plan
        /// **Validates: Requirements 13.1, 13.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesPlan()
        {
            return Prop.ForAll(Arb.From(SafeStringGen()), name =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player";
                var svc = new BuildPlanMutationService(ctx);
                var req = new BuildPlanCreateRequest { Name = name, Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
                var created = svc.Create(req);
                svc.Delete(created.UUID);
                var found = ctx.FindBuildPlan(created.UUID);
                return (found == null).ToProperty();
            });
        }
    }
}
