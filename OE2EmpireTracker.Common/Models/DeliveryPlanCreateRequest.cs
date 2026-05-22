namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying field values for creating a new delivery plan.
    /// No UUID (service assigns). No OwnerUUID (service sets from current player).
    /// </summary>
    public class DeliveryPlanCreateRequest
    {
        /// <summary>
        /// Gets or sets the name of the delivery plan.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UUID of the route this plan is based on.
        /// </summary>
        public string RouteUUID { get; set; } = string.Empty;
    }
}
