using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Completeness tests for StockPlanReferenceCounter.
    /// Verifies that every source listed in spec/design/reference-counting.md
    /// is actually counted by the counter.
    /// Sources: StockProfileEntry.StockPlanUUID
    /// </summary>
    [TestFixture]
    public class StockPlanReferenceCounterCompletenessTests
    {
        private const string TargetUUID = "stockplan-completeness-uuid";

        [Test]
        public void CountReferences_StockProfileEntryStockPlanUUID_Counted()
        {
            var profile = new StockProfile
            {
                UUID = "profile-1",
                Entries = new List<StockProfileEntry>
                {
                    new StockProfileEntry { StockPlanUUID = TargetUUID }
                }
            };

            var counter = new StockPlanReferenceCounter(new[] { profile });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_EmptyData_ReturnsZero()
        {
            var counter = new StockPlanReferenceCounter(
                Enumerable.Empty<StockProfile>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new StockPlanReferenceCounter(
                Enumerable.Empty<StockProfile>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullProfiles_HandledGracefully()
        {
            var counter = new StockPlanReferenceCounter(null);

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_MultipleEntriesMatch_CountsAll()
        {
            var profile = new StockProfile
            {
                UUID = "profile-1",
                Entries = new List<StockProfileEntry>
                {
                    new StockProfileEntry { StockPlanUUID = TargetUUID },
                    new StockProfileEntry { StockPlanUUID = TargetUUID },
                    new StockProfileEntry { StockPlanUUID = "other-uuid" }
                }
            };

            var counter = new StockPlanReferenceCounter(new[] { profile });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(2));
        }
    }
}
