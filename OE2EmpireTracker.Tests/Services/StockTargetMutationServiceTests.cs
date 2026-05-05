using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class StockTargetMutationServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
        }

        [Test]
        public void UpdatePlan_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            var req = new StockPlanUpdateRequest { Name = "X", IsActive = true, Targets = new List<StockTarget>() };
            Assert.Throws<InvalidOperationException>(() => svc.UpdatePlan("nonexistent", req));
        }

        [Test]
        public void CreatePlan_AssignsNonEmptyUUID()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            var req = new StockPlanCreateRequest { Name = "Test", IsActive = true, Targets = new List<StockTarget>() };
            var result = svc.CreatePlan(req);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void DeletePlan_EmptyUUID_ReturnsWithoutError()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            Assert.DoesNotThrow(() => svc.DeletePlan(string.Empty));
        }

        [Test]
        public void DeletePlan_NonExistentUUID_ReturnsWithoutError()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            Assert.DoesNotThrow(() => svc.DeletePlan("nonexistent"));
        }

        [Test]
        public void UpdateProfile_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            var req = new StockProfileUpdateRequest { Name = "X", IsActive = true, Entries = new List<StockProfileEntry>() };
            Assert.Throws<InvalidOperationException>(() => svc.UpdateProfile("nonexistent", req));
        }

        [Test]
        public void CreateProfile_AssignsNonEmptyUUID()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            var req = new StockProfileCreateRequest { Name = "Test", IsActive = true, Entries = new List<StockProfileEntry>() };
            var result = svc.CreateProfile(req);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void DeleteProfile_EmptyUUID_ReturnsWithoutError()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            Assert.DoesNotThrow(() => svc.DeleteProfile(string.Empty));
        }

        [Test]
        public void UpdatePlan_FiresStockDataChangedEvent()
        {
            var ctx = PlayerContext.GetInstance();
            ctx.CurrentPlayerUUID = "test-player";
            var svc = new StockTargetMutationService(ctx);
            var plan = new StockPlan { UUID = Guid.NewGuid().ToString(), OwnerUUID = "test-player", Name = "Seed" };
            ctx.AddStockPlan(plan);
            string firedUUID = null;
            ctx.StockDataChanged += (s, e) => firedUUID = e.StockPlanUUID;
            svc.UpdatePlan(plan.UUID, new StockPlanUpdateRequest { Name = "Updated", IsActive = true, Targets = new List<StockTarget>() });
            Assert.That(firedUUID, Is.EqualTo(plan.UUID));
        }
    }
}
