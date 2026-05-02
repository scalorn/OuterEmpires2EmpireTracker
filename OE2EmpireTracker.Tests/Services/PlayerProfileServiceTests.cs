using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for PlayerProfileService edge cases and event firing.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Requirements 14.8, 14.10, 15.6, 16.3, 16.6, 17.3, 17.5
    /// </summary>
    [TestFixture]
    public class PlayerProfileServiceTests
    {
        private PlayerContext playerContext;
        private PlayerProfileService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            service = new PlayerProfileService(playerContext);
        }

        // -------------------------------------------------------------------
        // Requirement 14.10: Update throws InvalidOperationException on unknown UUID
        // -------------------------------------------------------------------

        [Test]
        public void Update_UnknownUUID_ThrowsInvalidOperationException()
        {
            var request = new PlayerProfileUpdateRequest { Name = "Test" };
            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        [Test]
        public void Update_NullUUID_ThrowsArgumentNullException()
        {
            var request = new PlayerProfileUpdateRequest { Name = "Test" };
            Assert.Throws<ArgumentNullException>(
                () => service.Update(null, request));
        }

        [Test]
        public void Update_EmptyUUID_ThrowsArgumentNullException()
        {
            var request = new PlayerProfileUpdateRequest { Name = "Test" };
            Assert.Throws<ArgumentNullException>(
                () => service.Update(string.Empty, request));
        }

        [Test]
        public void Update_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => service.Update("some-uuid", null));
        }

        // -------------------------------------------------------------------
        // Requirement 14.8: Update fires PlayerProfileDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresPlayerProfileDataChangedEvent()
        {
            var profile = new PlayerProfile { UUID = Guid.NewGuid().ToString(), Name = "EventTest" };
            playerContext.AddPlayerProfile(profile);

            string firedUuid = null;
            playerContext.PlayerProfileDataChanged += (s, e) => firedUuid = e.PlayerUUID;

            var request = new PlayerProfileUpdateRequest { Name = "Updated" };
            service.Update(profile.UUID, request);

            Assert.That(firedUuid, Is.EqualTo(profile.UUID));
        }

        // -------------------------------------------------------------------
        // Requirement 15.6: Create fires both events
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresPlayerProfilesChangedEvent()
        {
            bool profilesChangedFired = false;
            playerContext.PlayerProfilesChanged += (s, e) => profilesChangedFired = true;

            var request = new PlayerProfileCreateRequest { Name = "NewProfile" };
            service.Create(request);

            Assert.That(profilesChangedFired, Is.True);
        }

        [Test]
        public void Create_FiresPlayerProfileDataChangedEvent()
        {
            string firedUuid = null;
            playerContext.PlayerProfileDataChanged += (s, e) => firedUuid = e.PlayerUUID;

            var request = new PlayerProfileCreateRequest { Name = "NewProfile" };
            var result = service.Create(request);

            Assert.That(firedUuid, Is.EqualTo(result.UUID));
        }

        [Test]
        public void Create_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => service.Create(null));
        }

        // -------------------------------------------------------------------
        // Requirement 16.6: Delete is no-op on empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        [Test]
        public void Delete_NullUUID_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => service.Delete(null));
        }

        [Test]
        public void Delete_UnknownUUID_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 16.3: Delete calls CascadeDeletePlayer
        // -------------------------------------------------------------------

        [Test]
        public void Delete_RemovesOwnedColonies()
        {
            var profile = new PlayerProfile { UUID = Guid.NewGuid().ToString(), Name = "CascadeTest" };
            playerContext.AddPlayerProfile(profile);

            var colony = new Colony { UUID = Guid.NewGuid().ToString(), OwnerUUID = profile.UUID };
            playerContext.AddColony(colony);

            service.Delete(profile.UUID);

            // Profile should be gone
            Assert.That(playerContext.FindMutablePlayerProfile(profile.UUID), Is.Null);
        }

        [Test]
        public void Delete_FiresBothEvents()
        {
            var profile = new PlayerProfile { UUID = Guid.NewGuid().ToString(), Name = "EventTest" };
            playerContext.AddPlayerProfile(profile);

            bool profilesChangedFired = false;
            string dataChangedUuid = null;
            playerContext.PlayerProfilesChanged += (s, e) => profilesChangedFired = true;
            playerContext.PlayerProfileDataChanged += (s, e) => dataChangedUuid = e.PlayerUUID;

            service.Delete(profile.UUID);

            Assert.That(profilesChangedFired, Is.True);
            Assert.That(dataChangedUuid, Is.EqualTo(profile.UUID));
        }

        // -------------------------------------------------------------------
        // Requirement 17.3: Import creates new profile when no name match
        // -------------------------------------------------------------------

        [Test]
        public void Import_NoNameMatch_CreatesNewProfile()
        {
            int countBefore = playerContext.PlayerProfileList.Count;

            var tempProfile = new PlayerProfile
            {
                Name = "UniqueImportName_" + Guid.NewGuid().ToString("N"),
                Faction = "TestFaction",
            };

            var result = service.Import(tempProfile);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Name, Is.EqualTo(tempProfile.Name));
            Assert.That(playerContext.PlayerProfileList.Count, Is.EqualTo(countBefore + 1));
        }

        // -------------------------------------------------------------------
        // Requirement 17.5: Import fires both events
        // -------------------------------------------------------------------

        [Test]
        public void Import_FiresBothEvents()
        {
            bool profilesChangedFired = false;
            string dataChangedUuid = null;
            playerContext.PlayerProfilesChanged += (s, e) => profilesChangedFired = true;
            playerContext.PlayerProfileDataChanged += (s, e) => dataChangedUuid = e.PlayerUUID;

            var tempProfile = new PlayerProfile
            {
                Name = "ImportEventTest_" + Guid.NewGuid().ToString("N"),
            };

            var result = service.Import(tempProfile);

            Assert.That(profilesChangedFired, Is.True);
            Assert.That(dataChangedUuid, Is.EqualTo(result.UUID));
        }

        [Test]
        public void Import_NullProfile_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => service.Import(null));
        }

        // -------------------------------------------------------------------
        // Requirement 17.3: MergeProfile preserves UUID
        // -------------------------------------------------------------------

        [Test]
        public void Import_NameMatch_PreservesUUID()
        {
            var existing = new PlayerProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "MergeTarget",
                Faction = "OldFaction",
            };
            playerContext.AddPlayerProfile(existing);
            string originalUuid = existing.UUID;

            var tempProfile = new PlayerProfile
            {
                Name = "MergeTarget",
                Faction = "NewFaction",
                TotalCredits = 500m,
            };

            var result = service.Import(tempProfile);

            Assert.That(result.UUID, Is.EqualTo(originalUuid));
            Assert.That(result.Faction, Is.EqualTo("NewFaction"));
            Assert.That(result.TotalCredits, Is.EqualTo(500m));
        }

        [Test]
        public void Import_NameMatch_CaseInsensitive()
        {
            var existing = new PlayerProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "CaseTest",
            };
            playerContext.AddPlayerProfile(existing);

            var tempProfile = new PlayerProfile
            {
                Name = "CASETEST",
                Faction = "Imported",
            };

            var result = service.Import(tempProfile);

            Assert.That(result.UUID, Is.EqualTo(existing.UUID));
            Assert.That(result.Faction, Is.EqualTo("Imported"));
        }

        // -------------------------------------------------------------------
        // MergeProfile internal method
        // -------------------------------------------------------------------

        [Test]
        public void MergeProfile_PreservesUUID()
        {
            var existing = new PlayerProfile
            {
                UUID = "keep-this-uuid",
                Name = "Old",
                Faction = "OldFaction",
            };

            var parsed = new PlayerProfile
            {
                UUID = "discard-this-uuid",
                Name = "New",
                Faction = "NewFaction",
                TotalCredits = 100m,
                SkillPoints = 5,
            };

            PlayerProfileService.MergeProfile(existing, parsed);

            Assert.That(existing.UUID, Is.EqualTo("keep-this-uuid"));
            Assert.That(existing.Name, Is.EqualTo("New"));
            Assert.That(existing.Faction, Is.EqualTo("NewFaction"));
            Assert.That(existing.TotalCredits, Is.EqualTo(100m));
            Assert.That(existing.SkillPoints, Is.EqualTo(5));
        }
    }
}