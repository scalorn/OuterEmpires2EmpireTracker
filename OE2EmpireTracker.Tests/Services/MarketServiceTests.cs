using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class MarketServiceTests
    {
        #region RecordSale

        private MarketListing CreateListing(int quantity, string itemName = "Laser Mk2")
        {
            return new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "player-1",
                StationUUID = "station-1",
                ItemType = ItemType.ItemTypeEnum.ShipPart,
                ItemReferenceID = "bp-laser",
                ItemName = itemName,
                Quantity = quantity,
                PricePerUnit = 100m,
                CurrentHP = 80,
                MaxHP = 100,
                MaxRepairPercent = 0.95m
            };
        }

        [Test]
        public void RecordSale_ValidQuantity_DecrementsListingAndReturnsTransaction()
        {
            var listing = CreateListing(10);
            var tx = MarketService.RecordSale(listing, 3, 150m, "BuyerJoe", "Federation", "station-1");

            Assert.That(tx, Is.Not.Null);
            Assert.That(listing.Quantity, Is.EqualTo(7));
            Assert.That(tx.TransactionType, Is.EqualTo(TransactionType.Sell));
            Assert.That(tx.Quantity, Is.EqualTo(3));
            Assert.That(tx.PricePerUnit, Is.EqualTo(150m));
            Assert.That(tx.TotalPrice, Is.EqualTo(450m));
            Assert.That(tx.Counterparty, Is.EqualTo("BuyerJoe"));
            Assert.That(tx.CounterpartyFaction, Is.EqualTo("Federation"));
            Assert.That(tx.ItemName, Is.EqualTo("Laser Mk2"));
            Assert.That(tx.StationUUID, Is.EqualTo("station-1"));
        }

        [Test]
        public void RecordSale_SnapshotsConditionFields()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, 1, 100m, "Buyer", "Fac", "s1");

            Assert.That(tx.CurrentHP, Is.EqualTo(80));
            Assert.That(tx.MaxHP, Is.EqualTo(100));
            Assert.That(tx.MaxRepairPercent, Is.EqualTo(0.95m));
        }

        [Test]
        public void RecordSale_SnapshotsListingUUID()
        {
            var listing = CreateListing(5);
            string listingUUID = listing.UUID;
            var tx = MarketService.RecordSale(listing, 1, 100m, "Buyer", "Fac", "s1");

            Assert.That(tx.ListingUUID, Is.EqualTo(listingUUID));
        }

        [Test]
        public void RecordSale_ExactQuantity_SetsListingToZero()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, 5, 200m, "Buyer", "Fac", "s1");

            Assert.That(tx, Is.Not.Null);
            Assert.That(listing.Quantity, Is.EqualTo(0));
            Assert.That(tx.TotalPrice, Is.EqualTo(1000m));
        }

        [Test]
        public void RecordSale_QuantityExceedsListing_ReturnsNull()
        {
            var listing = CreateListing(3);
            var tx = MarketService.RecordSale(listing, 5, 100m, "Buyer", "Fac", "s1");

            Assert.That(tx, Is.Null);
            Assert.That(listing.Quantity, Is.EqualTo(3), "Listing should not be modified");
        }

        [Test]
        public void RecordSale_ZeroQuantity_ReturnsNull()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, 0, 100m, "Buyer", "Fac", "s1");

            Assert.That(tx, Is.Null);
            Assert.That(listing.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void RecordSale_NegativeQuantity_ReturnsNull()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, -1, 100m, "Buyer", "Fac", "s1");

            Assert.That(tx, Is.Null);
        }

        [Test]
        public void RecordSale_NullListing_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MarketService.RecordSale(null, 1, 100m, "Buyer", "Fac", "s1"));
        }

        [Test]
        public void RecordSale_NullCounterparty_DefaultsToEmpty()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, 1, 100m, null, null, null);

            Assert.That(tx.Counterparty, Is.EqualTo(string.Empty));
            Assert.That(tx.CounterpartyFaction, Is.EqualTo(string.Empty));
            Assert.That(tx.StationUUID, Is.EqualTo(string.Empty));
        }

        [Test]
        public void RecordSale_SetsOwnerUUIDFromListing()
        {
            var listing = CreateListing(5);
            listing.OwnerUUID = "owner-abc";
            var tx = MarketService.RecordSale(listing, 1, 100m, "B", "F", "s1");

            Assert.That(tx.OwnerUUID, Is.EqualTo("owner-abc"));
        }

        [Test]
        public void RecordSale_SetsTimestamp()
        {
            var listing = CreateListing(5);
            var tx = MarketService.RecordSale(listing, 1, 100m, "B", "F", "s1");

            Assert.That(tx.Timestamp, Is.Not.Empty);
            Assert.That(DateTime.TryParse(tx.Timestamp, out _), Is.True);
        }

        #endregion

        #region RecordPurchase

        [Test]
        public void RecordPurchase_CreatesTransactionAndAddsToHold()
        {
            var station = new Station
            {
                UUID = "station-1",
                Name = "Alpha Station"
            };

            var tx = MarketService.RecordPurchase(
                ItemType.ItemTypeEnum.ShipPart,
                "Shield Gen",
                "bp-shield",
                2,
                500m,
                "station-1",
                "SellerBob",
                "Empire",
                "player-1",
                uuid => uuid == "station-1" ? station : null);

            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.TransactionType, Is.EqualTo(TransactionType.Buy));
            Assert.That(tx.Quantity, Is.EqualTo(2));
            Assert.That(tx.PricePerUnit, Is.EqualTo(500m));
            Assert.That(tx.TotalPrice, Is.EqualTo(1000m));
            Assert.That(tx.Counterparty, Is.EqualTo("SellerBob"));
            Assert.That(tx.CounterpartyFaction, Is.EqualTo("Empire"));
            Assert.That(tx.ItemName, Is.EqualTo("Shield Gen"));

            Assert.That(station.Holds.ContainsKey("player-1"), Is.True);
            Assert.That(station.Holds["player-1"].Count(), Is.EqualTo(2));
        }

        [Test]
        public void RecordPurchase_ExistingHold_AddsToIt()
        {
            var station = new Station { UUID = "station-1" };
            var existingHold = new ItemBag();
            existingHold.AddItem(new Item(ItemType.ItemTypeEnum.Resource, "Iron") { UUID = "existing-1", Quantity = 1 });
            station.Holds["player-1"] = existingHold;

            var tx = MarketService.RecordPurchase(
                ItemType.ItemTypeEnum.Commodity,
                "Electronics",
                "comm-elec",
                1,
                200m,
                "station-1",
                "Seller",
                "Fac",
                "player-1",
                uuid => station);

            Assert.That(station.Holds["player-1"].Count(), Is.EqualTo(2));
        }

        [Test]
        public void RecordPurchase_StationNotFound_StillReturnsTransaction()
        {
            var tx = MarketService.RecordPurchase(
                ItemType.ItemTypeEnum.ShipPart,
                "Laser",
                "bp-laser",
                1,
                100m,
                "missing-station",
                "Seller",
                "Fac",
                "player-1",
                uuid => null);

            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.TransactionType, Is.EqualTo(TransactionType.Buy));
        }

        [Test]
        public void RecordPurchase_NullStationFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MarketService.RecordPurchase(
                    ItemType.ItemTypeEnum.ShipPart, "X", "ref", 1, 10m,
                    "s1", "C", "F", "p1", null));
        }

        [Test]
        public void RecordPurchase_NullOwnerUUID_SkipsHoldUpdate()
        {
            var station = new Station { UUID = "station-1" };

            var tx = MarketService.RecordPurchase(
                ItemType.ItemTypeEnum.Commodity,
                "Fuel",
                "comm-fuel",
                1,
                50m,
                "station-1",
                "Seller",
                "Fac",
                null,
                uuid => station);

            Assert.That(tx, Is.Not.Null);
            Assert.That(station.Holds.Count, Is.EqualTo(0));
        }

        [Test]
        public void RecordPurchase_SetsTimestamp()
        {
            var tx = MarketService.RecordPurchase(
                ItemType.ItemTypeEnum.Resource, "Iron", "res-iron", 1, 10m,
                "s1", "C", "F", "p1", uuid => null);

            Assert.That(tx.Timestamp, Is.Not.Empty);
            Assert.That(DateTime.TryParse(tx.Timestamp, out _), Is.True);
        }

        #endregion

        #region ComputeProfitLoss

        private MarketTransaction CreateTx(TransactionType type, string itemName, int qty, decimal pricePerUnit, string stationUUID = "s1", string timestamp = null)
        {
            return new MarketTransaction
            {
                UUID = Guid.NewGuid().ToString(),
                TransactionType = type,
                ItemName = itemName,
                Quantity = qty,
                PricePerUnit = pricePerUnit,
                TotalPrice = pricePerUnit * qty,
                StationUUID = stationUUID,
                Timestamp = timestamp ?? DateTime.UtcNow.ToString("o")
            };
        }

        [Test]
        public void ComputeProfitLoss_MixedTransactions_ComputesCorrectTotals()
        {
            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Sell, "Laser", 10, 100m),
                CreateTx(TransactionType.Buy, "Laser", 5, 80m),
                CreateTx(TransactionType.Sell, "Shield", 3, 200m)
            };

            var summary = MarketService.ComputeProfitLoss(transactions);

            Assert.That(summary.TotalSalesRevenue, Is.EqualTo(1600m));
            Assert.That(summary.TotalPurchaseCost, Is.EqualTo(400m));
            Assert.That(summary.NetProfitLoss, Is.EqualTo(1200m));
        }

        [Test]
        public void ComputeProfitLoss_PerItemBreakdown()
        {
            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Sell, "Laser", 10, 100m),
                CreateTx(TransactionType.Buy, "Laser", 5, 80m),
                CreateTx(TransactionType.Sell, "Shield", 3, 200m)
            };

            var summary = MarketService.ComputeProfitLoss(transactions);

            Assert.That(summary.ItemBreakdown.ContainsKey("Laser"), Is.True);
            Assert.That(summary.ItemBreakdown.ContainsKey("Shield"), Is.True);

            var laser = summary.ItemBreakdown["Laser"];
            Assert.That(laser.SalesRevenue, Is.EqualTo(1000m));
            Assert.That(laser.PurchaseCost, Is.EqualTo(400m));
            Assert.That(laser.NetProfitLoss, Is.EqualTo(600m));
            Assert.That(laser.QuantitySold, Is.EqualTo(10));
            Assert.That(laser.QuantityBought, Is.EqualTo(5));

            var shield = summary.ItemBreakdown["Shield"];
            Assert.That(shield.SalesRevenue, Is.EqualTo(600m));
            Assert.That(shield.PurchaseCost, Is.EqualTo(0m));
            Assert.That(shield.NetProfitLoss, Is.EqualTo(600m));
        }

        [Test]
        public void ComputeProfitLoss_EmptyList_ReturnsZeroSummary()
        {
            var summary = MarketService.ComputeProfitLoss(new List<MarketTransaction>());

            Assert.That(summary.TotalSalesRevenue, Is.EqualTo(0m));
            Assert.That(summary.TotalPurchaseCost, Is.EqualTo(0m));
            Assert.That(summary.NetProfitLoss, Is.EqualTo(0m));
            Assert.That(summary.ItemBreakdown.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComputeProfitLoss_NullTransactions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MarketService.ComputeProfitLoss(null));
        }

        [Test]
        public void ComputeProfitLoss_FilterByItemName()
        {
            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Sell, "Laser", 10, 100m),
                CreateTx(TransactionType.Sell, "Shield", 5, 200m)
            };

            var summary = MarketService.ComputeProfitLoss(transactions, itemNameFilter: "Laser");

            Assert.That(summary.TotalSalesRevenue, Is.EqualTo(1000m));
            Assert.That(summary.ItemBreakdown.Count, Is.EqualTo(1));
            Assert.That(summary.ItemBreakdown.ContainsKey("Laser"), Is.True);
        }

        [Test]
        public void ComputeProfitLoss_FilterByStation()
        {
            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Sell, "Laser", 10, 100m, "station-A"),
                CreateTx(TransactionType.Sell, "Laser", 5, 100m, "station-B")
            };

            var summary = MarketService.ComputeProfitLoss(transactions, stationUUIDFilter: "station-A");

            Assert.That(summary.TotalSalesRevenue, Is.EqualTo(1000m));
        }

        [Test]
        public void ComputeProfitLoss_FilterByDateRange()
        {
            var early = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc).ToString("o");
            var mid = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc).ToString("o");
            var late = new DateTime(2024, 12, 31, 12, 0, 0, DateTimeKind.Utc).ToString("o");

            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Sell, "Laser", 1, 100m, "s1", early),
                CreateTx(TransactionType.Sell, "Laser", 2, 100m, "s1", mid),
                CreateTx(TransactionType.Sell, "Laser", 3, 100m, "s1", late)
            };

            var summary = MarketService.ComputeProfitLoss(
                transactions,
                startDate: new DateTime(2024, 3, 1),
                endDate: new DateTime(2024, 9, 1));

            Assert.That(summary.TotalSalesRevenue, Is.EqualTo(200m));
        }

        [Test]
        public void ComputeProfitLoss_NetLoss_NegativeValue()
        {
            var transactions = new List<MarketTransaction>
            {
                CreateTx(TransactionType.Buy, "Laser", 10, 100m),
                CreateTx(TransactionType.Sell, "Laser", 2, 50m)
            };

            var summary = MarketService.ComputeProfitLoss(transactions);

            Assert.That(summary.NetProfitLoss, Is.EqualTo(-900m));
        }

        #endregion
    }
}
