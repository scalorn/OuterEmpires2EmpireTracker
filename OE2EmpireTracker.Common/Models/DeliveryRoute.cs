using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// An ordered sequence of colony stops for planning deliveries.
    /// </summary>
    public class DeliveryRoute
    {
        public DeliveryRoute()
        {
            Stops = new List<RouteStop>();
        }

        public string UUID { get; internal set; }

        public string Name { get; internal set; } = string.Empty;

        public string OwnerUUID { get; internal set; } = string.Empty;

        public List<RouteStop> Stops { get; internal set; }
    }

    /// <summary>
    /// A single stop on a delivery route, referencing a colony, station, asteroid, or ship.
    /// </summary>
    public class RouteStop
    {
        [DefaultValue("")]
        public string ColonyUUID { get; internal set; } = string.Empty;
        public int Sequence { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DestinationType.Colony)]
        public DestinationType DestinationType { get; internal set; } = DestinationType.Colony;

        public string DestinationUUID { get; internal set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(RouteStopPurpose.Cargo)]
        public RouteStopPurpose Purpose { get; internal set; } = RouteStopPurpose.Cargo;

        public decimal FuelEstimate { get; internal set; } = 0m;
    }
}
