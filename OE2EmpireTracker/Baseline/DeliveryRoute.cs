using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Baseline
{
    /// <summary>
    /// An ordered sequence of colony stops for planning deliveries.
    /// </summary>
    public class DeliveryRoute
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public List<RouteStop> Stops { get; set; }

        public DeliveryRoute()
        {
            Stops = new List<RouteStop>();
        }
    }

    /// <summary>
    /// A single stop on a delivery route, referencing a colony.
    /// </summary>
    public class RouteStop
    {
        public string ColonyUUID { get; set; }
        public int Sequence { get; set; }
    }
}
