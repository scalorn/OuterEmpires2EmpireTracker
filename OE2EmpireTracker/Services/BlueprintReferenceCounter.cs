using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public class BlueprintReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _flatpackMap;
        private readonly Dictionary<string, int> _researchingMap;
        private readonly Dictionary<string, int> _manufacturingMap;
        private readonly Dictionary<string, int> _baseBlueprintMap;
        private readonly Dictionary<string, int> _scannerMap;
        private readonly Dictionary<string, int> _buildItemMap;
        private readonly Dictionary<string, int> _shipComponentMap;
        private readonly Dictionary<string, int> _stockTargetMap;

        public BlueprintReferenceCounter(
            IEnumerable<Colony> colonies,
            IEnumerable<Blueprint> allBlueprints,
            IEnumerable<Survey> surveys,
            IEnumerable<BuildPlan> buildPlans = null,
            IEnumerable<ShipTemplate> shipTemplates = null,
            IEnumerable<Ship> ships = null,
            IEnumerable<Station> stations = null,
            IEnumerable<MarketListing> marketListings = null,
            IEnumerable<MarketTransaction> marketTransactions = null,
            IEnumerable<StockPlan> stockPlans = null)
        {
            var colonyList = colonies ?? Enumerable.Empty<Colony>();
            var bpList = allBlueprints ?? Enumerable.Empty<Blueprint>();
            var surveyList = surveys ?? Enumerable.Empty<Survey>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();
            var templateList = shipTemplates ?? Enumerable.Empty<ShipTemplate>();
            var shipList = ships ?? Enumerable.Empty<Ship>();
            var stationList = stations ?? Enumerable.Empty<Station>();
            var marketListingList = marketListings ?? Enumerable.Empty<MarketListing>();
            var marketTransactionList = marketTransactions ?? Enumerable.Empty<MarketTransaction>();
            var stockPlanList = stockPlans ?? Enumerable.Empty<StockPlan>();

            _flatpackMap = new Dictionary<string, int>();
            _researchingMap = new Dictionary<string, int>();
            _manufacturingMap = new Dictionary<string, int>();
            foreach (var colony in colonyList)
            {
                if (colony.Structures == null) continue;
                foreach (var s in colony.Structures)
                {
                    if (!string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                    { _flatpackMap.TryGetValue(s.FlatpackBlueprintUUID, out int c); _flatpackMap[s.FlatpackBlueprintUUID] = c + 1; }
                    if (!string.IsNullOrEmpty(s.ResearchingBlueprintUUID))
                    { _researchingMap.TryGetValue(s.ResearchingBlueprintUUID, out int c); _researchingMap[s.ResearchingBlueprintUUID] = c + 1; }
                    if (!string.IsNullOrEmpty(s.ManufacturingBlueprintUUID))
                    { _manufacturingMap.TryGetValue(s.ManufacturingBlueprintUUID, out int c); _manufacturingMap[s.ManufacturingBlueprintUUID] = c + 1; }
                }
            }

            _baseBlueprintMap = new Dictionary<string, int>();
            foreach (var b in bpList)
            {
                if (!string.IsNullOrEmpty(b.BaseBlueprintUUID) && b.UUID != b.BaseBlueprintUUID)
                { _baseBlueprintMap.TryGetValue(b.BaseBlueprintUUID, out int c); _baseBlueprintMap[b.BaseBlueprintUUID] = c + 1; }
            }

            _scannerMap = new Dictionary<string, int>();
            foreach (var s in surveyList)
            {
                if (!string.IsNullOrEmpty(s.ScannerBlueprintUUID))
                { _scannerMap.TryGetValue(s.ScannerBlueprintUUID, out int c); _scannerMap[s.ScannerBlueprintUUID] = c + 1; }
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var plan in buildPlanList)
            {
                if (plan.Items == null) continue;
                foreach (var item in plan.Items)
                {
                    if (!string.IsNullOrEmpty(item.BlueprintUUID))
                    { _buildItemMap.TryGetValue(item.BlueprintUUID, out int c); _buildItemMap[item.BlueprintUUID] = c + 1; }
                }
            }

            _shipComponentMap = new Dictionary<string, int>();
            foreach (var tmpl in templateList)
            {
                if (!string.IsNullOrEmpty(tmpl.HullBlueprintUUID))
                { _shipComponentMap.TryGetValue(tmpl.HullBlueprintUUID, out int c); _shipComponentMap[tmpl.HullBlueprintUUID] = c + 1; }
                if (tmpl.Components != null)
                {
                    foreach (var comp in tmpl.Components)
                    {
                        if (!string.IsNullOrEmpty(comp.BlueprintUUID))
                        { _shipComponentMap.TryGetValue(comp.BlueprintUUID, out int c); _shipComponentMap[comp.BlueprintUUID] = c + 1; }
                    }
                }
            }
            foreach (var ship in shipList)
            {
                if (!string.IsNullOrEmpty(ship.HullBlueprintUUID))
                { _shipComponentMap.TryGetValue(ship.HullBlueprintUUID, out int c); _shipComponentMap[ship.HullBlueprintUUID] = c + 1; }
                if (ship.Components != null)
                {
                    foreach (var comp in ship.Components)
                    {
                        if (!string.IsNullOrEmpty(comp.BlueprintUUID))
                        { _shipComponentMap.TryGetValue(comp.BlueprintUUID, out int c); _shipComponentMap[comp.BlueprintUUID] = c + 1; }
                    }
                }
            }
            foreach (var station in stationList)
            {
                if (!string.IsNullOrEmpty(station.StationBlueprintUUID))
                { _shipComponentMap.TryGetValue(station.StationBlueprintUUID, out int c); _shipComponentMap[station.StationBlueprintUUID] = c + 1; }
                if (station.Components != null)
                {
                    foreach (var comp in station.Components)
                    {
                        if (!string.IsNullOrEmpty(comp.BlueprintUUID))
                        { _shipComponentMap.TryGetValue(comp.BlueprintUUID, out int c); _shipComponentMap[comp.BlueprintUUID] = c + 1; }
                    }
                }
            }

            foreach (var listing in marketListingList)
            {
                if (!string.IsNullOrEmpty(listing.ItemReferenceID))
                { _shipComponentMap.TryGetValue(listing.ItemReferenceID, out int c); _shipComponentMap[listing.ItemReferenceID] = c + 1; }
            }

            // MarketTransaction item references
            foreach (var tx in marketTransactionList)
            {
                if (!string.IsNullOrEmpty(tx.ItemReferenceID))
                { _shipComponentMap.TryGetValue(tx.ItemReferenceID, out int c); _shipComponentMap[tx.ItemReferenceID] = c + 1; }
            }

            // StockPlan target item references
            _stockTargetMap = new Dictionary<string, int>();
            foreach (var plan in stockPlanList)
            {
                if (plan.Targets == null) continue;
                foreach (var target in plan.Targets)
                {
                    if (!string.IsNullOrEmpty(target.ItemReferenceID))
                    { _stockTargetMap.TryGetValue(target.ItemReferenceID, out int c); _stockTargetMap[target.ItemReferenceID] = c + 1; }
                }
            }
        }

        public ReferenceReport CountReferences(string blueprintUUID)
        {
            if (string.IsNullOrEmpty(blueprintUUID))
                return ReferenceReport.Empty;

            _flatpackMap.TryGetValue(blueprintUUID, out int flatpackCount);
            _researchingMap.TryGetValue(blueprintUUID, out int researchingCount);
            _manufacturingMap.TryGetValue(blueprintUUID, out int manufacturingCount);
            _baseBlueprintMap.TryGetValue(blueprintUUID, out int baseBlueprintCount);
            _scannerMap.TryGetValue(blueprintUUID, out int scannerCount);
            _buildItemMap.TryGetValue(blueprintUUID, out int buildItemCount);
            _shipComponentMap.TryGetValue(blueprintUUID, out int shipComponentCount);
            _stockTargetMap.TryGetValue(blueprintUUID, out int stockTargetCount);

            return new ReferenceReport(
                flatpackCount, researchingCount, manufacturingCount,
                baseBlueprintCount, scannerCount, buildItemCount, shipComponentCount, stockTargetCount);
        }
    }
}