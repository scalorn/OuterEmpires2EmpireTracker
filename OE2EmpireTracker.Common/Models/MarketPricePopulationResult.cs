// <copyright file="MarketPricePopulationResult.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Result of auto-populating a pricing plan from stored market price stats.
    /// Shows which resources were updated and which had no matching price data.
    /// </summary>
    public class MarketPricePopulationResult
    {
        /// <summary>
        /// Gets the list of resource keys that were updated with market prices.
        /// Keys are in the format "{ResourceName}|{Purity}".
        /// </summary>
        public List<string> UpdatedResources { get; set; } = new List<string>();

        /// <summary>
        /// Gets the list of resource keys that had no matching stored price stats.
        /// Keys are in the format "{ResourceName}|{Purity}".
        /// </summary>
        public List<string> MissingResources { get; set; } = new List<string>();
    }
}
