namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new delivery plan.
    /// No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class DeliveryPlanCreateRequest
    {
        public string Name { get; set; }

        public string RouteUUID { get; set; }
    }
}