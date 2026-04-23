using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for ship build operations: generating build items
    /// from templates, validating assembly locations, and computing stats.
    /// </summary>
    public static class ShipBuildService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Generates Manufactory BuildItems for a ship template order.
        /// Creates items for hull + each component, skipping items already in stock.
        /// </summary>
        public static List<BuildItem> GenerateShipBuildItems(
            ShipTemplate template,
            int quantity,
            DestinationType assemblyLocationType,
            string assemblyLocationUUID,
            Func<string, Blueprint> blueprintFinder,
            Func<string, int> stockChecker)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var items = new List<BuildItem>();
            var checker = stockChecker ?? (uuid => 0);

            for (int ship = 0; ship < quantity; ship++)
            {
                // Hull
                if (!string.IsNullOrEmpty(template.HullBlueprintUUID))
                {
                    int inStock = checker(template.HullBlueprintUUID);
                    if (inStock <= 0)
                    {
                        var bp = blueprintFinder(template.HullBlueprintUUID);
                        items.Add(CreateComponentItem(
                            template.HullBlueprintUUID,
                            bp?.Name ?? "Hull",
                            assemblyLocationType, assemblyLocationUUID));
                    }
                }

                // Components
                foreach (var slot in template.Components)
                {
                    if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;
                    int inStock = checker(slot.BlueprintUUID);
                    if (inStock <= 0)
                    {
                        var bp = blueprintFinder(slot.BlueprintUUID);
                        items.Add(CreateComponentItem(
                            slot.BlueprintUUID,
                            bp?.Name ?? slot.SlotType,
                            assemblyLocationType, assemblyLocationUUID));
                    }
                }
            }

            sw.Stop();
            Log.Info("PERF GenerateShipBuildItems: {0} items for {1}x '{2}' in {3}ms",
                items.Count, quantity, template.Name, sw.ElapsedMilliseconds);
            return items;
        }

        private static BuildItem CreateComponentItem(
            string blueprintUUID, string itemName,
            DestinationType assemblyLocationType, string assemblyLocationUUID)
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                Status = BuildItemStatus.Staged,
                BlueprintUUID = blueprintUUID,
                ItemName = itemName,
                Quantity = 1,
                AssemblyLocationType = assemblyLocationType,
                AssemblyLocationUUID = assemblyLocationUUID
            };
        }

        /// <summary>
        /// Validates that a ship class can be assembled at the given station type.
        /// Returns null if valid, or an error message if invalid.
        /// Class 2-5: any station type. Class 6: Station or Starbase. Class 7-8: Starbase only.
        /// </summary>
        public static string ValidateAssemblyLocation(int shipClass, StationType stationType)
        {
            if (shipClass <= 5) return null; // Class 2-5 can build anywhere
            if (shipClass == 6)
            {
                if (stationType == StationType.Station || stationType == StationType.Starbase)
                    return null;
                return string.Format("Class {0} ships require a Station or Starbase for assembly.", shipClass);
            }

            // Class 7-8
            if (stationType == StationType.Starbase) return null;
            return string.Format("Class {0} ships require a Starbase for assembly.", shipClass);
        }

        /// <summary>
        /// Computes ship stats from hull blueprint and installed component blueprints.
        /// </summary>
        public static ShipStats ComputeStats(
            Blueprint hullBlueprint,
            IEnumerable<ShipComponentSlot> components,
            Func<string, Blueprint> blueprintFinder)
        {
            if (hullBlueprint == null) throw new ArgumentNullException(nameof(hullBlueprint));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var stats = new ShipStats();
            var compList = components ?? Enumerable.Empty<ShipComponentSlot>();

            // Hull stats
            AddBlueprintStats(stats, hullBlueprint);

            // Read hull identity
            string career = string.Empty;
            hullBlueprint.Properties?.GetString(BlueprintPropertyKeys.LicenseCareer, string.Empty, out career);
            stats.LicenseCareer = career ?? string.Empty;
            decimal licLevel = 0m;
            hullBlueprint.Properties?.GetDecimal(BlueprintPropertyKeys.LicenseLevel, 0m, out licLevel);
            stats.LicenseLevel = (int)licLevel;

            // Component stats
            foreach (var slot in compList)
            {
                if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;
                var bp = blueprintFinder(slot.BlueprintUUID);
                if (bp == null) continue;
                AddBlueprintStats(stats, bp);
            }

            stats.PowerBalance = stats.PowerGenerated - stats.PowerConsumed;

            sw.Stop();
            Log.Debug("PERF ComputeStats: '{0}' in {1}ms", hullBlueprint.Name, sw.ElapsedMilliseconds);
            return stats;
        }

        /// <summary>
        /// Computes station stats from station blueprint and installed components.
        /// </summary>
        public static StationStats ComputeStationStats(
            Blueprint stationBlueprint,
            IEnumerable<ShipComponentSlot> components,
            Func<string, Blueprint> blueprintFinder)
        {
            if (stationBlueprint == null) throw new ArgumentNullException(nameof(stationBlueprint));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var stats = new StationStats();
            var compList = components ?? Enumerable.Empty<ShipComponentSlot>();

            AddStationBlueprintStats(stats, stationBlueprint);

            foreach (var slot in compList)
            {
                if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;
                var bp = blueprintFinder(slot.BlueprintUUID);
                if (bp == null) continue;
                AddStationBlueprintStats(stats, bp);
            }

            stats.PowerBalance = stats.PowerGenerated - stats.PowerConsumed;

            sw.Stop();
            Log.Debug("PERF ComputeStationStats: '{0}' in {1}ms", stationBlueprint.Name, sw.ElapsedMilliseconds);
            return stats;
        }

        private static void AddBlueprintStats(ShipStats stats, Blueprint bp)
        {
            if (bp?.Properties == null) return;
            decimal v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.Mass, 0m, out v)) stats.TotalMass += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerGenerated, 0m, out v)) stats.PowerGenerated += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerConsumed, 0m, out v)) stats.PowerConsumed += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.CargoCapacity, 0m, out v)) stats.CargoCapacity += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.FuelCapacity, 0m, out v)) stats.FuelCapacity += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.RawMaterialCapacity, 0m, out v)) stats.HopperCapacity += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.Health, 0m, out v)) stats.TotalHealth += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.EnergyDefence, 0m, out v)) stats.EnergyDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.KineticDefence, 0m, out v)) stats.KineticDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.MissileDefence, 0m, out v)) stats.MissileDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.ShieldHitpoints, 0m, out v)) stats.ShieldHitpoints += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.ShieldRegen, 0m, out v)) stats.ShieldRegen += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.Acceleration, 0m, out v)) stats.Acceleration += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.RotationalThrust, 0m, out v)) stats.RotationalThrust += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.JumpDistance, 0m, out v)) stats.MaxJumpDistance += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.FuelPerJump, 0m, out v)) stats.FuelPerJump += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.MiningYield, 0m, out v)) stats.MiningYield += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.MiningCycleTime, 0m, out v)) stats.MiningCycleTime += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.ScanLevel, 0m, out v)) stats.ScanLevel = Math.Max(stats.ScanLevel, (int)v);
        }

        private static void AddStationBlueprintStats(StationStats stats, Blueprint bp)
        {
            if (bp?.Properties == null) return;
            decimal v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.Mass, 0m, out v)) stats.TotalMass += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerGenerated, 0m, out v)) stats.PowerGenerated += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerConsumed, 0m, out v)) stats.PowerConsumed += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.Health, 0m, out v)) stats.TotalHealth += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.EnergyDefence, 0m, out v)) stats.EnergyDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.KineticDefence, 0m, out v)) stats.KineticDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.MissileDefence, 0m, out v)) stats.MissileDefence += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.ShieldHitpoints, 0m, out v)) stats.ShieldHitpoints += v;
            if (bp.Properties.GetDecimal(BlueprintPropertyKeys.ShieldRegen, 0m, out v)) stats.ShieldRegen += v;
        }
    }
}
