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
            { 1, "Worker Wages" },
            { 2, "Market Sale" },
            { 3, "Market Purchase" },
            { 4, "Refueling" },
            { 5, "Colony Income" },
            { 6, "Transfer Received" },
            { 7, "Transfer Sent" },
            { 8, "Job Payment" },
            { 9, "Bounty" },
            { 10, "Insurance Payout" },
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
