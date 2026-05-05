using System;
using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for AsteroidService CRUD operations.
    /// Feature: bl-122-asteroid-readonly
    /// Validates: Requirements 7.1, 7.2, 7.3, 7.4
    /// </summary>
    [TestFixture]
    public class AsteroidServicePropertyTests
    {
        private PlayerContext _ctx;
        private AsteroidService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.SuppressUI = true;
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());
            _svc = new AsteroidService(_ctx);
        }

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

        /// <summary>
        /// Property 4: Service.Update Round-Trip.
        /// Validates: Requirements 7.1
        /// </summary>
        [Test]
        public void Update_RoundTrip_PreservesAllFields()
        {
            Prop.ForAll(
                SafeStringGen().ToArbitrary(),
                SafeStringGen().ToArbitrary(),
                SafeStringGen().ToArbitrary(),
                (name1, name2, sys2) =>
            {
                var createReq = new AsteroidCreateRequest { Name = name1, SystemName = name1, Reserves = new List<AsteroidReserve>() };
                var created = _svc.Create(createReq);

                var updateReq = new AsteroidUpdateRequest
                {
                    Original = created,
                    Name = name2,
                    SystemName = sys2,
                    Reserves = new List<AsteroidReserve>(),
                };
                var updated = _svc.Update(created.UUID, updateReq);

                return (updated.Name == name2 && updated.SystemName == sys2 && updated.UUID == created.UUID).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 5: Service.Create Round-Trip.
        /// Validates: Requirements 7.2
        /// </summary>
        [Test]
        public void Create_RoundTrip_PreservesAllFields()
        {
            Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name, sys) =>
            {
                var req = new AsteroidCreateRequest { Name = name, SystemName = sys, Reserves = new List<AsteroidReserve>() };
                var created = _svc.Create(req);

                return (created.Name == name && created.SystemName == sys && !string.IsNullOrEmpty(created.UUID)).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 6: Service.Delete Removes Asteroid.
        /// Validates: Requirements 7.3
        /// </summary>
        [Test]
        public void Delete_RemovesAsteroid()
        {
            Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name, sys) =>
            {
                var req = new AsteroidCreateRequest { Name = name, SystemName = sys, Reserves = new List<AsteroidReserve>() };
                var created = _svc.Create(req);
                _svc.Delete(created.UUID);

                var found = _ctx.FindMutableAsteroid(created.UUID);
                return (found == null).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property: Service.Update preserves reserves.
        /// Validates: Requirements 7.1
        /// </summary>
        [Test]
        public void Update_PreservesReserves()
        {
            Prop.ForAll(
                SafeStringGen().ToArbitrary(),
                SafeStringGen().ToArbitrary(),
                ValidReserveGen().ToArbitrary(),
                (name, sys, reserve) =>
            {
                var createReq = new AsteroidCreateRequest { Name = name, SystemName = sys, Reserves = new List<AsteroidReserve>() };
                var created = _svc.Create(createReq);

                var reserves = new List<AsteroidReserve> { reserve };
                var updateReq = new AsteroidUpdateRequest
                {
                    Original = created,
                    Name = name,
                    SystemName = sys,
                    Reserves = reserves,
                };
                var updated = _svc.Update(created.UUID, updateReq);

                var roReserves = updated.Reserves;
                if (roReserves.Count != 1) return false.ToProperty();
                return (roReserves[0].ResourceName == reserve.ResourceName
                    && roReserves[0].Purity == reserve.Purity
                    && roReserves[0].MaxReserve == reserve.MaxReserve
                    && roReserves[0].CurrentReserve == reserve.CurrentReserve
                    && roReserves[0].ResetTimestamp == reserve.ResetTimestamp).ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}