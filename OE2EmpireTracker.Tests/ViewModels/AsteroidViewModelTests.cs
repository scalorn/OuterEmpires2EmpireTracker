using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    [TestFixture]
    public class AsteroidViewModelTests
    {
        [Test]
        public void IsNew_TrueAfterReset()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            vm.Reset();
            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            Assert.That(vm.IsNew, Is.False);
        }

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var vm = new AsteroidViewModel();
            var ro = new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" });
            vm.LoadFrom(ro);
            vm.Name = "NewRock";
            vm.SystemName = "Alpha";
            var req = vm.BuildUpdateRequest();
            Assert.That(req.Original, Is.EqualTo(ro));
            Assert.That(req.Name, Is.EqualTo("NewRock"));
            Assert.That(req.SystemName, Is.EqualTo("Alpha"));
        }

        [Test]
        public void BuildCreateRequest_CopiesAllFields()
        {
            var vm = new AsteroidViewModel();
            vm.Name = "NewAsteroid";
            vm.SystemName = "Beta";
            var req = vm.BuildCreateRequest();
            Assert.That(req.Name, Is.EqualTo("NewAsteroid"));
            Assert.That(req.SystemName, Is.EqualTo("Beta"));
        }

        [Test]
        public void Reset_ClearsAllFields()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            vm.Reset();
            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.SystemName, Is.EqualTo(string.Empty));
            Assert.That(vm.Original, Is.Null);
            Assert.That(vm.Reserves, Is.Empty);
        }

        [Test]
        public void AddReserve_IncreasesCount()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            vm.AddReserve(new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" });
            Assert.That(vm.Reserves.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveReserve_DecreasesCount()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid
            {
                UUID = "a1",
                Name = "Rock",
                SystemName = "Sol",
                Reserves = new List<AsteroidReserve>
                {
                    new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" },
                },
            }));
            vm.RemoveReserve(0);
            Assert.That(vm.Reserves.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveReserve_InvalidIndex_NoOp()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            Assert.DoesNotThrow(() => vm.RemoveReserve(-1));
            Assert.DoesNotThrow(() => vm.RemoveReserve(99));
        }

        [Test]
        public void UpdateReserve_ReplacesAtIndex()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid
            {
                UUID = "a1",
                Name = "Rock",
                SystemName = "Sol",
                Reserves = new List<AsteroidReserve>
                {
                    new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" },
                },
            }));
            var replacement = new AsteroidReserve { ResourceName = "Gold", Purity = "Low", MaxReserve = 200, CurrentReserve = 10, ResetTimestamp = "2025-01-01" };
            vm.UpdateReserve(0, replacement);
            Assert.That(vm.Reserves[0].ResourceName, Is.EqualTo("Gold"));
        }

        [Test]
        public void UpdateReserve_InvalidIndex_NoOp()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            var replacement = new AsteroidReserve { ResourceName = "Gold", Purity = "Low", MaxReserve = 200, CurrentReserve = 10, ResetTimestamp = "2025-01-01" };
            Assert.DoesNotThrow(() => vm.UpdateReserve(-1, replacement));
            Assert.DoesNotThrow(() => vm.UpdateReserve(99, replacement));
        }

        [Test]
        public void IsDirty_DetectsReserveCountChange()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            Assert.That(vm.IsDirty, Is.False);
            vm.AddReserve(new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" });
            Assert.That(vm.IsDirty, Is.True);
        }

        [Test]
        public void BuildUpdateRequest_DeepCopiesReserves()
        {
            var vm = new AsteroidViewModel();
            vm.LoadFrom(new ReadOnlyAsteroid(new Asteroid { UUID = "a1", Name = "Rock", SystemName = "Sol" }));
            vm.AddReserve(new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" });
            var req = vm.BuildUpdateRequest();
            req.Reserves[0].ResourceName = "Modified";
            Assert.That(vm.Reserves[0].ResourceName, Is.EqualTo("Iron"));
        }
    }
}
