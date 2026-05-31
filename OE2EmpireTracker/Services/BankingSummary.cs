namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Summary of banking transaction totals for a filtered set.
    /// </summary>
    public class BankingSummary
    {
        /// <summary>
        /// Gets or sets the total income (sum of positive credit changes).
        /// </summary>
        public decimal TotalIncome { get; set; }

        /// <summary>
        /// Gets or sets the total expenses (sum of absolute negative credit changes).
        /// </summary>
        public decimal TotalExpenses { get; set; }

        /// <summary>
        /// Gets or sets the net change (TotalIncome - TotalExpenses).
        /// </summary>
        public decimal NetChange { get; set; }
    }
}
