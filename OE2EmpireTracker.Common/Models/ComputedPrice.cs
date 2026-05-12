namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Result of a price computation. Not persisted.
    /// Price is always computed from available inputs; IsComplete indicates
    /// whether all inputs had price entries in the plan.
    /// </summary>
    public class ComputedPrice
    {
        public decimal Price { get; set; }
        public bool IsComplete { get; set; }
    }
}
