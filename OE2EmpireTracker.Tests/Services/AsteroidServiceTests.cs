using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class AsteroidServiceTests
    {
        private PlayerContext _ctx;
        private AsteroidService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());
            _svc = new AsteroidService(_ctx);
        }

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            Assert.That(created.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_FiresAsteroidDataChangedEvent()
        {
            string firedUUID = null;
            _ctx.AsteroidDataChanged += (s, e) => firedUUID = e.AsteroidUUID;
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }

        [Test]
        public void Create_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _svc.Create(null));
        }

        [Test]
        public void Create_BlankName_DefaultsToNewAsteroid()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "  ", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            Assert.That(created.Name, Is.EqualTo("New Asteroid"));
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _svc.Update("nonexistent", new AsteroidUpdateRequest { Name = "X", SystemName = "Y", Reserves = new List<AsteroidReserve>() }));
        }

        [Test]
        public void Update_NullUUID_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _svc.Update(null, new AsteroidUpdateRequest { Name = "X", SystemName = "Y", Reserves = new List<AsteroidReserve>() }));
        }

        [Test]
        public void Update_NullRequest_ThrowsArgumentNullException()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            Assert.Throws<ArgumentNullException>(() => _svc.Update(created.UUID, null));
        }

        [Test]
        public void Update_FiresAsteroidDataChangedEvent()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            string firedUUID = null;
            _ctx.AsteroidDataChanged += (s, e) => firedUUID = e.AsteroidUUID;
            _svc.Update(created.UUID, new AsteroidUpdateRequest { Name = "Updated", SystemName = "Alpha", Reserves = new List<AsteroidReserve>() });
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => _svc.Delete(string.Empty));
        }

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => _svc.Delete("nonexistent"));
        }

        [Test]
        public void Delete_FiresAsteroidDataChangedEvent()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            string firedUUID = null;
            _ctx.AsteroidDataChanged += (s, e) => firedUUID = e.AsteroidUUID;
            _svc.Delete(created.UUID);
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }

        [Test]
        public void Update_DeepCopiesReserves()
        {
            var created = _svc.Create(new AsteroidCreateRequest { Name = "Test", SystemName = "Sol", Reserves = new List<AsteroidReserve>() });
            var reserves = new List<AsteroidReserve>
            {
                new AsteroidReserve { ResourceName = "Iron", Purity = "High", MaxReserve = 100, CurrentReserve = 50, ResetTimestamp = "2024-01-01" },
            };
            var updated = _svc.Update(created.UUID, new AsteroidUpdateRequest { Name = "Test", SystemName = "Sol", Reserves = reserves });
            reserves[0].ResourceName = "Modified";
            Assert.That(updated.Reserves[0].ResourceName, Is.EqualTo("Iron"));
        }
    }
}
