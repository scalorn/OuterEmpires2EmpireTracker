using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for StockTargetViewModel edit buffer.
    /// Feature: bl-119-stocktargets-readonly
    /// </summary>
    [TestFixture]
    public class StockTargetViewModelPropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<StockTargetScope> ScopeGen()
        {
            return Gen.Elements(
                StockTargetScope.EmpireWide,
                StockTargetScope.Colony,
                StockTargetScope.Station);
        }

        private static Gen<ItemType.ItemTypeEnum> ItemTypeGen()
        {
            return Gen.Elements(
                ItemType.ItemTypeEnum.Commodity,
                ItemType.ItemTypeEnum.Resource,
                ItemType.ItemTypeEnum.ShipPart,
                ItemType.ItemTypeEnum.ShipHull);
        }

        private static Gen<StockTarget> StockTargetGen()
        {
            return from uuid in SafeStringGen()
                   from itemType in ItemTypeGen()
                   from refId in SafeStringGen()
                   from name in SafeStringGen()
                   from shipUUID in SafeStringGen()
                   from qty in Gen.Choose(1, 1000)
                   from critical in Gen.Choose(0, 500)
                   from scope in ScopeGen()
                   from locUUID in SafeStringGen()
                   select new StockTarget
                   {
                       UUID = uuid,
                       ItemType = itemType,
                       ItemReferenceID = refId,
                       ItemName = name,
                       ShipTemplateUUID = shipUUID,
                       TargetQuantity = qty,
                       CriticalThreshold = critical,
                       Scope = scope,
                       LocationUUID = locUUID,
                   };
        }

        private static Gen<StockPlan> ValidStockPlanGen()
        {
            return from uuid in SafeStringGen()
                   from owner in SafeStringGen()
                   from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from repUUID in SafeStringGen()
                   from count in Gen.Choose(0, 5)
                   from targets in Gen.ListOf(count, StockTargetGen())
                   select new StockPlan
                   {
                       UUID = uuid,
                       OwnerUUID = owner,
                       Name = name,
                       IsActive = active,
                       ReplenishmentBuildPlanUUID = repUUID,
                       Targets = targets.ToList(),
                   };
        }

        private static Gen<StockProfileEntry> EntryGen()
        {
            return from groupId in SafeStringGen()
                   from planUUID in SafeStringGen()
                   select new StockProfileEntry
                   {
                       GroupID = groupId,
                       StockPlanUUID = planUUID,
                   };
        }

        private static Gen<StockProfile> ValidStockProfileGen()
        {
            return from uuid in SafeStringGen()
                   from owner in SafeStringGen()
                   from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from entries in Gen.ListOf(count, EntryGen())
                   select new StockProfile
                   {
                       UUID = uuid,
                       OwnerUUID = owner,
                       Name = name,
                       IsActive = active,
                       Entries = entries.ToList(),
                   };
        }

        /// <summary>
        /// Property 1: LoadFrom Round-Trip Preserves All StockPlan Fields
        /// **Validates: Requirements 4.1, 4.2, 4.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadPlanFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidStockPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyStockPlan(plan);
                var vm = new StockTargetViewModel();
                vm.LoadPlanFrom(ro);

                return (vm.PlanUUID == plan.UUID)
                    .And(vm.PlanOwnerUUID == plan.OwnerUUID)
                    .And(vm.PlanName == plan.Name)
                    .And(vm.PlanIsActive == plan.IsActive)
                    .And(vm.ReplenishmentBuildPlanUUID == plan.ReplenishmentBuildPlanUUID)
                    .And(vm.Targets.Count == plan.Targets.Count);
            });
        }

        /// <summary>
        /// Property 2: LoadFrom Round-Trip Preserves All StockProfile Fields
        /// **Validates: Requirements 5.1, 5.2, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadProfileFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidStockProfileGen().ToArbitrary(), profile =>
            {
                var ro = new ReadOnlyStockProfile(profile);
                var vm = new StockTargetViewModel();
                vm.LoadProfileFrom(ro);

                return (vm.ProfileUUID == profile.UUID)
                    .And(vm.ProfileOwnerUUID == profile.OwnerUUID)
                    .And(vm.ProfileName == profile.Name)
                    .And(vm.ProfileIsActive == profile.IsActive)
                    .And(vm.Entries.Count == profile.Entries.Count);
            });
        }

        /// <summary>
        /// Property 3: IsDirty False After LoadFrom (Plan)
        /// **Validates: Requirements 7.1, 7.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsPlanDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidStockPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyStockPlan(plan);
                var vm = new StockTargetViewModel();
                vm.LoadPlanFrom(ro);
                return (!vm.IsPlanDirty).ToProperty();
            });
        }

        /// <summary>
        /// Property 4: IsDirty False After LoadFrom (Profile)
        /// **Validates: Requirements 7.1, 7.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsProfileDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidStockProfileGen().ToArbitrary(), profile =>
            {
                var ro = new ReadOnlyStockProfile(profile);
                var vm = new StockTargetViewModel();
                vm.LoadProfileFrom(ro);
                return (!vm.IsProfileDirty).ToProperty();
            });
        }
    }
}
