using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OE2EmpireTracker.Models
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
    /// A single stop on a delivery route, referencing a colony, station, asteroid, or ship.
    /// </summary>
    public class RouteStop
    {
        public string ColonyUUID { get; set; }
        public int Sequence { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Colony)]
        public DestinationType DestinationType { get; set; } = DestinationType.Colony;

        public string DestinationUUID { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(RouteStopPurpose.Cargo)]
        public RouteStopPurpose Purpose { get; set; } = RouteStopPurpose.Cargo;

        public decimal FuelEstimate { get; set; } = 0m;
    }
}
