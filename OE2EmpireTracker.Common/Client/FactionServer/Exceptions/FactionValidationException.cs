// <copyright file="FactionValidationException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Thrown on HTTP 400 with parsed validation errors.
    /// </summary>
    [Serializable]
    public class FactionValidationException : FactionServerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionValidationException"/> class.
        /// </summary>
        public FactionValidationException()
            : base("Validation failed")
        {
            Errors = new List<FactionValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionValidationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public FactionValidationException(string message)
            : base(message)
        {
            Errors = new List<FactionValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionValidationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FactionValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            Errors = new List<FactionValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionValidationException"/> class.
        /// </summary>
        /// <param name="errors">The list of validation errors.</param>
        public FactionValidationException(List<FactionValidationError> errors)
            : base($"Validation failed with {errors.Count} error(s)")
        {
            Errors = errors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionValidationException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FactionValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Errors = new List<FactionValidationError>();
        }

        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public List<FactionValidationError> Errors { get; }
    }
}
