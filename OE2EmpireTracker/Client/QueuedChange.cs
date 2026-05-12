// <copyright file="QueuedChange.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Represents a data change queued for later sync to the server.
    /// </summary>
    public class QueuedChange
    {
        /// <summary>
        /// Gets or sets the character UUID this change belongs to.
        /// </summary>
        public string CharacterUUID { get; set; }

        /// <summary>
        /// Gets or sets the data type (e.g. colonies, blueprints, surveys).
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets the JSON payload of the change.
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this change was queued.
        /// </summary>
        public DateTime QueuedUtc { get; set; }
    }
}
