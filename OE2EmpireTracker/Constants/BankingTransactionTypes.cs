using System.Collections.Generic;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Maps integer transaction type codes from the game API to human-readable labels.
    /// </summary>
    public static class BankingTransactionTypes
    {
        /// <summary>
        /// Known transaction type codes and their display labels.
        /// </summary>
        public static readonly Dictionary<int, string> TypeLabels = new Dictionary<int, string>
        {
            { 1, "Refuelling" },
            { 2, "Transport Job" },
            { 3, "Market Sale" },
            { 4, "Broker Fee" },
            { 5, "Market Purchase" },
            { 6, "Buy Order Escrow" },
            { 7, "Ship Repair" },
            { 9, "Worker Wages" },
            { 10, "Colony Payout" },
            { 12, "Transfer" },
            { 13, "Insurance" },
            { 14, "Clone" },
            { 15, "Sales Tax" },
        };

        /// <summary>
        /// Returns the human-readable label for a transaction type code.
        /// Falls back to "{detail} ({typeCode})" for unknown codes, or "Unknown ({typeCode})" if detail is null/empty.
        /// </summary>
        /// <param name="typeCode">The integer transaction type code from the game API.</param>
        /// <param name="detail">The transaction detail string, used as fallback for unknown codes.</param>
        /// <returns>A human-readable label for the transaction type.</returns>
        public static string GetLabel(int typeCode, string detail)
        {
            if (TypeLabels.TryGetValue(typeCode, out string label))
            {
                return label;
            }

            if (!string.IsNullOrEmpty(detail))
            {
                return $"{detail} ({typeCode})";
            }

            return $"Unknown ({typeCode})";
        }
    }
}
