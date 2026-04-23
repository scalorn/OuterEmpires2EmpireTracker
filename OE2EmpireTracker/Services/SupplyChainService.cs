using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class SupplyChainDeliveryRequest
    {
        public string SupplyChainUUID { get; set; }
        public int StageSequence { get; set; }
        public string ResourceName { get; set; }
        public string ResourcePurity { get; set; }
        public int ExcessQuantity { get; set; }
        public string DeliveryRouteUUID { get; set; }
        public string SourceLocationUUID { get; set; }
        public DestinationType SourceLocationType { get; set; }
    }

    public static class SupplyChainService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Checks all active supply chain stages for accumulation threshold breaches.
        /// Returns delivery requests for stages where accumulated quantity exceeds threshold.
        /// </summary>
        public static List<SupplyChainDeliveryRequest> CheckThresholds(
            IEnumerable<SupplyChain> chains,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            Func<string, Ship> shipFinder,
            string currentPlayerUUID)
        {
            var requests = new List<SupplyChainDeliveryRequest>();
            if (chains == null) return requests;

            foreach (var chain in chains.Where(c => c.IsActive))
            {
                foreach (var stage in chain.Stages)
                {
                    if (stage.AccumulationThreshold <= 0) continue;
                    if (string.IsNullOrEmpty(stage.DeliveryRouteUUID)) continue;

                    int qty = ResolveInventory(stage, colonyFinder, stationFinder, shipFinder, currentPlayerUUID);
                    if (qty > stage.AccumulationThreshold)
                    {
                        requests.Add(new SupplyChainDeliveryRequest
                        {
                            SupplyChainUUID = chain.UUID,
                            StageSequence = stage.Sequence,
                            ResourceName = stage.ResourceName,
                            ResourcePurity = stage.ResourcePurity,
                            ExcessQuantity = qty - stage.AccumulationThreshold,
                            DeliveryRouteUUID = stage.DeliveryRouteUUID,
                            SourceLocationUUID = stage.LocationUUID,
                            SourceLocationType = stage.LocationType
                        });
                        Log.Info("Supply chain threshold exceeded: chain={0} stage={1} resource={2}({3}) qty={4} threshold={5}",
                            chain.Name, stage.Sequence, stage.ResourceName, stage.ResourcePurity,
                            qty, stage.AccumulationThreshold);
                    }
                }
            }

            return requests;
        }

        private static int ResolveInventory(
            SupplyChainStage stage,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            Func<string, Ship> shipFinder,
            string currentPlayerUUID)
        {
            switch (stage.LocationType)
            {
                case DestinationType.Colony:
                    var colony = colonyFinder(stage.LocationUUID);
                    if (colony?.Items == null) return 0;
                    return colony.Items.FindResource(stage.ResourceName, stage.ResourcePurity)
                        .Sum(i => i.Quantity);

                case DestinationType.Station:
                    var station = stationFinder(stage.LocationUUID);
                    if (station == null) return 0;
                    if (station.Holds.TryGetValue(currentPlayerUUID, out var hold) && hold != null)
                        return hold.FindResource(stage.ResourceName, stage.ResourcePurity)
                            .Sum(i => i.Quantity);
                    return 0;

                case DestinationType.Ship:
                    var ship = shipFinder(stage.LocationUUID);
                    if (ship?.Cargo == null) return 0;
                    return ship.Cargo.FindResource(stage.ResourceName, stage.ResourcePurity)
                        .Sum(i => i.Quantity);

                default:
                    return 0;
            }
        }
    }
}