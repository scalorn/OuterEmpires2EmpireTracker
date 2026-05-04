using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for MarketListingService edge cases and event firing.
    /// Feature: bl-114-market-readonly
    /// Validates: Requirements 4.2, 4.3, 4.8, 5.3, 5.6, 5.8, 6.2, 6.5, 6.6, 7.2, 7.3, 7.4, 7.7, 7.8, 7.9
    /// </summary>
    [TestFixture]
    public class MarketListingServiceTests
    {
        private PlayerContext playerContext;
        private MarketListingService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new MarketListingService(playerContext);
        }

        [Test]
        public void CreateListing_AssignsNonEmptyUUID()
        {
            var request = new MarketListingCreateRequest { ItemName = "Test", Quantity = 1 };
            var result = service.CreateListing(request);
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CreateListing_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new MarketListingCreateRequest { ItemName = "Test", Quantity = 1 };
            var result = service.CreateListing(request);
            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        [Test]
        public void CreateListing_FiresMarketDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.MarketDataChanged += (s, e) => eventFired = true;
            var request = new MarketListingCreateRequest { ItemName = "Test", Quantity = 1 };
            service.CreateListing(request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void UpdateListing_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new MarketListingUpdateRequest { ItemName = "Test" };
            Assert.Throws<InvalidOperationException>(
                () => service.UpdateListing("nonexistent-uuid", request));
        }

        [Test]
        public void UpdateListing_FiresMarketDataChangedEvent()
        {
            var created = service.CreateListing(new MarketListingCreateRequest { ItemName = "Seed", Quantity = 1 });
            bool eventFired = false;
            playerContext.MarketDataChanged += (s, e) => eventFired = true;
            var request = new MarketListingUpdateRequest { ItemName = "Updated" };
            service.UpdateListing(created.UUID, request);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void DeleteListing_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.DeleteListing(string.Empty));
        }

        [Test]
        public void DeleteListing_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.DeleteListing("nonexistent-uuid"));
        }

        [Test]
        public void DeleteListing_FiresMarketDataChangedEvent()
        {
            var created = service.CreateListing(new MarketListingCreateRequest { ItemName = "ToDelete", Quantity = 1 });
            bool eventFired = false;
            playerContext.MarketDataChanged += (s, e) => eventFired = true;
            service.DeleteListing(created.UUID);
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void RecordSale_NonExistentListingUUID_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(
                () => service.RecordSale("nonexistent-uuid", 1, 10m, "Buyer", "Faction", "station"));
        }

        [Test]
        public void RecordSale_QuantityExceedingAvailability_ReturnsNull()
        {
            var created = service.CreateListing(new MarketListingCreateRequest
            {
                ItemName = "Limited",
                Quantity = 5,
                PricePerUnit = 10m,
                StationUUID = "station-1",
            });

            var tx = service.RecordSale(created.UUID, 10, 10m, "Buyer", "Faction", "station-1");
            Assert.That(tx, Is.Null);
        }

        [Test]
        public void RecordSale_ValidInputs_ReturnsTransactionWithCorrectFields()
        {
            var created = service.CreateListing(new MarketListingCreateRequest
            {
                ItemName = "Widget",
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Quantity = 20,
                PricePerUnit = 5m,
                StationUUID = "station-1",
            });

            var tx = service.RecordSale(created.UUID, 3, 7.5m, "Alice", "Rebels", "station-1");

            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.ItemName, Is.EqualTo("Widget"));
            Assert.That(tx.Quantity, Is.EqualTo(3));
            Assert.That(tx.PricePerUnit, Is.EqualTo(7.5m));
            Assert.That(tx.TotalPrice, Is.EqualTo(22.5m));
            Assert.That(tx.TransactionType, Is.EqualTo(TransactionType.Sell));
            Assert.That(tx.Counterparty, Is.EqualTo("Alice"));
            Assert.That(tx.CounterpartyFaction, Is.EqualTo("Rebels"));
        }

        [Test]
        public void RecordSale_FiresMarketDataChangedEvent()
        {
            var created = service.CreateListing(new MarketListingCreateRequest
            {
                ItemName = "EventTest",
                Quantity = 10,
                PricePerUnit = 5m,
                StationUUID = "station-1",
            });

            bool eventFired = false;
            playerContext.MarketDataChanged += (s, e) => eventFired = true;
            service.RecordSale(created.UUID, 2, 5m, "Buyer", "Faction", "station-1");
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void RecordSale_AddsTransactionToPlayerContext()
        {
            var created = service.CreateListing(new MarketListingCreateRequest
            {
                ItemName = "TxTest",
                Quantity = 10,
                PricePerUnit = 5m,
                StationUUID = "station-1",
            });

            int beforeCount = playerContext.GetCurrentPlayerTransactions().Count;
            service.RecordSale(created.UUID, 2, 5m, "Buyer", "Faction", "station-1");
            int afterCount = playerContext.GetCurrentPlayerTransactions().Count;
            Assert.That(afterCount, Is.EqualTo(beforeCount + 1));
        }
    }
}
