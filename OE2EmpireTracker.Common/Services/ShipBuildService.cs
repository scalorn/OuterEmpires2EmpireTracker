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
            Func<string, ReadOnlyBlueprint> blueprintFinder,
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
                            assemblyLocationType,
                            assemblyLocationUUID));
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
                            assemblyLocationType,
                            assemblyLocationUUID));
                    }
                }
            }

            sw.Stop();
            Log.Info(
                "PERF GenerateShipBuildItems: {0} items for {1}x '{2}' in {3}ms",
                items.Count,
                quantity,
                template.Name,
                sw.ElapsedMilliseconds);
            return items;
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
            ReadOnlyBlueprint hullBlueprint,
            IEnumerable<ShipComponentSlot> components,
            Func<string, ReadOnlyBlueprint> blueprintFinder)
        {
            if (hullBlueprint == null) throw new ArgumentNullException(nameof(hullBlueprint));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var stats = new ShipStats();
            var compList = components ?? Enumerable.Empty<ShipComponentSlot>();
            decimal fuelPerJASPerMass = 0m;

            // Hull stats
            AddBlueprintStats(stats, hullBlueprint);

            // Read hull identity
            stats.ShipType = hullBlueprint.Name ?? string.Empty;
            stats.ShipClass = hullBlueprint.Class;

            string career = string.Empty;
            hullBlueprint.Properties?.GetString(BlueprintPropertyKeys.LicenseCareer, string.Empty, out career);
            stats.LicenseCareer = career ?? string.Empty;
            decimal licLevel = 0m;
            hullBlueprint.Properties?.GetDecimal(BlueprintPropertyKeys.LicenseLevel, 0m, out licLevel);
            stats.LicenseLevel = (int)licLevel;

            // Hull eng capacity available
            decimal engAvail = 0m;
            if (hullBlueprint.Properties != null)
            {
                hullBlueprint.Properties.GetDecimal(BlueprintPropertyKeys.EngCapacityAvailable, 0m, out engAvail);
            }

            stats.EngCapacityAvailable = engAvail;

            // Weapon and mining draw tracking for per-type grouping
            var weaponDraws = new Dictionary<string, List<decimal>>();
            var miningDraws = new Dictionary<string, List<decimal>>();

            // Component stats
            foreach (var slot in compList)
            {
                if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;
                var bp = blueprintFinder(slot.BlueprintUUID);
                if (bp == null) continue;
                AddBlueprintStats(stats, bp);

                // Collect eng capacity required from components
                decimal engReq = 0m;
                if (bp.Properties != null)
                {
                    bp.Properties.GetDecimal(BlueprintPropertyKeys.EngCapacityRequired, 0m, out engReq);
                }

                stats.EngCapacityUsed += engReq;

                // Collect type-specific power data
                string bpType = bp.BluePrintType ?? string.Empty;
                decimal drawPerSec = 0m;
                if (bp.Properties != null)
                {
                    bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerDrawPerSecond, 0m, out drawPerSec);
                }

                if (bpType == BlueprintTypes.Reactor)
                {
                    decimal pp = 0m;
                    decimal rr = 0m;
                    if (bp.Properties != null)
                    {
                        bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerProvided, 0m, out pp);
                        bp.Properties.GetDecimal(BlueprintPropertyKeys.PowerRegenRate, 0m, out rr);
                    }

                    stats.PowerProvided += pp;
                    stats.PowerRegenRate += rr;
                }
                else if (bpType == BlueprintTypes.Shield)
                {
                    stats.ShieldPowerDraw += drawPerSec;
                }
                else if (IsWeaponType(bpType))
                {
                    stats.TotalWeaponPowerDraw += drawPerSec;
                    if (!weaponDraws.ContainsKey(bpType))
                    {
                        weaponDraws[bpType] = new List<decimal>();
                    }

                    weaponDraws[bpType].Add(drawPerSec);
                }
                else if (bpType == BlueprintTypes.MiningLaser)
                {
                    if (!miningDraws.ContainsKey(bpType))
                    {
                        miningDraws[bpType] = new List<decimal>();
                    }

                    miningDraws[bpType].Add(drawPerSec);
                }
                else if (bpType == BlueprintTypes.NavComp)
                {
                    decimal chargeTime = 0m;
                    if (bp.Properties != null)
                    {
                        bp.Properties.GetDecimal(BlueprintPropertyKeys.JumpChargeTime, 0m, out chargeTime);
                    }

                    stats.JumpChargeTime = chargeTime;
                }
                else if (bpType == BlueprintTypes.JumpDrive)
                {
                    decimal fpm = 0m;
                    if (bp.Properties != null)
                    {
                        bp.Properties.GetDecimal(BlueprintPropertyKeys.FuelPerJASPerMass, 0m, out fpm);
                    }

                    fuelPerJASPerMass = fpm;
                }
            }

            stats.PowerBalance = stats.PowerGenerated - stats.PowerConsumed;

            // Build per-type sustainability lists
            foreach (var kvp in weaponDraws)
            {
                decimal avgDraw = kvp.Value.Count > 0 ? kvp.Value[0] : 0m;
                stats.WeaponSustainByType.Add(new WeaponSustainEntry
                {
                    WeaponType = kvp.Key,
                    PowerDrawPerSecond = avgDraw,
                    Count = kvp.Value.Count,
                });
            }

            foreach (var kvp in miningDraws)
            {
                decimal avgDraw = kvp.Value.Count > 0 ? kvp.Value[0] : 0m;
                stats.MiningSustainByType.Add(new MiningSustainEntry
                {
                    LaserType = kvp.Key,
                    PowerDrawPerSecond = avgDraw,
                    Count = kvp.Value.Count,
                });
            }

            // Phase 2: Compute derived stats
            ComputeDerivedStats(stats, fuelPerJASPerMass);

            sw.Stop();
            Log.Debug("PERF ComputeStats: '{0}' in {1}ms", hullBlueprint.Name, sw.ElapsedMilliseconds);
            return stats;
        }

        /// <summary>
        /// Computes station stats from station blueprint and installed components.
        /// </summary>
        public static StationStats ComputeStationStats(
            ReadOnlyBlueprint stationBlueprint,
            IEnumerable<ShipComponentSlot> components,
            Func<string, ReadOnlyBlueprint> blueprintFinder)
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

        private static BuildItem CreateComponentItem(
            string blueprintUUID,
            string itemName,
            DestinationType assemblyLocationType,
            string assemblyLocationUUID)
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

        private static void AddBlueprintStats(ShipStats stats, ReadOnlyBlueprint bp)
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

        private static void ComputeDerivedStats(ShipStats stats, decimal fuelPerJASPerMass)
        {
            // Propulsion
            stats.AccelerationFactor = stats.TotalMass > 0
                ? stats.Acceleration / stats.TotalMass : 0m;
            stats.TurnRate = stats.TotalMass > 0
                ? stats.RotationalThrust / stats.TotalMass : 0m;

            // Jump
            stats.JumpFuelPerJAS = fuelPerJASPerMass * stats.TotalMass;
            stats.JumpFuelRange = stats.JumpFuelPerJAS > 0
                ? stats.FuelCapacity / stats.JumpFuelPerJAS : 0m;

            // Shield sustainability
            if (stats.ShieldPowerDraw > 0)
            {
                stats.ShieldUptime = stats.PowerRegenRate >= stats.ShieldPowerDraw
                    ? -1m
                    : stats.PowerProvided / (stats.ShieldPowerDraw - stats.PowerRegenRate);
            }

            // Weapon sustainability (aggregate)
            if (stats.TotalWeaponPowerDraw > 0)
            {
                stats.WeaponSustainTime = stats.PowerRegenRate >= stats.TotalWeaponPowerDraw
                    ? -1m
                    : stats.PowerProvided / (stats.TotalWeaponPowerDraw - stats.PowerRegenRate);
            }

            // Per-type sustainability
            foreach (var entry in stats.WeaponSustainByType)
            {
                entry.SustainableCount = entry.PowerDrawPerSecond > 0
                    ? stats.PowerRegenRate / entry.PowerDrawPerSecond : 0m;
            }

            foreach (var entry in stats.MiningSustainByType)
            {
                entry.SustainableCount = entry.PowerDrawPerSecond > 0
                    ? stats.PowerRegenRate / entry.PowerDrawPerSecond : 0m;
            }
        }

        private static bool IsWeaponType(string blueprintType)
        {
            return blueprintType.StartsWith(BlueprintTypes.Beamer, StringComparison.Ordinal)
                || blueprintType.StartsWith(BlueprintTypes.Coilgun, StringComparison.Ordinal)
                || blueprintType.StartsWith(BlueprintTypes.Railgun, StringComparison.Ordinal)
                || blueprintType.StartsWith(BlueprintTypes.MissileLauncher, StringComparison.Ordinal)
                || blueprintType.StartsWith(BlueprintTypes.TorpedoLauncher, StringComparison.Ordinal);
        }

        private static void AddStationBlueprintStats(StationStats stats, ReadOnlyBlueprint bp)
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
