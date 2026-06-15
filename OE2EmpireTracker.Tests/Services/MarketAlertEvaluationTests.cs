using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for market alert evaluation correctness.
    /// Feature: market-integration
    /// **Validates: Design Alert Evaluation**
    /// </summary>
    [TestFixture]
    public class MarketAlertEvaluationTests
    {
        private PlayerContext _ctx;
        private SystemRepository _repo;
        private SystemGridIndex _gridIndex;
        private MarketDataService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());

            _repo = new SystemRepository();
            _repo.ReplaceAll(new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
            });

            _gridIndex = new SystemGridIndex(_repo, 50);
            _svc = new MarketDataService(_ctx, _gridIndex);
        }

        private static MarketListing CreateSellOrder(
            long marketId,
            decimal price,
            string itemType = "Resource",
            string baseItemTypeId = "Noble Gases",
            string purity = "High",
            string stationUuid = "station-1")
        {
            return new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = marketId,
                BuyOrder = false,
                ItemType = ParseItemType(itemType),
                BaseItemTypeID = baseItemTypeId,
                ResourcePurity = purity,
                PricePerUnit = price,
                Quantity = 100,
                StationUUID = stationUuid,
                ItemName = baseItemTypeId + " (" + purity + ")",
            };
        }

        private static MarketListing CreateBuyOrder(
            long marketId,
            decimal price,
            string baseItemTypeId = "Noble Gases",
            string purity = "High",
            string stationUuid = "station-1")
        {
            return new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                MarketId = marketId,
                BuyOrder = true,
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = baseItemTypeId,
                ResourcePurity = purity,
                PricePerUnit = price,
                Quantity = 50,
                StationUUID = stationUuid,
                ItemName = baseItemTypeId + " (" + purity + ")",
            };
        }

        private static MarketAlert CreateAlert(
            MarketAlertType alertType,
            string baseItemTypeId = "Noble Gases",
            string purity = "High",
            PriceCondition priceCondition = PriceCondition.AnyPrice,
            decimal? threshold = null,
            string stationUuid = "")
        {
            return new MarketAlert
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Alert",
                Enabled = true,
                AlertType = alertType,
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = baseItemTypeId,
                ResourcePurity = purity,
                PriceCondition = priceCondition,
                PriceThreshold = threshold,
                StationUUID = stationUuid,
            };
        }

        private static ItemType.ItemTypeEnum ParseItemType(string s)
        {
            switch (s)
            {
                case "Resource": return ItemType.ItemTypeEnum.Resource;
                case "Commodity": return ItemType.ItemTypeEnum.Commodity;
                case "Blueprint": return ItemType.ItemTypeEnum.Blueprint;
                default: return ItemType.ItemTypeEnum.None;
            }
        }

        // -------------------------------------------------------------------
        // Test: Alert fires for matching sell orders
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_SellOrderAppears_FiresForMatchingSellOrder()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(1001, 10.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Not.Null);
            Assert.That(firedArgs.Alert.UUID, Is.EqualTo(alert.UUID));
            Assert.That(firedArgs.MatchingOrders.Count, Is.EqualTo(1));
            Assert.That(firedArgs.MatchingOrders[0].MarketId, Is.EqualTo(1001));
        }

        // -------------------------------------------------------------------
        // Test: Alert fires for matching buy orders
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_BuyOrderAppears_FiresForMatchingBuyOrder()
        {
            var alert = CreateAlert(MarketAlertType.BuyOrderAppears);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateBuyOrder(2001, 15.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Not.Null);
            Assert.That(firedArgs.MatchingOrders[0].MarketId, Is.EqualTo(2001));
        }

        // -------------------------------------------------------------------
        // Test: SellOrderAppears does not fire for buy orders
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_SellOrderAppears_DoesNotFireForBuyOrder()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateBuyOrder(3001, 20.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }

        // -------------------------------------------------------------------
        // Test: Respects AtOrBelow price condition
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_AtOrBelow_FiresWhenPriceBelowThreshold()
        {
            var alert = CreateAlert(
                MarketAlertType.SellOrderAppears,
                priceCondition: PriceCondition.AtOrBelow,
                threshold: 20.00m);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(4001, 15.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Not.Null);
        }

        [Test]
        public void EvaluateAlerts_AtOrBelow_DoesNotFireWhenPriceAboveThreshold()
        {
            var alert = CreateAlert(
                MarketAlertType.SellOrderAppears,
                priceCondition: PriceCondition.AtOrBelow,
                threshold: 10.00m);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(5001, 25.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }

        // -------------------------------------------------------------------
        // Test: Respects AtOrAbove price condition
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_AtOrAbove_FiresWhenPriceAboveThreshold()
        {
            var alert = CreateAlert(
                MarketAlertType.BuyOrderAppears,
                priceCondition: PriceCondition.AtOrAbove,
                threshold: 50.00m);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateBuyOrder(6001, 75.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Not.Null);
        }

        [Test]
        public void EvaluateAlerts_AtOrAbove_DoesNotFireWhenPriceBelowThreshold()
        {
            var alert = CreateAlert(
                MarketAlertType.BuyOrderAppears,
                priceCondition: PriceCondition.AtOrAbove,
                threshold: 100.00m);
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateBuyOrder(7001, 25.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }

        // -------------------------------------------------------------------
        // Test: Skips already-triggered MarketIds
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_SkipsAlreadyTriggeredMarketId()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            alert.LastTriggeredMarketId = 8001;
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(8001, 10.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }

        [Test]
        public void EvaluateAlerts_FiresForNewMarketIdAfterTriggered()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            alert.LastTriggeredMarketId = 8001;
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(8002, 10.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Not.Null);
            Assert.That(firedArgs.MatchingOrders[0].MarketId, Is.EqualTo(8002));
        }

        // -------------------------------------------------------------------
        // Test: Disabled alerts don't fire
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_DisabledAlert_DoesNotFire()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            alert.Enabled = false;
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(9001, 10.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }

        // -------------------------------------------------------------------
        // Test: Station filter works
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_StationFilter_OnlyMatchesCorrectStation()
        {
            var alert = CreateAlert(
                MarketAlertType.SellOrderAppears,
                stationUuid: "station-specific");
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(10001, 10.00m, stationUuid: "station-other"),
            };

            _svc.EvaluateAlerts(freshOrders);
            Assert.That(firedArgs, Is.Null);

            // Now with matching station
            freshOrders = new List<MarketListing>
            {
                CreateSellOrder(10002, 10.00m, stationUuid: "station-specific"),
            };

            _svc.EvaluateAlerts(freshOrders);
            Assert.That(firedArgs, Is.Not.Null);
        }

        // -------------------------------------------------------------------
        // Test: Updates tracking fields on trigger
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_UpdatesTrackingFieldsOnTrigger()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            _ctx.MarketSyncData.Alerts.Add(alert);

            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(11001, 10.00m),
            };

            SystemClock.FreezeAt(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
            try
            {
                _svc.EvaluateAlerts(freshOrders);

                Assert.That(alert.LastTriggeredMarketId, Is.EqualTo(11001));
                Assert.That(alert.LastTriggeredTimestamp, Is.Not.Empty);
            }
            finally
            {
                SystemClock.Reset();
            }
        }

        // -------------------------------------------------------------------
        // Test: Item type mismatch does not fire
        // -------------------------------------------------------------------

        [Test]
        public void EvaluateAlerts_ItemTypeMismatch_DoesNotFire()
        {
            var alert = CreateAlert(MarketAlertType.SellOrderAppears);
            alert.ItemType = ItemType.ItemTypeEnum.Commodity;
            _ctx.MarketSyncData.Alerts.Add(alert);

            MarketAlertTriggeredEventArgs firedArgs = null;
            _svc.MarketAlertTriggered += (s, e) => firedArgs = e;

            // Order is Resource, alert expects Commodity
            var freshOrders = new List<MarketListing>
            {
                CreateSellOrder(12001, 10.00m),
            };

            _svc.EvaluateAlerts(freshOrders);

            Assert.That(firedArgs, Is.Null);
        }
    }
}
