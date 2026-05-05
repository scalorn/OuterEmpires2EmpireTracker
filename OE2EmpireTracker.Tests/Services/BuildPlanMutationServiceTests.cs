using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BuildPlanMutationService.
    /// Feature: bl-118-buildplan-readonly
    /// </summary>
    [TestFixture]
    public class BuildPlanMutationServiceTests
    {
        private PlayerContext playerContext;
        private BuildPlanMutationService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player";
            service = new BuildPlanMutationService(playerContext);
        }

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var req = new BuildPlanUpdateRequest { Name = "X", Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
            Assert.Throws<InvalidOperationException>(() => service.Update("nonexistent", req));
        }

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent"));
        }

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var req = new BuildPlanCreateRequest { Name = "Test", Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
            var result = service.Create(req);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var req = new BuildPlanCreateRequest { Name = "Test", Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
            var result = service.Create(req);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player"));
        }

        [Test]
        public void Update_FiresBuildPlanDataChangedEvent()
        {
            var plan = new BuildPlan { UUID = "p1", OwnerUUID = "test-player", Name = "P" };
            playerContext.AddBuildPlan(plan);
            bool fired = false;
            playerContext.BuildPlanDataChanged += (s, e) => fired = true;
            var req = new BuildPlanUpdateRequest { Name = "Updated", Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
            service.Update("p1", req);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Create_FiresBuildPlanDataChangedEvent()
        {
            bool fired = false;
            playerContext.BuildPlanDataChanged += (s, e) => fired = true;
            var req = new BuildPlanCreateRequest { Name = "New", Description = string.Empty, IsActive = true, Items = new List<BuildItem>() };
            service.Create(req);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Delete_FiresBuildPlanDataChangedEvent()
        {
            var plan = new BuildPlan { UUID = "p1", OwnerUUID = "test-player", Name = "P" };
            playerContext.AddBuildPlan(plan);
            bool fired = false;
            playerContext.BuildPlanDataChanged += (s, e) => fired = true;
            service.Delete("p1");
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Update_ReplacesItemsList()
        {
            var plan = new BuildPlan { UUID = "p1", OwnerUUID = "test-player", Name = "P" };
            plan.Items.Add(new BuildItem { UUID = "old", ItemName = "Old" });
            playerContext.AddBuildPlan(plan);
            var newItems = new List<BuildItem> { new BuildItem { UUID = "new1", ItemName = "New1" } };
            var req = new BuildPlanUpdateRequest { Name = "P", Description = string.Empty, IsActive = true, Items = newItems };
            var result = service.Update("p1", req);
            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items[0].ItemName, Is.EqualTo("New1"));
        }

        [Test]
        public void Create_PopulatesItemsFromRequest()
        {
            var items = new List<BuildItem> { new BuildItem { UUID = "i1", ItemName = "Item1" } };
            var req = new BuildPlanCreateRequest { Name = "P", Description = string.Empty, IsActive = true, Items = items };
            var result = service.Create(req);
            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items[0].ItemName, Is.EqualTo("Item1"));
        }
    }
}
