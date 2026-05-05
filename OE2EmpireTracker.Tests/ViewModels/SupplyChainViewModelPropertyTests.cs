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
    /// Property-based tests for SupplyChainViewModel edit buffer.
    /// Feature: bl-120-supplychain-readonly
    /// </summary>
    [TestFixture]
    public class SupplyChainViewModelPropertyTests
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

        private static Gen<DestinationType> LocationTypeGen()
        {
            return Gen.Elements(
                DestinationType.Colony,
                DestinationType.Station,
                DestinationType.Asteroid,
                DestinationType.Ship);
        }

        private static Gen<SupplyChainStage> StageGen()
        {
            return from seq in Gen.Choose(1, 100)
                   from stageType in StageTypeGen()
                   from locType in LocationTypeGen()
                   from locUUID in SafeStringGen()
                   from resource in SafeStringGen()
                   from purity in SafeStringGen()
                   from threshold in Gen.Choose(0, 1000)
                   from rate in Gen.Choose(0, 100)
                   from routeUUID in SafeStringGen()
                   select new SupplyChainStage
                   {
                       Sequence = seq,
                       StageType = stageType,
                       LocationType = locType,
                       LocationUUID = locUUID,
                       ResourceName = resource,
                       ResourcePurity = purity,
                       AccumulationThreshold = threshold,
                       ProductionRatePerHour = rate,
                       DeliveryRouteUUID = routeUUID,
                   };
        }

        private static Gen<SupplyChain> ValidSupplyChainGen()
        {
            return from uuid in SafeStringGen()
                   from owner in SafeStringGen()
                   from name in SafeStringGen()
                   from active in Arb.Generate<bool>()
                   from count in Gen.Choose(0, 5)
                   from stages in Gen.ListOf(count, StageGen())
                   select new SupplyChain
                   {
                       UUID = uuid,
                       OwnerUUID = owner,
                       Name = name,
                       IsActive = active,
                       Stages = stages.ToList(),
                   };
        }

        /// <summary>
        /// Property 1: LoadFrom Round-Trip Preserves All Fields
        /// **Validates: Requirements 3.1, 3.2, 3.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidSupplyChainGen().ToArbitrary(), chain =>
            {
                var ro = new ReadOnlySupplyChain(chain);
                var vm = new SupplyChainViewModel();
                vm.LoadFrom(ro);

                return (vm.UUID == chain.UUID)
                    .And(vm.OwnerUUID == chain.OwnerUUID)
                    .And(vm.Name == chain.Name)
                    .And(vm.IsActive == chain.IsActive)
                    .And(vm.Stages.Count == chain.Stages.Count);
            });
        }

        /// <summary>
        /// Property 2: IsDirty False Immediately After LoadFrom
        /// **Validates: Requirements 5.1, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidSupplyChainGen().ToArbitrary(), chain =>
            {
                var ro = new ReadOnlySupplyChain(chain);
                var vm = new SupplyChainViewModel();
                vm.LoadFrom(ro);
                return (!vm.IsDirty).ToProperty();
            });
        }

        /// <summary>
        /// Property 3: IsDirty Detects Scalar Field Change
        /// **Validates: Requirements 5.1, 5.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsScalarFieldChange()
        {
            return Prop.ForAll(ValidSupplyChainGen().ToArbitrary(), chain =>
            {
                var ro = new ReadOnlySupplyChain(chain);
                var vm = new SupplyChainViewModel();
                vm.LoadFrom(ro);
                vm.Name = chain.Name + "_changed";
                return vm.IsDirty.ToProperty();
            });
        }

        /// <summary>
        /// Property 4: IsDirty Detects Stages Change
        /// **Validates: Requirements 5.1, 5.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsStagesChange()
        {
            return Prop.ForAll(ValidSupplyChainGen().ToArbitrary(), chain =>
            {
                var ro = new ReadOnlySupplyChain(chain);
                var vm = new SupplyChainViewModel();
                vm.LoadFrom(ro);
                vm.AddStage(new SupplyChainStage
                {
                    Sequence = 99,
                    StageType = SupplyChainStageType.Mine,
                    ResourceName = "TestResource",
                });
                return vm.IsDirty.ToProperty();
            });
        }
    }
}