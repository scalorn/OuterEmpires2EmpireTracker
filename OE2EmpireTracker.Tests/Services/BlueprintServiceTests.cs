using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BlueprintService edge cases and event firing.
    /// Feature: BL-108 Blueprint Immutable Data Model
    /// Validates: Error handling, event firing, and move operations.
    /// </summary>
    [TestFixture]
    public class BlueprintServiceTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private BlueprintService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            empireContext = EmpireContext.GetInstance();
            service = new BlueprintService(playerContext, empireContext);
        }

        // -------------------------------------------------------------------
        // Update error handling
        // -------------------------------------------------------------------

        [Test]
        public void Update_UnknownUUID_ThrowsInvalidOperationException()
        {
            var request = new BlueprintUpdateRequest { Name = "Test" };
            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        [Test]
        public void Update_NullUUID_ThrowsArgumentNullException()
        {
            var request = new BlueprintUpdateRequest { Name = "Test" };
            Assert.Throws<ArgumentNullException>(
                () => service.Update(null, request));
        }

        [Test]
        public void Update_EmptyUUID_ThrowsArgumentNullException()
        {
            var request = new BlueprintUpdateRequest { Name = "Test" };
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
        // Update fires event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresBlueprintDataChangedEvent()
        {
            var bp = new Blueprint { UUID = Guid.NewGuid().ToString(), Name = "EventTest" };
            playerContext.AddBlueprint(bp);

            string firedUuid = null;
            playerContext.BlueprintDataChanged += (s, e) => firedUuid = e.BlueprintUUID;

            var request = new BlueprintUpdateRequest { Name = "Updated" };
            service.Update(bp.UUID, request);

            Assert.That(firedUuid, Is.EqualTo(bp.UUID));
        }

        // -------------------------------------------------------------------
        // Create error handling and events
        // -------------------------------------------------------------------

        [Test]
        public void Create_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => service.Create(null, false));
        }

        [Test]
        public void Create_FiresBlueprintDataChangedEvent()
        {
            string firedUuid = null;
            playerContext.BlueprintDataChanged += (s, e) => firedUuid = e.BlueprintUUID;

            var request = new BlueprintCreateRequest { Name = "NewBlueprint" };
            var result = service.Create(request, false);

            Assert.That(firedUuid, Is.EqualTo(result.UUID));
        }

        // -------------------------------------------------------------------
        // Delete error handling and events
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NullUUID_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => service.Delete(null));
        }

        [Test]
        public void Delete_EmptyUUID_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => service.Delete(string.Empty));
        }

        [Test]
        public void Delete_UnknownUUID_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        [Test]
        public void Delete_FiresBlueprintDataChangedEvent()
        {
            var bp = new Blueprint { UUID = Guid.NewGuid().ToString(), Name = "DeleteTest" };
            playerContext.AddBlueprint(bp);

            string firedUuid = null;
            playerContext.BlueprintDataChanged += (s, e) => firedUuid = e.BlueprintUUID;

            service.Delete(bp.UUID);

            Assert.That(firedUuid, Is.EqualTo(bp.UUID));
        }

        // -------------------------------------------------------------------
        // MoveToGlobal / MoveToPlayer
        // -------------------------------------------------------------------

        [Test]
        public void MoveToGlobal_MovesFromPlayerToGlobal()
        {
            var bp = new Blueprint { UUID = Guid.NewGuid().ToString(), Name = "MoveTest" };
            playerContext.AddBlueprint(bp);

            service.MoveToGlobal(bp.UUID);

            Assert.That(playerContext.FindMutableBlueprint(bp.UUID), Is.Null,
                "Blueprint should no longer be in player list");
            Assert.That(empireContext.FindMutableGlobalBlueprint(bp.UUID), Is.Not.Null,
                "Blueprint should be in global list");
        }

        [Test]
        public void MoveToPlayer_MovesFromGlobalToPlayer()
        {
            var bp = new Blueprint { UUID = Guid.NewGuid().ToString(), Name = "MoveTest" };
            empireContext.AddGlobalBlueprint(bp);

            service.MoveToPlayer(bp.UUID);

            Assert.That(empireContext.FindMutableGlobalBlueprint(bp.UUID), Is.Null,
                "Blueprint should no longer be in global list");
            Assert.That(playerContext.FindMutableBlueprint(bp.UUID), Is.Not.Null,
                "Blueprint should be in player list");
        }
    }
}