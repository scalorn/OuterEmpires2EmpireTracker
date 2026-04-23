using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BuildPlanReferenceCounterTests
    {
        [Test]
        public void CountReferences_NoStockPlans_ReturnsZero()
        {
            var counter = new BuildPlanReferenceCounter(new List<StockPlan>());
            Assert.That(counter.CountReferences("plan-1"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = "plan-1" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_EmptyUUID_ReturnsZero()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = "plan-1" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences(string.Empty), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_OneMatch_ReturnsOne()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = "plan-1" },
                new StockPlan { UUID = "sp-2", ReplenishmentBuildPlanUUID = "plan-2" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences("plan-1"), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultipleMatches_ReturnsTotalCount()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = "plan-1" },
                new StockPlan { UUID = "sp-2", ReplenishmentBuildPlanUUID = "plan-1" },
                new StockPlan { UUID = "sp-3", ReplenishmentBuildPlanUUID = "plan-2" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences("plan-1"), Is.EqualTo(2));
        }

        [Test]
        public void CountReferences_NoMatch_ReturnsZero()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = "plan-1" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences("plan-999"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullStockPlans_ReturnsZero()
        {
            var counter = new BuildPlanReferenceCounter(null);
            Assert.That(counter.CountReferences("plan-1"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_StockPlanWithEmptyReplenishmentUUID_NotCounted()
        {
            var stockPlans = new List<StockPlan>
            {
                new StockPlan { UUID = "sp-1", ReplenishmentBuildPlanUUID = string.Empty },
                new StockPlan { UUID = "sp-2", ReplenishmentBuildPlanUUID = "plan-1" }
            };

            var counter = new BuildPlanReferenceCounter(stockPlans);
            Assert.That(counter.CountReferences("plan-1"), Is.EqualTo(1));
        }
    }
}
