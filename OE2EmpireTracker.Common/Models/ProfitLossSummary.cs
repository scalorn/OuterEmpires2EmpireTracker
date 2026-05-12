using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Summary of profit/loss computed from a set of market transactions.
    /// </summary>
    public class ProfitLossSummary
    {
        /// <summary>Total revenue from all Sell transactions.</summary>
        public decimal TotalSalesRevenue { get; set; } = 0m;

        /// <summary>Total cost from all Buy transactions.</summary>
        public decimal TotalPurchaseCost { get; set; } = 0m;

        /// <summary>Net profit or loss (TotalSalesRevenue - TotalPurchaseCost).</summary>
        public decimal NetProfitLoss { get; set; } = 0m;

        /// <summary>
        /// Per-item breakdown keyed by ItemName.
        /// Each entry contains sales revenue, purchase cost, and net for that item.
        /// </summary>
        public Dictionary<string, ProfitLossItemBreakdown> ItemBreakdown { get; set; }
            = new Dictionary<string, ProfitLossItemBreakdown>();
    }

    /// <summary>
    /// Profit/loss breakdown for a single item name.
    /// </summary>
    public class ProfitLossItemBreakdown
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal SalesRevenue { get; set; } = 0m;
        public decimal PurchaseCost { get; set; } = 0m;
        public decimal NetProfitLoss { get; set; } = 0m;
        public int QuantitySold { get; set; } = 0;
        public int QuantityBought { get; set; } = 0;
    }
}
