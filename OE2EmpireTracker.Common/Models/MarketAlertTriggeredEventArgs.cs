using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Event arguments for the <see cref="Services.MarketDataService.MarketAlertTriggered"/> event.
    /// Contains the alert that was triggered and the matching orders that caused it.
    /// </summary>
    public class MarketAlertTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketAlertTriggeredEventArgs"/> class.
        /// </summary>
        /// <param name="alert">The alert that was triggered.</param>
        /// <param name="matchingOrders">The orders that matched the alert criteria.</param>
        public MarketAlertTriggeredEventArgs(MarketAlert alert, IReadOnlyList<MarketListing> matchingOrders)
        {
            Alert = alert ?? throw new ArgumentNullException(nameof(alert));
            MatchingOrders = matchingOrders ?? throw new ArgumentNullException(nameof(matchingOrders));
        }

        /// <summary>
        /// Gets the alert that was triggered.
        /// </summary>
        public MarketAlert Alert { get; }

        /// <summary>
        /// Gets the orders that matched the alert criteria.
        /// </summary>
        public IReadOnlyList<MarketListing> MatchingOrders { get; }
    }
}
