using System;
using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for AsteroidViewModel edit buffer.
    /// Feature: bl-122-asteroid-readonly
    /// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 5.1, 5.2, 5.3
    /// </summary>
    [TestFixture]
    public class AsteroidViewModelPropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<AsteroidReserve> ValidReserveGen()
        {
            return from resName in SafeStringGen()
                   from purity in SafeStringGen()
                   from maxRes in Gen.Choose(1, 10000)
                   from curRes in Gen.Choose(0, 10000)
                   from ts in SafeStringGen()
                   select new AsteroidReserve
                   {
                       ResourceName = resName,
                       Purity = purity,
                       MaxReserve = maxRes,
                       CurrentReserve = curRes,
                       ResetTimestamp = ts,
                   };
        }

        private static Gen<Asteroid> ValidAsteroidGen()
        {
            return from name in SafeStringGen()
                   from sys in SafeStringGen()
                   from reserves in Gen.ListOf(ValidReserveGen())
                   select new Asteroid
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       SystemName = sys,
                       Reserves = new List<AsteroidReserve>(reserves),
                   };
        }

        /// <summary>
        /// Property 1: LoadFrom Round-Trip Preserves All Fields.
        /// Validates: Requirements 3.1, 3.2, 3.3, 3.4
        /// </summary>
        [Test]
        public void LoadFrom_RoundTrip_PreservesAllFields()
        {
            Prop.ForAll(ValidAsteroidGen().ToArbitrary(), asteroid =>
            {
                var vm = new AsteroidViewModel();
                var ro = new ReadOnlyAsteroid(asteroid);
                vm.LoadFrom(ro);

                if (vm.UUID != asteroid.UUID) return false.ToProperty();
                if (vm.Name != asteroid.Name) return false.ToProperty();
                if (vm.SystemName != asteroid.SystemName) return false.ToProperty();
                if (vm.Reserves.Count != asteroid.Reserves.Count) return false.ToProperty();

                for (int i = 0; i < asteroid.Reserves.Count; i++)
                {
                    var orig = asteroid.Reserves[i];
                    var local = vm.Reserves[i];
                    if (local.ResourceName != orig.ResourceName) return false.ToProperty();
                    if (local.Purity != orig.Purity) return false.ToProperty();
                    if (local.MaxReserve != orig.MaxReserve) return false.ToProperty();
                    if (local.CurrentReserve != orig.CurrentReserve) return false.ToProperty();
                    if (local.ResetTimestamp != orig.ResetTimestamp) return false.ToProperty();
                }

                return (vm.Original == ro).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 2: IsDirty False After LoadFrom.
        /// Validates: Requirements 5.1, 5.3
        /// </summary>
        [Test]
        public void IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            Prop.ForAll(ValidAsteroidGen().ToArbitrary(), asteroid =>
            {
                var vm = new AsteroidViewModel();
                var ro = new ReadOnlyAsteroid(asteroid);
                vm.LoadFrom(ro);

                return (!vm.IsDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 3: IsDirty Detects Name Change.
        /// Validates: Requirements 5.2
        /// </summary>
        [Test]
        public void IsDirty_DetectsNameChange()
        {
            Prop.ForAll(ValidAsteroidGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (asteroid, newName) =>
            {
                var vm = new AsteroidViewModel();
                var ro = new ReadOnlyAsteroid(asteroid);
                vm.LoadFrom(ro);
                vm.Name = newName;

                bool shouldBeDirty = newName != asteroid.Name;
                return (vm.IsDirty == shouldBeDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property: IsDirty Detects SystemName Change.
        /// Validates: Requirements 5.2
        /// </summary>
        [Test]
        public void IsDirty_DetectsSystemNameChange()
        {
            Prop.ForAll(ValidAsteroidGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (asteroid, newSys) =>
            {
                var vm = new AsteroidViewModel();
                var ro = new ReadOnlyAsteroid(asteroid);
                vm.LoadFrom(ro);
                vm.SystemName = newSys;

                bool shouldBeDirty = newSys != asteroid.SystemName;
                return (vm.IsDirty == shouldBeDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
