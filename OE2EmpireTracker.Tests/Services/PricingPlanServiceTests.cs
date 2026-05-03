using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for PricingPlanService edge cases and event firing.
    /// Feature: bl-123-pricingplan-readonly
    /// Validates: Requirements 12.6, 12.8, 13.2, 13.3, 13.7, 14.4, 14.5
    /// </summary>
    [TestFixture]
    public class PricingPlanServiceTests
    {
        private PlayerContext playerContext;
        private PricingPlanService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new PricingPlanService(playerContext);
        }

        // -------------------------------------------------------------------
        // Requirement 12.8: Update throws InvalidOperationException on unknown UUID
        // -------------------------------------------------------------------

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new PricingPlanUpdateRequest
            {
                Name = "Test",
                Description = string.Empty,
                ResourcePrices = new Dictionary<string, decimal>(),
            };

            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        // -------------------------------------------------------------------
        // Requirement 14.5: Delete with empty UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        // -------------------------------------------------------------------
        // Requirement 14.5: Delete with non-existent UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 13.2: Create assigns non-empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var request = new PricingPlanCreateRequest
            {
                Name = "NewPlan",
                Description = string.Empty,
                ResourcePrices = new Dictionary<string, decimal>(),
            };

            var result = service.Create(request);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Requirement 13.3: Create sets OwnerUUID to current player UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new PricingPlanCreateRequest
            {
                Name = "OwnerTest",
                Description = string.Empty,
                ResourcePrices = new Dictionary<string, decimal>(),
            };

            var result = service.Create(request);

            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 12.6: Update fires PricingDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresPricingDataChangedEvent()
        {
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "EventTest",
            };
            playerContext.AddPricingPlan(plan);

            bool eventFired = false;
            playerContext.PricingDataChanged += (s, e) => eventFired = true;

            var request = new PricingPlanUpdateRequest
            {
                Name = "Updated",
                Description = string.Empty,
                ResourcePrices = new Dictionary<string, decimal>(),
            };
            service.Update(plan.UUID, request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 13.7: Create fires PricingDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresPricingDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.PricingDataChanged += (s, e) => eventFired = true;

            var request = new PricingPlanCreateRequest
            {
                Name = "NewPlan",
                Description = string.Empty,
                ResourcePrices = new Dictionary<string, decimal>(),
            };
            service.Create(request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 14.4: Delete fires PricingDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Delete_FiresPricingDataChangedEvent()
        {
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                Name = "DeleteEventTest",
            };
            playerContext.AddPricingPlan(plan);

            bool eventFired = false;
            playerContext.PricingDataChanged += (s, e) => eventFired = true;

            service.Delete(plan.UUID);

            Assert.That(eventFired, Is.True);
        }
    }
}