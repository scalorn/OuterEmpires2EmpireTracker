using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class StockShortfall
    {
        public StockTarget Target { get; set; }
        public string PlanUUID { get; set; }
        public int CurrentQuantity { get; set; }
        public int ShortfallQuantity { get; set; }
        public bool IsCritical { get; set; }
    }

    public static class StockTargetService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Checks all active stock plans and returns shortfalls.
        /// Within a plan, overlapping component requirements use max(quantity) (OR pooling).
        /// Across plans, each plan's requirements are summed (AND/dedicated).
        /// Ship template targets are expanded into component requirements.
        /// </summary>
        public static List<StockShortfall> CheckTargets(
            IEnumerable<StockPlan> plans,
            string currentPlayerUUID,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            Func<string, ShipTemplate> templateFinder,
            Func<string, Blueprint> blueprintFinder,
            IEnumerable<Colony> allColonies,
            IEnumerable<Station> allStations)
        {
            var shortfalls = new List<StockShortfall>();
            if (plans == null) return shortfalls;

            var activePlans = plans.Where(p => p.IsActive).ToList();
            var colonies = allColonies?.ToList() ?? new List<Colony>();
            var stations = allStations?.ToList() ?? new List<Station>();

            foreach (var plan in activePlans)
            {
                if (plan.Targets == null || plan.Targets.Count == 0) continue;

                foreach (var target in plan.Targets)
                {
                    int currentQty = ResolveCurrentQuantity(
                        target, currentPlayerUUID, colonyFinder, stationFinder,
                        templateFinder, blueprintFinder, colonies, stations);

                    int shortfall = target.TargetQuantity - currentQty;
                    if (shortfall <= 0) continue;

                    shortfalls.Add(new StockShortfall
                    {
                        Target = target,
                        PlanUUID = plan.UUID,
                        CurrentQuantity = currentQty,
                        ShortfallQuantity = shortfall,
                        IsCritical = currentQty < target.CriticalThreshold
                    });
                }
            }

            Log.Info("CheckTargets: {0} active plans, {1} shortfalls found",
                activePlans.Count, shortfalls.Count);
            return shortfalls;
        }

        /// <summary>
        /// Generates build items to cover shortfalls, avoiding duplicates
        /// with existing build items in the replenishment plan.
        /// </summary>
        public static List<BuildItem> GenerateReplenishmentItems(
            List<StockShortfall> shortfalls,
            IEnumerable<BuildPlan> existingPlans)
        {
            var items = new List<BuildItem>();
            if (shortfalls == null || shortfalls.Count == 0) return items;

            // Build a set of existing item keys to avoid duplicates
            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (existingPlans != null)
            {
                foreach (var plan in existingPlans)
                {
                    if (plan.Items == null) continue;
                    foreach (var item in plan.Items)
                    {
                        if (item.Status != BuildItemStatus.Completed)
                        {
                            string key = item.ItemType + ":" + item.BlueprintUUID;
                            existingKeys.Add(key);
                        }
                    }
                }
            }

            foreach (var shortfall in shortfalls)
            {
                var target = shortfall.Target;
                string key = target.ItemType + ":" + target.ItemReferenceID;
                if (existingKeys.Contains(key)) continue;

                var buildItem = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = MapToBuildItemType(target.ItemType),
                    BlueprintUUID = target.ItemReferenceID,
                    ItemName = target.ItemName,
                    Quantity = shortfall.ShortfallQuantity,
                    Status = BuildItemStatus.Staged
                };

                items.Add(buildItem);
                existingKeys.Add(key);
            }

            Log.Info("GenerateReplenishmentItems: {0} items generated from {1} shortfalls",
                items.Count, shortfalls.Count);
            return items;
        }

        private static int ResolveCurrentQuantity(
            StockTarget target,
            string currentPlayerUUID,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            Func<string, ShipTemplate> templateFinder,
            Func<string, Blueprint> blueprintFinder,
            List<Colony> allColonies,
            List<Station> allStations)
        {
            // For ship template targets, we count the minimum across all components
            // (the bottleneck determines how many complete ships we can build)
            if (!string.IsNullOrEmpty(target.ShipTemplateUUID))
            {
                return CountShipTemplateStock(target, currentPlayerUUID,
                    colonyFinder, stationFinder, templateFinder, blueprintFinder,
                    allColonies, allStations);
            }

            // For simple item targets, count directly
            return CountItemStock(target, currentPlayerUUID,
                colonyFinder, stationFinder, allColonies, allStations);
        }

        private static int CountItemStock(
            StockTarget target,
            string currentPlayerUUID,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            List<Colony> allColonies,
            List<Station> allStations)
        {
            int total = 0;

            switch (target.Scope)
            {
                case StockTargetScope.EmpireWide:
                    foreach (var colony in allColonies)
                    {
                        if (colony.Items == null) continue;
                        total += colony.Items.CountByType(target.ItemType, target.ItemReferenceID);
                    }

                    foreach (var station in allStations)
                    {
                        ItemBag hold;
                        if (station.Holds.TryGetValue(currentPlayerUUID, out hold) && hold != null)
                            total += hold.CountByType(target.ItemType, target.ItemReferenceID);
                    }

                    break;

                case StockTargetScope.Colony:
                    var colony2 = colonyFinder(target.LocationUUID);
                    if (colony2?.Items != null)
                        total = colony2.Items.CountByType(target.ItemType, target.ItemReferenceID);
                    break;

                case StockTargetScope.Station:
                    var station2 = stationFinder(target.LocationUUID);
                    if (station2 != null)
                    {
                        ItemBag hold2;
                        if (station2.Holds.TryGetValue(currentPlayerUUID, out hold2) && hold2 != null)
                            total = hold2.CountByType(target.ItemType, target.ItemReferenceID);
                    }

                    break;
            }

            return total;
        }

        private static int CountShipTemplateStock(
            StockTarget target,
            string currentPlayerUUID,
            Func<string, Colony> colonyFinder,
            Func<string, Station> stationFinder,
            Func<string, ShipTemplate> templateFinder,
            Func<string, Blueprint> blueprintFinder,
            List<Colony> allColonies,
            List<Station> allStations)
        {
            var template = templateFinder(target.ShipTemplateUUID);
            if (template == null) return 0;

            // Count complete ships by finding the minimum component availability
            // Each component needs target.TargetQuantity units
            int minAvailable = int.MaxValue;

            // Check hull
            if (!string.IsNullOrEmpty(template.HullBlueprintUUID))
            {
                var hullTarget = new StockTarget
                {
                    ItemType = ItemType.ItemTypeEnum.ShipHull,
                    ItemReferenceID = template.HullBlueprintUUID,
                    Scope = target.Scope,
                    LocationUUID = target.LocationUUID
                };

                int hullCount = CountItemStock(hullTarget, currentPlayerUUID,
                    colonyFinder, stationFinder, allColonies, allStations);
                if (hullCount < minAvailable) minAvailable = hullCount;
            }

            // Check each component
            foreach (var comp in template.Components)
            {
                if (string.IsNullOrEmpty(comp.BlueprintUUID)) continue;
                var compTarget = new StockTarget
                {
                    ItemType = ItemType.ItemTypeEnum.ShipPart,
                    ItemReferenceID = comp.BlueprintUUID,
                    Scope = target.Scope,
                    LocationUUID = target.LocationUUID
                };

                int compCount = CountItemStock(compTarget, currentPlayerUUID,
                    colonyFinder, stationFinder, allColonies, allStations);
                if (compCount < minAvailable) minAvailable = compCount;
            }

            return minAvailable == int.MaxValue ? 0 : minAvailable;
        }

        private static BuildItemType MapToBuildItemType(ItemType.ItemTypeEnum itemType)
        {
            switch (itemType)
            {
                case ItemType.ItemTypeEnum.Commodity: return BuildItemType.Commodity;
                case ItemType.ItemTypeEnum.ShipHull:
                case ItemType.ItemTypeEnum.ShipPart: return BuildItemType.Manufactory;
                case ItemType.ItemTypeEnum.Resource: return BuildItemType.Mining;
                default: return BuildItemType.Manufactory;
            }
        }
    }
}