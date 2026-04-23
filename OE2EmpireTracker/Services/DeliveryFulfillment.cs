using System;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Static helper for delivery fulfillment operations.
    /// Extracted from FormDeliveryExecution to enable unit testing.
    /// </summary>
    public static class DeliveryFulfillment
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Fulfills or unfulfills a commodity request on a colony.
        /// When delivered: sets Delivered = Requested and Fulfilled = true.
        /// When undelivered: sets Delivered = 0 and Fulfilled = false.
        /// </summary>
        public static bool FulfillCommodity(Colony colony, string commodityName, bool delivered)
        {
            if (colony == null || string.IsNullOrEmpty(commodityName)) return false;

            var cr = colony.Commodities.FirstOrDefault(c => c.Name == commodityName);
            if (cr == null)
            {
                Log.Warn("No matching CommodityRequested '{0}' on colony {1}", commodityName, colony.ColonyName);
                return false;
            }

            if (delivered)
            {
                cr.Delivered = cr.Requested;
                cr.Fulfilled = true;
            }
            else
            {
                cr.Delivered = 0;
                cr.Fulfilled = false;
            }

            return true;
        }

        /// <summary>
        /// Stages or unstages a flatpack on a colony by matching the blueprint UUID.
        /// </summary>
        public static bool StageFlatpack(Colony colony, string flatpackBlueprintUUID, bool staged)
        {
            if (colony == null || string.IsNullOrEmpty(flatpackBlueprintUUID)) return false;

            var structure = colony.Structures.FirstOrDefault(s =>
                s.FlatpackBlueprintUUID == flatpackBlueprintUUID);
            if (structure == null)
            {
                Log.Warn("No matching ColonyStructure with FlatpackBlueprintUUID '{0}' on colony {1}",
                    flatpackBlueprintUUID, colony.ColonyName);
                return false;
            }

            structure.Properties.SetProperty(Constants.GameConstants.PropStaged, staged ? "True" : "False");
            return true;
        }

        /// <summary>
        /// Delivers or removes workers from a colony warehouse.
        /// When delivered: adds quantity to existing stack or creates new item.
        /// When undelivered: subtracts quantity (minimum 0).
        /// </summary>
        public static bool DeliverWorkers(Colony colony, string workerDetailID, string workerName, int quantity, bool delivered)
        {
            if (colony == null || string.IsNullOrEmpty(workerDetailID) || quantity <= 0) return false;

            var existing = colony.Items.FindByType(ItemType.ItemTypeEnum.WorkDetail, workerDetailID);

            if (delivered)
            {
                if (existing.Count > 0)
                {
                    existing[0].Quantity += quantity;
                }
                else
                {
                    var workerItem = new Item(ItemType.ItemTypeEnum.WorkDetail, workerDetailID);
                    workerItem.UUID = Guid.NewGuid().ToString();
                    workerItem.BaseItemTypeID = workerDetailID;
                    workerItem.Name = workerName;
                    workerItem.Quantity = quantity;
                    workerItem.Volume = Constants.GameConstants.WorkerVolume;
                    colony.Items.AddItem(workerItem);
                }
            }
            else
            {
                if (existing.Count > 0)
                {
                    existing[0].Quantity = Math.Max(0, existing[0].Quantity - quantity);
                }
            }

            return true;
        }
    }
}
