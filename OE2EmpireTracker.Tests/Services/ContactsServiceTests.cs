using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ContactsServiceTests
    {
        private PlayerContext _ctx;
        private ContactsService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.SuppressUI = true;
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());
            _svc = new ContactsService(_ctx);
        }

        [Test]
        public void CreateFaction_AssignsNonEmptyUUID()
        {
            var created = _svc.CreateFaction(new FactionCreateRequest { Name = "Test", Description = "Desc" });
            Assert.That(created.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CreateFaction_FiresContactDataChangedEvent()
        {
            string firedUUID = null;
            _ctx.ContactDataChanged += (s, e) => firedUUID = e.CharacterUUID;
            var created = _svc.CreateFaction(new FactionCreateRequest { Name = "Test", Description = "Desc" });
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }

        [Test]
        public void UpdateFaction_NonExistentUUID_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _svc.UpdateFaction("nonexistent", new FactionUpdateRequest { Name = "X", Description = "Y" }));
        }

        [Test]
        public void DeleteFaction_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => _svc.DeleteFaction(string.Empty));
        }

        [Test]
        public void DeleteFaction_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => _svc.DeleteFaction("nonexistent"));
        }

        [Test]
        public void CreateCharacter_AssignsNonEmptyUUID()
        {
            var created = _svc.CreateCharacter(new ExternalCharacterCreateRequest { Name = "Char", FactionUUID = string.Empty });
            Assert.That(created.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CreateCharacter_FiresContactDataChangedEvent()
        {
            string firedUUID = null;
            _ctx.ContactDataChanged += (s, e) => firedUUID = e.CharacterUUID;
            var created = _svc.CreateCharacter(new ExternalCharacterCreateRequest { Name = "Char", FactionUUID = string.Empty });
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }

        [Test]
        public void UpdateCharacter_NonExistentUUID_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _svc.UpdateCharacter("nonexistent", new ExternalCharacterUpdateRequest { Name = "X", FactionUUID = "f1" }));
        }

        [Test]
        public void DeleteCharacter_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => _svc.DeleteCharacter(string.Empty));
        }

        [Test]
        public void DeleteCharacter_FiresContactDataChangedEvent()
        {
            var created = _svc.CreateCharacter(new ExternalCharacterCreateRequest { Name = "Char", FactionUUID = string.Empty });
            string firedUUID = null;
            _ctx.ContactDataChanged += (s, e) => firedUUID = e.CharacterUUID;
            _svc.DeleteCharacter(created.UUID);
            Assert.That(firedUUID, Is.EqualTo(created.UUID));
        }
    }
}
