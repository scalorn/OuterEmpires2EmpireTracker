// <copyright file="ApiValidationError.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Represents a single field-level validation error returned by the API.
    /// </summary>
    public class ApiValidationError
    {
        /// <summary>
        /// Gets or sets the name of the field that failed validation.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the validation error message.
        /// </summary>
        public string Error { get; set; }
    }
}
