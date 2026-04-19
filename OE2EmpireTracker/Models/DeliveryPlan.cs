using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// A persisted delivery plan tied to a route, containing per-stop
    /// drop-off and pick-up item lists.
    /// </summary>
    public class DeliveryPlan
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string RouteUUID { get; set; } = string.Empty;
        public string ShipUUID { get; set; } = string.Empty;
        public bool Completed { get; set; } = false;
        public List<DeliveryPlanStop> Stops { get; set; }

        public DeliveryPlan()
        {
            Stops = new List<DeliveryPlanStop>();
        }

        /// <summary>
        /// Calculates what needs to be loaded before departure.
        /// A drop-off item needs pre-loading if it hasn't been picked up
        /// at an earlier stop in sufficient quantity.
        /// </summary>
        public List<DeliveryItem> CalculateLoadList()
        {
            var pickedUp = new Dictionary<string, int>();
            var needed = new Dictionary<string, DeliveryItem>();

            foreach (var stop in Stops.OrderBy(s => s.Sequence))
            {
                foreach (var dropItem in stop.DropOff)
                {
                    string key = $"{dropItem.ItemType}|{dropItem.BaseItemTypeID}|{dropItem.ResourcePurity}";
                    int available = 0;
                    pickedUp.TryGetValue(key, out available);

                    int shortfall = dropItem.Quantity - available;
                    if (shortfall > 0)
                    {
                        if (needed.ContainsKey(key))
                        {
                            needed[key].Quantity += shortfall;
                        }
                        else
                        {
                            needed[key] = new DeliveryItem
                            {
                                ItemType = dropItem.ItemType,
                                BaseItemTypeID = dropItem.BaseItemTypeID,
                                Name = dropItem.Name,
                                ResourcePurity = dropItem.ResourcePurity,
                                Quantity = shortfall
                            };
                        }
                        if (available > 0)
                            pickedUp[key] = 0;
                    }
                    else
                    {
                        pickedUp[key] = available - dropItem.Quantity;
                    }
                }

                foreach (var pickItem in stop.PickUp)
                {
                    string key = $"{pickItem.ItemType}|{pickItem.BaseItemTypeID}|{pickItem.ResourcePurity}";
                    int current = 0;
                    pickedUp.TryGetValue(key, out current);
                    pickedUp[key] = current + pickItem.Quantity;
                }
            }

            return needed.Values.OrderBy(i => i.Name).ToList();
        }
    }

    /// <summary>
    /// A single stop in a delivery plan with drop-off and pick-up lists.
    /// </summary>
    public class DeliveryPlanStop
    {
        public string ColonyUUID { get; set; }
        public int Sequence { get; set; }
        public bool StopCompleted { get; set; } = false;
        public List<DeliveryItem> DropOff { get; set; }
        public List<DeliveryItem> PickUp { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Colony)]
        public DestinationType DestinationType { get; set; } = DestinationType.Colony;

        public string DestinationUUID { get; set; } = string.Empty;

        public DeliveryPlanStop()
        {
            DropOff = new List<DeliveryItem>();
            PickUp = new List<DeliveryItem>();
        }
    }

    /// <summary>
    /// A single item in a delivery plan (drop-off or pick-up).
    /// </summary>
    public class DeliveryItem
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;
        public string BaseItemTypeID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ResourcePurity { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public bool Delivered { get; set; } = false;

        /// <summary>
        /// Display name including purity for resources (e.g. "Iron (High)").
        /// </summary>
        [JsonIgnore]
        public string ExtendedName
        {
            get
            {
                if (!string.IsNullOrEmpty(ResourcePurity))
                    return $"{Name} ({ResourcePurity})";
                return Name;
            }
        }
    }
}