using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class SupplyChainServiceTests
    {
        private const string PlayerUUID = "player-1";

        [Test]
        public void CheckThresholds_EmptyChains_ReturnsEmptyRequests()
        {
            var result = SupplyChainService.CheckThresholds(
                Enumerable.Empty<SupplyChain>(),
                _ => null,
                _ => null,
                _ => null,
                PlayerUUID);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckThresholds_InactiveChain_IsSkipped()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                IsActive = false,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 10,
                        DeliveryRouteUUID = "route-1",
                        LocationType = DestinationType.Colony,
                        LocationUUID = "col-1",
                        ResourceName = "Iron",
                        ResourcePurity = "High"
                    }
                }
            };

            var colony = MakeColonyWithResource("col-1", "Iron", "High", 100);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                uuid => uuid == "col-1" ? colony : null,
                _ => null, _ => null, PlayerUUID);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckThresholds_StageBelowThreshold_NoRequest()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                IsActive = true,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 100,
                        DeliveryRouteUUID = "route-1",
                        LocationType = DestinationType.Colony,
                        LocationUUID = "col-1",
                        ResourceName = "Iron",
                        ResourcePurity = "High"
                    }
                }
            };

            var colony = MakeColonyWithResource("col-1", "Iron", "High", 50);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                uuid => uuid == "col-1" ? colony : null,
                _ => null, _ => null, PlayerUUID);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckThresholds_StageAboveThreshold_ReturnsRequestWithCorrectExcess()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                Name = "Iron Chain",
                IsActive = true,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 50,
                        DeliveryRouteUUID = "route-1",
                        LocationType = DestinationType.Colony,
                        LocationUUID = "col-1",
                        ResourceName = "Iron",
                        ResourcePurity = "High"
                    }
                }
            };

            var colony = MakeColonyWithResource("col-1", "Iron", "High", 80);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                uuid => uuid == "col-1" ? colony : null,
                _ => null, _ => null, PlayerUUID);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ExcessQuantity, Is.EqualTo(30));
            Assert.That(result[0].SupplyChainUUID, Is.EqualTo("chain-1"));
            Assert.That(result[0].StageSequence, Is.EqualTo(1));
            Assert.That(result[0].ResourceName, Is.EqualTo("Iron"));
            Assert.That(result[0].DeliveryRouteUUID, Is.EqualTo("route-1"));
        }

        [Test]
        public void CheckThresholds_StageWithNoRoute_IsSkipped()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                IsActive = true,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 10,
                        DeliveryRouteUUID = string.Empty,
                        LocationType = DestinationType.Colony,
                        LocationUUID = "col-1",
                        ResourceName = "Iron",
                        ResourcePurity = "High"
                    }
                }
            };

            var colony = MakeColonyWithResource("col-1", "Iron", "High", 100);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                uuid => uuid == "col-1" ? colony : null,
                _ => null, _ => null, PlayerUUID);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckThresholds_ColonyLocation_ResolvesInventoryCorrectly()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                IsActive = true,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 20,
                        DeliveryRouteUUID = "route-1",
                        LocationType = DestinationType.Colony,
                        LocationUUID = "col-1",
                        ResourceName = "Copper",
                        ResourcePurity = "Medium"
                    }
                }
            };

            var colony = MakeColonyWithResource("col-1", "Copper", "Medium", 35);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                uuid => uuid == "col-1" ? colony : null,
                _ => null, _ => null, PlayerUUID);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ExcessQuantity, Is.EqualTo(15));
        }

        [Test]
        public void CheckThresholds_StationLocation_ResolvesInventoryFromPlayerHold()
        {
            var chain = new SupplyChain
            {
                UUID = "chain-1",
                IsActive = true,
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage
                    {
                        Sequence = 1,
                        AccumulationThreshold = 25,
                        DeliveryRouteUUID = "route-1",
                        LocationType = DestinationType.Station,
                        LocationUUID = "sta-1",
                        ResourceName = "Iron",
                        ResourcePurity = "Refined"
                    }
                }
            };

            var station = MakeStationWithResource("sta-1", PlayerUUID, "Iron", "Refined", 60);

            var result = SupplyChainService.CheckThresholds(
                new[] { chain },
                _ => null,
                uuid => uuid == "sta-1" ? station : null,
                _ => null, PlayerUUID);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ExcessQuantity, Is.EqualTo(35));
            Assert.That(result[0].SourceLocationType, Is.EqualTo(DestinationType.Station));
        }

        [Test]
        public void CheckThresholds_NullChains_ReturnsEmpty()
        {
            var result = SupplyChainService.CheckThresholds(
                null,
                _ => null, _ => null, _ => null, PlayerUUID);

            Assert.That(result, Is.Empty);
        }

        private Colony MakeColonyWithResource(string uuid, string resource, string purity, int qty)
        {
            var colony = new Colony { UUID = uuid, Items = new ItemBag() };
            colony.Items.AddItem(new Item
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = resource,
                ResourcePurity = purity,
                Quantity = qty
            });
            return colony;
        }

        private Station MakeStationWithResource(string uuid, string playerUUID, string resource, string purity, int qty)
        {
            var station = new Station { UUID = uuid };
            var hold = new ItemBag();
            hold.AddItem(new Item
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = ItemType.ItemTypeEnum.Resource,
                BaseItemTypeID = resource,
                ResourcePurity = purity,
                Quantity = qty
            });
            station.Holds[playerUUID] = hold;
            return station;
        }
    }
}
