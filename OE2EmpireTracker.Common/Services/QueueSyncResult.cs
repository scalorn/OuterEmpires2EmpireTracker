using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Result of a queue-based sync cycle, containing success/failure counts and timing.
    /// </summary>
    public class QueueSyncResult
    {
        /// <summary>
        /// Gets or sets the number of work items that completed successfully.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the number of work items that failed.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the total elapsed time for the sync cycle.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Gets or sets the labels of work items that failed during the sync cycle.
        /// </summary>
        public List<string> FailedLabels { get; set; } = new List<string>();
    }
}
