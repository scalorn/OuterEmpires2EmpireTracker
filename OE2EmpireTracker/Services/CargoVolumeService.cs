using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Computes cargo volume and mass for delivery load lists.
    /// Handles crate contents with one-level recursion (crates cannot nest).
    /// </summary>
    public static class CargoVolumeService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Result of a cargo volume computation.
        /// </summary>
        public class CargoLoadResult
        {
            public decimal TotalVolume { get; set; }
            public decimal TotalMass { get; set; }
        }

        /// <summary>
        /// Computes total volume and mass for a delivery load list.
        /// For crates, recursively sums contents volume (one level only).
        /// </summary>
        /// <param name="loadList">The delivery items to compute volume for.</param>
        /// <param name="blueprintFinder">Function to look up a blueprint by UUID/BaseItemTypeID.</param>
        /// <returns>Total volume and mass.</returns>
        public static CargoLoadResult ComputeLoadVolume(
            List<DeliveryItem> loadList,
            Func<string, Blueprint> blueprintFinder)
        {
            if (loadList == null) return new CargoLoadResult();
            if (blueprintFinder == null) blueprintFinder = _ => null;

            var result = new CargoLoadResult();

            foreach (var item in loadList)
            {
                decimal unitVolume = GetItemVolume(item, blueprintFinder);
                decimal unitMass = GetItemMass(item, blueprintFinder);
                result.TotalVolume += item.Quantity * unitVolume;
                result.TotalMass += item.Quantity * unitMass;
            }

            Log.Debug("ComputeLoadVolume: {0} items, totalVol={1:N0}, totalMass={2:N0}",
                loadList.Count, result.TotalVolume, result.TotalMass);
            return result;
        }

        /// <summary>
        /// Returns the per-unit cargo volume for a delivery item.
        /// For crates, sums the volume of contents (one level only).
        /// </summary>
        public static decimal GetItemVolume(DeliveryItem item, Func<string, Blueprint> blueprintFinder)
        {
            switch (item.ItemType)
            {
                case ItemType.ItemTypeEnum.Resource: return GameConstants.VolumeResource;
                case ItemType.ItemTypeEnum.Commodity: return GameConstants.VolumeCommodity;
                case ItemType.ItemTypeEnum.WorkDetail: return GameConstants.VolumeWorkDetail;
                case ItemType.ItemTypeEnum.Blueprint:
                case ItemType.ItemTypeEnum.Survey: return GameConstants.VolumeBlueprint;
                default:
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID))
                    {
                        var bp = blueprintFinder(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            // Check if this is a crate blueprint
                            if (IsCrateType(bp))
                            {
                                return GetCrateContentsVolume(bp, blueprintFinder);
                            }

                            decimal vol = 0m;
                            bp.Properties.getDecimal(BlueprintPropertyKeys.CargoVolumeSize, 0, out vol);
                            return vol;
                        }
                    }
                    return 0.0m;
            }
        }

        /// <summary>
        /// Returns the per-unit mass for a delivery item.
        /// </summary>
        public static decimal GetItemMass(DeliveryItem item, Func<string, Blueprint> blueprintFinder)
        {
            switch (item.ItemType)
            {
                case ItemType.ItemTypeEnum.Resource: return GameConstants.MassResource;
                case ItemType.ItemTypeEnum.Commodity: return GameConstants.MassCommodity;
                case ItemType.ItemTypeEnum.WorkDetail: return GameConstants.MassWorkDetail;
                case ItemType.ItemTypeEnum.Blueprint:
                case ItemType.ItemTypeEnum.Survey: return GameConstants.VolumeBlueprint;
                default:
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID))
                    {
                        var bp = blueprintFinder(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            decimal mass = 0m;
                            bp.Properties.getDecimal(BlueprintPropertyKeys.Mass, 0, out mass);
                            return mass;
                        }
                    }
                    return 0.0m;
            }
        }

        /// <summary>
        /// Checks if a blueprint represents a crate type.
        /// </summary>
        private static bool IsCrateType(Blueprint bp)
        {
            if (bp == null) return false;
            return !string.IsNullOrEmpty(bp.BluePrintType) &&
                   bp.BluePrintType.IndexOf("Crate", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Computes the total volume of a crate's contents (one level only).
        /// Looks at the crate blueprint's resource requirements as a proxy for contents.
        /// Falls back to the crate's own Cargo Volume Size if no contents can be determined.
        /// </summary>
        private static decimal GetCrateContentsVolume(Blueprint crateBp, Func<string, Blueprint> blueprintFinder)
        {
            // A crate's volume is its own Cargo Volume Size property
            decimal crateVol = 0m;
            crateBp.Properties.getDecimal(BlueprintPropertyKeys.CargoVolumeSize, 0, out crateVol);
            return crateVol;
        }

        /// <summary>
        /// Splits a load list into multiple trips, each within the given cargo capacity.
        /// Items are assigned to trips in order; an item that exceeds capacity goes alone.
        /// </summary>
        /// <param name="loadList">Items to split across trips.</param>
        /// <param name="cargoCapacity">Maximum volume per trip.</param>
        /// <param name="blueprintFinder">Function to look up blueprints.</param>
        /// <returns>List of trips, each containing a subset of items with quantities.</returns>
        public static List<List<DeliveryItem>> SplitIntoTrips(
            List<DeliveryItem> loadList,
            decimal cargoCapacity,
            Func<string, Blueprint> blueprintFinder)
        {
            if (loadList == null || loadList.Count == 0)
                return new List<List<DeliveryItem>>();

            if (cargoCapacity <= 0)
            {
                // Can't split with zero capacity  return everything in one trip
                return new List<List<DeliveryItem>> { new List<DeliveryItem>(loadList) };
            }

            if (blueprintFinder == null) blueprintFinder = _ => null;

            var trips = new List<List<DeliveryItem>>();
            var currentTrip = new List<DeliveryItem>();
            decimal currentVolume = 0m;

            foreach (var item in loadList)
            {
                decimal unitVolume = GetItemVolume(item, blueprintFinder);
                int remaining = item.Quantity;

                while (remaining > 0)
                {
                    // How many units fit in the current trip?
                    int canFit;
                    if (unitVolume <= 0)
                    {
                        canFit = remaining; // zero-volume items always fit
                    }
                    else
                    {
                        decimal spaceLeft = cargoCapacity - currentVolume;
                        canFit = (int)(spaceLeft / unitVolume);
                        if (canFit <= 0)
                        {
                            // Current trip is full  start a new one
                            if (currentTrip.Count > 0)
                            {
                                trips.Add(currentTrip);
                                currentTrip = new List<DeliveryItem>();
                                currentVolume = 0m;
                            }
                            // Recalculate with fresh trip
                            spaceLeft = cargoCapacity;
                            canFit = (int)(spaceLeft / unitVolume);
                            if (canFit <= 0) canFit = 1; // At least one unit per trip even if oversized
                        }
                    }

                    int toAdd = Math.Min(canFit, remaining);
                    currentTrip.Add(new DeliveryItem
                    {
                        ItemType = item.ItemType,
                        BaseItemTypeID = item.BaseItemTypeID,
                        Name = item.Name,
                        ResourcePurity = item.ResourcePurity,
                        Quantity = toAdd
                    });
                    currentVolume += toAdd * unitVolume;
                    remaining -= toAdd;
                }
            }

            if (currentTrip.Count > 0)
                trips.Add(currentTrip);

            Log.Info("SplitIntoTrips: {0} items split into {1} trips (capacity={2:N0})",
                loadList.Count, trips.Count, cargoCapacity);
            return trips;
        }
    }
}