// <copyright file="FactionValidationError.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// A single validation error from the Faction Server.
    /// </summary>
    public class FactionValidationError
    {
        /// <summary>
        /// Gets or sets the type of entity that failed validation.
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UUID of the entity that failed validation.
        /// </summary>
        public string EntityUUID { get; set; }

        /// <summary>
        /// Gets or sets the name of the field that failed validation.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the validation error message.
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }
}
