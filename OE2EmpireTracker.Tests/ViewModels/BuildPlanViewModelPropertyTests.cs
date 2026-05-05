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
    /// Property-based tests for BuildPlanViewModel edit buffer.
    /// Feature: bl-118-buildplan-readonly
    /// </summary>
    [TestFixture]
    public class BuildPlanViewModelPropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<BuildItemType> BuildItemTypeGen()
        {
            return Gen.Elements(
                BuildItemType.Manufactory,
                BuildItemType.Commodity,
                BuildItemType.ShipTemplate,
                BuildItemType.Mining,
                BuildItemType.Refining,
                BuildItemType.Research);
        }

        private static Gen<BuildItemStatus> BuildItemStatusGen()
        {
            return Gen.Elements(
                BuildItemStatus.Staged,
                BuildItemStatus.Delivering,
                BuildItemStatus.Ready,
                BuildItemStatus.InProgress,
                BuildItemStatus.Completed);
        }

        private static Gen<BuildItem> BuildItemGen()
        {
            return from uuid in SafeStringGen()
                   from itemType in BuildItemTypeGen()
                   from status in BuildItemStatusGen()
                   from bpUUID in SafeStringGen()
                   from name in SafeStringGen()
                   from qty in Gen.Choose(1, 100)
                   from seq in Gen.Choose(0, 10)
                   select new BuildItem
                   {
                       UUID = uuid,
                       ItemType = itemType,
                       Status = status,
                       BlueprintUUID = bpUUID,
                       ItemName = name,
                       Quantity = qty,
                       SequenceInStructure = seq,
                   };
        }

        private static Gen<BuildPlan> ValidBuildPlanGen()
        {
            return from uuid in SafeStringGen()
                   from owner in SafeStringGen()
                   from name in SafeStringGen()
                   from desc in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from items in Gen.ListOf(count, BuildItemGen())
                   select new BuildPlan
                   {
                       UUID = uuid,
                       OwnerUUID = owner,
                       Name = name,
                       Description = desc,
                       IsActive = active,
                       Items = items.ToList(),
                   };
        }

        /// <summary>
        /// Property 1: LoadFrom Round-Trip Preserves All Fields
        /// **Validates: Requirements 3.1, 3.2, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidBuildPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyBuildPlan(plan);
                var vm = new BuildPlanViewModel();
                vm.LoadFrom(ro);

                return (vm.UUID == plan.UUID)
                    .And(vm.OwnerUUID == plan.OwnerUUID)
                    .And(vm.Name == plan.Name)
                    .And(vm.Description == plan.Description)
                    .And(vm.IsActive == plan.IsActive)
                    .And(vm.Items.Count == plan.Items.Count);
            });
        }

        /// <summary>
        /// Property 2: IsDirty False Immediately After LoadFrom
        /// **Validates: Requirements 6.1, 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidBuildPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyBuildPlan(plan);
                var vm = new BuildPlanViewModel();
                vm.LoadFrom(ro);
                return (!vm.IsDirty).ToProperty();
            });
        }

        /// <summary>
        /// Property 3: IsDirty Detects Scalar Field Change
        /// **Validates: Requirements 6.1, 6.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsScalarFieldChange()
        {
            return Prop.ForAll(ValidBuildPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyBuildPlan(plan);
                var vm = new BuildPlanViewModel();
                vm.LoadFrom(ro);
                vm.Name = plan.Name + "_changed";
                return vm.IsDirty.ToProperty();
            });
        }

        /// <summary>
        /// Property 4: IsDirty Detects Items Change
        /// **Validates: Requirements 6.1, 6.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsItemsChange()
        {
            return Prop.ForAll(ValidBuildPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyBuildPlan(plan);
                var vm = new BuildPlanViewModel();
                vm.LoadFrom(ro);
                vm.AddItem(new BuildItem { UUID = Guid.NewGuid().ToString(), ItemName = "test" });
                return vm.IsDirty.ToProperty();
            });
        }
    }
}
