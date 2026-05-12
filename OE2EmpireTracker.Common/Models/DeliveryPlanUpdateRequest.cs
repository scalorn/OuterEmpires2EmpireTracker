using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the current plan state from the ViewModel to the service for an update operation.
    /// </summary>
    public class DeliveryPlanUpdateRequest
    {
        public string Name { get; set; }

        public List<DeliveryPlanStop> Stops { get; set; }
    }
}