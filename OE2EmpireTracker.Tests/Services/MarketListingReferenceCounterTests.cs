using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class MarketListingReferenceCounterTests
    {
        [Test]
        public void CountReferences_EmptyTransactions_ReturnsZero()
        {
            var counter = new MarketListingReferenceCounter(Enumerable.Empty<MarketTransaction>());
            Assert.That(counter.CountReferences("listing-1"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new MarketListingReferenceCounter(Enumerable.Empty<MarketTransaction>());
            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_EmptyUUID_ReturnsZero()
        {
            var counter = new MarketListingReferenceCounter(Enumerable.Empty<MarketTransaction>());
            Assert.That(counter.CountReferences(""), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_SingleMatch_ReturnsOne()
        {
            var transactions = new List<MarketTransaction>
            {
                new MarketTransaction { UUID = "tx-1", ListingUUID = "listing-1" }
            };
            var counter = new MarketListingReferenceCounter(transactions);
            Assert.That(counter.CountReferences("listing-1"), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultipleMatches_CountsAll()
        {
            var transactions = new List<MarketTransaction>
            {
                new MarketTransaction { UUID = "tx-1", ListingUUID = "listing-1" },
                new MarketTransaction { UUID = "tx-2", ListingUUID = "listing-1" },
                new MarketTransaction { UUID = "tx-3", ListingUUID = "listing-2" }
            };
            var counter = new MarketListingReferenceCounter(transactions);
            Assert.That(counter.CountReferences("listing-1"), Is.EqualTo(2));
            Assert.That(counter.CountReferences("listing-2"), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_NoMatch_ReturnsZero()
        {
            var transactions = new List<MarketTransaction>
            {
                new MarketTransaction { UUID = "tx-1", ListingUUID = "listing-1" }
            };
            var counter = new MarketListingReferenceCounter(transactions);
            Assert.That(counter.CountReferences("listing-999"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullTransactions_HandledGracefully()
        {
            var counter = new MarketListingReferenceCounter(null);
            Assert.That(counter.CountReferences("listing-1"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_TransactionsWithEmptyListingUUID_NotCounted()
        {
            var transactions = new List<MarketTransaction>
            {
                new MarketTransaction { UUID = "tx-1", ListingUUID = "" },
                new MarketTransaction { UUID = "tx-2", ListingUUID = "listing-1" }
            };
            var counter = new MarketListingReferenceCounter(transactions);
            Assert.That(counter.CountReferences("listing-1"), Is.EqualTo(1));
        }
    }
}