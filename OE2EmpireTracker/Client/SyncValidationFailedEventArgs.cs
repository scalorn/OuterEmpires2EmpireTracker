// <copyright file="SyncValidationFailedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments raised when a bulk import to the server fails validation (HTTP 400).
    /// Contains the validation errors returned by the server.
    /// </summary>
    public class SyncValidationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the character UUID whose data failed validation.
        /// </summary>
        public string CharacterUUID { get; set; }

        /// <summary>
        /// Gets or sets the raw error response body from the server.
        /// </summary>
        public string ErrorResponseBody { get; set; }

        /// <summary>
        /// Gets or sets the list of individual validation error messages.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; set; }
    }
}
