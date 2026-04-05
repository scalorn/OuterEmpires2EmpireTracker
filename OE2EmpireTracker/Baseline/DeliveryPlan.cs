using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Baseline
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
        public List<DeliveryPlanStop> Stops { get; set; }

        public DeliveryPlan()
        {
            Stops = new List<DeliveryPlanStop>();
        }
    }

    /// <summary>
    /// A single stop in a delivery plan with drop-off and pick-up lists.
    /// </summary>
    public class DeliveryPlanStop
    {
        public string ColonyUUID { get; set; }
        public int Sequence { get; set; }
        public List<DeliveryItem> DropOff { get; set; }
        public List<DeliveryItem> PickUp { get; set; }

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
        public ItemType.ItemTypeEnum ItemType { get; set; } = Data.ItemType.ItemTypeEnum.None;
        public string BaseItemTypeID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public bool Delivered { get; set; } = false;
    }
}
