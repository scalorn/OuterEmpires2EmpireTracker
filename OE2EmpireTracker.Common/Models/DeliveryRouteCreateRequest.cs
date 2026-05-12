using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new delivery route.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class DeliveryRouteCreateRequest
    {
        public string Name { get; set; }

        public List<RouteStop> Stops { get; set; }
    }
}
