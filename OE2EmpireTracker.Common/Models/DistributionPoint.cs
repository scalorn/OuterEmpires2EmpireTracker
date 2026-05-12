namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// A single point on a yield distribution curve.
    /// </summary>
    public class DistributionPoint
    {
        /// <summary>Gets or sets the bin midpoint (X-axis value).</summary>
        public decimal BinMidpoint { get; set; }

        /// <summary>Gets or sets the percentage of surveys in this bin (Y-axis value).</summary>
        public decimal Percentage { get; set; }
    }
}
