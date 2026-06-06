// <copyright file="WorkItem.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Represents a unit of work to be dispatched by the GameApiRequestQueue.
    /// </summary>
    public class WorkItem
    {
        /// <summary>
        /// Gets or sets a human-readable label identifying this work item (used for logging and error reporting).
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the async delegate that performs the work.
        /// Returns a list of cascading work items to enqueue upon completion (empty list if none).
        /// </summary>
        public Func<CancellationToken, Task<IReadOnlyList<WorkItem>>> ExecuteAsync { get; set; }
    }

    /// <summary>
    /// Records a failure that occurred during work item execution.
    /// </summary>
    public class QueueError
    {
        /// <summary>
        /// Gets or sets the label of the work item that failed.
        /// </summary>
        public string WorkItemLabel { get; set; }

        /// <summary>
        /// Gets or sets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Provides a summary of queue processing progress.
    /// </summary>
    public class QueueCompletionStatus
    {
        /// <summary>
        /// Gets or sets the total number of work items processed (succeeded + failed).
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of work items that completed successfully.
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the number of work items that failed with an exception.
        /// </summary>
        public int Failed { get; set; }
    }
}
